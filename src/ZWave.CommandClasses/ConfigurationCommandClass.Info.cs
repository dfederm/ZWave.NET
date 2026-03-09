using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class ConfigurationCommandClass
{
    /// <summary>
    /// Request usage information for a configuration parameter.
    /// </summary>
    /// <remarks>
    /// The info text may span multiple report frames which are automatically aggregated.
    /// </remarks>
    /// <param name="parameterNumber">The parameter number to query (0-65535).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The UTF-8 encoded info text of the parameter.</returns>
    public async Task<string> GetInfoAsync(ushort parameterNumber, CancellationToken cancellationToken)
    {
        ConfigurationInfoGetCommand command = ConfigurationInfoGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        List<byte> infoBytes = [];
        byte reportsToFollow;
        do
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<ConfigurationInfoReportCommand>(cancellationToken)
                .ConfigureAwait(false);
            reportsToFollow = ConfigurationInfoReportCommand.ParseInto(reportFrame, infoBytes, Logger);
        }
        while (reportsToFollow > 0);

        return Encoding.UTF8.GetString(CollectionsMarshal.AsSpan(infoBytes));
    }

    internal readonly struct ConfigurationInfoGetCommand : ICommand
    {
        public ConfigurationInfoGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.InfoGet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationInfoGetCommand Create(ushort parameterNumber)
        {
            Span<byte> commandParameters = stackalloc byte[2];
            parameterNumber.WriteBytesBE(commandParameters);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationInfoGetCommand(frame);
        }
    }

    internal readonly struct ConfigurationInfoReportCommand : ICommand
    {
        public ConfigurationInfoReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.InfoReport;

        public CommandClassFrame Frame { get; }

        public static ConfigurationInfoReportCommand Create(
            ushort parameterNumber,
            byte reportsToFollow,
            ReadOnlySpan<byte> infoBytes)
        {
            Span<byte> commandParameters = stackalloc byte[3 + infoBytes.Length];
            parameterNumber.WriteBytesBE(commandParameters);
            commandParameters[2] = reportsToFollow;
            infoBytes.CopyTo(commandParameters[3..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationInfoReportCommand(frame);
        }

        /// <summary>
        /// Parses an Info Report frame and appends the info bytes to the provided list.
        /// </summary>
        /// <returns>The number of reports still to follow.</returns>
        public static byte ParseInto(CommandClassFrame frame, List<byte> infoBytes, ILogger logger)
            => ParseTextReportInto(frame, infoBytes, logger, "Info");
    }
}
