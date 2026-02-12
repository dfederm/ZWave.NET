namespace ZWave.Serial.Commands;

[Flags]
public enum ReceivedStatus : byte
{
    // Bit 0 is reserved

    /// <summary>
    /// The Z-Wave frame has been received with low output power.
    /// </summary>
    LowPower = 1 << 1,

    // Bit 2 is reserved

    /// <summary>
    /// The Z-Wave frame has been received using broadcast addressing.
    /// </summary>
    BroadcastAddressing = 1 << 3,

    /// <summary>
    /// The Z-Wave frame has been received using multicast addressing.
    /// </summary>
    MulticastAddressing = 1 << 4,

    /// <summary>
    /// the Z-Wave frame has been received using an Explore NPDU.
    /// </summary>
    Explore = 1 << 5,

    /// <summary>
    /// The frame not addressed to the Z-Wave Module. This is useful only in promiscuous mode.
    /// </summary>
    ForeignFrame = 1 << 6,

    /// <summary>
    /// The frame was sent on another HomeID.
    /// </summary>
    ForeignHomeId = 1 << 7,
}
