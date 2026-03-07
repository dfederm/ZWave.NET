using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a parsed Supervision Get command.
/// </summary>
public readonly record struct SupervisionGet(
    /// <summary>
    /// Whether the sender requests future status updates (spec Table 4.33).
    /// When true, the receiver should send additional Supervision Reports as status changes.
    /// </summary>
    bool StatusUpdates,

    /// <summary>
    /// The session identifier (0-63) used to correlate Get/Report pairs.
    /// </summary>
    byte SessionId,

    /// <summary>
    /// The encapsulated command class frame.
    /// </summary>
    CommandClassFrame EncapsulatedFrame);

/// <summary>
/// Represents a parsed Supervision Report command.
/// </summary>
public readonly record struct SupervisionReport(
    /// <summary>
    /// Whether more Supervision Reports follow for this session (spec Table 4.34).
    /// </summary>
    bool MoreStatusUpdates,

    /// <summary>
    /// Whether the receiving node should initiate a Wake Up Period (v2, spec §4.2.9.2).
    /// </summary>
    bool WakeUpRequest,

    /// <summary>
    /// The session identifier matching the Supervision Get that initiated this session.
    /// </summary>
    byte SessionId,

    /// <summary>
    /// The current status of the command process.
    /// </summary>
    SupervisionStatus Status,

    /// <summary>
    /// The time needed to complete the current operation.
    /// </summary>
    DurationReport Duration);

public sealed partial class SupervisionCommandClass
{
    /// <summary>
    /// Event raised when a Supervision Report is received.
    /// </summary>
    public event Action<SupervisionReport>? OnSupervisionReportReceived;

    /// <summary>
    /// Creates a Supervision Get frame wrapping the specified command.
    /// </summary>
    /// <param name="statusUpdates">Whether to request future status updates.</param>
    /// <param name="sessionId">The session identifier (0-63).</param>
    /// <param name="encapsulatedFrame">The command to encapsulate.</param>
    public static CommandClassFrame CreateGet(bool statusUpdates, byte sessionId, CommandClassFrame encapsulatedFrame)
        => SupervisionGetCommand.Create(statusUpdates, sessionId, encapsulatedFrame).Frame;

    /// <summary>
    /// Parses a Supervision Get frame.
    /// </summary>
    public static SupervisionGet ParseGet(CommandClassFrame frame, ILogger logger)
        => SupervisionGetCommand.Parse(frame, logger);

    /// <summary>
    /// Creates a Supervision Report frame.
    /// </summary>
    /// <param name="moreStatusUpdates">Whether more reports follow for this session.</param>
    /// <param name="wakeUpRequest">Whether to request a Wake Up Period (v2).</param>
    /// <param name="sessionId">The session identifier matching the initiating Get.</param>
    /// <param name="status">The current status of the command process.</param>
    /// <param name="duration">The time needed to complete the operation.</param>
    public static CommandClassFrame CreateReport(
        bool moreStatusUpdates,
        bool wakeUpRequest,
        byte sessionId,
        SupervisionStatus status,
        DurationReport duration)
        => SupervisionReportCommand.Create(moreStatusUpdates, wakeUpRequest, sessionId, status, duration).Frame;

    /// <summary>
    /// Parses a Supervision Report frame.
    /// </summary>
    public static SupervisionReport ParseReport(CommandClassFrame frame, ILogger logger)
        => SupervisionReportCommand.Parse(frame, logger);

    /// <summary>
    /// Supervision Get Command (spec §4.2.8.3).
    /// </summary>
    /// <remarks>
    /// Wire format:
    ///   byte 0: CC = 0x6C
    ///   byte 1: Command = 0x01 (SUPERVISION_GET)
    ///   byte 2: StatusUpdates(bit7) | Reserved(bit6) | SessionID(bits5..0)
    ///   byte 3: Encapsulated Command Length
    ///   byte 4..N: Encapsulated Command
    /// </remarks>
    internal readonly struct SupervisionGetCommand : ICommand
    {
        public SupervisionGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Supervision;

        public static byte CommandId => (byte)SupervisionCommand.Get;

        public CommandClassFrame Frame { get; }

        public static SupervisionGetCommand Create(
            bool statusUpdates,
            byte sessionId,
            CommandClassFrame encapsulatedFrame)
        {
            if (sessionId > 63)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "Session ID must be between 0 and 63.");
            }

