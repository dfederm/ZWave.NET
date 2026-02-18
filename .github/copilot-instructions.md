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

Implements the Z-Wave Serial API (INS12350) frame-level protocol:
- **`Frame`** / **`DataFrame`** — Wire-level frame types (SOF/ACK/NAK/CAN). `DataFrame` handles the SOF-framed data with checksum.
- **`FrameParser`** — Parses frames from a byte stream using `System.IO.Pipelines`.
- **`ZWaveSerialPortCoordinator`** — Manages the serial port, frame send/receive channels, ACK handshake, and retransmission.
- **`Commands/`** — Each Serial API command (e.g. `SendData`, `GetInitData`, `SoftReset`) is a struct implementing `ICommand<T>` (defined in `Serial/Commands/ICommand.cs`). Commands that expect a callback implement `IRequestWithCallback<T>`.
- **`CommandId` enum** — All Serial API function IDs.

### Command Classes Layer (`src/ZWave.CommandClasses/`)

Implements Z-Wave Command Classes (SDS13781). This project references `ZWave.Protocol` but **not** `ZWave.Serial`, enabling mock driver implementations without a serial dependency.
- **`CommandClass`** / **`CommandClass<TCommand>`** — Abstract base classes. Each CC (e.g. `BinarySwitchCommandClass`) inherits from `CommandClass<TEnum>` where `TEnum` is a byte-backed enum of commands. Takes `IDriver` and `INode` interfaces (not concrete types).
- **`IDriver`** / **`INode`** — Interfaces defined here that abstract the driver layer. `Driver` and `Node` implement these in the ZWave project.
- **`[CommandClass(CommandClassId.X)]` attribute** — Applied to each CC class. The source generator `CommandClassFactoryGenerator` scans for this attribute and generates `CommandClassFactory` with a mapping from `CommandClassId` → constructor.
- **`ICommand` interface** (`CommandClasses/ICommand.cs`) — Different from the Serial API `ICommand`. Used for CC-level commands with `CommandClassId` and `CommandId`.
- **`CommandClassFrame`** — Wraps CC payload bytes (CC ID + Command ID + parameters).

### High-Level Objects (`src/ZWave/`)

- **`Driver`** — Entry point. Implements `IDriver`. Opens serial port, manages frame send/receive, processes unsolicited requests, coordinates request-response and callback flows.
- **`Controller`** — Represents the Z-Wave USB controller. Runs identification sequence on startup.
- **`Node`** — Represents a Z-Wave network node. Implements `INode`. Handles interviews and command class discovery.

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
- Test files mirror the source structure: `src/ZWave.Serial.Tests/Commands/` contains tests for each serial command.

## Adding New Functionality

**New Serial API Command**: Create a struct in `src/ZWave.Serial/Commands/` implementing `ICommand<T>` (and `IRequestWithCallback<T>` if it uses callbacks). Add the command ID to `CommandId` enum. Add tests in `src/ZWave.Serial.Tests/Commands/`.

**New Command Class**: Create a class in `src/ZWave.CommandClasses/` inheriting `CommandClass<TEnum>`. Apply `[CommandClass(CommandClassId.X)]`. Define private inner structs for each command (Set/Get/Report) implementing `ICommand`. The source generator auto-registers it.

## Protocol References

- Serial API: [INS12350 Programming Guide](https://www.silabs.com/documents/public/user-guides/INS12350-Serial-API-Host-Appl.-Prg.-Guide.pdf), [Silicon Labs Serial API Docs](https://docs.silabs.com/z-wave/latest/zwave-api/serial-api)
- Command Classes: [SDS13781 Specification](https://www.zwavepublic.com/files/sds13781-z-wave-application-command-class-specificationpdf)
- Full spec collection: [zwave-js/specs](https://github.com/zwave-js/specs)

---

Trust these instructions. Only search the codebase if information here is incomplete or found to be incorrect.
