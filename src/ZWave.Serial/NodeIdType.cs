namespace ZWave.Serial;

/// <summary>
/// The NodeID base type encoding used by the Serial API.
/// </summary>
/// <remarks>
/// As defined by the Z-Wave Host API Specification, SerialApiSetup.SetNodeIdBaseType sub-command.
/// </remarks>
public enum NodeIdType : byte
{
    /// <summary>
    /// 8-bit NodeID encoding (classic Z-Wave).
    /// </summary>
    Short = 0x01,

    /// <summary>
    /// 16-bit NodeID encoding (required for Z-Wave Long Range).
    /// </summary>
    Long = 0x02,
}
