using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

public sealed partial class Security0CommandClass
{
    /// <summary>
    /// Sets the 16-byte S0 network key locally for this node.
    /// </summary>
    /// <remarks>
    /// This is used for nodes whose network key is already known (e.g. restored from persistence).
    /// During inclusion the key is instead received from the node via the Network Key Set command
    /// (spec §3.5.4), which is a follow-up.
    /// </remarks>
    public void SetNetworkKey(ReadOnlySpan<byte> networkKey)
        => _manager.SetNetworkKey(networkKey);

    /// <summary>
    /// Gets whether a network key has been set for this node.
    /// </summary>
    public bool HasNetworkKey => _manager.HasNetworkKey;

    /// <summary>
    /// Network Key Set command (spec §3.5.4.1). Carries the 16-byte network key.
    /// </summary>
    internal readonly struct NetworkKeySetCommand : ICommand
    {
        public NetworkKeySetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.NetworkKeySet;

        public CommandClassFrame Frame { get; }

        public static NetworkKeySetCommand Create(ReadOnlySpan<byte> networkKey)
        {
            if (networkKey.Length != 16)
            {
                ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, "The S0 network key must be 16 bytes long");
            }

            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, networkKey);
            return new NetworkKeySetCommand(frame);
        }

        public static byte[] Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length != 16)
            {
                logger.LogWarning("Network Key Set frame is malformed ({Length} bytes, expected 16)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Network Key Set frame is malformed");
            }

            return frame.CommandParameters.ToArray();
        }
    }

    /// <summary>
    /// Network Key Verify command (spec §3.5.3.5). No parameters; sent by the node after it
    /// successfully decrypts the Network Key Set.
    /// </summary>
    internal readonly struct NetworkKeyVerifyCommand : ICommand
    {
        public NetworkKeyVerifyCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.NetworkKeyVerify;

        public CommandClassFrame Frame { get; }

        public static NetworkKeyVerifyCommand Create()
            => new(CommandClassFrame.Create(CommandClassId, CommandId));
    }

    /// <summary>
    /// Scheme Inherit command (spec §3.5.3.6). Carries the Supported Security Schemes byte.
    /// </summary>
    internal readonly struct SchemeInheritCommand : ICommand
    {
        public SchemeInheritCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.Security0;

        public static byte CommandId => (byte)Security0Command.SchemeInherit;

        public CommandClassFrame Frame { get; }

        public static SchemeInheritCommand Create()
            => new(CommandClassFrame.Create(CommandClassId, CommandId, [SupportedSecuritySchemesS0]));
    }
}
