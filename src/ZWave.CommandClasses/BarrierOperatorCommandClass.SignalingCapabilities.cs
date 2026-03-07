using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class BarrierOperatorCommandClass
{
    /// <summary>
    /// Gets the signaling subsystem types supported by this device,
    /// or <see langword="null"/> if not yet queried.
    /// </summary>
    public IReadOnlySet<BarrierOperatorSignalingSubsystemType>? SupportedSignalingSubsystems { get; private set; }

    /// <summary>
    /// Queries the device for its supported signaling subsystem types.
    /// </summary>
    public async Task<IReadOnlySet<BarrierOperatorSignalingSubsystemType>> GetSupportedSignalingSubsystemsAsync(
        CancellationToken cancellationToken)
    {
        SignalSupportedGetCommand command = SignalSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<SignalSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        IReadOnlySet<BarrierOperatorSignalingSubsystemType> supported = SignalSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedSignalingSubsystems = supported;
        return supported;
    }

    internal readonly struct SignalSupportedGetCommand : ICommand
    {
        public SignalSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.SignalSupportedGet;

        public CommandClassFrame Frame { get; }

        public static SignalSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new SignalSupportedGetCommand(frame);
        }
    }

    internal readonly struct SignalSupportedReportCommand : ICommand
    {
        public SignalSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.BarrierOperator;

        public static byte CommandId => (byte)BarrierOperatorCommand.SignalSupportedReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Parses the signaling capabilities bitmask.
        /// Per spec, bit 0 of byte 0 indicates subsystem type 0x01, bit 1 indicates type 0x02, etc.
        /// </summary>
        public static IReadOnlySet<BarrierOperatorSignalingSubsystemType> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Barrier Operator Signal Supported Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Barrier Operator Signal Supported Report frame is too short");
            }

            return BitMaskHelper.ParseBitMask<BarrierOperatorSignalingSubsystemType>(frame.CommandParameters.Span, offset: 1);
        }
    }
}
