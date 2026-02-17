namespace ZWave.Serial.Commands;

/// <summary>
/// Sends command completed to sending controller. Called in replication mode when a command
/// from the sender has been processed.
/// </summary>
public readonly struct ReplicationReceiveCompleteRequest : ICommand<ReplicationReceiveCompleteRequest>
{
    public ReplicationReceiveCompleteRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ReplicationReceiveComplete;

    public DataFrame Frame { get; }

    public static ReplicationReceiveCompleteRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new ReplicationReceiveCompleteRequest(frame);
    }

    public static ReplicationReceiveCompleteRequest Create(DataFrame frame) => new ReplicationReceiveCompleteRequest(frame);
}
