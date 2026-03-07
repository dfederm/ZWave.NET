using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class MultilevelSwitchCommandClass
{
    /// <summary>
    /// Gets the switch type (V3+).
    /// </summary>
    public MultilevelSwitchType? SwitchType { get; private set; }

    /// <summary>
    /// Request the supported Switch Types of a supporting device (V3+).
    /// </summary>
    public async Task<MultilevelSwitchType> GetSupportedAsync(CancellationToken cancellationToken)
    {
        var command = MultilevelSwitchSupportedGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<MultilevelSwitchSupportedReportCommand>(cancellationToken).ConfigureAwait(false);
        MultilevelSwitchType switchType = MultilevelSwitchSupportedReportCommand.Parse(reportFrame, Logger);
        SwitchType = switchType;
        return switchType;
    }

    internal readonly struct MultilevelSwitchSupportedGetCommand : ICommand
    {
        public MultilevelSwitchSupportedGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.SupportedGet;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchSupportedGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new MultilevelSwitchSupportedGetCommand(frame);
        }
    }

    internal readonly struct MultilevelSwitchSupportedReportCommand : ICommand
    {
        public MultilevelSwitchSupportedReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.MultilevelSwitch;

        public static byte CommandId => (byte)MultilevelSwitchCommand.SupportedReport;

        public CommandClassFrame Frame { get; }

        public static MultilevelSwitchType Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Multilevel Switch Supported Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Multilevel Switch Supported Report frame is too short");
            }

            return (MultilevelSwitchType)(frame.CommandParameters.Span[0] & 0b0001_1111);
        }
    }
}
