namespace ZWave;

/// <summary>
/// Identifies a Z-Wave command class.
/// </summary>
/// <remarks>
/// As defined by the Z-Wave Application Specification.
/// </remarks>
public enum CommandClassId : byte
{
    /// <summary>
    /// The No Operation Command Class can be used to check if a node is reachable by sending a command which generates no operation.
    /// </summary>
    NoOperation = 0x00,

    /// <summary>
    /// The Basic Command Class allows a controlling device to operate the primary functionality of a supporting
    /// device without any further knowledge.
    /// </summary>
    Basic = 0x20,

    /// <summary>
    /// The Controller Replication Command Class is used to copy network configuration from a Z-Wave controller to another.
    /// </summary>
    ControllerReplication = 0x21,

    /// <summary>
    /// The Application Status Command Class is used to inform other nodes of the current operational state of the application.
    /// </summary>
    ApplicationStatus = 0x22,

    /// <summary>
    /// The Z/IP Command Class is used to transfer Z-Wave commands across an IP network.
    /// </summary>
    ZIp = 0x23,

    /// <summary>
    /// The Binary Switch Command Class is used to control the On/Off state of supporting nodes.
    /// </summary>
    BinarySwitch = 0x25,

    /// <summary>
    /// The Multilevel Switch Command Class is used to control devices with multilevel capability.
    /// </summary>
    MultilevelSwitch = 0x26,

    /// <summary>
    /// The All Switch Command Class is used to control the On/Off functionality of all Switch devices simultaneously.
    /// </summary>
    AllSwitch = 0x27,

    /// <summary>
    /// The Binary Toggle Switch Command Class is used to toggle the On/Off state of a switch.
    /// </summary>
    BinaryToggleSwitch = 0x28,

    /// <summary>
    /// The Multilevel Toggle Switch Command Class is used to toggle the level of a multilevel switch.
    /// </summary>
    MultilevelToggleSwitch = 0x29,

    /// <summary>
    /// The Scene Activation Command Class is used to activate scenes in supporting devices.
    /// </summary>
    SceneActivation = 0x2B,

    /// <summary>
    /// The Scene Actuator Configuration Command Class is used to configure scenes for an actuator device.
    /// </summary>
    SceneActuatorConfiguration = 0x2c,

    /// <summary>
    /// The Scene Controller Configuration Command Class is used to configure a scene controller to activate a specific scene.
    /// </summary>
    SceneControllerConfiguration = 0x2d,

    /// <summary>
    /// The Binary Sensor Command Class is used to realize binary sensors, such as movement sensors and door/window sensors.
    /// </summary>
    BinarySensor = 0x30,

    /// <summary>
    /// The Multilevel Sensor Command Class is used to advertise numerical sensor readings.
    /// </summary>
    MultilevelSensor = 0x31,

    /// <summary>
    /// The Meter Command Class is used to advertise instantaneous and accumulated numerical readings.
    /// </summary>
    Meter = 0x32,

    /// <summary>
    /// The Color Switch Command Class is used to control color capable devices. 
    /// </summary>
    ColorSwitch = 0x33,

    /// <summary>
    /// The Network Management Inclusion Command Class is used by a Z/IP Gateway to include/exclude Z-Wave nodes.
    /// </summary>
    NetworkManagementInclusion = 0x34,

    /// <summary>
    /// The Pulse Meter Command Class is used to advertise pulse meter readings.
    /// </summary>
    PulseMeter = 0x35,

    /// <summary>
    /// The Basic Tariff Information Command Class is used to advertise basic tariff information.
    /// </summary>
    BasicTariffInformation = 0x36,

    /// <summary>
    /// The HRV Status Command Class is used to advertise heat recovery ventilation status.
    /// </summary>
    HrvStatus = 0x37,

    /// <summary>
    /// The HRV Control Command Class is used to control heat recovery ventilation devices.
    /// </summary>
    HrvControl = 0x39,

    /// <summary>
    /// The Demand Control Plan Configuration Command Class is used to configure demand control plans.
    /// </summary>
    DemandControlPlanConfiguration = 0x3a,

