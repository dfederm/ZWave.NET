---
name: zwave-implement-cc
description: Guide for implementing Z-Wave Command Classes (CCs) in ZWave.NET
---

# Implementing a Z-Wave Command Class

This skill guides you through implementing a new Z-Wave Command Class (CC) in the ZWave.NET codebase. Everything goes in a **single file** in `src/ZWave.CommandClasses/`.

## Checklist

Use this as a quick reference. Each item is explained in detail below.

1. Ensure `CommandClassId` has an entry for this CC in `src/ZWave.Protocol/CommandClassId.cs`
2. Create `src/ZWave.CommandClasses/{Name}CommandClass.cs`
3. Define domain enums and structs (public types for the CC's values)
4. Define the command enum (`byte`-backed, one entry per spec command)
5. Define state struct(s) if the CC has cacheable device state
6. Implement the CC class: `[CommandClass]` attribute, constructor, `IsCommandSupported`, `InterviewAsync`, public API methods, `ProcessCommandCore`
7. Implement private inner command structs (Get, Set, Report, etc.)
8. Build with `dotnet build --configuration Release` to verify

## Prerequisites

Before starting, you need:
1. The **Command Class name** and its `CommandClassId` enum value from `src/ZWave.Protocol/CommandClassId.cs`.
2. The **Z-Wave Application Specification** for the CC, or the user's description of the commands, their IDs, and their payload formats.

If the `CommandClassId` enum does not yet have an entry for this CC, add it to `src/ZWave.Protocol/CommandClassId.cs` with the correct byte value and XML doc comment.

## File Structure

Create a single file: `src/ZWave.CommandClasses/{Name}CommandClass.cs`

The file uses the `ZWave.CommandClasses` namespace (file-scoped). The contents are ordered as follows:

1. Domain enums and structs (public) — types representing the CC's values and state
2. A **command enum** (`byte`-backed) listing every command in the CC
3. A **state struct** (if the CC reports cacheable state) — `public readonly struct`
4. The **CC class** itself — `sealed`, inherits `CommandClass<TCommand>`
5. **Private inner command structs** — one per command (Get, Set, Report, etc.), nested inside the CC class

## Design Principles

### Specification Conformance

**The implementation MUST conform to the Z-Wave Application Specification exactly.** Do not guess, assume, or improvise when the spec is unclear or ambiguous. If any aspect of a command's behavior, field encoding, validation rule, or edge case is not explicitly clear from the spec text provided, **stop and ask the user** before proceeding. This includes but is not limited to:

- Field meanings or encodings that are not fully described
- Behavior when optional fields are absent
- How to handle "reserved" values that a device might send
- Whether a field should be validated or passed through
- Whether a command's response should be cached or returned directly
- Any design decision where multiple reasonable interpretations of the spec exist

It is always better to ask than to make a wrong assumption that will need to be fixed later.

### Interpreting the Specification

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in the Z-Wave specifications are to be interpreted as described in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119). Follow the spec's normative language when deciding how to handle fields, validation, and behavior.

### Forward Compatibility and Version Handling

Z-Wave Command Classes are designed to be forward-compatible. When implementing a CC that spans multiple versions, **implement the receiver for the highest version**. In practice this means:

- **Do NOT mask reserved bits.** If bits are reserved in V1 but assigned meaning in V2, do not zero them out when parsing V1. Assume the sending device is compliant and pass through all bits. This allows the implementation to naturally handle newer devices even before the CC version is explicitly known.
- Do not add version checks that discard data. Version checks should only be used to determine whether a field is *present* in the payload (based on payload length), not to ignore data that is present.

**`Version` vs `EffectiveVersion`**: The base class provides both. `Version` (nullable `byte?`) is the actual reported version, or `null` if not yet known. `EffectiveVersion` is `Version.GetValueOrDefault(1)` — it defaults to 1 when unknown. Use `Version` in `IsCommandSupported` to express "we don't know yet" (`null`). Use `EffectiveVersion` when parsing payloads or building commands, because if we don't know the version we must assume V1 payload format.

