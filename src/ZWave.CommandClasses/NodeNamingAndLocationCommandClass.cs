using System.Text;

namespace ZWave.CommandClasses;

public enum NodeNamingAndLocationCommand : byte
{
    /// <summary>
    /// Set the name of a node.
    /// </summary>
    NameSet = 0x01,

    /// <summary>
    /// Request the name of a node.
    /// </summary>
    NameGet = 0x02,

    /// <summary>
    /// Advertise the name of a node.
    /// </summary>
    NameReport = 0x03,

    /// <summary>
    /// Set the location of a node.
    /// </summary>
    LocationSet = 0x04,

    /// <summary>
    /// Request the location of a node.
    /// </summary>
    LocationGet = 0x05,

    /// <summary>
    /// Advertise the location of a node.
    /// </summary>
    LocationReport = 0x06,
}

[CommandClass(CommandClassId.NodeNamingAndLocation)]
public sealed class NodeNamingAndLocationCommandClass : CommandClass<NodeNamingAndLocationCommand>
{
    internal NodeNamingAndLocationCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the name of the node.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Gets the location of the node.
    /// </summary>
    public string? Location { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(NodeNamingAndLocationCommand command)
        => command switch
        {
            NodeNamingAndLocationCommand.NameSet => true,
            NodeNamingAndLocationCommand.NameGet => true,
            NodeNamingAndLocationCommand.LocationSet => true,
            NodeNamingAndLocationCommand.LocationGet => true,
            _ => false,
        };

    /// <summary>
    /// Request the name of the node.
    /// </summary>
    public async Task<string?> GetNameAsync(CancellationToken cancellationToken)
    {
        var command = NodeNamingNameGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<NodeNamingNameReportCommand>(cancellationToken).ConfigureAwait(false);
        return Name;
    }

    /// <summary>
    /// Set the name of the node.
    /// </summary>
    public async Task SetNameAsync(string name, CancellationToken cancellationToken)
    {
        var command = NodeNamingNameSetCommand.Create(name);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the location of the node.
    /// </summary>
    public async Task<string?> GetLocationAsync(CancellationToken cancellationToken)
    {
        var command = NodeNamingLocationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<NodeNamingLocationReportCommand>(cancellationToken).ConfigureAwait(false);
        return Location;
    }

    /// <summary>
    /// Set the location of the node.
    /// </summary>
    public async Task SetLocationAsync(string location, CancellationToken cancellationToken)
    {
        var command = NodeNamingLocationSetCommand.Create(location);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetNameAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetLocationAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((NodeNamingAndLocationCommand)frame.CommandId)
        {
            case NodeNamingAndLocationCommand.NameSet:
            case NodeNamingAndLocationCommand.NameGet:
            case NodeNamingAndLocationCommand.LocationSet:
            case NodeNamingAndLocationCommand.LocationGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case NodeNamingAndLocationCommand.NameReport:
            {
                var command = new NodeNamingNameReportCommand(frame);
                Name = command.Text;
                break;
            }
            case NodeNamingAndLocationCommand.LocationReport:
            {
                var command = new NodeNamingLocationReportCommand(frame);
                Location = command.Text;
                break;
            }
        }
    }

    private static Encoding GetTextEncoding(byte encodingByte)
        => (encodingByte & 0x07) switch
        {
            0x00 => Encoding.ASCII,
            0x01 => Encoding.Latin1,
            0x02 => Encoding.BigEndianUnicode,
            _ => Encoding.ASCII,
        };

    private static string DecodeText(ReadOnlySpan<byte> commandParameters)
    {
        Encoding encoding = GetTextEncoding(commandParameters[0]);
        ReadOnlySpan<byte> textData = commandParameters.Slice(1);
        return encoding.GetString(textData);
    }

    private readonly struct NodeNamingNameSetCommand : ICommand
    {
        public NodeNamingNameSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NameSet;

        public CommandClassFrame Frame { get; }

        public static NodeNamingNameSetCommand Create(string name)
        {
            // Use UTF-16 encoding (0x02) to support all characters
            byte[] textBytes = Encoding.BigEndianUnicode.GetBytes(name);
            Span<byte> commandParameters = stackalloc byte[1 + textBytes.Length];
            commandParameters[0] = 0x02;
            textBytes.CopyTo(commandParameters.Slice(1));
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new NodeNamingNameSetCommand(frame);
        }
    }

    private readonly struct NodeNamingNameGetCommand : ICommand
    {
        public NodeNamingNameGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NameGet;

        public CommandClassFrame Frame { get; }

        public static NodeNamingNameGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new NodeNamingNameGetCommand(frame);
        }
    }

    private readonly struct NodeNamingNameReportCommand : ICommand
    {
        public NodeNamingNameReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NameReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The name of the node.
        /// </summary>
        public string Text => DecodeText(Frame.CommandParameters.Span);
    }

    private readonly struct NodeNamingLocationSetCommand : ICommand
    {
        public NodeNamingLocationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.LocationSet;

        public CommandClassFrame Frame { get; }

        public static NodeNamingLocationSetCommand Create(string location)
        {
            // Use UTF-16 encoding (0x02) to support all characters
            byte[] textBytes = Encoding.BigEndianUnicode.GetBytes(location);
            Span<byte> commandParameters = stackalloc byte[1 + textBytes.Length];
            commandParameters[0] = 0x02;
            textBytes.CopyTo(commandParameters.Slice(1));
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new NodeNamingLocationSetCommand(frame);
        }
    }

    private readonly struct NodeNamingLocationGetCommand : ICommand
    {
        public NodeNamingLocationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.LocationGet;

        public CommandClassFrame Frame { get; }

        public static NodeNamingLocationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new NodeNamingLocationGetCommand(frame);
        }
    }

    private readonly struct NodeNamingLocationReportCommand : ICommand
    {
        public NodeNamingLocationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.LocationReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The location of the node.
        /// </summary>
        public string Text => DecodeText(Frame.CommandParameters.Span);
    }
}
