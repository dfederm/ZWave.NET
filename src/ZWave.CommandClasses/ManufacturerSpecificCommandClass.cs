using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the type of device ID.
/// </summary>
public enum ManufacturerSpecificDeviceIdType : byte
{
    FactoryDefault = 0x00,

    SerialNumber = 0x01,

    PseudoRandom = 0x02,
}

public enum ManufacturerSpecificCommand : byte
{
    /// <summary>
    /// Request manufacturer specific information from another node.
    /// </summary>
    Get = 0x04,

    /// <summary>
    /// Advertise manufacturer specific device information.
    /// </summary>
    Report = 0x05,

    /// <summary>
    /// Request device specific information.
    /// </summary>
    DeviceSpecificGet = 0x06,

    /// <summary>
    /// Advertise device specific information.
    /// </summary>
    DeviceSpecificReport = 0x07,
}

/// <summary>
/// Represents manufacturer identification information.
/// </summary>
public readonly record struct ManufacturerInformation(
    /// <summary>
    /// The unique ID identifying the manufacturer of the device.
    /// </summary>
    ushort ManufacturerId,

    /// <summary>
    /// A unique ID identifying the actual product type.
    /// </summary>
    ushort ProductTypeId,

    /// <summary>
    /// A unique ID identifying the actual product.
    /// </summary>
    ushort ProductId);

[CommandClass(CommandClassId.ManufacturerSpecific)]
public sealed class ManufacturerSpecificCommandClass : CommandClass<ManufacturerSpecificCommand>
{
    private readonly Dictionary<ManufacturerSpecificDeviceIdType, string> _deviceIds = new();

    public ManufacturerSpecificCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the manufacturer identification information.
    /// </summary>
    public ManufacturerInformation? ManufacturerInformation { get; private set; }

    /// <summary>
    /// Gets the device-specific IDs reported by the device.
    /// </summary>
    public IReadOnlyDictionary<ManufacturerSpecificDeviceIdType, string> DeviceIds => _deviceIds;

