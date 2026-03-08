using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the protection capabilities of a device.
/// </summary>
public readonly record struct ProtectionSupportedReport(
    /// <summary>
    /// Whether the device supports exclusive control.
    /// </summary>
    bool SupportsExclusiveControl,

    /// <summary>
    /// Whether the device supports an RF protection timeout.
    /// </summary>
    bool SupportsTimeout,

    /// <summary>
    /// The local protection states supported by the device.
    /// </summary>
    IReadOnlySet<LocalProtectionState> SupportedLocalStates,

    /// <summary>
    /// The RF protection states supported by the device.
    /// </summary>
    IReadOnlySet<RfProtectionState> SupportedRfStates);

public sealed partial class ProtectionCommandClass
{
    /// <summary>
    /// Gets the protection capabilities of the device.
    /// </summary>
    public ProtectionSupportedReport? SupportedReport { get; private set; }

    /// <summary>
    /// Request the protection capabilities of a device.
    /// </summary>
    public async Task<ProtectionSupportedReport> GetSupportedAsync(CancellationToken cancellationToken)
    {
        ProtectionSupportedGetCommand command = ProtectionSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ProtectionSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        ProtectionSupportedReport report = ProtectionSupportedReportCommand.Parse(reportFrame, Logger);
        SupportedReport = report;
        return report;
    }

    internal readonly struct ProtectionSupportedGetCommand : ICommand
    {
        public ProtectionSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static ProtectionSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ProtectionSupportedGetCommand(frame);
        }
    }

    internal readonly struct ProtectionSupportedReportCommand : ICommand
    {
        public ProtectionSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static ProtectionSupportedReportCommand Create(
            bool supportsExclusiveControl,
            bool supportsTimeout,
            IReadOnlySet<LocalProtectionState> supportedLocalStates,
            IReadOnlySet<RfProtectionState> supportedRfStates)
        {
            Span<byte> commandParameters = stackalloc byte[5];

            byte flags = 0;
            if (supportsExclusiveControl)
            {
                flags |= 0b0000_0010;
            }

            if (supportsTimeout)
            {
                flags |= 0b0000_0001;
            }

            commandParameters[0] = flags;

            foreach (LocalProtectionState state in supportedLocalStates)
            {
                int bitIndex = (byte)state;
                commandParameters[1 + (bitIndex / 8)] |= (byte)(1 << (bitIndex % 8));
            }

            foreach (RfProtectionState state in supportedRfStates)
            {
                int bitIndex = (byte)state;
                commandParameters[3 + (bitIndex / 8)] |= (byte)(1 << (bitIndex % 8));
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionSupportedReportCommand(frame);
        }

        public static ProtectionSupportedReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 5)
            {
                logger.LogWarning(
                    "Protection Supported Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Protection Supported Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            bool supportsExclusiveControl = (span[0] & 0b0000_0010) != 0;
            bool supportsTimeout = (span[0] & 0b0000_0001) != 0;

            HashSet<LocalProtectionState> supportedLocalStates =
                BitMaskHelper.ParseBitMask<LocalProtectionState>(span.Slice(1, 2));
            HashSet<RfProtectionState> supportedRfStates =
                BitMaskHelper.ParseBitMask<RfProtectionState>(span.Slice(3, 2));

            return new ProtectionSupportedReport(
                supportsExclusiveControl,
                supportsTimeout,
                supportedLocalStates,
                supportedRfStates);
        }
    }
}
