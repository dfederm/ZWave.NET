# ZWave.NET — Copilot Agent Instructions

## Project Overview

ZWave.NET is a .NET library implementing the Z-Wave serial protocol for communicating with Z-Wave USB controllers (e.g. UZB-7, Aeotec Z-Stick). It uses **C# preview** features (static abstract interface members). Check `global.json` for the required SDK version and the `.csproj` files for target frameworks. The solution has eight projects:

| Project | Path | Purpose |
|---|---|---|
| **ZWave.Protocol** | `src/ZWave.Protocol/` | Shared Z-Wave domain types (CommandClassId, ZWaveException, NodeType, etc.) |
| **ZWave.Serial** | `src/ZWave.Serial/` | Serial frame protocol and Serial API command structs |
| **ZWave.CommandClasses** | `src/ZWave.CommandClasses/` | Command class implementations (references Protocol, NOT Serial) |
| **ZWave** | `src/ZWave/` | Driver — orchestration layer (Driver, Controller, Node) |
| **ZWave.Serial.Tests** | `src/ZWave.Serial.Tests/` | Unit tests for the serial layer (MSTest with `MSTest.Sdk`) |
| **ZWave.CommandClasses.Tests** | `src/ZWave.CommandClasses.Tests/` | Unit tests for command classes (MSTest with `MSTest.Sdk`) |
| **ZWave.Server** | `src/ZWave.Server/` | Blazor Server demo app |
| **ZWave.BuildTools** | `src/ZWave.BuildTools/` | Roslyn source generators (targets `netstandard2.0`) |

### Project dependency graph

```
ZWave.Protocol          (no dependencies)
  ↑          ↑
ZWave.Serial  ZWave.CommandClasses    (independent of each other)
  ↑          ↑
     ZWave (Driver)
        ↑
     ZWave.Server
```

`ZWave.BuildTools` is referenced as an analyzer by `ZWave.CommandClasses`.

## Build & Test Commands

Always run these commands from the repository root. The CI pipeline runs exactly these steps:

