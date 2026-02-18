namespace ZWave.CommandClasses;

/// <summary>
/// The duration for reaching the target value.
/// </summary>
/// <remarks>
/// As defined by SDS13781 Table 7
/// </remarks>
public struct DurationSet
{
    public DurationSet(byte value)
    {
        Value = value;
    }

    public DurationSet(TimeSpan duration)
    {
        // Instantly
        if (duration == TimeSpan.Zero)
        {
            Value = 0;
        }

        // 1 second (0x01) to 127 seconds (0x7F) in 1 second resolution.
        else if (duration <= TimeSpan.FromSeconds(127))
        {
            Value = (byte)Math.Round(duration.TotalSeconds);
        }

        // 1 minute (0x80) to 127 minutes (0xFE) in 1 minute resolution.
        else if (duration <= TimeSpan.FromMinutes(127))
        {
            Value = (byte)(Math.Round(duration.TotalMinutes) + 0x7f);
        }

        else
        {
            throw new ArgumentException("Value must be less or equal to 127 minutes", nameof(duration));
        }
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
