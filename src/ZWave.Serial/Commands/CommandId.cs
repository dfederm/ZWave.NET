namespace ZWave.Serial.Commands;

/// <summary>
/// The ids for various Serial API commands.
/// </summary>
/// <remarks>
/// Reference specs:
/// https://www.silabs.com/documents/public/user-guides/INS12350-Serial-API-Host-Appl.-Prg.-Guide.pdf
/// https://www.silabs.com/documents/public/user-guides/INS13954-Instruction-Z-Wave-500-Series-Appl-Programmers-Guide-v6_8x_0x.pdf
/// </remarks>
public enum CommandId : byte
{
    /// <summary>
    /// Determine Serial API protocol version number, Serial API capabilities, nodes currently
    /// stored in the external NVM (only controllers) and chip used in a specific Serial API
    /// Z-Wave Module.
    /// </summary>
    GetInitData = 0x02,

    /// <summary>
    /// Generate the Node Information frame and to save information about node capabilities.
    /// </summary>
    ApplicationNodeInformation = 0x03,

    /// <summary>
    /// Called when an application command or request has been received from another node.
    /// </summary>
    ApplicationCommandHandler = 0x04,

    /// <summary>
    /// Returns a bitmask containing the capabilities of the controller.
    /// </summary>
    GetControllerCapabilities = 0x05,

    /// <summary>
    /// Set the timeout in the Serial API
    /// </summary>
    SerialApiSetTimeouts = 0x06,

    /// <summary>
    /// Determine exactly which Serial API functions a specific Serial API Z-Wave Module supports
    /// </summary>
    GetSerialApiCapabilities = 0x07,

    /// <summary>
    /// Request the Z-Wave Module to perform a soft reset
    /// </summary>
    SoftReset = 0x08,

    /// <summary>
    /// Transmit the data buffer using S2 multicast to a list of Z-Wave Nodes.
    /// </summary>
    SendDataMultiEx = 0x09,

    /// <summary>
    /// Used by the Z-Wave Module to indicate that it is ready to be operated after a reboot or reset operation.
    /// </summary>
    SerialApiStarted = 0x0a,

    /// <summary>
    /// Control the callback parameter list extension.
    /// </summary>
    SerialApiSetup = 0x0b,

    /// <summary>
    /// Notify the protocol of the command classes it supports using each security key.
    /// This function is only required in slave_routing and slave_enhanced_232 based applications.
    /// </summary>
    ApplicationSecureCommandsSupported = 0x0c,

    /// <summary>
    /// Power down the RF when not in use
    /// </summary>
    SetRFReceiveMode = 0x10,

    /// <summary>
    /// Set the SoC in a specified power down mode
    /// </summary>
    SetSleepMode = 0x11,

    /// <summary>
    /// Create and transmit a “Node Information” frame.
    /// </summary>
    SendNodeInformation = 0x12,

    /// <summary>
    /// Transmit the data buffer to a single Z-Wave Node or all Z-Wave Nodes (broadcast).
    /// </summary>
    SendData = 0x13,

    /// <summary>
    /// Transmit the data buffer to a list of Z-Wave Nodes (multicast frame).
    /// </summary>
    SendDataMulti = 0x14,

    /// <summary>
    /// Get the Z-Wave basis API library version.
    /// </summary>
    GetLibraryVersion = 0x15,

    /// <summary>
    /// Abort the ongoing transmit started with ZW_SendData or ZW_SendDataMulti.
    /// </summary>
    SendDataAbort = 0x16,

    /// <summary>
    /// Set the power level used for RF transmission
    /// </summary>
    RFPowerLevelSet = 0x17,

    /// <summary>
    /// Overwrite the current neighbor information for a given node ID in the protocol locally
    /// </summary>
    SetRoutingInfo = 0x1b,

    /// <summary>
    /// Returns a random word using the 500 series built-in hardware random number generator
    /// based on (internal) RF noise(RFRNG).
    /// </summary>
    GetRandomWord = 0x1c,

    /// <summary>
    /// Returns a pseudo-random number
    /// </summary>
    Random = 0x1d,

    /// <summary>
    /// Set the power level locally in the node when finding neighbors.
    /// </summary>
    RFPowerlevelRediscoverySet = 0x1e,

    /// <summary>
    /// Get the Home-ID and Node-ID from the controller.
    /// </summary>
    MemoryGetId = 0x20,