```shell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

- `Directory.Build.rsp` enables `/Restore` by default, so a bare `dotnet build` also restores. But CI does explicit restore then `--no-restore` for build.
- **Warnings are errors**: `TreatWarningsAsErrors` and `MSBuildTreatWarningsAsErrors` are both `true` in `Directory.Build.props`. Every warning will fail the build.
- **Do NOT run `dotnet format --verify-no-changes`** — this is not part of CI, and it produces false compilation errors because it cannot run source generators. There are also many pre-existing formatting issues in the repo.

## CI / Validation

Two GitHub Actions workflows in `.github/workflows/`:
- **`pr.yml`** — Runs on pull requests to `main`: restore → build (Release) → test
- **`ci.yml`** — Runs on push to `main`: same steps plus test result artifact upload

Both require `fetch-depth: 0` for Nerdbank.GitVersioning. There is no separate lint or format check step.

## Architecture & Key Patterns

### Shared Protocol Types (`src/ZWave.Protocol/`)

The foundational layer contains Z-Wave domain types shared across all projects, all in `namespace ZWave;`:
- **`CommandClassId`** — Enum of all Z-Wave command class IDs.
- **`CommandClassInfo`** — Record struct with CC ID, supported/controlled flags.
- **`ZWaveException`** / **`ZWaveErrorCode`** — Z-Wave-specific error types. `ZWaveException` has private constructors; use the `[DoesNotReturn]` static `ZWaveException.Throw(errorCode, message)` helpers instead of `throw new ZWaveException(...)`. Use `ArgumentNullException.ThrowIfNull()` and `ArgumentException.ThrowIfNullOrEmpty()` for argument validation.
- **`FrequentListeningMode`** / **`NodeType`** — Node classification enums.

### Serial API Layer (`src/ZWave.Serial/`)

Implements the Z-Wave Serial API frame-level protocol (as defined by the Z-Wave Host API Specification):
- **`Frame`** / **`DataFrame`** — Wire-level frame types (SOF/ACK/NAK/CAN). `DataFrame` handles the SOF-framed data with checksum.
- **`FrameParser`** — Parses frames from a byte stream using `System.IO.Pipelines`.
- **`ZWaveSerialPortCoordinator`** — Manages the serial port, frame send/receive channels, ACK handshake, and retransmission.
- **`Commands/`** — Each Serial API command (e.g. `SendData`, `GetInitData`, `SoftReset`) is a struct implementing `ICommand<T>` (defined in `Serial/Commands/ICommand.cs`). Commands that expect a callback implement `IRequestWithCallback<T>`.
- **`CommandId` enum** — All Serial API function IDs.
- **`NodeIdType`** / **`NodeIdTypeExtensions`** — Enum (`Short` = 8-bit, `Long` = 16-bit) and extension methods (`NodeIdSize()`, `WriteNodeId()`, `ReadNodeId()`) for conditional NodeID field encoding per Z-Wave Host API Specification §4.3.16.17.
- **`CommandParsingContext`** — Record struct carrying protocol-level parsing context (currently just `NodeIdType`). Passed to `ICommand<T>.Create(DataFrame, CommandParsingContext)` so response/callback structs know how to decode variable-size NodeID fields. Decouples session state from the wire-format `DataFrame`. Request `Create()` methods accept `NodeIdType` directly and use `WriteNodeId` to encode NodeIDs.
- **`CommandDataParsingHelpers`** — Internal shared utilities: `ParseCommandClasses` (CC byte list → `CommandClassInfo` records) and `ParseNodeBitmask` (bitmask → `HashSet<ushort>` node IDs).

### Command Classes Layer (`src/ZWave.CommandClasses/`)

Implements Z-Wave Command Classes (Z-Wave Application Specification). This project references `ZWave.Protocol` but **not** `ZWave.Serial`, enabling mock driver implementations without a serial dependency.
- **`IEndpoint`** — Interface representing a functional sub-unit of a node. Properties: `NodeId`, `EndpointIndex`, `CommandClasses`, `GetCommandClass()`. Endpoint 0 is the "Root Device" (the node itself); endpoints 1–127 are sub-devices discovered via Multi Channel CC.
- **`INode`** — Extends `IEndpoint` with node-level properties (`FrequentListeningMode`). A node IS endpoint 0.
- **`IDriver`** — Interface abstracting the driver layer. `SendCommandAsync` accepts `nodeId` and `endpointIndex` parameters. The concrete `Driver` implementation automatically applies Multi Channel encapsulation when `endpointIndex > 0` and de-encapsulates incoming Multi Channel frames, so command classes send/receive plain (non-encapsulated) frames regardless of endpoint.
- **`CommandClass`** / **`CommandClass<TCommand>`** — Abstract base classes. Each CC (e.g. `BinarySwitchCommandClass`) inherits from `CommandClass<TEnum>` where `TEnum` is a byte-backed enum of commands. Takes `IDriver` and `IEndpoint` interfaces (not concrete types). The `Endpoint` property provides access to the endpoint this CC belongs to. Each CC declares a `Category` (Management, Transport, or Application) which determines interview phase ordering.
- **`CommandClassCategory`** — Enum (`Management`, `Transport`, `Application`) per spec §6.2–6.4. Management CCs (Version, Z-Wave Plus Info, Association, Multi Channel Association, etc.) are interviewed first, then Transport CCs (Multi Channel, Security), then Application CCs (actuators, sensors). New CCs must override `Category` if they are not Application CCs (the default).
- **`[CommandClass(CommandClassId.X)]` attribute** — Applied to each CC class. The source generator `CommandClassFactoryGenerator` scans for this attribute and generates `CommandClassFactory` with a mapping from `CommandClassId` → constructor.
- **`ICommand` interface** (`CommandClasses/ICommand.cs`) — Different from the Serial API `ICommand`. Used for CC-level commands with `CommandClassId` and `CommandId`.
- **`CommandClassFrame`** — Wraps CC payload bytes (CC ID + Command ID + parameters).

### High-Level Objects (`src/ZWave/`)

- **`Driver`** — Entry point. Implements `IDriver`. Opens serial port, manages frame send/receive, processes unsolicited requests, coordinates request-response and callback flows. Tracks `NodeIdType` (default `Short`) and creates `CommandParsingContext` instances to pass when parsing incoming frames. Handles encapsulation/de-encapsulation per spec §4.1.3.5 order. On receive (reverse order): Security/CRC-16/Transport Service → Multi Channel → Supervision → Multi Command. On send: payload → Multi Command → Supervision → Multi Channel → Security/CRC-16/Transport Service. Currently implements Multi Channel and Supervision layers; future encapsulation layers (Security, Transport Service) plug into the same hooks. For incoming Supervision Get frames, the Driver de-encapsulates the inner command, processes it, and sends back a Supervision Report (SUCCESS) — unless the frame was received via multicast/broadcast, per spec CC:006C.01.01.11.005. Incoming Supervision Report frames are routed directly to the node's Supervision CC instance.
- **`Controller`** — Represents the Z-Wave USB controller. Runs identification sequence on startup. Negotiates `SetNodeIdBaseType(Long)` during init if supported by the module. Stores mutable `Associations` list for the controller's lifeline group (modified when other nodes send Association Set/Remove).
- **`ControllerCommandHandler`** — Handles incoming commands from other nodes directed at the controller (the "supporting side"). When another node queries the controller (e.g., Association Get, AGI Group Name Get), this class dispatches to the appropriate handler which constructs and sends the response. Lives in the Driver layer (not the CC layer) because handlers need Driver/Controller context. Uses fire-and-forget for async responses to avoid blocking the frame processing loop.
- **`Node`** — Represents a Z-Wave network node. Implements `INode` (and thus `IEndpoint` with `EndpointIndex = 0`). A node IS endpoint 0 (the "Root Device"). Contains a dictionary of child `Endpoint` instances (1–127) discovered via the Multi Channel CC interview. Key methods: `GetEndpoint(byte)`, `GetAllEndpoints()`, `GetOrAddEndpoint(byte)`. The `ProcessCommand` method accepts an `endpointIndex` parameter to route frames to the correct endpoint's CC instance. The interview follows a phased approach per spec: Management CCs → Transport CCs (Multi Channel discovers endpoints) → Application CCs on root, then repeats for each endpoint. Node IDs are `ushort` throughout the codebase to support both classic (1–232) and Long Range (256+) nodes.
- **`Endpoint`** — Represents a Multi Channel End Point (1–127). Implements `IEndpoint`. Holds its own CC dictionary (copy-on-write), device class info (`GenericDeviceClass`, `SpecificDeviceClass`), and provides `ProcessCommand`, `AddCommandClasses`, `InterviewCommandClassesAsync` methods. Created during the Multi Channel CC interview.
- **`MultiChannelCommandClass`** — Implements the Multi Channel CC (version 4) in `src/ZWave.CommandClasses/`. Discovers endpoints during interview by sending EndPoint Get and Capability Get commands. Provides static methods `CreateEncapsulation()` and `ParseEncapsulation()` used by the Driver for its encapsulation pipeline. Exposes `internal event Action<TReport>?` events (`OnEndpointReportReceived`, `OnCapabilityReportReceived`, `OnCommandEncapsulationReceived`) that fire on both solicited and unsolicited reports. Node subscribes to `OnCapabilityReportReceived` to create Endpoint instances. The interview flow per spec §6.4.2.1: EndPoint Get → for each EP: Capability Get. If Identical flag is set, queries only EP1 and clones for others. Note: The Driver handles Multi Channel de-encapsulation of incoming frames upstream (in `ProcessDataFrame`), so `ProcessUnsolicitedCommand` for `CommandEncapsulation` is only reached if frames are routed to the CC directly (not the normal path). This report-event pattern is the convention for all CC implementations.
- **`SupervisionCommandClass`** — Implements the Supervision CC (version 2) in `src/ZWave.CommandClasses/`. Provides application-level delivery confirmation for Set-type and unsolicited Report commands. A Transport CC (interviewed after Management CCs) with no mandatory interview. Provides static methods `CreateGet()`, `ParseGet()`, `CreateReport()`, `ParseReport()` used by the Driver for encapsulation/de-encapsulation. The `SupervisionGet` record wraps an encapsulated command with a 6-bit session ID (0-63) and a `StatusUpdates` flag. The `SupervisionReport` record carries status (`NoSupport`, `Working`, `Fail`, `Success`), duration, and v2's `WakeUpRequest` bit. The Driver handles de-encapsulation in its receive path and automatic Supervision Report responses. Future work: wrapping outgoing Set commands with Supervision Get (send-side, requires status state machine).

### Source Generators (`src/ZWave.BuildTools/`)

- **`CommandClassFactoryGenerator`** — Generates `CommandClassFactory` from `[CommandClass]` attributes. If you add a new CC class, just apply the attribute — the factory is auto-generated.
- **`MultilevelSensorTypeGenerator`** / **`MultilevelSensorScaleGenerator`** — Generate sensor type/scale enums and lookup tables from JSON config files in `src/ZWave.CommandClasses/Config/`.
- The BuildTools project is referenced as an analyzer in `ZWave.CommandClasses.csproj` (`OutputItemType="Analyzer"`).

### Logging

Uses `Microsoft.Extensions.Logging` with source-generated `[LoggerMessage]` attributes. Serial API log messages are in `src/ZWave.Serial/Logging.cs` (event IDs 100-199). Driver log messages are in `src/ZWave/Logging.cs` (event IDs 200-299). Follow this pattern for new log messages.

### Zero-Allocation Response Patterns

Response structs that contain variable-length collections use count + indexer methods instead of allocating arrays. For example, `GetRoutingTableEntriesResponse` exposes `RoutesCount` + `GetRoute(int index)` and `RadioDebugGetProtocolListResponse` exposes `ProtocolCount` + `GetProtocol(int index)`. Sub-structures like `RoutingTableEntry` and `TransmissionStatusReport` use `ReadOnlyMemory<byte>` backing with property getters that parse on access.

## Coding Conventions

- **Nullable reference types** enabled. **Implicit usings** enabled.
- **Naming** (from `.editorconfig`): private fields `_camelCase`, private static fields `s_camelCase`, constants `PascalCase`, interfaces `IPascalCase`.
- **No `var`** — explicit types preferred (`csharp_style_var_*` = `false`).
- Allman-style braces (`csharp_new_line_before_open_brace = all`).
- NuGet package versions are centrally managed in `Directory.Packages.props`. When adding a package, add the version there and reference it without a version in the `.csproj`.
- `InternalsVisibleTo` is set: `ZWave.Protocol` → `ZWave.Serial`, `ZWave.Serial` → `ZWave.Serial.Tests`, `ZWave.CommandClasses` → `ZWave` and `ZWave.CommandClasses.Tests`.
- **Public method naming** — Use natural English verb phrases for public CC methods, not the spec command names. The spec command names (e.g. `SupportedGet`, `DefaultReset`, `PropertiesGet`) are used for the command **enum values** and **internal command struct names**, but public methods use English word order: `GetSupportedAsync`, `ResetToDefaultAsync`, `GetPropertiesAsync`. Examples: spec `NameGet` → method `GetNameAsync`, spec `EventSupportedGet` → method `GetEventSupportedAsync`, spec `DefaultReset` → method `ResetToDefaultAsync`.
- **Binary literals for bitmasks** — prefer `0b` format (e.g. `0b0000_0010`) over `0x` hex when working with bitmask constants, as it makes the specific bit positions immediately clear.

## Testing Patterns

- Tests use **MSTest** with the `MSTest.Sdk` project SDK and the **Microsoft.Testing.Platform** runner.
- Tests run in parallel at method level.
- Serial API command tests inherit from `CommandTestBase` and use `TestSendableCommand` / `TestReceivableCommand` helper methods that verify frame structure round-tripping. `TestReceivableCommand` defaults to `NodeIdType.Short`; pass an explicit `CommandParsingContext` for 16-bit mode tests.
- `CommandDataParsingHelpersTests` covers the shared parsing helpers (`ParseCommandClasses`, `ParseNodeBitmask`).
- Test files mirror the source structure: `src/ZWave.Serial.Tests/Commands/` contains tests for each serial command; `src/ZWave.CommandClasses.Tests/` contains tests for command class implementations.
- **Ref struct properties** (e.g. `ReadOnlySpan<T>`) cannot be compared via reflection. Add them to `CommandTestBase.ExcludedComparisonProperties` and write dedicated assertion methods. `AssertExtensions` has a guard that fails loudly if non-excluded ref struct properties are encountered.

## Adding New Functionality

**New Serial API Command**: Create a struct in `src/ZWave.Serial/Commands/` implementing `ICommand<T>` (and `IRequestWithCallback<T>` if it uses callbacks). Add the command ID to `CommandId` enum. Add tests in `src/ZWave.Serial.Tests/Commands/`. Node ID parameters are `ushort`. The `ICommand<T>` interface requires `static TCommand Create(DataFrame frame, CommandParsingContext context)` — for response/callback structs that decode NodeID fields, store `context.NodeIdType` as a private property and use its `ReadNodeId(span, offset)` / `NodeIdSize()` methods; for commands without NodeIDs, simply ignore the context. For request structs, accept a `NodeIdType nodeIdType` parameter in the custom `Create()` factory method and use `nodeIdType.WriteNodeId(buffer, offset, nodeId)` to encode NodeIDs (returns the offset after the written field). Use `nodeIdType.NodeIdSize()` when computing buffer sizes. Use `CommandDataParsingHelpers` for shared parsing (node bitmasks, command class lists). Reuse existing shared types where applicable: `TransmissionOptions`, `TransmissionStatus`, `TransmissionStatusReport`, `SecurityKey`, `TxSecurityOptions`, `PowerLockType`, `DebugInterfaceProtocol`, `RoutingTableEntry` (with `RouteType`, `RouteBeamType`, `RouteSpeed`), and `NvmOperationSubCommand`/`NvmOperationStatus`.

**New SerialApiSetup Sub-Command**: The `SerialApiSetupRequest` is a `partial struct`. Each sub-command adds a factory method in a separate file (e.g. `SerialApiSetupSetNodeIdBaseType.cs`) and defines its own response struct implementing `ICommand<T>` with `CommandId => CommandId.SerialApiSetup`. Add the sub-command value to `SerialApiSetupSubcommand` enum. The response's first byte is always a `WasSubcommandSupported` flag (0 = not supported). Tests go in `SerialApiSetupTests.cs`.

**New Sub-Command Based Command**: Several Serial API commands use sub-commands (e.g. `NvmBackupRestore`, `ExtendedNvmBackupRestore`, `NetworkRestore`, `FirmwareUpdateNvm`, `NonceManagement`). These use a `partial struct` with static factory methods for each sub-command. Define the sub-command enum and status enum alongside the struct. The response struct reads the sub-command byte and status from the command parameters.

**New Command Class**: Create a class in `src/ZWave.CommandClasses/` inheriting `CommandClass<TEnum>`. Apply `[CommandClass(CommandClassId.X)]`. Constructor takes `(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)`. Define internal inner structs for each command (Set/Get/Report) implementing `ICommand` (internal enables direct unit testing). The source generator auto-registers it. Use `Endpoint` property to access the endpoint (e.g. `Endpoint.NodeId`, `Endpoint.CommandClasses`). Override `Category` to return the correct `CommandClassCategory` (Management for §6.3 CCs, Transport for §6.4 CCs; Application is the default for §6.2 CCs). The `ProcessUnsolicitedCommand` override should only handle commands that can actually arrive unsolicited (typically just Report); do not add no-op cases for Set/Get. For large CCs with many command groups, use the **partial class pattern**: the main file (`{Name}CommandClass.cs`) contains the command enum, constructor, `IsCommandSupported`, `InterviewAsync`, `ProcessUnsolicitedCommand`, and callbacks; each command group goes in a separate partial file (`{Name}CommandClass.{Group}.cs`) with its report record struct, inner command structs, and public accessor methods. Test classes follow the same split (`{Name}CommandClassTests.cs` + `{Name}CommandClassTests.{Group}.cs`). For CCs with per-key readings (e.g. per-sensor-type values, per-component state), eagerly initialize the readings dictionary to `new()` (non-nullable property); capability/discovery properties (e.g. `SupportedSensorTypes`) start `null` (nullable) and are populated during interview. Report command structs should have both `Parse` (for incoming) and `Create` (for outgoing) static methods to make them **bidirectional**. When a report contains a "Next" field used for discovery chaining (e.g. `NextIndicatorId` in Indicator Supported Report), that field is an interview implementation detail and MUST NOT be exposed in the public report record struct. Instead, have `Parse` return a value tuple `(TReport Report, TId NextId)` — the public `GetSupportedAsync` discards the next ID with `_`, while the interview loop destructures both. See the skill for details.

**New Controller Command Handler**: When a CC requires the controller to respond to incoming queries from other nodes (the "supporting side"), add handler methods in `src/ZWave/ControllerCommandHandler.cs`. This pattern is used instead of adding virtual methods to `CommandClass` because: (1) handlers need Driver/Controller context that CCs don't have, (2) it avoids polluting the CC base class, (3) it cleanly separates "controlling" (CC layer) from "supporting" (Driver layer) concerns. Steps: add a `case` in the `HandleCommand` dispatch switch for the CC ID, add private handler methods for each command, use the CC's report struct `Create` methods to construct responses, and send via `SendResponse`. CCs that currently have handlers: Association CC (Get/Set/Remove/SupportedGroupingsGet/SpecificGroupGet) and AGI CC (GroupNameGet/GroupInfoGet/CommandListGet). Future CCs needing handlers: Version, Z-Wave Plus Info, Powerlevel, Time, Manufacturer Specific.

## Protocol References

The official Z-Wave specification package can be downloaded from the [Z-Wave Alliance](https://z-wavealliance.org/development-resources-overview/specification-for-developers/). The two most relevant specs are:
- **Z-Wave Host API Specification** — Serial API frame format, handshake, initialization, command definitions (replaces the old INS12350 document)
- **Z-Wave Application Specification** — Command Class message formats, versioning, required fields

Additional resources:
- [Silicon Labs Serial API Reference](https://docs.silabs.com/z-wave/latest/zwave-api/serial-api)
- [zwave-js/specs](https://github.com/zwave-js/specs) — Community-maintained specification collection

---

Trust these instructions. Only search the codebase if information here is incomplete or found to be incorrect.
