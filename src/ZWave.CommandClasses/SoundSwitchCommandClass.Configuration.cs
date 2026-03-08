using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the configuration of a Sound Switch device (volume and default tone).
/// </summary>
public readonly record struct SoundSwitchConfigurationReport(
    /// <summary>
    /// The current volume setting (0-100%).
    /// </summary>
    byte Volume,

    /// <summary>
    /// The currently configured default tone identifier.
    /// </summary>
    byte DefaultToneIdentifier);

public sealed partial class SoundSwitchCommandClass
{
    /// <summary>
    /// Gets the last configuration report received from the device.
    /// </summary>
    public SoundSwitchConfigurationReport? LastConfigurationReport { get; private set; }

    /// <summary>
    /// Event raised when a Configuration Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<SoundSwitchConfigurationReport>? OnConfigurationReportReceived;

    /// <summary>
    /// Request the current configuration for playing tones at the device.
    /// </summary>
    public async Task<SoundSwitchConfigurationReport> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        SoundSwitchConfigurationGetCommand command = SoundSwitchConfigurationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<SoundSwitchConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        SoundSwitchConfigurationReport report = SoundSwitchConfigurationReportCommand.Parse(reportFrame, Logger);
        LastConfigurationReport = report;
        OnConfigurationReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Set only the volume at the device without changing the default tone.
    /// </summary>
    /// <param name="volume">
    /// The volume level: 0 = mute, 1-100 = volume percentage, 255 = restore most recent non-zero volume.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetVolumeAsync(byte volume, CancellationToken cancellationToken)
    {
        await SetConfigurationAsync(volume, defaultToneIdentifier: 0x00, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Set the volume and default tone configuration at the device.
    /// </summary>
    /// <param name="volume">
    /// The volume level: 0 = mute, 1-100 = volume percentage, 255 = restore most recent non-zero volume.
    /// </param>
    /// <param name="defaultToneIdentifier">
    /// The default tone identifier. 0 = do not change the default tone (configure volume only).
    /// Values 1..N set the specified tone as default.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetConfigurationAsync(byte volume, byte defaultToneIdentifier, CancellationToken cancellationToken)
    {
        var command = SoundSwitchConfigurationSetCommand.Create(volume, defaultToneIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct SoundSwitchConfigurationSetCommand : ICommand
    {
        public SoundSwitchConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ConfigurationSet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchConfigurationSetCommand Create(byte volume, byte defaultToneIdentifier)
        {
            ReadOnlySpan<byte> commandParameters = [volume, defaultToneIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SoundSwitchConfigurationSetCommand(frame);
        }
    }

    internal readonly struct SoundSwitchConfigurationGetCommand : ICommand
    {
        public SoundSwitchConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ConfigurationGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchConfigurationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new SoundSwitchConfigurationGetCommand(frame);
        }
    }

    internal readonly struct SoundSwitchConfigurationReportCommand : ICommand
    {
        public SoundSwitchConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ConfigurationReport;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchConfigurationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Sound Switch Configuration Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Sound Switch Configuration Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            byte volume = span[0];
            byte defaultToneIdentifier = span[1];

            return new SoundSwitchConfigurationReport(volume, defaultToneIdentifier);
        }
    }
}
