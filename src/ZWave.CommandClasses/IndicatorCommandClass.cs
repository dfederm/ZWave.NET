using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies an indicator resource on a supporting node.
/// </summary>
/// <remarks>
/// Values are defined in the "Indicator Command Class, list of assigned indicators and Property IDs" document.
/// </remarks>
public enum IndicatorId : byte
{
    /// <summary>
    /// Indicates that the alarm is armed.
    /// </summary>
    Armed = 0x01,

    /// <summary>
    /// Indicates that the alarm is disarmed.
    /// </summary>
    NotArmed = 0x02,

    /// <summary>
    /// Indicates that the device is ready.
    /// </summary>
    Ready = 0x03,

    /// <summary>
    /// Indicates a general error.
    /// </summary>
    Fault = 0x04,

    /// <summary>
    /// Indicates that the device is temporarily busy.
    /// </summary>
    Busy = 0x05,

    /// <summary>
    /// Signals that the device is waiting for an ID.
    /// </summary>
    EnterId = 0x06,

    /// <summary>
    /// Signals that the device is waiting for a PIN code.
    /// </summary>
    EnterPin = 0x07,

    /// <summary>
    /// Indicates that the entered code is accepted.
    /// </summary>
    CodeAccepted = 0x08,

    /// <summary>
    /// Indicates that the entered code is not accepted.
    /// </summary>
    CodeNotAccepted = 0x09,

    /// <summary>
    /// Indicates that the alarm is armed in stay mode.
    /// </summary>
    ArmedStay = 0x0A,

    /// <summary>
    /// Indicates that the alarm is armed in away mode.
    /// </summary>
    ArmedAway = 0x0B,

    /// <summary>
    /// Indicates that the alarm is triggered with no specific reason.
    /// </summary>
    Alarming = 0x0C,

    /// <summary>
    /// Indicates that the alarm is triggered due to a burglar event.
    /// </summary>
    AlarmingBurglar = 0x0D,

    /// <summary>
    /// Indicates that the alarm is triggered due to a fire alarm event.
    /// </summary>
    AlarmingSmokeFire = 0x0E,

    /// <summary>
    /// Indicates that the alarm is triggered due to a carbon monoxide event.
    /// </summary>
    AlarmingCarbonMonoxide = 0x0F,

    /// <summary>
    /// Indicates that the device expects a bypass challenge code.
    /// </summary>
    BypassChallenge = 0x10,

    /// <summary>
    /// Indicates that the alarm is about to be activated unless disarmed.
    /// </summary>
    EntryDelay = 0x11,

    /// <summary>
    /// Indicates that the alarm will be active after the exit delay.
    /// </summary>
    ExitDelay = 0x12,

    /// <summary>
    /// Indicates that the alarm is triggered due to a medical emergency.
    /// </summary>
    AlarmingMedical = 0x13,

    /// <summary>
    /// Indicates that the alarm is triggered due to a freeze warning.
    /// </summary>
    AlarmingFreezeWarning = 0x14,

    /// <summary>
    /// Indicates that the alarm is triggered due to a water leak.
    /// </summary>
    AlarmingWaterLeak = 0x15,

    /// <summary>
    /// Indicates that the alarm is triggered due to a panic alarm.
    /// </summary>
    AlarmingPanic = 0x16,

    /// <summary>
    /// Indicates that alarm zone 1 is armed.
    /// </summary>
    Zone1Armed = 0x20,

    /// <summary>
    /// Indicates that alarm zone 2 is armed.
    /// </summary>
    Zone2Armed = 0x21,

    /// <summary>
    /// Indicates that alarm zone 3 is armed.
    /// </summary>
    Zone3Armed = 0x22,

    /// <summary>
    /// Indicates that alarm zone 4 is armed.
    /// </summary>
    Zone4Armed = 0x23,

    /// <summary>
    /// Indicates that alarm zone 5 is armed.
    /// </summary>
    Zone5Armed = 0x24,

    /// <summary>
    /// Indicates that alarm zone 6 is armed.
    /// </summary>
    Zone6Armed = 0x25,

    /// <summary>
    /// Indicates that alarm zone 7 is armed.
    /// </summary>
    Zone7Armed = 0x26,

    /// <summary>
    /// Indicates that alarm zone 8 is armed.
    /// </summary>
    Zone8Armed = 0x27,

    /// <summary>
    /// LCD backlight indicator.
    /// </summary>
    LcdBacklight = 0x30,

    /// <summary>
    /// Button backlight for letters.
    /// </summary>
    ButtonBacklightLetters = 0x40,

