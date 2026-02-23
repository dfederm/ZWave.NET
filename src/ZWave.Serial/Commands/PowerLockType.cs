namespace ZWave.Serial.Commands;

/// <summary>
/// The power lock type for power management commands.
/// </summary>
public enum PowerLockType : byte
{
    /// <summary>
    /// Radio peripheral.
    /// </summary>
    Radio = 0,

    /// <summary>
    /// IO peripheral.
    /// </summary>
    IO = 1,
}
