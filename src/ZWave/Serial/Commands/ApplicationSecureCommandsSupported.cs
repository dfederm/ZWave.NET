namespace ZWave.Serial.Commands;

/// <summary>
/// Notify the protocol of the command classes it supports using each security key.
/// </summary>
public readonly struct ApplicationSecureCommandsSupportedRequest : ICommand<ApplicationSecureCommandsSupportedRequest>
{
    public ApplicationSecureCommandsSupportedRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationSecureCommandsSupported;

    public DataFrame Frame { get; }

    public static ApplicationSecureCommandsSupportedRequest Create(
        byte securityKeysBitmask,
        ReadOnlySpan<byte> commandClasses)
    {
        Span<byte> commandParameters = stackalloc byte[2 + commandClasses.Length];
        commandParameters[0] = securityKeysBitmask;
        commandParameters[1] = (byte)commandClasses.Length;
        commandClasses.CopyTo(commandParameters.Slice(2, commandClasses.Length));

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ApplicationSecureCommandsSupportedRequest(frame);
    }

    public static ApplicationSecureCommandsSupportedRequest Create(DataFrame frame) => new ApplicationSecureCommandsSupportedRequest(frame);
}
