using System.Buffers.Binary;

namespace ZWave.Serial;

/// <summary>
/// The NodeID base type encoding used by the Serial API.
/// </summary>
/// <remarks>
/// As defined by the Z-Wave Host API Specification, SerialApiSetup.SetNodeIdBaseType sub-command.
/// All Z-Wave API Commands MUST use the length defined by this type for encoding NodeID fields.
/// The default NodeID field length MUST be 8 bits.
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

/// <summary>
/// Extension methods for <see cref="NodeIdType"/> to support conditional NodeID field encoding.
/// </summary>
public static class NodeIdTypeExtensions
{
    /// <summary>
    /// Gets the size in bytes of a NodeID field for this base type.
    /// </summary>
    public static int NodeIdSize(this NodeIdType nodeIdType) => nodeIdType switch
    {
        NodeIdType.Short => 1,
        NodeIdType.Long => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(nodeIdType), nodeIdType, "Unknown NodeIdType"),
    };

    /// <summary>
    /// Writes a NodeID to the buffer at the specified offset using the configured encoding.
    /// </summary>
    /// <returns>The offset immediately after the written NodeID field.</returns>
    public static int WriteNodeId(this NodeIdType nodeIdType, Span<byte> buffer, int offset, ushort nodeId)
    {
        if (nodeIdType == NodeIdType.Long)
        {
            nodeId.WriteBytesBE(buffer.Slice(offset, 2));
            return offset + 2;
        }

        buffer[offset] = (byte)nodeId;
        return offset + 1;
    }

    /// <summary>
    /// Reads a NodeID from the buffer at the specified offset using the configured encoding.
    /// </summary>
    /// <returns>The NodeID value.</returns>
    public static ushort ReadNodeId(this NodeIdType nodeIdType, ReadOnlySpan<byte> buffer, int offset)
    {
        if (nodeIdType == NodeIdType.Long)
        {
            return buffer.Slice(offset, 2).ToUInt16BE();
        }

        return buffer[offset];
    }
}
