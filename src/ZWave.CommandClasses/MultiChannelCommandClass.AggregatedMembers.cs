using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class MultiChannelCommandClass
{
    /// <summary>
    /// Queries the members of an Aggregated End Point (version 4).
    /// </summary>
    /// <returns>The list of individual endpoint indices that are members of the aggregated endpoint.</returns>
    public async Task<IReadOnlyList<byte>> GetAggregatedMembersAsync(byte aggregatedEndpointIndex, CancellationToken cancellationToken)
    {
        AggregatedMembersGetCommand command = AggregatedMembersGetCommand.Create(aggregatedEndpointIndex);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        CommandClassFrame reportFrame = await AwaitNextReportAsync<AggregatedMembersReportCommand>(
            frame => AggregatedMembersReportCommand.GetAggregatedEndpointIndex(frame) == aggregatedEndpointIndex,
            cancellationToken).ConfigureAwait(false);
        return AggregatedMembersReportCommand.Parse(reportFrame, Logger);
    }

    internal readonly struct AggregatedMembersGetCommand : ICommand
    {
        public AggregatedMembersGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.AggregatedMembersGet;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Creates an Aggregated Members Get command.
        /// </summary>
        /// <remarks>
        /// Wire format (from spec §4.2.3.6):
        ///   params[0]: Res(bit7) | Aggregated End Point(bits6..0)
        /// </remarks>
        public static AggregatedMembersGetCommand Create(byte aggregatedEndpointIndex)
        {
            if (aggregatedEndpointIndex is 0 or > 127)
            {
                throw new ArgumentOutOfRangeException(nameof(aggregatedEndpointIndex), aggregatedEndpointIndex, "Aggregated endpoint index must be between 1 and 127.");
            }

            ReadOnlySpan<byte> parameters = [aggregatedEndpointIndex];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new AggregatedMembersGetCommand(frame);
        }
    }

    internal readonly struct AggregatedMembersReportCommand : ICommand
    {
        public AggregatedMembersReportCommand(CommandClassFrame frame) => Frame = frame;

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.AggregatedMembersReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Gets the Aggregated End Point index from a report frame without fully parsing it.
        /// </summary>
        public static byte GetAggregatedEndpointIndex(CommandClassFrame frame)
            => (byte)(frame.CommandParameters.Span[0] & 0b0111_1111);

        /// <summary>
        /// Parses a Multi Channel Aggregated Members Report frame.
        /// </summary>
        /// <remarks>
        /// Wire format (from spec §4.2.3.7):
        ///   params[0]: Res(bit7) | Aggregated End Point(bits6..0)
        ///   params[1]: Number of Bit Masks
        ///   params[2..N]: Aggregated Members Bit Mask
        /// </remarks>
        /// <returns>The list of individual endpoint indices that are members of the aggregated endpoint.</returns>
        public static IReadOnlyList<byte> Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Multi Channel Aggregated Members Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Multi Channel Aggregated Members Report frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            byte numberOfBitMasks = parameters[1];

            List<byte> memberEndpointIndices = new List<byte>();
            if (numberOfBitMasks > 0 && parameters.Length >= 2 + numberOfBitMasks)
            {
                for (int byteNum = 0; byteNum < numberOfBitMasks; byteNum++)
                {
                    byte mask = parameters[2 + byteNum];
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((mask & (1 << bitNum)) != 0)
                        {
                            // Bit 0 of byte 0 = EP1, bit 1 = EP2, etc.
                            byte endpointIndex = (byte)(byteNum * 8 + bitNum + 1);
                            memberEndpointIndices.Add(endpointIndex);
                        }
                    }
                }
            }

            return memberEndpointIndices;
        }
    }
}
