using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public enum PowerlevelCommand : byte
{
    /// <summary>
    /// Set the power level indicator value, which should be used by the node when transmitting RF.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request the current power level value.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise the current power level.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Instruct the destination node to transmit a number of test frames to the specified NodeID with the RF
    /// power level specified.
    /// </summary>
    TestNodeSet = 0x04,

    /// <summary>
    /// Request the result of the latest Powerlevel Test
    /// </summary>
    TestNodeGet = 0x05,

    /// <summary>
    /// Report the latest result of a test frame transmission started by the Powerlevel Test Node Set Command.
    /// </summary>
    TestNodeReport = 0x06,
}

[CommandClass(CommandClassId.Powerlevel)]
public sealed partial class PowerlevelCommandClass : CommandClass<PowerlevelCommand>
{
    internal PowerlevelCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <inheritdoc />
    public override bool? IsCommandSupported(PowerlevelCommand command)
        => command switch
        {
            PowerlevelCommand.Set => true,
            PowerlevelCommand.Get => true,
            PowerlevelCommand.TestNodeSet => true,
            PowerlevelCommand.TestNodeGet => true,
            _ => false,
        };

    internal override CommandClassCategory Category => CommandClassCategory.Management;

    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((PowerlevelCommand)frame.CommandId)
        {
            case PowerlevelCommand.Report:
            {
                PowerlevelReport report = PowerlevelReportCommand.Parse(frame, Logger);
                LastReport = report;
                OnPowerlevelReportReceived?.Invoke(report);
                break;
            }
            case PowerlevelCommand.TestNodeReport:
            {
                PowerlevelTestResult? result = PowerlevelTestNodeReportCommand.Parse(frame, Logger);
                LastTestResult = result;
                if (result.HasValue)
                {
                    OnTestNodeReportReceived?.Invoke(result.Value);
                }

                break;
            }
        }
    }
}