    /// <summary>
    /// Button backlight for digits.
    /// </summary>
    ButtonBacklightDigits = 0x41,

    /// <summary>
    /// Button backlight for command buttons.
    /// </summary>
    ButtonBacklightCommand = 0x42,

    /// <summary>
    /// Indication for button 1.
    /// </summary>
    Button1Indication = 0x43,

    /// <summary>
    /// Indication for button 2.
    /// </summary>
    Button2Indication = 0x44,

    /// <summary>
    /// Indication for button 3.
    /// </summary>
    Button3Indication = 0x45,

    /// <summary>
    /// Indication for button 4.
    /// </summary>
    Button4Indication = 0x46,

    /// <summary>
    /// Indication for button 5.
    /// </summary>
    Button5Indication = 0x47,

    /// <summary>
    /// Indication for button 6.
    /// </summary>
    Button6Indication = 0x48,

    /// <summary>
    /// Indication for button 7.
    /// </summary>
    Button7Indication = 0x49,

    /// <summary>
    /// Indication for button 8.
    /// </summary>
    Button8Indication = 0x4A,

    /// <summary>
    /// Indication for button 9.
    /// </summary>
    Button9Indication = 0x4B,

    /// <summary>
    /// Indication for button 10.
    /// </summary>
    Button10Indication = 0x4C,

    /// <summary>
    /// Indication for button 11.
    /// </summary>
    Button11Indication = 0x4D,

    /// <summary>
    /// Indication for button 12.
    /// </summary>
    Button12Indication = 0x4E,

    /// <summary>
    /// Used to identify the node (e.g. make an LED blink).
    /// </summary>
    NodeIdentify = 0x50,

    /// <summary>
    /// Generic event sound notification 1.
    /// </summary>
    GenericEventSoundNotification1 = 0x60,

    /// <summary>
    /// Generic event sound notification 2.
    /// </summary>
    GenericEventSoundNotification2 = 0x61,

    /// <summary>
    /// Generic event sound notification 3.
    /// </summary>
    GenericEventSoundNotification3 = 0x62,

    /// <summary>
    /// Generic event sound notification 4.
    /// </summary>
    GenericEventSoundNotification4 = 0x63,

    /// <summary>
    /// Generic event sound notification 5.
    /// </summary>
    GenericEventSoundNotification5 = 0x64,

    /// <summary>
    /// Generic event sound notification 6.
    /// </summary>
    GenericEventSoundNotification6 = 0x65,

    /// <summary>
    /// Generic event sound notification 7.
    /// </summary>
    GenericEventSoundNotification7 = 0x66,

    /// <summary>
    /// Generic event sound notification 8.
    /// </summary>
    GenericEventSoundNotification8 = 0x67,

    /// <summary>
    /// Generic event sound notification 9.
    /// </summary>
    GenericEventSoundNotification9 = 0x68,

    /// <summary>
    /// Generic event sound notification 10.
    /// </summary>
    GenericEventSoundNotification10 = 0x69,

    /// <summary>
    /// Generic event sound notification 11.
    /// </summary>
    GenericEventSoundNotification11 = 0x6A,

    /// <summary>
    /// Generic event sound notification 12.
    /// </summary>
    GenericEventSoundNotification12 = 0x6B,

    /// <summary>
    /// Generic event sound notification 13.
    /// </summary>
    GenericEventSoundNotification13 = 0x6C,

    /// <summary>
    /// Generic event sound notification 14.
    /// </summary>
    GenericEventSoundNotification14 = 0x6D,

    /// <summary>
    /// Generic event sound notification 15.
    /// </summary>
    GenericEventSoundNotification15 = 0x6E,

    /// <summary>
    /// Generic event sound notification 16.
    /// </summary>
    GenericEventSoundNotification16 = 0x6F,

    /// <summary>
    /// Generic event sound notification 17.
    /// </summary>
    GenericEventSoundNotification17 = 0x70,

    /// <summary>
    /// Generic event sound notification 18.
    /// </summary>
    GenericEventSoundNotification18 = 0x71,

    /// <summary>
    /// Generic event sound notification 19.
    /// </summary>
    GenericEventSoundNotification19 = 0x72,

    /// <summary>
    /// Generic event sound notification 20.
    /// </summary>
    GenericEventSoundNotification20 = 0x73,

    /// <summary>
    /// Generic event sound notification 21.
    /// </summary>
    GenericEventSoundNotification21 = 0x74,

    /// <summary>
    /// Generic event sound notification 22.
    /// </summary>
    GenericEventSoundNotification22 = 0x75,

