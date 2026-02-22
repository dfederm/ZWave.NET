# ZWave.NET — Copilot Agent Instructions

## Project Overview

ZWave.NET is a .NET library implementing the Z-Wave serial protocol for communicating with Z-Wave USB controllers (e.g. UZB-7, Aeotec Z-Stick). It uses **C# preview** features (static abstract interface members). Check `global.json` for the required SDK version and the `.csproj` files for target frameworks. The solution has seven projects:

| Project | Path | Purpose |
|---|---|---|
| **ZWave.Protocol** | `src/ZWave.Protocol/` | Shared Z-Wave domain types (CommandClassId, ZWaveException, NodeType, etc.) |
| **ZWave.Serial** | `src/ZWave.Serial/` | Serial frame protocol and Serial API command structs |
| **ZWave.CommandClasses** | `src/ZWave.CommandClasses/` | Command class implementations (references Protocol, NOT Serial) |
| **ZWave** | `src/ZWave/` | Driver — orchestration layer (Driver, Controller, Node) |
| **ZWave.Serial.Tests** | `src/ZWave.Serial.Tests/` | Unit tests for the serial layer (MSTest with `MSTest.Sdk`) |
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
- **`ZWaveException`** / **`ZWaveErrorCode`** — Z-Wave-specific error types.
- **`FrequentListeningMode`** / **`NodeType`** — Node classification enums.

### Serial API Layer (`src/ZWave.Serial/`)

Implements the Z-Wave Serial API frame-level protocol (as defined by the Z-Wave Host API Specification):
- **`Frame`** / **`DataFrame`** — Wire-level frame types (SOF/ACK/NAK/CAN). `DataFrame` handles the SOF-framed data with checksum.
- **`FrameParser`** — Parses frames from a byte stream using `System.IO.Pipelines`.
- **`ZWaveSerialPortCoordinator`** — Manages the serial port, frame send/receive channels, ACK handshake, and retransmission.
- **`Commands/`** — Each Serial API command (e.g. `SendData`, `GetInitData`, `SoftReset`) is a struct implementing `ICommand<T>` (defined in `Serial/Commands/ICommand.cs`). Commands that expect a callback implement `IRequestWithCallback<T>`.
- **`CommandId` enum** — All Serial API function IDs.
- **`CommandDataParsingHelpers`** — Internal shared utilities: `ParseCommandClasses` (CC byte list → `CommandClassInfo` records) and `ParseNodeBitmask` (bitmask → `HashSet<ushort>` node IDs).

### Command Classes Layer (`src/ZWave.CommandClasses/`)

Implements Z-Wave Command Classes (Z-Wave Application Specification). This project references `ZWave.Protocol` but **not** `ZWave.Serial`, enabling mock driver implementations without a serial dependency.
- **`IEndpoint`** — Interface representing a functional sub-unit of a node. Properties: `NodeId`, `EndpointIndex`, `CommandClasses`, `GetCommandClass()`. Endpoint 0 is the "Root Device" (the node itself); endpoints 1–127 are sub-devices discovered via Multi Channel CC.
- **`INode`** — Extends `IEndpoint` with node-level properties (`FrequentListeningMode`). A node IS endpoint 0.
- **`IDriver`** — Interface abstracting the driver layer. `SendCommandAsync` accepts `nodeId` and `endpointIndex` parameters.
- **`CommandClass`** / **`CommandClass<TCommand>`** — Abstract base classes. Each CC (e.g. `BinarySwitchCommandClass`) inherits from `CommandClass<TEnum>` where `TEnum` is a byte-backed enum of commands. Takes `IDriver` and `IEndpoint` interfaces (not concrete types). The `Endpoint` property provides access to the endpoint this CC belongs to.
- **`[CommandClass(CommandClassId.X)]` attribute** — Applied to each CC class. The source generator `CommandClassFactoryGenerator` scans for this attribute and generates `CommandClassFactory` with a mapping from `CommandClassId` → constructor.
- **`ICommand` interface** (`CommandClasses/ICommand.cs`) — Different from the Serial API `ICommand`. Used for CC-level commands with `CommandClassId` and `CommandId`.
- **`CommandClassFrame`** — Wraps CC payload bytes (CC ID + Command ID + parameters).

### High-Level Objects (`src/ZWave/`)