    /// <inheritdoc />
    public override bool? IsCommandSupported(ManufacturerSpecificCommand command)
        => command switch
        {
            ManufacturerSpecificCommand.Get => true,
            ManufacturerSpecificCommand.DeviceSpecificGet => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    public async Task<ManufacturerInformation> GetAsync(CancellationToken cancellationToken)
    {
        ManufacturerSpecificGetCommand command = ManufacturerSpecificGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ManufacturerSpecificReportCommand>(cancellationToken).ConfigureAwait(false);
        ManufacturerInformation info = ManufacturerSpecificReportCommand.Parse(reportFrame, Logger);
        ManufacturerInformation = info;
        return info;
    }

    public async Task<string> GetDeviceIdAsync(ManufacturerSpecificDeviceIdType deviceIdType, CancellationToken cancellationToken)
    {
        ManufacturerSpecificDeviceSpecificGetCommand command = ManufacturerSpecificDeviceSpecificGetCommand.Create(deviceIdType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        // From the spec:
        //      In case the Device ID Type specified in a Device Specific Get command is not supported by the 
        //      responding node, the responding node MAY return the factory default Device ID Type (as if receiving 
        //      the value 0 in the Device Specific Get command).
        // So we can't check the device id type from the report with the one provided, nor can we use the provided device id type as a key to lookup in _deviceIds.
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ManufacturerSpecificDeviceSpecificReportCommand>(cancellationToken).ConfigureAwait(false);
        ManufacturerSpecificDeviceIdType reportedDeviceIdType = ManufacturerSpecificDeviceSpecificReportCommand.ParseDeviceIdType(reportFrame, Logger);
        string deviceId = ManufacturerSpecificDeviceSpecificReportCommand.Parse(reportFrame, Logger);
        _deviceIds[reportedDeviceIdType] = deviceId;
        return deviceId;
    }

    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);

        // TODO: Does this CC need to be interviewed before Version CC (zwavejs does this)?
        //       If so, we can't really make this call.
        if (IsCommandSupported(ManufacturerSpecificCommand.DeviceSpecificGet).GetValueOrDefault())
        {
            // Spec: A sending node SHOULD specify a value of zero when issuing the Device Specific Get command since the responding node may only be able to return one Device ID Type.
            _ = await GetDeviceIdAsync(ManufacturerSpecificDeviceIdType.FactoryDefault, cancellationToken).ConfigureAwait(false);
        }
    }

    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        switch ((ManufacturerSpecificCommand)frame.CommandId)
        {
            case ManufacturerSpecificCommand.Get:
            case ManufacturerSpecificCommand.DeviceSpecificGet:
            {
                break;
            }
            case ManufacturerSpecificCommand.Report:
            {
                ManufacturerInformation = ManufacturerSpecificReportCommand.Parse(frame, Logger);
                break;
            }
            case ManufacturerSpecificCommand.DeviceSpecificReport:
            {
                ManufacturerSpecificDeviceIdType deviceIdType = ManufacturerSpecificDeviceSpecificReportCommand.ParseDeviceIdType(frame, Logger);
                _deviceIds[deviceIdType] = ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, Logger);
                break;
            }
        }
    }

    private readonly struct ManufacturerSpecificGetCommand : ICommand
    {
        public ManufacturerSpecificGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ManufacturerSpecific;

        public static byte CommandId => (byte)ManufacturerSpecificCommand.Get;

        public CommandClassFrame Frame { get; }

        public static ManufacturerSpecificGetCommand Create()
        {
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
            return new ManufacturerSpecificGetCommand(frame);
        }
    }

    private readonly struct ManufacturerSpecificReportCommand : ICommand
    {
        public ManufacturerSpecificReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ManufacturerSpecific;

        public static byte CommandId => (byte)ManufacturerSpecificCommand.Report;

        public CommandClassFrame Frame { get; }

        public static ManufacturerInformation Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 6)
            {
                logger.LogWarning("Manufacturer Specific Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Manufacturer Specific Report frame is too short");
            }

            ushort manufacturerId = frame.CommandParameters.Span[0..2].ToUInt16BE();
            ushort productTypeId = frame.CommandParameters.Span[2..4].ToUInt16BE();
            ushort productId = frame.CommandParameters.Span[4..6].ToUInt16BE();
            return new ManufacturerInformation(manufacturerId, productTypeId, productId);
        }
    }

    private readonly struct ManufacturerSpecificDeviceSpecificGetCommand : ICommand
    {
        public ManufacturerSpecificDeviceSpecificGetCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ManufacturerSpecific;

        public static byte CommandId => (byte)ManufacturerSpecificCommand.DeviceSpecificGet;

        public CommandClassFrame Frame { get; }

        public static ManufacturerSpecificDeviceSpecificGetCommand Create(ManufacturerSpecificDeviceIdType deviceIdType)
        {
            ReadOnlySpan<byte> commandParameters = [(byte)deviceIdType];
            CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
            return new ManufacturerSpecificDeviceSpecificGetCommand(frame);
        }
    }

    private readonly struct ManufacturerSpecificDeviceSpecificReportCommand : ICommand
    {
        public ManufacturerSpecificDeviceSpecificReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ManufacturerSpecific;

        public static byte CommandId => (byte)ManufacturerSpecificCommand.DeviceSpecificReport;

        public CommandClassFrame Frame { get; }

        public static ManufacturerSpecificDeviceIdType ParseDeviceIdType(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 1)
            {
                logger.LogWarning("Manufacturer Specific Device Specific Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Manufacturer Specific Device Specific Report frame is too short");
            }

            return (ManufacturerSpecificDeviceIdType)(frame.CommandParameters.Span[0] & 0b0000_0111);
        }

        public static string Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Manufacturer Specific Device Specific Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                throw new ZWaveException(ZWaveErrorCode.InvalidPayload, "Manufacturer Specific Device Specific Report frame is too short");
            }

            int deviceIdDataLength = frame.CommandParameters.Span[1] & 0b0001_1111;
            ReadOnlySpan<byte> deviceIdData = frame.CommandParameters.Span.Slice(2, deviceIdDataLength);

            int deviceIdDataFormat = (frame.CommandParameters.Span[1] & 0b1110_0000) >> 5;
            if (deviceIdDataFormat == 0)
            {
                // UTF-8
                return Encoding.UTF8.GetString(deviceIdData);
            }
            else
            {
                // Binary
                return $"0x{Convert.ToHexString(deviceIdData)}";
            }
        }
    }
}