    /// <summary>
    /// The Demand Control Plan Monitor Command Class is used to monitor demand control plans.
    /// </summary>
    DemandControlPlanMonitor = 0x3b,

    /// <summary>
    /// The Meter Table Configuration Command Class is used to configure meter tables.
    /// </summary>
    MeterTableConfiguration = 0x3c,

    /// <summary>
    /// The Meter Table Monitor Command Class is used to monitor meter table data.
    /// </summary>
    MeterTableMonitor = 0x3d,

    /// <summary>
    /// The Meter Table Push Configuration Command Class is used to configure meter table push settings.
    /// </summary>
    MeterTablePushConfiguration = 0x3e,

    /// <summary>
    /// The Prepayment Command Class is used for prepayment functionality.
    /// </summary>
    Prepayment = 0x3f,

    /// <summary>
    /// The Thermostat Mode Command Class is used to control the mode of the thermostat.
    /// </summary>
    ThermostatMode = 0x40,

    /// <summary>
    /// The Prepayment Encapsulation Command Class is used to encapsulate prepayment commands.
    /// </summary>
    PrepaymentEncapsulation = 0x41,

    /// <summary>
    /// The Thermostat Operating State Command Class is used to report the operating state of the thermostat.
    /// </summary>
    ThermostatOperatingState = 0x42,

    /// <summary>
    /// The Thermostat Setpoint Command Class is used to configure the setpoint of a thermostat.
    /// </summary>
    ThermostatSetpoint = 0x43,

    /// <summary>
    /// The Thermostat Fan Mode Command Class is used to control the fan mode of the thermostat.
    /// </summary>
    ThermostatFanMode = 0x44,

    /// <summary>
    /// The Thermostat Fan State Command Class is used to report the fan state of the thermostat.
    /// </summary>
    ThermostatFanState = 0x45,

    /// <summary>
    /// The Climate Control Schedule Command Class is used to configure and read climate control schedules.
    /// </summary>
    ClimateControlSchedule = 0x46,

    /// <summary>
    /// The Thermostat Setback Command Class is used to configure the setback state of the thermostat.
    /// </summary>
    ThermostatSetback = 0x47,

    /// <summary>
    /// The Rate Table Configuration Command Class is used to configure rate tables.
    /// </summary>
    RateTableConfiguration = 0x48,

    /// <summary>
    /// The Rate Table Monitor Command Class is used to monitor rate table data.
    /// </summary>
    RateTableMonitor = 0x49,

    /// <summary>
    /// The Tariff Table Configuration Command Class is used to configure tariff tables.
    /// </summary>
    TariffTableConfiguration = 0x4a,

    /// <summary>
    /// The Tariff Table Monitor Command Class is used to monitor tariff table data.
    /// </summary>
    TariffTableMonitor = 0x4b,

    /// <summary>
    /// The Door Lock Logging Command Class is used to read door lock log records.
    /// </summary>
    DoorLockLogging = 0x4c,

    /// <summary>
    /// The Network Management Basic Node Command Class is used by a Z/IP Gateway for basic node management.
    /// </summary>
    NetworkManagementBasicNode = 0x4d,

    /// <summary>
    /// The Schedule Entry Lock Command Class is used to manage schedule entries for door locks.
    /// </summary>
    ScheduleEntryLock = 0x4e,

    /// <summary>
    /// The Z/IP 6LoWPAN Command Class is used for 6LoWPAN operations.
    /// </summary>
    ZIp6LoWpan = 0x4f,

    /// <summary>
    /// The Basic Window Covering Command Class is used to control basic window covering devices.
    /// </summary>
    BasicWindowCovering = 0x50,

    /// <summary>
    /// The Move To Position Window Covering Command Class is used to move window coverings to a specific position.
    /// </summary>
    MoveToPositionWindowCovering = 0x51,

    /// <summary>
    /// The Network Management Proxy Command Class is used by a Z/IP Gateway to manage network proxying.
    /// </summary>
    NetworkManagementProxy = 0x52,

