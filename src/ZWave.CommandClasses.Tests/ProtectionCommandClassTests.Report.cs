using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ProtectionCommandClassTests
{
    [TestMethod]
    public void SetCommand_Create_V1_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionSetCommand command =
            ProtectionCommandClass.ProtectionSetCommand.Create(1, LocalProtectionState.ProtectionBySequence, RfProtectionState.Unprotected);

        Assert.AreEqual(CommandClassId.Protection, ProtectionCommandClass.ProtectionSetCommand.CommandClassId);
        Assert.AreEqual((byte)ProtectionCommand.Set, ProtectionCommandClass.ProtectionSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + 1 param
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_V1_Unprotected()
    {
        ProtectionCommandClass.ProtectionSetCommand command =
            ProtectionCommandClass.ProtectionSetCommand.Create(1, LocalProtectionState.Unprotected, RfProtectionState.NoRfControl);

        // V1 ignores RF state, sends only 1 byte
        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_V1_NoOperationPossible()
    {
        ProtectionCommandClass.ProtectionSetCommand command =
            ProtectionCommandClass.ProtectionSetCommand.Create(1, LocalProtectionState.NoOperationPossible, RfProtectionState.Unprotected);

        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void SetCommand_Create_V2_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionSetCommand command =
            ProtectionCommandClass.ProtectionSetCommand.Create(2, LocalProtectionState.NoOperationPossible, RfProtectionState.NoRfControl);

        Assert.AreEqual(4, command.Frame.Data.Length); // CC + Cmd + 2 params
        Assert.AreEqual(2, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]); // Local: NoOperationPossible
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[1]); // RF: NoRfControl
    }

    [TestMethod]
    public void SetCommand_Create_V2_NoRfResponse()
    {
        ProtectionCommandClass.ProtectionSetCommand command =
            ProtectionCommandClass.ProtectionSetCommand.Create(2, LocalProtectionState.Unprotected, RfProtectionState.NoRfResponse);

        Assert.AreEqual(0x00, command.Frame.CommandParameters.Span[0]); // Local: Unprotected
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[1]); // RF: NoRfResponse
    }

    [TestMethod]
    public void GetCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionGetCommand command =
            ProtectionCommandClass.ProtectionGetCommand.Create();

        Assert.AreEqual(CommandClassId.Protection, ProtectionCommandClass.ProtectionGetCommand.CommandClassId);
        Assert.AreEqual((byte)ProtectionCommand.Get, ProtectionCommandClass.ProtectionGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length); // CC + Cmd only
    }

    [TestMethod]
    public void ReportCommand_Parse_V1_Unprotected()
    {
        byte[] data = [0x75, 0x03, 0x00];
        CommandClassFrame frame = new(data);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.Unprotected, report.LocalProtection);
        Assert.IsNull(report.RfProtection);
    }

    [TestMethod]
    public void ReportCommand_Parse_V1_ProtectionBySequence()
    {
        byte[] data = [0x75, 0x03, 0x01];
        CommandClassFrame frame = new(data);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.ProtectionBySequence, report.LocalProtection);
        Assert.IsNull(report.RfProtection);
    }

    [TestMethod]
    public void ReportCommand_Parse_V1_NoOperationPossible()
    {
        byte[] data = [0x75, 0x03, 0x02];
        CommandClassFrame frame = new(data);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.NoOperationPossible, report.LocalProtection);
        Assert.IsNull(report.RfProtection);
    }

    [TestMethod]
    public void ReportCommand_Parse_V2_BothStates()
    {
        // Local: NoOperationPossible (0x02), RF: NoRfControl (0x01)
        byte[] data = [0x75, 0x03, 0x02, 0x01];
        CommandClassFrame frame = new(data);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.NoOperationPossible, report.LocalProtection);
        Assert.IsNotNull(report.RfProtection);
        Assert.AreEqual(RfProtectionState.NoRfControl, report.RfProtection.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_V2_NoRfResponse()
    {
        byte[] data = [0x75, 0x03, 0x00, 0x02];
        CommandClassFrame frame = new(data);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.Unprotected, report.LocalProtection);
        Assert.AreEqual(RfProtectionState.NoRfResponse, report.RfProtection!.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_V2_ReservedBitsIgnored()
    {
        // Upper nibble set (reserved bits) — should be ignored, lower nibble is the state
        byte[] data = [0x75, 0x03, 0xF1, 0xF2];
        CommandClassFrame frame = new(data);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.ProtectionBySequence, report.LocalProtection);
        Assert.AreEqual(RfProtectionState.NoRfResponse, report.RfProtection!.Value);
    }

    [TestMethod]
    public void ReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x75, 0x03];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ProtectionCommandClass.ProtectionReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void ReportCommand_Create_V1()
    {
        ProtectionCommandClass.ProtectionReportCommand command =
            ProtectionCommandClass.ProtectionReportCommand.Create(1, LocalProtectionState.ProtectionBySequence, RfProtectionState.Unprotected);

        Assert.AreEqual(1, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x01, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void ReportCommand_Create_V2()
    {
        ProtectionCommandClass.ProtectionReportCommand command =
            ProtectionCommandClass.ProtectionReportCommand.Create(2, LocalProtectionState.NoOperationPossible, RfProtectionState.NoRfResponse);

        Assert.AreEqual(2, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]);
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[1]);
    }

    [TestMethod]
    public void ReportCommand_RoundTrip_V1()
    {
        ProtectionCommandClass.ProtectionReportCommand command =
            ProtectionCommandClass.ProtectionReportCommand.Create(1, LocalProtectionState.NoOperationPossible, RfProtectionState.Unprotected);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.NoOperationPossible, report.LocalProtection);
        Assert.IsNull(report.RfProtection);
    }

    [TestMethod]
    public void ReportCommand_RoundTrip_V2()
    {
        ProtectionCommandClass.ProtectionReportCommand command =
            ProtectionCommandClass.ProtectionReportCommand.Create(2, LocalProtectionState.ProtectionBySequence, RfProtectionState.NoRfControl);

        ProtectionReport report = ProtectionCommandClass.ProtectionReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.AreEqual(LocalProtectionState.ProtectionBySequence, report.LocalProtection);
        Assert.AreEqual(RfProtectionState.NoRfControl, report.RfProtection!.Value);
    }
}
