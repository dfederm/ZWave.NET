using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the information associated with a tone on a Sound Switch device.
/// </summary>
public readonly record struct SoundSwitchToneInfo(
    /// <summary>
    /// The tone identifier (1-based).
    /// </summary>
    byte ToneIdentifier,

    /// <summary>
    /// The duration in seconds it takes to play this tone.
    /// </summary>
    ushort DurationSeconds,

    /// <summary>
    /// The name or label assigned to this tone, encoded in UTF-8.
    /// </summary>
    string Name);

public sealed partial class SoundSwitchCommandClass
{
    private readonly Dictionary<byte, SoundSwitchToneInfo> _toneInfos = [];

    /// <summary>
    /// Gets the information for each tone, keyed by tone identifier.
    /// </summary>
    public IReadOnlyDictionary<byte, SoundSwitchToneInfo> ToneInfos => _toneInfos;

    /// <summary>
    /// Request the information associated with a specific tone.
    /// </summary>
    public async Task<SoundSwitchToneInfo> GetToneInfoAsync(byte toneIdentifier, CancellationToken cancellationToken)
    {
        SoundSwitchToneInfoGetCommand command = SoundSwitchToneInfoGetCommand.Create(toneIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<SoundSwitchToneInfoReportCommand>(
            predicate: frame => frame.CommandParameters.Length > 0
                && frame.CommandParameters.Span[0] == toneIdentifier,
            cancellationToken).ConfigureAwait(false);
        SoundSwitchToneInfo toneInfo = SoundSwitchToneInfoReportCommand.Parse(reportFrame, Logger);
        _toneInfos[toneInfo.ToneIdentifier] = toneInfo;
        return toneInfo;
    }

    internal readonly struct SoundSwitchToneInfoGetCommand : ICommand
    {
        public SoundSwitchToneInfoGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ToneInfoGet;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchToneInfoGetCommand Create(byte toneIdentifier)
        {
            ReadOnlySpan<byte> commandParameters = [toneIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new SoundSwitchToneInfoGetCommand(frame);
        }
    }

    internal readonly struct SoundSwitchToneInfoReportCommand : ICommand
    {
        public SoundSwitchToneInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.SoundSwitch;

        public static byte CommandId => (byte)SoundSwitchCommand.ToneInfoReport;

        public CommandClassFrame Frame { get; }

        public static SoundSwitchToneInfo Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: Tone Identifier (1) + Duration (2) + Name Length (1) = 4 bytes
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Sound Switch Tone Info Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Sound Switch Tone Info Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            byte toneIdentifier = span[0];
            ushort durationSeconds = span.Slice(1, 2).ToUInt16BE();
            byte nameLength = span[3];

            if (frame.CommandParameters.Length < 4 + nameLength)
            {
                logger.LogWarning(
                    "Sound Switch Tone Info Report frame is too short for declared name length ({NameLength} bytes, but only {Available} available)",
                    nameLength,
                    frame.CommandParameters.Length - 4);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Sound Switch Tone Info Report frame is too short for declared name length");
            }

            string name = nameLength > 0
                ? Encoding.UTF8.GetString(span.Slice(4, nameLength))
                : string.Empty;

            return new SoundSwitchToneInfo(toneIdentifier, durationSeconds, name);
        }
    }
}
