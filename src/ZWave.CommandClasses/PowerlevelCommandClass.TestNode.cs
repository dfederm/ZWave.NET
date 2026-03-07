using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Defines the status of a Powerlevel test operation.
/// </summary>
public enum PowerlevelTestStatus : byte
{
    /// <summary>
    /// No frame was returned during the test.
    /// </summary>
    Failed = 0x00,

    /// <summary>
    /// At least 1 frame was returned during the test.
    /// </summary>
    Success = 0x01,

    /// <summary>
    /// The test is still ongoing.
    /// </summary>
    InProgress = 0x02,
}

/// <summary>
/// Represents the result of a powerlevel test.
/// </summary>
public readonly record struct PowerlevelTestResult(
    /// <summary>
    /// The node ID of the node which is or has been under test.
    /// </summary>
    ushort NodeId,

    /// <summary>
    /// The result of the test.
    /// </summary>
    PowerlevelTestStatus Status,

    /// <summary>
    /// The number of test frames transmitted which the Test NodeID has acknowledged.
    /// </summary>
    ushort FrameAcknowledgedCount);

public sealed partial class PowerlevelCommandClass
{
    /// <summary>
    /// Gets the last powerlevel test result.
    /// </summary>
    public PowerlevelTestResult? LastTestResult { get; private set; }

    /// <summary>
    /// Event raised when a Powerlevel Test Node Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<PowerlevelTestResult>? OnTestNodeReportReceived;

    /// <summary>
    /// Instruct the destination node to transmit a number of test frames to the specified NodeID with the RF
    /// power level specified. The result of the test may be requested with <see cref="GetLastTestResultsAsync"/>,
    /// or received via the <see cref="OnTestNodeReportReceived"/> event if the node sends an unsolicited report.
    /// </summary>
    /// <param name="testNodeId">The test NodeID that should receive the test frames.</param>
    /// <param name="powerlevel">The power level indicator value to use in the test frame transmission.</param>
    /// <param name="testFrameCount">The number of test frames to transmit. Valid range is 1..65535.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task TestNodeSetAsync(
        ushort testNodeId,
        Powerlevel powerlevel,
        ushort testFrameCount,
        CancellationToken cancellationToken)
    {
        if (testNodeId == Endpoint.NodeId)
        {
            throw new ArgumentException("The test node must be different from the node performing the test.", nameof(testNodeId));
        }

        INode? testNode = Driver.GetNode(testNodeId);
        if (testNode == null)
        {
            throw new ArgumentException($"The test node {testNodeId} does not exist.", nameof(testNodeId));
        }

        if (testNode.FrequentListeningMode != FrequentListeningMode.None)
        {
            ZWaveException.Throw(
                ZWaveErrorCode.CommandInvalidArgument,
                $"The test node {testNodeId} is FLiRS and cannot be used for a Powerlevel test");
        }

        // TODO: Throw if test node is sleeping

        var command = PowerlevelTestNodeSetCommand.Create(testNodeId, powerlevel, testFrameCount);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the result of the latest Powerlevel Test.
    /// </summary>
    public async Task<PowerlevelTestResult?> GetLastTestResultsAsync(CancellationToken cancellationToken)
    {
        PowerlevelTestNodeGetCommand command = PowerlevelTestNodeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<PowerlevelTestNodeReportCommand>(cancellationToken).ConfigureAwait(false);
        PowerlevelTestResult? result = PowerlevelTestNodeReportCommand.Parse(reportFrame, Logger);
        LastTestResult = result;
        if (result.HasValue)
        {
            OnTestNodeReportReceived?.Invoke(result.Value);
        }

        return result;
    }

    internal readonly struct PowerlevelTestNodeSetCommand : ICommand
    {
        public PowerlevelTestNodeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.TestNodeSet;

        public CommandClassFrame Frame { get; }

        public static PowerlevelTestNodeSetCommand Create(
            ushort testNodeId,
            Powerlevel powerlevel,
            ushort testFrameCount)
        {
            Span<byte> commandParameters = stackalloc byte[4];
            commandParameters[0] = (byte)testNodeId;
            commandParameters[1] = (byte)powerlevel;
            testFrameCount.WriteBytesBE(commandParameters[2..4]);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new PowerlevelTestNodeSetCommand(frame);
        }
    }

    internal readonly struct PowerlevelTestNodeGetCommand : ICommand
    {
        public PowerlevelTestNodeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.TestNodeGet;

        public CommandClassFrame Frame { get; }

        public static PowerlevelTestNodeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new PowerlevelTestNodeGetCommand(frame);
        }
    }

    internal readonly struct PowerlevelTestNodeReportCommand : ICommand
    {
        public PowerlevelTestNodeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.TestNodeReport;

        public CommandClassFrame Frame { get; }

        public static PowerlevelTestResult? Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Powerlevel Test Node Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Powerlevel Test Node Report frame is too short");
            }

            ushort nodeId = frame.CommandParameters.Span[0];
            if (nodeId == 0)
            {
                return null;
            }

            PowerlevelTestStatus status = (PowerlevelTestStatus)frame.CommandParameters.Span[1];
            ushort frameAcknowledgedCount = frame.CommandParameters.Span[2..4].ToUInt16BE();
            return new PowerlevelTestResult(nodeId, status, frameAcknowledgedCount);
        }
    }
}
