namespace ZWave.Serial.Commands;

/// <summary>
/// Enables the Auto Program Mode and resets the Z-Wave SOC.
/// </summary>
public readonly struct FlashAutoProgSetRequest : ICommand<FlashAutoProgSetRequest>
{
    public FlashAutoProgSetRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.FlashAutoProgSet;

    public DataFrame Frame { get; }

    public static FlashAutoProgSetRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new FlashAutoProgSetRequest(frame);
    }

    public static FlashAutoProgSetRequest Create(DataFrame frame) => new FlashAutoProgSetRequest(frame);
}