    /// <summary>
    /// Read one byte from the NVM allocated for the application
    /// </summary>
    MemoryGetByte = 0x21,

    /// <summary>
    /// Write one byte to the application area of the NVM.
    /// </summary>
    MemoryPutByte = 0x22,

    /// <summary>
    /// Read a number of bytes from the NVM allocated for the application.
    /// </summary>
    MemoryGetBuffer = 0x23,

    /// <summary>
    /// Copy a number of bytes from a RAM buffer to the application area of the NVM.
    /// </summary>
    MemoryPutBuffer = 0x24,

    /// <summary>
    /// Enables the Auto Program Mode and resets the 500 Series Z-Wave SOC after 7.8ms.
    /// </summary>
    FlashAutoProgSet = 0x27,

    /// <summary>
    /// Read a value from the NVR Flash memory area.
    /// </summary>
    NvrGetValue = 0x28,

    /// <summary>
    /// Get NVM ID from external NVM
    /// </summary>
    NvmGetId = 0x29,

    /// <summary>
    /// Read a number of bytes from external NVM starting from address offset.
    /// </summary>
    NvmExtReadLongBuffer = 0x2a,

    /// <summary>
    /// Write a number of bytes to external NVM starting from address offset.
    /// </summary>
    NvmExtWriteLongBuffer = 0x2b,

    /// <summary>
    /// Read a byte from external NVM at address offset.
    /// </summary>
    NvmExtReadLongByte = 0x2c,

    /// <summary>
    /// Write a byte to external NVM at address offset.
    /// </summary>
    NvmExtWriteLongByte = 0x2d,

    /// <summary>
    /// Clears the protocols internal tx timers
    /// </summary>
    ClearTxTimers = 0x37,

    /// <summary>
    /// Gets the protocol's internal tx timer for the specified channel.
    /// </summary>
    GetTxTimer = 0x38,

    /// <summary>
    /// Clears the current Network Statistics collected by the Z-Wave protocol
    /// </summary>
    ClearNetworkStats = 0x39,

    /// <summary>
    /// Retrieves the current Network Statistics as collected by the Z-Wave protocol.
    /// </summary>
    GetNetworkStats = 0x3a,

    /// <summary>
    /// Returns the most recent background RSSI levels detected
    /// </summary>
    GetBackgroundRSSI = 0x3b,

    /// <summary>
    /// Sets the “Listen Before Talk” threshold that controlles at what RSSI level a Z-Wave node
    /// will refuse to transmit because of noise.
    /// </summary>
    SetListenBeforeTalkThreshold = 0x3c,

    /// <summary>
    /// Remove a specific node from a Z-Wave network.
    /// </summary>
    RemoveNodeIdFromNetwork = 0x3f,

    /// <summary>
    /// Return the Node Information Frame without command classes from the NVM for a given node ID.
    /// </summary>
    GetNodeProtocolInfo = 0x41,

    /// <summary>
    /// Set the Controller back to the factory default state.
    /// </summary>
    SetDefault = 0x42,

    /// <summary>
    /// Sends command completed to sending controller. Called in replication mode when a command from the
    /// sender has been processed and indicates that the controller is ready for next packet.
    /// </summary>
    ReplicationReceiveComplete = 0x44,

    /// <summary>
    /// Used when the controller is in replication mode. It sends the payload and expects the receiver to respond
    /// with a command complete message
    /// </summary>
    ReplicationSend = 0x45,

    /// <summary>
    /// Assign static return routes (up to 4) to a Routing Slave or Enhanced 232 Slave node.
    /// </summary>
    AssignReturnRoute = 0x46,

    /// <summary>
    /// Delete all static return routes from a Routing Slave or Enhanced 232 Slave node.
    /// </summary>
    DeleteReturnRoute = 0x47,

    /// <summary>
    /// Get the neighbors from the specified node.
    /// </summary>
    RequestNodeNeighborUpdate = 0x48,

    /// <summary>
    /// Update local data structures or to control smart start inclusion
    /// </summary>
    ApplicationUpdate = 0x49,

    /// <summary>
    /// Add a node to a Z-Wave network.
    /// </summary>
    AddNodeToNetwork = 0x4a,

    /// <summary>
    /// Remove a node from a Z-Wave network.
    /// </summary>
    RemoveNodeFromNetwork = 0x4b,

    /// <summary>
    /// (Obsolete) Add a controller to the Z-Wave network as a replacement for the old primary controller.
    /// </summary>
    CreateNewPrimaryController = 0x4c,

