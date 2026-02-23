namespace ZWave.Serial.Commands;

/// <summary>
/// The type of a routing table entry.
/// </summary>
public enum RouteType : byte
{
    /// <summary>
    /// Assigned Priority Route (APR).
    /// </summary>
    AssignedPriorityRoute = 0x00,

    /// <summary>
    /// Last Working Route (LWR).
    /// </summary>
    LastWorkingRoute = 0x01,

    /// <summary>
    /// Next Last Working Route (NLWR).
    /// </summary>
    NextLastWorkingRoute = 0x02,

    /// <summary>
    /// Next Next Last Working Route (NNLWR).
    /// </summary>
    NextNextLastWorkingRoute = 0x03,

    /// <summary>
    /// Calculated Route.
    /// </summary>
    CalculatedRoute = 0x04,
}

/// <summary>
/// The beam type used with a route table entry.
/// </summary>
public enum RouteBeamType : byte
{
    /// <summary>
    /// No beam (not a FLiRS node).
    /// </summary>
    None = 0x00,

    /// <summary>
    /// 250 ms beam.
    /// </summary>
    Beam250ms = 0x01,

    /// <summary>
    /// 1000 ms beam.
    /// </summary>
    Beam1000ms = 0x02,
}

/// <summary>
/// The speed used with a route table entry.
/// </summary>
public enum RouteSpeed : byte
{
    /// <summary>
    /// 9.6 kbps.
    /// </summary>
    ZWave9k6 = 0x00,

    /// <summary>
    /// 40 kbps.
    /// </summary>
    ZWave40k = 0x01,

    /// <summary>
    /// 100 kbps.
    /// </summary>
    ZWave100k = 0x02,
}

/// <summary>
/// Represents a single routing table entry with type, beam/speed, and up to 4 hops.
/// </summary>
public readonly struct RoutingTableEntry
{
    /// <summary>
    /// The size of a routing table entry in bytes.
    /// </summary>
    public const int Size = 6;

    private readonly ReadOnlyMemory<byte> _data;

    /// <summary>
    /// Create a routing table entry from raw frame data.
    /// </summary>
    public RoutingTableEntry(ReadOnlyMemory<byte> data)
    {
        _data = data;
    }

    /// <summary>
    /// Create a routing table entry from semantic data.
    /// </summary>
    /// <param name="routeType">The route type.</param>
    /// <param name="beam">The beam type.</param>
    /// <param name="speed">The route speed.</param>
    /// <param name="hops">The hop node IDs (0 to 4). Unused hops are zero-filled.</param>
    public RoutingTableEntry(RouteType routeType, RouteBeamType beam, RouteSpeed speed, ReadOnlySpan<byte> hops)
    {
        byte[] data = new byte[Size];
        data[0] = (byte)routeType;
        data[1] = (byte)(((byte)beam << 6) | ((byte)speed & 0x03));
        for (int i = 0; i < hops.Length && i < 4; i++)
        {
            data[2 + i] = hops[i];
        }

        _data = data;
    }

    /// <summary>
    /// The route type.
    /// </summary>
    public RouteType RouteType => (RouteType)_data.Span[0];

    /// <summary>
    /// The beam type used with this route (bits 7-6).
    /// </summary>
    public RouteBeamType Beam => (RouteBeamType)((_data.Span[1] >> 6) & 0x03);

    /// <summary>
    /// The speed used with this route (bits 1-0).
    /// </summary>
    public RouteSpeed Speed => (RouteSpeed)(_data.Span[1] & 0x03);

    /// <summary>
    /// The hop node IDs for this route, excluding trailing zeros.
    /// A direct-range route has zero hops.
    /// </summary>
    public ReadOnlySpan<byte> Hops
    {
        get
        {
            ReadOnlySpan<byte> allHops = _data.Span[2..6];
            int count = allHops.IndexOf((byte)0);
            return count < 0 ? allHops : allHops[..count];
        }
    }

    /// <summary>
    /// Writes this entry's 6 bytes to the destination span.
    /// </summary>
    public void WriteTo(Span<byte> destination)
    {
        _data.Span[..Size].CopyTo(destination);
    }
}
