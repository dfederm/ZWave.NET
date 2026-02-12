namespace ZWave.Serial.Commands;

/// <summary>
/// The Firmware Update API provides functionality for implementing firmware update.
/// </summary>
public readonly struct FirmwareUpdateRequest : ICommand<FirmwareUpdateRequest>
{
    public FirmwareUpdateRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.FirmwareUpdate;

    public DataFrame Frame { get; }

    /// <summary>
    /// The raw firmware update data.
    /// </summary>
    public ReadOnlyMemory<byte> Data => Frame.CommandParameters;

    public static FirmwareUpdateRequest Create(DataFrame frame) => new FirmwareUpdateRequest(frame);
}
