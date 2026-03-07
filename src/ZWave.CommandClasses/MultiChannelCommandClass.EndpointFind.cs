using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class MultiChannelCommandClass
{
    /// <summary>
    /// Finds End Points matching a specific Generic and/or Specific Device Class.
    /// </summary>
    /// <remarks>
    /// Use <paramref name="genericDeviceClass"/> = 0xFF to match all Generic Device Classes.
    /// If <paramref name="genericDeviceClass"/> is 0xFF, <paramref name="specificDeviceClass"/> must also be 0xFF.
    /// Use <paramref name="specificDeviceClass"/> = 0xFF to match all Specific Device Classes within the given Generic Device Class.
    /// </remarks>
    /// <returns>The list of matching endpoint indices.</returns>
    public async Task<IReadOnlyList<byte>> FindEndpointsAsync(byte genericDeviceClass, byte specificDeviceClass, CancellationToken cancellationToken)
    {
        EndpointFindCommand command = EndpointFindCommand.Create(genericDeviceClass, specificDeviceClass);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        // Per spec §4.2.2.8: the report echoes back the requested Generic/Specific Device Class.
        // Use a predicate to match only reports for our request.
        Predicate<CommandClassFrame> predicate = frame =>
            frame.CommandParameters.Length >= 3
            && frame.CommandParameters.Span[1] == genericDeviceClass
            && frame.CommandParameters.Span[2] == specificDeviceClass;

        List<byte> endpointIndices = [];
        int reportsToFollow = 1;
        while (reportsToFollow > 0)
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<EndpointFindReportCommand>(predicate, cancellationToken).ConfigureAwait(false);
            reportsToFollow = EndpointFindReportCommand.Parse(reportFrame, endpointIndices, Logger);
        }

        return endpointIndices;
    }

    internal readonly struct EndpointFindCommand : ICommand
    {
        public EndpointFindCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.EndpointFind;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Creates an Endpoint Find command.
        /// </summary>
        /// <remarks>
        /// Per spec §4.2.2.7:
        ///   params[0]: Generic Device Class (0xFF = all)
        ///   params[1]: Specific Device Class (0xFF = all)
        /// </remarks>
        public static EndpointFindCommand Create(byte genericDeviceClass, byte specificDeviceClass)
        {
            if (genericDeviceClass == 0xFF && specificDeviceClass != 0xFF)
            {
                throw new ArgumentException("When Generic Device Class is 0xFF, Specific Device Class must also be 0xFF.", nameof(specificDeviceClass));
            }

            ReadOnlySpan<byte> parameters = [genericDeviceClass, specificDeviceClass];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, parameters);
            return new EndpointFindCommand(frame);
        }
    }

    internal readonly struct EndpointFindReportCommand : ICommand
    {
        public EndpointFindReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultiChannel;

        public static byte CommandId => (byte)MultiChannelCommand.EndpointFindReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Parses a Multi Channel End Point Find Report frame, appending discovered endpoints to
        /// <paramref name="endpointIndices"/>.
        /// </summary>
        /// <remarks>
        /// Wire format per spec §4.2.2.8:
        ///   params[0]: Reports to Follow
        ///   params[1]: Generic Device Class (echo of request)
        ///   params[2]: Specific Device Class (echo of request)
        ///   params[3..N]: End Point list (each byte: bit7=Reserved, bits6..0=End Point)
        /// </remarks>
        /// <returns>The number of reports still to follow.</returns>
        public static byte Parse(
            CommandClassFrame frame,
            List<byte> endpointIndices,
            ILogger logger)
        {
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning("Multi Channel End Point Find Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multi Channel End Point Find Report frame is too short");
            }

            ReadOnlySpan<byte> parameters = frame.CommandParameters.Span;
            byte reportsToFollow = parameters[0];

            for (int i = 3; i < parameters.Length; i++)
            {
                byte endpointIndex = (byte)(parameters[i] & 0b0111_1111);
                if (endpointIndex != 0)
                {
                    endpointIndices.Add(endpointIndex);
                }
            }

            return reportsToFollow;
        }
    }
}
