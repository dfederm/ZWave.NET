using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a parsed CRC-16 Encapsulation frame.
/// </summary>
public readonly record struct Crc16Encapsulation(
    /// <summary>
    /// The encapsulated command class frame (checksum already verified).
    /// </summary>
    CommandClassFrame EncapsulatedFrame);

public sealed partial class Crc16EncapsulationCommandClass
{
    /// <summary>
    /// Creates a CRC-16 Encapsulation frame wrapping the specified command.
    /// </summary>
    /// <param name="encapsulatedFrame">The command to encapsulate.</param>
    public static CommandClassFrame CreateEncapsulation(CommandClassFrame encapsulatedFrame)
        => CommandEncapsulationCommand.Create(encapsulatedFrame).Frame;

    /// <summary>
    /// Parses a CRC-16 Encapsulation frame, verifying the checksum.
    /// </summary>
    /// <returns>The encapsulated frame, or <c>null</c> if the frame is malformed or the checksum does not match.</returns>
    /// <remarks>
    /// A checksum mismatch or malformed frame is a normal, expected occurrence on low-speed
    /// links (which is the purpose of this CC), so it returns <c>null</c> rather than throwing.
    /// </remarks>
    public static Crc16Encapsulation? ParseEncapsulation(CommandClassFrame frame, ILogger logger)
        => CommandEncapsulationCommand.Parse(frame, logger);

    /// <summary>
    /// CRC-16 Encapsulation Command (spec §3.1.2).
    /// </summary>
    /// <remarks>
    /// Wire format:
    ///   byte 0: CC = 0x56
    ///   byte 1: Command = 0x01 (CRC 16 ENCAP)
    ///   byte 2..N-2: Encapsulated command (Command Class + Command ID + parameters)
    ///   byte N-1: Checksum MSB
    ///   byte N: Checksum LSB
    /// The CRC-16 (CCIT-FALSE) is computed over bytes 0..N-2 (CC ID through the last payload
    /// byte) and stored big-endian (MSB first).
    /// </remarks>
    internal readonly struct CommandEncapsulationCommand : ICommand
    {
        public CommandEncapsulationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Crc16Encapsulation;

        public static byte CommandId => (byte)Crc16EncapsulationCommand.CommandEncapsulation;

        public CommandClassFrame Frame { get; }

        public static CommandEncapsulationCommand Create(CommandClassFrame encapsulatedFrame)
        {
            ReadOnlySpan<byte> encapsulatedData = encapsulatedFrame.Data.Span;
            byte[] parameters = new byte[2 + encapsulatedData.Length];

            // The CRC-16 checksum covers the CC id, command id, and the encapsulated data (spec
            // §3.1.2). Lay the buffer out as [CC][Cmd][data...] so it can be computed over directly.
            parameters[0] = (byte)CommandClassId;
            parameters[1] = CommandId;
            encapsulatedData.CopyTo(parameters.AsSpan(2));
            ushort checksum = Crc16.Compute(parameters);

            // Rebuild the buffer in place as the frame parameters [data...][checksum MSB][checksum
            // LSB] by shifting the data bytes down by two, then appending the big-endian checksum.
            for (int i = 0; i < encapsulatedData.Length; i++)
            {
                parameters[i] = parameters[i + 2];
            }

            parameters[^2] = (byte)(checksum >> 8);
            parameters[^1] = (byte)(checksum & 0xFF);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new CommandEncapsulationCommand(frame);
        }

        public static Crc16Encapsulation? Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum frame: CC(1) + Cmd(1) + inner CC(1) + inner Cmd(1) + checksum(2) = 6 bytes.
            if (frame.Data.Length < 6)
            {
                logger.LogWarning("CRC-16 Encapsulation frame is too short ({Length} bytes)", frame.Data.Length);
                return null;
            }

            ReadOnlySpan<byte> data = frame.Data.Span;

            // The inner command class must be 8-bit (a single leading byte). Extended 16-bit
            // command classes (0xFF-prefixed) are not representable by CommandClassId and are
            // therefore unsupported here.
            if (data[2] == 0xFF)
            {
                logger.LogWarning("CRC-16 Encapsulation frame contains an extended (16-bit) command class which is not supported");
                return null;
            }

            ushort storedChecksum = (ushort)((data[^2] << 8) | data[^1]);
            ushort computedChecksum = Crc16.Compute(data[..^2]);
            if (storedChecksum != computedChecksum)
            {
                logger.LogWarning(
                    "CRC-16 Encapsulation checksum mismatch (stored {Stored:X4}, computed {Computed:X4})",
                    storedChecksum,
                    computedChecksum);
                return null;
            }

            CommandClassFrame encapsulatedFrame = new CommandClassFrame(frame.Data.Slice(2, frame.Data.Length - 4));
            return new Crc16Encapsulation(encapsulatedFrame);
        }
    }
}
