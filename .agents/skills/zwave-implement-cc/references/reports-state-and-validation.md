# Reports, State, and Validation

Load this reference when designing report flow, events, caching, payload validation, or multi-frame aggregation.

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
