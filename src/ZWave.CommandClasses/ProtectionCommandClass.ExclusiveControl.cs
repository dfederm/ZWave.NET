using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class ProtectionCommandClass
{
    /// <summary>
    /// Gets the node ID that has exclusive control over this device.
    /// A value of 0 means no exclusive control is set.
    /// </summary>
    public byte? ExclusiveControlNodeId { get; private set; }

    /// <summary>
    /// Event raised when a Protection Exclusive Control Report is received.
    /// </summary>
    public event Action<byte>? OnExclusiveControlReportReceived;

    /// <summary>
    /// Request the exclusive control node ID from a device.
    /// </summary>
    public async Task<byte> GetExclusiveControlAsync(CancellationToken cancellationToken)
    {
        ProtectionExclusiveControlGetCommand command = ProtectionExclusiveControlGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ProtectionExclusiveControlReportCommand>(cancellationToken).ConfigureAwait(false);
        byte nodeId = ProtectionExclusiveControlReportCommand.Parse(reportFrame, Logger);
        ExclusiveControlNodeId = nodeId;
        OnExclusiveControlReportReceived?.Invoke(nodeId);
        return nodeId;
    }

    /// <summary>
    /// Set the node ID that has exclusive control over a device.
    /// </summary>
    /// <param name="nodeId">
    /// The node ID to grant exclusive control. Use 0 to reset exclusive control.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetExclusiveControlAsync(byte nodeId, CancellationToken cancellationToken)
    {
        var command = ProtectionExclusiveControlSetCommand.Create(nodeId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ProtectionExclusiveControlSetCommand : ICommand
    {
        public ProtectionExclusiveControlSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.ExclusiveControlSet;

        public CommandClassFrame Frame { get; }

        public static ProtectionExclusiveControlSetCommand Create(byte nodeId)
        {
            ReadOnlySpan<byte> commandParameters = [nodeId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionExclusiveControlSetCommand(frame);
        }
    }

    internal readonly struct ProtectionExclusiveControlGetCommand : ICommand
    {
        public ProtectionExclusiveControlGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.ExclusiveControlGet;

        public CommandClassFrame Frame { get; }

        public static ProtectionExclusiveControlGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ProtectionExclusiveControlGetCommand(frame);
        }
    }

    internal readonly struct ProtectionExclusiveControlReportCommand : ICommand
    {
        public ProtectionExclusiveControlReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.ExclusiveControlReport;

        public CommandClassFrame Frame { get; }

        public static ProtectionExclusiveControlReportCommand Create(byte nodeId)
        {
            ReadOnlySpan<byte> commandParameters = [nodeId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionExclusiveControlReportCommand(frame);
        }

        public static byte Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Protection Exclusive Control Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Protection Exclusive Control Report frame is too short");
            }

            return frame.CommandParameters.Span[0];
        }
    }
}
