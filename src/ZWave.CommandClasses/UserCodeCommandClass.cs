using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// User ID status values for the User Code Command Class.
/// </summary>
public enum UserIdStatus : byte
{
    /// <summary>
    /// The user identifier is available (not set).
    /// </summary>
    Available = 0x00,

    /// <summary>
    /// The user identifier is enabled and grants access.
    /// </summary>
    EnabledGrantAccess = 0x01,

    /// <summary>
    /// The user identifier is in use but disabled.
    /// </summary>
    Disabled = 0x02,

    /// <summary>
    /// The user identifier is used for messaging/notifications.
    /// </summary>
    Messaging = 0x03,

    /// <summary>
    /// The user identifier activates passage mode (permanent access toggle).
    /// </summary>
    PassageMode = 0x04,

    /// <summary>
    /// The requested user identifier is not valid.
    /// </summary>
    StatusNotAvailable = 0xFE,
}

/// <summary>
/// Keypad mode values for the User Code Command Class.
/// </summary>
public enum UserCodeKeypadMode : byte
{
    /// <summary>
    /// Normal mode: all user codes work normally.
    /// </summary>
    Normal = 0x00,

    /// <summary>
    /// Vacation mode: normal user codes are ignored.
    /// </summary>
    Vacation = 0x01,

    /// <summary>
    /// Privacy mode: all keypad input is ignored, including admin code.
    /// </summary>
    Privacy = 0x02,

    /// <summary>
    /// Locked out mode: all keypad input is ignored as a brute-force prevention measure.
    /// </summary>
    LockedOut = 0x03,
}

/// <summary>
/// Commands for the User Code Command Class.
/// </summary>
public enum UserCodeCommand : byte
{
    /// <summary>
    /// Set a user code.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request a user code.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Report a user code.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the number of supported users.
    /// </summary>
    UsersNumberGet = 0x04,

    /// <summary>
    /// Report the number of supported users.
    /// </summary>
    UsersNumberReport = 0x05,

    /// <summary>
    /// Request user code capabilities.
    /// </summary>
    CapabilitiesGet = 0x06,

    /// <summary>
    /// Report user code capabilities.
    /// </summary>
    CapabilitiesReport = 0x07,

    /// <summary>
    /// Set the keypad mode.
    /// </summary>
    KeypadModeSet = 0x08,

    /// <summary>
    /// Request the current keypad mode.
    /// </summary>
    KeypadModeGet = 0x09,

    /// <summary>
    /// Report the current keypad mode.
    /// </summary>
    KeypadModeReport = 0x0A,

    /// <summary>
    /// Set one or more user codes (extended, 16-bit user IDs).
    /// </summary>
    ExtendedUserCodeSet = 0x0B,

    /// <summary>
    /// Request a user code (extended, 16-bit user ID).
    /// </summary>
    ExtendedUserCodeGet = 0x0C,

    /// <summary>
    /// Report one or more user codes (extended, 16-bit user IDs).
    /// </summary>
    ExtendedUserCodeReport = 0x0D,

    /// <summary>
    /// Set the admin code.
    /// </summary>
    AdminCodeSet = 0x0E,

    /// <summary>
    /// Request the admin code.
    /// </summary>
    AdminCodeGet = 0x0F,

    /// <summary>
    /// Report the admin code.
    /// </summary>
    AdminCodeReport = 0x10,

    /// <summary>
    /// Request the user code checksum.
    /// </summary>
    ChecksumGet = 0x11,

    /// <summary>
    /// Report the user code checksum.
    /// </summary>
    ChecksumReport = 0x12,
}

/// <summary>
/// Implementation of the User Code Command Class (versions 1-2).
/// </summary>
[CommandClass(CommandClassId.UserCode)]
public sealed partial class UserCodeCommandClass : CommandClass<UserCodeCommand>
{
    internal UserCodeCommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Validates a user code string for length (4-10) and character range.
    /// </summary>
    /// <param name="code">The code to validate.</param>
    /// <param name="paramName">The parameter name for exception messages.</param>
    /// <param name="digitsOnly">
    /// If <see langword="true"/>, only ASCII digits (0x30-0x39) are allowed (V1).
    /// If <see langword="false"/>, any ASCII character (0x00-0x7F) is allowed (V2).
    /// </param>
    private static void ValidateCode(string code, string paramName, bool digitsOnly)
    {
        if (code.Length < 4 || code.Length > 10)
        {
            throw new ArgumentOutOfRangeException(paramName, code.Length, "Code length must be between 4 and 10 characters.");
        }

        for (int i = 0; i < code.Length; i++)
        {
            char c = code[i];
            if (digitsOnly)
            {
                if (c < '0' || c > '9')
                {
                    throw new ArgumentException($"User code must contain only ASCII digits (0-9). Found '{c}' at position {i}.", paramName);
                }
            }
            else
            {
                if (c > 127)
                {
                    throw new ArgumentException($"Code must contain only ASCII characters. Found non-ASCII character at position {i}.", paramName);
                }
            }
        }
    }

    /// <summary>
    /// Gets the number of supported user codes.
    /// </summary>
    public ushort? SupportedUsersCount { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(UserCodeCommand command)
        => command switch
        {
            UserCodeCommand.Set => true,
            UserCodeCommand.Get => true,
            UserCodeCommand.UsersNumberGet => true,
            UserCodeCommand.CapabilitiesGet => Version.HasValue ? Version >= 2 : null,
            UserCodeCommand.KeypadModeSet => Version.HasValue ? Version >= 2 : null,
            UserCodeCommand.KeypadModeGet => Version.HasValue ? Version >= 2 : null,
            UserCodeCommand.ExtendedUserCodeSet => Version.HasValue ? Version >= 2 : null,
            UserCodeCommand.ExtendedUserCodeGet => Version.HasValue ? Version >= 2 : null,
            UserCodeCommand.AdminCodeSet => Version.HasValue ? Version >= 2 : null,
            UserCodeCommand.AdminCodeGet => Version.HasValue ? Version >= 2 : null,
            UserCodeCommand.ChecksumGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    /// <inheritdoc />
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetSupportedUsersCountAsync(cancellationToken).ConfigureAwait(false);

        if (IsCommandSupported(UserCodeCommand.CapabilitiesGet).GetValueOrDefault())
        {
            _ = await GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (IsCommandSupported(UserCodeCommand.KeypadModeGet).GetValueOrDefault())
        {
            _ = await GetKeypadModeAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((UserCodeCommand)frame.CommandId)
        {
            case UserCodeCommand.Report:
            {
                UserCodeReport report = UserCodeReportCommand.Parse(frame, Logger);
                OnUserCodeReportReceived?.Invoke(report);
                break;
            }
            case UserCodeCommand.ExtendedUserCodeReport:
            {
                ExtendedUserCodeReport report = ExtendedUserCodeReportCommand.ParseBulk(frame, Logger);
                OnExtendedUserCodeReportReceived?.Invoke(report);
                break;
            }
            case UserCodeCommand.KeypadModeReport:
            {
                UserCodeKeypadMode keypadMode = KeypadModeReportCommand.Parse(frame, Logger);
                LastKeypadMode = keypadMode;
                OnKeypadModeReportReceived?.Invoke(keypadMode);
                break;
            }
        }
    }
}
