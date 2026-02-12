namespace ZWave.Serial.Commands;

[Flags]
public enum TransmissionOptions : byte
{
    /// <summary>
    /// Request the destination node to return an MPDU acknowledgement.
    /// </summary>
    ACK = 1 << 0,

    /// <summary>
    /// Obsolete
    /// </summary>
    LowPower = 1 << 1,

    /// <summary>
    /// Enable automatic routing
    /// </summary>
    AutoRoute = 1 << 2,

    // Bit 3 is reserved

    /// <summary>
    /// Explicitly disable any routing
    /// </summary>
    NoRoute = 1 << 4,

    /// <summary>
    /// Enable the usage of Explore NPDUs if needed
    /// </summary>
    Explore = 1 << 5,
}
