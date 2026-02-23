namespace ZWave.Serial.Commands;

/// <summary>
/// The security mode for the Security Setup command.
/// </summary>
public enum SecuritySetupMode : byte
{
    /// <summary>
    /// Request the security keys.
    /// </summary>
    GetSecurityKeys = 0x00,

    /// <summary>
    /// Request the public DSK derived from the Learn Mode Authenticated ECDH key pair.
    /// </summary>
    GetSecurity2PublicDsk = 0x02,

    /// <summary>
    /// Set the requested security inclusion keys.
    /// </summary>
    SetSecurityInclusionRequestedKeys = 0x05,

    /// <summary>
    /// Request the supported security modes.
    /// </summary>
    GetSecurityCapabilities = 0xFE,
}

/// <summary>
/// Request and provide security keys and authentication during S2 inclusion.
/// This command is only supported by End Node library types.
/// </summary>
public readonly struct SecuritySetupRequest : ICommand<SecuritySetupRequest>
{
    public SecuritySetupRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.SecuritySetup;

    public DataFrame Frame { get; }

    /// <summary>
    /// Create a Security Setup request with no parameters (e.g. GetSecurityKeys, GetSecurity2PublicDsk, GetSecurityCapabilities).
    /// </summary>
    public static SecuritySetupRequest Create(SecuritySetupMode mode)
    {
        ReadOnlySpan<byte> commandParameters = [(byte)mode];
        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SecuritySetupRequest(frame);
    }

    /// <summary>
    /// Create a Security Setup request with parameters (e.g. SetSecurityInclusionRequestedKeys).
    /// </summary>
    public static SecuritySetupRequest Create(SecuritySetupMode mode, ReadOnlySpan<byte> parameters)
    {
        Span<byte> commandParameters = stackalloc byte[2 + parameters.Length];
        commandParameters[0] = (byte)mode;
        commandParameters[1] = (byte)parameters.Length;
        parameters.CopyTo(commandParameters.Slice(2, parameters.Length));

        DataFrame frame = DataFrame.Create(Type, CommandId, commandParameters);
        return new SecuritySetupRequest(frame);
    }

    public static SecuritySetupRequest Create(DataFrame frame, CommandParsingContext context) => new SecuritySetupRequest(frame);
}

/// <summary>
/// Response for the Security Setup command.
/// </summary>
public readonly struct SecuritySetupResponse : ICommand<SecuritySetupResponse>
{
    public SecuritySetupResponse(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.RES;

    public static CommandId CommandId => CommandId.SecuritySetup;

    public DataFrame Frame { get; }

    /// <summary>
    /// The security mode echoed back in the response.
    /// </summary>
    public SecuritySetupMode SecurityMode => (SecuritySetupMode)Frame.CommandParameters.Span[0];

    private byte ParameterLength => Frame.CommandParameters.Span[1];

    /// <summary>
    /// The response parameters. The meaning depends on the <see cref="SecurityMode"/>.
    /// </summary>
    public ReadOnlySpan<byte> Parameters
        => Frame.CommandParameters.Span.Length >= 2 + ParameterLength
            ? Frame.CommandParameters.Span.Slice(2, ParameterLength)
            : [];

    /// <summary>
    /// The bitmask of security keys the node possesses.
    /// Only valid when <see cref="SecurityMode"/> is <see cref="SecuritySetupMode.GetSecurityKeys"/>.
    /// </summary>
    public SecurityKeyFlags SecurityKeys => (SecurityKeyFlags)Parameters[0];

    public static SecuritySetupResponse Create(DataFrame frame, CommandParsingContext context) => new SecuritySetupResponse(frame);
}

