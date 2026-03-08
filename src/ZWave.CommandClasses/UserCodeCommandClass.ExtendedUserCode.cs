using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a single user code entry in an Extended User Code Report or Set.
/// </summary>
/// <param name="UserIdentifier">The user identifier (1-65535).</param>
/// <param name="Status">The status of the user identifier.</param>
/// <param name="UserCode">The user code as an ASCII string, or <see langword="null"/> if not set.</param>
public readonly record struct ExtendedUserCodeEntry(
    ushort UserIdentifier,
    UserIdStatus Status,
    string? UserCode);

/// <summary>
/// Represents an Extended User Code Report from a version 2 device.
/// </summary>
/// <param name="Entries">The user code entries in this report.</param>
/// <param name="NextUserIdentifier">The next user identifier in use after the last entry, or 0 if this is the last one.</param>
public readonly record struct ExtendedUserCodeReport(
    IReadOnlyList<ExtendedUserCodeEntry> Entries,
    ushort NextUserIdentifier);

public sealed partial class UserCodeCommandClass
{
    /// <summary>
    /// Raised when an Extended User Code Report is received.
    /// </summary>
    public event Action<ExtendedUserCodeReport>? OnExtendedUserCodeReportReceived;

    /// <summary>
    /// Gets the user code for a single user identifier.
    /// </summary>
    /// <param name="userIdentifier">The user identifier to query (1-65535).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user code entry for the requested identifier.</returns>
    public async Task<ExtendedUserCodeEntry> GetExtendedUserCodeAsync(
        ushort userIdentifier,
        CancellationToken cancellationToken)
    {
        var command = ExtendedUserCodeGetCommand.Create(userIdentifier, reportMore: false);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ExtendedUserCodeReportCommand>(
            predicate: frame => frame.CommandParameters.Length >= 3
                && frame.CommandParameters.Span[1..3].ToUInt16BE() == userIdentifier,
            cancellationToken).ConfigureAwait(false);
        return ExtendedUserCodeReportCommand.Parse(reportFrame, Logger);
    }