- **`Driver`** — Entry point. Implements `IDriver`. Opens serial port, manages frame send/receive, processes unsolicited requests, coordinates request-response and callback flows.
- **`Controller`** — Represents the Z-Wave USB controller. Runs identification sequence on startup.
- **`Node`** — Represents a Z-Wave network node. Implements `INode` (and thus `IEndpoint` with `EndpointIndex = 0`). Handles interviews and command class discovery. Node IDs are `ushort` throughout the codebase to support both classic (1–232) and Long Range (256+) nodes.

### Source Generators (`src/ZWave.BuildTools/`)

- **`CommandClassFactoryGenerator`** — Generates `CommandClassFactory` from `[CommandClass]` attributes. If you add a new CC class, just apply the attribute — the factory is auto-generated.
- **`MultilevelSensorTypeGenerator`** / **`MultilevelSensorScaleGenerator`** — Generate sensor type/scale enums and lookup tables from JSON config files in `src/ZWave.CommandClasses/Config/`.
- The BuildTools project is referenced as an analyzer in `ZWave.CommandClasses.csproj` (`OutputItemType="Analyzer"`).

### Logging

Uses `Microsoft.Extensions.Logging` with source-generated `[LoggerMessage]` attributes. Serial API log messages are in `src/ZWave.Serial/Logging.cs` (event IDs 100-199). Driver log messages are in `src/ZWave/Logging.cs` (event IDs 200-299). Follow this pattern for new log messages.

## Coding Conventions

- **Nullable reference types** enabled. **Implicit usings** enabled.
- **Naming** (from `.editorconfig`): private fields `_camelCase`, private static fields `s_camelCase`, constants `PascalCase`, interfaces `IPascalCase`.
- **No `var`** — explicit types preferred (`csharp_style_var_*` = `false`).
- Allman-style braces (`csharp_new_line_before_open_brace = all`).
- NuGet package versions are centrally managed in `Directory.Packages.props`. When adding a package, add the version there and reference it without a version in the `.csproj`.
- `InternalsVisibleTo` is set: `ZWave.Serial` → `ZWave.Serial.Tests`, `ZWave.CommandClasses` → `ZWave`.

## Testing Patterns

- Tests use **MSTest** with the `MSTest.Sdk` project SDK and the **Microsoft.Testing.Platform** runner.
- Tests run in parallel at method level.
- Serial API command tests inherit from `CommandTestBase` and use `TestSendableCommand` / `TestReceivableCommand` helper methods that verify frame structure round-tripping.
- `CommandDataParsingHelpersTests` covers the shared parsing helpers (`ParseCommandClasses`, `ParseNodeBitmask`).
- Test files mirror the source structure: `src/ZWave.Serial.Tests/Commands/` contains tests for each serial command.

## Adding New Functionality

**New Serial API Command**: Create a struct in `src/ZWave.Serial/Commands/` implementing `ICommand<T>` (and `IRequestWithCallback<T>` if it uses callbacks). Add the command ID to `CommandId` enum. Add tests in `src/ZWave.Serial.Tests/Commands/`. Node ID parameters are `ushort`; cast to `(byte)nodeId` when writing to the buffer. Use `CommandDataParsingHelpers` for shared parsing (node bitmasks, command class lists).

**New Command Class**: Create a class in `src/ZWave.CommandClasses/` inheriting `CommandClass<TEnum>`. Apply `[CommandClass(CommandClassId.X)]`. Constructor takes `(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)`. Define private inner structs for each command (Set/Get/Report) implementing `ICommand`. The source generator auto-registers it. Use `Endpoint` property to access the endpoint (e.g. `Endpoint.NodeId`, `Endpoint.CommandClasses`).

## Protocol References

The official Z-Wave specification package can be downloaded from the [Z-Wave Alliance](https://z-wavealliance.org/development-resources-overview/specification-for-developers/). The two most relevant specs are:
- **Z-Wave Host API Specification** — Serial API frame format, handshake, initialization, command definitions (replaces the old INS12350 document)
- **Z-Wave Application Specification** — Command Class message formats, versioning, required fields

Additional resources:
- [Silicon Labs Serial API Reference](https://docs.silabs.com/z-wave/latest/zwave-api/serial-api)
- [zwave-js/specs](https://github.com/zwave-js/specs) — Community-maintained specification collection

---

Trust these instructions. Only search the codebase if information here is incomplete or found to be incorrect.
