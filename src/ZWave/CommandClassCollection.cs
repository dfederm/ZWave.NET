using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;

namespace ZWave;

/// <summary>
/// Manages a copy-on-write collection of command classes for an endpoint.
/// </summary>
/// <remarks>
/// This class encapsulates the copy-on-write dictionary pattern, command class lookup,
/// interview orchestration, and command routing shared by <see cref="Node"/> (endpoint 0)
/// and <see cref="Endpoint"/> (endpoints 1–127).
/// </remarks>
internal sealed class CommandClassCollection
{
    private readonly IDriver _driver;
    private readonly IEndpoint _endpoint;
    private readonly ILogger _logger;

    // Copy-on-write dictionary for lock-free reads. Writes are protected by _writeLock.
    private volatile Dictionary<CommandClassId, CommandClass> _commandClasses = new Dictionary<CommandClassId, CommandClass>();
    private readonly object _writeLock = new object();

    internal CommandClassCollection(IDriver driver, IEndpoint endpoint, ILogger logger)
    {
        _driver = driver;
        _endpoint = endpoint;
        _logger = logger;
    }

    internal IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses
    {
        get
        {
            Dictionary<CommandClassId, CommandClass> commandClasses = _commandClasses;
            Dictionary<CommandClassId, CommandClassInfo> result = new Dictionary<CommandClassId, CommandClassInfo>(commandClasses.Count);
            foreach (KeyValuePair<CommandClassId, CommandClass> pair in commandClasses)
            {
                result.Add(pair.Key, pair.Value.Info);
            }

            return result;
        }
    }

    internal CommandClass GetCommandClass(CommandClassId commandClassId)
    {
        if (!TryGetCommandClass(commandClassId, out CommandClass? commandClass))
        {
            ZWaveException.Throw(ZWaveErrorCode.CommandClassNotImplemented, $"The command class {commandClassId} is not supported.");
        }

        return commandClass;
    }

    internal TCommandClass GetCommandClass<TCommandClass>()
        where TCommandClass : CommandClass
        => (TCommandClass)GetCommandClass(CommandClassFactory.GetCommandClassId<TCommandClass>());

    internal bool TryGetCommandClass(CommandClassId commandClassId, [NotNullWhen(true)] out CommandClass? commandClass)
        => _commandClasses.TryGetValue(commandClassId, out commandClass);

    internal bool TryGetCommandClass<TCommandClass>([NotNullWhen(true)] out TCommandClass? commandClass)
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

    internal void AddCommandClasses(IReadOnlyList<CommandClassInfo> commandClassInfos)
    {
        if (commandClassInfos.Count == 0)
        {
            return;
        }

        lock (_writeLock)
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
                Dictionary<CommandClassId, CommandClass> newDict = new Dictionary<CommandClassId, CommandClass>(currentDict.Count + commandClassInfos.Count);
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
                        CommandClass commandClass = CommandClassFactory.Create(commandClassInfo, _driver, _endpoint, _logger);
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

    internal void ProcessCommand(CommandClassFrame frame)
    {
        if (!TryGetCommandClass(frame.CommandClassId, out CommandClass? commandClass))
        {
            // TODO: Log
            return;
        }

        commandClass.ProcessCommand(frame);
    }

    internal async Task InterviewCommandClassesAsync(
        CommandClassCategory category,
        HashSet<CommandClassId> interviewedCommandClasses,
        CancellationToken cancellationToken)
    {
        /*
            Command classes may depend on other command classes, so we need to interview them in topographical order.
            Instead of sorting them completely out of the gate, we'll just create a list of all the command classes (list A) and if its dependencies
            are met interview it and if not add to another list (list B). After exhausing the list A, swap list A and B and repeat until both are empty.
        */
        Dictionary<CommandClassId, CommandClass> currentCommandClasses = _commandClasses;
        Queue<CommandClass> commandClasses = new Queue<CommandClass>(currentCommandClasses.Count);
        foreach ((_, CommandClass commandClass) in currentCommandClasses)
        {
            if (commandClass.Category == category)
            {
                commandClasses.Enqueue(commandClass);
            }
        }

        Queue<CommandClass> blockedCommandClasses = new Queue<CommandClass>(commandClasses.Count);
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
}
