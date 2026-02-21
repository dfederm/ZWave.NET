# Contributing to ZWave.NET

Thanks for your interest in contributing! Here's what you need to know.

## Getting Started

**Prerequisites:**
- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later (see `global.json`)
- A code editor with C# support (Visual Studio, VS Code with C# Dev Kit, or Rider)
- Familiarity with the Z-Wave protocol is helpful but not required - see [Protocol References](#protocol-references)

**Build and test:**

```shell
dotnet build
dotnet test
```

Please make sure the build is clean before opening a PR.

## Project Layout

| Project | Description |
|---|---|
| `src/ZWave.Protocol/` | Shared Z-Wave domain types (CommandClassId, ZWaveException, etc.) |
| `src/ZWave.Serial/` | Serial frame protocol and Serial API command structs |
| `src/ZWave.CommandClasses/` | Command class implementations |
| `src/ZWave/` | Driver - orchestration layer (Driver, Controller, Node) |
| `src/ZWave.Serial.Tests/` | Unit tests for the serial layer (MSTest) |
| `src/ZWave.Server/` | Blazor Server demo app for manual testing with real hardware |
| `src/ZWave.BuildTools/` | Roslyn source generators that run at compile time |

For a deeper dive into how these fit together, see [docs/architecture.md](docs/architecture.md).

## Code Style

The `.editorconfig` defines all formatting and naming rules. Your editor should pick these up automatically. The highlights:

- Use explicit types, not `var`
- Allman-style braces (opening brace on its own line)
- Private fields: `_camelCase`, private static fields: `s_camelCase`
- XML doc comments on public APIs

NuGet package versions are centrally managed in `Directory.Packages.props` - add the version there and reference the package without a version in the `.csproj`.

## Common Contributions

### Adding a Command Class

This is the most common type of contribution. Look at an existing command class like `BinarySwitchCommandClass.cs` as a template - it shows the full pattern:

1. Add the CC ID to the `CommandClassId` enum in `src/ZWave.Protocol/` if it's not already there
2. Create a new file in `src/ZWave.CommandClasses/` with:
   - A byte-backed enum for the commands (Set, Get, Report, etc.)
   - A class inheriting `CommandClass<TEnum>` with a `[CommandClass(CommandClassId.X)]` attribute
   - Private inner structs for each command
3. That's it - the source generator auto-registers your class in the factory

Refer to the [Z-Wave Application Specification](https://z-wavealliance.org/development-resources-overview/specification-for-developers/) for message formats and field definitions.

### Adding a Serial API Command

Look at an existing command like `GetLibraryVersion.cs` (simple request/response) or `SendData.cs` (request with callback) as a template:

1. Add the command ID to the `CommandId` enum
2. Create a new file in `src/ZWave.Serial/Commands/`
3. Add tests in `src/ZWave.Serial.Tests/Commands/` - the `CommandTestBase` class has helpers for verifying frame round-tripping

Refer to the Z-Wave Host API Specification (from the [Z-Wave Alliance specification package](https://z-wavealliance.org/development-resources-overview/specification-for-developers/)) for command definitions.

### Logging

Log messages are defined in `Logging.cs` files using source-generated `[LoggerMessage]` partial methods. Event IDs are grouped: 100–199 for Serial API (in `src/ZWave.Serial/Logging.cs`), 200–299 for Driver (in `src/ZWave/Logging.cs`). Pick the next available ID in the appropriate range.

## Submitting Changes

1. Fork the repository and create a branch from `main`
2. Make your changes
3. Make sure `dotnet build` and `dotnet test` pass
4. Open a pull request against `main`

## Protocol References

The official Z-Wave specification package can be downloaded from the [Z-Wave Alliance](https://z-wavealliance.org/development-resources-overview/specification-for-developers/). The two most relevant specs are:
- **Z-Wave Host API Specification** — Serial API frame format, handshake, initialization, command definitions
- **Z-Wave Application Specification** — Command Class message formats, versioning, required fields

Additional resources:
- [Silicon Labs Z-Wave Serial API Reference](https://docs.silabs.com/z-wave/latest/zwave-api/serial-api)
- [zwave-js/specs](https://github.com/zwave-js/specs) — Community-maintained specification collection
