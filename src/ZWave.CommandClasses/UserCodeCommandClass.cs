using System.Text;

namespace ZWave.CommandClasses;

public enum UserCodeCommand : byte
{
    /// <summary>
    /// Set a user code at the receiving node.
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// Request a user code from a node.
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// Advertise a user code at the sending node.
    /// </summary>
    Report = 0x03,

    /// <summary>
    /// Request the number of supported user codes from a node.
    /// </summary>
    UsersNumberGet = 0x04,

    /// <summary>
    /// Advertise the number of supported user codes at the sending node.
    /// </summary>
    UsersNumberReport = 0x05,
}

/// <summary>
/// Defines the status of a user identifier.
/// </summary>
public enum UserIdStatus : byte
{
    /// <summary>
    /// The user identifier is available (not set).
    /// </summary>
    Available = 0x00,

    /// <summary>
    /// The user identifier is occupied (set).
    /// </summary>
    Occupied = 0x01,

    /// <summary>
    /// The user identifier is reserved by the administrator.
    /// </summary>
    ReservedByAdministrator = 0x02,

    /// <summary>
    /// The status of the user identifier is not available.
    /// </summary>
    StatusNotAvailable = 0xFE,
}

/// <summary>
/// Represents a user code entry with its status and code value.
/// </summary>
public readonly struct UserCodeEntry
{
    public UserCodeEntry(UserIdStatus status, string? userCode)
    {
        Status = status;
        UserCode = userCode;
    }

    /// <summary>
    /// Gets the status of the user identifier.
    /// </summary>
    public UserIdStatus Status { get; }

    /// <summary>
    /// Gets the user code value, or null if the status is not <see cref="UserIdStatus.Occupied"/>.
    /// </summary>
    public string? UserCode { get; }
}

[CommandClass(CommandClassId.UserCode)]
public sealed class UserCodeCommandClass : CommandClass<UserCodeCommand>
{
    private Dictionary<byte, UserCodeEntry>? _userCodes;

    internal UserCodeCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets the number of supported user codes, or null if not yet determined.
    /// </summary>
    public ushort? SupportedUsers { get; private set; }

    /// <summary>
    /// Gets the cached user code entries keyed by user identifier.
    /// </summary>
    public IReadOnlyDictionary<byte, UserCodeEntry>? UserCodes => _userCodes;

    /// <inheritdoc />
    public override bool? IsCommandSupported(UserCodeCommand command)
        => command switch
        {
            UserCodeCommand.Set => true,
            UserCodeCommand.Get => true,
            UserCodeCommand.UsersNumberGet => true,
            _ => false,
        };

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetUsersNumberAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Request the number of supported user codes from a node.
    /// </summary>
    public async Task<ushort> GetUsersNumberAsync(CancellationToken cancellationToken)
    {
        UserCodeUsersNumberGetCommand command = UserCodeUsersNumberGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<UserCodeUsersNumberReportCommand>(cancellationToken).ConfigureAwait(false);
        return SupportedUsers!.Value;
    }

    /// <summary>
    /// Request a user code from a node.
    /// </summary>
    public async Task<UserCodeEntry> GetAsync(byte userIdentifier, CancellationToken cancellationToken)
    {
        UserCodeGetCommand command = UserCodeGetCommand.Create(userIdentifier);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        await AwaitNextReportAsync<UserCodeReportCommand>(
            predicate: frame =>
            {
                UserCodeReportCommand report = new UserCodeReportCommand(frame);
                return report.UserIdentifier == userIdentifier;
            },
            cancellationToken).ConfigureAwait(false);
        return _userCodes![userIdentifier];
    }

    /// <summary>
    /// Set a user code at the receiving node.
    /// </summary>
    public async Task SetAsync(byte userIdentifier, UserIdStatus status, string? userCode, CancellationToken cancellationToken)
    {
        UserCodeSetCommand command = UserCodeSetCommand.Create(userIdentifier, status, userCode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((UserCodeCommand)frame.CommandId)
        {
            case UserCodeCommand.Set:
            case UserCodeCommand.Get:
            case UserCodeCommand.UsersNumberGet:
            {
                // We don't expect to recieve these commands
                break;
            }
            case UserCodeCommand.Report:
            {
                UserCodeReportCommand command = new UserCodeReportCommand(frame);
                byte userIdentifier = command.UserIdentifier;
                UserIdStatus status = command.UserIdStatus;
                string? userCode = command.UserCode;

                _userCodes ??= new Dictionary<byte, UserCodeEntry>();
                _userCodes[userIdentifier] = new UserCodeEntry(status, userCode);
                break;
            }
            case UserCodeCommand.UsersNumberReport:
            {
                UserCodeUsersNumberReportCommand command = new UserCodeUsersNumberReportCommand(frame, EffectiveVersion);
                SupportedUsers = command.SupportedUsers;
                break;
            }
        }
    }

    private readonly struct UserCodeSetCommand : ICommand
    {
        public UserCodeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.Set;

        public CommandClassFrame Frame { get; }

        public static UserCodeSetCommand Create(byte userIdentifier, UserIdStatus status, string? userCode)
        {
            byte[] codeBytes = userCode != null ? Encoding.ASCII.GetBytes(userCode) : [];
            Span<byte> commandParameters = stackalloc byte[2 + codeBytes.Length];
            commandParameters[0] = userIdentifier;
            commandParameters[1] = (byte)status;
            codeBytes.CopyTo(commandParameters[2..]);
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new UserCodeSetCommand(frame);
        }
    }

    private readonly struct UserCodeGetCommand : ICommand
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

    private readonly struct UserCodeReportCommand : ICommand
    {
        public UserCodeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.Report;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The user identifier.
        /// </summary>
        public byte UserIdentifier => Frame.CommandParameters.Span[0];

        /// <summary>
        /// The status of the user identifier.
        /// </summary>
        public UserIdStatus UserIdStatus => (UserIdStatus)Frame.CommandParameters.Span[1];

        /// <summary>
        /// The user code value, or null if the status is not <see cref="CommandClasses.UserIdStatus.Occupied"/>.
        /// </summary>
        public string? UserCode
        {
            get
            {
                if (UserIdStatus != UserIdStatus.Occupied || Frame.CommandParameters.Length <= 2)
                {
                    return null;
                }

                ReadOnlySpan<byte> codeBytes = Frame.CommandParameters.Span[2..];
                return Encoding.ASCII.GetString(codeBytes);
            }
        }
    }

    private readonly struct UserCodeUsersNumberGetCommand : ICommand
    {
        public UserCodeUsersNumberGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.UsersNumberGet;

        public CommandClassFrame Frame { get; }

        public static UserCodeUsersNumberGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new UserCodeUsersNumberGetCommand(frame);
        }
    }

    private readonly struct UserCodeUsersNumberReportCommand : ICommand
    {
        private readonly byte _version;

        public UserCodeUsersNumberReportCommand(CommandClassFrame frame, byte version)
        {
            Frame = frame;
            _version = version;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.UsersNumberReport;

        public CommandClassFrame Frame { get; }

        /// <summary>
        /// The number of supported user codes.
        /// </summary>
        public ushort SupportedUsers
        {
            get
            {
                if (_version >= 2 && Frame.CommandParameters.Length > 1)
                {
                    // V2 uses 2 bytes (big-endian) for the user count.
                    return Frame.CommandParameters.Span[..2].ToUInt16BE();
                }

                return Frame.CommandParameters.Span[0];
            }
        }
    }
}
