using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands for the Scene Controller Configuration Command Class.
/// </summary>
public enum SceneControllerConfigurationCommand : byte
{
    /// <summary>
    /// Configure the scene settings for an association group.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the scene settings for an association group.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current scene controller settings.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents a Scene Controller Configuration Report received from a device.
/// </summary>
/// <remarks>
/// A scene ID of 0 indicates that the scene is disabled for the specified group.
/// </remarks>
public readonly record struct SceneControllerConfigurationReport(
    /// <summary>
    /// The association group ID for which settings are being advertised.
    /// </summary>
    byte GroupId,

    /// <summary>
    /// The scene ID associated with the group.
    /// Values 1–255 indicate an actual scene ID.
    /// The value 0 indicates the group/scene is disabled.
    /// </summary>
    byte SceneId,

    /// <summary>
    /// The dimming duration associated with the group.
    /// </summary>
    TimeSpan DimmingDuration);

/// <summary>
/// Implementation of the Scene Controller Configuration Command Class (version 1).
/// </summary>
/// <remarks>
/// <para>The Scene Controller Configuration Command Class is used to configure nodes
/// launching scenes using their association groups.</para>
/// <para>A node supporting this command class MUST support 255 scene IDs (1 to 255)
/// and MUST support the Association Command Class.</para>
/// </remarks>
[CommandClass(CommandClassId.SceneControllerConfiguration)]
public sealed class SceneControllerConfigurationCommandClass : CommandClass<SceneControllerConfigurationCommand>
{
    internal SceneControllerConfigurationCommandClass(
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
    public SceneControllerConfigurationReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Scene Controller Configuration Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<SceneControllerConfigurationReport>? OnReportReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(SceneControllerConfigurationCommand command)
        => command switch
        {
            SceneControllerConfigurationCommand.Set => true,
            SceneControllerConfigurationCommand.Get => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override Task InterviewAsync(CancellationToken cancellationToken)
    {
        // The number of groups depends on the Association CC; querying all groups during interview
        // is impractical. Scene controller configuration is queried on demand via GetAsync.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Request the scene settings for a given association group.
    /// </summary>
    /// <param name="groupId">
    /// The association group ID to query. Values 1–255 request a specific group.
    /// The value 0 requests the currently active group and scene (last activated).
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scene controller configuration report.</returns>
    public async Task<SceneControllerConfigurationReport> GetAsync(byte groupId, CancellationToken cancellationToken)
    {
        var command = SceneControllerConfigurationGetCommand.Create(groupId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<SceneControllerConfigurationReportCommand>(cancellationToken).ConfigureAwait(false);
        SceneControllerConfigurationReport report = SceneControllerConfigurationReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Configure the scene settings for an association group.
    /// </summary>
    /// <param name="groupId">
    /// The association group ID to configure.
    /// Values MUST be a sequence starting from 1.
    /// Group ID 1 SHOULD NOT be used as it is reserved for the Lifeline association group.
    /// </param>
    /// <param name="sceneId">
    /// The scene ID to associate with the group.
    /// Values 1–255 associate a scene. The value 0 disables the scene for the group.
    /// </param>
    /// <param name="dimmingDuration">
    /// The dimming duration the node should use in the Scene Activation Set command
    /// when issuing it via the specified group.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(byte groupId, byte sceneId, TimeSpan dimmingDuration, CancellationToken cancellationToken)
    {
        var command = SceneControllerConfigurationSetCommand.Create(groupId, sceneId, dimmingDuration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((SceneControllerConfigurationCommand)frame.CommandId)
        {
            case SceneControllerConfigurationCommand.Report:
            {
                SceneControllerConfigurationReport report = SceneControllerConfigurationReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct SceneControllerConfigurationSetCommand : ICommand
    {
        public SceneControllerConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneControllerConfiguration;

        public static byte CommandId => (byte)SceneControllerConfigurationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static SceneControllerConfigurationSetCommand Create(byte groupId, byte sceneId, TimeSpan dimmingDuration)
        {
            ReadOnlySpan<byte> commandParameters = [groupId, sceneId, DurationEncoding.Encode(dimmingDuration)];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SceneControllerConfigurationSetCommand(frame);
        }
    }

    internal readonly struct SceneControllerConfigurationGetCommand : ICommand
    {
        public SceneControllerConfigurationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneControllerConfiguration;

        public static byte CommandId => (byte)SceneControllerConfigurationCommand.Get;

        public CommandClassFrame Frame { get; }

        public static SceneControllerConfigurationGetCommand Create(byte groupId)
        {
            ReadOnlySpan<byte> commandParameters = [groupId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SceneControllerConfigurationGetCommand(frame);
        }
    }

    internal readonly struct SceneControllerConfigurationReportCommand : ICommand
    {
        public SceneControllerConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneControllerConfiguration;

        public static byte CommandId => (byte)SceneControllerConfigurationCommand.Report;

        public CommandClassFrame Frame { get; }

        public static SceneControllerConfigurationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // Report: Group ID (1) + Scene ID (1) + Dimming Duration (1) = 3 bytes
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning(
                    "Scene Controller Configuration Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Scene Controller Configuration Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte groupId = span[0];
            byte sceneId = span[1];
            TimeSpan dimmingDuration = DurationEncoding.Decode(span[2], maxMinuteByte: 0xFE) ?? TimeSpan.Zero;

            return new SceneControllerConfigurationReport(groupId, sceneId, dimmingDuration);
        }
    }
}
