namespace ZWave.Serial.Commands;

/// <summary>
/// Notify the application of security events.
/// </summary>
public readonly struct ApplicationSecurityEventRequest : ICommand<ApplicationSecurityEventRequest>
{
    public ApplicationSecurityEventRequest(DataFrame frame)
    {
        Frame = frame;
    }

    public static DataFrameType Type => DataFrameType.REQ;

    public static CommandId CommandId => CommandId.ApplicationSecurityEvent;

    public DataFrame Frame { get; }

    /// <summary>
    /// The security event type.
    /// </summary>
    public byte Event => Frame.CommandParameters.Span[0];

    /// <summary>
    /// The node ID associated with the security event.
    /// </summary>
    public ushort NodeId => Frame.CommandParameters.Span[1];

    public static ApplicationSecurityEventRequest Create(DataFrame frame) => new ApplicationSecurityEventRequest(frame);
}
