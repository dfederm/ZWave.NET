using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class SceneControllerConfigurationCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_HasCorrectFormat()
    {
        SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand command =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand.Create(
                groupId: 0x02, sceneId: 0x05, dimmingDuration: TimeSpan.FromSeconds(10));

        Assert.AreEqual(
            CommandClassId.SceneControllerConfiguration,
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)SceneControllerConfigurationCommand.Set,
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand.CommandId);
        // CC + Cmd + GroupId + SceneId + DimmingDuration = 5 bytes
        Assert.AreEqual(5, command.Frame.Data.Length);
        Assert.AreEqual((byte)0x02, command.Frame.CommandParameters.Span[0]); // GroupId
        Assert.AreEqual((byte)0x05, command.Frame.CommandParameters.Span[1]); // SceneId
        Assert.AreEqual((byte)0x0A, command.Frame.CommandParameters.Span[2]); // DimmingDuration = 10 seconds
    }

    [TestMethod]
    public void SetCommand_Create_DisableScene()
    {
        SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand command =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand.Create(
                groupId: 0x03, sceneId: 0x00, dimmingDuration: TimeSpan.Zero);

        Assert.AreEqual((byte)0x03, command.Frame.CommandParameters.Span[0]); // GroupId
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[1]); // SceneId=0 (disable)
        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[2]); // DimmingDuration
    }

    [TestMethod]
    public void GetCommand_Create_SpecificGroup()
    {
        SceneControllerConfigurationCommandClass.SceneControllerConfigurationGetCommand command =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationGetCommand.Create(0x02);

        Assert.AreEqual(
            CommandClassId.SceneControllerConfiguration,
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationGetCommand.CommandClassId);
        Assert.AreEqual(
            (byte)SceneControllerConfigurationCommand.Get,
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + GroupId
        Assert.AreEqual((byte)0x02, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void GetCommand_Create_CurrentlyActiveGroup()
    {
        SceneControllerConfigurationCommandClass.SceneControllerConfigurationGetCommand command =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationGetCommand.Create(0x00);

        Assert.AreEqual((byte)0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void Report_Parse_ValidFrame()
    {
        // CC=0x2D, Cmd=0x03, GroupId=2, SceneId=5, DimmingDuration=0x0A (10 seconds)
        byte[] data = [0x2D, 0x03, 0x02, 0x05, 0x0A];
        CommandClassFrame frame = new(data);

        SceneControllerConfigurationReport report =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationReportCommand.Parse(
                frame, NullLogger.Instance);

        Assert.AreEqual((byte)2, report.GroupId);
        Assert.AreEqual((byte)5, report.SceneId);
        Assert.AreEqual(TimeSpan.FromSeconds(10), report.DimmingDuration);
    }

    [TestMethod]
    public void Report_Parse_DisabledScene()
    {
        // CC=0x2D, Cmd=0x03, GroupId=3, SceneId=0 (disabled), DimmingDuration=0x00
        byte[] data = [0x2D, 0x03, 0x03, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        SceneControllerConfigurationReport report =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationReportCommand.Parse(
                frame, NullLogger.Instance);

        Assert.AreEqual((byte)3, report.GroupId);
        Assert.AreEqual((byte)0, report.SceneId);
        Assert.AreEqual(TimeSpan.Zero, report.DimmingDuration);
    }

    [TestMethod]
    public void Report_Parse_MaxMinuteDuration()
    {
        // CC=0x2D, Cmd=0x03, GroupId=0xFF, SceneId=0xFF, DimmingDuration=0xFE (127 minutes)
        byte[] data = [0x2D, 0x03, 0xFF, 0xFF, 0xFE];
        CommandClassFrame frame = new(data);

        SceneControllerConfigurationReport report =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationReportCommand.Parse(
                frame, NullLogger.Instance);

        Assert.AreEqual((byte)0xFF, report.GroupId);
        Assert.AreEqual((byte)0xFF, report.SceneId);
        Assert.AreEqual(TimeSpan.FromMinutes(127), report.DimmingDuration);
    }

    [TestMethod]
    public void Report_Parse_TooShort_Throws()
    {
        // CC=0x2D, Cmd=0x03, only 2 parameter bytes (need 3)
        byte[] data = [0x2D, 0x03, 0x02, 0x05];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => SceneControllerConfigurationCommandClass.SceneControllerConfigurationReportCommand.Parse(
                frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SetCommand_Create_RoundTrips()
    {
        SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand command =
            SceneControllerConfigurationCommandClass.SceneControllerConfigurationSetCommand.Create(
                groupId: 0x02, sceneId: 0x0A, dimmingDuration: TimeSpan.FromSeconds(5));

        // Verify the bytes can be read back
        Assert.AreEqual((byte)0x02, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0x0A, command.Frame.CommandParameters.Span[1]);
        Assert.AreEqual((byte)0x05, command.Frame.CommandParameters.Span[2]);
    }
}
