namespace ZWave.CommandClasses;

/// <summary>
/// Shared encoding/decoding logic for Z-Wave duration fields.
/// </summary>
/// <remarks>
/// Multiple Z-Wave command classes use a common seconds/minutes encoding scheme:
/// <list type="bullet">
/// <item><description><c>0x00</c>: Instantly (<see cref="TimeSpan.Zero"/>)</description></item>
/// <item><description><c>0x01</c>–<c>0x7F</c>: 1 to 127 seconds</description></item>
/// <item><description><c>0x80</c>–<c>maxMinuteByte</c>: 1 to N minutes</description></item>
/// </list>
/// The upper bound of the minutes range varies by command class (e.g. 0xFD for generic Duration, 0xFE for Scene dimming duration).
/// Values above the minutes range have command-class-specific meanings (Unknown, Factory Default, etc.).
/// </remarks>
internal static class DurationEncoding
{
    /// <summary>
    /// Decodes a duration byte using the common seconds/minutes encoding.
    /// </summary>
    /// <param name="value">The raw duration byte.</param>
    /// <param name="maxMinuteByte">
    /// The highest byte value that encodes minutes.
    /// Use <c>0xFD</c> for generic Duration (Table 8), <c>0xFE</c> for Scene dimming duration (Tables 2.496/2.497).
    /// </param>
    /// <returns>The decoded duration, or <see langword="null"/> for values above <paramref name="maxMinuteByte"/>.</returns>
    internal static TimeSpan? Decode(byte value, byte maxMinuteByte)
    {
        return value switch
        {
            0 => TimeSpan.Zero,
            >= 0x01 and <= 0x7F => TimeSpan.FromSeconds(value),
            _ when value >= 0x80 && value <= maxMinuteByte => TimeSpan.FromMinutes(value - 0x7F),
            _ => null,
        };
    }

    /// <summary>
    /// Encodes a <see cref="TimeSpan"/> as a duration byte using the common seconds/minutes encoding.
    /// </summary>
    /// <param name="duration">The duration to encode. Must be between <see cref="TimeSpan.Zero"/> and 127 minutes.</param>
    /// <returns>The encoded duration byte.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="duration"/> exceeds 127 minutes.</exception>
    internal static byte Encode(TimeSpan duration)
    {
        if (duration == TimeSpan.Zero)
        {
            return 0;
        }

        if (duration <= TimeSpan.FromSeconds(127))
        {
            return (byte)Math.Round(duration.TotalSeconds);
        }

        if (duration <= TimeSpan.FromMinutes(127))
        {
            return (byte)(Math.Round(duration.TotalMinutes) + 0x7F);
        }

        throw new ArgumentException("Value must be less than or equal to 127 minutes", nameof(duration));
    }
}
