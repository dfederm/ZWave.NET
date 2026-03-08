using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents the capabilities of a User Code Command Class device.
/// </summary>
/// <param name="AdminCodeSupport">Whether the admin code functionality is supported.</param>
/// <param name="AdminCodeDeactivationSupport">Whether the admin code can be deactivated.</param>
/// <param name="ChecksumSupport">Whether the user code checksum functionality is supported.</param>
/// <param name="MultipleReportSupport">Whether reporting multiple user codes in a single command is supported.</param>
/// <param name="MultipleSetSupport">Whether setting multiple user codes in a single command is supported.</param>
/// <param name="SupportedStatuses">The set of supported user ID status values.</param>
/// <param name="SupportedKeypadModes">The set of supported keypad modes.</param>
/// <param name="SupportedKeys">The set of supported ASCII key codes for user code input.</param>
public readonly record struct UserCodeCapabilities(
    bool AdminCodeSupport,
    bool AdminCodeDeactivationSupport,
    bool ChecksumSupport,
    bool MultipleReportSupport,
    bool MultipleSetSupport,
    IReadOnlySet<UserIdStatus> SupportedStatuses,
    IReadOnlySet<UserCodeKeypadMode> SupportedKeypadModes,
    IReadOnlySet<byte> SupportedKeys);

public sealed partial class UserCodeCommandClass
{
    /// <summary>
    /// Gets the capabilities of the device. Populated during interview for version 2+ devices.
    /// </summary>
    public UserCodeCapabilities? Capabilities { get; private set; }

    /// <summary>
    /// Gets the user code capabilities from the device.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The capabilities of the device.</returns>
    public async Task<UserCodeCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        var command = CapabilitiesGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<CapabilitiesReportCommand>(cancellationToken).ConfigureAwait(false);
        UserCodeCapabilities capabilities = CapabilitiesReportCommand.Parse(reportFrame, Logger);
        Capabilities = capabilities;
        return capabilities;
    }

    internal readonly struct CapabilitiesGetCommand : ICommand
    {
        public CapabilitiesGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.CapabilitiesGet;

        public CommandClassFrame Frame { get; }

        public static CapabilitiesGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new CapabilitiesGetCommand(frame);
        }
    }

    internal readonly struct CapabilitiesReportCommand : ICommand
    {
        public CapabilitiesReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.UserCode;

        public static byte CommandId => (byte)UserCodeCommand.CapabilitiesReport;

        public CommandClassFrame Frame { get; }

        public static UserCodeCapabilities Parse(CommandClassFrame frame, ILogger logger)
        {
            // Minimum: 1 byte header for statuses + 0 bitmask + 1 byte header for keypad modes + 0 bitmask + 1 byte header for keys
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning(
                    "User Code Capabilities Report frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Capabilities Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            int offset = 0;

            // Byte 0: AC Support (1) | ACD Support (1) | Reserved (1) | Status Bit Mask Length (5)
            bool adminCodeSupport = (span[offset] & 0b1000_0000) != 0;
            bool adminCodeDeactivationSupport = (span[offset] & 0b0100_0000) != 0;
            int statusBitMaskLength = span[offset] & 0b0001_1111;
            offset++;

            if (span.Length < offset + statusBitMaskLength)
            {
                logger.LogWarning(
                    "User Code Capabilities Report frame is too short for status bitmask ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Capabilities Report frame is too short for status bitmask");
            }

            HashSet<UserIdStatus> supportedStatuses =
                BitMaskHelper.ParseBitMask<UserIdStatus>(span.Slice(offset, statusBitMaskLength));
            offset += statusBitMaskLength;

            if (span.Length < offset + 1)
            {
                logger.LogWarning(
                    "User Code Capabilities Report frame is too short for keypad mode header ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Capabilities Report frame is too short for keypad mode header");
            }

            // Next byte: UCC Support (1) | MUCR Support (1) | MUCS Support (1) | Keypad Modes Bit Mask Length (5)
            bool checksumSupport = (span[offset] & 0b1000_0000) != 0;
            bool multipleReportSupport = (span[offset] & 0b0100_0000) != 0;
            bool multipleSetSupport = (span[offset] & 0b0010_0000) != 0;
            int keypadModesBitMaskLength = span[offset] & 0b0001_1111;
            offset++;

            if (span.Length < offset + keypadModesBitMaskLength)
            {
                logger.LogWarning(
                    "User Code Capabilities Report frame is too short for keypad modes bitmask ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Capabilities Report frame is too short for keypad modes bitmask");
            }

            HashSet<UserCodeKeypadMode> supportedKeypadModes =
                BitMaskHelper.ParseBitMask<UserCodeKeypadMode>(span.Slice(offset, keypadModesBitMaskLength));
            offset += keypadModesBitMaskLength;

            if (span.Length < offset + 1)
            {
                logger.LogWarning(
                    "User Code Capabilities Report frame is too short for keys header ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Capabilities Report frame is too short for keys header");
            }

            // Next byte: Reserved (3) | Supported Keys Bit Mask Length (5)
            int keysBitMaskLength = span[offset] & 0b0001_1111;
            offset++;

            if (span.Length < offset + keysBitMaskLength)
            {
                logger.LogWarning(
                    "User Code Capabilities Report frame is too short for keys bitmask ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "User Code Capabilities Report frame is too short for keys bitmask");
            }

            // Parse supported keys (ASCII codes) — manual parsing since byte is not an enum
            HashSet<byte> supportedKeys = [];
            ReadOnlySpan<byte> keysBitMask = span.Slice(offset, keysBitMaskLength);
            for (int byteNum = 0; byteNum < keysBitMask.Length; byteNum++)
            {
                for (int bitNum = 0; bitNum < 8; bitNum++)
                {
                    if ((keysBitMask[byteNum] & (1 << bitNum)) != 0)
                    {
                        byte asciiCode = (byte)((byteNum * 8) + bitNum);
                        supportedKeys.Add(asciiCode);
                    }
                }
            }

            return new UserCodeCapabilities(
                adminCodeSupport,
                adminCodeDeactivationSupport,
                checksumSupport,
                multipleReportSupport,
                multipleSetSupport,
                supportedStatuses,
                supportedKeypadModes,
                supportedKeys);
        }
    }
}