### State vs. Direct API

Not everything a CC can do should be exposed as cached state on the CC class. Use this guideline:

- **Cached state properties** (set via `ProcessCommandCore`, queried during interview): Use for ongoing **device state and behavior** — values that change over time and that callers may want to read without sending a command. Examples: whether a light is on/off, current thermostat setpoint, battery level.
- **Direct API methods** (return a value directly, no cached property): Use for **device data and capabilities** — values that are static, queried on-demand, or used for configuration/diagnostics rather than ongoing monitoring. Examples: supported logging types, device-specific IDs, test results.

When in doubt, **ask the user** whether a value should be cached state or a direct API.

### Payload Validation

When the specification says a field value MUST be within a certain range or a frame MUST have a minimum length, **validate incoming payloads and ignore invalid ones**. In `ProcessCommandCore`, if a report frame fails validation (e.g., invalid enum value, unexpected length), skip processing it rather than storing bad data. Log a warning if appropriate, but do not throw — invalid frames from misbehaving devices should not crash the driver.

### Command Naming

Method names should be natural C# API names, not direct translations of the spec's wire-level command names. Reorder words for readability when the spec name is awkward:

| Spec command name | C# method name |
|---|---|
| `LOGGING_SUPPORTED_GET` | `GetSupportedLoggingTypesAsync` |
| `SUPPORTED_GET` | `GetSupportedAsync` |
| `INTERVAL_CAPABILITIES_GET` | `GetIntervalCapabilitiesAsync` |
| `SET` | `SetAsync` |

The inner command struct names should still match the spec ordering for traceability (e.g., `LoggingSupportedGetCommand`).

### Aggregating Partial Reports

Some commands return results across multiple report frames (indicated by a "Reports to Follow" field). These **must be aggregated** so the public API returns a single complete result. The caller should not need to know about the multi-frame nature of the response.

```csharp
public async Task<IReadOnlyList<{Item}>> GetAllItemsAsync(CancellationToken cancellationToken)
{
    var command = {Name}GetCommand.Create();
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

    List<{Item}> allItems = new List<{Item}>();
    byte reportsToFollow;
    do
    {
        CommandClassFrame reportFrame = await AwaitNextReportAsync<{Name}ReportCommand>(cancellationToken).ConfigureAwait(false);
        {Name}ReportCommand report = new {Name}ReportCommand(reportFrame);
        allItems.AddRange(report.Items);
        reportsToFollow = report.ReportsToFollow;
    }
    while (reportsToFollow > 0);

    return allItems;
}
```

## Step-by-Step Implementation

### 1. Define the Command Enum

Create a `byte`-backed enum with an entry for each command defined in the spec. Use the hex command IDs from the specification.

```csharp
namespace ZWave.CommandClasses;

public enum {Name}Command : byte
{
    /// <summary>
    /// {Description from spec}
    /// </summary>
    Set = 0x01,

    /// <summary>
    /// {Description from spec}
    /// </summary>
    Get = 0x02,

    /// <summary>
    /// {Description from spec}
    /// </summary>
    Report = 0x03,
}
```

### 2. Define Public Domain Types

Define any enums, structs, or other types needed to represent the CC's data. These go at the top of the file, before the CC class.

**State structs** follow this pattern — a `public readonly struct` with a constructor and properties:

```csharp
public readonly struct {Name}State
{
    public {Name}State(/* parameters for each field */)
    {
        // assign all properties
    }

    /// <summary>
    /// {Description}
    /// </summary>
    public {Type} {PropertyName} { get; }
}
```

### 3. Implement the CC Class

