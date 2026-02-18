namespace ZWave.CommandClasses;

/// <summary>
/// An interpreted value from or for a node
/// </summary>
/// <remarks>
/// As defined by SDS13781 Table 21, Table 82
/// </remarks>
public struct GenericValue
{
    public GenericValue(byte value)
    {
        Value = value;
    }

    public GenericValue(int level)
    {
        if (level < 0 || level > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The value must be in the range [0..100]");
        }

        Value = level == 100 ? (byte)0xff : (byte)level;
    }

    public GenericValue(bool state)
    {
        Value = state ? (byte)0xff : (byte)0;
    }

    /// <summary>
    /// Gets the raw byte value.
    /// </summary>
    public byte Value { get; }

    /// <summary>
    /// Gets the interpreted level (0-100), or null if unknown.
    /// </summary>
    public int? Level => Value switch
    {
        <= 99 => Value,
        0xfe => null, // Unknown
        0xff => 100,
        _ => null, // Reserved. Treat as unknown
    };

    /// <summary>
    /// Gets the interpreted boolean state, or null if unknown.
    /// </summary>
    public bool? State => Value switch
    {
        0 => false,
        <= 99 => true,
        0xfe => null, // Unknown
        0xff => true,
        _ => null, // Reserved. Treat as unknown
    };

    public static implicit operator GenericValue(byte b) => new GenericValue(b);

    public static implicit operator GenericValue(int i) => new GenericValue(i);

    public static implicit operator GenericValue(bool b) => new GenericValue(b);
}
