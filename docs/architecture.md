# Architecture

This document describes the layered architecture of ZWave.NET and how the major components interact.

## Overview

ZWave.NET is a host-side implementation of the Z-Wave serial protocol. It communicates with a Z-Wave USB controller (the "chip") over a serial port and exposes Z-Wave network nodes and their command classes to the host application.

```
┌─────────────────────────────────────────────────────┐
│                  Host Application                   │
├─────────────────────────────────────────────────────┤
│  Driver / Controller / Node                         │
│  (src/ZWave/Driver.cs, Controller.cs, Node.cs)      │
├─────────────────────────────────────────────────────┤
│  Command Classes                                    │
│  (src/ZWave/CommandClasses/)                        │
├─────────────────────────────────────────────────────┤
│  Serial API Commands                                │
│  (src/ZWave/Serial/Commands/)                       │
├─────────────────────────────────────────────────────┤
│  Serial Frame Layer                                 │
│  (src/ZWave/Serial/)                                │
├─────────────────────────────────────────────────────┤
│  System.IO.Ports (SerialPort)                       │
└─────────────────────────────────────────────────────┘
          │                         ▲
          ▼                         │
   ┌──────────────────────────────────────┐
   │        Z-Wave USB Controller         │
   │   (e.g. Aeotec Z-Stick, UZB-7)      │
   └──────────────────────────────────────┘
```

## Layers

### Serial Frame Layer (`src/ZWave/Serial/`)

The lowest layer handles raw byte framing over the serial port per INS12350 (Serial API Host Application Programming Guide).

| File | Purpose |
|---|---|
| `FrameHeader.cs` | Constants for frame header bytes: SOF (0x01), ACK (0x06), NAK (0x15), CAN (0x18) |
| `Frame.cs` | Represents a single-byte frame (ACK/NAK/CAN) or a multi-byte data frame (SOF). Immutable struct. |
| `DataFrame.cs` | SOF data frame: `[SOF][Length][Type][CommandId][Parameters...][Checksum]`. Computes and validates XOR checksums. |
| `DataFrameType.cs` | REQ (request) or RES (response) |
| `FrameType.cs` | ACK, NAK, CAN, or Data |
| `FrameParser.cs` | Stateless parser that extracts `Frame` instances from a `ReadOnlySequence<byte>` using `System.IO.Pipelines`. Skips invalid data gracefully. |
| `ZWaveSerialPortCoordinator.cs` | Manages the serial port lifecycle, read/write loops, ACK handshake, retransmission (up to 3 retries per INS12350 §6.3), and port re-opening on failure. Uses `System.Threading.Channels` for decoupled send/receive. |

**Data flow:**
1. `ZWaveSerialPortCoordinator` reads bytes from the serial port into a `PipeReader`
2. `FrameParser.TryParseData` extracts complete frames
3. Data frames with valid checksums are ACK'd and written to a receive channel
4. The write loop reads from a send channel, transmits frames, and waits for ACK/NAK/CAN delivery confirmation with a 1600ms timeout

### Serial API Commands (`src/ZWave/Serial/Commands/`)

Each Z-Wave Serial API function is represented as a struct. These map to the function IDs in INS12350. All function IDs defined in the `CommandId` enum have corresponding implementation structs.

**Key interfaces:**
- `ICommand<T>` - A serial API command with a `Type` (REQ/RES), `CommandId`, factory `Create(DataFrame)` method, and a `Frame` property
- `IRequestWithCallback<T>` - Extends `ICommand<T>` for commands that trigger an asynchronous callback with a `SessionId` and an `ExpectsResponseStatus` flag

**`CommandId` enum** - All Serial API function IDs (e.g. `SendData = 0x13`, `GetInitData = 0x02`).

**Communication patterns:**
- **Fire-and-forget:** Host sends a REQ with no response expected (e.g. `SoftReset`, `SendDataAbort`, `WatchdogKick`)
- **Request → Response:** Host sends a REQ, chip replies with a RES (e.g. `MemoryGetId`, `GetLibraryVersion`, `GetRoutingInfo`, `IsFailedNode`)
- **Request → Response + Callback:** Host sends a REQ, chip replies with a RES (status), then later sends an unsolicited REQ as a callback (e.g. `SendData`, `SetSucNodeId`, `RemoveFailedNode`). The callback is correlated by `SessionId`.
- **Request → Callback (no response):** Host sends a REQ, chip later sends an unsolicited REQ as a callback without an initial RES (e.g. `SetDefault`, `SetLearnMode`, `RequestNodeNeighborUpdate`)
- **Set only:** Host sends a REQ to configure the chip with no response (e.g. `ApplicationNodeInformation`, `SetPromiscuousMode`)
- **Unsolicited request:** Chip sends a REQ without the host asking (e.g. `ApplicationCommandHandler`, `ApplicationUpdate`, `ApplicationCommandHandlerBridge`, `SerialApiStarted`)

### Command Classes (`src/ZWave/CommandClasses/`)

Implements Z-Wave Command Classes per SDS13781 (Z-Wave Application Command Class Specification). These are the application-level messages exchanged between Z-Wave nodes.