            ReadOnlySpan<byte> encapsulatedData = encapsulatedFrame.Data.Span;
            if (encapsulatedData.Length == 0 || encapsulatedData.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(encapsulatedFrame), encapsulatedData.Length, "Encapsulated command length must be in the range 1..255 bytes.");
            }

            byte[] parameters = new byte[2 + encapsulatedData.Length];
            parameters[0] = (byte)((statusUpdates ? 0b1000_0000 : 0) | (sessionId & 0b0011_1111));
            parameters[1] = (byte)encapsulatedData.Length;
            encapsulatedData.CopyTo(parameters.AsSpan(2));

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new SupervisionGetCommand(frame);
        }

        public static SupervisionGet Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: 1 byte (flags+sessionID) + 1 byte (length) + 1 byte (min encapsulated)
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Supervision Get frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Supervision Get frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            bool statusUpdates = (parameters[0] & 0b1000_0000) != 0;
            byte sessionId = (byte)(parameters[0] & 0b0011_1111);
            byte encapsulatedLength = parameters[1];

            if (frame.CommandParameters.Length < 2 + encapsulatedLength || encapsulatedLength < 1)
            {
                logger.LogWarning(
                    "Supervision Get frame has invalid encapsulated command length ({EncapLength} bytes, available {Available})",
                    encapsulatedLength,
                    frame.CommandParameters.Length - 2);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Supervision Get frame has invalid encapsulated command length");
            }

            CommandClassFrame encapsulatedFrame = new CommandClassFrame(frame.CommandParameters.Slice(2, encapsulatedLength));

            return new SupervisionGet(statusUpdates, sessionId, encapsulatedFrame);
        }
    }

    /// <summary>
    /// Supervision Report Command (spec §4.2.8.4, §4.2.9.2).
    /// </summary>
    /// <remarks>
    /// Wire format:
    ///   byte 0: CC = 0x6C
    ///   byte 1: Command = 0x02 (SUPERVISION_REPORT)
    ///   byte 2: MoreStatusUpdates(bit7) | WakeUpRequest(bit6, v2) | SessionID(bits5..0)
    ///   byte 3: Status
    ///   byte 4: Duration
    /// </remarks>
    internal readonly struct SupervisionReportCommand : ICommand
    {
        public SupervisionReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Supervision;

        public static byte CommandId => (byte)SupervisionCommand.Report;

        public CommandClassFrame Frame { get; }

        public static SupervisionReportCommand Create(
            bool moreStatusUpdates,
            bool wakeUpRequest,
            byte sessionId,
            SupervisionStatus status,
            DurationReport duration)
        {
            if (sessionId > 63)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "Session ID must be between 0 and 63.");
            }

            Span<byte> parameters =
            [
                (byte)(
                    (moreStatusUpdates ? 0b1000_0000 : 0)
                    | (wakeUpRequest ? 0b0100_0000 : 0)
                    | (sessionId & 0b0011_1111)),
                (byte)status,
                duration.Value,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new SupervisionReportCommand(frame);
        }

        public static SupervisionReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Supervision Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Supervision Report frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            bool moreStatusUpdates = (parameters[0] & 0b1000_0000) != 0;
            bool wakeUpRequest = (parameters[0] & 0b0100_0000) != 0;
            byte sessionId = (byte)(parameters[0] & 0b0011_1111);
            SupervisionStatus status = (SupervisionStatus)parameters[1];
            DurationReport duration = parameters[2];

            return new SupervisionReport(moreStatusUpdates, wakeUpRequest, sessionId, status, duration);
        }
    }
}