```csharp
[CommandClass(CommandClassId.{Name})]
public sealed class {Name}CommandClass : CommandClass<{Name}Command>
{
    internal {Name}CommandClass(CommandClassInfo info, IDriver driver, INode node)
        : base(info, driver, node)
    {
    }

    // Public state properties (nullable, set from reports)
    public {Name}State? State { get; private set; }

    // IsCommandSupported — return true/false/null based on version
    public override bool? IsCommandSupported({Name}Command command)
        => command switch
        {
            {Name}Command.Get => true,
            {Name}Command.Set => true,
            // For version-gated commands:
            // {Name}Command.SomeV2Command => Version.HasValue ? Version >= 2 : null,
            _ => false,
        };

    // InterviewAsync — query the device for its current state only
    internal override async Task InterviewAsync(CancellationToken cancellationToken)
    {
        _ = await GetAsync(cancellationToken).ConfigureAwait(false);
    }

    // Public API methods (Get, Set, etc.)
    // ... see patterns below ...

    // ProcessCommandCore — handle incoming report frames
    protected override void ProcessCommandCore(CommandClassFrame frame)
    {
        // ... see pattern below ...
    }

    // Private inner command structs
    // ... see patterns below ...
}
```

#### Key Points for the CC Class

- **`[CommandClass(CommandClassId.{Name})]` attribute**: This is required. A Roslyn source generator scans for this attribute and auto-generates the `CommandClassFactory` mapping. No other registration is needed.
- **Constructor**: Always `internal`, takes `(CommandClassInfo info, IDriver driver, INode node)`, calls `base(info, driver, node)`.
- **`IsCommandSupported`**: Return `true` for always-available commands, `false` for report-only/unsupported commands, and use `Version.HasValue ? Version >= N : null` for version-gated commands. Use `null` when it's unknown whether the command is supported.
- **`Dependencies`**: Only override if this CC does NOT depend on the Version CC. The default is `{ CommandClassId.Version }`. The Version CC itself overrides this to `Array.Empty<CommandClassId>()`.

### 4. Implement Public API Methods

There are two patterns depending on whether the result is cached state or returned directly.

#### Get with Cached State (state property updated by `ProcessCommandCore`)

Use when the value is ongoing device state (see "State vs. Direct API" above).

```csharp
public async Task<{Name}State> GetAsync(CancellationToken cancellationToken)
{
    var command = {Name}GetCommand.Create();
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    await AwaitNextReportAsync<{Name}ReportCommand>(cancellationToken).ConfigureAwait(false);
    return State!.Value;
}
```

The pattern is: create command → `SendCommandAsync` → `AwaitNextReportAsync<TReport>` → return the state property that `ProcessCommandCore` populated.

#### Get with Direct Return (no cached state)

Use when the value is device data queried on demand. Parse the report frame directly and return the result without storing it on the CC class.

```csharp
public async Task<byte> GetCommandClassVersionAsync(CommandClassId commandClassId, CancellationToken cancellationToken)
{
    var command = {Name}CommandClassGetCommand.Create(commandClassId);
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    CommandClassFrame reportFrame = await AwaitNextReportAsync<{Name}CommandClassReportCommand>(
        predicate: frame =>
        {
            {Name}CommandClassReportCommand report = new {Name}CommandClassReportCommand(frame);
            return report.RequestedCommandClass == commandClassId;
        },
        cancellationToken).ConfigureAwait(false);
    {Name}CommandClassReportCommand reportCommand = new {Name}CommandClassReportCommand(reportFrame);
    return reportCommand.CommandClassVersion;
}
```

Use the predicate overload of `AwaitNextReportAsync` when you need to match a specific report (e.g., by a key field in the response).

#### Set (fire and forget)

```csharp
public async Task SetAsync({parameters}, CancellationToken cancellationToken)
{
    var command = {Name}SetCommand.Create(EffectiveVersion, {parameters});
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
}
```

Pass `EffectiveVersion` when the command payload varies by version (see "Version vs EffectiveVersion" above).

### 5. Implement `ProcessCommandCore`

