using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class BatteryCommandClassTests
{
    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        BatteryCommandClass.BatteryGetCommand command = BatteryCommandClass.BatteryGetCommand.Create();

        Assert.AreEqual(CommandClassId.Battery, BatteryCommandClass.BatteryGetCommand.CommandClassId);
        Assert.AreEqual((byte)BatteryCommand.Get, BatteryCommandClass.BatteryGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_Version1_BatteryLevelOnly()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=85
        byte[] data = [0x80, 0x03, 85];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)85, report.BatteryLevel.Value);
        Assert.AreEqual(85, report.BatteryLevel.Level);
        Assert.IsFalse(report.BatteryLevel.IsLow);
        Assert.IsNull(report.ChargingStatus);
        Assert.IsNull(report.IsRechargeable);
        Assert.IsNull(report.IsBackupBattery);
        Assert.IsNull(report.IsOverheating);
        Assert.IsNull(report.HasLowFluid);
        Assert.IsNull(report.ReplaceRechargeStatus);
        Assert.IsNull(report.IsLowTemperature);
        Assert.IsNull(report.IsDisconnected);
    }

    [TestMethod]
    public void Report_Parse_Version1_LowBatteryWarning()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=0xFF (low battery)
        byte[] data = [0x80, 0x03, 0xFF];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFF, report.BatteryLevel.Value);
        Assert.AreEqual(0, report.BatteryLevel.Level);
        Assert.IsTrue(report.BatteryLevel.IsLow);
    }

    [TestMethod]
    public void Report_Parse_Version1_ZeroPercent()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=0x00
        byte[] data = [0x80, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, report.BatteryLevel.Value);
        Assert.AreEqual(0, report.BatteryLevel.Level);
        Assert.IsFalse(report.BatteryLevel.IsLow);
    }

    [TestMethod]
    public void Report_Parse_Version1_FullBattery()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=0x64 (100%)
        byte[] data = [0x80, 0x03, 0x64];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)100, report.BatteryLevel.Value);
        Assert.AreEqual(100, report.BatteryLevel.Level);
        Assert.IsFalse(report.BatteryLevel.IsLow);
    }

    [TestMethod]
    public void Report_Parse_Version2_AllStatusFlags()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=50,
        // Byte1: ChargingStatus=01 (Charging), Rechargeable=1, Backup=1, Overheating=1, LowFluid=1, Replace=11 (Now)
        //   = 0b01_1_1_1_1_11 = 0x7F
        // Byte2: Disconnected=1
        //   = 0b0000_0001 = 0x01
        byte[] data = [0x80, 0x03, 50, 0x7F, 0x01];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)50, report.BatteryLevel.Value);
        Assert.AreEqual(BatteryChargingStatus.Charging, report.ChargingStatus);
        Assert.IsTrue(report.IsRechargeable);
        Assert.IsTrue(report.IsBackupBattery);
        Assert.IsTrue(report.IsOverheating);
        Assert.IsTrue(report.HasLowFluid);
        Assert.AreEqual(BatteryRechargeOrReplaceStatus.Now, report.ReplaceRechargeStatus);
        Assert.IsTrue(report.IsDisconnected);
    }

    [TestMethod]
    public void Report_Parse_Version2_Discharging_NotRechargeable()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=75,
        // Byte1: ChargingStatus=00 (Discharging), Rechargeable=0, Backup=0, Overheating=0, LowFluid=0, Replace=00 (Ok)
        //   = 0b00_0_0_0_0_00 = 0x00
        // Byte2: Disconnected=0
        //   = 0x00
        byte[] data = [0x80, 0x03, 75, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BatteryChargingStatus.Discharging, report.ChargingStatus);
        Assert.IsFalse(report.IsRechargeable);
        Assert.IsFalse(report.IsBackupBattery);
        Assert.IsFalse(report.IsOverheating);
        Assert.IsFalse(report.HasLowFluid);
        Assert.AreEqual(BatteryRechargeOrReplaceStatus.Ok, report.ReplaceRechargeStatus);
        Assert.IsFalse(report.IsDisconnected);
    }

    [TestMethod]
    public void Report_Parse_Version2_Maintaining_ReplaceSoon()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=30,
        // Byte1: ChargingStatus=10 (Maintaining), Rechargeable=1, Backup=0, Overheating=0, LowFluid=0, Replace=01 (Soon)
        //   = 0b10_1_0_0_0_01 = 0xA1
        // Byte2: 0x00
        byte[] data = [0x80, 0x03, 30, 0xA1, 0x00];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(BatteryChargingStatus.Maintaining, report.ChargingStatus);
        Assert.IsTrue(report.IsRechargeable);
        Assert.IsFalse(report.IsBackupBattery);
        Assert.AreEqual(BatteryRechargeOrReplaceStatus.Soon, report.ReplaceRechargeStatus);
    }

    [TestMethod]
    public void Report_Parse_Version3_LowTemperature()
    {
        // CC=0x80, Cmd=0x03, BatteryLevel=60,
        // Byte1: ChargingStatus=01, Rechargeable=1, rest 0
        //   = 0b01_1_0_0_0_00 = 0x60
        // Byte2: LowTemperature=1, Disconnected=0
        //   = 0b0000_0010 = 0x02
        byte[] data = [0x80, 0x03, 60, 0x60, 0x02];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.IsLowTemperature);
        Assert.IsFalse(report.IsDisconnected);
    }

    [TestMethod]
    public void Report_Parse_Version3_LowTemperatureAndDisconnected()
    {
        // Byte2: LowTemperature=1, Disconnected=1
        //   = 0b0000_0011 = 0x03
        byte[] data = [0x80, 0x03, 0, 0x00, 0x03];
        CommandClassFrame frame = new(data);

        BatteryReport report = BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.IsLowTemperature);
        Assert.IsTrue(report.IsDisconnected);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x80, Cmd=0x03, no parameters
        byte[] data = [0x80, 0x03];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => BatteryCommandClass.BatteryReportCommand.Parse(frame, NullLogger.Instance));
    }
}
