using System.Runtime.InteropServices;

namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    /// <summary>
    /// Create a request to get the list of supported RF regions.
    /// </summary>
    public static SerialApiSetupRequest GetSupportedRegions()
        => Create(SerialApiSetupSubcommand.GetSupportedRegions, []);
}

/// <summary>
/// Response to a GetSupportedRegions sub-command.
/// </summary>
public readonly struct SerialApiSetupGetSupportedRegionsResponse : ICommand<SerialApiSetupGetSupportedRegionsResponse>
{
    public SerialApiSetupGetSupportedRegionsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    /// <summary>
    /// Indicates whether the sub-command was supported by the Z-Wave module.
    /// </summary>
    public bool WasSubcommandSupported => Frame.CommandParameters.Span[0] > 0;

    /// <summary>
    /// Gets the number of supported regions.
    /// </summary>
    public byte Count => Frame.CommandParameters.Span[1];

    /// <summary>
    /// Gets the list of supported RF regions.
    /// </summary>
    public ReadOnlySpan<RfRegion> GetSupportedRegions()
        => MemoryMarshal.Cast<byte, RfRegion>(Frame.CommandParameters.Span[2..(2 + Count)]);

    public static SerialApiSetupGetSupportedRegionsResponse Create(DataFrame frame) => new SerialApiSetupGetSupportedRegionsResponse(frame);
}
