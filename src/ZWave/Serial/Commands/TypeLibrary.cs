namespace ZWave.Serial.Commands;

/// <summary>
/// Get the Z-Wave library type.
/// </summary>
public readonly struct TypeLibraryRequest : ICommand<TypeLibraryRequest>
{
    public TypeLibraryRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.TypeLibrary;

    public DataFrame Frame { get; }

    public static TypeLibraryRequest Create()
    {
        var frame = DataFrame.Create(Type, CommandId);
        return new TypeLibraryRequest(frame);
    }

    public static TypeLibraryRequest Create(DataFrame frame) => new TypeLibraryRequest(frame);
}

public readonly struct TypeLibraryResponse : ICommand<TypeLibraryResponse>
{
    public TypeLibraryResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.TypeLibrary;

    public DataFrame Frame { get; }

    /// <summary>
    /// The library type that runs on the Z-Wave Module.
    /// </summary>
    public LibraryType LibraryType => (LibraryType)Frame.CommandParameters.Span[0];

    public static TypeLibraryResponse Create(DataFrame frame) => new TypeLibraryResponse(frame);
}
