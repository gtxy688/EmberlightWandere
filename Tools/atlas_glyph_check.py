#!/usr/bin/env python3
"""Report which characters appear in Assembly-CSharp.dll string literals but are absent
from the pre-baked TMP atlas.

Why this exists
---------------
The atlas only contains what EmberFonts.AllGlyphs asks for, and AllGlyphs is assembled from
specific arrays and named constants. Any player-facing string that is still an inline literal
inside a method body is invisible to both reflection and array-walking -- which is how
休闲适合轻松构筑 shipped as hollow boxes.

Rather than keeping a hand-written list of "sources to remember", this reads the compiled
assembly's #US (user strings) metadata heap, so every string literal in the game is covered,
wherever it lives in the source.

Usage:  python Tools/atlas_glyph_check.py [dll] [sdf-asset]
Exit code is 1 when characters are missing, so it can gate a bake.
"""

import re
import struct
import sys
from pathlib import Path

# The console on this machine defaults to GBK, which cannot represent the glyphs being
# reported -- printing them would raise instead of showing the one thing this tool exists for.
try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

ROOT = Path(__file__).resolve().parent.parent
DLL = ROOT / "Library" / "ScriptAssemblies" / "Assembly-CSharp.dll"
ATLAS = ROOT / "Assets" / "Resources" / "Fonts" / "NotoSansCJKsc-Regular SDF.asset"


def user_strings(path):
    """Every literal in the #US heap of a .NET assembly.

    The heap starts with a 0 byte, then a sequence of compressed-length-prefixed UTF-16LE
    strings. The high bit of the final byte marks "has special characters", which is only a
    flag -- the text itself is always plain UTF-16LE.
    """
    data = path.read_bytes()
    if data[:2] != b"MZ":
        raise ValueError(f"{path} is not a PE file")
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise ValueError(f"{path} has no PE signature")

    coff = pe + 4
    sections, opt_size = struct.unpack_from("<H", data, coff + 2)[0], struct.unpack_from("<H", data, coff + 16)[0]
    opt = coff + 20
    magic = struct.unpack_from("<H", data, opt)[0]
    dd = opt + (96 if magic == 0x10B else 112)  # PE32 vs PE32+
    cli_rva = struct.unpack_from("<I", data, dd + 14 * 8)[0]

    # Map the CLI header RVA to a file offset via the section table.
    sec = opt + opt_size
    def rva_to_offset(rva):
        for i in range(sections):
            base = sec + i * 40
            va, vsize = struct.unpack_from("<II", data, base + 12)
            raw_size, raw = struct.unpack_from("<II", data, base + 16)
            if va <= rva < va + max(vsize, raw_size):
                return raw + (rva - va)
        raise ValueError("CLI header RVA is not inside any section")

    cli = rva_to_offset(cli_rva)
    md_rva, md_size = struct.unpack_from("<II", data, cli + 8)
    md = rva_to_offset(md_rva)
    if data[md:md + 4] != b"BSJB":
        raise ValueError("metadata root signature missing")

    version_len = struct.unpack_from("<I", data, md + 12)[0]
    p = md + 16 + version_len + 4  # version string, then flags + stream count
    streams = struct.unpack_from("<H", data, p)[0]
    p += 2
    us_off = us_size = None
    for _ in range(streams):
        off, size = struct.unpack_from("<II", data, p)
        p += 8
        end = data.index(b"\0", p)
        name = data[p:end].decode("ascii", "replace")
        p = end + 1
        p += (-p) % 4  # streams are 4-byte aligned
        if name == "#US":
            us_off, us_size = md + off, size
    if us_off is None:
        raise ValueError("#US heap not found")

    heap = data[us_off:us_off + us_size]
    out, i = [], 1  # byte 0 of the heap is a single null
    while i < len(heap):
        b0 = heap[i]
        if b0 == 0:
            i += 1
            continue
        if b0 & 0x80 == 0:
            length, i = b0, i + 1
        elif b0 & 0xC0 == 0x80:
            length, i = ((b0 & 0x3F) << 8) | heap[i + 1], i + 2
        else:
            length, i = ((b0 & 0x1F) << 24) | (heap[i + 1] << 16) | (heap[i + 2] << 8) | heap[i + 3], i + 4
        if length == 0:
            continue
        raw, i = heap[i:i + length], i + length
        text = raw[:-1] if raw[-1] & 0x80 else raw  # strip the flag byte
        try:
            out.append(text.decode("utf-16-le", "replace"))
        except Exception:
            pass
    return out


def atlas_characters(path):
    """Characters the baked atlas holds.

    The .asset is Unity YAML; each TMP character entry carries both the codepoint and the
    glyph it resolves to. Reading the text avoids loading 8 MB of binary texture data.
    """
    text = path.read_text(encoding="utf-8", errors="replace")
    m = re.search(r"m_Unicode: (\d+)\n\s+m_GlyphIndex:", text)
    found = set()
    for hit in re.finditer(r"m_Unicode: (\d+)", text):
        code = int(hit.group(1))
        if code > 0:
            found.add(chr(code))
    if not found:
        raise ValueError(f"no m_Unicode entries in {path}; is it the right asset?")
    return found


def main():
    dll = Path(sys.argv[1]) if len(sys.argv) > 1 else DLL
    atlas = Path(sys.argv[2]) if len(sys.argv) > 2 else ATLAS

    literals = user_strings(dll)
    have = atlas_characters(atlas)

    wanted = set()
    for s in literals:
        for c in s:
            # ASCII is covered by the font's own Latin set; controls never get glyphs.
            if ord(c) < 127 or c < " " or c.isspace():
                continue
            # U+FFFD is what the UTF-16 decode produces for a malformed or truncated heap
            # entry -- a decoding artefact here, not a character the game can draw.
            if c == "\ufffd":
                continue
            # Private-use and unassigned planes are never real copy either.
            if 0xE000 <= ord(c) <= 0xF8FF:
                continue
            wanted.add(c)

    missing = sorted(wanted - have)

    # TMP resolves two "special characters" against the font asset on every font assignment and
    # warns per label when either is absent: U+005F for underline and U+2026 for ellipsis
    # (TMP_Text.GetSpecialCharacters). Neither is guaranteed to appear in any string literal --
    # U+005F in particular does not -- so they are checked explicitly rather than being left to
    # the literal scan. Adding them to a glyph set is the fix, not disabling the warning.
    tmp_special = {0x5F: "underline", 0x2026: "ellipsis"}
    special_missing = [cp for cp in tmp_special if chr(cp) not in have]

    print(f"assembly literals : {len(literals)}")
    print(f"distinct visible  : {len(wanted)}")
    print(f"atlas holds       : {len(have)}")
    print(f"MISSING ({len(missing)}): {''.join(missing)}")
    print("TMP special chars : " + (
        "all present" if not special_missing else
        ", ".join(f"U+{cp:04X} ({tmp_special[cp]}) ABSENT" for cp in special_missing)))

    if missing or special_missing:
        # A ready-to-paste C# literal, so the fix is a copy rather than a transcription.
        # `missing` holds characters, `special_missing` holds codepoints -- normalise before union.
        payload = sorted(set(missing) | {chr(cp) for cp in special_missing})
        escaped = "".join("\\u%04x" % ord(c) for c in payload)
        print(f"\nC# literal for EmberFonts.ExtraVisibleGlyphs:\n\"{escaped}\"")
        return 1
    print("\ncoverage complete")
    return 0


if __name__ == "__main__":
    sys.exit(main())
