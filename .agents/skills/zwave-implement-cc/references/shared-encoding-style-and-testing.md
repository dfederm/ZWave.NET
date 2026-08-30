# Shared Encoding, Style, Testing, and Validation

Load this reference while encoding payloads, applying repository style, organizing tests, or validating an implementation.

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

## Build Validation

After implementing, build from the repo root:

```shell
dotnet build --configuration Release
```

The source generator will automatically pick up the `[CommandClass]` attribute and register the new CC in the factory. No manual registration is needed.

Do **not** run `dotnet format` — it cannot run source generators and will produce false errors.
