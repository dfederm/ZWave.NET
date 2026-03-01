using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.CommandClasses.Tests;

public partial class VersionCommandClassTests
{
    [TestMethod]
    public void MigrationCapabilitiesGetCommand_Create_HasCorrectFormat()
    {
        VersionCommandClass.VersionMigrationCapabilitiesGetCommand command =
            VersionCommandClass.VersionMigrationCapabilitiesGetCommand.Create();

        Assert.AreEqual(CommandClassId.Version, VersionCommandClass.VersionMigrationCapabilitiesGetCommand.CommandClassId);
        Assert.AreEqual((byte)VersionCommand.MigrationCapabilitiesGet, VersionCommandClass.VersionMigrationCapabilitiesGetCommand.CommandId);
        Assert.AreEqual(2, command.Frame.Data.Length);
    }

    [TestMethod]
    public void MigrationCapabilitiesReport_Parse_TwoOperations()
    {
        // CC=0x86, Cmd=0x1A, Count=2, Op1=0x01(UserCodeToUserCredential), Op2=0x02(UserCredentialToUserCode)
        byte[] data = [0x86, 0x1A, 0x02, 0x01, 0x02];
        CommandClassFrame frame = new(data);

        VersionMigrationCapabilities capabilities =
            VersionCommandClass.VersionMigrationCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(2, capabilities.SupportedOperations);
        Assert.AreEqual(MigrationOperationId.UserCodeToUserCredential, capabilities.SupportedOperations[0]);
        Assert.AreEqual(MigrationOperationId.UserCredentialToUserCode, capabilities.SupportedOperations[1]);
    }

    [TestMethod]
    public void MigrationCapabilitiesReport_Parse_NoOperations()
    {
        // CC=0x86, Cmd=0x1A, Count=0
        byte[] data = [0x86, 0x1A, 0x00];
        CommandClassFrame frame = new(data);

        VersionMigrationCapabilities capabilities =
            VersionCommandClass.VersionMigrationCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.IsEmpty(capabilities.SupportedOperations);
    }

    [TestMethod]
    public void MigrationCapabilitiesReport_Parse_OneOperation()
    {
        // CC=0x86, Cmd=0x1A, Count=1, Op=0x01
        byte[] data = [0x86, 0x1A, 0x01, 0x01];
        CommandClassFrame frame = new(data);

        VersionMigrationCapabilities capabilities =
            VersionCommandClass.VersionMigrationCapabilitiesReportCommand.Parse(frame, NullLogger.Instance);

        Assert.HasCount(1, capabilities.SupportedOperations);
        Assert.AreEqual(MigrationOperationId.UserCodeToUserCredential, capabilities.SupportedOperations[0]);
    }

