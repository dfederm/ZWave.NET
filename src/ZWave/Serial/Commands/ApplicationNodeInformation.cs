namespace ZWave.Serial.Commands;

/// <summary>
/// Generate the Node Information frame and to save information about node capabilities.
/// </summary>
public readonly struct ApplicationNodeInformationRequest : ICommand<ApplicationNodeInformationRequest>
{
    public ApplicationNodeInformationRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationNodeInformation;

    public DataFrame Frame { get; }

    public static ApplicationNodeInformationRequest Create(
        byte deviceOptionMask,
        byte genericType,
        byte specificType,
        ReadOnlySpan<byte> commandClasses)
    {
        Span<byte> commandParameters = stackalloc byte[4 + commandClasses.Length];
        commandParameters[0] = deviceOptionMask;
        commandParameters[1] = genericType;
        commandParameters[2] = specificType;
        commandParameters[3] = (byte)commandClasses.Length;
        commandClasses.CopyTo(commandParameters.Slice(4, commandClasses.Length));

        var frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new ApplicationNodeInformationRequest(frame);
    }

    public static ApplicationNodeInformationRequest Create(DataFrame frame) => new ApplicationNodeInformationRequest(frame);
}
