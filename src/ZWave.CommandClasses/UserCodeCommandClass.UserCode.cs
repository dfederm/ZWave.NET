using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a User Code Report from a device.
/// </summary>
/// <param name="UserIdentifier">The user identifier (1-255).</param>
/// <param name="Status">The status of the user identifier.</param>
/// <param name="UserCode">The user code as an ASCII string, or <see langword="null"/> if not set.</param>
public readonly record struct UserCodeReport(
    byte UserIdentifier,
    UserIdStatus Status,
    string? UserCode);

public sealed partial class UserCodeCommandClass
{
    /// <summary>
    /// Raised when a User Code Report is received.
    /// </summary>
    public event Action<UserCodeReport>? OnUserCodeReportReceived;

    /// <summary>
    /// Gets the user code for the specified user identifier.
    /// </summary>
    /// <param name="userIdentifier">The user identifier (1-255).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user code report.</returns>
    public async Task<UserCodeReport> GetUserCodeAsync(byte userIdentifier, CancellationToken cancellationToken)
    {
        var command = UserCodeGetCommand.Create(userIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<UserCodeReportCommand>(
            predicate: frame => frame.CommandParameters.Length > 0
                && frame.CommandParameters.Span[0] == userIdentifier,
            cancellationToken).ConfigureAwait(false);
        UserCodeReport report = UserCodeReportCommand.Parse(reportFrame, Logger);
        OnUserCodeReportReceived?.Invoke(report);
        return report;
    }

    /// <summary>
    /// Sets the user code for the specified user identifier.
    /// </summary>
    /// <param name="userIdentifier">The user identifier (1-255).</param>
    /// <param name="status">The user ID status to set. Must not be <see cref="UserIdStatus.Available"/>; use <see cref="ClearUserCodeAsync"/> instead.</param>
    /// <param name="userCode">The user code as an ASCII string (4-10 characters).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetUserCodeAsync(
        byte userIdentifier,
        UserIdStatus status,
        string userCode,
        CancellationToken cancellationToken)
    {
        var command = UserCodeSetCommand.Create(userIdentifier, status, userCode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears the user code for the specified user identifier, or all user identifiers.
    /// </summary>
    /// <param name="userIdentifier">The user identifier (1-255), or 0 to clear all users.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ClearUserCodeAsync(byte userIdentifier, CancellationToken cancellationToken)
    {
        var command = UserCodeSetCommand.CreateClear(userIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the number of supported user codes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of supported user codes.</returns>
    public async Task<ushort> GetSupportedUsersCountAsync(CancellationToken cancellationToken)
    {
        var command = UsersNumberGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<UsersNumberReportCommand>(cancellationToken).ConfigureAwait(false);
        ushort count = UsersNumberReportCommand.Parse(reportFrame, Logger);
        SupportedUsersCount = count;
        return count;
    }

    internal readonly struct UserCodeSetCommand : ICommand
    {
        public UserCodeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.Set;

        public CommandClassFrame Frame { get; }

        public static UserCodeSetCommand Create(byte userIdentifier, UserIdStatus status, string userCode)
        {
            // Per spec CC:0063.01.01.11.006: length 4-10, CC:0063.01.01.11.007: ASCII digits only
            ValidateCode(userCode, nameof(userCode), digitsOnly: true);

            Span<byte> commandParameters = stackalloc byte[2 + userCode.Length];
            commandParameters[0] = userIdentifier;
            commandParameters[1] = (byte)status;
            Encoding.ASCII.GetBytes(userCode, commandParameters[2..]);

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new UserCodeSetCommand(frame);
        }

        public static UserCodeSetCommand CreateClear(byte userIdentifier)
        {
            // Per spec CC:0063.01.01.11.009: User Code MUST be 0x00000000 when status is Available
            Span<byte> commandParameters = stackalloc byte[6];
            commandParameters[0] = userIdentifier;
            commandParameters[1] = (byte)UserIdStatus.Available;
            // Bytes 2-5 are already 0x00 from stackalloc

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new UserCodeSetCommand(frame);
        }
    }

    internal readonly struct UserCodeGetCommand : ICommand
    {
        public UserCodeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.Get;

        public CommandClassFrame Frame { get; }

        public static UserCodeGetCommand Create(byte userIdentifier)
        {
            ReadOnlySpan<byte> commandParameters = [userIdentifier];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new UserCodeGetCommand(frame);
        }
    }

    internal readonly struct UserCodeReportCommand : ICommand
    {
        public UserCodeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.Report;

        public CommandClassFrame Frame { get; }

        public static UserCodeReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("User Code Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "User Code Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte userIdentifier = span[0];
            UserIdStatus status = (UserIdStatus)span[1];

            string? userCode = null;
            if (status != UserIdStatus.Available
                && status != UserIdStatus.StatusNotAvailable
                && span.Length > 2)
            {
                userCode = Encoding.ASCII.GetString(span[2..]);
            }

            return new UserCodeReport(userIdentifier, status, userCode);
        }
    }

    internal readonly struct UsersNumberGetCommand : ICommand
    {
        public UsersNumberGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.UsersNumberGet;

        public CommandClassFrame Frame { get; }

        public static UsersNumberGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new UsersNumberGetCommand(frame);
        }
    }

    internal readonly struct UsersNumberReportCommand : ICommand
    {
        public UsersNumberReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.UsersNumberReport;

        public CommandClassFrame Frame { get; }

        public static ushort Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Users Number Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Users Number Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            // V2 adds a 16-bit Extended Supported Users field; use it when present
            if (span.Length >= 3)
            {
                return span[1..3].ToUInt16BE();
            }

            return span[0];
        }
    }
}
