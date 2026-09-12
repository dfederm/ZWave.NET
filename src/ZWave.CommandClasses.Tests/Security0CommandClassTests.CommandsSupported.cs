using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class Security0CommandClassTests
{
    [TestMethod]
    public void CommandsSupportedReport_CreateParseRoundTrip()
    {
        CommandClassId[] supported = [CommandClassId.BinarySwitch, CommandClassId.Version];
        CommandClassId[] controlled = [CommandClassId.Meter];
        Security0CommandClass.CommandsSupportedReportCommand report =
            Security0CommandClass.CommandsSupportedReportCommand.Create(supported, controlled);

        (Security0CommandsSupportedReport parsed, byte reportsToFollow) =
            Security0CommandClass.CommandsSupportedReportCommand.Parse(report.Frame, NullLogger.Instance);

        Assert.AreEqual(0, reportsToFollow);
        Assert.HasCount(3, parsed.CommandClasses);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.BinarySwitch, IsSupported: true, IsControlled: false), parsed.CommandClasses[0]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Version, IsSupported: true, IsControlled: false), parsed.CommandClasses[1]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Meter, IsSupported: false, IsControlled: true), parsed.CommandClasses[2]);
    }

    [TestMethod]
    public void CommandsSupportedReport_Parse_ReportsToFollowZero_ParsesClasses()
    {
        // reports-to-follow = 0x00 (single-frame report), then the supported classes, the
        // SupportControlMark (0xEF), then the controlled classes.
        byte[] parameters = [0x00, (byte)CommandClassId.BinarySwitch, (byte)CommandClassId.Version, (byte)CommandClassId.SupportControlMark, (byte)CommandClassId.Meter];
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.CommandsSupportedReport, parameters);

        (Security0CommandsSupportedReport parsed, byte reportsToFollow) =
            Security0CommandClass.CommandsSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(0, reportsToFollow);
        Assert.HasCount(3, parsed.CommandClasses);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.BinarySwitch, IsSupported: true, IsControlled: false), parsed.CommandClasses[0]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Version, IsSupported: true, IsControlled: false), parsed.CommandClasses[1]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Meter, IsSupported: false, IsControlled: true), parsed.CommandClasses[2]);
    }

    [TestMethod]
    public void CommandsSupportedReport_Parse_NoMark_AllSupported()
    {
        // No SupportControlMark present: every class is supported.
        byte[] parameters = [0x00, (byte)CommandClassId.BinarySwitch, (byte)CommandClassId.Version];
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.CommandsSupportedReport, parameters);

        (Security0CommandsSupportedReport parsed, byte reportsToFollow) =
            Security0CommandClass.CommandsSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(0, reportsToFollow);
        Assert.HasCount(2, parsed.CommandClasses);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.BinarySwitch, IsSupported: true, IsControlled: false), parsed.CommandClasses[0]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Version, IsSupported: true, IsControlled: false), parsed.CommandClasses[1]);
    }

    [TestMethod]
    public void CommandsSupportedReport_Parse_ReportsToFollowNonZero_SurfacesCount()
    {
        // reports-to-follow = 0x02 (two more frames are coming), then this frame's classes.
        byte[] parameters = [0x02, (byte)CommandClassId.BinarySwitch, (byte)CommandClassId.Version];
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.CommandsSupportedReport, parameters);

        (Security0CommandsSupportedReport parsed, byte reportsToFollow) =
            Security0CommandClass.CommandsSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(0x02, reportsToFollow);
        Assert.HasCount(2, parsed.CommandClasses);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.BinarySwitch, IsSupported: true, IsControlled: false), parsed.CommandClasses[0]);
        Assert.AreEqual(new CommandClassInfo(CommandClassId.Version, IsSupported: true, IsControlled: false), parsed.CommandClasses[1]);
    }

    [TestMethod]
    public void CommandsSupportedReport_Parse_TruncatedExtended_Throws()
    {
        // reports-to-follow (0x00) then a 0xF1 extended MSB with no following LSB byte.
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.CommandsSupportedReport, [0x00, 0xF1]);

        Assert.Throws<ZWaveException>(() =>
            Security0CommandClass.CommandsSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void CommandsSupportedReport_Parse_Empty_Throws()
    {
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.CommandsSupportedReport);

        Assert.Throws<ZWaveException>(() =>
            Security0CommandClass.CommandsSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }
}