    [TestMethod]
    public void MigrationCapabilitiesReport_Parse_TooShort_Throws()
    {
        byte[] data = [0x86, 0x1A];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionMigrationCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void MigrationCapabilitiesReport_Parse_PayloadTooShortForDeclaredCount_Throws()
    {
        // Declares 3 operations but only has 2 bytes of operation data
        byte[] data = [0x86, 0x1A, 0x03, 0x01, 0x02];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionMigrationCapabilitiesReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void MigrationSetCommand_Create_HasCorrectFormat()
    {
        VersionCommandClass.VersionMigrationSetCommand command =
            VersionCommandClass.VersionMigrationSetCommand.Create(MigrationOperationId.UserCodeToUserCredential);

        Assert.AreEqual(CommandClassId.Version, VersionCommandClass.VersionMigrationSetCommand.CommandClassId);
        Assert.AreEqual((byte)VersionCommand.MigrationSet, VersionCommandClass.VersionMigrationSetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + OperationId
        Assert.AreEqual((byte)MigrationOperationId.UserCodeToUserCredential, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void MigrationGetCommand_Create_HasCorrectFormat()
    {
        VersionCommandClass.VersionMigrationGetCommand command =
            VersionCommandClass.VersionMigrationGetCommand.Create(MigrationOperationId.UserCredentialToUserCode);

        Assert.AreEqual(CommandClassId.Version, VersionCommandClass.VersionMigrationGetCommand.CommandClassId);
        Assert.AreEqual((byte)VersionCommand.MigrationGet, VersionCommandClass.VersionMigrationGetCommand.CommandId);
        Assert.AreEqual(3, command.Frame.Data.Length); // CC + Cmd + OperationId
        Assert.AreEqual((byte)MigrationOperationId.UserCredentialToUserCode, command.Frame.CommandParameters.Span[0]);
    }

    [TestMethod]
    public void MigrationReport_Parse_Ready()
    {
        // CC=0x86, Cmd=0x1D, OpId=0x01, Status=0x00(Ready), ETC=0x0000
        byte[] data = [0x86, 0x1D, 0x01, 0x00, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        VersionMigrationReport report =
            VersionCommandClass.VersionMigrationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MigrationOperationId.UserCodeToUserCredential, report.OperationId);
        Assert.AreEqual(MigrationStatus.Ready, report.Status);
        Assert.AreEqual((ushort)0, report.EstimatedTimeOfCompletion);
    }

    [TestMethod]
    public void MigrationReport_Parse_InProgress_WithEstimatedTime()
    {
        // CC=0x86, Cmd=0x1D, OpId=0x02, Status=0x01(InProgress), ETC=300 (0x012C)
        byte[] data = [0x86, 0x1D, 0x02, 0x01, 0x01, 0x2C];
        CommandClassFrame frame = new(data);

        VersionMigrationReport report =
            VersionCommandClass.VersionMigrationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MigrationOperationId.UserCredentialToUserCode, report.OperationId);
        Assert.AreEqual(MigrationStatus.InProgress, report.Status);
        Assert.AreEqual((ushort)300, report.EstimatedTimeOfCompletion);
    }

    [TestMethod]
    public void MigrationReport_Parse_Success()
    {
        // CC=0x86, Cmd=0x1D, OpId=0x01, Status=0x02(Success), ETC=0x0000
        byte[] data = [0x86, 0x1D, 0x01, 0x02, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        VersionMigrationReport report =
            VersionCommandClass.VersionMigrationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MigrationStatus.MigrationCompleteSuccess, report.Status);
    }

    [TestMethod]
    public void MigrationReport_Parse_Failure()
    {
        // CC=0x86, Cmd=0x1D, OpId=0x01, Status=0x03(Failure), ETC=0x0000
        byte[] data = [0x86, 0x1D, 0x01, 0x03, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        VersionMigrationReport report =
            VersionCommandClass.VersionMigrationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MigrationStatus.MigrationCompleteFailure, report.Status);
    }

    [TestMethod]
    public void MigrationReport_Parse_Unsupported()
    {
        // CC=0x86, Cmd=0x1D, OpId=0x01, Status=0x04(Unsupported), ETC=0x0000
        byte[] data = [0x86, 0x1D, 0x01, 0x04, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        VersionMigrationReport report =
            VersionCommandClass.VersionMigrationReportCommand.Parse(frame, NullLogger.Instance);

        Assert.AreEqual(MigrationStatus.Unsupported, report.Status);
    }

    [TestMethod]
    public void MigrationReport_Parse_TooShort_Throws()
    {
        // Only 3 bytes of parameters (need 4: OpId + Status + ETC(2))
        byte[] data = [0x86, 0x1D, 0x01, 0x00, 0x00];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionMigrationReportCommand.Parse(frame, NullLogger.Instance));
    }

    [TestMethod]
    public void MigrationReport_Parse_EmptyPayload_Throws()
    {
        byte[] data = [0x86, 0x1D];
        CommandClassFrame frame = new(data);

        Assert.Throws<ZWaveException>(
            () => VersionCommandClass.VersionMigrationReportCommand.Parse(frame, NullLogger.Instance));
    }
}
