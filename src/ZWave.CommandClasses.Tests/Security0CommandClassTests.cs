namespace ZWave.CommandClasses.Tests;

[TestClass]
public partial class Security0CommandClassTests
{
    private static byte[] NetworkKey => Convert.FromHexString("2b7e151628aed2a6abf7158809cf4f3c");

    private static string Hex(byte[] value) => Convert.ToHexString(value).ToLowerInvariant();

    private static (S0SecurityManager Sender, S0SecurityManager Receiver) CreateManagers()
    {
        S0SecurityManager sender = new(ownNodeId: 1);
        S0SecurityManager receiver = new(ownNodeId: 2);
        sender.SetNetworkKey(NetworkKey);
        receiver.SetNetworkKey(NetworkKey);
        return (sender, receiver);
    }
}
