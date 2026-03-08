using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands for the Scene Actuator Configuration Command Class.
/// </summary>
public enum SceneActuatorConfigurationCommand : byte
{
    /// <summary>
    /// Associate a scene ID with actuator settings.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the settings for a given scene ID.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the settings associated with a scene ID.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents a Scene Actuator Configuration Report received from a device.
/// </summary>
/// <remarks>
/// A scene ID of 0 indicates that no scene is currently active at the sending node
/// and the Level and Dimming Duration fields should be ignored.
/// </remarks>
public readonly record struct SceneActuatorConfigurationReport(
    /// <summary>
    /// The scene ID for which settings are being advertised.
    /// Values 1–255 indicate an actual scene ID.
    /// The value 0 indicates no scene is currently active.
    /// </summary>
    byte SceneId,

    /// <summary>
    /// The actuator setting (level) associated with this scene.
    /// Corresponds to the Value field of the Basic Set Command.
    /// </summary>
    byte Level,

    /// <summary>
    /// The dimming duration for the transition to the target level.
    /// </summary>
    TimeSpan DimmingDuration);

/// <summary>
/// Implementation of the Scene Actuator Configuration Command Class (version 1).
/// </summary>
/// <remarks>
/// The Scene Actuator Configuration Command Class is used to configure scene settings
/// for a node supporting an actuator Command Class (e.g. Multilevel Switch, Binary Switch).
/// A node supporting this command class MUST support 255 Scene IDs (1 to 255).
/// </remarks>
[CommandClass(CommandClassId.SceneActuatorConfiguration)]
public sealed class SceneActuatorConfigurationCommandClass : CommandClass<SceneActuatorConfigurationCommand>
{
    internal SceneActuatorConfigurationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public SceneActuatorConfigurationReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Scene Actuator Configuration Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<SceneActuatorConfigurationReport>? OnReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(SceneActuatorConfigurationCommand command)
        => command switch
        {
            SceneActuatorConfigurationCommand.Set => true,
            SceneActuatorConfigurationCommand.Get => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override Task InterviewAsync(CancellationToken cancellationToken)
    {
        // The device supports 255 scenes; querying all of them during interview is impractical.
        // Scene configuration is queried on demand via GetAsync.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Request the settings for a given scene ID.
    /// </summary>
    /// <param name="sceneId">
    /// The scene ID to query. Values 1–255 request a specific scene.
    /// The value 0 requests the currently active scene (if any).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scene actuator configuration report.</returns>
    public async Task<SceneActuatorConfigurationReport> GetAsync(byte sceneId, CancellationToken cancellationToken)
    {
        var command = SceneActuatorConfigurationGetCommand.Create(sceneId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<SceneActuatorConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        SceneActuatorConfigurationReport report = SceneActuatorConfigurationReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Associate a scene ID with actuator settings.
    /// </summary>
    /// <param name="sceneId">The scene ID to configure. Must be in the range 1–255.</param>
    /// <param name="dimmingDuration">The dimming duration for the transition to the target level.</param>
    /// <param name="overrideLevel">
    /// If <see langword="true"/>, the <paramref name="level"/> value is used for the scene.
    /// If <see langword="false"/>, the current actuator settings are captured for the scene
    /// and the <paramref name="level"/> value is ignored.
    /// </param>
    /// <param name="level">
    /// The actuator level to associate with the scene. Only used when <paramref name="overrideLevel"/> is <see langword="true"/>.
    /// Corresponds to the Value field of the Basic Set Command.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(byte sceneId, TimeSpan dimmingDuration, bool overrideLevel, byte level, CancellationToken cancellationToken)
    {
        var command = SceneActuatorConfigurationSetCommand.Create(sceneId, dimmingDuration, overrideLevel, level);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((SceneActuatorConfigurationCommand)frame.CommandId)
        {
            case SceneActuatorConfigurationCommand.Report:
            {
                SceneActuatorConfigurationReport report = SceneActuatorConfigurationReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct SceneActuatorConfigurationSetCommand : ICommand
    {
        public SceneActuatorConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneActuatorConfiguration;

        public static byte CommandId => (byte)SceneActuatorConfigurationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static SceneActuatorConfigurationSetCommand Create(byte sceneId, TimeSpan dimmingDuration, bool overrideLevel, byte level)
        {
            // Set: Scene ID (1) + Dimming Duration (1) + Override|Reserved (1) + Level (1) = 4 bytes
            Span<byte> commandParameters =
            [
                sceneId,
                DurationEncoding.Encode(dimmingDuration),
                (byte)(overrideLevel ? 0b1000_0000 : 0),
                level,
            ];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SceneActuatorConfigurationSetCommand(frame);
        }
    }

    internal readonly struct SceneActuatorConfigurationGetCommand : ICommand
    {
        public SceneActuatorConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneActuatorConfiguration;

        public static byte CommandId => (byte)SceneActuatorConfigurationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static SceneActuatorConfigurationGetCommand Create(byte sceneId)
        {
            ReadOnlySpan<byte> commandParameters = [sceneId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SceneActuatorConfigurationGetCommand(frame);
        }
    }

    internal readonly struct SceneActuatorConfigurationReportCommand : ICommand
    {
        public SceneActuatorConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneActuatorConfiguration;

        public static byte CommandId => (byte)SceneActuatorConfigurationCommand.Report;

        public CommandClassFrame Frame { get; }

        public static SceneActuatorConfigurationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // Report: Scene ID (1) + Level (1) + Dimming Duration (1) = 3 bytes
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning(
                    "Scene Actuator Configuration Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Scene Actuator Configuration Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte sceneId = span[0];
            byte level = span[1];
            TimeSpan dimmingDuration = DurationEncoding.Decode(span[2], maxMinuteByte: 0xFE) ?? TimeSpan.Zero;

            return new SceneActuatorConfigurationReport(sceneId, level, dimmingDuration);
        }
    }
}