This method handles all incoming report frames. It updates cached state properties and completes awaited report tasks.

```csharp
protected override void ProcessCommandCore(CommandClassFrame frame)
{
    switch (({Name}Command)frame.CommandId)
    {
        case {Name}Command.Set:
        case {Name}Command.Get:
        {
            // We don't expect to recieve these commands
            break;
        }
        case {Name}Command.Report:
        {
            // Validate payload length per spec requirements
            if (frame.CommandParameters.Length < 1)
            {
                break;
            }

            var command = new {Name}ReportCommand(frame, EffectiveVersion);

            // Validate field values per spec MUST constraints
            // (e.g., skip if an enum value is outside the defined range)

            State = new {Name}State(
                command.Property1,
                command.Property2);
            break;
        }
    }
}
```

- Group all outbound-only commands (Set, Get) in a single case with a comment: `// We don't expect to recieve these commands`
- For each report, construct the inner command struct, then update the state property.
- Pass `EffectiveVersion` to report commands that have version-dependent fields.
- **Validate payloads**: If the spec defines MUST constraints on field values or frame lengths, check them before storing. Silently ignore (break out of the case) if a frame is invalid. Do not throw.
- Reports that map to direct API methods (no cached state) still need a case here — even if the case body is empty, because `ProcessCommand` in the base class handles completing the awaited report task after calling `ProcessCommandCore`.

### 6. Implement Private Inner Command Structs

Each command is a `private readonly struct` implementing `ICommand` (from `ZWave.CommandClasses`), nested inside the CC class. There are three patterns:

#### Get Command (no parameters)

```csharp
private readonly struct {Name}GetCommand : ICommand
{
    public {Name}GetCommand(CommandClassFrame frame)
    {
        Frame = frame;
    }

    public static CommandClassId CommandClassId => CommandClassId.{Name};

    public static byte CommandId => (byte){Name}Command.Get;

    public CommandClassFrame Frame { get; }

    public static {Name}GetCommand Create()
    {
        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId);
        return new {Name}GetCommand(frame);
    }
}
```

#### Set Command (with parameters)

```csharp
private readonly struct {Name}SetCommand : ICommand
{
    public {Name}SetCommand(CommandClassFrame frame)
    {
        Frame = frame;
    }

    public static CommandClassId CommandClassId => CommandClassId.{Name};

    public static byte CommandId => (byte){Name}Command.Set;

    public CommandClassFrame Frame { get; }

    public static {Name}SetCommand Create(byte version, {parameter types})
    {
        // Build the command parameters byte array
        Span<byte> commandParameters = stackalloc byte[{size}];
        commandParameters[0] = {value};
        // ... fill remaining bytes ...

        CommandClassFrame frame = CommandClassFrame.Create(CommandClassId, CommandId, commandParameters);
        return new {Name}SetCommand(frame);
    }
}
```

For simple parameters, prefer the collection expression syntax:
```csharp
ReadOnlySpan<byte> commandParameters = [(byte)param1, param2];
```

For version-conditional fields (e.g., duration added in V2):
```csharp
bool includeDuration = version >= 2 && duration.HasValue;
Span<byte> commandParameters = stackalloc byte[1 + (includeDuration ? 1 : 0)];
```

#### Report Command (parsing received frame)

```csharp
private readonly struct {Name}ReportCommand : ICommand
{
    private readonly byte _version;

    public {Name}ReportCommand(CommandClassFrame frame, byte version)
    {
        Frame = frame;
        _version = version;
    }

    public static CommandClassId CommandClassId => CommandClassId.{Name};

    public static byte CommandId => (byte){Name}Command.Report;

    public CommandClassFrame Frame { get; }

    /// <summary>
    /// {Description}
    /// </summary>
    public {Type} {Property} => Frame.CommandParameters.Span[0];

    // For fields added in later versions, check payload length (NOT version) for presence:
    public {Type}? {OptionalProperty} => Frame.CommandParameters.Length > 1
        ? Frame.CommandParameters.Span[1]
        : null;
}
```

