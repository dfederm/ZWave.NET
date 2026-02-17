namespace ZWave.Serial;

public enum DataFrameType : byte
{
    /// <summary>
    /// Request
    /// </summary>
    REQ = 0x00,

    /// <summary>
    /// Response
    /// </summary>
    RES = 0x01,
}
