using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class ConfigurationCommandClass
{
    /// <summary>
    /// Request the name of a configuration parameter.
    /// </summary>
    /// <remarks>
    /// The name may span multiple report frames which are automatically aggregated.
    /// </remarks>
    /// <param name="parameterNumber">The parameter number to query (0-65535).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The UTF-8 encoded name of the parameter.</returns>
    public async Task<string> GetNameAsync(ushort parameterNumber, CancellationToken cancellationToken)
    {
        ConfigurationNameGetCommand command = ConfigurationNameGetCommand.Create(parameterNumber);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        List<byte> nameBytes = [];
        byte reportsToFollow;
        do
        {
            CommandClassFrame reportFrame = await AwaitNextReportAsync<ConfigurationNameReportCommand>(cancellationToken)
                .ConfigureAwait(false);
            reportsToFollow = ConfigurationNameReportCommand.ParseInto(reportFrame, nameBytes, Logger);
        }
        while (reportsToFollow > 0);

        return Encoding.UTF8.GetString(CollectionsMarshal.AsSpan(nameBytes));
    }

    internal readonly struct ConfigurationNameGetCommand : ICommand
    {
        public ConfigurationNameGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.NameGet;

        public CommandClassFrame Frame { get; }

        public static ConfigurationNameGetCommand Create(ushort parameterNumber)
        {
            Span<byte> commandParameters = stackalloc byte[2];
            parameterNumber.WriteBytesBE(commandParameters);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationNameGetCommand(frame);
        }
    }

    internal readonly struct ConfigurationNameReportCommand : ICommand
    {
        public ConfigurationNameReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Configuration;

        public static byte CommandId => (byte)ConfigurationCommand.NameReport;

        public CommandClassFrame Frame { get; }

        public static ConfigurationNameReportCommand Create(
            ushort parameterNumber,
            byte reportsToFollow,
            ReadOnlySpan<byte> nameBytes)
        {
            Span<byte> commandParameters = stackalloc byte[3 + nameBytes.Length];
            parameterNumber.WriteBytesBE(commandParameters);
            commandParameters[2] = reportsToFollow;
            nameBytes.CopyTo(commandParameters[3..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ConfigurationNameReportCommand(frame);
        }

        /// <summary>
        /// Parses a Name Report frame and appends the name bytes to the provided list.
        /// </summary>
        /// <returns>The number of reports still to follow.</returns>
        public static byte ParseInto(CommandClassFrame frame, List<byte> nameBytes, ILogger logger)
            => ParseTextReportInto(frame, nameBytes, logger, "Name");
    }
}
