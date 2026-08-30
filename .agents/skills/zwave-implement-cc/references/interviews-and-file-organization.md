# Interviews and File Organization

Load this reference when choosing file layout, command support rules, interview behavior, or partial-class boundaries.

## File Structure

For simple CCs with few commands, a single file is sufficient: `src/ZWave.CommandClasses/{Name}CommandClass.cs`

For larger CCs with multiple command groups, use the **partial class pattern** described below.

The file uses the `ZWave.CommandClasses` namespace (file-scoped) and must include `using Microsoft.Extensions.Logging;`. The contents are ordered as follows:

1. Domain enums and report record structs (public) — types representing the CC's values
2. A **command enum** (`byte`-backed) listing every command in the CC
3. The **CC class** itself — `sealed`, inherits `CommandClass<TCommand>`
4. **Internal inner command structs** — one per command (Get, Set, Report, etc.), nested inside the CC class

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
