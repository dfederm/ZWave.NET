namespace ZWave.CommandClasses;

public enum SceneActuatorConfigurationCommand : byte
{
    /// <summary>
    /// Set the actuator configuration for a given scene.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the actuator configuration for a given scene.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the actuator configuration for a given scene.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents the actuator configuration state for a scene.
/// </summary>
public readonly struct SceneActuatorConfigurationState
{
    public SceneActuatorConfigurationState(byte level, DurationReport dimmingDuration)
    {
        Level = level;
        DimmingDuration = dimmingDuration;
    }

    /// <summary>
    /// The level for the scene.
    /// </summary>
    public byte Level { get; }

    /// <summary>
    /// The dimming duration for the scene.
    /// </summary>
    public DurationReport DimmingDuration { get; }
}

[CommandClass(CommandClassId.SceneActuatorConfiguration)]
public sealed class SceneActuatorConfigurationCommandClass : CommandClass<SceneActuatorConfigurationCommand>
{
    private readonly Dictionary<byte, SceneActuatorConfigurationState> _scenes = new Dictionary<byte, SceneActuatorConfigurationState>();

    internal SceneActuatorConfigurationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the known scene actuator configuration states, keyed by scene ID.
    /// </summary>
    public IReadOnlyDictionary<byte, SceneActuatorConfigurationState> Scenes => _scenes;

    /// <inheritdoc />
    public override bool? IsCommandSupported(SceneActuatorConfigurationCommand command)
        => command switch
        {
            SceneActuatorConfigurationCommand.Set => true,
            SceneActuatorConfigurationCommand.Get => true,
            _ => false,
        };

    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Request the actuator configuration for a given scene.
    /// </summary>
    public async Task<SceneActuatorConfigurationState> GetAsync(byte sceneId, CancellationToken cancellationToken)
    {
        SceneActuatorConfigurationGetCommand command = SceneActuatorConfigurationGetCommand.Create(sceneId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<SceneActuatorConfigurationReportCommand>(
            frame => frame.CommandParameters.Span[0] == sceneId,
            cancellationToken).ConfigureAwait(false);
        return _scenes[sceneId];
    }

    /// <summary>
    /// Set the actuator configuration for a given scene.
    /// </summary>
    public async Task SetAsync(
        byte sceneId,
        DurationSet dimmingDuration,
        bool @override,
        byte level,
        CancellationToken cancellationToken)
    {
        SceneActuatorConfigurationSetCommand command = SceneActuatorConfigurationSetCommand.Create(
            sceneId,
            dimmingDuration,
            @override,
            level);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((SceneActuatorConfigurationCommand)frame.CommandId)
        {
            case SceneActuatorConfigurationCommand.Set:
            case SceneActuatorConfigurationCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case SceneActuatorConfigurationCommand.Report:
            {
                SceneActuatorConfigurationReportCommand command = new SceneActuatorConfigurationReportCommand(frame);
                _scenes[command.SceneId] = new SceneActuatorConfigurationState(
                    command.Level,
                    command.DimmingDuration);
                break;
            }
        }
    }

    private readonly struct SceneActuatorConfigurationSetCommand : ICommand
    {
        public SceneActuatorConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneActuatorConfiguration;

        public static byte CommandId => (byte)SceneActuatorConfigurationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static SceneActuatorConfigurationSetCommand Create(
            byte sceneId,
            DurationSet dimmingDuration,
            bool @override,
            byte level)
        {
            Span<byte> commandParameters = stackalloc byte[4];
            commandParameters[0] = sceneId;
            commandParameters[1] = dimmingDuration.Value;
            commandParameters[2] = @override ? (byte)0b1000_0000 : (byte)0x00;
            commandParameters[3] = level;
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SceneActuatorConfigurationSetCommand(frame);
        }
    }

    private readonly struct SceneActuatorConfigurationGetCommand : ICommand
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

    private readonly struct SceneActuatorConfigurationReportCommand : ICommand
    {
        public SceneActuatorConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneActuatorConfiguration;

        public static byte CommandId => (byte)SceneActuatorConfigurationCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The scene ID.
        /// </summary>
        public byte SceneId => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The level for the scene.
        /// </summary>
        public byte Level => Frame.CommandParameters.Span[1];

        /// <summary>
        /// The dimming duration for the scene.
        /// </summary>
        public DurationReport DimmingDuration => new DurationReport(Frame.CommandParameters.Span[2]);
    }
}
