using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class UserCodeCommandClass
{
    /// <summary>
    /// Gets the admin code from the device.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The admin code, or <see langword="null"/> if the admin code is deactivated or not supported.</returns>
    public async Task<string?> GetAdminCodeAsync(CancellationToken cancellationToken)
    {
        var command = AdminCodeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<AdminCodeReportCommand>(cancellationToken).ConfigureAwait(false);
        string? adminCode = AdminCodeReportCommand.Parse(reportFrame, Logger);
        return adminCode;
    }

    /// <summary>
    /// Sets or deactivates the admin code on the device.
    /// </summary>
    /// <param name="adminCode">The admin code to set (4-10 ASCII characters), or <see langword="null"/> to deactivate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetAdminCodeAsync(string? adminCode, CancellationToken cancellationToken)
    {
        var command = AdminCodeSetCommand.Create(adminCode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct AdminCodeSetCommand : ICommand
    {
        public AdminCodeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.AdminCodeSet;

        public CommandClassFrame Frame { get; }

        public static AdminCodeSetCommand Create(string? adminCode)
        {
            if (adminCode is not null)
            {
                // Per spec CC:0063.02.0E.11.003: length 0 or 4-10, CC:0063.02.0E.11.006: ASCII
                ValidateCode(adminCode, nameof(adminCode), digitsOnly: false);
            }

            int codeLength = adminCode is not null ? Encoding.ASCII.GetByteCount(adminCode) : 0;
            Span<byte> commandParameters = stackalloc byte[1 + codeLength];

            // Reserved (4 bits) | Admin Code Length (4 bits)
            commandParameters[0] = (byte)(codeLength & 0x0F);

            if (adminCode is not null)
            {
                Encoding.ASCII.GetBytes(adminCode, commandParameters[1..]);
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new AdminCodeSetCommand(frame);
        }
    }

    internal readonly struct AdminCodeGetCommand : ICommand
    {
        public AdminCodeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.AdminCodeGet;

        public CommandClassFrame Frame { get; }

        public static AdminCodeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new AdminCodeGetCommand(frame);
        }
    }

    internal readonly struct AdminCodeReportCommand : ICommand
    {
        public AdminCodeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.AdminCodeReport;

        public CommandClassFrame Frame { get; }

        public static string? Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "Admin Code Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Admin Code Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            // Reserved (4 bits) | Admin Code Length (4 bits)
            int codeLength = span[0] & 0x0F;

            if (codeLength == 0)
            {
                return null;
            }

            if (span.Length < 1 + codeLength)
            {
                logger.LogWarning(
                    "Admin Code Report frame is too short for admin code ({Length} bytes, need {Expected})",
                    frame.CommandParameters.Length,
                    1 + codeLength);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Admin Code Report frame is too short for admin code");
            }

            return Encoding.ASCII.GetString(span.Slice(1, codeLength));
        }
    }
}
