namespace ZWave.Serial;

/// <summary>
/// The active Z-Wave Long Range radio channel.
/// </summary>
/// <remarks>
/// As defined by the Z-Wave Host API Specification.
/// </remarks>
public enum LongRangeChannel : byte
{
    /// <summary>
    /// Long Range is not supported by the currently configured RF region.
    /// </summary>
    Unsupported = 0x00,

    /// <summary>
    /// Long Range channel A.
    /// </summary>
    A = 0x01,

    /// <summary>
    /// Long Range channel B.
    /// </summary>
    B = 0x02,

    /// <summary>
    /// Long Range channel automatically selected by the Z-Wave algorithm.
    /// </summary>
    Auto = 0xff,
}
