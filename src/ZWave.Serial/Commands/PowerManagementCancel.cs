namespace ZWave.Serial.Commands;

/// <summary>
/// Cancel a power lock set with the Power Management Stay Awake Command.
/// </summary>
public readonly struct PowerManagementCancelRequest : ICommand<PowerManagementCancelRequest>
{
    public PowerManagementCancelRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.PowerManagementCancel;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to cancel a power lock.
    /// </summary>
    /// <param name="powerLockType">Which peripheral should have the power lock cancelled.</param>
    public static PowerManagementCancelRequest Create(PowerLockType powerLockType)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)powerLockType];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new PowerManagementCancelRequest(frame);
    }

    public static PowerManagementCancelRequest Create(DataFrame frame, CommandParsingContext context) => new PowerManagementCancelRequest(frame);
}
