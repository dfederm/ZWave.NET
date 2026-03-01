using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class VersionCommandClass
{
    /// <summary>
    /// Request the version of a specific command class from a device.
    /// </summary>
    public async Task<byte> GetCommandClassVersionAsync(CommandClassId commandClassId, CancellationToken cancellationToken)
    {
        var command = VersionCommandClassGetCommand.Create(commandClassId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionCommandClassReportCommand>(
            predicate: frame => frame.CommandParameters.Length > 0
                && (CommandClassId)frame.CommandParameters.Span[0] == commandClassId,
            cancellationToken).ConfigureAwait(false);
        (CommandClassId _, byte commandClassVersion) = VersionCommandClassReportCommand.Parse(reportFrame, Logger);
        Endpoint.GetCommandClass(commandClassId).SetVersion(commandClassVersion);
        return commandClassVersion;
    }

    internal readonly struct VersionCommandClassGetCommand : ICommand
    {
        public VersionCommandClassGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.CommandClassGet;

        public CommandClassFrame Frame { get; }

        public static VersionCommandClassGetCommand Create(CommandClassId commandClassId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)commandClassId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new VersionCommandClassGetCommand(frame);
        }
    }

    internal readonly struct VersionCommandClassReportCommand : ICommand
    {
        public VersionCommandClassReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.CommandClassReport;

        public CommandClassFrame Frame { get; }

        public static (CommandClassId RequestedCommandClass, byte CommandClassVersion) Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Version Command Class Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Version Command Class Report frame is too short");
            }

            return ((CommandClassId)frame.CommandParameters.Span[0], frame.CommandParameters.Span[1]);
        }
    }
}
