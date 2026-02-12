namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    public static SerialApiSetupRequest SetTxStatusReport(bool enable)
    {
        ReadOnlySpan<byte> subcommandParameters = [(byte)(enable ? 1 : 0)];
        return Create(SerialApiSetupSubcommand.SetTxStatusReport, subcommandParameters);
    }
}

public readonly struct SerialApiSetupSetTxStatusReportResponse : ICommand<SerialApiSetupSetTxStatusReportResponse>
{
    public SerialApiSetupSetTxStatusReportResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    // The value 0 MUST indicate that the received Z-Wave API setup sub command in the Initial data frame is not supported.
    public bool WasSubcommandSupported => Frame.CommandParameters.Span[0] > 0;

    public bool Success => Frame.CommandParameters.Span[1] != 0;

    public static SerialApiSetupSetTxStatusReportResponse Create(DataFrame frame) => new SerialApiSetupSetTxStatusReportResponse(frame);
}
