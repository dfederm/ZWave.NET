using System.Text;
using Microsoft.Extensions.Logging;

namespace ZWave.CommandClasses;

/// <summary>
/// Identifies the type of device ID.
/// </summary>
public enum ManufacturerSpecificDeviceIdType : byte
{
    /// <summary>
    /// Return OEM factory default Device ID Type.
    /// </summary>
    FactoryDefault = 0x00,

    /// <summary>
    /// Serial Number.
    /// </summary>
    SerialNumber = 0x01,

    /// <summary>
    /// Pseudo Random.
    /// </summary>
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

/// <summary>
/// Represents a Device Specific Report received from a device.
/// </summary>
public readonly record struct DeviceSpecificReport(
    /// <summary>
    /// The type of device ID reported by the device.
    /// </summary>
    ManufacturerSpecificDeviceIdType DeviceIdType,

    /// <summary>
    /// The device ID value, formatted as a string.
    /// UTF-8 data is returned as-is; binary data is returned as a hex string prefixed with "0x".
    /// </summary>
    string DeviceId);

[CommandClass(CommandClassId.ManufacturerSpecific)]
public sealed class ManufacturerSpecificCommandClass : CommandClass<ManufacturerSpecificCommand>
{
    private readonly Dictionary<ManufacturerSpecificDeviceIdType, string> _deviceIds = new();

    internal ManufacturerSpecificCommandClass(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Raised when a Manufacturer Specific Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<ManufacturerInformation>? OnManufacturerInformationReceived;

    /// <summary>
    /// Raised when a Device Specific Report is received, whether solicited or unsolicited.
    /// </summary>
    public event Action<DeviceSpecificReport>? OnDeviceSpecificReportReceived;

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

    /// <summary>
    /// Requests the manufacturer specific information from the device.
    /// </summary>
    public async Task<ManufacturerInformation> GetAsync(CancellationToken cancellationToken)
    {
        ManufacturerSpecificGetCommand command = ManufacturerSpecificGetCommand.Create();
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ManufacturerSpecificReportCommand>(cancellationToken).ConfigureAwait(false);
        ManufacturerInformation info = ManufacturerSpecificReportCommand.Parse(reportFrame, Logger);
        ManufacturerInformation = info;
        OnManufacturerInformationReceived?.Invoke(info);
        return info;
    }

    /// <summary>
    /// Requests device specific information for the given device ID type.
    /// </summary>
    /// <remarks>
    /// Per the spec, if the requested device ID type is not supported by the device,
    /// the device MAY return the factory default device ID type instead.
    /// </remarks>
    public async Task<DeviceSpecificReport> GetDeviceIdAsync(ManufacturerSpecificDeviceIdType deviceIdType, CancellationToken cancellationToken)
    {
        ManufacturerSpecificDeviceSpecificGetCommand command = ManufacturerSpecificDeviceSpecificGetCommand.Create(deviceIdType);
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        CommandClassFrame reportFrame = await AwaitNextReportAsync<ManufacturerSpecificDeviceSpecificReportCommand>(cancellationToken).ConfigureAwait(false);
        DeviceSpecificReport report = ManufacturerSpecificDeviceSpecificReportCommand.Parse(reportFrame, Logger);
        _deviceIds[report.DeviceIdType] = report.DeviceId;
        OnDeviceSpecificReportReceived?.Invoke(report);
        return report;
    }

    internal override CommandClassCategory Category => CommandClassCategory.Management;

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
            case ManufacturerSpecificCommand.Report:
            {
                ManufacturerInformation info = ManufacturerSpecificReportCommand.Parse(frame, Logger);
                ManufacturerInformation = info;
                OnManufacturerInformationReceived?.Invoke(info);
                break;
            }
            case ManufacturerSpecificCommand.DeviceSpecificReport:
            {
                DeviceSpecificReport report = ManufacturerSpecificDeviceSpecificReportCommand.Parse(frame, Logger);
                _deviceIds[report.DeviceIdType] = report.DeviceId;
                OnDeviceSpecificReportReceived?.Invoke(report);
                break;
            }
        }
    }

    internal readonly struct ManufacturerSpecificGetCommand : ICommand
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

    internal readonly struct ManufacturerSpecificReportCommand : ICommand
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
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Manufacturer Specific Report frame is too short");
            }

            ushort manufacturerId = frame.CommandParameters.Span[0..2].ToUInt16BE();
            ushort productTypeId = frame.CommandParameters.Span[2..4].ToUInt16BE();
            ushort productId = frame.CommandParameters.Span[4..6].ToUInt16BE();
            return new ManufacturerInformation(manufacturerId, productTypeId, productId);
        }
    }

    internal readonly struct ManufacturerSpecificDeviceSpecificGetCommand : ICommand
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

    internal readonly struct ManufacturerSpecificDeviceSpecificReportCommand : ICommand
    {
        public ManufacturerSpecificDeviceSpecificReportCommand(CommandClassFrame frame)
        {
            Frame = frame;
        }

        public static CommandClassId CommandClassId => CommandClassId.ManufacturerSpecific;

        public static byte CommandId => (byte)ManufacturerSpecificCommand.DeviceSpecificReport;

        public CommandClassFrame Frame { get; }

        public static DeviceSpecificReport Parse(CommandClassFrame frame, ILogger logger)
        {
            if (frame.CommandParameters.Length < 2)
            {
                logger.LogWarning("Device Specific Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Device Specific Report frame is too short");
            }

            ReadOnlySpan<byte> span = frame.CommandParameters.Span;

            ManufacturerSpecificDeviceIdType deviceIdType = (ManufacturerSpecificDeviceIdType)(span[0] & 0b0000_0111);

            int deviceIdDataFormat = (span[1] & 0b1110_0000) >> 5;
            int deviceIdDataLength = span[1] & 0b0001_1111;

            if (deviceIdDataLength == 0)
            {
                logger.LogWarning("Device Specific Report has zero Device ID Data Length");
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Device Specific Report has zero Device ID Data Length");
            }

            if (frame.CommandParameters.Length < 2 + deviceIdDataLength)
            {
                logger.LogWarning(
                    "Device Specific Report frame is too short for declared data length ({Length} bytes, expected at least {Expected})",
                    frame.CommandParameters.Length,
                    2 + deviceIdDataLength);
                ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "Device Specific Report frame is too short for declared data length");
            }

            ReadOnlySpan<byte> deviceIdData = span.Slice(2, deviceIdDataLength);

            string deviceId = deviceIdDataFormat == 0
                ? Encoding.UTF8.GetString(deviceIdData)
                : $"0x{Convert.ToHexString(deviceIdData)}";

            return new DeviceSpecificReport(deviceIdType, deviceId);
        }
    }
}