    /// <summary>
    /// Add a controller to the Z-Wave network and transfer the role as primary controller to it
    /// </summary>
    ControllerChange = 0x4d,

    /// <summary>
    /// Assign a application defined Priority Return Route to a routing or an enhanced slave that always will be
    /// tried as the first return route attempt.
    /// </summary>
    AssignPriorityReturnRoute = 0x4f,

    /// <summary>
    /// Enable or disable home and node ID’s learn mode.
    /// </summary>
    SetLearnMode = 0x50,

    /// <summary>
    /// Notify presence of a SUC/SIS to a Routing Slave or Enhanced 232 Slave.
    /// </summary>
    AssignSucReturnRoute = 0x51,

    /// <summary>
    /// Request a network update from a SUC/SIS controller. Any changes are reported to the application
    /// by calling ApplicationControllerUpdate.
    /// </summary>
    RequestNetworkUpdate = 0x53,

    /// <summary>
    /// Configure a static/bridge controller to be a SUC/SIS node or not.
    /// </summary>
    SetSucNodeId = 0x54,

    /// <summary>
    /// Delete the return routes of the SUC/SIS node from a Routing Slave node or Enhanced 232 Slave node.
    /// </summary>
    DeleteSucReturnRoute = 0x55,

    /// <summary>
    /// Get the currently registered SUC/SIS node ID.
    /// </summary>
    GetSucNodeId = 0x56,

    /// <summary>
    /// Transmit SUC/SIS node ID from a primary controller or static controller to the controller node ID specified.
    /// </summary>
    SendSucId = 0x57,

    /// <summary>
    /// Assign a application defined Priority SUC Return Route to a routing or an enhanced slave that always
    /// will be tried as the first return route attempt.
    /// </summary>
    AssignPrioritySucReturnRoute = 0x58,

    /// <summary>
    /// Request a SUC/SIS controller to update the requesting nodes neighbors.
    /// </summary>
    RediscoveryNeeded = 0x59,

    /// <summary>
    /// Request new return route destinations from the SUC/SIS node.
    /// </summary>
    RequestNewRouteDestinations = 0x5c,

    /// <summary>
    /// Check if the supplied nodeID is marked as being within direct range in any of the existing return routes.
    /// </summary>
    IsNodeWithinDirectRange = 0x5d,

    /// <summary>
    /// Initiate a Network-Wide Inclusion process
    /// </summary>
    ExploreRequestInclusion = 0x5e,

    /// <summary>
    /// Initiate a Network-Wide Exclusion process
    /// </summary>
    ExploreRequestExclusion = 0x5f,

    /// <summary>
    /// Request the Node Information Frame from a controller based node in the network.
    /// </summary>
    RequestNodeInfo = 0x60,

    /// <summary>
    /// Remove a non-responding node from the routing table in the requesting controller.
    /// </summary>
    RemoveFailedNode = 0x61,

    /// <summary>
    /// Test if a node ID is stored in the failed node ID list.
    /// </summary>
    IsFailedNode = 0x62,

    /// <summary>
    /// Replaces a non-responding node with a new one in the requesting controller.
    /// </summary>
    ReplaceFailedNode = 0x63,

    /// <summary>
    /// The Firmware Update API provides functionality which together with the SDK supplied ZW_Bootloader
    /// module and a big enough external NVM makes it possible to implement firmware update
    /// </summary>
    FirmwareUpdate = 0x78,

    /// <summary>
    /// Read out neighbor information from the protocol.
    /// </summary>
    GetRoutingInfo = 0x80,

    /// <summary>
    /// Returns the number of transmits that the protocol has done since last reset of the variable.
    /// </summary>
    GetTransmitCounter = 0x81,

    /// <summary>
    /// Reset the number of transmits that the protocol has done since last reset of the variable.
    /// </summary>
    ResetTransmitCounter = 0x82,

    /// <summary>
    /// Restore protocol node information from a backup.
    /// </summary>
    StoreNodeInfo = 0x83,

    /// <summary>
    /// Restore HomeID and NodeID information from a backup.
    /// </summary>
    StoreHomeId = 0x84,

    /// <summary>
    /// Locks or unlocks response route for a given node ID.
    /// </summary>
    LockRoute = 0x90,

    /// <summary>
    /// Get the route with the highest priority
    /// </summary>
    GetPriorityRoute = 0x92,

