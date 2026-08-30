using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a parsed Multi Command Encapsulation frame.
/// </summary>
public readonly record struct MultiCommandEncapsulation(
    /// <summary>
    /// The encapsulated command class frames, in the order they were transmitted.
    /// </summary>
    CommandClassFrame[] Commands);

public sealed partial class MultiCommandCommandClass
{
    /// <summary>
    /// Creates a Multi Command Encapsulation frame bundling the specified commands.
    /// </summary>
    /// <param name="commands">The commands to bundle (at least one).</param>
    public static CommandClassFrame CreateEncapsulation(CommandClassFrame[] commands)
        => CommandEncapsulationCommand.Create(commands).Frame;

    /// <summary>
    /// Parses a Multi Command Encapsulation frame into its constituent commands.
    /// </summary>
    public static MultiCommandEncapsulation ParseEncapsulation(CommandClassFrame frame, ILogger logger)
        => CommandEncapsulationCommand.Parse(frame, logger);

    /// <summary>
    /// Multi Command Encapsulation Command (spec §3.4.3).
    /// </summary>
    /// <remarks>
    /// Wire format:
    ///   byte 0: CC = 0x8F
    ///   byte 1: Command = 0x01 (MULTI CMD ENCAP)
    ///   byte 2: Number of commands (Count)
    ///   then, per command: [Length][Command Class (1 or 2 bytes)][Command ID][Data...]
    ///   where Length is the size of (Command Class + Command ID + Data) for that command.
    /// </remarks>
    internal readonly struct CommandEncapsulationCommand : ICommand
    {
        public CommandEncapsulationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiCommand;

        public static byte CommandId => (byte)MultiCommandCommand.CommandEncapsulation;

        public CommandClassFrame Frame { get; }

        public static CommandEncapsulationCommand Create(CommandClassFrame[] commands)
        {
            if (commands.Length == 0)
            {
                throw new ArgumentException("At least one command is required.", nameof(commands));
            }

            if (commands.Length > 255)
            {
                throw new ArgumentException("A Multi Command frame may contain at most 255 commands.", nameof(commands));
            }

            int totalLength = 1; // Count byte.
            foreach (CommandClassFrame command in commands)
            {
                int length = command.Data.Length;
                if (length < 2 || length > 255)
                {
                    throw new ArgumentException("Each encapsulated command must be between 2 and 255 bytes.");
                }

                totalLength += 1 + length; // Length byte + command bytes.
            }

            byte[] parameters = new byte[totalLength];
            int offset = 0;
            parameters[offset++] = (byte)commands.Length;
            foreach (CommandClassFrame command in commands)
            {
                parameters[offset++] = (byte)command.Data.Length;
                command.Data.Span.CopyTo(parameters.AsSpan(offset));
                offset += command.Data.Length;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new CommandEncapsulationCommand(frame);
        }

        public static MultiCommandEncapsulation Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Multi Command frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multi Command frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            byte count = parameters[0];

            var commands = new CommandClassFrame[count];
            int offset = 1;
            for (int i = 0; i < count; i++)
            {
                if (offset >= parameters.Length)
                {
                    logger.LogWarning("Multi Command frame is truncated: expected {Count} commands but ran out of data at command {Index}", count, i);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multi Command frame is truncated");
                }

                int length = parameters[offset];
                if (length < 2 || offset + 1 + length > parameters.Length)
                {
                    logger.LogWarning("Multi Command frame is truncated: command {Index} claims {Length} bytes but only {Available} are available", i, length, parameters.Length - offset - 1);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multi Command frame is truncated");
                }

                // Extended 16-bit command classes (0xFF-prefixed) are not representable by
                // CommandClassId and are therefore unsupported here.
                if (parameters[offset + 1] == 0xFF)
                {
                    logger.LogWarning("Multi Command frame contains an extended (16-bit) command class which is not supported");
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multi Command frame contains an unsupported extended command class");
                }

                commands[i] = new CommandClassFrame(frame.Data.Slice(2 + offset + 1, length));
                offset += 1 + length;
            }

            if (offset != parameters.Length)
            {
                logger.LogWarning("Multi Command frame has {Extra} trailing bytes after the declared command count", parameters.Length - offset);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multi Command frame has trailing bytes");
            }

            return new MultiCommandEncapsulation(commands);
        }
    }
}