    /// <summary>
    /// Generic event sound notification 23.
    /// </summary>
    GenericEventSoundNotification23 = 0x76,

    /// <summary>
    /// Generic event sound notification 24.
    /// </summary>
    GenericEventSoundNotification24 = 0x77,

    /// <summary>
    /// Generic event sound notification 25.
    /// </summary>
    GenericEventSoundNotification25 = 0x78,

    /// <summary>
    /// Generic event sound notification 26.
    /// </summary>
    GenericEventSoundNotification26 = 0x79,

    /// <summary>
    /// Generic event sound notification 27.
    /// </summary>
    GenericEventSoundNotification27 = 0x7A,

    /// <summary>
    /// Generic event sound notification 28.
    /// </summary>
    GenericEventSoundNotification28 = 0x7B,

    /// <summary>
    /// Generic event sound notification 29.
    /// </summary>
    GenericEventSoundNotification29 = 0x7C,

    /// <summary>
    /// Generic event sound notification 30.
    /// </summary>
    GenericEventSoundNotification30 = 0x7D,

    /// <summary>
    /// Generic event sound notification 31.
    /// </summary>
    GenericEventSoundNotification31 = 0x7E,

    /// <summary>
    /// Generic event sound notification 32.
    /// </summary>
    GenericEventSoundNotification32 = 0x7F,

    /// <summary>
    /// Buzzer indicator.
    /// </summary>
    Buzzer = 0xF0,
}

/// <summary>
/// Identifies a property of an indicator resource.
/// </summary>
/// <remarks>
/// Values are defined in the "Indicator Command Class, list of assigned indicators and Property IDs" document.
/// </remarks>
public enum IndicatorPropertyId : byte
{
    /// <summary>
    /// Multilevel indicator (light or sound level).
    /// Values: 0x00=OFF, 0x01-0x63=lowest to 100%, 0xFF=restore most recent level.
    /// </summary>
    Multilevel = 0x01,

    /// <summary>
    /// Binary indicator (on or off).
    /// Values: 0x00=OFF, 0x01-0x63=ON, 0xFF=ON.
    /// </summary>
    Binary = 0x02,

    /// <summary>
    /// On/Off period duration in tenths of a second (0x00-0xFF = 0-25.5 seconds).
    /// If specified, the <see cref="OnOffCycles"/> property MUST also be specified.
    /// </summary>
    OnOffPeriod = 0x03,

    /// <summary>
    /// Number of On/Off periods to run (0x00-0xFE = 0-254, 0xFF = run until stopped).
    /// If specified, the <see cref="OnOffPeriod"/> property MUST also be specified.
    /// </summary>
    OnOffCycles = 0x04,

    /// <summary>
    /// On time within an On/Off period in tenths of a second, allowing asymmetric periods.
    /// 0x00 = symmetric (On time equals Off time), 0x01-0xFF = 0.1-25.5 seconds.
    /// </summary>
    OnTimeWithinOnOffPeriod = 0x05,

    /// <summary>
    /// Timeout in minutes (0x00-0xFF = 0-255 minutes).
    /// </summary>
    TimeoutMinutes = 0x06,

    /// <summary>
    /// Timeout in seconds (0x00-0x3B = 0-59 seconds; 0x3C-0xFF reserved).
    /// </summary>
    TimeoutSeconds = 0x07,

    /// <summary>
    /// Timeout in hundredths of a second (0x00-0x63 = 0.00-0.99 seconds; 0x64-0xFF reserved).
    /// </summary>
    TimeoutHundredths = 0x08,

    /// <summary>
    /// Sound volume level (0x00=mute, 0x01-0x64=1-100%, 0xFF=restore most recent level).
    /// This property MUST NOT switch on the indication.
    /// </summary>
    SoundLevel = 0x09,

    /// <summary>
    /// Timeout in hours (0x00-0xFF = 0-255 hours).
    /// </summary>
    TimeoutHours = 0x0A,

    /// <summary>
    /// Advertise-only property indicating the indicator can continue working during sleep.
    /// MUST NOT be used in a controlling command.
    /// </summary>
    LowPower = 0x10,
}

/// <summary>
/// Defines the commands for the Indicator Command Class.
/// </summary>
public enum IndicatorCommand : byte
{
    /// <summary>
    /// Set one or more indicator resources on a supporting node.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the state of an indicator resource.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report the state of an indicator resource.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the supported properties of an indicator resource.
    /// </summary>
    SupportedGet = 0x04,

    /// <summary>
    /// Report the supported properties of an indicator resource.
    /// </summary>
    SupportedReport = 0x05,
}

