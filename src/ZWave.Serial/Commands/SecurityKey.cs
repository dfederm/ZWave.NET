namespace ZWave.Serial.Commands;

/// <summary>
/// Security key type used for encryption.
/// </summary>
public enum SecurityKey : byte
{
    /// <summary>
    /// No security key (unencrypted).
    /// </summary>
    None = 0x00,

    /// <summary>
    /// S2 Unauthenticated key.
    /// </summary>
    S2Unauthenticated = 0x01,

    /// <summary>
    /// S2 Authenticated key.
    /// </summary>
    S2Authenticated = 0x02,

    /// <summary>
    /// S2 Access Control key.
    /// </summary>
    S2Access = 0x03,

    /// <summary>
    /// S0 key.
    /// </summary>
    S0 = 0x04,
}
