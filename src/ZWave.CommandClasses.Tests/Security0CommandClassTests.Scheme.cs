using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class Security0CommandClassTests
{
    [TestMethod]
    public void SchemeGet_HasCorrectFormat()
    {
        Security0CommandClass.SchemeGetCommand command = Security0CommandClass.SchemeGetCommand.Create();

        Assert.AreEqual((byte)Security0Command.SchemeGet, Security0CommandClass.SchemeGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SchemeReport_CreateParseRoundTrip()
    {
        // Spec §3.5.3.3: the report carries the Supported Security Schemes byte; S0 is 0x00.
        Security0CommandClass.SchemeReportCommand report = Security0CommandClass.SchemeReportCommand.Create();

        Security0SchemeReport parsed = Security0CommandClass.SchemeReportCommand.Parse(report.Frame, NullLogger.Instance);

        Assert.AreEqual((byte)0x00, parsed.SupportedSecuritySchemes);
    }

    [TestMethod]
    public void SchemeReport_Parse_Malformed_Throws()
    {
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId.Security0, (byte)Security0Command.SchemeReport, [1, 2]);

        Assert.Throws<ZWaveException>(() => Security0CommandClass.SchemeReportCommand.Parse(frame, NullLogger.Instance));
    }
}
