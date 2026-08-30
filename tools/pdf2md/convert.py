#!/usr/bin/env python3
"""Convert a Z-Wave specification PDF into progressive-disclosure Markdown.

The output is a folder tree of many small, topic-scoped ``.md`` files plus an
``index.md`` per directory, so an AI agent (or human) can navigate cheaply.

Pipeline (deterministic, two passes):
  1. Read the PDF outline (bookmarks) -> the section tree.
  2. Rebuild "visual lines" per page (spans sharing a y are merged, so a heading
     whose number and title are separate objects becomes one line).
  3. Bind each bookmark to a real heading line in the body (numbered headings by
     number-prefix, others by position). Bookmarks that point *into* a table are
     treated as in-table anchors, not headings.
  4. Detect tables (``find_tables``) and figures (embedded images) per page.
  5. Partition the section tree into files using a page/char size bound.
  6. Emit every file, resolving internal (GoTo) links to a concrete
     ``file.md#anchor`` and keeping external (URI) links as-is.

Run:  python convert.py INPUT.pdf -o OUTDIR -s SLUG
"""

import argparse
import hashlib
import posixpath
import re
import shutil
import sys
import unicodedata
from pathlib import Path

import fitz  # PyMuPDF


def pymupdf_version() -> str:
    v = getattr(fitz, "version", None)
    if isinstance(v, (list, tuple)) and v:
        return str(v[0])
    if isinstance(v, str) and v:
        return v
    m = re.search(r"PyMuPDF\s+([\d.]+)", fitz.__doc__ or "")
    return m.group(1) if m else "unknown"


# --------------------------------------------------------------------------- #
# Helpers
# --------------------------------------------------------------------------- #

def write_text(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)


def p2posix(p) -> str:
    return str(p).replace("\\", "/")


def rel_posix(file_path, base) -> str:
    return posixpath.normpath(posixpath.relpath(p2posix(file_path), p2posix(base)))


def escape_md_cell(cell) -> str:
    if cell is None:
        return ""
    s = str(cell).replace("\n", " ").replace("\r", " ")
    s = s.replace("|", "\\|")
    return re.sub(r"\s+", " ", s).strip()


def slugify(text: str) -> str:
    t = unicodedata.normalize("NFC", text.strip().lower())
    t = re.sub(r"[^\w\s-]", "", t, flags=re.UNICODE)
    t = re.sub(r"\s+", "-", t)
    t = re.sub(r"-+", "-", t).strip("-")
    return t or "section"


def numkey(number: str) -> str:
    if not number:
        return "00"
    return ".".join(f"{int(part):02d}" for part in number.split("."))


def number_prefix_match(text: str, number: str) -> bool:
    """True if text starts with `number` as a full dotted token (4.1 != 4.11)."""
    m = re.match(r"^" + re.escape(number) + r"(?!\d)", text)
    return bool(m)


_CTRL_RE = re.compile(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f-\x9f]")


def clean_title(text: str) -> str:
    """Normalize a TOC/section title so emitted headings, anchors and links stay
    clean. Some PDFs put a record-separator byte (\\x1e) where a hyphen belongs
    ('Z\\x1eWave' -> 'Z-Wave') and tab-separate the number from the name; both
    are collapsed here and any remaining C0/C1 control bytes are dropped."""
    t = text.replace("\x1e", "-")
    t = t.replace("\t", " ")
    t = _CTRL_RE.sub("", t)
    return re.sub(r"\s+", " ", t).strip()


FOOTER_RE = re.compile(
    r"(may only be copied|All Rights Reserved|Copyright|Z-Wave Alliance|"
    r"^\d{4}/\d{2}/\d{2}$|^\d{1,4}$)", re.IGNORECASE)

MIN_IMAGE_PT = 22  # figures smaller than this on any side are logos/bullets

# Running headers/footers (page numbers, vendor ads, copyright lines) live in a
# narrow band at the very bottom of the page. Real body text in these specs never
# comes closer than ~77pt to the bottom edge, so anything small sitting lower than
# this is chrome and is dropped.
BOTTOM_BAND_PT = 70


# --------------------------------------------------------------------------- #
# Data model
# --------------------------------------------------------------------------- #

