using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Represents a Central Scene Notification received from a device.
/// </summary>
public readonly record struct CentralSceneNotification(
    /// <summary>
    /// The sequence number for duplicate detection. Incremented each time a notification is issued.
    /// </summary>
    byte SequenceNumber,

    /// <summary>
    /// The key attribute specifying the state of the key (e.g. pressed, released, held down, multi-tap).
    /// </summary>
    CentralSceneKeyAttribute KeyAttribute,

    /// <summary>
    /// The scene number that was activated.
    /// </summary>
    byte SceneNumber,

    /// <summary>
    /// Whether the Slow Refresh capability is active for this notification (version 3).
    /// Only meaningful when <see cref="KeyAttribute"/> is <see cref="CentralSceneKeyAttribute.KeyHeldDown"/>.
    /// For version 1–2 devices, this will always be <see langword="false"/> since the bit is reserved.
    /// </summary>
    bool SlowRefresh);

public sealed partial class CentralSceneCommandClass
{
    /// <summary>
    /// Gets the last notification received from the device.
    /// </summary>
    public CentralSceneNotification? LastNotification { get; private set; }

    /// <summary>
    /// Event raised when a Central Scene Notification is received.
    /// </summary>
    public event Action<CentralSceneNotification>? OnNotificationReceived;

    internal readonly struct CentralSceneNotificationCommand : ICommand
    {
        public CentralSceneNotificationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.CentralScene;

        public static byte CommandId => (byte)CentralSceneCommand.Notification;

        public CommandClassFrame Frame { get; }

        public static CentralSceneNotification Parse(CommandClassFrame frame, ILogger logger)
        {
            // Notification: Sequence Number (1) + Properties1 (1) + Scene Number (1) = 3 bytes
            if (frame.CommandParameters.Length < 3)
            {
                logger.LogWarning(
                    "Central Scene Notification frame is too short ({Length} bytes)",
                    frame.CommandParameters.Length);
                ZWaveException.Throw(
                    ZWaveErrorCode.InvalidPayload,
                    "Central Scene Notification frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;
            byte sequenceNumber = span[0];

            // Properties1 byte layout (V3):
            //   Bit 7: Slow Refresh (V3, reserved in V1-V2)
            //   Bits 6-3: Reserved
            //   Bits 2-0: Key Attributes
            // Per forward-compatibility rules, we do not mask reserved bits.
            CentralSceneKeyAttribute keyAttribute = (CentralSceneKeyAttribute)(span[1] & 0b0000_0111);

            // Slow Refresh is bit 7. Per spec: "A receiving node MUST ignore this field if the
            // command is not carrying the Key Held Down key attribute."
            // We parse it unconditionally for forward-compatibility. V1/V2 devices will always
            // send 0 for this reserved bit, so SlowRefresh will be false for older devices.
            bool slowRefresh = (span[1] & 0b1000_0000) != 0;

            byte sceneNumber = span[2];

            return new CentralSceneNotification(sequenceNumber, keyAttribute, sceneNumber, slowRefresh);
        }
    }
}
