using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class NodeNamingAndLocationCommandClass
{
    /// <summary>
    /// Gets the name of the node, or <see langword="null"/> if not yet retrieved.
    /// </summary>
    public string? Name { get; private set; }

    /// <summary>
    /// Occurs when a Node Name Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<string>? OnNodeNameReportReceived;

    /// <summary>
    /// Request the stored name from a node.
    /// </summary>
    public async Task<string> GetNameAsync(CancellationToken cancellationToken)
    {
        NodeNameGetCommand command = NodeNameGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NodeNameReportCommand>(cancellationToken).ConfigureAwait(false);
        string name = NodeNameReportCommand.Parse(reportFrame, Logger);
        Name = name;
        OnNodeNameReportReceived?.Invoke(name);
        return name;
    }

    /// <summary>
    /// Set the name of a node.
    /// </summary>
    /// <param name="name">The name to assign to the node. Maximum 16 ASCII characters or 8 Unicode characters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentException">The name exceeds the maximum encoded length of 16 bytes.</exception>
    public async Task SetNameAsync(string name, CancellationToken cancellationToken)
    {
        NodeNameSetCommand command = NodeNameSetCommand.Create(name);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct NodeNameSetCommand : ICommand
    {
        public NodeNameSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NodeNameSet;

        public CommandClassFrame Frame { get; }

        public static NodeNameSetCommand Create(string name)
        {
            CharPresentation charPresentation = GetCharPresentation(name);
            Span<byte> commandParameters = stackalloc byte[1 + MaxTextBytes];
            commandParameters[0] = (byte)((byte)charPresentation & 0b0000_0111);
            int bytesWritten = EncodeText(name, charPresentation, commandParameters[1..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters[..(1 + bytesWritten)]);
            return new NodeNameSetCommand(frame);
        }
    }

    internal readonly struct NodeNameGetCommand : ICommand
    {
        public NodeNameGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NodeNameGet;

        public CommandClassFrame Frame { get; }

        public static NodeNameGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new NodeNameGetCommand(frame);
        }
    }

    internal readonly struct NodeNameReportCommand : ICommand
    {
        public NodeNameReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NodeNameReport;

        public CommandClassFrame Frame { get; }

        public static string Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Node Name Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Node Name Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            CharPresentation charPresentation = (CharPresentation)(span[0] & 0b0000_0111);
            ReadOnlySpan<byte> textBytes = span.Length > 1 ? span[1..] : [];
            return DecodeText(charPresentation, textBytes);
        }
    }
}
