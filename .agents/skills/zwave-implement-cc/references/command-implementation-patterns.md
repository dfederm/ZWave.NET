# Command Implementation Patterns

Load this reference while defining command enums, public report types, command-class APIs, and inner command structs.

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
- **"Next" fields for discovery chaining** (e.g. `NextIndicatorId` in Indicator Supported Report) are interview implementation details and MUST NOT appear in the public report struct. Instead, have the `Parse` method return a value tuple `(TReport Report, TId NextId)`. The public `GetSupportedAsync` discards the next ID with `_`, while the interview loop destructures both values to follow the chain.

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

Use when the value is ongoing device state (see [State vs. Direct API](reports-state-and-validation.md#state-vs-direct-api)).

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

Pass `EffectiveVersion` when the command payload varies by version (see [Forward Compatibility and Version Handling](specification-conformance.md#forward-compatibility-and-version-handling)).

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
- Report commands should also have a **static `Create` method** for **bidirectional** support (constructing outgoing frames, e.g. for controller-side responses and unit testing round-trips).
