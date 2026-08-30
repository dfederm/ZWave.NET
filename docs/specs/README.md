# Z-Wave Specification (Markdown)

Markdown converted from the Z-Wave Alliance specification PDFs, structured for
progressive disclosure so that both humans and AI agents can navigate it
efficiently.

Each specification is a folder of many small, topic-scoped `.md` files plus an
`index.md` per directory. Every generated file carries provenance (source PDF,
section, page range, generator, PyMuPDF version) in a leading HTML comment.

The source PDFs are stored via [Git LFS](https://git-lfs.com) under `sources/`.

## Layout

```
docs/specs/
  sources/                          # source PDFs (Git LFS)
  zwave-500-series-programmers-guide/
    index.md                        # provenance + chapter list
    assets/                         # extracted figures
    01-abbreviations.md
    03-zwave-software-architecture/ # a chapter = a directory
      index.md
      03-1-zwave-system-startup-code.md
      ...
  command-class-specification/      # one file per command class / major section
  zwave-host-api-specification/
```

- **Heading depth** in a file maps to `#`..`####`. Deeper sections that fit are
  rendered as headings inside their parent file; larger sections are split into
  their own files (and sub-`index.md`).
- **Internal links** point to a specific `file.md#anchor`. **External links**
  keep their original URL.
- **Figures** are extracted to `assets/` and embedded in place.

## Regenerating

The generator lives in [`tools/pdf2md/`](../../tools/pdf2md/).

1. Make sure Python 3 and PyMuPDF are installed (`pip install -r tools/pdf2md/requirements.txt`).
2. Make sure Git LFS is set up (`git lfs install`).
3. Run:

   ```powershell
   pwsh tools/pdf2md/convert.ps1
   ```

   This converts every PDF in `docs/specs/sources/` into the matching folder
   under `docs/specs/`. Re-running is idempotent: each output folder is fully
   regenerated. To convert a single file, or to change a folder name, see
   `tools/pdf2md/convert.ps1` (the `source → folder` map).
