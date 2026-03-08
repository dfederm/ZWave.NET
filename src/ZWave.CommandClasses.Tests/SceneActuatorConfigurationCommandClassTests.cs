using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class SceneActuatorConfigurationCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_HasCorrectFormat()
    {
        SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationSetCommand command =
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationSetCommand.Create(
                sceneId: 0x05, dimmingDuration: TimeSpan.FromSeconds(10), overrideLevel: true, level: 0x63);

        Assert.AreEqual(
            CommandClassId.SceneActuatorConfiguration,
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationSetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)SceneActuatorConfigurationCommand.Set,
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationSetCommand.CommandId);
        // CC + Cmd + SceneId + DimmingDuration + Override|Reserved + Level = 6 bytes
        Assert.AreEqual(6, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x05, command.Frame.CommandParameters.Span[0]); // SceneId
        Assert.AreEqual((byte)0x0A, command.Frame.CommandParameters.Span[1]); // DimmingDuration = 10 seconds
        Assert.AreEqual((byte)0x80, command.Frame.CommandParameters.Span[2]); // Override=1, Reserved=0
        Assert.AreEqual((byte)0x63, command.Frame.CommandParameters.Span[3]); // Level
    }

    [TestMethod]
    public void SetCommand_Create_OverrideFalse_UsesCurrentSettings()
    {
        SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationSetCommand command =
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationSetCommand.Create(
                sceneId: 0x01, dimmingDuration: TimeSpan.Zero, overrideLevel: false, level: 0xFF);

        Assert.AreEqual((byte)0x01, command.Frame.CommandParameters.Span[0]); // SceneId
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[1]); // DimmingDuration = instantly
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[2]); // Override=0
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[3]); // Level (ignored by receiver)
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationGetCommand command =
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationGetCommand.Create(0x05);

        Assert.AreEqual(
            CommandClassId.SceneActuatorConfiguration,
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)SceneActuatorConfigurationCommand.Get,
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + SceneId
        Assert.AreEqual((byte)0x05, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_CurrentActiveScene()
    {
        SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationGetCommand command =
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationGetCommand.Create(0x00);

        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void Report_Parse_ValidFrame()
    {
        // CC=0x2C, Cmd=0x03, SceneId=5, Level=0x63, DimmingDuration=0x0A (10 seconds)
        byte[] data = [0x2C, 0x03, 0x05, 0x63, 0x0A];
        CommandClassFrame frame = new(data);

        SceneActuatorConfigurationReport report =
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationReportCommand.Parse(
                frame, NullLogger.Instance);

        Assert.AreEqual((byte)5, report.SceneId);
        Assert.AreEqual((byte)0x63, report.Level);
        Assert.AreEqual(TimeSpan.FromSeconds(10), report.DimmingDuration);
    }

    [TestMethod]
    public void Report_Parse_NoActiveScene()
    {
        // CC=0x2C, Cmd=0x03, SceneId=0 (no active scene), Level=0x00, DimmingDuration=0x00
        byte[] data = [0x2C, 0x03, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        SceneActuatorConfigurationReport report =
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationReportCommand.Parse(
                frame, NullLogger.Instance);

        Assert.AreEqual((byte)0, report.SceneId);
        Assert.AreEqual((byte)0x00, report.Level);
        Assert.AreEqual(TimeSpan.Zero, report.DimmingDuration);
    }

    [TestMethod]
    public void Report_Parse_MaxMinuteDuration()
    {
        // CC=0x2C, Cmd=0x03, SceneId=10, Level=0xFF, DimmingDuration=0xFE (127 minutes)
        byte[] data = [0x2C, 0x03, 0x0A, 0xFF, 0xFE];
        CommandClassFrame frame = new(data);

        SceneActuatorConfigurationReport report =
            SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationReportCommand.Parse(
                frame, NullLogger.Instance);

        Assert.AreEqual((byte)10, report.SceneId);
        Assert.AreEqual((byte)0xFF, report.Level);
        Assert.AreEqual(TimeSpan.FromMinutes(127), report.DimmingDuration);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x2C, Cmd=0x03, only 2 parameter bytes (need 3)
        byte[] data = [0x2C, 0x03, 0x05, 0x63];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => SceneActuatorConfigurationCommandClass.SceneActuatorConfigurationReportCommand.Parse(
                frame, NullLogger.Instance));
    }
}
