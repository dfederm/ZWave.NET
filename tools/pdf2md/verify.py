#!/usr/bin/env python3
"""Verify internal Markdown links across a docs/specs tree.

Checks that every relative ``.md`` link points to a file that exists and, when an
anchor is present, that the anchor matches a heading in the target file (anchors
are recomputed with the same slugify + de-dup rules used by convert.py).

Run:  python verify.py [docs-root]     (default: docs/specs)
"""

import posixpath
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from convert import slugify  # noqa: E402

HEADING_RE = re.compile(r"^(#{1,6})\s+(.*\S)\s*$")
LINK_RE = re.compile(r"\]\(([^)\s]+\.md)(?:#([^)\s]+))?\)")


def heading_anchors(path: Path):
    anchors = {}
    seen = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        m = HEADING_RE.match(line)
        if not m:
            continue
        base = slugify(m.group(2))
        n = seen.get(base, 0)
        seen[base] = n + 1
        anchors[base if n == 0 else f"{base}-{n + 1}"] = True
    return anchors


def main(root="docs/specs"):
    root = Path(root)
    if not root.exists():
        sys.exit(f"error: no such directory: {root}")
    md_files = list(root.rglob("*.md"))
    anchor_cache = {}

    def anchors_for(p: Path):
        if p not in anchor_cache:
            anchor_cache[p] = heading_anchors(p)
        return anchor_cache[p]

    checked = 0
    missing_file = []
    missing_anchor = []
    for f in md_files:
        text = f.read_text(encoding="utf-8")
        for m in LINK_RE.finditer(text):
            target, anchor = m.group(1), m.group(2)
            # only relative links (ignore http/https and absolute)
            if re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", target):
                continue
            tp = (f.parent / target).resolve()
            checked += 1
            if not tp.exists():
                missing_file.append((rel(f, root), target))
                continue
            if anchor and anchor not in anchors_for(tp):
                missing_anchor.append((rel(f, root), target, anchor))

    print(f"docs-root: {root}")
    print(f"md files: {len(md_files)}   internal links checked: {checked}")
    print(f"missing target files: {len(missing_file)}")
    for src, tgt in missing_file[:30]:
        print(f"    [file] {src} -> {tgt}")
    print(f"missing anchors: {len(missing_anchor)}")
    for src, tgt, anc in missing_anchor[:30]:
        print(f"    [anchor] {src} -> {tgt}#{anc}")
    if missing_file or missing_anchor:
        sys.exit(1)
    print("OK: all internal links resolve.")


def rel(p: Path, root: Path) -> str:
    return posixpath.relpath(str(p), str(root)).replace("\\", "/")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "docs/specs")
