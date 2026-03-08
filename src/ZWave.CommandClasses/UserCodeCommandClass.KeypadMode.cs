using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class UserCodeCommandClass
{
    /// <summary>
    /// Gets the last keypad mode reported by the device.
    /// </summary>
    public UserCodeKeypadMode? LastKeypadMode { get; private set; }

    /// <summary>
    /// Raised when a Keypad Mode Report is received.
    /// </summary>
    public event Action<UserCodeKeypadMode>? OnKeypadModeReportReceived;

    /// <summary>
    /// Gets the current keypad mode from the device.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current keypad mode.</returns>
    public async Task<UserCodeKeypadMode> GetKeypadModeAsync(CancellationToken cancellationToken)
    {
        var command = KeypadModeGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<KeypadModeReportCommand>(cancellationToken).ConfigureAwait(false);
        UserCodeKeypadMode keypadMode = KeypadModeReportCommand.Parse(reportFrame, Logger);
        LastKeypadMode = keypadMode;
        OnKeypadModeReportReceived?.Invoke(keypadMode);
        return keypadMode;
    }

    /// <summary>
    /// Sets the keypad mode on the device.
    /// </summary>
    /// <param name="keypadMode">The keypad mode to set.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task SetKeypadModeAsync(UserCodeKeypadMode keypadMode, CancellationToken cancellationToken)
    {
        var command = KeypadModeSetCommand.Create(keypadMode);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    }

    internal readonly struct KeypadModeSetCommand : ICommand
    {
        public KeypadModeSetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.KeypadModeSet;

        public CommandClassFrame Frame { get; }

        public static KeypadModeSetCommand Create(UserCodeKeypadMode keypadMode)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)keypadMode];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new KeypadModeSetCommand(frame);
        }
    }

    internal readonly struct KeypadModeGetCommand : ICommand
    {
        public KeypadModeGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.KeypadModeGet;

        public CommandClassFrame Frame { get; }

        public static KeypadModeGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new KeypadModeGetCommand(frame);
        }
    }

    internal readonly struct KeypadModeReportCommand : ICommand
    {
        public KeypadModeReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.KeypadModeReport;

        public CommandClassFrame Frame { get; }

        public static UserCodeKeypadMode Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning(
                    "User Code Keypad Mode Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Keypad Mode Report frame is too short");
            }

            return (UserCodeKeypadMode)frame.CommandParameters.Span[0];
        }
    }
}