    /// <summary>
    /// The Schedule Command Class is used to manage schedules.
    /// </summary>
    Schedule = 0x53,

    /// <summary>
    /// The Network Management Primary Command Class is used by a Z/IP Gateway for primary controller operations.
    /// </summary>
    NetworkManagementPrimary = 0x54,

    /// <summary>
    /// The Transport Service Command Class is used to transfer data that does not fit in a single Z-Wave frame.
    /// </summary>
    TransportService = 0x55,

    /// <summary>
    /// The CRC-16 Encapsulation Command Class is used to encapsulate commands with a CRC-16 checksum.
    /// </summary>
    Crc16Encapsulation = 0x56,

    /// <summary>
    /// The Application Capability Command Class is used to advertise capabilities of a node.
    /// </summary>
    ApplicationCapability = 0x57,

    /// <summary>
    /// The Z/IP ND Command Class is used for Z/IP neighbor discovery operations.
    /// </summary>
    ZIpND = 0x58,

    /// <summary>
    /// The Association Group Information Command Class is used to advertise the capabilities of each association group.
    /// </summary>
    AssociationGroupInformation = 0x59,

    /// <summary>
    /// The Device Reset Locally Command Class is used to notify other nodes that the device has been reset to factory defaults.
    /// </summary>
    DeviceResetLocally = 0x5a,

    /// <summary>
    /// The Central Scene Command Class is used to advertise scene activations from devices with push buttons or similar controls.
    /// </summary>
    CentralScene = 0x5b,

    /// <summary>
    /// The IP Association Command Class is used to manage IP-based associations.
    /// </summary>
    IpAssociation = 0x5c,

    /// <summary>
    /// The Anti-theft Command Class is used to implement anti-theft functionality for Z-Wave devices.
    /// </summary>
    AntiTheft = 0x5d,

    /// <summary>
    /// The Z-Wave Plus Info Command Class is used to differentiate between Z-Wave Plus, Z-Wave for IP and Z-Wave devices.
    /// Furthermore this command class provides additional information about the Z-Wave Plus device in question.
    /// </summary>
    ZWavePlusInfo = 0x5e,

    /// <summary>
    /// The Z/IP Gateway Command Class is used for Z/IP gateway operations.
    /// </summary>
    ZIpGateway = 0x5f,

    /// <summary>
    /// The Multi Channel Command Class is used to address one or more end points in a multi-channel device.
    /// </summary>
    MultiChannel = 0x60,

    /// <summary>
    /// The Z/IP Portal Command Class is used for Z/IP portal operations.
    /// </summary>
    ZIpPortal = 0x61,

    /// <summary>
    /// The Door Lock Command Class is used to operate and configure a door lock device.
    /// </summary>
    DoorLock = 0x62,

    /// <summary>
    /// The User Code Command Class is used to manage user codes for entry control devices.
    /// </summary>
    UserCode = 0x63,

    /// <summary>
    /// The Humidity Control Setpoint Command Class is used to configure the setpoint of a humidity control device.
    /// </summary>
    HumidityControlSetpoint = 0x64,

    /// <summary>
    /// The Barrier Operator Command Class is used to control barrier operator devices such as garage doors.
    /// </summary>
    BarrierOperator = 0x66,

    /// <summary>
    /// The Network Management Installation and Maintenance Command Class is used for network diagnostics.
    /// </summary>
    NetworkManagementInstallationAndMaintenance = 0x67,

    /// <summary>
    /// The Z/IP Naming and Location Command Class is used to assign names and locations to Z/IP nodes.
    /// </summary>
    ZIpNamingAndLocation = 0x68,

    /// <summary>
    /// The Mailbox Command Class is used for mailbox operations in Z/IP networks.
    /// </summary>
    Mailbox = 0x69,

    /// <summary>
    /// The Window Covering Command Class is used to control window coverings with multiple parameters.
    /// </summary>
    WindowCovering = 0x6a,

    /// <summary>
    /// The Irrigation Command Class is used to control irrigation systems.
    /// </summary>
    Irrigation = 0x6b,

