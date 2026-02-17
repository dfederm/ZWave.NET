namespace ZWave.Serial.Commands;

public readonly partial struct SerialApiSetupRequest
{
    public static SerialApiSetupRequest GetSupportedCommands()
        => Create(SerialApiSetupSubcommand.GetSupportedCommands, ReadOnlySpan<byte>.Empty);
}

public readonly struct SerialApiSetupGetSupportedCommandsResponse : ICommand<SerialApiSetupGetSupportedCommandsResponse>
{
    public SerialApiSetupGetSupportedCommandsResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SerialApiSetup;

    public DataFrame Frame { get; }

    // The value 0 MUST indicate that the received Z-Wave API setup sub command in the Initial data frame is not supported.
    public bool WasSubcommandSupported => Frame.CommandParameters.Span[0] > 0;

    public HashSet<SerialApiSetupSubcommand> SupportedSubcommands
    {
        get
        {
            var supportedSubcommands = new HashSet<SerialApiSetupSubcommand>();

            // Ensure GetSupportedCommands is considered supported since it was just called.
            supportedSubcommands.Add(SerialApiSetupSubcommand.GetSupportedCommands);

            // The first subcommand parameter is a flags field. Thus it can only advertise
            // support for functions that have identifiers that are powers of 2.
            byte supportedSubcommandFlags = Frame.CommandParameters.Span[1];
            for (int bitNum = 0; bitNum < 8; bitNum++)
            {
                if ((supportedSubcommandFlags & (1 << bitNum)) != 0)
                {
                    // As per the spec, bit 0 corresponds to subcommand 1, so add 1 to the bit number.
                    SerialApiSetupSubcommand subcommand = (SerialApiSetupSubcommand)(1 << (bitNum + 1));
                    supportedSubcommands.Add(subcommand);
                }
            }

            // The remaining payload, if it exists, describes the extended bitmask
            if (Frame.CommandParameters.Span.Length > 2)
            {
                var bitMask = Frame.CommandParameters.Span[2..];

                for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
                {
                    for (int bitNum = 0; bitNum < 8; bitNum++)
                    {
                        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
                        {
                            // As per the spec, bit 0 corresponds to subcommand 1, so we need to add 1.
                            SerialApiSetupSubcommand subcommand = (SerialApiSetupSubcommand)((byteNum << 3) + bitNum + 1);
                            supportedSubcommands.Add(subcommand);
                        }
                    }
                }
            }

            supportedSubcommands.TrimExcess();

            return supportedSubcommands;
        }
    }

    public static SerialApiSetupGetSupportedCommandsResponse Create(DataFrame frame) => new SerialApiSetupGetSupportedCommandsResponse(frame);
}
