using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the current configuration of an Entry Control device.
/// </summary>
public readonly record struct EntryControlConfigurationReport(
    /// <summary>
    /// Gets the number of key entries cached before sending a notification.
    /// </summary>
    byte KeyCacheSize,

    /// <summary>
    /// Gets the timeout in seconds between key entries before sending a notification.
    /// </summary>
    byte KeyCacheTimeout);

public sealed partial class EntryControlCommandClass
{
    /// <summary>
    /// Gets the last configuration reported by the device.
    /// </summary>
    public EntryControlConfigurationReport? LastConfiguration { get; private set; }

    /// <summary>
    /// Request the current configuration from the device.
    /// </summary>
    public async Task<EntryControlConfigurationReport> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        EntryControlConfigurationGetCommand command = EntryControlConfigurationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<EntryControlConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        EntryControlConfigurationReport report = EntryControlConfigurationReportCommand.Parse(reportFrame, Logger);
        LastConfiguration = report;
        return report;
    }

    /// <summary>
    /// Set the key cache size and timeout on the device.
    /// </summary>
    /// <param name="keyCacheSize">The number of key entries to cache before sending a notification. Must be in the range 1-32.</param>
    /// <param name="keyCacheTimeout">The timeout in seconds between key entries. Should be in the range 1-10.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SetConfigurationAsync(byte keyCacheSize, byte keyCacheTimeout, CancellationToken cancellationToken)
    {
        EntryControlConfigurationSetCommand command = EntryControlConfigurationSetCommand.Create(keyCacheSize, keyCacheTimeout);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct EntryControlConfigurationGetCommand : ICommand
    {
        public EntryControlConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.ConfigurationGet;

        public CommandClassFrame Frame { get; }

        public static EntryControlConfigurationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new EntryControlConfigurationGetCommand(frame);
        }
    }

    internal readonly struct EntryControlConfigurationSetCommand : ICommand
    {
        public EntryControlConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.ConfigurationSet;

        public CommandClassFrame Frame { get; }

        public static EntryControlConfigurationSetCommand Create(byte keyCacheSize, byte keyCacheTimeout)
        {
            ReadOnlySpan<byte> commandParameters = [keyCacheSize, keyCacheTimeout];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new EntryControlConfigurationSetCommand(frame);
        }
    }

    internal readonly struct EntryControlConfigurationReportCommand : ICommand
    {
        public EntryControlConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.EntryControl;

        public static byte CommandId => (byte)EntryControlCommand.ConfigurationReport;

        public CommandClassFrame Frame { get; }

        public static EntryControlConfigurationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: KeyCacheSize(1) + KeyCacheTimeout(1) = 2 bytes
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning(
                    "Entry Control Configuration Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                throw new ZWaveException(
                    ZWaveErrorCode.InvalidPayload,
                    "Entry Control Configuration Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte keyCacheSize = span[0];
            byte keyCacheTimeout = span[1];

            return new EntryControlConfigurationReport(keyCacheSize, keyCacheTimeout);
        }
    }
}