**Base classes:**
- `CommandClass` - Abstract base with interview lifecycle, version tracking, awaited report management, and dependency declaration
- `CommandClass<TCommand>` - Generic version that adds `IsCommandSupported(TCommand)` where `TCommand` is a byte-backed enum

**Key types:**
- `CommandClassId` enum - All Z-Wave CC IDs (e.g. `BinarySwitch = 0x25`)
- `CommandClassInfo` - Record struct with CC ID, supported/controlled flags
- `CommandClassFrame` - Wraps CC payload: `[CommandClassId][CommandId][Parameters...]`
- `ICommand` - Interface for CC-level commands (distinct from the Serial API `ICommand<T>`)

**Each CC file contains:**
1. A byte-backed **command enum** (e.g. `BinarySwitchCommand`)
2. **State types** (e.g. `BinarySwitchState`)
3. The **CC class** with `[CommandClass(...)]` attribute, inheriting `CommandClass<TEnum>`
4. Private inner **command structs** implementing `ICommand` for Set/Get/Report

**Registration:** The `[CommandClass(CommandClassId.X)]` attribute is scanned by the `CommandClassFactoryGenerator` source generator, which generates `CommandClassFactory` - a mapping from `CommandClassId` to constructor delegate. Unrecognized CCs fall back to `NotImplementedCommandClass`.

### High-Level Objects

**`Driver`** (`Driver.cs`) - Entry point for the library. Created via `Driver.CreateAsync()`.
- Opens the serial port via `ZWaveSerialPortCoordinator`
- Runs the initialization sequence per INS12350 §6.1 (NAK → soft reset → wait for SerialApiStarted)
- Manages request-response flow (only one REQ→RES session at a time per INS12350 §6.5.2)
- Tracks pending callbacks by `(CommandId, SessionId)` key
- Routes unsolicited requests (`ApplicationCommandHandler`, `ApplicationUpdate`) to the correct `Node`

**`Controller`** (`Controller.cs`) - Represents the USB controller.
- `IdentifyAsync()` queries the chip: home/node ID, serial API capabilities, library version, controller capabilities, supported setup subcommands, SUC node ID, and init data (node list)
- Promotes itself to SUC/SIS if none exists on the network
- Creates `Node` instances for every node in the network

**`Node`** (`Node.cs`) - Represents a Z-Wave network node.
- `InterviewAsync()` queries protocol info, requests node info, discovers command classes, then interviews each CC in topological dependency order
- Uses copy-on-write dictionary for thread-safe command class access
- Exposes `GetCommandClass<T>()` for typed access to a node's command classes

## Source Generators (`src/ZWave.BuildTools/`)

The `ZWave.BuildTools` project contains Roslyn incremental source generators that target `netstandard2.0` (required by the analyzer infrastructure). It is referenced as an analyzer in `ZWave.csproj`.

| Generator | Input | Output |
|---|---|---|
| `CommandClassFactoryGenerator` | `[CommandClass]` attributes on CC classes | `CommandClassFactory` with ID→constructor mapping and type→ID reverse mapping |
| `MultilevelSensorTypeGenerator` | `Config/MultilevelSensorTypes.json` | Sensor type enum and lookup |
| `MultilevelSensorScaleGenerator` | `Config/MultilevelSensorScales.json` | Sensor scale enum and lookup |

## Logging

All log messages are defined in `Logging.cs` using source-generated `[LoggerMessage]` partial methods on an `ILogger` extension class.

Event ID ranges:
- **100–199:** Serial API layer (port open/close, frame send/receive, errors)
- **200–299:** Driver and controller (initialization, identity, capabilities)

## Error Handling

`ZWaveException` is thrown for Z-Wave-specific errors, categorized by `ZWaveErrorCode`:
- `DriverInitializationFailed` / `ControllerInitializationFailed` - Startup failures
- `CommandSendFailed` - Frame delivery failed after retransmissions
- `CommandFailed` - Response indicated failure
- `CommandClassNotImplemented` / `CommandNotSupported` - CC or command not available on a node
- `CommandNotReady` - CC not yet interviewed
- `CommandInvalidArgument` - Invalid parameter

## Protocol References

The implementation references these official specifications:

- **INS12350** - Serial API Host Application Programming Guide (frame format, handshake, initialization sequence, command definitions)
- **SDS13781** - Z-Wave Application Command Class Specification (CC message formats, versioning, required fields)
- **INS13954** - Z-Wave 500 Series Application Programmer's Guide (legacy reference for older command IDs)
- **ITU-T G.9959** - PHY/MAC layer standard

Online resources:
- [Silicon Labs Serial API Reference](https://docs.silabs.com/z-wave/latest/zwave-api/serial-api)
- [Silicon Labs Serial API Programming Guide](https://docs.silabs.com/z-wave/latest/z-wave-serial-api-host-app-programming-guide/)
- [Z-Wave Alliance - Specification for Developers](https://z-wavealliance.org/development-resources-overview/specification-for-developers/)
- [zwave-js/specs](https://github.com/zwave-js/specs) - Full specification collection
