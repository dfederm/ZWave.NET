using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class ThermostatSetbackCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_NoOverride_ZeroSetback()
    {
        ThermostatSetbackCommandClass.ThermostatSetbackSetCommand command =
            ThermostatSetbackCommandClass.ThermostatSetbackSetCommand.Create(
                ThermostatSetbackType.NoOverride,
                new ThermostatSetbackState(0));

        Assert.AreEqual(CommandClassId.ThermostatSetback, ThermostatSetbackCommandClass.ThermostatSetbackSetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatSetbackCommand.Set, ThermostatSetbackCommandClass.ThermostatSetbackSetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length);
        // Byte 0: SetbackType=0x00 (NoOverride)
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
        // Byte 1: SetbackState=0x00 (0 degrees)
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_TemporaryOverride_PositiveSetback()
    {
        // +2.0 degrees = raw value 20
        ThermostatSetbackCommandClass.ThermostatSetbackSetCommand command =
            ThermostatSetbackCommandClass.ThermostatSetbackSetCommand.Create(
                ThermostatSetbackType.TemporaryOverride,
                new ThermostatSetbackState(20));

        Assert.AreEqual(4, command.Frame.Data.Length);
        // Byte 0: SetbackType=0x01 (TemporaryOverride)
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
        // Byte 1: SetbackState=20 (0x14)
        Assert.AreEqual(0x14, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_PermanentOverride_NegativeSetback()
    {
        // -1.5 degrees = raw value -15
        ThermostatSetbackCommandClass.ThermostatSetbackSetCommand command =
            ThermostatSetbackCommandClass.ThermostatSetbackSetCommand.Create(
                ThermostatSetbackType.PermanentOverride,
                new ThermostatSetbackState(-15));

        Assert.AreEqual(4, command.Frame.Data.Length);
        // Byte 0: SetbackType=0x02 (PermanentOverride)
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]);
        // Byte 1: SetbackState=-15 as unsigned = 0xF1
        Assert.AreEqual(0xF1, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_FrostProtection()
    {
        ThermostatSetbackCommandClass.ThermostatSetbackSetCommand command =
            ThermostatSetbackCommandClass.ThermostatSetbackSetCommand.Create(
                ThermostatSetbackType.NoOverride,
                ThermostatSetbackState.FrostProtection);

        // Byte 1: 0x79 = 121 (Frost Protection)
        Assert.AreEqual(0x79, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_EnergySavingMode()
    {
        ThermostatSetbackCommandClass.ThermostatSetbackSetCommand command =
            ThermostatSetbackCommandClass.ThermostatSetbackSetCommand.Create(
                ThermostatSetbackType.NoOverride,
                ThermostatSetbackState.EnergySavingMode);

        // Byte 1: 0x7A = 122 (Energy Saving Mode)
        Assert.AreEqual(0x7A, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ThermostatSetbackCommandClass.ThermostatSetbackGetCommand command =
            ThermostatSetbackCommandClass.ThermostatSetbackGetCommand.Create();

        Assert.AreEqual(CommandClassId.ThermostatSetback, ThermostatSetbackCommandClass.ThermostatSetbackGetCommand.CommandClassId);
        Assert.AreEqual((byte)ThermostatSetbackCommand.Get, ThermostatSetbackCommandClass.ThermostatSetbackGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void Report_Parse_NoOverride_ZeroSetback()
    {
        // CC=0x47, Cmd=0x03, Type=0x00 (NoOverride), State=0x00 (0 degrees)
        byte[] data = [0x47, 0x03, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatSetbackType.NoOverride, report.SetbackType);
        Assert.AreEqual(0, report.SetbackState.RawValue);
        Assert.IsTrue(report.SetbackState.IsTemperatureSetback);
        Assert.AreEqual(0m, report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_TemporaryOverride_PositiveSetback()
    {
        // CC=0x47, Cmd=0x03, Type=0x01 (TemporaryOverride), State=0x14 (20 = +2.0 degrees)
        byte[] data = [0x47, 0x03, 0x01, 0x14];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatSetbackType.TemporaryOverride, report.SetbackType);
        Assert.AreEqual(20, report.SetbackState.RawValue);
        Assert.IsTrue(report.SetbackState.IsTemperatureSetback);
        Assert.AreEqual(2.0m, report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_PermanentOverride_NegativeSetback()
    {
        // CC=0x47, Cmd=0x03, Type=0x02 (PermanentOverride), State=0xF1 (-15 = -1.5 degrees)
        byte[] data = [0x47, 0x03, 0x02, 0xF1];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(ThermostatSetbackType.PermanentOverride, report.SetbackType);
        Assert.AreEqual(-15, report.SetbackState.RawValue);
        Assert.IsTrue(report.SetbackState.IsTemperatureSetback);
        Assert.AreEqual(-1.5m, report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_FrostProtection()
    {
        // CC=0x47, Cmd=0x03, Type=0x00, State=0x79 (121 = Frost Protection)
        byte[] data = [0x47, 0x03, 0x00, 0x79];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(121, report.SetbackState.RawValue);
        Assert.IsFalse(report.SetbackState.IsTemperatureSetback);
        Assert.IsNull(report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_EnergySavingMode()
    {
        // CC=0x47, Cmd=0x03, Type=0x00, State=0x7A (122 = Energy Saving Mode)
        byte[] data = [0x47, 0x03, 0x00, 0x7A];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(122, report.SetbackState.RawValue);
        Assert.IsFalse(report.SetbackState.IsTemperatureSetback);
        Assert.IsNull(report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_UnusedState()
    {
        // CC=0x47, Cmd=0x03, Type=0x00, State=0x7F (127 = Unused State)
        byte[] data = [0x47, 0x03, 0x00, 0x7F];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(127, report.SetbackState.RawValue);
        Assert.IsFalse(report.SetbackState.IsTemperatureSetback);
        Assert.IsNull(report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_MaxPositiveSetback()
    {
        // CC=0x47, Cmd=0x03, Type=0x00, State=0x78 (120 = +12.0 degrees, max temperature setback)
        byte[] data = [0x47, 0x03, 0x00, 0x78];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(120, report.SetbackState.RawValue);
        Assert.IsTrue(report.SetbackState.IsTemperatureSetback);
        Assert.AreEqual(12.0m, report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_MaxNegativeSetback()
    {
        // CC=0x47, Cmd=0x03, Type=0x00, State=0x80 (-128 = -12.8 degrees, max negative setback)
        byte[] data = [0x47, 0x03, 0x00, 0x80];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(-128, report.SetbackState.RawValue);
        Assert.IsTrue(report.SetbackState.IsTemperatureSetback);
        Assert.AreEqual(-12.8m, report.SetbackState.TemperatureSetbackKelvin);
    }

    [TestMethod]
    public void Report_Parse_ReservedSetbackType_Preserved()
    {
        // CC=0x47, Cmd=0x03, Type=0x03 (reserved), State=0x00
        byte[] data = [0x47, 0x03, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((ThermostatSetbackType)0x03, report.SetbackType);
    }

    [TestMethod]
    public void Report_Parse_ReservedBitsInType_Ignored()
    {
        // CC=0x47, Cmd=0x03, Type byte=0xFD (upper 6 bits set, lower 2 = 0x01 TemporaryOverride), State=0x00
        byte[] data = [0x47, 0x03, 0xFD, 0x00];
        CommandClassFrame frame = new(data);

        ThermostatSetbackReport report =
            ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance);

        // Only lower 2 bits extracted for setback type
        Assert.AreEqual(ThermostatSetbackType.TemporaryOverride, report.SetbackType);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x47, Cmd=0x03, only 1 parameter byte (need 2)
        byte[] data = [0x47, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => ThermostatSetbackCommandClass.ThermostatSetbackReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SetbackState_SpecialValues_AreCorrect()
    {
        Assert.AreEqual(121, ThermostatSetbackState.FrostProtection.RawValue);
        Assert.AreEqual(122, ThermostatSetbackState.EnergySavingMode.RawValue);
        Assert.AreEqual(127, ThermostatSetbackState.UnusedState.RawValue);

        Assert.IsFalse(ThermostatSetbackState.FrostProtection.IsTemperatureSetback);
        Assert.IsFalse(ThermostatSetbackState.EnergySavingMode.IsTemperatureSetback);
        Assert.IsFalse(ThermostatSetbackState.UnusedState.IsTemperatureSetback);
    }
}