    /// <summary>
    /// Set the Priority Routefor a destination node
    /// </summary>
    SetPriorityRoute = 0x93,

    /// <summary>
    /// Returns a bitmask of security keys the node posseses.
    /// </summary>
    GetSecurityKeys = 0x9c,

    /// <summary>
    /// Notify the application of security events.
    /// This function is only required in slave_routing and slave_enhanced_232 based applications.
    /// </summary>
    ApplicationSecurityEvent = 0x9d,

    /// <summary>
    /// Used to set node information for all Virtual Slave Nodes in the embedded module
    /// </summary>
    SerialApiApplicationSlaveNodeInformation = 0xa0,

    /// <summary>
    /// Create and transmit a Virtual Slave node “Node Information” frame a Virtual Slave node.
    /// </summary>
    SendSlaveNodeInformation = 0xa2,

    /// <summary>
    /// enables the possibility for enabling or disabling “Slave Learn Mode”, which when enabled
    /// makes it possible for other controllers (primary or inclusion controllers) to add or remove
    /// a Virtual Slave Node to the Z-Wave network.
    /// </summary>
    SetSlaveLearnMode = 0xa4,

    /// <summary>
    /// Request a buffer containing available Virtual Slave nodes in the Z-Wave network.
    /// </summary>
    GetVirtualNodes = 0xa5,

    /// <summary>
    /// Checks if a node is a Virtual Slave node.
    /// </summary>
    IsVirtualNode = 0xa6,

    /// <summary>
    /// Transmit the data buffer to a list of Z-Wave Nodes (multicast frame).
    /// </summary>
    SendDataMultiBridge = 0xab,

    /// <summary>
    /// Called when an application command has been received from another node to the Bridge
    /// Controller or an existing virtual slave node.
    /// </summary>
    ApplicationCommandHandlerBridge = 0xa8,

    /// <summary>
    /// Transmit the data buffer to a single Z-Wave Node or all Z-Wave Nodes (broadcast).
    /// </summary>
    SendDataBridge = 0xa9,

    /// <summary>
    /// Set the WUT timer interval
    /// </summary>
    SetWutTimeout = 0xb4,

    /// <summary>
    /// Enable the 500 Series Z-Wave SoC built-in watchdog.
    /// </summary>
    WatchdogEnable = 0xb6,

    /// <summary>
    /// Disable the 500 Series Z-Wave SoC built-in watchdog.
    /// </summary>
    WatchdogDisable = 0xb7,

    /// <summary>
    /// Keep the watchdog timer from resetting the 500 Series Z-Wave SoC
    /// </summary>
    WatchdogKick = 0xb8,

    /// <summary>
    /// Set the trigger level for external interrupts
    /// </summary>
    SetExtIntLevel = 0xb9,

    /// <summary>
    /// Get the current power level used in RF transmitting.
    /// </summary>
    RFPowerLevelGet = 0xba,

    /// <summary>
    /// Get the number of neighbors the specified node has registered.
    /// </summary>
    GetNeighborCount = 0xbb,

    /// <summary>
    /// Check if two nodes are marked as being within direct range of each other
    /// </summary>
    AreNodesNeighbors = 0xbc,

    /// <summary>
    /// Get the Z-Wave library type.
    /// </summary>
    TypeLibrary = 0xbd,

    /// <summary>
    /// Send a test frame directly to nodeID without any routing.
    /// </summary>
    SendTestFrame = 0xbe,

    /// <summary>
    /// Request the status of the protocol
    /// </summary>
    GetProtocolStatus = 0xbf,

    /// <summary>
    /// Enable or disable promiscuous mode.
    /// </summary>
    SetPromiscuousMode = 0xd0,

    /// <summary>
    /// set the maximum number of source routing attempts before the next mechanism kicks-in.
    /// </summary>
    SetRoutingMax = 0xd4,

    /// <summary>
    /// Set the maximum interval between SmartStart inclusion requests
    /// </summary>
    NetworkManagementSetMaxInclusionRequestIntervals = 0xd6,

    /// <summary>
    /// Obtain the list of Long Range nodes
    /// </summary>
    SerialApiGetLongRangeNodes = 0xda,

    /// <summary>
    /// Get the DCDC Configuration
    /// </summary>
    GetDcdcConfig = 0xde,

    /// <summary>
    /// Set the DCDC Configuration
    /// </summary>
    SetDcdcConfig = 0xdf,
}
