using System.Security.Cryptography;

namespace ZWave.CommandClasses;

/// <summary>
/// Tracks the Security 0 state for a single node: the (derived) network keys and the pool of
/// nonces issued to and received from peer nodes.
/// </summary>
/// <remarks>
/// Each nonce is 8 random bytes; its first byte (the "nonce ID") is the pool key. Nonces that
/// this node generated have an "issuer" equal to <see cref="_ownNodeId"/> and must be kept
/// (used to decrypt/replay-check inbound commands); nonces received from a peer are stored as
/// "free" and consumed when this node sends to that peer. Nonces expire after <see cref="_nonceTimeout"/>.
/// </remarks>
internal sealed class S0SecurityManager
{
    private readonly record struct NonceEntry(ReadOnlyMemory<byte> Nonce, ushort Receiver, bool Free, DateTimeOffset ExpiresAt);

    private readonly Lock _lock = new();
    private readonly ushort _ownNodeId;
    private readonly TimeSpan _nonceTimeout;
    private readonly Dictionary<(ushort Issuer, byte NonceId), NonceEntry> _nonces = new();
    private byte[]? _networkKey;
    private byte[]? _authKey;
    private byte[]? _encryptionKey;

    public S0SecurityManager(ushort ownNodeId, TimeSpan? nonceTimeout = null)
    {
        _ownNodeId = ownNodeId;
        _nonceTimeout = nonceTimeout ?? TimeSpan.FromSeconds(20);
    }

    /// <summary>
    /// Gets whether a network key has been set for this node.
    /// </summary>
    public bool HasNetworkKey
    {
        get
        {
            lock (_lock)
            {
                return _networkKey is not null;
            }
        }
    }

    /// <summary>
    /// Sets (or replaces) the 16-byte network key and invalidates the cached derived keys.
    /// </summary>
    public void SetNetworkKey(ReadOnlySpan<byte> networkKey)
    {
        if (networkKey.Length != 16)
        {
            ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, "The S0 network key must be 16 bytes long");
        }

