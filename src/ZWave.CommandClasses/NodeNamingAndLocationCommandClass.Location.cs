using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class NodeNamingAndLocationCommandClass
{
    /// <summary>
    /// Gets the location of the node, or <see langword="null"/> if not yet retrieved.
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>
    /// Occurs when a Node Location Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<string>? OnNodeLocationReportReceived;

    /// <summary>
    /// Request the stored location from a node.
    /// </summary>
    public async Task<string> GetLocationAsync(CancellationToken cancellationToken)
    {
        NodeLocationGetCommand command = NodeLocationGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<NodeLocationReportCommand>(cancellationToken).ConfigureAwait(false);
        string location = NodeLocationReportCommand.Parse(reportFrame, Logger);
        Location = location;
        OnNodeLocationReportReceived?.Invoke(location);
        return location;
    }

    /// <summary>
    /// Set the location of a node.
    /// </summary>
    /// <param name="location">The location to assign to the node. Maximum 16 ASCII characters or 8 Unicode characters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="ArgumentException">The location exceeds the maximum encoded length of 16 bytes.</exception>
    public async Task SetLocationAsync(string location, CancellationToken cancellationToken)
    {
        NodeLocationSetCommand command = NodeLocationSetCommand.Create(location);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct NodeLocationSetCommand : ICommand
    {
        public NodeLocationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NodeLocationSet;

        public CommandClassFrame Frame { get; }

        public static NodeLocationSetCommand Create(string location)
        {
            CharPresentation charPresentation = GetCharPresentation(location);
            Span<byte> commandParameters = stackalloc byte[1 + MaxTextBytes];
            commandParameters[0] = (byte)((byte)charPresentation & 0b0000_0111);
            int bytesWritten = EncodeText(location, charPresentation, commandParameters[1..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters[..(1 + bytesWritten)]);
            return new NodeLocationSetCommand(frame);
        }
    }

    internal readonly struct NodeLocationGetCommand : ICommand
    {
        public NodeLocationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NodeLocationGet;

        public CommandClassFrame Frame { get; }

        public static NodeLocationGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new NodeLocationGetCommand(frame);
        }
    }

    internal readonly struct NodeLocationReportCommand : ICommand
    {
        public NodeLocationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.NodeNamingAndLocation;

        public static byte CommandId => (byte)NodeNamingAndLocationCommand.NodeLocationReport;

        public CommandClassFrame Frame { get; }

        public static string Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Node Location Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Node Location Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            CharPresentation charPresentation = (CharPresentation)(span[0] & 0b0000_0111);
            ReadOnlySpan<byte> textBytes = span.Length > 1 ? span[1..] : [];
            return DecodeText(charPresentation, textBytes);
        }
    }
}
