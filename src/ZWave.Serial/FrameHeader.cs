namespace ZWave.Serial;

internal static class FrameHeader
{
    /// <summary>
    /// Start of Frame
    /// </summary>
    public const byte SOF = 0x01;

    /// <summary>
    /// Acknowledge
    /// </summary>
    public const byte ACK = 0x06;

    /// <summary>
    /// Negative acknowledge
    /// </summary>
    public const byte NAK = 0x15;

    /// <summary>
    /// Cancel
    /// </summary>
    public const byte CAN = 0x18;

    /// <summary>
    /// All valid headers
    /// </summary>
    public static ReadOnlySpan<byte> ValidHeaders => [SOF, ACK, NAK, CAN];
}