    /// <summary>
    /// Gets multiple user codes in bulk starting from the specified user identifier.
    /// The device reports as many user codes as possible in a single response.
    /// </summary>
    /// <param name="userIdentifier">The user identifier to start from (1-65535).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extended user code report containing one or more user code entries.</returns>
    public async Task<ExtendedUserCodeReport> GetBulkExtendedUserCodesAsync(
        ushort userIdentifier,
        CancellationToken cancellationToken)
    {
        var command = ExtendedUserCodeGetCommand.Create(userIdentifier, reportMore: true);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ExtendedUserCodeReportCommand>(
            predicate: frame => frame.CommandParameters.Length >= 3
                && frame.CommandParameters.Span[1..3].ToUInt16BE() == userIdentifier,
            cancellationToken).ConfigureAwait(false);
        ExtendedUserCodeReport report = ExtendedUserCodeReportCommand.ParseBulk(reportFrame, Logger);
        OnExtendedUserCodeReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Sets a single user code using the extended format.
    /// </summary>
    /// <param name="userIdentifier">The user identifier (1-65535).</param>
    /// <param name="status">The user ID status to set. Must not be <see cref="UserIdStatus.Available"/>; use <see cref="ClearExtendedUserCodeAsync"/> instead.</param>
    /// <param name="userCode">The user code as an ASCII string (4-10 characters).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetExtendedUserCodeAsync(
        ushort userIdentifier,
        UserIdStatus status,
        string userCode,
        CancellationToken cancellationToken)
    {
        var command = ExtendedUserCodeSetCommand.Create(userIdentifier, status, userCode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears a single user code, or all user codes.
    /// </summary>
    /// <param name="userIdentifier">The user identifier (1-65535), or 0 to clear all users.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ClearExtendedUserCodeAsync(ushort userIdentifier, CancellationToken cancellationToken)
    {
        var command = ExtendedUserCodeSetCommand.CreateClear(userIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets one or more user codes using the extended format.
    /// </summary>
    /// <param name="entries">The user code entries to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetBulkExtendedUserCodesAsync(
        IReadOnlyList<ExtendedUserCodeEntry> entries,
        CancellationToken cancellationToken)
    {
        var command = ExtendedUserCodeSetCommand.CreateBulk(entries);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct ExtendedUserCodeSetCommand : ICommand
    {
        public ExtendedUserCodeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.ExtendedUserCodeSet;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// Creates a command to set a single user code (allocation-free).
        /// </summary>
        public static ExtendedUserCodeSetCommand Create(
            ushort userIdentifier,
            UserIdStatus status,
            string userCode)
        {
            // Per spec CC:0063.02.0B.11.00A: length 4-10, CC:0063.02.0B.11.00C: ASCII
            ValidateCode(userCode, nameof(userCode), digitsOnly: false);

            // 1 (count) + 2 (UserID) + 1 (Status) + 1 (Len) + code
            Span<byte> commandParameters = stackalloc byte[5 + userCode.Length];
            commandParameters[0] = 1;
            userIdentifier.WriteBytesBE(commandParameters[1..3]);
            commandParameters[3] = (byte)status;
            commandParameters[4] = (byte)(userCode.Length & 0x0F);
            Encoding.ASCII.GetBytes(userCode, commandParameters[5..]);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ExtendedUserCodeSetCommand(frame);
        }

        /// <summary>
        /// Creates a command to clear a single user code or all user codes (allocation-free).
        /// </summary>
        public static ExtendedUserCodeSetCommand CreateClear(ushort userIdentifier)
        {
            // 1 (count) + 2 (UserID) + 1 (Status=Available) + 1 (Len=0)
            Span<byte> commandParameters = stackalloc byte[5];
            commandParameters[0] = 1;
            userIdentifier.WriteBytesBE(commandParameters[1..3]);
            commandParameters[3] = (byte)UserIdStatus.Available;
            // commandParameters[4] is already 0 (code length = 0)

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ExtendedUserCodeSetCommand(frame);
        }

        /// <summary>
        /// Creates a command to set multiple user codes.
        /// </summary>
        public static ExtendedUserCodeSetCommand CreateBulk(IReadOnlyList<ExtendedUserCodeEntry> entries)
        {
            // Validate all entries up front
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Status != UserIdStatus.Available && entries[i].UserCode is not null)
                {
                    // Per spec CC:0063.02.0B.11.00A: length 4-10, CC:0063.02.0B.11.00C: ASCII
                    ValidateCode(entries[i].UserCode!, nameof(entries), digitsOnly: false);
                }
            }

            // Calculate total size: 1 (count) + sum of block sizes
            // Since validation passed, all code chars are ASCII so string.Length == byte count
            int totalSize = 1;
            for (int i = 0; i < entries.Count; i++)
            {
                int codeLength = entries[i].Status != UserIdStatus.Available && entries[i].UserCode is not null
                    ? entries[i].UserCode!.Length
                    : 0;
                // UserIdentifier(2) + UserIdStatus(1) + Reserved|UserCodeLength(1) + UserCode(N)
                totalSize += 2 + 1 + 1 + codeLength;
            }

            byte[] buffer = new byte[totalSize];
            buffer[0] = (byte)entries.Count;
            int offset = 1;

            for (int i = 0; i < entries.Count; i++)
            {
                ExtendedUserCodeEntry entry = entries[i];
                entry.UserIdentifier.WriteBytesBE(buffer.AsSpan(offset, 2));
                offset += 2;

                buffer[offset] = (byte)entry.Status;
                offset++;

                int codeLength = 0;
                if (entry.Status != UserIdStatus.Available && entry.UserCode is not null)
                {
                    codeLength = entry.UserCode.Length;
                    Encoding.ASCII.GetBytes(entry.UserCode, buffer.AsSpan(offset + 1, codeLength));
                }

                // Reserved (4 bits) | User Code Length (4 bits)
                buffer[offset] = (byte)(codeLength & 0x0F);
                offset += 1 + codeLength;
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, buffer);
            return new ExtendedUserCodeSetCommand(frame);
        }
    }

    internal readonly struct ExtendedUserCodeGetCommand : ICommand
    {
        public ExtendedUserCodeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.ExtendedUserCodeGet;

        public CommandClassFrame Frame { get; }

        public static ExtendedUserCodeGetCommand Create(ushort userIdentifier, bool reportMore)
        {
            Span<byte> commandParameters = stackalloc byte[3];
            userIdentifier.WriteBytesBE(commandParameters[0..2]);
            commandParameters[2] = (byte)(reportMore ? 0x01 : 0x00);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ExtendedUserCodeGetCommand(frame);
        }
    }

    internal readonly struct ExtendedUserCodeReportCommand : ICommand
    {
        public ExtendedUserCodeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.ExtendedUserCodeReport;

        public CommandClassFrame Frame { get; }

        public static ExtendedUserCodeEntry Parse(CommandClassFrame frame, ILogger logger)
        {
            ValidateReportHeader(frame, logger);
            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            int offset = 1;
            return ParseEntryBlock(frame, span, ref offset, 0, logger);
        }

        public static ExtendedUserCodeReport ParseBulk(CommandClassFrame frame, ILogger logger)
        {
            ValidateReportHeader(frame, logger);

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte numberOfUserCodes = span[0];

            List<ExtendedUserCodeEntry> entries = new(numberOfUserCodes);
            int offset = 1;

            for (int i = 0; i < numberOfUserCodes; i++)
            {
                entries.Add(ParseEntryBlock(frame, span, ref offset, i, logger));
            }

            // Next User Identifier (16 bits) at the end
            if (span.Length < offset + 2)
            {
                logger.LogWarning(
                    "Extended User Code Report frame is too short for Next User Identifier ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Extended User Code Report frame is too short for Next User Identifier");
            }

            ushort nextUserIdentifier = span[offset..(offset + 2)].ToUInt16BE();

            return new ExtendedUserCodeReport(entries, nextUserIdentifier);
        }

        private static void ValidateReportHeader(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: 1 (count) + 4 (one block: UserID(2) + Status(1) + Len(1)) + 2 (NextUserID)
            if (frame.CommandParameters.Length < 7)
            {
                logger.LogWarning(
                    "Extended User Code Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Extended User Code Report frame is too short");
            }

            byte numberOfUserCodes = frame.CommandParameters.Span[0];
            if (numberOfUserCodes == 0)
            {
                logger.LogWarning("Extended User Code Report has 0 user code blocks");
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Extended User Code Report has 0 user code blocks");
            }
        }

        private static ExtendedUserCodeEntry ParseEntryBlock(
            CommandClassFrame frame,
            ReadOnlySpan<byte> span,
            ref int offset,
            int blockIndex,
            ILogger logger)
        {
            // Each block: UserIdentifier(2) + Status(1) + Reserved|Length(1) = 4 bytes minimum
            if (span.Length < offset + 4)
            {
                logger.LogWarning(
                    "Extended User Code Report frame is too short for user code block {Index} ({Length} bytes)",
                    blockIndex,
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Extended User Code Report frame is too short for user code block");
            }

            ushort userIdentifier = span[offset..(offset + 2)].ToUInt16BE();
            offset += 2;

            UserIdStatus status = (UserIdStatus)span[offset];
            offset++;

            // Reserved (4 bits) | User Code Length (4 bits)
            int codeLength = span[offset] & 0x0F;
            offset++;

            if (span.Length < offset + codeLength)
            {
                logger.LogWarning(
                    "Extended User Code Report frame is too short for user code data ({Length} bytes, need {Expected})",
                    frame.CommandParameters.Length,
                    offset + codeLength);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Extended User Code Report frame is too short for user code data");
            }

            string? userCode = null;
            if (codeLength > 0)
            {
                userCode = Encoding.ASCII.GetString(span.Slice(offset, codeLength));
            }

            offset += codeLength;
            return new ExtendedUserCodeEntry(userIdentifier, status, userCode);
        }
    }
}
