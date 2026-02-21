using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Defines RF transmit power levels relative to normal power.
/// </summary>
public enum Powerlevel : byte
{
    Normal = 0x00,

    Minus1dBm = 0x01,

    Minus2dBm = 0x02,

    Minus3dBm = 0x03,

    Minus4dBm = 0x04,

    Minus5dBm = 0x05,

    Minus6dBm = 0x06,

    Minus7dBm = 0x07,

    Minus8dBm = 0x08,

    Minus9dBm = 0x09,
}

public enum PowerlevelTestStatus : byte
{
    /// <summary>
    /// No frame was returned during the test
    /// </summary>
    Failed = 0x00,

    /// <summary>
    /// At least 1 frame was returned during the test
    /// </summary>
    Success = 0x01,

    /// <summary>
    /// The test is still ongoing
    /// </summary>
    InProgress = 0x02,
}

public enum PowerlevelCommand : byte
{
    /// <summary>
    /// Set the power level indicator value, which should be used by the node when transmitting RF.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current power level value.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current power level.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Instruct the destination node to transmit a number of test frames to the specified NodeID with the RF
    /// power level specified.
    /// </summary>
    TestNodeSet = 0x04,

    /// <summary>
    /// Request the result of the latest Powerlevel Test
    /// </summary>
    TestNodeGet = 0x05,

    /// <summary>
    /// Report the latest result of a test frame transmission started by the Powerlevel Test Node Set Command.
    /// </summary>
    TestNodeReport = 0x06,
}

/// <summary>
/// Represents a Powerlevel Report received from a device.
/// </summary>
public readonly record struct PowerlevelReport(
    /// <summary>
    /// The current power level indicator value in effect on the node
    /// </summary>
    Powerlevel Powerlevel,

    /// <summary>
    /// The time in seconds the node has back at Power level before resetting to normal Power level.
    /// </summary>
    /// <remarks>
    /// May be null when <see cref="Powerlevel"/> is <see cref="Powerlevel.Normal"/>.
    /// </remarks>
    byte? TimeoutInSeconds);

/// <summary>
/// Represents the result of a powerlevel test.
/// </summary>
public readonly record struct PowerlevelTestResult(
    /// <summary>
    /// The node ID of the node which is or has been under test.
    /// </summary>
    byte NodeId,

    /// <summary>
    /// The result of the test
    /// </summary>
    PowerlevelTestStatus Status,

    /// <summary>
    /// The number of test frames transmitted which the Test NodeID has acknowledged.
    /// </summary>
    ushort FrameAcknowledgedCount);