/// <summary>
/// Represents a single indicator object in a Set or Report command (version 2+).
/// </summary>
public readonly record struct IndicatorObject(
    /// <summary>
    /// The indicator resource identifier.
    /// </summary>
    IndicatorId IndicatorId,

    /// <summary>
    /// The property of the indicator resource.
    /// </summary>
    IndicatorPropertyId PropertyId,

    /// <summary>
    /// The value to assign to (or reported for) the property.
    /// </summary>
    byte Value);

/// <summary>
/// Represents an Indicator Report received from a device.
/// </summary>
/// <remarks>
/// For version 1 devices, only <see cref="Indicator0Value"/> is populated and
/// <see cref="Objects"/> is empty.
/// For version 2+ devices, <see cref="Indicator0Value"/> provides backward compatibility
/// and <see cref="Objects"/> contains the detailed indicator state.
/// </remarks>
public readonly record struct IndicatorReport(
    /// <summary>
    /// The backward-compatible indicator value.
    /// For version 1 devices, this is the sole indicator state (0x00=off, 0x01-0x63=on, 0xFF=on).
    /// For version 2+ devices, a controlling node SHOULD ignore this if <see cref="Objects"/> is non-empty.
    /// </summary>
    byte Indicator0Value,

    /// <summary>
    /// The indicator objects reported for the queried indicator (version 2+).
    /// All objects carry the same <see cref="IndicatorObject.IndicatorId"/>.
    /// Empty for version 1 devices.
    /// </summary>
    IReadOnlyList<IndicatorObject> Objects);

/// <summary>
/// Controls indicator resources (LEDs, LCDs, buzzers) on a supporting node.
/// </summary>
/// <remarks>
/// <para>
/// Version 1 provides a single unspecified indicator that can be turned on or off.
/// Version 2 introduces multiple named indicator resources with typed properties.
/// Version 3 adds new indicator IDs and property IDs, including the Node Identify indicator (0x50).
/// </para>
/// <para>
/// The Identify feature (Indicator 0x50) is mandatory for all Z-Wave Plus v2 nodes and allows
/// a controller to make a device blink/beep so the user can physically locate it.
/// </para>
/// </remarks>
[CommandClass(CommandClassId.Indicator)]
public sealed partial class IndicatorCommandClass : CommandClass<IndicatorCommand>
{
    internal IndicatorCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(IndicatorCommand command)
        => command switch
        {
            IndicatorCommand.Set => true,
            IndicatorCommand.Get => true,
            IndicatorCommand.SupportedGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    /// <summary>
    /// Interviews the device to discover its indicator capabilities and current state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per spec CL:0087.01.21.01.1, for version 2+, the interview walks the
    /// next-indicator-ID chain starting from indicator ID 0x00,
    /// then queries the current state of each supported indicator.
    /// </para>
    /// <para>
    /// For version 1, the interview simply queries the single indicator state.
    /// </para>
    /// </remarks>
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        if (IsCommandSupported(IndicatorCommand.SupportedGet).GetValueOrDefault())
        {
            // Walk the supported indicator chain per spec Figure 6.38.
            IndicatorId nextIndicatorId = 0;
            List<IndicatorId> supportedIndicators = [];
            do
            {
                IndicatorSupportedGetCommand command = IndicatorSupportedGetCommand.Create(nextIndicatorId);
                await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
                CommandClassFrame reportFrame = await AwaitNextReportAsync<IndicatorSupportedReportCommand>(
                    predicate: frame => frame.CommandParameters.Length >= 1
                        && (IndicatorId)frame.CommandParameters.Span[0] == nextIndicatorId,
                    cancellationToken).ConfigureAwait(false);
                (IndicatorId nextId, IReadOnlySet<IndicatorPropertyId> propertyIds) =
                    IndicatorSupportedReportCommand.Parse(reportFrame, Logger);

                if (propertyIds.Count > 0)
                {
                    _supportedIndicators[nextIndicatorId] = propertyIds;
                    supportedIndicators.Add(nextIndicatorId);
                }

                nextIndicatorId = nextId;
            }
            while (nextIndicatorId != 0);

            // Get the current state of each supported indicator.
            foreach (IndicatorId indicatorId in supportedIndicators)
            {
                _ = await GetAsync(indicatorId, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            // Version 1: simple get.
            _ = await GetAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((IndicatorCommand)frame.CommandId)
        {
            case IndicatorCommand.Report:
            {
                IndicatorReport report = IndicatorReportCommand.Parse(frame, Logger);
                ApplyReport(report);
                break;
            }
        }
    }
}
