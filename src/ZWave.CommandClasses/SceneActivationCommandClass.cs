namespace ZWave.CommandClasses;

public enum SceneActivationCommand : byte
{
    /// <summary>
    /// Activate a scene on the receiving node.
    /// </summary>
    Set = 0x01,
}

/// <summary>
/// Represents the Scene Activation Command Class.
/// </summary>
/// <remarks>
/// A device sends the Set command unsolicited to activate a scene.
/// The controller does not send any commands to the device for this command class.
/// </remarks>
[CommandClass(CommandClassId.SceneActivation)]
public sealed class SceneActivationCommandClass : CommandClass<SceneActivationCommand>
{
    internal SceneActivationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the last activated scene ID.
    /// </summary>
    public byte? LastSceneId { get; private set; }

    /// <summary>
    /// Gets the last dimming duration.
    /// </summary>
    public DurationSet? LastDimmingDuration { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(SceneActivationCommand command) => false;

    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((SceneActivationCommand)frame.CommandId)
        {
            case SceneActivationCommand.Set:
            {
                var command = new SceneActivationSetCommand(frame);
                LastSceneId = command.SceneId;
                LastDimmingDuration = command.DimmingDuration;
                break;
            }
        }
    }

    private readonly struct SceneActivationSetCommand : ICommand
    {
        public SceneActivationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneActivation;

        public static byte CommandId => (byte)SceneActivationCommand.Set;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The scene ID to activate (1-255).
        /// </summary>
        public byte SceneId => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The dimming duration for the scene activation.
        /// </summary>
        public DurationSet DimmingDuration => new DurationSet(Frame.CommandParameters.Span[1]);
    }
}
