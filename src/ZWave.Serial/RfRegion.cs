namespace ZWave.Serial;

/// <summary>
/// The Z-Wave RF region, defining the number of channels and center frequency.
/// </summary>
/// <remarks>
/// As defined by the Z-Wave Host API Specification, Table 4.6.
/// </remarks>
public enum RfRegion : byte
{
    /// <summary>
    /// Europe.
    /// </summary>
    Europe = 0x00,

    /// <summary>
    /// United States.
    /// </summary>
    US = 0x01,

    /// <summary>
    /// Australia / New Zealand.
    /// </summary>
    AustraliaNewZealand = 0x02,

    /// <summary>
    /// Hong Kong.
    /// </summary>
    HongKong = 0x03,

    /// <summary>
    /// India.
    /// </summary>
    India = 0x05,

    /// <summary>
    /// Israel.
    /// </summary>
    Israel = 0x06,

    /// <summary>
    /// Russia.
    /// </summary>
    Russia = 0x07,

    /// <summary>
    /// China.
    /// </summary>
    China = 0x08,

    /// <summary>
    /// United States (Long Range).
    /// </summary>
    USLongRange = 0x09,

    /// <summary>
    /// Europe (Long Range).
    /// </summary>
    EuropeLongRange = 0x0B,

    /// <summary>
    /// Japan.
    /// </summary>
    Japan = 0x20,

    /// <summary>
    /// Korea.
    /// </summary>
    Korea = 0x21,

    /// <summary>
    /// Undefined or unknown region.
    /// </summary>
    Undefined = 0xFE,

    /// <summary>
    /// Default region (EU).
    /// </summary>
    Default = 0xFF,
}
