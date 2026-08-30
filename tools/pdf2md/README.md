# pdf2md — Z-Wave spec PDF → Markdown

Converts a Z-Wave Alliance specification PDF into a tree of small, topic-scoped
Markdown files with per-directory `index.md` pages, designed for progressive
disclosure (easy navigation for humans and AI agents).

## Requirements

- Python 3.10+
- PyMuPDF — `pip install -r requirements.txt`
- Git LFS only matters for *storing* the source PDFs, not for running this tool.

## Usage

Convert a single PDF:

```powershell
python convert.py <input.pdf> -o docs/specs -s <output-folder>
```

Convert all three Z-Wave specs (the default workflow):

```powershell
pwsh convert.ps1
```

Options:

| Flag | Default | Meaning |
| --- | --- | --- |
| `-o / --out` | `docs/specs` | Output root directory |
| `-s / --slug` | derived from filename | Output folder name |
| `--max-pages` | `15` | Split a section into its own files when it spans more pages than this |
| `--max-chars` | `60000` | Split a section when its text exceeds this many characters |
| `--no-images` | off | Skip extracting figures |

## How it works

1. Reads the PDF **outline** (bookmarks) as the section tree.
2. Rebuilds **visual lines** per page (spans sharing a y are merged, so a heading
   whose number and title are separate objects becomes one line).
3. Binds each *numbered* bookmark to its real heading line. Non-numbered bookmarks
   (field / table-row anchors in the frame-format tables) are not headings.
4. Detects **tables** (`find_tables`) and **figures** (embedded images, deduped).
5. Splits the section tree into size-bounded files.
6. Emits each file, resolving **internal links** to a concrete `file.md#anchor`
   and keeping **external links** as-is.

## Output conventions

- A section becomes a **directory + `index.md`** when it has subsections and
  exceeds the size bound; otherwise it is a single `.md` file.
- Heading depth maps to `#`..`####`.
- Every generated file has a leading HTML comment with provenance:
  `generated-by`, `pymupdf` version, `source`, `section`, and `pages`.
- The top-level `index.md` links back to the source PDF in `../sources/`.
- Filenames use zero-padded section numbers (`04.01-...`) so they sort in order.
