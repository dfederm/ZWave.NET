namespace ZWave.Serial.Commands;

/// <summary>
/// Bitmask of security keys a node possesses.
/// Encoded per Z-Wave Host API Specification Table 4.339.
/// </summary>
[Flags]
public enum SecurityKeyFlags : byte
{
    /// <summary>
    /// No security key.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// S2 Unauthenticated network key.
    /// </summary>
    S2Unauthenticated = 0x01,

    /// <summary>
    /// S2 Authenticated network key.
    /// </summary>
    S2Authenticated = 0x02,

    /// <summary>
    /// S2 Access Control network key.
    /// </summary>
    S2Access = 0x04,

    /// <summary>
    /// Security 0 network key.
    /// </summary>
    S0 = 0x80,
}
