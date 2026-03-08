using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Commands for the Scene Activation Command Class.
/// </summary>
public enum SceneActivationCommand : byte
{
    /// <summary>
    /// Activate the setting associated with a scene ID.
    /// </summary>
    Set = 0x01,
}

/// <summary>
/// Represents a Scene Activation Set received from a device.
/// </summary>
/// <remarks>
/// A <see langword="null"/> dimming duration indicates that the device should use the duration
/// configured by Scene Actuator Configuration Set / Scene Controller Configuration Set (wire value 0xFF).
/// </remarks>
public readonly record struct SceneActivation(
    /// <summary>
    /// The scene ID to activate. Valid range is 1–255.
    /// </summary>
    byte SceneId,

    /// <summary>
    /// The dimming duration for the transition to the target level.
    /// <see langword="null"/> means the device should use the previously configured duration.
    /// </summary>
    TimeSpan? DimmingDuration);

/// <summary>
/// Implementation of the Scene Activation Command Class (version 1).
/// </summary>
/// <remarks>
/// The Scene Activation Command Class is used for launching scenes in a number of actuator nodes.
/// A node supporting this command class MUST also support the Scene Actuator Configuration Command Class.
/// </remarks>
[CommandClass(CommandClassId.SceneActivation)]
public sealed class SceneActivationCommandClass : CommandClass<SceneActivationCommand>
{
    internal SceneActivationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last scene activation received from the device.
    /// </summary>
    public SceneActivation? LastActivation { get; private set; }

    /// <summary>
    /// Event raised when a Scene Activation Set is received, both solicited and unsolicited.
    /// </summary>
    public event Action<SceneActivation>? OnActivationReceived;

    /// <inheritdoc />
    public override bool? IsCommandSupported(SceneActivationCommand command)
        => command switch
        {
            SceneActivationCommand.Set => true,
            _ => false,
        };

    /// <inheritdoc />
    internal override Task InterviewAsync(CancellationToken cancellationToken)
    {
        // Scene Activation CC has no Get command, so there is nothing to query during interview.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Activate the specified scene on supporting devices.
    /// </summary>
    /// <param name="sceneId">The scene ID to activate. Must be in the range 1–255.</param>
    /// <param name="dimmingDuration">
    /// The dimming duration for the transition.
    /// Use <see langword="null"/> to use the duration configured by Scene Actuator Configuration Set.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAsync(byte sceneId, TimeSpan? dimmingDuration, CancellationToken cancellationToken)
    {
        var command = SceneActivationSetCommand.Create(sceneId, dimmingDuration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((SceneActivationCommand)frame.CommandId)
        {
            case SceneActivationCommand.Set:
            {
                SceneActivation activation = SceneActivationSetCommand.Parse(frame, Logger);
                LastActivation = activation;
                OnActivationReceived?.Invoke(activation);
                break;
            }
        }
    }

    internal readonly struct SceneActivationSetCommand : ICommand
    {
        public SceneActivationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneActivation;

        public static byte CommandId => (byte)SceneActivationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static SceneActivationSetCommand Create(byte sceneId, TimeSpan? dimmingDuration)
        {
            byte durationByte = dimmingDuration.HasValue
                ? DurationEncoding.Encode(dimmingDuration.Value)
                : (byte)0xFF;
            ReadOnlySpan<byte> commandParameters = [sceneId, durationByte];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SceneActivationSetCommand(frame);
        }

        public static SceneActivation Parse(CommandClassFrame frame, ILogger logger)
        {
            // Scene Activation Set: Scene ID (1 byte) + Dimming Duration (1 byte)
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Scene Activation Set frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Scene Activation Set frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte sceneId = span[0];

            // 0xFF = use configured duration (null), 0x00-0xFE decoded by DurationEncoding
            TimeSpan? dimmingDuration = DurationEncoding.Decode(span[1], maxMinuteByte: 0xFE);

            return new SceneActivation(sceneId, dimmingDuration);
        }
    }
}