    /// <summary>
    /// The Supervision Command Class is used to request application-level delivery confirmation.
    /// </summary>
    Supervision = 0x6c,

    /// <summary>
    /// The Humidity Control Mode Command Class is used to control the mode of a humidity control device.
    /// </summary>
    HumidityControlMode = 0x6d,

    /// <summary>
    /// The Humidity Control Operating State Command Class is used to report the operating state of a humidity control device.
    /// </summary>
    HumidityControlOperatingState = 0x6e,

    /// <summary>
    /// The Entry Control Command Class is used by entry control devices such as keypads.
    /// </summary>
    EntryControl = 0x6f,

    /// <summary>
    /// The Configuration Command Class is used to set and read device-specific configuration parameters.
    /// </summary>
    Configuration = 0x70,

    /// <summary>
    /// The Notification Command Class is used to advertise events or states, such as movement detection, door open/close
    /// or system failure
    /// </summary>
    Notification = 0x71,

    /// <summary>
    /// The Manufacturer Specific Command Class is used to advertise manufacturer specific and device specific information.
    /// </summary>
    ManufacturerSpecific = 0x72,

    /// <summary>
    /// The Powerlevel Command Class defines RF transmit power controlling Commands useful when installing or testing a network.
    /// </summary>
    Powerlevel = 0x73,

    /// <summary>
    /// The Inclusion Controller Command Class is used by inclusion controllers for network management.
    /// </summary>
    InclusionController = 0x74,

    /// <summary>
    /// The Protection Command Class is used to prevent unintentional control of a device.
    /// </summary>
    Protection = 0x75,

    /// <summary>
    /// The Lock Command Class is used to operate a simple lock device.
    /// </summary>
    Lock = 0x76,

    /// <summary>
    /// The Node Naming and Location Command Class is used to assign names and locations to Z-Wave nodes.
    /// </summary>
    NodeNamingAndLocation = 0x77,

    /// <summary>
    /// The Node Provisioning Command Class is used to manage the provisioning list for SmartStart.
    /// </summary>
    NodeProvisioning = 0x78,

    /// <summary>
    /// The Sound Switch Command Class is used to configure and control tones and volume on devices with audio capability.
    /// </summary>
    SoundSwitch = 0x79,

    /// <summary>
    /// The Firmware Update Meta Data Command Class is used to offer and perform firmware updates.
    /// </summary>
    FirmwareUpdateMetaData = 0x7a,

    /// <summary>
    /// The Grouping Name Command Class is used to assign names to association groups.
    /// </summary>
    GroupingName = 0x7b,

    /// <summary>
    /// The Remote Association Activation Command Class is used to activate remote associations.
    /// </summary>
    RemoteAssociationActivation = 0x7c,

    /// <summary>
    /// The Remote Association Configuration Command Class is used to configure remote associations.
    /// </summary>
    RemoteAssociationConfiguration = 0x7d,

    /// <summary>
    /// The Anti-theft Unlock Command Class is used to unlock a device that has been locked by the Anti-theft Command Class.
    /// </summary>
    AntiTheftUnlock = 0x7e,

    /// <summary>
    /// The Battery Command Class is used to request and report the battery types, status and levels of a given device.
    /// </summary>
    Battery = 0x80,

    /// <summary>
    /// The Clock Command Class is used to set and read the current time of a device.
    /// </summary>
    Clock = 0x81,

    /// <summary>
    /// The Hail Command Class is used by a node to advertise that it requires attention from the controller.
    /// </summary>
    Hail = 0x82,

    /// <summary>
    /// The Wake Up Command Class allows a battery-powered device to notify another device (always listening), that it is awake
    /// and ready to receive any queued commands.
    /// </summary>
    WakeUp = 0x84,

    /// <summary>
    /// The Association Command Class is used to manage associations between nodes.
    /// </summary>
    Association = 0x85,