class Section:
    __slots__ = (
        "level", "title", "number", "name", "page", "y",
        "parent", "children", "bound", "is_real",
        "stream_start", "stream_end", "page_span", "char_span",
        "is_directory", "dir", "file", "render_file", "anchor", "owns_file",
        "_real_children",
    )

    def __init__(self, level, title, number, name, page, y):
        self.level = level
        self.title = title
        self.number = number
        self.name = name
        self.page = page
        self.y = y
        self.parent = None
        self.children = []
        self.bound = None
        self.is_real = False
        self.stream_start = 0
        self.stream_end = 0
        self.page_span = 1
        self.char_span = 0
        self.is_directory = False
        self.dir = None
        self.file = None
        self.render_file = None
        self.anchor = ""
        self.owns_file = False
        self._real_children = []


class VisualLine:
    __slots__ = ("page", "y0", "y1", "x0", "x1", "text", "bold", "size",
                 "in_table", "spans", "section")

    def __init__(self, page, y0, y1, x0, x1, text, bold, size, in_table):
        self.page = page
        self.y0 = y0
        self.y1 = y1
        self.x0 = x0
        self.x1 = x1
        self.text = text
        self.bold = bold
        self.size = size
        self.in_table = in_table
        self.spans = []      # list of (text, linkref)
        self.section = None


# --------------------------------------------------------------------------- #
# Per-page extraction
# --------------------------------------------------------------------------- #

def extract_tables(page):
    """Return a list of (rect, markdown) tuples."""
    try:
        found = page.find_tables()
    except Exception:
        return []
    out = []
    for tab in found.tables:
        try:
            rows = tab.extract()
        except Exception:
            continue
        if not rows:
            continue
        md = []
        for i, row in enumerate(rows):
            md.append("| " + " | ".join(escape_md_cell(c) for c in row) + " |")
            if i == 0:
                md.append("| " + " | ".join("---" for _ in row) + " |")
        out.append((fitz.Rect(tab.bbox), "\n".join(md)))
    return out


def extract_images(doc, page, page_index, doc_root: Path, image_cache: dict):
    """Return a list of (rect, relpath). Extracts non-trivial images to assets/."""
    out = []
    try:
        infos = page.get_image_info(xrefs=True)
    except Exception:
        return out
    for info in infos:
        xref = info.get("xref")
        bbox = info.get("bbox")
        if not xref or not bbox:
            continue
        rect = fitz.Rect(bbox)
        if rect.width < MIN_IMAGE_PT or rect.height < MIN_IMAGE_PT:
            continue
        try:
            img = doc.extract_image(xref)
        except Exception:
            continue
        data = img.get("image") or b""
        if not data:
            continue
        h = hashlib.sha1(data).hexdigest()[:10]
        ext = img.get("ext") or "png"
        relpath = f"assets/img-{h}.{ext}"
        if relpath not in image_cache:
            dest = doc_root / relpath
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(data)
            image_cache[relpath] = True
        out.append((rect, relpath))
    return out


def build_visual_lines(doc, page_index, tables_rects, links):
    """Build merged visual lines for one page, attaching link refs per span."""
    page = doc[page_index]
    d = page.get_text("dict")

    groups = {}
    for block in d.get("blocks", []):
        for line in block.get("lines", []):
            for span in line.get("spans", []):
                txt = span.get("text", "")
                if txt is None or not txt.strip():
                    continue
                key = round(line["bbox"][1] / 2.0)
                g = groups.get(key)
                if g is None:
                    g = {"y0": line["bbox"][1], "y1": line["bbox"][3], "spans": []}
                    groups[key] = g
                g["y0"] = min(g["y0"], line["bbox"][1])
                g["y1"] = max(g["y1"], line["bbox"][3])
                g["spans"].append(span)

    def in_table(x, y):
        return any(r.contains(fitz.Point(x + 1, y + 1)) for r in tables_rects)

    vlines = []
    for g in sorted(groups.values(), key=lambda g: g["y0"]):
        g["spans"].sort(key=lambda s: (round(s["bbox"][1]), s["bbox"][0]))
        text = " ".join(s.get("text", "").strip() for s in g["spans"] if s.get("text", "").strip())
        text = re.sub(r"\s+", " ", text).strip()
        if not text:
            continue
        bold = any("Bold" in (s.get("font") or "") for s in g["spans"])
        size = max(s.get("size", 0) for s in g["spans"])
        x0 = min(s["bbox"][0] for s in g["spans"])
        x1 = max(s["bbox"][2] for s in g["spans"])
        vl = VisualLine(page_index, g["y0"], g["y1"], x0, x1, text, bold, size,
                        in_table(x0, g["y0"]))
        for s in g["spans"]:
            stxt = s.get("text", "").strip()
            if not stxt:
                continue
            linkref = None
            srect = fitz.Rect(s["bbox"])
            for l in links:
                f = l.get("from")
                if f is None:
                    continue
                if srect.intersects(f):
                    kind = l.get("kind")
                    if kind == 2:
                        linkref = ("uri", l.get("uri", ""))
                    elif kind == 1:
                        linkref = ("goto", l.get("page", 0), l.get("to").y)
                    break
            vl.spans.append((stxt, linkref))
        vlines.append(vl)
    vlines.sort(key=lambda v: (v.y0, v.x0))
    return vlines


