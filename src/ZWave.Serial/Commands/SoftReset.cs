namespace ZWave.Serial.Commands;

public readonly struct SoftResetRequest : ICommand<SoftResetRequest>
{
    public SoftResetRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SoftReset;

    public DataFrame Frame { get; }

    public static SoftResetRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new SoftResetRequest(frame);
    }

    public static SoftResetRequest Create(DataFrame frame) => new SoftResetRequest(frame);
}
