using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a parsed Multi Channel Command Encapsulation frame.
/// </summary>
public readonly record struct MultiChannelCommandEncapsulation(
    /// <summary>
    /// The source End Point (0 = Root Device).
    /// </summary>
    byte SourceEndpoint,

    /// <summary>
    /// Whether the destination is specified as a bit mask.
    /// </summary>
    bool IsBitAddress,

    /// <summary>
    /// The destination End Point index or bit mask (depending on <see cref="IsBitAddress"/>).
    /// </summary>
    byte Destination,

    /// <summary>
    /// The encapsulated command class frame.
    /// </summary>
    CommandClassFrame EncapsulatedFrame);

public sealed partial class MultiChannelCommandClass
{
    /// <summary>
    /// Event raised when a Multi Channel Command Encapsulation is received.
    /// </summary>
    public Action<MultiChannelCommandEncapsulation>? OnCommandEncapsulationReceived { get; set; }

    /// <summary>
    /// Creates a Multi Channel Command Encapsulation frame targeting a single endpoint.
    /// </summary>
    /// <param name="sourceEndpoint">Source endpoint (0 = Root Device).</param>
    /// <param name="destinationEndpoint">Destination endpoint index (1–127).</param>
    /// <param name="encapsulatedFrame">The command to encapsulate.</param>
    public static CommandClassFrame CreateEncapsulation(byte sourceEndpoint, byte destinationEndpoint, CommandClassFrame encapsulatedFrame)
        => CommandEncapsulationCommand.Create(sourceEndpoint, destinationEndpoint, isBitAddress: false, encapsulatedFrame).Frame;

    /// <summary>
    /// Creates a Multi Channel Command Encapsulation frame targeting multiple endpoints via bit addressing.
    /// </summary>
    /// <param name="sourceEndpoint">Source endpoint (0 = Root Device).</param>
    /// <param name="destinationEndpoints">The destination endpoint indices (each must be 1–7).</param>
    /// <param name="encapsulatedFrame">The command to encapsulate.</param>
    public static CommandClassFrame CreateEncapsulation(byte sourceEndpoint, ReadOnlySpan<byte> destinationEndpoints, CommandClassFrame encapsulatedFrame)
    {
        byte bitMask = 0;
        foreach (byte ep in destinationEndpoints)
        {
            if (ep < 1 || ep > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationEndpoints), ep, "Bit-addressed endpoints must be between 1 and 7.");
            }

            bitMask |= (byte)(1 << (ep - 1));
        }

        return CommandEncapsulationCommand.Create(sourceEndpoint, bitMask, isBitAddress: true, encapsulatedFrame).Frame;
    }

    /// <summary>
    /// Parses a Multi Channel Command Encapsulation frame.
    /// </summary>
    public static MultiChannelCommandEncapsulation ParseEncapsulation(CommandClassFrame frame, ILogger logger)
        => CommandEncapsulationCommand.Parse(frame, logger);

    internal readonly struct CommandEncapsulationCommand : ICommand
    {
        public CommandEncapsulationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.CommandEncapsulation;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Creates a Multi Channel Command Encapsulation frame.
        /// </summary>
        /// <remarks>
        /// Wire format (from spec §4.2.2.9):
        ///   params[0]: Res(bit7) | Source End Point(bits6..0)
        ///   params[1]: Bit Address(bit7) | Destination End Point(bits6..0)
        ///   params[2..N]: Encapsulated command (CC ID + Command ID + parameters)
        /// </remarks>
        public static CommandEncapsulationCommand Create(
            byte sourceEndpoint,
            byte destinationEndpoint,
            bool isBitAddress,
            CommandClassFrame encapsulatedFrame)
        {
            if (sourceEndpoint > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceEndpoint), sourceEndpoint, "Source endpoint must be between 0 and 127.");
            }

            if (destinationEndpoint > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationEndpoint), destinationEndpoint, "Destination endpoint must be between 0 and 127.");
            }

            if (sourceEndpoint == 0 && destinationEndpoint == 0)
            {
                throw new ArgumentException("Source and destination endpoints cannot both be 0.");
            }

            ReadOnlySpan<byte> encapsulatedData = encapsulatedFrame.Data.Span;
            byte[] parameters = new byte[2 + encapsulatedData.Length];
            parameters[0] = (byte)(sourceEndpoint & 0b0111_1111);
            parameters[1] = (byte)((destinationEndpoint & 0b0111_1111) | (isBitAddress ? 0b1000_0000 : 0));
            encapsulatedData.CopyTo(parameters.AsSpan(2));

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new CommandEncapsulationCommand(frame);
        }

        /// <summary>
        /// Parses a Multi Channel Command Encapsulation frame.
        /// </summary>
        public static MultiChannelCommandEncapsulation Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Multi Channel Command Encapsulation frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multi Channel Command Encapsulation frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            byte sourceEndpoint = (byte)(parameters[0] & 0b0111_1111);
            bool isBitAddress = (parameters[1] & 0b1000_0000) != 0;
            byte destination = (byte)(parameters[1] & 0b0111_1111);

            CommandClassFrame encapsulatedFrame = new CommandClassFrame(frame.CommandParameters[2..]);

            return new MultiChannelCommandEncapsulation(sourceEndpoint, isBitAddress, destination, encapsulatedFrame);
        }
    }
}
