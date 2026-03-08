using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ProtectionCommandClassTests
{
    [TestMethod]
    public void TimeoutSetCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionTimeoutSetCommand command =
            ProtectionCommandClass.ProtectionTimeoutSetCommand.Create(0x3C);

        Assert.AreEqual(CommandClassId.Protection, ProtectionCommandClass.ProtectionTimeoutSetCommand.CommandClassId);
        Assert.AreEqual((byte)ProtectionCommand.TimeoutSet, ProtectionCommandClass.ProtectionTimeoutSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + Timeout
        Assert.AreEqual(0x3C, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void TimeoutSetCommand_Create_NoTimer()
    {
        ProtectionCommandClass.ProtectionTimeoutSetCommand command =
            ProtectionCommandClass.ProtectionTimeoutSetCommand.Create(0x00);

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void TimeoutSetCommand_Create_Infinite()
    {
        ProtectionCommandClass.ProtectionTimeoutSetCommand command =
            ProtectionCommandClass.ProtectionTimeoutSetCommand.Create(0xFF);

        Assert.AreEqual(0xFF, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void TimeoutGetCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionTimeoutGetCommand command =
            ProtectionCommandClass.ProtectionTimeoutGetCommand.Create();

        Assert.AreEqual(CommandClassId.Protection, ProtectionCommandClass.ProtectionTimeoutGetCommand.CommandClassId);
        Assert.AreEqual((byte)ProtectionCommand.TimeoutGet, ProtectionCommandClass.ProtectionTimeoutGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_NoTimer()
    {
        byte[] data = [0x75, 0x0B, 0x00];
        CommandClassFrame frame = new(data);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.Zero, timeout);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_Seconds()
    {
        // 30 seconds
        byte[] data = [0x75, 0x0B, 0x1E];
        CommandClassFrame frame = new(data);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromSeconds(30), timeout);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_MaxSeconds()
    {
        // 60 seconds = 0x3C
        byte[] data = [0x75, 0x0B, 0x3C];
        CommandClassFrame frame = new(data);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromSeconds(60), timeout);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_Minutes()
    {
        // 2 minutes = 0x41
        byte[] data = [0x75, 0x0B, 0x41];
        CommandClassFrame frame = new(data);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromMinutes(2), timeout);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_MaxMinutes()
    {
        // 191 minutes = 0xFE
        byte[] data = [0x75, 0x0B, 0xFE];
        CommandClassFrame frame = new(data);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromMinutes(191), timeout);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_Infinite()
    {
        byte[] data = [0x75, 0x0B, 0xFF];
        CommandClassFrame frame = new(data);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(System.Threading.Timeout.InfiniteTimeSpan, timeout);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_ReservedValue()
    {
        // 0x3D is in the reserved gap (0x3D-0x40)
        byte[] data = [0x75, 0x0B, 0x3D];
        CommandClassFrame frame = new(data);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsNull(timeout);
    }

    [TestMethod]
    public void TimeoutReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x75, 0x0B];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void TimeoutReportCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionTimeoutReportCommand command =
            ProtectionCommandClass.ProtectionTimeoutReportCommand.Create(0x41);

        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x41, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void TimeoutReportCommand_RoundTrip()
    {
        ProtectionCommandClass.ProtectionTimeoutReportCommand command =
            ProtectionCommandClass.ProtectionTimeoutReportCommand.Create(0x3C);

        TimeSpan? timeout = ProtectionCommandClass.ProtectionTimeoutReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(TimeSpan.FromSeconds(60), timeout);
    }
}
