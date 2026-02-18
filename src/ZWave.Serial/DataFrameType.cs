namespace ZWave.Serial;

/// <summary>
/// The type of a Z-Wave Serial API data frame.
/// </summary>
/// <remarks>
/// As defined by INS12350 section 5.4.3.
/// </remarks>
public enum DataFrameType : byte
{
    /// <summary>
    /// Request
    /// </summary>
    REQ = 0x00,

    /// <summary>
    /// Response
    /// </summary>
    RES = 0x01,
}
