using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Defines the commands for the Sound Switch Command Class.
/// </summary>
public enum SoundSwitchCommand : byte
{
    /// <summary>
    /// Request the number of tones supported by the device.
    /// </summary>
    TonesNumberGet = 0x01,

    /// <summary>
    /// Report the number of tones supported by the device.
    /// </summary>
    TonesNumberReport = 0x02,

    /// <summary>
    /// Request the information associated with a specific tone.
    /// </summary>
    ToneInfoGet = 0x03,

    /// <summary>
    /// Report the information associated with a specific tone.
    /// </summary>
    ToneInfoReport = 0x04,

    /// <summary>
    /// Set the volume and default tone configuration.
    /// </summary>
    ConfigurationSet = 0x05,

    /// <summary>
    /// Request the current configuration for playing tones.
    /// </summary>
    ConfigurationGet = 0x06,

    /// <summary>
    /// Report the current configuration for playing tones.
    /// </summary>
    ConfigurationReport = 0x07,

    /// <summary>
    /// Instruct the device to play or stop playing a tone.
    /// </summary>
    TonePlaySet = 0x08,

    /// <summary>
    /// Request the current tone being played by the device.
    /// </summary>
    TonePlayGet = 0x09,

    /// <summary>
    /// Report the current tone being played by the device.
    /// </summary>
    TonePlayReport = 0x0A,
}

/// <summary>
/// Controls devices with speaker or sound notification capability such as doorbells, alarm clocks, sirens,
/// or any device issuing sound notifications.
/// </summary>
[CommandClass(CommandClassId.SoundSwitch)]
public sealed partial class SoundSwitchCommandClass : CommandClass<SoundSwitchCommand>
{
    internal SoundSwitchCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(SoundSwitchCommand command)
        => command switch
        {
            SoundSwitchCommand.TonesNumberGet => true,
            SoundSwitchCommand.ToneInfoGet => true,
            SoundSwitchCommand.ConfigurationSet => true,
            SoundSwitchCommand.ConfigurationGet => true,
            SoundSwitchCommand.TonePlaySet => true,
            SoundSwitchCommand.TonePlayGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Step 1: Get number of supported tones
        byte tonesCount = await GetTonesNumberAsync(cancellationToken).ConfigureAwait(false);

        // Step 2: Get info for each tone
        for (byte toneId = 1; toneId <= tonesCount; toneId++)
        {
            _ = await GetToneInfoAsync(toneId, cancellationToken).ConfigureAwait(false);
        }

        // Step 3: Get current configuration (volume + default tone)
        _ = await GetConfigurationAsync(cancellationToken).ConfigureAwait(false);

        // Step 4: Get current playing state
        _ = await GetTonePlayAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((SoundSwitchCommand)frame.CommandId)
        {
            case SoundSwitchCommand.ConfigurationReport:
            {
                SoundSwitchConfigurationReport report = SoundSwitchConfigurationReportCommand.Parse(frame, Logger);
                LastConfigurationReport = report;
                OnConfigurationReportReceived?.Invoke(report);
                break;
            }
            case SoundSwitchCommand.TonePlayReport:
            {
                SoundSwitchTonePlayReport report = SoundSwitchTonePlayReportCommand.Parse(frame, Logger);
                LastTonePlayReport = report;
                OnTonePlayReportReceived?.Invoke(report);
                break;
            }
        }
    }
}
