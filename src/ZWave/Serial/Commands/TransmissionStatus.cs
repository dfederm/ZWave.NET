namespace ZWave.Serial.Commands;

public enum TransmissionStatus : byte
{
    /// <summary>
    /// Transmission completed and successful
    /// </summary>
    Ok = 0x00,

    /// <summary>
    /// Transmission completed but no Acknowledgment
    /// </summary>
    NoAck = 0x01,

    /// <summary>
    /// Transmission failed
    /// </summary>
    Fail = 0x02,

    /// <summary>
    /// Transmission failed due to routing being busy.
    /// </summary>
    NotIdle = 0x03,

    /// <summary>
    /// Transmission failed due to routing resolution.
    /// </summary>
    NoRoute = 0x04,

    /// <summary>
    /// Transmission completed and successful, including S2 resynchronization backoff.
    /// </summary>
    Verified = 0x05,
}
