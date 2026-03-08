namespace ZWave.CommandClasses;

/// <summary>
/// The duration for reaching the target value.
/// </summary>
/// <remarks>
/// As defined by the Z-Wave Application Specification, Table 7.
/// </remarks>
public struct DurationSet
{
    public DurationSet(byte value)
    {
        Value = value;
    }

    public DurationSet(TimeSpan duration)
    {
        Value = DurationEncoding.Encode(duration);
    }

    /// <summary>
    /// Factory default duration.
    /// </summary>
    public static DurationSet FactoryDefault => new DurationSet(0xff);

    /// <summary>
    /// Gets the raw duration byte value.
    /// </summary>
    public byte Value { get; }

    public static implicit operator DurationSet(byte b) => new DurationSet(b);
}