    /// <summary>
    /// The Version Command Class may be used to obtain the Z-Wave library type, the Z-Wave protocol version used by the
    /// application, the individual command class versions used by the application and the vendor specific application version
    /// from a Z-Wave enabled device.
    /// </summary>
    Version = 0x86,
    
    /// <summary>
    /// The Indicator Command Class is used to set and read the state of an indicator, such as an LED.
    /// </summary>
    Indicator = 0x87,

    /// <summary>
    /// The Proprietary Command Class is used to transfer vendor-specific data.
    /// </summary>
    Proprietary = 0x88,

    /// <summary>
    /// The Language Command Class is used to set and read the language of a device.
    /// </summary>
    Language = 0x89,

    /// <summary>
    /// The Time Command Class is used to read the current time and date from a device.
    /// </summary>
    Time = 0x8a,

    /// <summary>
    /// The Time Parameters Command Class is used to set and read time parameters such as date and UTC time.
    /// </summary>
    TimeParameters = 0x8b,

    /// <summary>
    /// The Geographic Location Command Class is used to set and read the geographic location of a device.
    /// </summary>
    GeographicLocation = 0x8c,

    /// <summary>
    /// The Multi Channel Association Command Class is used to manage associations that include specific end points.
    /// </summary>
    MultiChannelAssociation = 0x8e,

    /// <summary>
    /// The Multi Command Command Class is used to encapsulate multiple commands in a single frame.
    /// </summary>
    MultiCommand = 0x8f,

    /// <summary>
    /// The Energy Production Command Class is used to advertise energy production readings.
    /// </summary>
    EnergyProduction = 0x90,

    /// <summary>
    /// The Manufacturer Proprietary Command Class is used to transfer manufacturer-specific data.
    /// </summary>
    ManufacturerProprietary = 0x91,

    /// <summary>
    /// The Screen Meta Data Command Class is used to transfer screen display data.
    /// </summary>
    ScreenMetaData = 0x92,

    /// <summary>
    /// The Screen Attributes Command Class is used to advertise screen capabilities of a device.
    /// </summary>
    ScreenAttributes = 0x93,

    /// <summary>
    /// The Simple AV Control Command Class is used to control audio/video devices.
    /// </summary>
    SimpleAvControl = 0x94,

    /// <summary>
    /// The Security 0 Command Class is used to encapsulate commands using the legacy S0 security protocol.
    /// </summary>
    Security0 = 0x98,

    /// <summary>
    /// The IP Configuration Command Class is used to configure IP settings of a Z/IP node.
    /// </summary>
    IpConfiguration = 0x9a,

    /// <summary>
    /// The Association Command Configuration Command Class is used to configure commands sent via associations.
    /// </summary>
    AssociationCommandConfiguration = 0x9b,

    /// <summary>
    /// The Alarm Sensor Command Class is used to realize alarm sensors.
    /// </summary>
    AlarmSensor = 0x9c,

    /// <summary>
    /// The Alarm Silence Command Class is used to silence an active alarm.
    /// </summary>
    AlarmSilence = 0x9d,

    /// <summary>
    /// The Sensor Configuration Command Class is used to configure sensor parameters.
    /// </summary>
    SensorConfiguration = 0x9e,

    /// <summary>
    /// The Security 2 Command Class is used to encapsulate commands using the S2 security protocol.
    /// </summary>
    Security2 = 0x9f,

    /// <summary>
    /// The IR Repeater Command Class is used for infrared repeater functionality.
    /// </summary>
    IrRepeater = 0xa0,

    /// <summary>
    /// The Authentication Command Class is used for authentication operations.
    /// </summary>
    Authentication = 0xa1,

    /// <summary>
    /// The Authentication Media Write Command Class is used to write authentication media.
    /// </summary>
    AuthenticationMediaWrite = 0xa2,

    /// <summary>
    /// The Generic Schedule Command Class is used to configure and manage generic schedules.
    /// </summary>
    GenericSchedule = 0xa3,

    /// <summary>
    /// The Support/Control Mark is used to separate supported and controlled command classes in the NIF.
    /// </summary>
    SupportControlMark = 0xef,
}
