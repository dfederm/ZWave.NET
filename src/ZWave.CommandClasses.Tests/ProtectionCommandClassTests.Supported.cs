using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class ProtectionCommandClassTests
{
    [TestMethod]
    public void SupportedGetCommand_Create_HasCorrectFormat()
    {
        ProtectionCommandClass.ProtectionSupportedGetCommand command =
            ProtectionCommandClass.ProtectionSupportedGetCommand.Create();

        Assert.AreEqual(CommandClassId.Protection, ProtectionCommandClass.ProtectionSupportedGetCommand.CommandClassId);
        Assert.AreEqual((byte)ProtectionCommand.SupportedGet, ProtectionCommandClass.ProtectionSupportedGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void SupportedReportCommand_Parse_AllFeatures()
    {
        // Flags: EC=1 (bit 1), Timeout=1 (bit 0) → 0x03
        // Local bitmask: states 0,1,2 → byte1=0x07, byte2=0x00
        // RF bitmask: states 0,1,2 → byte1=0x07, byte2=0x00
        byte[] data = [0x75, 0x05, 0x03, 0x07, 0x00, 0x07, 0x00];
        CommandClassFrame frame = new(data);

        ProtectionSupportedReport report =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.SupportsExclusiveControl);
        Assert.IsTrue(report.SupportsTimeout);
        Assert.Contains(LocalProtectionState.Unprotected, report.SupportedLocalStates);
        Assert.Contains(LocalProtectionState.ProtectionBySequence, report.SupportedLocalStates);
        Assert.Contains(LocalProtectionState.NoOperationPossible, report.SupportedLocalStates);
        Assert.Contains(RfProtectionState.Unprotected, report.SupportedRfStates);
        Assert.Contains(RfProtectionState.NoRfControl, report.SupportedRfStates);
        Assert.Contains(RfProtectionState.NoRfResponse, report.SupportedRfStates);
    }

    [TestMethod]
    public void SupportedReportCommand_Parse_NoOptionalFeatures()
    {
        // Flags: 0x00 (no EC, no Timeout)
        // Local bitmask: states 0,1 → byte1=0x03, byte2=0x00
        // RF bitmask: state 0 only → byte1=0x01, byte2=0x00
        byte[] data = [0x75, 0x05, 0x00, 0x03, 0x00, 0x01, 0x00];
        CommandClassFrame frame = new(data);

        ProtectionSupportedReport report =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.SupportsExclusiveControl);
        Assert.IsFalse(report.SupportsTimeout);
        Assert.HasCount(2, report.SupportedLocalStates);
        Assert.Contains(LocalProtectionState.Unprotected, report.SupportedLocalStates);
        Assert.Contains(LocalProtectionState.ProtectionBySequence, report.SupportedLocalStates);
        Assert.HasCount(1, report.SupportedRfStates);
        Assert.Contains(RfProtectionState.Unprotected, report.SupportedRfStates);
    }

    [TestMethod]
    public void SupportedReportCommand_Parse_ExclusiveControlOnly()
    {
        // Flags: EC=1 (bit 1) → 0x02
        byte[] data = [0x75, 0x05, 0x02, 0x07, 0x00, 0x07, 0x00];
        CommandClassFrame frame = new(data);

        ProtectionSupportedReport report =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.SupportsExclusiveControl);
        Assert.IsFalse(report.SupportsTimeout);
    }

    [TestMethod]
    public void SupportedReportCommand_Parse_TimeoutOnly()
    {
        // Flags: Timeout=1 (bit 0) → 0x01
        byte[] data = [0x75, 0x05, 0x01, 0x07, 0x00, 0x07, 0x00];
        CommandClassFrame frame = new(data);

        ProtectionSupportedReport report =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsFalse(report.SupportsExclusiveControl);
        Assert.IsTrue(report.SupportsTimeout);
    }

    [TestMethod]
    public void SupportedReportCommand_Parse_ReservedBitsIgnored()
    {
        // Flags with reserved bits set: 0xFC | 0x03 = 0xFF
        byte[] data = [0x75, 0x05, 0xFF, 0x07, 0x00, 0x07, 0x00];
        CommandClassFrame frame = new(data);

        ProtectionSupportedReport report =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsTrue(report.SupportsExclusiveControl);
        Assert.IsTrue(report.SupportsTimeout);
    }

    [TestMethod]
    public void SupportedReportCommand_Parse_TooShort_Throws()
    {
        byte[] data = [0x75, 0x05, 0x03, 0x07];
        CommandClassFrame frame = new(data);

        Assert.ThrowsExactly<ZWaveException>(
            () => ProtectionCommandClass.ProtectionSupportedReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void SupportedReportCommand_Create_HasCorrectFormat()
    {
        HashSet<LocalProtectionState> localStates =
        [
            LocalProtectionState.Unprotected,
            LocalProtectionState.ProtectionBySequence,
            LocalProtectionState.NoOperationPossible,
        ];
        HashSet<RfProtectionState> rfStates =
        [
            RfProtectionState.Unprotected,
            RfProtectionState.NoRfControl,
        ];

        ProtectionCommandClass.ProtectionSupportedReportCommand command =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Create(true, false, localStates, rfStates);

        Assert.AreEqual(5, command.Frame.CommandParameters.Length);
        Assert.AreEqual(0x02, command.Frame.CommandParameters.Span[0]); // EC=1, Timeout=0
    }

    [TestMethod]
    public void SupportedReportCommand_RoundTrip()
    {
        HashSet<LocalProtectionState> localStates =
        [
            LocalProtectionState.Unprotected,
            LocalProtectionState.NoOperationPossible,
        ];
        HashSet<RfProtectionState> rfStates =
        [
            RfProtectionState.Unprotected,
            RfProtectionState.NoRfResponse,
        ];

        ProtectionCommandClass.ProtectionSupportedReportCommand command =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Create(true, true, localStates, rfStates);

        ProtectionSupportedReport report =
            ProtectionCommandClass.ProtectionSupportedReportCommand.Parse(command.Frame, NullLogger.Instance);

        Assert.IsTrue(report.SupportsExclusiveControl);
        Assert.IsTrue(report.SupportsTimeout);
        Assert.HasCount(2, report.SupportedLocalStates);
        Assert.Contains(LocalProtectionState.Unprotected, report.SupportedLocalStates);
        Assert.Contains(LocalProtectionState.NoOperationPossible, report.SupportedLocalStates);
        Assert.HasCount(2, report.SupportedRfStates);
        Assert.Contains(RfProtectionState.Unprotected, report.SupportedRfStates);
        Assert.Contains(RfProtectionState.NoRfResponse, report.SupportedRfStates);
    }
}
