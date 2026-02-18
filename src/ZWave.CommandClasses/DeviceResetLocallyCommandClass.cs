namespace ZWave.CommandClasses;

public enum DeviceResetLocallyCommand : byte
{
    /// <summary>
    /// Notify other nodes that the device has been reset to factory defaults.
    /// </summary>
    Notification = 0x01,
}

/// <summary>
/// Represents the Device Reset Locally Command Class.
/// </summary>
/// <remarks>
/// A device sends the Notification command unsolicited when it has been reset to factory defaults.
/// The controller does not send any commands to the device for this command class.
/// </remarks>
[CommandClass(CommandClassId.DeviceResetLocally)]
public sealed class DeviceResetLocallyCommandClass : CommandClass<DeviceResetLocallyCommand>
{
    internal DeviceResetLocallyCommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the device has been reset to factory defaults.
    /// </summary>
    public bool WasReset { get; private set; }

    /// <inheritdoc />
    public override bool? IsCommandSupported(DeviceResetLocallyCommand command) => false;

    internal override Task InterviewAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        switch ((DeviceResetLocallyCommand)frame.CommandId)
        {
            case DeviceResetLocallyCommand.Notification:
            {
                _ = new DeviceResetLocallyNotificationCommand(frame);
                WasReset = true;
                break;
            }
        }
    }

    private readonly struct DeviceResetLocallyNotificationCommand : ICommand
    {
        public DeviceResetLocallyNotificationCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.DeviceResetLocally;

        public static byte CommandId => (byte)DeviceResetLocallyCommand.Notification;

        public CommandClassFrame Frame { get; }
    }
}
