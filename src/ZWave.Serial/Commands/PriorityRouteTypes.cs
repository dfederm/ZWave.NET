namespace ZWave.Serial.Commands;

/// <summary>
/// The kind of priority route returned by Get Priority Route.
/// Encoded per Z-Wave Host API Specification Table 4.168.
/// </summary>
public enum PriorityRouteKind : byte
{
    /// <summary>
    /// No route to the given node ID was found.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// The returned route is the Last Working Route (LWR).
    /// </summary>
    LastWorkingRoute = 0x01,

    /// <summary>
    /// The returned route is the Next to Last Working Route (NLWR).
    /// </summary>
    NextLastWorkingRoute = 0x02,

    /// <summary>
    /// The returned route is the Application Priority Route.
    /// </summary>
    ApplicationPriorityRoute = 0x10,
}

/// <summary>
/// Route speed for priority route commands.
/// Encoded per Z-Wave Host API Specification Table 4.9.
/// </summary>
public enum PriorityRouteSpeed : byte
{
    /// <summary>
    /// 9.6 kbits/s.
    /// </summary>
    ZWave9k6 = 0x01,

    /// <summary>
    /// 40 kbits/s.
    /// </summary>
    ZWave40k = 0x02,

    /// <summary>
    /// 100 kbits/s.
    /// </summary>
    ZWave100k = 0x03,
}
