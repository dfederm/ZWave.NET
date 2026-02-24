namespace ZWave.Serial.Commands;

/// <summary>
/// Get the Z-Wave library type.
/// </summary>
public readonly struct GetLibraryTypeRequest : ICommand<GetLibraryTypeRequest>
{
    public GetLibraryTypeRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.GetLibraryType;

    public DataFrame Frame { get; }

    public static GetLibraryTypeRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new GetLibraryTypeRequest(frame);
    }

    public static GetLibraryTypeRequest Create(DataFrame frame, CommandParsingContext context) => new GetLibraryTypeRequest(frame);
}

public readonly struct GetLibraryTypeResponse : ICommand<GetLibraryTypeResponse>
{
    public GetLibraryTypeResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.GetLibraryType;

    public DataFrame Frame { get; }

    /// <summary>
    /// The library type that runs on the Z-Wave Module.
    /// </summary>
    public LibraryType LibraryType => (LibraryType)Frame.CommandParameters.Span[0];

    public static GetLibraryTypeResponse Create(DataFrame frame, CommandParsingContext context) => new GetLibraryTypeResponse(frame);
}
