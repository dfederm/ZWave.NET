using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Defines RF transmit power levels relative to normal power.
/// </summary>
public enum Powerlevel : byte
{
    /// <summary>
    /// Normal power.
    /// </summary>
    Normal = 0x00,

    /// <summary>
    /// -1 dBm.
    /// </summary>
    Minus1dBm = 0x01,

    /// <summary>
    /// -2 dBm.
    /// </summary>
    Minus2dBm = 0x02,

    /// <summary>
    /// -3 dBm.
    /// </summary>
    Minus3dBm = 0x03,

    /// <summary>
    /// -4 dBm.
    /// </summary>
    Minus4dBm = 0x04,

    /// <summary>
    /// -5 dBm.
    /// </summary>
    Minus5dBm = 0x05,

    /// <summary>
    /// -6 dBm.
    /// </summary>
    Minus6dBm = 0x06,

    /// <summary>
    /// -7 dBm.
    /// </summary>
    Minus7dBm = 0x07,

    /// <summary>
    /// -8 dBm.
    /// </summary>
    Minus8dBm = 0x08,

    /// <summary>
    /// -9 dBm.
    /// </summary>
    Minus9dBm = 0x09,
}

/// <summary>
/// Represents a Powerlevel Report received from a device.
/// </summary>
public readonly record struct PowerlevelReport(
    /// <summary>
    /// The current power level indicator value in effect on the node.
    /// </summary>
    Powerlevel Powerlevel,

    /// <summary>
    /// The time in seconds the node has back at Power level before resetting to normal Power level.
    /// </summary>
    /// <remarks>
    /// May be null when <see cref="Powerlevel"/> is <see cref="CommandClasses.Powerlevel.Normal"/>.
    /// </remarks>
    byte? TimeoutInSeconds);

public sealed partial class PowerlevelCommandClass
{
    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public PowerlevelReport? LastReport { get; private set; }

    /// <summary>
    /// Event raised when a Powerlevel Report is received, both solicited and unsolicited.
    /// </summary>
    public event Action<PowerlevelReport>? OnPowerlevelReportReceived;

    /// <summary>
    /// Set the power level indicator value, which should be used by the node when transmitting RF.
    /// </summary>
    /// <param name="powerlevel">
    /// The power level indicator value, which should be used by the node when transmitting RF.
    /// </param>
    /// <param name="timeoutInSeconds">
    /// The timeout in seconds for this power level indicator value before returning the power level defined by
    /// the application. Must be non-zero unless the power level is <see cref="Powerlevel.Normal"/>.
    /// </param>
    public async Task SetAsync(
        Powerlevel powerlevel,
        byte timeoutInSeconds,
        CancellationToken cancellationToken)
    {
        if (timeoutInSeconds == 0 && powerlevel != Powerlevel.Normal)
        {
            throw new ArgumentException("Timeout must be non-zero", nameof(timeoutInSeconds));
        }

        var command = PowerlevelSetCommand.Create(powerlevel, timeoutInSeconds);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the current power level value.
    /// </summary>
    public async Task<PowerlevelReport> GetAsync(CancellationToken cancellationToken)
    {
        PowerlevelGetCommand command = PowerlevelGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<PowerlevelReportCommand>(cancellationToken).ConfigureAwait(false);
        PowerlevelReport report = PowerlevelReportCommand.Parse(reportFrame, Logger);
        LastReport = report;
        OnPowerlevelReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct PowerlevelSetCommand : ICommand
    {
        public PowerlevelSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.Set;

        public CommandClassFrame Frame { get; }

        public static PowerlevelSetCommand Create(Powerlevel powerlevel, byte timeoutInSeconds)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)powerlevel, timeoutInSeconds];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new PowerlevelSetCommand(frame);
        }
    }

    internal readonly struct PowerlevelGetCommand : ICommand
    {
        public PowerlevelGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.Get;

        public CommandClassFrame Frame { get; }

        public static PowerlevelGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new PowerlevelGetCommand(frame);
        }
    }

    internal readonly struct PowerlevelReportCommand : ICommand
    {
        public PowerlevelReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Powerlevel;

        public static byte CommandId => (byte)PowerlevelCommand.Report;

        public CommandClassFrame Frame { get; }

        public static PowerlevelReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Powerlevel Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Powerlevel Report frame is too short");
            }

            Powerlevel powerlevel = (Powerlevel)frame.CommandParameters.Span[0];
            byte? timeoutInSeconds = powerlevel != Powerlevel.Normal
                ? frame.CommandParameters.Span[1]
                : null;
            return new PowerlevelReport(powerlevel, timeoutInSeconds);
        }
    }
}
