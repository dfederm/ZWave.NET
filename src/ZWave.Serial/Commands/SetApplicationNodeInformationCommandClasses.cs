namespace ZWave.Serial.Commands;

/// <summary>
/// Configure the list of supported Command Classes for each inclusion state.
/// This command is only supported by End Node library types.
/// </summary>
public readonly struct SetApplicationNodeInformationCommandClassesRequest : ICommand<SetApplicationNodeInformationCommandClassesRequest>
{
    public SetApplicationNodeInformationCommandClassesRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SetApplicationNodeInformationCommandClasses;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a request to set the application node information command classes.
    /// </summary>
    /// <param name="notIncludedCommandClasses">Command classes supported before the node is included in a network.</param>
    /// <param name="nonSecurelyIncludedCommandClasses">Non-securely supported command classes after inclusion.</param>
    /// <param name="securelyIncludedCommandClasses">Securely supported command classes after inclusion.</param>
    public static SetApplicationNodeInformationCommandClassesRequest Create(
        ReadOnlySpan<byte> notIncludedCommandClasses,
        ReadOnlySpan<byte> nonSecurelyIncludedCommandClasses,
        ReadOnlySpan<byte> securelyIncludedCommandClasses)
    {
        int totalLength = 1 + notIncludedCommandClasses.Length
            + 1 + nonSecurelyIncludedCommandClasses.Length
            + 1 + securelyIncludedCommandClasses.Length;
        Span<byte> commandParameters = stackalloc byte[totalLength];

        int offset = 0;
        commandParameters[offset++] = (byte)notIncludedCommandClasses.Length;
        notIncludedCommandClasses.CopyTo(commandParameters.Slice(offset, notIncludedCommandClasses.Length));
        offset += notIncludedCommandClasses.Length;

        commandParameters[offset++] = (byte)nonSecurelyIncludedCommandClasses.Length;
        nonSecurelyIncludedCommandClasses.CopyTo(commandParameters.Slice(offset, nonSecurelyIncludedCommandClasses.Length));
        offset += nonSecurelyIncludedCommandClasses.Length;

        commandParameters[offset++] = (byte)securelyIncludedCommandClasses.Length;
        securelyIncludedCommandClasses.CopyTo(commandParameters.Slice(offset, securelyIncludedCommandClasses.Length));

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SetApplicationNodeInformationCommandClassesRequest(frame);
    }

    public static SetApplicationNodeInformationCommandClassesRequest Create(DataFrame frame, CommandParsingContext context) => new SetApplicationNodeInformationCommandClassesRequest(frame);
}
