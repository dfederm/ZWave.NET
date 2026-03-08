using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Specifies the character encoding used for node name and location text fields.
/// </summary>
internal enum CharPresentation : byte
{
    /// <summary>
    /// Standard ASCII codes (values 128-255 are ignored).
    /// </summary>
    Ascii = 0x00,

    /// <summary>
    /// Standard and OEM Extended ASCII codes.
    /// </summary>
    OemExtendedAscii = 0x01,

    /// <summary>
    /// Unicode UTF-16 (big-endian).
    /// </summary>
    Utf16 = 0x02,
}

/// <summary>
/// Represents the commands in the Node Naming and Location Command Class.
/// </summary>
public enum NodeNamingAndLocationCommand : byte
{
    /// <summary>
    /// Set the name of the receiving node.
    /// </summary>
    NodeNameSet = 0x01,

    /// <summary>
    /// Request the stored name from a node.
    /// </summary>
    NodeNameGet = 0x02,

    /// <summary>
    /// Advertise the name assigned to the sending node.
    /// </summary>
    NodeNameReport = 0x03,

    /// <summary>
    /// Set the location of the receiving node.
    /// </summary>
    NodeLocationSet = 0x04,

    /// <summary>
    /// Request the stored node location from a node.
    /// </summary>
    NodeLocationGet = 0x05,

    /// <summary>
    /// Advertise the node location.
    /// </summary>
    NodeLocationReport = 0x06,
}

/// <summary>
/// The Node Naming and Location Command Class is used to assign a name and a location text string to a supporting node.
/// </summary>
[CommandClass(CommandClassId.NodeNamingAndLocation)]
public sealed partial class NodeNamingAndLocationCommandClass : CommandClass<NodeNamingAndLocationCommand>
{
    /// <summary>
    /// The maximum number of bytes for a node name or location text field.
    /// </summary>
    internal const int MaxTextBytes = 16;

    internal NodeNamingAndLocationCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    internal override CommandClassCategory Category => CommandClassCategory.Management;

    /// <inheritdoc />
    public override bool? IsCommandSupported(NodeNamingAndLocationCommand command)
        => command switch
        {
            NodeNamingAndLocationCommand.NodeNameSet => true,
            NodeNamingAndLocationCommand.NodeNameGet => true,
            NodeNamingAndLocationCommand.NodeLocationSet => true,
            NodeNamingAndLocationCommand.NodeLocationGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetNameAsync(cancellationToken).ConfigureAwait(false);
        _ = await GetLocationAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((NodeNamingAndLocationCommand)frame.CommandId)
        {
            case NodeNamingAndLocationCommand.NodeNameReport:
            {
                string name = NodeNameReportCommand.Parse(frame, Logger);
                Name = name;
                OnNodeNameReportReceived?.Invoke(name);
                break;
            }
            case NodeNamingAndLocationCommand.NodeLocationReport:
            {
                string location = NodeLocationReportCommand.Parse(frame, Logger);
                Location = location;
                OnNodeLocationReportReceived?.Invoke(location);
                break;
            }
        }
    }

    /// <summary>
    /// Decodes text bytes using the specified character presentation.
    /// </summary>
    internal static string DecodeText(CharPresentation charPresentation, ReadOnlySpan<byte> data)
        => charPresentation switch
        {
            CharPresentation.Utf16 => Encoding.BigEndianUnicode.GetString(data),
            // The spec defines OEM Extended ASCII (0x01) as a distinct encoding, but no specific
            // code page is referenced. In practice, devices use only the 7-bit ASCII subset.
            // This matches zwave-js, which also decodes OEM Extended ASCII as plain ASCII.
            _ => Encoding.ASCII.GetString(data),
        };

    /// <summary>
    /// Determines the best character presentation for the given text.
    /// Uses ASCII if all characters are in the 7-bit range, otherwise UTF-16 BE.
    /// </summary>
    internal static CharPresentation GetCharPresentation(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] > 127)
            {
                return CharPresentation.Utf16;
            }
        }

        return CharPresentation.Ascii;
    }

    /// <summary>
    /// Encodes text into the destination span using the specified character presentation.
    /// </summary>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="ArgumentException">The encoded text exceeds <see cref="MaxTextBytes"/> bytes.</exception>
    internal static int EncodeText(string text, CharPresentation charPresentation, Span<byte> destination)
    {
        Encoding encoding = charPresentation == CharPresentation.Utf16
            ? Encoding.BigEndianUnicode
            : Encoding.ASCII;

        if (!encoding.TryGetBytes(text.AsSpan(), destination[..MaxTextBytes], out int bytesWritten))
        {
            throw new ArgumentException($"Text exceeds the maximum of {MaxTextBytes} encoded bytes.", nameof(text));
        }

        return bytesWritten;
    }
}
