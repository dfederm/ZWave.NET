namespace ZWave.CommandClasses;

/// <summary>
/// The duration left to reach the target value advertised in a report.
/// </summary>
/// <remarks>
/// As defined by the Z-Wave Application Specification, Table 8.
/// </remarks>
public readonly struct DurationReport
{
    public DurationReport(byte value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the raw duration byte value.
    /// </summary>
    public byte Value { get; }

    /// <summary>
    /// Gets the interpreted duration, or null if unknown.
    /// </summary>
    public TimeSpan? Duration => DurationEncoding.Decode(Value, maxMinuteByte: 0xFD);

    public static implicit operator DurationReport(byte b) => new DurationReport(b);
}
