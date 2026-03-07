using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class TimeCommandClass
{
    /// <summary>
    /// Event raised when a Date Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<DateOnly>? OnDateReportReceived;

    /// <summary>
    /// Gets the last date report received from the device.
    /// </summary>
    public DateOnly? LastDateReport { get; private set; }

    /// <summary>
    /// Request the current date from a supporting device.
    /// </summary>
    public async Task<DateOnly> GetDateAsync(CancellationToken cancellationToken)
    {
        DateGetCommand command = DateGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<DateReportCommand>(cancellationToken).ConfigureAwait(false);
        DateOnly date = DateReportCommand.Parse(reportFrame, Logger);
        LastDateReport = date;
        OnDateReportReceived?.Invoke(date);
        return date;
    }

    /// <summary>
    /// Set the current date at a supporting device.
    /// </summary>
    public async Task SetDateAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var command = DateSetCommand.Create(date);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct DateGetCommand : ICommand
    {
        public DateGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.DateGet;

        public CommandClassFrame Frame { get; }

        public static DateGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new DateGetCommand(frame);
        }
    }

    internal readonly struct DateSetCommand : ICommand
    {
        public DateSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.DateSet;

        public CommandClassFrame Frame { get; }

        public static DateSetCommand Create(DateOnly date)
        {
            Span<byte> commandParameters = stackalloc byte[4];
            ((ushort)date.Year).WriteBytesBE(commandParameters[0..2]);
            commandParameters[2] = (byte)date.Month;
            commandParameters[3] = (byte)date.Day;

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new DateSetCommand(frame);
        }
    }

    internal readonly struct DateReportCommand : ICommand
    {
        public DateReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Time;

        public static byte CommandId => (byte)TimeCommand.DateReport;

        public CommandClassFrame Frame { get; }

        public static DateOnly Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Date Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Date Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            int year = span[0..2].ToUInt16BE();
            int month = span[2];
            int day = span[3];

            try
            {
                return new DateOnly(year, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                logger.LogWarning("Date Report has invalid date values (year={Year}, month={Month}, day={Day})", year, month, day);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Date Report has invalid date values");
                return default;
            }
        }
    }
}
