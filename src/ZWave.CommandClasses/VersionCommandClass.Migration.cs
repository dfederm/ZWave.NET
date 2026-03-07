using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies a discrete migration operation.
/// </summary>
public enum MigrationOperationId : byte
{
    /// <summary>
    /// Migrate existing User Code CC V2 user codes to equivalent Users and PIN Codes
    /// from User Credential CC V1.
    /// </summary>
    UserCodeToUserCredential = 0x01,

    /// <summary>
    /// Migrate existing Users and PIN codes from a credential database to equivalent
    /// User Codes from the User Code CC.
    /// </summary>
    UserCredentialToUserCode = 0x02,
}

/// <summary>
/// Identifies the status of a migration operation.
/// </summary>
public enum MigrationStatus : byte
{
    /// <summary>
    /// The node is ready to run this migration operation.
    /// </summary>
    Ready = 0x00,

    /// <summary>
    /// The node is currently running this migration operation.
    /// </summary>
    InProgress = 0x01,

    /// <summary>
    /// The node successfully completed this migration operation.
    /// </summary>
    MigrationCompleteSuccess = 0x02,

    /// <summary>
    /// The node failed to complete this migration operation.
    /// </summary>
    MigrationCompleteFailure = 0x03,

    /// <summary>
    /// The node does not, or no longer, support this migration operation.
    /// </summary>
    Unsupported = 0x04,
}

/// <summary>
/// Represents a Version Migration Capabilities Report listing supported migration operations.
/// </summary>
public readonly record struct VersionMigrationCapabilities(
    /// <summary>
    /// The set of migration operations supported by the device.
    /// </summary>
    IReadOnlyList<MigrationOperationId> SupportedOperations);

/// <summary>
/// Represents a Version Migration Report containing the status of a migration operation.
/// </summary>
public readonly record struct VersionMigrationReport(
    /// <summary>
    /// The migration operation this report pertains to.
    /// </summary>
    MigrationOperationId OperationId,

    /// <summary>
    /// The current status of the migration operation.
    /// </summary>
    MigrationStatus Status,

    /// <summary>
    /// The estimated time of completion in seconds. Only meaningful when <see cref="Status"/> is
    /// <see cref="MigrationStatus.InProgress"/>.
    /// </summary>
    ushort EstimatedTimeOfCompletion);

public sealed partial class VersionCommandClass
{
    /// <summary>
    /// Occurs when a Version Migration Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<VersionMigrationReport>? OnMigrationReportReceived;

    /// <summary>
    /// Request which migration operations are supported by the device.
    /// </summary>
    public async Task<VersionMigrationCapabilities> GetMigrationCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var command = VersionMigrationCapabilitiesGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionMigrationCapabilitiesReportCommand>(cancellationToken).ConfigureAwait(false);
        VersionMigrationCapabilities capabilities = VersionMigrationCapabilitiesReportCommand.Parse(reportFrame, Logger);
        return capabilities;
    }

    /// <summary>
    /// Trigger a specific migration operation on the device.
    /// </summary>
    public async Task MigrateAsync(MigrationOperationId operationId, CancellationToken cancellationToken)
    {
        var command = VersionMigrationSetCommand.Create(operationId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the status of a migration operation.
    /// </summary>
    public async Task<VersionMigrationReport> GetMigrationStatusAsync(MigrationOperationId operationId, CancellationToken cancellationToken)
    {
        var command = VersionMigrationGetCommand.Create(operationId);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionMigrationReportCommand>(
            predicate: frame =>
            {
                return frame.CommandParameters.Length > 0
                    && (MigrationOperationId)frame.CommandParameters.Span[0] == operationId;
            },
            cancellationToken).ConfigureAwait(false);
        VersionMigrationReport report = VersionMigrationReportCommand.Parse(reportFrame, Logger);
        OnMigrationReportReceived?.Invoke(report);
        return report;
    }

    internal readonly struct VersionMigrationCapabilitiesGetCommand : ICommand
    {
        public VersionMigrationCapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.MigrationCapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static VersionMigrationCapabilitiesGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new VersionMigrationCapabilitiesGetCommand(frame);
        }
    }

    internal readonly struct VersionMigrationCapabilitiesReportCommand : ICommand
    {
        public VersionMigrationCapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.MigrationCapabilitiesReport;

        public CommandClassFrame Frame { get; }

        public static VersionMigrationCapabilities Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Version Migration Capabilities Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Version Migration Capabilities Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte count = span[0];
            MigrationOperationId[] operations;
            if (count == 0)
            {
                operations = [];
            }
            else
            {
                if (frame.CommandParameters.Length < 1 + count)
                {
                    logger.LogWarning(
                        "Version Migration Capabilities Report declares {Count} operations but only has {Length} bytes of payload",
                        count,
                        frame.CommandParameters.Length);
                    ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Version Migration Capabilities Report payload is too short for declared operation count");
                }

                operations = new MigrationOperationId[count];
                for (int i = 0; i < count; i++)
                {
                    operations[i] = (MigrationOperationId)span[1 + i];
                }
            }

            return new VersionMigrationCapabilities(operations);
        }
    }

    internal readonly struct VersionMigrationSetCommand : ICommand
    {
        public VersionMigrationSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.MigrationSet;

        public CommandClassFrame Frame { get; }

        public static VersionMigrationSetCommand Create(MigrationOperationId operationId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)operationId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new VersionMigrationSetCommand(frame);
        }
    }

    internal readonly struct VersionMigrationGetCommand : ICommand
    {
        public VersionMigrationGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.MigrationGet;

        public CommandClassFrame Frame { get; }

        public static VersionMigrationGetCommand Create(MigrationOperationId operationId)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)operationId];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new VersionMigrationGetCommand(frame);
        }
    }

    internal readonly struct VersionMigrationReportCommand : ICommand
    {
        public VersionMigrationReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Version;

        public static byte CommandId => (byte)VersionCommand.MigrationReport;

        public CommandClassFrame Frame { get; }

        public static VersionMigrationReport Parse(CommandClassFrame frame, ILogger logger)
        {
            // MigrationOperationId(1) + MigrationStatus(1) + EstimatedTimeOfCompletion(2) = 4 bytes
            if (frame.CommandParameters.Length < 4)
            {
                logger.LogWarning("Version Migration Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Version Migration Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            MigrationOperationId operationId = (MigrationOperationId)span[0];
            MigrationStatus status = (MigrationStatus)span[1];
            ushort estimatedTime = span[2..4].ToUInt16BE();

            return new VersionMigrationReport(operationId, status, estimatedTime);
        }
    }
}
