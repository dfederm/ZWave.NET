namespace ZWave.CommandClasses;

/// <summary>
/// The duration left to reach the target value advertised in a report.
/// </summary>
/// <remarks>
/// As defined by SDS13781 Table 8
/// </remarks>
public readonly struct DurationReport
{
    public DurationReport(byte value)
    {
        Value = value;
    }

    public byte Value { get; }

    public TimeSpan? Duration =>
        Value switch
        {
            // 0 seconds. Already at the Target Value.
            0 => TimeSpan.Zero,

            // 1 second (0x01) to 127 seconds (0x7F) in 1 second resolution.
            >= 0x01 and <= 0x7f => TimeSpan.FromSeconds(Value),

            // 1 minute (0x80) to 126 minutes (0xFD) in 1 minute resolution.
            >= 0x80 and <= 0xfd => TimeSpan.FromMinutes(Value - 0x7f),

            // Unknown duration
            0xfe => null,

            // Reserved. Treat the same as unknown?
            0xff => null,
        };

    public static implicit operator DurationReport(byte b) => new DurationReport(b);
}
