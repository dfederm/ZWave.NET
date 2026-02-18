namespace ZWave;

/// <summary>
/// Indicates whether a node is a Frequently Listening Routing Slave (FLiRS).
/// </summary>
public enum FrequentListeningMode
{
    /// <summary>
    /// The node is not a FLiRS node.
    /// </summary>
    None,

    /// <summary>
    /// The node wakes up every 1000ms to listen for incoming frames.
    /// </summary>
    Sensor1000ms,

    /// <summary>
    /// The node wakes up every 250ms to listen for incoming frames.
    /// </summary>
    Sensor250ms,
}
