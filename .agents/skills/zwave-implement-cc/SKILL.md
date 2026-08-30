---
name: zwave-implement-cc
description: Implement or extend Z-Wave Command Classes in ZWave.NET when translating Application Specification commands into APIs, parsers, state, interviews, and tests.
---

# Implementing a Z-Wave Command Class

Use this skill for new or expanded Command Class (CC) work in `src/ZWave.CommandClasses/`. Read only the linked reference needed for the current decision.

## Required Inputs

Before implementation, obtain:

1. The Command Class name and its `CommandClassId` value.
2. The applicable Z-Wave Application Specification text, including command IDs, payload formats, versions, and normative behavior.

If `src/ZWave.Protocol/CommandClassId.cs` has no entry, add the correct byte value and XML documentation.

## Non-Negotiable Rules

- Conform to the specification exactly. Do not guess when behavior, encoding, validation, optional fields, reserved values, caching, or edge cases are ambiguous; stop and ask the user.
- Interpret normative keywords according to RFC 2119.
- Implement receivers for the highest CC version: do not mask reserved bits, and determine optional received fields from payload length rather than negotiated version.
- Use `Version` for support uncertainty and `EffectiveVersion` when constructing version-dependent outbound payloads.
- Parse each report exactly once. Solicited reports are parsed by the awaiting API; only unmatched unsolicited reports reach `ProcessUnsolicitedCommand`.
- Report parsers validate in safe stages, log a warning, then throw `ZWaveErrorCode.InvalidPayload`.
- Awaiter predicates inspect bounded raw bytes and MUST NOT call `Parse`.
- Reports that can arrive unsolicited expose events raised exactly once for both solicited and unsolicited paths.

Read [Specification Conformance and Versioning](references/specification-conformance.md) before interpreting specification or version rules. Read [Reports, State, and Validation](references/reports-state-and-validation.md) before deciding report flow, caching, events, validation, or aggregation.

## Procedure

1. **Inspect the repository context.** Read root `AGENTS.md`, the specification section, and analogous CC source and test files.
2. **Plan the public model.** Identify domain enums, `readonly record struct` report types, cached state versus direct-return APIs, events, capability properties, and per-key readings.
3. **Plan support and interview behavior.** Classify the CC category, map commands by version, and interview only values that should become cached state. Use [Interviews and File Organization](references/interviews-and-file-organization.md).
4. **Choose the file layout.** Start with `{Name}CommandClass.cs`; split related command groups into partial files only when the implementation becomes difficult to navigate.
5. **Implement the command surface.** Add the byte-backed command enum, `[CommandClass]` class, internal constructor, `IsCommandSupported`, `InterviewAsync`, APIs, unsolicited handling, and nested `ICommand` structs. Follow [Command Implementation Patterns](references/command-implementation-patterns.md).
6. **Implement wire formats.** Build outbound frames, parse inbound frames, preserve forward-compatible bits and fields, and aggregate all `Reports to Follow` frames before returning.
7. **Add or update tests.** Mirror the source/partial-file structure. Test command creation, parsing, validation failures, version-dependent payloads, state/event behavior, predicates, and multi-frame aggregation as applicable.
8. **Validate.** Follow [Shared Encoding, Style, Testing, and Validation](references/shared-encoding-style-and-testing.md), then run the smallest targeted tests and a Release build.

## Completion Checklist

- `CommandClassId` is present and correct.
- Public types use repository naming, nullability, and XML documentation conventions.
- `[CommandClass(CommandClassId.{Name})]` is present; no manual factory registration was added.
- `IsCommandSupported` returns `null` when version-gated support is unknown.
- Interview behavior queries cached state, not every on-demand capability.
- Solicited and unsolicited reports parse once, update the intended state, and raise intended events once.
- Parsers validate before indexing or slicing, log, and throw invalid-payload errors.
- Optional received fields use payload length; outbound version differences use `EffectiveVersion`.
- Report commands include a static `Create` method for bidirectional support and round-trip testing.
- Multi-frame reports are fully aggregated.
- Tests cover the changed wire and API behavior and follow any partial-class split.
- `dotnet build --configuration Release` succeeds. Do not run `dotnet format`.

## References

- [Specification Conformance and Versioning](references/specification-conformance.md)
- [Reports, State, and Validation](references/reports-state-and-validation.md)
- [Command Implementation Patterns](references/command-implementation-patterns.md)
- [Interviews and File Organization](references/interviews-and-file-organization.md)
- [Shared Encoding, Style, Testing, and Validation](references/shared-encoding-style-and-testing.md)
