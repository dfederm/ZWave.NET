# Specification Conformance and Versioning

Load this reference before interpreting Z-Wave Application Specification requirements or versioned payloads.

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