- Report structs take `(CommandClassFrame frame, byte version)` when they have version-dependent fields, or just `(CommandClassFrame frame)` if all fields exist in V1.
- Parse bytes directly from `Frame.CommandParameters.Span[index]`.
- For multi-byte values, use extension methods: `.ToUInt16BE()`, `.ToUInt32BE()`, `.ToInt32BE()`.
- For bitmask fields, use bit manipulation: `(Frame.CommandParameters.Span[N] & 0b0000_1111)`.
- **Do NOT mask reserved bits.** If a field has reserved bits in one version but they are defined in a later version, parse all bits unconditionally. This ensures forward compatibility.
- For optional fields added in later versions, check **payload length** to determine if the field is present. This is the forward-compatible approach — a V1 device sends a shorter payload, a V2+ device sends a longer one.
- Report commands do **not** have a `Create` method — they are only constructed from received frames.

## Common Patterns

### Version-Gated Commands

When a command was added in version N of the CC:
- `IsCommandSupported`: `Version.HasValue ? Version >= N : null`
- `InterviewAsync`: `if (IsCommandSupported({Name}Command.X).GetValueOrDefault()) { ... }`

### Interview Pattern

The interview queries the device for its current **state** (not all data). Only call Gets during the interview for values that should be cached as state properties. Common patterns:
- **Simple**: Just call `GetAsync` for the primary state.
- **With version-gated extras**: Call Get, then conditionally call additional Gets for state added in later versions.
- **With supported-types discovery**: First query supported types, then iterate and Get each one's current state.
- **No interview needed**: Return `Task.CompletedTask` for CCs where all functionality is on-demand (e.g. Powerlevel CC).

### Shared Types

These existing types are available for use:
- `DurationReport` — Duration from a report (byte → TimeSpan)
- `DurationSet` — Duration for a set command (TimeSpan → byte)
- `GenericValue` — Generic 0-100/on-off value
- `CommandClassFrame` — The raw frame wrapper (CC ID + Command ID + parameters)

### Multi-byte Value Encoding

Use the extension methods in `BinaryExtensions.cs`:
- Reading: `span.ToUInt16BE()`, `span.ToUInt32BE()`, `span.ToInt32BE()`
- Writing: `value.WriteBytesBE(span)`

### Bitmask Parsing (for Supported* reports)

```csharp
HashSet<{EnumType}> supported = new HashSet<{EnumType}>();
ReadOnlySpan<byte> bitMask = Frame.CommandParameters.Span.Slice(offset, length);
for (int byteNum = 0; byteNum < bitMask.Length; byteNum++)
{
    for (int bitNum = 0; bitNum < 8; bitNum++)
    {
        if ((bitMask[byteNum] & (1 << bitNum)) != 0)
        {
            {EnumType} value = ({EnumType})((byteNum << 3) + bitNum);
            supported.Add(value);
        }
    }
}
```

## Code Style Requirements

- **No `var`** — use explicit types (e.g. `CommandClassFrame frame = ...`, not `var frame = ...`). Exception: `var` is acceptable for the result of a `Create()` call on a command struct, since the type is obvious from the right-hand side.
- **Nullable reference types** are enabled — use `?` for nullable properties.
- **Allman-style braces** — opening brace on its own line.
- **XML doc comments** on all public types, properties, and methods.
- **`ConfigureAwait(false)`** on every `await`.
- **`sealed`** CC classes.
- **`internal`** constructor on the CC class.
- **Warnings are errors** — the build will fail on any warning.

## Build Validation

After implementing, build from the repo root:

```shell
dotnet build --configuration Release
```

The source generator will automatically pick up the `[CommandClass]` attribute and register the new CC in the factory. No manual registration is needed.

Do **not** run `dotnet format` — it cannot run source generators and will produce false errors.