[CommandClass(CommandClassId.Powerlevel)]
public sealed class PowerlevelCommandClass : CommandClass<PowerlevelCommand>
{
    public PowerlevelCommandClass(CommandClassInfo info, IDriver driver, INode node, ILogger logger)
        : base(info, driver, node, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public PowerlevelReport? LastReport { get; private set; }

    /// <summary>
    /// Gets the last powerlevel test result.
    /// </summary>
    public PowerlevelTestResult? LastTestResult { get; private set; }

    public override bool? IsCommandSupported(PowerlevelCommand command) => true;

    /// <summary>
    /// Set the power level indicator value, which should be used by the node when transmitting RF.
    /// </summary>
    /// <param name="powerlevel">
    /// The power level indicator value, which should be used by the node when transmitting RF.
    /// </param>
    /// <param name="timeoutInSeconds">
    /// The timeout in seconds for this power level indicator value before returning the power level defined by
    /// the application. Must be non-zero unless the power level is <see cref="Powerlevel.Normal"/>.
    /// </param>
    public async Task SetAsync(
        Powerlevel powerlevel,
        byte timeoutInSeconds,
        CancellationToken cancellationToken)
    {
        if (timeoutInSeconds == 0 && powerlevel != Powerlevel.Normal)
        {
            throw new ArgumentException("Timeout must be non-zero", nameof(timeoutInSeconds));
        }

        var command = PowerlevelSetCommand.Create(powerlevel, timeoutInSeconds);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current power level value.
    /// </summary>
    public async Task<PowerlevelReport> GetAsync(CancellationToken cancellationToken)
    {
        var command = PowerlevelGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<PowerlevelReportCommand>(cancellationToken).ConfigureAwait(false);
        PowerlevelReport report = PowerlevelReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        return report;
    }

    /// <summary>
    /// Instruct the destination node to transmit a number of test frames to the specified NodeID with the RF
    /// power level specified.
    /// </summary>
    public async Task<PowerlevelTestResult> TestNodeAsync(
        byte testNodeId,
        Powerlevel powerlevel,
        ushort testFrameCount,
        CancellationToken cancellationToken)
    {
        if (testNodeId == Node.Id)
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
            throw new ZWaveException(
                ZWaveErrorCode.CommandInvalidArgument,
                $"The test node {testNodeId} is FLiRS and cannot be used for a Powerlevel test");
        }

        // TODO: Throw if test node is sleeping

        var command = PowerlevelTestNodeSetCommand.Create(testNodeId, powerlevel, testFrameCount);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<PowerlevelTestNodeReportCommand>(cancellationToken).ConfigureAwait(false);
        PowerlevelTestResult? result = PowerlevelTestNodeReportCommand.Parse(reportFrame, Logger);
        LastTestResult = result;
        return LastTestResult!.Value;
    }

    /// <summary>
    /// Request the result of the latest Powerlevel Test
    /// </summary>
    public async Task<PowerlevelTestResult?> GetLastTestResultsAsync(CancellationToken cancellationToken)
    {
        var command = PowerlevelTestNodeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<PowerlevelTestNodeReportCommand>(cancellationToken).ConfigureAwait(false);
        PowerlevelTestResult? result = PowerlevelTestNodeReportCommand.Parse(reportFrame, Logger);
        LastTestResult = result;
        return result;
    }

    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((PowerlevelCommand)frame.CommandId)
        {
            case PowerlevelCommand.Set:
            case PowerlevelCommand.Get:
            case PowerlevelCommand.TestNodeSet:
            case PowerlevelCommand.TestNodeGet:
            {
                break;
            }
            case PowerlevelCommand.Report:
            {
                LastReport = PowerlevelReportCommand.Parse(frame, Logger);
                break;
            }
            case PowerlevelCommand.TestNodeReport:
            {
                LastTestResult = PowerlevelTestNodeReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct PowerlevelSetCommand : ICommand
    {
        public PowerlevelSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.Set;

        public CommandClassFrame Frame { get; }

        public static PowerlevelSetCommand Create(Powerlevel powerlevel, byte timeoutInSeconds)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)powerlevel, timeoutInSeconds];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new PowerlevelSetCommand(frame);
        }
    }

    private readonly struct PowerlevelGetCommand : ICommand
    {
        public PowerlevelGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.Get;

        public CommandClassFrame Frame { get; }

        public static PowerlevelSetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new PowerlevelSetCommand(frame);
        }
    }

    private readonly struct PowerlevelReportCommand : ICommand
    {
        public PowerlevelReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.Report;

        public CommandClassFrame Frame { get; }

        public static PowerlevelReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Powerlevel Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Powerlevel Report frame is too short");
            }

            Powerlevel powerlevel = (Powerlevel)frame.CommandParameters.Span[0];
            byte? timeoutInSeconds = powerlevel != Powerlevel.Normal && frame.CommandParameters.Length > 1
                ? frame.CommandParameters.Span[1]
                : null;
            return new PowerlevelReport(powerlevel, timeoutInSeconds);
        }
    }

    private readonly struct PowerlevelTestNodeSetCommand : ICommand
    {
        public PowerlevelTestNodeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.TestNodeSet;

        public CommandClassFrame Frame { get; }

        public static PowerlevelSetCommand Create(
            byte testNodeId,
            Powerlevel powerlevel,
            ushort testFrameCount)
        {
            Span<byte> commandParameters = stackalloc byte[4];
            commandParameters[0] = testNodeId;
            commandParameters[1] = (byte)powerlevel;
            testFrameCount.WriteBytesBE(commandParameters[2..4]);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new PowerlevelSetCommand(frame);
        }
    }

    private readonly struct PowerlevelTestNodeGetCommand : ICommand
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

    private readonly struct PowerlevelTestNodeReportCommand : ICommand
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
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Powerlevel Test Node Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Powerlevel Test Node Report frame is too short");
            }

            byte nodeId = frame.CommandParameters.Span[0];
            if (nodeId == 0)
            {
                return null;
            }

            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Powerlevel Test Node Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Powerlevel Test Node Report frame is too short");
            }

            PowerlevelTestStatus status = (PowerlevelTestStatus)frame.CommandParameters.Span[1];
            ushort frameAcknowledgedCount = frame.CommandParameters.Span[2..4].ToUInt16BE();
            return new PowerlevelTestResult(nodeId, status, frameAcknowledgedCount);
        }
    }
}
