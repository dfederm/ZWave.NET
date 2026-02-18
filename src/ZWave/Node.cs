using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;
using ZWave.Serial.Commands;

namespace ZWave;

/// <summary>
/// Represents a Z-Wave network node.
/// </summary>
public sealed class Node : INode
{
    private readonly Driver _driver;

    private readonly ILogger _logger;

    private readonly AsyncAutoResetEvent _nodeInfoRecievedEvent = new AsyncAutoResetEvent();

    // Copy-on-write dictionary for lock-free reads. Writes are protected by _commandClassesWriteLock.
    // NOTE! Any operation which uses this dictionary MUST be aware that it can be replaced at any time, so reading it to a local variable is recommended.
    private volatile Dictionary<CommandClassId, CommandClass> _commandClasses = new Dictionary<CommandClassId, CommandClass>();
    private readonly object _commandClassesWriteLock = new object();

    private readonly object _interviewStateLock = new object();

    private Task? _interviewTask;

    private CancellationTokenSource? _interviewCancellationTokenSource;

    internal Node(byte id, Driver driver, ILogger logger)
    {
        Id = id;
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the node ID.
    /// </summary>
    public byte Id { get; }

    /// <summary>
    /// Gets the current interview status of the node.
    /// </summary>
    public NodeInterviewStatus InterviewStatus { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this node is an always-listening device.
    /// </summary>
    public bool IsListening { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this node supports routing.
    /// </summary>
    public bool IsRouting { get; private set; }

    /// <summary>
    /// Gets the communication speeds supported by this node.
    /// </summary>
    public IReadOnlyList<int> SupportedSpeeds { get; private set; } = Array.Empty<int>();

    /// <summary>
    /// Gets the Z-Wave protocol version of this node.
    /// </summary>
    public byte ProtocolVersion { get; private set; }

    /// <summary>
    /// Gets the type of this node (controller or end node).
    /// </summary>
    public NodeType NodeType { get; private set; }

    /// <summary>
    /// Gets the frequent listening mode of this node.
    /// </summary>
    public FrequentListeningMode FrequentListeningMode { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this node supports beaming.
    /// </summary>
    public bool SupportsBeaming { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this node supports security.
    /// </summary>
    public bool SupportsSecurity { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses
    {
        get
        {
            Dictionary<CommandClassId, CommandClass> commandClasses = _commandClasses;
            var commandClassInfos = new Dictionary<CommandClassId, CommandClassInfo>(commandClasses.Count);
            foreach (KeyValuePair<CommandClassId, CommandClass> pair in commandClasses)
            {
                commandClassInfos.Add(pair.Key, pair.Value.Info);
            }

            return commandClassInfos;
        }
    }

    /// <summary>
    /// Gets a specific command class by its CLR type.
    /// </summary>
    public TCommandClass GetCommandClass<TCommandClass>()
        where TCommandClass : CommandClass
        => (TCommandClass)GetCommandClass(CommandClassFactory.GetCommandClassId<TCommandClass>());

    /// <summary>
    /// Tries to get a specific command class by its CLR type.
    /// </summary>
    public bool TryGetCommandClass<TCommandClass>([NotNullWhen(true)]out TCommandClass? commandClass)
        where TCommandClass : CommandClass
    {
        if (TryGetCommandClass(CommandClassFactory.GetCommandClassId<TCommandClass>(), out CommandClass? commandClassBase))
        {
            commandClass = (TCommandClass)commandClassBase;
            return true;
    }
        else
        {
            commandClass = null;
            return false;
        }
    }

    /// <inheritdoc />
    public CommandClass GetCommandClass(CommandClassId commandClassId)
        => !TryGetCommandClass(commandClassId, out CommandClass? commandClass)
            ? throw new ZWaveException(ZWaveErrorCode.CommandClassNotImplemented, $"The command class {commandClassId} is not implemented by this node.")
            : commandClass;

    /// <summary>
    /// Tries to get a specific command class by its command class ID.
    /// </summary>
    public bool TryGetCommandClass(CommandClassId commandClassId, [NotNullWhen(true)] out CommandClass? commandClass)
        => _commandClasses.TryGetValue(commandClassId, out commandClass);

    /// <summary>
    /// Interviews the node.
    /// </summary>
    /// <remarks>
    /// the interview may take a very long time, so the returned task should generally not be awaited.
    /// </remarks>
    public async Task InterviewAsync(CancellationToken cancellationToken)
    {
        Task interviewTask;
        lock (_interviewStateLock)
        {
            InterviewStatus = NodeInterviewStatus.None;

            // Cancel any previous interview
            _interviewCancellationTokenSource?.Cancel();
            Task? previousInterviewTask = _interviewTask;

            _interviewCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _interviewTask = interviewTask = Task.Run(async () =>
            {
                CancellationToken cancellationToken = _interviewCancellationTokenSource.Token;

                // Wait for any previous interview to stop
                if (previousInterviewTask != null)
                {
                    try
                    {
                        await previousInterviewTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Swallow the cancellation as we just cancelled it.
                    }
                }

                // Reset the status again in case the previous interview task modified it.
                InterviewStatus = NodeInterviewStatus.None;

                var getNodeProtocolInfoRequest = GetNodeProtocolInfoRequest.Create(Id);
                GetNodeProtocolInfoResponse getNodeProtocolInfoResponse = await _driver.SendCommandAsync<GetNodeProtocolInfoRequest, GetNodeProtocolInfoResponse>(
                    getNodeProtocolInfoRequest,
                    cancellationToken).ConfigureAwait(false);
                IsListening = getNodeProtocolInfoResponse.IsListening;
                IsRouting = getNodeProtocolInfoResponse.IsRouting;
                SupportedSpeeds = getNodeProtocolInfoResponse.SupportedSpeeds;
                ProtocolVersion = getNodeProtocolInfoResponse.ProtocolVersion;
                NodeType = getNodeProtocolInfoResponse.NodeType;
                FrequentListeningMode = getNodeProtocolInfoResponse.FrequentListeningMode;
                SupportsBeaming = getNodeProtocolInfoResponse.SupportsBeaming;
                SupportsSecurity = getNodeProtocolInfoResponse.SupportsSecurity;
                // TODO: Log

                InterviewStatus = NodeInterviewStatus.ProtocolInfo;

                // This is all we need for the controller node
                if (Id == _driver.Controller.NodeId)
                {
                    InterviewStatus = NodeInterviewStatus.Complete;
                    return;
                }

                // This request causes unsolicited requests from the controller (kind of like a callback) with command id ApplicationControllerUpdate
                var requestNodeInfoRequest = RequestNodeInfoRequest.Create(Id);
                int requestNodeInfoRequestNum = 0;
                ResponseStatusResponse requestNodeInfoResponse;
                do
                {
                    requestNodeInfoResponse = await _driver.SendCommandAsync<RequestNodeInfoRequest, ResponseStatusResponse>(requestNodeInfoRequest, cancellationToken)
                        .ConfigureAwait(false);

                    if (requestNodeInfoRequestNum > 0)
                    {
                        await Task.Delay(100 * requestNodeInfoRequestNum);
                    }

                    requestNodeInfoRequestNum++;
                }
                while (!requestNodeInfoResponse.WasRequestAccepted); // If the command is rejected, retry.

                await _nodeInfoRecievedEvent.WaitAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                InterviewStatus = NodeInterviewStatus.NodeInfo;

                await InterviewCommandClassesAsync(cancellationToken).ConfigureAwait(false);
                InterviewStatus = NodeInterviewStatus.Complete;
            },
            cancellationToken);
        }

        await interviewTask.ConfigureAwait(false);
    }

    internal void NotifyNodeInfoReceived(ApplicationUpdateRequest nodeInfoReceived)
    {
        // TODO: Log
        AddCommandClasses(nodeInfoReceived.Generic.CommandClasses);

        _nodeInfoRecievedEvent.Set();
    }

    private void AddCommandClasses(IReadOnlyList<CommandClassInfo> commandClassInfos)
    {
        if (commandClassInfos.Count == 0)
        {
            return;
        }

        lock (_commandClassesWriteLock)
        {
            Dictionary<CommandClassId, CommandClass> currentDict = _commandClasses;

            // First pass: check if we need to create a new dictionary
            bool needsNewDict = false;
            foreach (CommandClassInfo commandClassInfo in commandClassInfos)
            {
                if (!currentDict.ContainsKey(commandClassInfo.CommandClass))
                {
                    needsNewDict = true;
                    break;
                }
            }

            if (needsNewDict)
            {
                // Copy-on-write: create new dictionary with all entries
                var newDict = new Dictionary<CommandClassId, CommandClass>(currentDict.Count + commandClassInfos.Count);
                foreach (KeyValuePair<CommandClassId, CommandClass> pair in currentDict)
                {
                    newDict.Add(pair.Key, pair.Value);
                }

                foreach (CommandClassInfo commandClassInfo in commandClassInfos)
                {
                    if (newDict.TryGetValue(commandClassInfo.CommandClass, out CommandClass? existingCommandClass))
                    {
                        existingCommandClass.MergeInfo(commandClassInfo);
                    }
                    else
                    {
                        CommandClass commandClass = CommandClassFactory.Create(commandClassInfo, _driver, this);
                        newDict.Add(commandClassInfo.CommandClass, commandClass);
                    }
                }

                _commandClasses = newDict;
            }
            else
            {
                // All command classes already exist, just merge
                foreach (CommandClassInfo commandClassInfo in commandClassInfos)
                {
                    currentDict[commandClassInfo.CommandClass].MergeInfo(commandClassInfo);
                }
            }
        }
    }

    private async Task InterviewCommandClassesAsync(CancellationToken cancellationToken)
    {
        /*
            Command classes may depend on other command classes, so we need to interview them in topographical order.
            Instead of sorting them completely out of the gate, we'll just create a list of all the command classes (list A) and if its dependencies
            are met interview it and if not add to another list (list B). After exhausing the list A, swap list A and B and repeat until both are empty.
        */
        Dictionary<CommandClassId, CommandClass> currentCommandClasses = _commandClasses;
        Queue<CommandClass> commandClasses = new(currentCommandClasses.Count);
        foreach ((_, CommandClass commandClass) in currentCommandClasses)
        {
            commandClasses.Enqueue(commandClass);
        }

        HashSet<CommandClassId> interviewedCommandClasses = new(currentCommandClasses.Count);
        Queue<CommandClass> blockedCommandClasses = new(currentCommandClasses.Count);
        while (commandClasses.Count > 0)
        {
            while (commandClasses.Count > 0)
            {
                CommandClass commandClass = commandClasses.Dequeue();
                CommandClassId commandClassId = commandClass.Info.CommandClass;

                bool isBlocked = false;
                CommandClassId[] commandClassDependencies = commandClass.Dependencies;
                for (int i = 0; i < commandClassDependencies.Length; i++)
                {
                    if (!interviewedCommandClasses.Contains(commandClassDependencies[i]))
                    {
                        isBlocked = true;
                        break;
                    }
                }

                if (isBlocked)
                {
                    blockedCommandClasses.Enqueue(commandClass);
                }
                else
                {
                    await commandClass.InterviewAsync(cancellationToken);
                    interviewedCommandClasses.Add(commandClassId);
                }
            }

            Queue<CommandClass> tmp = commandClasses;
            commandClasses = blockedCommandClasses;
            blockedCommandClasses = tmp;
        }
    }

    internal void ProcessCommand(CommandClassFrame frame)
    {
        if (!TryGetCommandClass(frame.CommandClassId, out CommandClass? commandClass))
        {
            // TODO: Log
            return;
        }

        commandClass.ProcessCommand(frame);
    }
}
