using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using ZWave.CommandClasses;
using ZWave.Serial.Commands;

namespace ZWave;

/// <summary>
/// Represents a Z-Wave network node.
/// </summary>
/// <remarks>
/// A node IS endpoint 0 (the "Root Device"). Endpoints 1–127 are child endpoints
/// discovered via the Multi Channel Command Class. Each endpoint has its own set of
/// supported command classes.
/// </remarks>
public sealed class Node : INode
{
    private readonly Driver _driver;

    private readonly AsyncAutoResetEvent _nodeInfoRecievedEvent = new AsyncAutoResetEvent();

    // Command class storage, shared implementation with Endpoint via composition.
    private readonly CommandClassCollection _commandClassCollection;

    // Child endpoints (1–127). TODO: Populate.
    private readonly Dictionary<byte, Endpoint> _endpoints = [];

    private readonly object _interviewStateLock = new object();

    private Task? _interviewTask;

    private CancellationTokenSource? _interviewCancellationTokenSource;

    internal Node(ushort id, Driver driver, ILogger logger)
    {
        Id = id;
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _commandClassCollection = new CommandClassCollection(driver, this, logger);
    }

    /// <summary>
    /// Gets the node ID.
    /// </summary>
    public ushort Id { get; }

    /// <inheritdoc />
    ushort IEndpoint.NodeId => Id;

    /// <inheritdoc />
    byte IEndpoint.EndpointIndex => 0;

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
    public IReadOnlyList<int> SupportedSpeeds { get; private set; } = [];

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
    public IReadOnlyDictionary<CommandClassId, CommandClassInfo> CommandClasses => _commandClassCollection.CommandClasses;

    /// <summary>
    /// Gets a specific command class by its CLR type.
    /// </summary>
    public TCommandClass GetCommandClass<TCommandClass>()
        where TCommandClass : CommandClass
        => _commandClassCollection.GetCommandClass<TCommandClass>();

    /// <summary>
    /// Tries to get a specific command class by its CLR type.
    /// </summary>
    public bool TryGetCommandClass<TCommandClass>([NotNullWhen(true)] out TCommandClass? commandClass)
        where TCommandClass : CommandClass
        => _commandClassCollection.TryGetCommandClass(out commandClass);

    /// <inheritdoc />
    public CommandClass GetCommandClass(CommandClassId commandClassId)
        => _commandClassCollection.GetCommandClass(commandClassId);

    /// <summary>
    /// Tries to get a specific command class by its command class ID.
    /// </summary>
    public bool TryGetCommandClass(CommandClassId commandClassId, [NotNullWhen(true)] out CommandClass? commandClass)
        => _commandClassCollection.TryGetCommandClass(commandClassId, out commandClass);

    /// <summary>
    /// Gets the child endpoints (1–127) discovered via the Multi Channel Command Class.
    /// </summary>
    /// <remarks>
    /// This does not include endpoint 0 (the Root Device / this node). Use the node directly
    /// for endpoint 0 operations.
    /// </remarks>
    public IReadOnlyDictionary<byte, Endpoint> Endpoints => _endpoints;

    /// <summary>
    /// Gets a specific endpoint by its index.
    /// </summary>
    /// <param name="endpointIndex">The endpoint index. Zero returns this node as an <see cref="IEndpoint"/>.</param>
    /// <returns>The endpoint, or this node if <paramref name="endpointIndex"/> is zero.</returns>
    /// <exception cref="KeyNotFoundException">The endpoint does not exist.</exception>
    public IEndpoint GetEndpoint(byte endpointIndex)
    {
        if (endpointIndex == 0)
        {
            return this;
        }

        Dictionary<byte, Endpoint> endpoints = _endpoints;
        if (endpoints.TryGetValue(endpointIndex, out Endpoint? endpoint))
        {
            return endpoint;
        }

        throw new KeyNotFoundException($"Endpoint {endpointIndex} does not exist on node {Id}.");
    }

    /// <summary>
    /// Gets all endpoints, including this node as endpoint 0.
    /// </summary>
    public IEnumerable<IEndpoint> GetAllEndpoints()
    {
        yield return this;

        Dictionary<byte, Endpoint> endpoints = _endpoints;
        foreach (KeyValuePair<byte, Endpoint> pair in endpoints)
        {
            yield return pair.Value;
        }
    }

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

                var getNodeProtocolInfoRequest = GetNodeInformationProtocolDataRequest.Create(Id, _driver.NodeIdType);
                GetNodeInformationProtocolDataResponse getNodeProtocolInfoResponse = await _driver.SendCommandAsync<GetNodeInformationProtocolDataRequest, GetNodeInformationProtocolDataResponse>(
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
                var requestNodeInfoRequest = RequestNodeInfoRequest.Create(Id, _driver.NodeIdType);
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

                await _commandClassCollection.InterviewCommandClassesAsync(cancellationToken).ConfigureAwait(false);
                InterviewStatus = NodeInterviewStatus.Complete;
            },
            cancellationToken);
        }

        await interviewTask.ConfigureAwait(false);
    }

    internal void NotifyNodeInfoReceived(ApplicationUpdateRequest nodeInfoReceived)
    {
        // TODO: Log
        _commandClassCollection.AddCommandClasses(nodeInfoReceived.Generic.CommandClasses);

        _nodeInfoRecievedEvent.Set();
    }

    internal void ProcessCommand(CommandClassFrame frame, byte endpointIndex)
    {
        if (endpointIndex == 0)
        {
            _commandClassCollection.ProcessCommand(frame);
        }
        else
        {
            Dictionary<byte, Endpoint> endpoints = _endpoints;
            if (endpoints.TryGetValue(endpointIndex, out Endpoint? endpoint))
            {
                endpoint.ProcessCommand(frame);
            }
            else
            {
                // TODO: Log unknown endpoint
            }
        }
    }
}
