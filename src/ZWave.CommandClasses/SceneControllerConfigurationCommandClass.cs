namespace ZWave.CommandClasses;

public enum SceneControllerConfigurationCommand : byte
{
    /// <summary>
    /// Set the controller configuration for a given group.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the controller configuration for a given group.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the controller configuration for a given group.
    /// </summary>
    Report = 0x03,
}

/// <summary>
/// Represents the controller configuration state for a group.
/// </summary>
public readonly struct SceneControllerConfigurationState
{
    public SceneControllerConfigurationState(byte sceneId, DurationReport dimmingDuration)
    {
        SceneId = sceneId;
        DimmingDuration = dimmingDuration;
    }

    /// <summary>
    /// The scene ID associated with the group.
    /// </summary>
    public byte SceneId { get; }

    /// <summary>
    /// The dimming duration for the group.
    /// </summary>
    public DurationReport DimmingDuration { get; }
}

[CommandClass(CommandClassId.SceneControllerConfiguration)]
public sealed class SceneControllerConfigurationCommandClass : CommandClass<SceneControllerConfigurationCommand>
{
    private readonly Dictionary<byte, SceneControllerConfigurationState> _groups = new Dictionary<byte, SceneControllerConfigurationState>();

    internal SceneControllerConfigurationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the known scene controller configuration states, keyed by group ID.
    /// </summary>
    public IReadOnlyDictionary<byte, SceneControllerConfigurationState> Groups => _groups;

    /// <inheritdoc />
    public override bool? IsCommandSupported(SceneControllerConfigurationCommand command)
        => command switch
        {
            SceneControllerConfigurationCommand.Set => true,
            SceneControllerConfigurationCommand.Get => true,
            _ => false,
        };

    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Request the controller configuration for a given group.
    /// </summary>
    public async Task<SceneControllerConfigurationState> GetAsync(byte groupId, CancellationToken cancellationToken)
    {
        SceneControllerConfigurationGetCommand command = SceneControllerConfigurationGetCommand.Create(groupId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<SceneControllerConfigurationReportCommand>(
            frame => frame.CommandParameters.Span[0] == groupId,
            cancellationToken).ConfigureAwait(false);
        return _groups[groupId];
    }

    /// <summary>
    /// Set the controller configuration for a given group.
    /// </summary>
    public async Task SetAsync(
        byte groupId,
        byte sceneId,
        DurationSet dimmingDuration,
        CancellationToken cancellationToken)
    {
        SceneControllerConfigurationSetCommand command = SceneControllerConfigurationSetCommand.Create(
            groupId,
            sceneId,
            dimmingDuration);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((SceneControllerConfigurationCommand)frame.CommandId)
        {
            case SceneControllerConfigurationCommand.Set:
            case SceneControllerConfigurationCommand.Get:
            {
                // We don't expect to recieve these commands
                break;
            }
            case SceneControllerConfigurationCommand.Report:
            {
                SceneControllerConfigurationReportCommand command = new SceneControllerConfigurationReportCommand(frame);
                _groups[command.GroupId] = new SceneControllerConfigurationState(
                    command.SceneId,
                    command.DimmingDuration);
                break;
            }
        }
    }

    private readonly struct SceneControllerConfigurationSetCommand : ICommand
    {
        public SceneControllerConfigurationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneControllerConfiguration;

        public static byte CommandId => (byte)SceneControllerConfigurationCommand.Set;

        public CommandClassFrame Frame { get; }

        public static SceneControllerConfigurationSetCommand Create(
            byte groupId,
            byte sceneId,
            DurationSet dimmingDuration)
        {
            Span<byte> commandParameters = stackalloc byte[3];
            commandParameters[0] = groupId;
            commandParameters[1] = sceneId;
            commandParameters[2] = dimmingDuration.Value;
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SceneControllerConfigurationSetCommand(frame);
        }
    }

    private readonly struct SceneControllerConfigurationGetCommand : ICommand
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

    private readonly struct SceneControllerConfigurationReportCommand : ICommand
    {
        public SceneControllerConfigurationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SceneControllerConfiguration;

        public static byte CommandId => (byte)SceneControllerConfigurationCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The group ID.
        /// </summary>
        public byte GroupId => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The scene ID associated with the group.
        /// </summary>
        public byte SceneId => Frame.CommandParameters.Span[1];

        /// <summary>
        /// The dimming duration for the group.
        /// </summary>
        public DurationReport DimmingDuration => new DurationReport(Frame.CommandParameters.Span[2]);
    }
}
