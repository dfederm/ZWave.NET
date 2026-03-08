using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Protection Report received from a device.
/// </summary>
public readonly record struct ProtectionReport(
    /// <summary>
    /// The local (physical) protection state of the device.
    /// </summary>
    LocalProtectionState LocalProtection,

    /// <summary>
    /// The RF (wireless) protection state of the device.
    /// This is <see langword="null"/> for version 1 devices.
    /// </summary>
    RfProtectionState? RfProtection);

public sealed partial class ProtectionCommandClass
{
    /// <summary>
    /// Gets the last protection report received from the device.
    /// </summary>
    public ProtectionReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Protection Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<ProtectionReport>? OnProtectionReportReceived;

    /// <summary>
    /// Request the protection state of a device.
    /// </summary>
    public async Task<ProtectionReport> GetAsync(CancellationToken cancellationToken)
    {
        ProtectionGetCommand command = ProtectionGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ProtectionReportCommand>(cancellationToken).ConfigureAwait(false);
        ProtectionReport report = ProtectionReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnProtectionReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set the protection state of a device.
    /// </summary>
    /// <param name="localState">The local (physical) protection state to set.</param>
    /// <param name="rfState">
    /// The RF (wireless) protection state to set.
    /// Ignored for version 1 devices.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(
        LocalProtectionState localState,
        RfProtectionState rfState,
        CancellationToken cancellationToken)
    {
        var command = ProtectionSetCommand.Create(EffectiveVersion, localState, rfState);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ProtectionSetCommand : ICommand
    {
        public ProtectionSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.Set;

        public CommandClassFrame Frame { get; }

        public static ProtectionSetCommand Create(
            byte version,
            LocalProtectionState localState,
            RfProtectionState rfState)
        {
            ReadOnlySpan<byte> commandParameters = version >= 2
                ? [(byte)localState, (byte)rfState]
                : [(byte)localState];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionSetCommand(frame);
        }
    }

    internal readonly struct ProtectionGetCommand : ICommand
    {
        public ProtectionGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ProtectionGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ProtectionGetCommand(frame);
        }
    }

    internal readonly struct ProtectionReportCommand : ICommand
    {
        public ProtectionReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Protection;

        public static byte CommandId => (byte)ProtectionCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ProtectionReportCommand Create(
            byte version,
            LocalProtectionState localState,
            RfProtectionState rfState)
        {
            ReadOnlySpan<byte> commandParameters = version >= 2
                ? [(byte)localState, (byte)rfState]
                : [(byte)localState];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ProtectionReportCommand(frame);
        }

        public static ProtectionReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Protection Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Protection Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            LocalProtectionState localState = (LocalProtectionState)(span[0] & 0x0F);
            RfProtectionState? rfState = span.Length >= 2
                ? (RfProtectionState)(span[1] & 0x0F)
                : null;

            return new ProtectionReport(localState, rfState);
        }
    }
}
