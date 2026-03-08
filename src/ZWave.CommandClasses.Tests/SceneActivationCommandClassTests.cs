using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

[TestClass]
public class SceneActivationCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_HasCorrectFormat()
    {
        SceneActivationCommandClass.SceneActivationSetCommand command =
            SceneActivationCommandClass.SceneActivationSetCommand.Create(0x05, TimeSpan.FromSeconds(10));

        Assert.AreEqual(CommandClassId.SceneActivation, SceneActivationCommandClass.SceneActivationSetCommand.CommandClassId);
        Assert.AreEqual((byte)SceneActivationCommand.Set, SceneActivationCommandClass.SceneActivationSetCommand.CommandId);
        Assert.AreEqual(4, command.Frame.Data.Length); // CC + Cmd + SceneId + DimmingDuration
        Assert.AreEqual((byte)0x05, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0x0A, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Create_WithConfiguredDuration()
    {
        // null = use duration configured by Scene Actuator Configuration Set (wire value 0xFF)
        SceneActivationCommandClass.SceneActivationSetCommand command =
            SceneActivationCommandClass.SceneActivationSetCommand.Create(0x01, null);

        Assert.AreEqual((byte)0x01, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual((byte)0xFF, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void SetCommand_Parse_ValidFrame()
    {
        // CC=0x2B, Cmd=0x01, SceneId=10, DimmingDuration=0x05 (5 seconds)
        byte[] data = [0x2B, 0x01, 0x0A, 0x05];
        CommandClassFrame frame = new(data);

        SceneActivation activation =
            SceneActivationCommandClass.SceneActivationSetCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)10, activation.SceneId);
        Assert.IsNotNull(activation.DimmingDuration);
        Assert.AreEqual(TimeSpan.FromSeconds(5), activation.DimmingDuration.Value);
    }

    [TestMethod]
    public void SetCommand_Parse_InstantDuration()
    {
        // CC=0x2B, Cmd=0x01, SceneId=1, DimmingDuration=0x00 (Instantly)
        byte[] data = [0x2B, 0x01, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        SceneActivation activation =
            SceneActivationCommandClass.SceneActivationSetCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)1, activation.SceneId);
        Assert.IsNotNull(activation.DimmingDuration);
        Assert.AreEqual(TimeSpan.Zero, activation.DimmingDuration.Value);
    }

    [TestMethod]
    public void SetCommand_Parse_ConfiguredDuration()
    {
        // CC=0x2B, Cmd=0x01, SceneId=255, DimmingDuration=0xFF (use configured duration)
        byte[] data = [0x2B, 0x01, 0xFF, 0xFF];
        CommandClassFrame frame = new(data);

        SceneActivation activation =
            SceneActivationCommandClass.SceneActivationSetCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)255, activation.SceneId);
        Assert.IsNull(activation.DimmingDuration);
    }

    [TestMethod]
    public void SetCommand_Parse_MinuteDuration()
    {
        // CC=0x2B, Cmd=0x01, SceneId=50, DimmingDuration=0x80 (1 minute)
        byte[] data = [0x2B, 0x01, 0x32, 0x80];
        CommandClassFrame frame = new(data);

        SceneActivation activation =
            SceneActivationCommandClass.SceneActivationSetCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual((byte)50, activation.SceneId);
        Assert.IsNotNull(activation.DimmingDuration);
        Assert.AreEqual(TimeSpan.FromMinutes(1), activation.DimmingDuration.Value);
    }

    [TestMethod]
    public void SetCommand_Parse_MaxMinuteDuration()
    {
        // CC=0x2B, Cmd=0x01, SceneId=1, DimmingDuration=0xFE (127 minutes)
        byte[] data = [0x2B, 0x01, 0x01, 0xFE];
        CommandClassFrame frame = new(data);

        SceneActivation activation =
            SceneActivationCommandClass.SceneActivationSetCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromMinutes(127), activation.DimmingDuration!.Value);
    }

    [TestMethod]
    public void SetCommand_Parse_TooShort_Throws()
    {
        // CC=0x2B, Cmd=0x01, only 1 parameter byte (need 2)
        byte[] data = [0x2B, 0x01, 0x0A];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => SceneActivationCommandClass.SceneActivationSetCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SetCommand_Create_RoundTrips()
    {
        SceneActivationCommandClass.SceneActivationSetCommand command =
            SceneActivationCommandClass.SceneActivationSetCommand.Create(0x0A, TimeSpan.FromSeconds(5));

        SceneActivation activation =
            SceneActivationCommandClass.SceneActivationSetCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x0A, activation.SceneId);
        Assert.AreEqual(TimeSpan.FromSeconds(5), activation.DimmingDuration!.Value);
    }
}