        lock (_lock)
        {
            _networkKey = networkKey.ToArray();
            _authKey = null;
            _encryptionKey = null;
        }
    }

    /// <summary>
    /// Gets the derived authentication key (deriving and caching it on first use).
    /// </summary>
    public byte[] GetAuthKey()
    {
        lock (_lock)
        {
            if (_authKey is null)
            {
                _authKey = S0Crypto.DeriveAuthKey(EnsureNetworkKey());
            }

            return _authKey;
        }
    }

    /// <summary>
    /// Gets the derived encryption key (deriving and caching it on first use).
    /// </summary>
    public byte[] GetEncryptionKey()
    {
        lock (_lock)
        {
            if (_encryptionKey is null)
            {
                _encryptionKey = S0Crypto.DeriveEncryptionKey(EnsureNetworkKey());
            }

            return _encryptionKey;
        }
    }

    /// <summary>
    /// Generates a fresh nonce issued by this node, for the specified receiver.
    /// </summary>
    public byte[] GenerateNonce(ushort receiver)
    {
        lock (_lock)
        {
            ExpireStaleNonces();

            byte[] nonce = new byte[8];
            do
            {
                RandomNumberGenerator.Fill(nonce);
            }
            while (_nonces.ContainsKey((_ownNodeId, nonce[0])));

            _nonces[(_ownNodeId, nonce[0])] = new NonceEntry(nonce, receiver, Free: false, DateTimeOffset.UtcNow + _nonceTimeout);
            return nonce;
        }
    }

    /// <summary>
    /// Generates a fresh 8-byte sender nonce for attachment to an outbound encapsulated command.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="GenerateNonce(ushort)"/>, the nonce is not tracked in the pool. A sender
    /// nonce is used only for the current frame's IV and MAC and is carried in the frame itself;
    /// a peer never references it back by ID, so storing it would only let <see cref="GetOwnNonce(byte, ushort)"/>
    /// later return a nonce that was never issued to any peer.
    /// </remarks>
    public byte[] GenerateSenderNonce()
    {
        byte[] nonce = new byte[8];
        RandomNumberGenerator.Fill(nonce);
        return nonce;
    }

    /// <summary>
    /// Stores an 8-byte nonce received from the specified issuer.
    /// </summary>
    public void StoreReceivedNonce(ushort issuer, ReadOnlySpan<byte> nonce)
    {
        if (nonce.Length != 8)
        {
            ZWaveException.Throw(ZWaveErrorCode.CommandInvalidArgument, "The S0 nonce must be 8 bytes long");
        }

        lock (_lock)
        {
            _nonces[(issuer, nonce[0])] = new NonceEntry(nonce.ToArray(), _ownNodeId, Free: true, DateTimeOffset.UtcNow + _nonceTimeout);
        }
    }

    /// <summary>
    /// Removes and returns a usable (free, non-expired) nonce issued by the specified issuer, or
    /// <c>null</c> if none is available.
    /// </summary>
    public ReadOnlySpan<byte> GetUsableNonce(ushort issuer)
    {
        lock (_lock)
        {
            ExpireStaleNonces();

            foreach (KeyValuePair<(ushort Issuer, byte NonceId), NonceEntry> pair in _nonces)
            {
                if (pair.Key.Issuer == issuer && pair.Value.Free)
                {
                    NonceEntry entry = pair.Value;
                    _nonces.Remove(pair.Key);
                    return entry.Nonce.Span;
                }
            }

            return [];
        }
    }

    /// <summary>
    /// Gets this node's own nonce for the given nonce ID (used to decrypt an inbound command),
    /// or <c>null</c> if unknown, expired, or not issued to <paramref name="sourceNodeId"/>.
    /// </summary>
    /// <remarks>
    /// The nonce is only returned when it was issued to <paramref name="sourceNodeId"/>: a nonce
    /// issued to one peer must not be consumable by another, even though all S0 nodes share the
    /// network key (and hence can compute a valid MAC once they learn the nonce value).
    /// </remarks>
    public ReadOnlySpan<byte> GetOwnNonce(byte nonceId, ushort sourceNodeId)
    {
        lock (_lock)
        {
            ExpireStaleNonces();

            if (_nonces.TryGetValue((_ownNodeId, nonceId), out NonceEntry entry)
                && entry.Receiver == sourceNodeId)
            {
                return entry.Nonce.Span;
            }

            return [];
        }
    }

    /// <summary>
    /// Atomically retrieves and consumes this node's own nonce for the given nonce ID, so that a
    /// nonce can be consumed by at most one caller.
    /// </summary>
    /// <remarks>
    /// Under a single lock this expires stale entries, finds the nonce by ID, and verifies it was
    /// issued to <paramref name="sourceNodeId"/>. On success it removes the nonce (and any other
    /// nonce issued to the same peer, per the single-use replay rules) and returns an owned copy
    /// of the nonce bytes. Returns <c>null</c> if the nonce is unknown, expired, or not issued to
    /// <paramref name="sourceNodeId"/>. Batching the lookup and the removal in one critical
    /// section is what makes nonce consumption race-free.
    /// </remarks>
    public byte[]? ConsumeOwnNonce(byte nonceId, ushort sourceNodeId)
    {
        lock (_lock)
        {
            ExpireStaleNonces();

            if (_nonces.TryGetValue((_ownNodeId, nonceId), out NonceEntry entry)
                && entry.Receiver == sourceNodeId)
            {
                _nonces.Remove((_ownNodeId, nonceId));
                DeleteAllNoncesForReceiver(entry.Receiver);
                return entry.Nonce.ToArray();
            }

            return null;
        }
    }

    /// <summary>
    /// Removes all nonces for this node.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _nonces.Clear();
        }
    }

    private void DeleteAllNoncesForReceiver(ushort receiver)
    {
        foreach (KeyValuePair<(ushort Issuer, byte NonceId), NonceEntry> pair in _nonces)
        {
            if (pair.Value.Receiver == receiver)
            {
                _nonces.Remove(pair.Key);
            }
        }
    }

    private void ExpireStaleNonces()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (KeyValuePair<(ushort Issuer, byte NonceId), NonceEntry> pair in _nonces)
        {
            if (pair.Value.ExpiresAt <= now)
            {
                _nonces.Remove(pair.Key);
            }
        }
    }

    private byte[] EnsureNetworkKey()
    {
        if (_networkKey is null)
        {
            ZWaveException.Throw(ZWaveErrorCode.CommandNotReady, "No S0 network key has been set for this node");
        }

        return _networkKey;
    }
}
