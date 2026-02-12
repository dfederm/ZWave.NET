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
| `src/ZWave/` | Core library - serial protocol, command classes, driver |
| `src/ZWave.Tests/` | Unit tests (MSTest) |
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

1. Add the CC ID to the `CommandClassId` enum if it's not already there
2. Create a new file in `src/ZWave/CommandClasses/` with:
   - A byte-backed enum for the commands (Set, Get, Report, etc.)
   - A class inheriting `CommandClass<TEnum>` with a `[CommandClass(CommandClassId.X)]` attribute
   - Private inner structs for each command
3. That's it - the source generator auto-registers your class in the factory

Refer to the [Command Class specification (SDS13781)](https://www.zwavepublic.com/files/sds13781-z-wave-application-command-class-specificationpdf) for message formats and field definitions.

### Adding a Serial API Command

Look at an existing command like `GetLibraryVersion.cs` (simple request/response) or `SendData.cs` (request with callback) as a template:

1. Add the command ID to the `CommandId` enum
2. Create a new file in `src/ZWave/Serial/Commands/`
3. Add tests in `src/ZWave.Tests/Serial/Commands/` - the `CommandTestBase` class has helpers for verifying frame round-tripping

Refer to the [Serial API programming guide (INS12350)](https://www.silabs.com/documents/public/user-guides/INS12350-Serial-API-Host-Appl.-Prg.-Guide.pdf) for command definitions.

### Logging

Log messages are defined in `Logging.cs` using source-generated `[LoggerMessage]` partial methods. Event IDs are grouped: 100–199 for Serial API, 200–299 for Driver. Pick the next available ID in the appropriate range.

## Submitting Changes

1. Fork the repository and create a branch from `main`
2. Make your changes
3. Make sure `dotnet build` and `dotnet test` pass
4. Open a pull request against `main`

## Protocol References

- [INS12350 - Serial API Host Application Programming Guide](https://www.silabs.com/documents/public/user-guides/INS12350-Serial-API-Host-Appl.-Prg.-Guide.pdf)
- [SDS13781 - Z-Wave Application Command Class Specification](https://www.zwavepublic.com/files/sds13781-z-wave-application-command-class-specificationpdf)
- [Silicon Labs Z-Wave Serial API Reference](https://docs.silabs.com/z-wave/latest/zwave-api/serial-api)
- [zwave-js/specs](https://github.com/zwave-js/specs) - Full specification collection