# --------------------------------------------------------------------------- #
# Converter
# --------------------------------------------------------------------------- #

class Converter:
    def __init__(self, pdf_path, out_root, slug, max_pages, max_chars, do_images,
                 verbose):
        self.pdf_path = Path(pdf_path)
        self.doc_root = Path(out_root) / slug
        self.slug = slug
        self.max_pages = max_pages
        self.max_chars = max_chars
        self.do_images = do_images
        self.verbose = verbose
        self.doc = None
        self.sections = []
        self.real_sections = []
        self.stream = []
        self.page_lines = {}
        self.page_tables = {}
        self.page_images = {}
        self.image_cache = {}
        self.first_body_page = 0
        self.version = ""
        self.anchor_index = []
        self._current_file = None
        self._used_captions = set()

    def run(self):
        # regenerate from scratch (idempotent): clean before extracting assets
        if self.doc_root.exists():
            shutil.rmtree(self.doc_root)
        self.doc_root.mkdir(parents=True, exist_ok=True)
        self.doc = fitz.open(str(self.pdf_path))
        first = None
        for level, _t, pg in self.doc.get_toc():
            if level == 1:
                first = pg
                break
        self.first_body_page = (first - 1) if first else 0
        self.version = self._scrape_version()

        self._build_sections()
        self._build_pages()
        self._bind_headings()
        self._build_stream()
        self._compute_ranges()
        self._partition()
        self._assign_anchors()
        self._emit_all()
        stats = self._stats()
        self.doc.close()
        return stats

    def _scrape_version(self):
        try:
            txt = self.doc[0].get_text()
        except Exception:
            return ""
        m = re.search(r"[Vv]ersion[:\s]+([0-9][0-9A-Za-z.\-_x]*)", txt)
        return m.group(1).strip() if m else ""

    # ---- sections ----
    def _build_sections(self):
        stack = []
        for level, title, _pg, dest in self.doc.get_toc(simple=False):
            title = clean_title(title)
            m = re.match(r"^(\d+(?:\.\d+)*)\s+(.*\S)\s*$", title)
            if m:
                number, name = m.group(1), m.group(2).strip()
            else:
                number, name = None, title.strip()
            sec = Section(level, title.strip(), number, name, dest["page"], dest["to"].y)
            while stack and stack[-1].level >= level:
                stack.pop()
            if stack:
                stack[-1].children.append(sec)
                sec.parent = stack[-1]
            stack.append(sec)
            self.sections.append(sec)

    # ---- pages ----
    def _build_pages(self):
        for pi in range(self.first_body_page, self.doc.page_count):
            page = self.doc[pi]
            links = page.get_links()
            self.page_tables[pi] = extract_tables(page)
            trects = [r for r, _ in self.page_tables[pi]]
            self.page_images[pi] = (extract_images(self.doc, page, pi, self.doc_root,
                                                   self.image_cache)
                                    if self.do_images else [])
            self.page_lines[pi] = build_visual_lines(self.doc, pi, trects, links)

    # ---- bind headings ----
    def _bind_headings(self):
        for sec in self.sections:
            vlines = self.page_lines.get(sec.page, [])
            if not vlines:
                continue
            dy = sec.y
            boldc = [v for v in vlines if v.bold and 9.5 <= v.size <= 16 and not v.in_table]
            # Only *numbered* sections are real headings. Non-numbered bookmarks are
            # field/table-row anchors (they point into frame-format tables) and are
            # not rendered as headings.
            if not sec.number:
                continue
            bynum = [v for v in boldc if number_prefix_match(v.text, sec.number)]
            if not bynum:
                continue
            inband = [v for v in bynum if dy <= v.y0 <= dy + 18]
            pick = min(inband or bynum, key=lambda v: abs(v.y0 - (dy + 8)))
            sec.bound = pick
            pick.section = sec
            sec.is_real = True

    # ---- global stream ----
    def _build_stream(self):
        self.stream = []
        self._used_captions = set()
        for pi in range(self.first_body_page, self.doc.page_count):
            vlines = self.page_lines[pi]
            page_h = self.doc[pi].rect.height
            captions = [v for v in vlines if re.match(r"^(Figure|Table|Listing)\b",
                                                      v.text, re.I)]
            for rect, relpath in self.page_images.get(pi, []):
                alt = ""
                for c in captions:
                    if re.match(r"^Figure\b", c.text, re.I) and \
                            abs(c.y0 - (rect.y1 + 6)) < 40:
                        alt = c.text
                        self._used_captions.add(id(c))
                        break
                self.stream.append({"page": pi, "sorty": rect.y0, "kind": "image",
                                    "image": (relpath, alt or f"figure p{pi + 1}")})
            for rect, tmd in self.page_tables.get(pi, []):
                self.stream.append({"page": pi, "sorty": rect.y0, "kind": "table",
                                    "table_md": tmd})
            for v in vlines:
                if v.in_table or v.size < 8.5 or id(v) in self._used_captions:
                    continue
                if FOOTER_RE.search(v.text) and v.size < 9.5:
                    continue
                if v.size < 10.5 and v.y1 > page_h - BOTTOM_BAND_PT:
                    continue
                if v.section is not None and v.section.is_real:
                    self.stream.append({"page": pi, "sorty": v.y0, "kind": "heading",
                                        "section": v.section, "vl": v})
                else:
                    self.stream.append({"page": pi, "sorty": v.y0, "kind": "text", "vl": v})
        self.stream.sort(key=lambda it: (it["page"], it["sorty"], it["kind"] == "text"))

    # ---- ranges ----
    def _compute_ranges(self):
        self.real_sections = [s for s in self.sections if s.is_real]
        index_of = {}
        for i, it in enumerate(self.stream):
            if it["kind"] == "heading":
                index_of.setdefault((it["page"], round(it["sorty"], 1)), i)
        for sec in self.real_sections:
            sec.stream_start = index_of.get((sec.page, round(sec.bound.y0, 1)), 0)
        for i, sec in enumerate(self.real_sections):
            end = len(self.stream)
            for j in range(i + 1, len(self.real_sections)):
                if self.real_sections[j].level <= sec.level:
                    end = self.real_sections[j].stream_start
                    break
            sec.stream_end = end
            if sec.stream_start < sec.stream_end:
                first = self.stream[sec.stream_start]
                last = self.stream[sec.stream_end - 1]
                sec.page_span = last["page"] - first["page"] + 1
                sec.char_span = sum(self._item_len(it) for it in
                                    self.stream[sec.stream_start:sec.stream_end])

    @staticmethod
    def _item_len(it):
        if it["kind"] == "table":
            return len(it.get("table_md", ""))
        if it["kind"] == "image":
            return 0
        vl = it.get("vl")
        return len(vl.text) if vl else 0

    # ---- partition ----
    def _partition(self):
        for sec in self.real_sections:
            sec._real_children = []
        for sec in self.real_sections[1:]:
            par = None
            for cand in reversed(self.real_sections[:self.real_sections.index(sec)]):
                if cand.level < sec.level:
                    par = cand
                    break
            if par:
                par._real_children.append(sec)
        for ch in [s for s in self.real_sections if s.level == 1]:
            self._walk(ch, self.doc_root)

    def _walk(self, node, base_dir):
        has_children = len(node._real_children) > 0
        too_big = node.page_span > self.max_pages or node.char_span > self.max_chars
        node.is_directory = has_children and (too_big or node.level == 1)
        node.owns_file = True
        if node.is_directory:
            node.dir = Path(base_dir) / f"{numkey(node.number or '0')}-{slugify(node.name)}"
            node.file = node.dir / "index.md"
            node.render_file = node.file
            for ch in node._real_children:
                self._walk(ch, node.dir)
        else:
            node.file = Path(base_dir) / f"{numkey(node.number or '0')}-{slugify(node.name)}.md"
            node.render_file = node.file
            for ch in node._real_children:
                self._mark_headings(ch, node.file)

    def _mark_headings(self, node, file):
        node.render_file = file
        for ch in node._real_children:
            self._mark_headings(ch, file)

    # ---- anchors ----
    def _assign_anchors(self):
        file_headings = {}
        for sec in self.real_sections:
            file_headings.setdefault(sec.render_file, []).append(sec)
        for fpath, secs in file_headings.items():
            seen = {}
            for sec in secs:
                base = slugify(sec.title)
                n = seen.get(base, 0)
                seen[base] = n + 1
                sec.anchor = base if n == 0 else f"{base}-{n + 1}"

        for sec in self.real_sections:
            if sec.bound is None:
                continue
            self.anchor_index.append(
                (sec.page, sec.bound.y0, rel_posix(sec.render_file, self.doc_root), sec.anchor))
        self.anchor_index.sort(key=lambda t: (t[0], t[1]))

    def resolve_goto(self, page, y):
        best = None
        for (ap, ay, fp, anc) in self.anchor_index:
            if ap < page or (ap == page and ay <= y + 12):
                best = (fp, anc)
            else:
                break
        if best is None and self.anchor_index:
            best = (self.anchor_index[0][2], self.anchor_index[0][3])
        return best

    # ---- emit ----
    def _emit_all(self):
        for sec in self.real_sections:
            if not sec.owns_file:
                continue
            self._current_file = sec.file
            end = self._content_end(sec)
            items = self.stream[sec.stream_start:end]
            body = self._render_body(items, sec)
            header = self._provenance_header(sec)
            rel = rel_posix(sec.file, self.doc_root)
            write_text(self.doc_root / rel, header + body)

        self._write_contents()
        self._write_root_index()

    def _content_end(self, sec):
        if sec.is_directory and sec._real_children:
            return sec._real_children[0].stream_start
        return sec.stream_end

    def _provenance_header(self, sec):
        if sec.file.name == "index.md" and rel_posix(sec.file, self.doc_root) == "index.md":
            return ""
        last_page = self.stream[min(sec.stream_end, len(self.stream) - 1)]["page"]
        return (f"<!--\n  generated-by: tools/pdf2md/convert.py\n"
                f"  pymupdf: {pymupdf_version()}\n"
                f"  source: {self.pdf_path.name}\n"
                f"  section: \"{sec.title}\"\n"
                f"  pages: {sec.page + 1}-{last_page + 1}\n-->\n")

    def _append_block(self, path, block):
        text = path.read_text(encoding="utf-8") if path.exists() else ""
        write_text(path, text.rstrip("\n") + block)

    def _write_contents(self):
        for sec in self.real_sections:
            if not sec.is_directory or not sec._real_children:
                continue
            lines = ["", "", "## Contents", ""]
            base = sec.file.parent  # links are relative to this index.md's directory
            for ch in sec._real_children:
                link = rel_posix(ch.file, base)
                lines.append(f"- [{ch.title.strip()}]({link})")
            self._append_block(self.doc_root / rel_posix(sec.file, self.doc_root),
                               "\n".join(lines) + "\n")

    def _write_root_index(self):
        chapters = [s for s in self.real_sections if s.level == 1]
        lines = ["", "", "## Chapters", ""]
        for ch in chapters:
            lines.append(f"- [{ch.title.strip()}]({rel_posix(ch.file, self.doc_root)})")
        root_index = self.doc_root / "index.md"
        if root_index.exists():
            self._append_block(root_index, "\n".join(lines) + "\n")
        else:
            write_text(root_index, self._root_index_text() + "\n".join(lines) + "\n")

    def _root_index_text(self):
        prov = (f"<!--\n  generated-by: tools/pdf2md/convert.py\n"
                f"  pymupdf: {pymupdf_version()}\n  source: {self.pdf_path.name}\n-->\n\n")
        head = (f"# {self.pdf_path.stem}\n\n"
                f"*Source:* [{self.pdf_path.name}](../sources/{self.pdf_path.name})\n")
        if self.version:
            head += f"*Version:* {self.version}\n"
        head += (f"*Pages:* {self.doc.page_count}\n"
                 f"*Generated by:* `tools/pdf2md/convert.py` (PyMuPDF {pymupdf_version()})\n")
        return prov + head

    # ---- body rendering ----
    def _render_body(self, items, root_sec):
        out = []
        buf = []
        root_level = root_sec.level

        def flush():
            if buf:
                out.append(self._render_paragraph(buf))
                buf.clear()

        prev = None
        for it in items:
            kind = it["kind"]
            if kind == "heading":
                flush()
                sec = it["section"]
                lvl = max(1, min(6, 1 + (sec.level - root_level)))
                out.append("#" * lvl + " " + sec.title.strip())
                prev = it
            elif kind == "table":
                flush()
                out.append(it["table_md"])
                prev = it
            elif kind == "image":
                flush()
                rel, alt = it["image"]
                out.append(f"![{alt}]({rel})")
                prev = it
            else:
                vl = it["vl"]
                if prev is not None and prev["kind"] == "text" and prev["page"] == vl.page:
                    if it["sorty"] - prev["sorty"] > vl.size * 1.7:
                        flush()
                buf.append(vl)
                prev = it
        flush()
        text = "\n\n".join(x for x in out if x)
        return re.sub(r"\n{3,}", "\n\n", text).rstrip() + "\n"

    def _render_paragraph(self, vlines):
        tokens = []
        for vl in vlines:
            for txt, ref in vl.spans:
                if not txt:
                    continue
                target = None
                if ref and ref[0] == "uri":
                    target = ref[1]
                elif ref and ref[0] == "goto":
                    r = self.resolve_goto(ref[1], ref[2])
                    if r:
                        cur_dir = posixpath.dirname(rel_posix(self._current_file, self.doc_root))
                        target = posixpath.relpath(r[0], cur_dir or ".") + "#" + r[1]
                tokens.append((txt, target))
        merged = []
        for txt, tgt in tokens:
            if merged and merged[-1][1] is not None and merged[-1][1] == tgt:
                merged[-1] = (merged[-1][0] + " " + txt, tgt)
            else:
                merged.append((txt, tgt))
        parts = []
        for txt, tgt in merged:
            parts.append(f"[{txt}]({tgt})" if tgt else txt)
        result = ""
        for part in parts:
            if not result:
                result = part
            elif part[0] in ".,;:)?!>%\"'" or result[-1] in "([":
                result += part
            else:
                result += " " + part
        return re.sub(r"\s+", " ", result).strip()

    def _stats(self):
        n_files = sum(1 for _ in self.doc_root.rglob("*.md"))
        n_img = sum(1 for _ in self.doc_root.rglob("assets/*"))
        return {"doc": self.slug, "pages": self.doc.page_count,
                "sections": len(self.sections), "real_headings": len(self.real_sections),
                "md_files": n_files, "images": n_img}


def main(argv=None):
    ap = argparse.ArgumentParser(description="Convert a Z-Wave spec PDF to Markdown.")
    ap.add_argument("pdf", help="Input PDF path")
    ap.add_argument("-o", "--out", default="docs/specs")
    ap.add_argument("-s", "--slug", default=None)
    ap.add_argument("--max-pages", type=int, default=15)
    ap.add_argument("--max-chars", type=int, default=60000)
    ap.add_argument("--no-images", action="store_true")
    args = ap.parse_args(argv)

    pdf = Path(args.pdf)
    if not pdf.exists():
        sys.exit(f"error: input not found: {pdf}")
    slug = args.slug or slugify(pdf.stem)
    conv = Converter(pdf, Path(args.out), slug, args.max_pages, args.max_chars,
                     not args.no_images, verbose=False)
    stats = conv.run()
    print(f"[{stats['doc']}] pages={stats['pages']} sections={stats['sections']} "
          f"headings={stats['real_headings']} md_files={stats['md_files']} "
          f"images={stats['images']} -> {conv.doc_root}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
