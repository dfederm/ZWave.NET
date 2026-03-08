---
name: zwave-implement-cc
description: Guide for implementing Z-Wave Command Classes (CCs) in ZWave.NET
---

# Implementing a Z-Wave Command Class

This skill guides you through implementing a new Z-Wave Command Class (CC) in the ZWave.NET codebase, in `src/ZWave.CommandClasses/`.

## Checklist

Use this as a quick reference. Each item is explained in detail below.

1. Ensure `CommandClassId` has an entry for this CC in `src/ZWave.Protocol/CommandClassId.cs`
2. Create `src/ZWave.CommandClasses/{Name}CommandClass.cs`
3. Define domain enums and report record structs (public types for the CC's values)
4. Define the command enum (`byte`-backed, one entry per spec command)
5. Implement the CC class: `[CommandClass]` attribute, constructor, `IsCommandSupported`, `InterviewAsync`, `ProcessUnsolicitedCommand`
6. Implement internal inner command structs (Get, Set, Report, etc.) with static `Parse` methods on report commands
7. For large CCs, split into partial classes (see "Partial Class Pattern" below)
8. Build with `dotnet build --configuration Release` to verify

## Prerequisites

Before starting, you need:
1. The **Command Class name** and its `CommandClassId` enum value from `src/ZWave.Protocol/CommandClassId.cs`.
2. The **Z-Wave Application Specification** for the CC, or the user's description of the commands, their IDs, and their payload formats.

If the `CommandClassId` enum does not yet have an entry for this CC, add it to `src/ZWave.Protocol/CommandClassId.cs` with the correct byte value and XML doc comment.

## File Structure

For simple CCs with few commands, a single file is sufficient: `src/ZWave.CommandClasses/{Name}CommandClass.cs`

For larger CCs with multiple command groups, use the **partial class pattern** described below.

The file uses the `ZWave.CommandClasses` namespace (file-scoped) and must include `using Microsoft.Extensions.Logging;`. The contents are ordered as follows:

1. Domain enums and report record structs (public) — types representing the CC's values
2. A **command enum** (`byte`-backed) listing every command in the CC
3. The **CC class** itself — `sealed`, inherits `CommandClass<TCommand>`
4. **Internal inner command structs** — one per command (Get, Set, Report, etc.), nested inside the CC class

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
- **Do NOT use version checks to determine if fields are present.** Always use **payload length** instead. A V2+ device may send extended fields before version negotiation is complete, and a V1 device will simply send a shorter payload. Checking `_version >= 2` to decide if a field is present is a forward-compatibility violation.
- Do not add version checks that discard data. Version checks should only be used to determine whether a command is *supported* (in `IsCommandSupported`), not to ignore data that is present in a payload.

**`Version` vs `EffectiveVersion`**: The base class provides both. `Version` (nullable `byte?`) is the actual reported version, or `null` if not yet known. `EffectiveVersion` is `Version.GetValueOrDefault(1)` — it defaults to 1 when unknown. Use `Version` in `IsCommandSupported` to express "we don't know yet" (`null`). Use `EffectiveVersion` when building outbound commands where the command payload format varies by version.

### Solicited vs. Unsolicited Reports

The base class `CommandClass.ProcessCommand` distinguishes between **solicited** and **unsolicited** reports:

- **Solicited reports** are responses to a Get command. The base class matches incoming frames against registered awaiters (from `AwaitNextReportAsync`) and completes them directly. The `GetAsync` method then calls the report's `Parse` method on the returned frame. `ProcessUnsolicitedCommand` is **not** called for solicited reports.
- **Unsolicited reports** are spontaneous updates from the device (e.g., a light switch was physically toggled). These are dispatched to `ProcessUnsolicitedCommand`, which calls `Parse` and updates cached state.

This means `Parse` is called exactly **once** per report — either in `GetAsync` (solicited) or `ProcessUnsolicitedCommand` (unsolicited), never both.

### Report Events

For any report type that can be received unsolicited, the CC class MUST expose an `event Action<TReport>?` event that fires on **both** solicited and unsolicited reports. This allows library consumers (e.g., `Node`) to monitor all incoming data without polling.

**Naming**: `On{ReportType}Received` (e.g., `OnEndpointReportReceived`, `OnCapabilityReportReceived`).

**Raising events**:
- In `GetAsync` methods: raise the event after `Parse`, before returning.
- In `ProcessUnsolicitedCommand`: raise the event after `Parse`.
- This means every report, regardless of path, fires the event exactly once.

```csharp
// In the CC class (or the appropriate partial class for the command group):
public event Action<{Name}Report>? On{Name}ReportReceived;

// In GetAsync:
public async Task<{Name}Report> GetAsync(CancellationToken cancellationToken)
{
    ...
    {Name}Report report = {Name}ReportCommand.Parse(reportFrame, Logger);
    LastReport = report;
    On{Name}ReportReceived?.Invoke(report);
    return report;
}

// In ProcessUnsolicitedCommand:
case {Name}Command.Report:
{
    {Name}Report report = {Name}ReportCommand.Parse(frame, Logger);
    LastReport = report;
    On{Name}ReportReceived?.Invoke(report);
    break;
}
```

**Which reports get events**: All report types that a device could send unsolicited. Reports that are only ever solicited (e.g., responses to capability queries that never change) do not need events, but MAY have them if useful for monitoring.

### State vs. Direct API

Not everything a CC can do should be exposed as cached state on the CC class. Use this guideline:

- **Cached state properties** (updated in both `GetAsync` and `ProcessUnsolicitedCommand`): Use for ongoing **device state and behavior** — values that change over time and that callers may want to read without sending a command. Examples: whether a light is on/off, current thermostat setpoint, battery level.
  - Name the primary report property **`LastReport`** (e.g., `BasicReport? LastReport`).
  - For secondary report properties, use **`Last{Descriptive}`** (e.g., `LastHealthReport`, `LastTestResult`, `LastNotification`, `LastInterval`).
- **Cached capability/static properties** (queried once, don't change at runtime): Use descriptive names **without** the `Last` prefix. Examples: `HardwareInfo`, `Capabilities`, `SupportedSensorTypes`, `SwitchType`, `IntervalCapabilities`.
- **Direct API methods** (return a value directly, no cached property): Use for **device data** queried on-demand. Examples: supported logging types, device-specific IDs, command class versions.

When in doubt, **ask the user** whether a value should be cached state or a direct API.

### Nullable vs. Eager Initialization for State Properties

CCs that track per-key readings (e.g., per-sensor-type readings, per-color-component state) use two kinds of cached state:

- **Readings dictionaries** (per-key report values that change over time): **Eagerly initialize** to `new()` at the field declaration. The public property is **non-nullable**. Empty means "no readings received yet"; populated means readings exist. This ensures callers and UI code can always enumerate without null checks, and unsolicited reports are never silently dropped.
- **Capability properties** (supported types, supported scales — queried once during interview): Start as **`null`**. The public property is **nullable**. `null` means "not yet interviewed / version doesn't support this query". This lets callers and UI distinguish "unknown" from "known empty."

```csharp
// ✅ Readings dictionary — eagerly initialized, non-nullable
private Dictionary<SensorType, SensorReport?> _sensorValues = new();
public IReadOnlyDictionary<SensorType, SensorReport?> SensorValues => _sensorValues;

// ✅ Capability property — starts null, nullable
private Dictionary<SensorType, IReadOnlySet<Scale>?>? _supportedScales;
public IReadOnlyDictionary<SensorType, IReadOnlySet<Scale>?>? SupportedScales => _supportedScales;
```

For CCs with a version-dependent discovery path (e.g., MultilevelSensor V1-4 has no `SupportedSensorGet`), the readings dictionary is always available regardless of version, while capability properties remain `null` on older versions. When `GetSupportedAsync` runs, it rebuilds the readings dictionary to include keys for every supported type (preserving any existing values).

### Payload Validation

Validation is performed in the report command's static `Parse` method. The `Parse` method:

1. **Validates** the frame (e.g., minimum payload length, field value ranges)
2. **Logs a warning** via the `ILogger` parameter describing what's wrong
3. **Calls `ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, ...)`** with a concise message

The base class handles exception propagation differently depending on the report path:

- **Solicited reports**: The exception propagates naturally from `Parse` in `GetAsync` to the caller. The caller sees the exception.
- **Unsolicited reports**: The base class wraps `ProcessUnsolicitedCommand` in a try/catch and swallows the exception. Since `Parse` already logged a warning before throwing, no information is lost.

This means `Parse` methods should always log-then-throw on validation failure — they do not need to worry about which path is calling them.

**Multi-stage validation for variable-length payloads:** Some reports contain a length or size field that determines how many subsequent bytes to read (e.g., a `valueSize` field). In these cases, validate in stages:
1. First validate the minimum fixed-length header
2. Read the size/length field
3. Validate that the remaining payload is large enough for the declared size
4. Only then slice/access the variable-length data

```csharp
if (frame.CommandParameters.Length < 2)  // minimum header
    throw ...;

int valueSize = frame.CommandParameters.Span[1] & 0b0000_0111;

if (frame.CommandParameters.Length < 2 + valueSize)  // header + declared data
    throw ...;

ReadOnlySpan<byte> valueBytes = frame.CommandParameters.Span.Slice(2, valueSize);
```

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

The report command struct should expose a `ParseInto` method that takes the collection to append to (avoiding intermediate list allocations) and returns only the metadata (e.g. `reportsToFollow`). See `AssociationReportCommand.ParseInto` for the reference implementation.

```csharp
// In the report command struct:
public static byte ParseInto(
    CommandClassFrame frame,
    List<{Item}> items,
    ILogger logger)
{
    // validate frame...
    byte reportsToFollow = span[0];
    // parse items and add to the provided list...
    items.Add(...);
    return reportsToFollow;
}

// In the CC class:
public async Task<IReadOnlyList<{Item}>> GetAllItemsAsync(CancellationToken cancellationToken)
{
    var command = {Name}GetCommand.Create();
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

    List<{Item}> allItems = [];
    byte reportsToFollow;
    do
    {
        CommandClassFrame reportFrame = await AwaitNextReportAsync<{Name}ReportCommand>(cancellationToken).ConfigureAwait(false);
        reportsToFollow = {Name}ReportCommand.ParseInto(reportFrame, allItems, Logger);
    }
    while (reportsToFollow > 0);

    return allItems;
}
```

## Step-by-Step Implementation

### 1. Define the Command Enum

Create a `byte`-backed enum with an entry for each command defined in the spec. Use the hex command IDs from the specification.

```csharp
using Microsoft.Extensions.Logging;

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

### 2. Define Public Report Types

Define any enums and report record structs needed to represent the CC's data. These go at the top of the file, before the CC class.

**Report structs** are `public readonly record struct` with positional (primary constructor) parameters:

```csharp
/// <summary>
/// Represents a {Name} Report received from a device.
/// </summary>
public readonly record struct {Name}Report(
    /// <summary>
    /// {Description}
    /// </summary>
    {Type} {PropertyName},

    /// <summary>
    /// {Description of optional field}
    /// </summary>
    {Type}? {OptionalPropertyName});
```

Key points:
- Use `readonly record struct` with positional parameters — not manual constructors and properties.
- Name the type `{Name}Report` (not `{Name}State`). The CC class property is `LastReport` (not `State`).
- For fields added in later CC versions, make the type nullable (e.g., `GenericValue?`).
- XML doc comments go on each positional parameter.

### 3. Implement the CC Class

```csharp
[CommandClass(CommandClassId.{Name})]
public sealed class {Name}CommandClass : CommandClass<{Name}Command>
{
    internal {Name}CommandClass(
        CommandClassInfo info,
        IDriver driver,
        IEndpoint endpoint,
        ILogger logger)
        : base(info, driver, endpoint, logger)
    {
    }

    /// <summary>
    /// Gets the last report received from the device.
    /// </summary>
    public {Name}Report? LastReport { get; private set; }

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

    // ProcessUnsolicitedCommand — handle unsolicited incoming report frames
    protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
    {
        // ... see pattern below ...
    }

    // Internal inner command structs
    // ... see patterns below ...
}
```

#### Key Points for the CC Class

- **`[CommandClass(CommandClassId.{Name})]` attribute**: This is required. A Roslyn source generator scans for this attribute and auto-generates the `CommandClassFactory` mapping. No other registration is needed.
- **Constructor**: Always `internal`, takes `(CommandClassInfo info, IDriver driver, IEndpoint endpoint, ILogger logger)`, calls `base(info, driver, endpoint, logger)`. The `Endpoint` property provides access to the endpoint this CC belongs to (e.g. `Endpoint.NodeId`, `Endpoint.CommandClasses`, `Endpoint.GetCommandClass()`).
- **`IsCommandSupported`**: Return `true` for always-available commands, `false` for report-only/unsupported commands, and use `Version.HasValue ? Version >= N : null` for version-gated commands. Use `null` when it's unknown whether the command is supported.
- **`Dependencies`**: Only override if this CC does NOT depend on the Version CC. The default is `{ CommandClassId.Version }`. The Version CC itself overrides this to `Array.Empty<CommandClassId>()`.

### 4. Implement Public API Methods

There are two patterns depending on whether the result is cached state or returned directly. In both patterns, the `GetAsync` method receives the raw frame from `AwaitNextReportAsync` and calls `Parse` directly — `ProcessUnsolicitedCommand` is NOT called for solicited reports.

#### Get with Cached State

Use when the value is ongoing device state (see "State vs. Direct API" above).

```csharp
public async Task<{Name}Report> GetAsync(CancellationToken cancellationToken)
{
    {Name}GetCommand command = {Name}GetCommand.Create();
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    CommandClassFrame reportFrame = await AwaitNextReportAsync<{Name}ReportCommand>(cancellationToken).ConfigureAwait(false);
    {Name}Report report = {Name}ReportCommand.Parse(reportFrame, Logger);
    LastReport = report;
    On{Name}ReportReceived?.Invoke(report);
    return report;
}
```

The pattern is: create command → `SendCommandAsync` → `AwaitNextReportAsync<TReport>` → `Parse` the returned frame → update `LastReport` → raise event → return.

#### Get with Direct Return (no cached state)

Use when the value is device data queried on demand. Parse the report frame directly and return the result without storing it on the CC class.

```csharp
public async Task<byte> GetCommandClassVersionAsync(CommandClassId commandClassId, CancellationToken cancellationToken)
{
    var command = VersionCommandClassGetCommand.Create(commandClassId);
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
    CommandClassFrame reportFrame = await AwaitNextReportAsync<VersionCommandClassReportCommand>(
        predicate: frame =>
        {
            return frame.CommandParameters.Length > 0
                && (CommandClassId)frame.CommandParameters.Span[0] == commandClassId;
        },
        cancellationToken).ConfigureAwait(false);
    (CommandClassId _, byte commandClassVersion) = VersionCommandClassReportCommand.Parse(reportFrame, Logger);
    return commandClassVersion;
}
```

Use the predicate overload of `AwaitNextReportAsync` when you need to match a specific report (e.g., by a key field in the response).

**IMPORTANT: Predicate functions must NOT call `Parse`.** Predicates run on every incoming frame for this CC, including non-matching frames that may be malformed. Calling `Parse` in a predicate would:
1. Log spurious warnings for non-matching frames
2. Throw exceptions that break the awaiter matching loop
3. Parse the same frame twice (once in predicate, once after match)

Instead, predicates should read the raw `frame.CommandParameters.Span` bytes directly with bounds checks:
```csharp
predicate: frame =>
{
    return frame.CommandParameters.Length > 0
        && (SomeEnum)frame.CommandParameters.Span[0] == expectedValue;
}
```

#### Set (fire and forget)

```csharp
public async Task SetAsync({parameters}, CancellationToken cancellationToken)
{
    var command = {Name}SetCommand.Create(EffectiveVersion, {parameters});
    await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
}
```

Pass `EffectiveVersion` when the command payload varies by version (see "Version vs EffectiveVersion" above).

### 5. Implement `ProcessUnsolicitedCommand`

This method handles **unsolicited** incoming report frames only (device-initiated, not responses to Get commands). It updates cached state properties and raises report events. The base class calls this only when no awaiter matched the frame.

Only include cases for commands that can actually arrive unsolicited — typically Report commands. Do **not** add no-op cases for outbound-only commands like Set and Get.

```csharp
protected override void ProcessUnsolicitedCommand(CommandClassFrame frame)
{
    switch (({Name}Command)frame.CommandId)
    {
        case {Name}Command.Report:
        {
            {Name}Report report = {Name}ReportCommand.Parse(frame, Logger);
            LastReport = report;
            On{Name}ReportReceived?.Invoke(report);
            break;
        }
    }
}
```

Key points:
- Only handle commands that can arrive unsolicited (typically Report). Do not add cases for Set/Get.
- For each report, call the static `Parse` method, assign to cached state, and raise the event.
- **Do NOT validate payloads here** — validation belongs in `Parse`. If `Parse` throws, the base class catches and swallows the exception (Parse already logged a warning).
- Reports that map only to direct API methods (no cached state) do **not** need a case here — they are only received as solicited reports in `GetAsync`.

### 6. Implement Private Inner Command Structs

Each command is an `internal readonly struct` implementing `ICommand` (from `ZWave.CommandClasses`), nested inside the CC class. The `internal` visibility enables direct unit testing of command creation and report parsing. There are three patterns:

#### Get Command (no parameters)

```csharp
internal readonly struct {Name}GetCommand : ICommand
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
internal readonly struct {Name}SetCommand : ICommand
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

#### Report Command (with static Parse method)

```csharp
internal readonly struct {Name}ReportCommand : ICommand
{
    public {Name}ReportCommand(CommandClassFrame frame)
    {
        Frame = frame;
    }

    public static CommandClassId CommandClassId => CommandClassId.{Name};

    public static byte CommandId => (byte){Name}Command.Report;

    public CommandClassFrame Frame { get; }

    public static {Name}Report Parse(CommandClassFrame frame, ILogger logger)
    {
        // Validate minimum payload length
        if (frame.CommandParameters.Length < 1)
        {
            logger.LogWarning("{Name} Report frame is too short ({Length} bytes)", frame.CommandParameters.Length);
            ZWaveException.Throw(ZWaveErrorCode.InvalidPayload, "{Name} Report frame is too short");
        }

        ReadOnlySpan<byte> span = frame.CommandParameters.Span;

        {Type} requiredField = span[0];

        // For fields added in later versions, check payload length (NOT version):
        {Type}? optionalField = span.Length > 1
            ? span[1]
            : null;

        return new {Name}Report(requiredField, optionalField);
    }
}
```

Key points for report commands:
- Report commands have a **static `Parse` method** that takes `(CommandClassFrame frame, ILogger logger)` and returns the public report record struct. They do NOT store version or have instance properties for parsed fields.
- `Parse` validates the frame, logs warnings, and throws on validation errors. Both `GetAsync` and `ProcessUnsolicitedCommand` call `Parse` — the base class handles exception propagation appropriately for each path.
- Parse bytes directly from `frame.CommandParameters.Span[index]`.
- For multi-byte values, use extension methods: `.ToUInt16BE()`, `.ToUInt32BE()`, `.ToInt32BE()`.
- For bitmask fields, use bit manipulation: `(span[N] & 0b0000_1111)`.
- **Do NOT mask reserved bits.** If a field has reserved bits in one version but they are defined in a later version, parse all bits unconditionally. This ensures forward compatibility.
- For optional fields added in later versions, check **payload length** to determine if the field is present. Never use version checks for this.
- Report commands do **not** have a `Create` method — they are only used to identify the command ID for `AwaitNextReportAsync<T>` dispatch.

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
ReadOnlySpan<byte> bitMask = frame.CommandParameters.Span.Slice(offset, length);
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

## Partial Class Pattern for Large CCs

When a CC has many command groups (e.g., Multi Channel CC with Endpoint, Capability, EndpointFind, CommandEncapsulation, AggregatedMembers), split the implementation into partial classes to keep files manageable.

### File naming convention

- **Main file**: `{Name}CommandClass.cs` — contains the command enum, class declaration with constructor, `IsCommandSupported`, `InterviewAsync`, and `ProcessUnsolicitedCommand`
- **Group files**: `{Name}CommandClass.{Group}.cs` — each contains a command group (Get/Report or Get/Set/Report triplet), the associated public report record struct, the inner command structs, the public accessor methods, and the `event Action<TReport>?` event for that report

### Example: Multi Channel CC

```
MultiChannelCommandClass.cs                        — enum, constructor, interview, unsolicited handler
MultiChannelCommandClass.Endpoint.cs               — Endpoint Get/Report, record, event
MultiChannelCommandClass.Capability.cs             — Capability Get/Report, record, event
MultiChannelCommandClass.EndpointFind.cs           — Endpoint Find/Find Report, accessor
MultiChannelCommandClass.CommandEncapsulation.cs   — Encapsulation create/parse, event
MultiChannelCommandClass.AggregatedMembers.cs      — Aggregated Members Get/Report, accessor
```

### When to split

- **Always start with a single file.** Only split when the file grows large enough that related command groups become hard to navigate (roughly 300+ lines).
- Each partial file should be **self-contained** for its command group — a reader should be able to understand that group's wire format, parsing, and API without reading other files.
- The main file should contain **only CC-wide concerns**: the command enum, constructor, `IsCommandSupported`, `InterviewAsync`, `ProcessUnsolicitedCommand`, and any callbacks or shared state.

### Test files

Test classes follow the same partial class split:
- **Main file**: `{Name}CommandClassTests.cs` — `[TestClass] public partial class {Name}CommandClassTests { }`
- **Group files**: `{Name}CommandClassTests.{Group}.cs` — tests for each command group

## Build Validation

After implementing, build from the repo root:

```shell
dotnet build --configuration Release
```

The source generator will automatically pick up the `[CommandClass]` attribute and register the new CC in the factory. No manual registration is needed.

Do **not** run `dotnet format` — it cannot run source generators and will produce false errors.
