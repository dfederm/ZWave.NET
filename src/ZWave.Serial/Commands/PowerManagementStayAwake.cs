namespace ZWave.Serial.Commands;

/// <summary>
/// Keep the Z-Wave module awake.
/// </summary>
public readonly struct PowerManagementStayAwakeRequest : ICommand<PowerManagementStayAwakeRequest>
{
    public PowerManagementStayAwakeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.PowerManagementStayAwake;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to keep the Z-Wave module awake.
    /// </summary>
    /// <param name="powerLockType">Which peripheral should be kept awake.</param>
    /// <param name="powerLockTimeoutMs">How long the peripheral should stay awake in ms. 0 = no time limit.</param>
    /// <param name="wakeUpTimerTimeoutMs">If powerLockTimeoutMs is not 0, triggers a wake up event after this timeout in ms. 0 = no wake up event.</param>
    public static PowerManagementStayAwakeRequest Create(
        PowerLockType powerLockType,
        uint powerLockTimeoutMs,
        uint wakeUpTimerTimeoutMs)
    {
        Span<byte> commandParameters = stackalloc byte[9];
        commandParameters[0] = (byte)powerLockType;
        powerLockTimeoutMs.WriteBytesBE(commandParameters[1..5]);
        wakeUpTimerTimeoutMs.WriteBytesBE(commandParameters[5..9]);

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new PowerManagementStayAwakeRequest(frame);
    }

    public static PowerManagementStayAwakeRequest Create(DataFrame frame, CommandParsingContext context) => new PowerManagementStayAwakeRequest(frame);
}
