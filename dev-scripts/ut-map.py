"""Universal Tracker's map tab (Next 79): plain diagrams drawn by code from the apworld's own room data, never the
game's art. Writes apworld/bug_fables/ut_map/: maps.json, locations.json, layout.json, follow.json and images/*.png.

    python dev-scripts/ut-map.py --archipelago <your Archipelago checkout> [--check]

A tab per room (its areas as boxes, a pin per spot, a diamond per door on its box's edge, the ways between areas
and the room's other ways out), one per game area (its rooms) and the World (the areas). --check changes nothing and
fails when a file differs from what this script draws, a PNG by its pixels and palette (zlib's bytes may differ
between machines). Standard library only: PNGs written with zlib, text from the dot-matrix glyphs below.
"""
from __future__ import annotations

import argparse
import json
import math
import struct
import sys
import unicodedata
import zlib
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
OUT = REPO / 'apworld' / 'bug_fables' / 'ut_map'

# The diagrams' colours (at most 16, the preflight's "images" rule): background, box, box edge, text, line, title.
PALETTE = [(28, 30, 38), (50, 54, 68), (125, 133, 156), (232, 234, 242), (88, 94, 114), (247, 205, 112)]
BACK, BOX, EDGE, TEXT, LINE, TITLE = range(6)
# The marker: see-through, white, and the titles' colour.
MARKER_PALETTE = [(0, 0, 0), (255, 255, 255), (247, 205, 112)]

# Each glyph 5 dots wide and 7 high, drawn twice their size; anything else is drawn as '?'.
GLYPHS = {
    'A': '.###. #...# #...# ##### #...# #...# #...#', 'B': '####. #...# #...# ####. #...# #...# ####.',
    'C': '.###. #...# #.... #.... #.... #...# .###.', 'D': '####. #...# #...# #...# #...# #...# ####.',
    'E': '##### #.... #.... ####. #.... #.... #####', 'F': '##### #.... #.... ####. #.... #.... #....',
    'G': '.###. #...# #.... #.### #...# #...# .####', 'H': '#...# #...# #...# ##### #...# #...# #...#',
    'I': '.###. ..#.. ..#.. ..#.. ..#.. ..#.. .###.', 'J': '..### ...#. ...#. ...#. ...#. #..#. .##..',
    'K': '#...# #..#. #.#.. ##... #.#.. #..#. #...#', 'L': '#.... #.... #.... #.... #.... #.... #####',
    'M': '#...# ##.## #.#.# #.#.# #...# #...# #...#', 'N': '#...# #...# ##..# #.#.# #..## #...# #...#',
    'O': '.###. #...# #...# #...# #...# #...# .###.', 'P': '####. #...# #...# ####. #.... #.... #....',
    'Q': '.###. #...# #...# #...# #.#.# #..#. .##.#', 'R': '####. #...# #...# ####. #.#.. #..#. #...#',
    'S': '.#### #.... #.... .###. ....# ....# ####.', 'T': '##### ..#.. ..#.. ..#.. ..#.. ..#.. ..#..',
    'U': '#...# #...# #...# #...# #...# #...# .###.', 'V': '#...# #...# #...# #...# #...# .#.#. ..#..',
    'W': '#...# #...# #...# #.#.# #.#.# #.#.# .#.#.', 'X': '#...# #...# .#.#. ..#.. .#.#. #...# #...#',
    'Y': '#...# #...# .#.#. ..#.. ..#.. ..#.. ..#..', 'Z': '##### ....# ...#. ..#.. .#... #.... #####',
    '0': '.###. #...# #..## #.#.# ##..# #...# .###.', '1': '..#.. .##.. ..#.. ..#.. ..#.. ..#.. .###.',
    '2': '.###. #...# ....# ...#. ..#.. .#... #####', '3': '####. ....# ....# .###. ....# ....# ####.',
    '4': '...#. ..##. .#.#. #..#. ##### ...#. ...#.', '5': '##### #.... ####. ....# ....# #...# .###.',
    '6': '..##. .#... #.... ####. #...# #...# .###.', '7': '##### ....# ...#. ..#.. .#... .#... .#...',
    '8': '.###. #...# #...# .###. #...# #...# .###.', '9': '.###. #...# #...# .#### ....# ...#. .##..',
    ' ': '..... ..... ..... ..... ..... ..... .....', ':': '..... ..#.. ..#.. ..... ..#.. ..#.. .....',
    ',': '..... ..... ..... ..... .##.. ..#.. .#...', '.': '..... ..... ..... ..... ..... .##.. .##..',
    '-': '..... ..... ..... ##### ..... ..... .....', "'": '..#.. ..#.. .#... ..... ..... ..... .....',
    '(': '...#. ..#.. .#... .#... .#... ..#.. ...#.', ')': '.#... ..#.. ...#. ...#. ...#. ..#.. .#...',
    '!': '..#.. ..#.. ..#.. ..#.. ..#.. ..... ..#..', '?': '.###. #...# ....# ...#. ..#.. ..... ..#..',
    '&': '.##.. #..#. #.#.. .#... #.#.# #..#. .##.#', '/': '....# ...#. ...#. ..#.. .#... .#... #....',
    '#': '.#.#. .#.#. ##### .#.#. ##### .#.#. .#.#.',
}
SCALE = 2
ADVANCE = 6 * SCALE  # a glyph and one dot of space
TEXT_HEIGHT = 7 * SCALE

PIN = 32  # between pins; Universal Tracker draws each one 24 px wide (location_size)
MARGIN, GAP, TOP = 32, 64, 72
MAP_SETTINGS = {'location_size': 24, 'location_icon_size': 40, 'location_border_thickness': 3}


def plain(text: str) -> str:
    """The text as the glyphs can draw it: accents dropped, upper case."""
    return unicodedata.normalize('NFKD', text).encode('ascii', 'ignore').decode().upper()


def text_width(text: str) -> int:
    return len(plain(text)) * ADVANCE


class Canvas:
    def __init__(self, width: int, height: int, fill: int = BACK):
        if not (0 < width <= 2048 and 0 < height <= 2048):
            raise ValueError(f'a diagram of {width}x{height}: over 2048 px a side')
        self.width, self.height = width, height
        self.pixels = bytearray([fill]) * (width * height)

    def fill(self, x: int, y: int, w: int, h: int, color: int) -> None:
        x0, x1 = max(x, 0), min(x + w, self.width)
        for row in range(max(y, 0), min(y + h, self.height)):
            start = row * self.width
            self.pixels[start + x0:start + x1] = bytes([color]) * (x1 - x0)

    def box(self, x: int, y: int, w: int, h: int) -> None:
        self.fill(x, y, w, h, EDGE)
        self.fill(x + 2, y + 2, w - 4, h - 4, BOX)

    def line(self, x0: int, y0: int, x1: int, y1: int, color: int, width: int = 3) -> None:
        steps = max(abs(x1 - x0), abs(y1 - y0), 1)
        for i in range(steps + 1):
            x = x0 + (x1 - x0) * i // steps
            y = y0 + (y1 - y0) * i // steps
            self.fill(x - width // 2, y - width // 2, width, width, color)

    def text(self, x: int, y: int, text: str, color: int) -> None:
        for n, ch in enumerate(plain(text)):
            rows = GLYPHS.get(ch, GLYPHS['?']).split()
            for r, bits in enumerate(rows):
                for c, bit in enumerate(bits):
                    if bit == '#':
                        self.fill(x + n * ADVANCE + c * SCALE, y + r * SCALE, SCALE, SCALE, color)

    def png(self, palette: list[tuple[int, int, int]], see_through: int | None = None) -> bytes:
        """A 4-bit palette PNG, every row unfiltered: the only format the preflight lets in."""
        row_bytes = (self.width + 1) // 2
        rows = bytearray()
        for y in range(self.height):
            line = self.pixels[y * self.width:(y + 1) * self.width] + b'\0'
            rows.append(0)
            rows += bytes((line[i] << 4) | line[i + 1] for i in range(0, row_bytes * 2, 2))
        return png_bytes(self.width, self.height, palette, bytes(rows), see_through)


def chunk(kind: bytes, body: bytes) -> bytes:
    return struct.pack('>I', len(body)) + kind + body + struct.pack('>I', zlib.crc32(kind + body))


def png_bytes(width: int, height: int, palette: list[tuple[int, int, int]], rows: bytes,
              see_through: int | None) -> bytes:
    header = struct.pack('>IIBBBBB', width, height, 4, 3, 0, 0, 0)
    data = (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', header) + chunk(b'PLTE', bytes(v for rgb in palette for v in rgb))
            + (chunk(b'tRNS', bytes([255] * see_through + [0])) if see_through is not None else b'')
            + chunk(b'IDAT', zlib.compress(rows, 9)) + chunk(b'IEND', b''))
    if len(data) > 65536:
        raise ValueError(f'a diagram of {len(data)} bytes: over the preflight\'s 64 KB')
    return data


def pixels_of(data: bytes) -> tuple[bytes, ...]:
    """What a PNG shows: its header, palette, transparency and inflated rows, whatever zlib made of them."""
    pos, parts = 8, {}
    while pos < len(data):
        length, kind = struct.unpack_from('>I4s', data, pos)
        parts[kind] = parts.get(kind, b'') + data[pos + 8:pos + 8 + length]
        pos += 12 + length
    return parts.get(b'IHDR', b''), parts.get(b'PLTE', b''), parts.get(b'tRNS', b''), zlib.decompress(
        parts.get(b'IDAT', b''))


def load(archipelago: Path):
    """The apworld's tables, imported through the checkout it's linked into."""
    sys.path.insert(0, str(archipelago))
    import worlds  # noqa: F401  (Archipelago's loader, which finds the linked world)
    from worlds.bug_fables import data_tables, enemysanity, regions
    return data_tables, enemysanity, regions


def map_of(region: str) -> str:
    return region.split(' (', 1)[0]


def grid(sizes: list[tuple[int, int]], columns: int) -> tuple[list[tuple[int, int]], int, int]:
    """Boxes in reading order, `columns` to a row: each one's top left, and the width and height they take."""
    rows = [sizes[i:i + columns] for i in range(0, len(sizes), columns)]
    widths = [max((row[c][0] for row in rows if c < len(row)), default=0) for c in range(columns)]
    heights = [max(h for _, h in row) for row in rows]
    spots = []
    for r, row in enumerate(rows):
        for c in range(len(row)):
            spots.append((sum(widths[:c]) + GAP * c, sum(heights[:r]) + GAP * r))
    return spots, sum(widths) + GAP * (len(widths) - 1), sum(heights) + GAP * (len(heights) - 1)


def edge_point(box: tuple[int, int, int, int], cx: float, cy: float, dx: float, dy: float) -> float:
    """How far along (dx, dy) from inside a box the ray leaves it."""
    x, y, w, h = box
    ts = []
    for d, low, high, c in ((dx, x, x + w, cx), (dy, y, y + h, cy)):
        if d > 0:
            ts.append((high - c) / d)
        elif d < 0:
            ts.append((low - c) / d)
    return min(ts) if ts else 0.0


class Pins:
    """Pin groups for one tab, never two of a kind at one point (Universal Tracker keys them by their place)."""

    def __init__(self, tab: str):
        self.tab, self.groups, self.taken = tab, [], set()

    def add(self, name: str, sections: list[str], x: int, y: int, kind: str) -> tuple[int, int]:
        while (kind, x, y) in self.taken:
            x += 12
        self.taken.add((kind, x, y))
        self.groups.append({'name': name, 'sections': [{'name': s} for s in sections],
                            'map_locations': [{'map': self.tab, 'x': x, 'y': y}]})
        return x, y


def room_tab(room: str, title: str, boxes: list[str], label: dict[str, str], spots: dict[str, list[str]],
             doors: dict[str, list[str]], others: dict[str, list[str]], edges: list[tuple[str, str, list[str]]]):
    """One room: its areas as boxes, its spots, doors and ways out as pins. The picture, the pins and the marker."""
    sizes = []
    for region in boxes:
        cols = min(6, len(spots[region])) or 1
        rows = math.ceil(len(spots[region]) / 6)
        inner = max(text_width(label[region]), cols * PIN, (len(doors[region]) + 1) * 34,
                    (len(others[region]) + 1) * 34, 160)
        sizes.append((inner + 32, 48 + rows * PIN + 16))
    columns = 1 if len(boxes) == 1 else 2 if len(boxes) <= 4 else 3
    places, width, height = grid(sizes, columns)
    canvas = Canvas(max(MARGIN * 2 + width, MARGIN * 2 + text_width(title) + 48), TOP + height + MARGIN + 16)
    rect = {region: (MARGIN + px, TOP + py, w, h) for region, (px, py), (w, h) in zip(boxes, places, sizes,
                                                                                       strict=True)}
    pins = Pins(room)
    middles = []
    for a, b, names in edges:
        (ax, ay, aw, ah), (bx, by, bw, bh) = rect[a], rect[b]
        cx, cy, ex, ey = ax + aw / 2, ay + ah / 2, bx + bw / 2, by + bh / 2
        canvas.line(int(cx), int(cy), int(ex), int(ey), LINE)
        t_out = edge_point(rect[a], cx, cy, ex - cx, ey - cy)
        t_in = 1 - edge_point(rect[b], ex, ey, cx - ex, cy - ey)
        t = (t_out + t_in) / 2 if t_in > t_out else 0.5
        middles.append((names, int(cx + (ex - cx) * t), int(cy + (ey - cy) * t), f'{label[a]} - {label[b]}'))
    for region in boxes:
        x, y, w, h = rect[region]
        canvas.box(x, y, w, h)
        canvas.text(x + 12, y + 12, label[region], TEXT)
        cols = min(6, len(spots[region])) or 1
        for i, name in enumerate(spots[region]):
            pins.add(name, [name], x + 16 + PIN // 2 + (i % cols) * PIN, y + 48 + PIN // 2 + (i // cols) * PIN,
                     'location')
        for j, name in enumerate(doors[region]):
            pins.add(name, [name], x + 24 + j * 34, y, 'entrance')
        for j, name in enumerate(others[region]):
            pins.add(name, [name], x + 24 + j * 34, y + h, 'entrance')
    for names, x, y, group in middles:
        pins.add(group, names, x, y, 'entrance')
    canvas.text(MARGIN, 24, title, TITLE)
    marker = (MARGIN + text_width(title) + 28, 24 + TEXT_HEIGHT // 2)
    return canvas, pins, marker


def group_tab(tab: str, title: str, members: list[tuple[str, str, list[str]]], columns: int):
    """Boxes for an area's rooms (or the world's areas), each with one pin holding all its spots."""
    sizes = [(max(text_width(name) + 64, 200), 64) for _, name, _ in members]
    places, width, height = grid(sizes, columns)
    canvas = Canvas(max(MARGIN * 2 + width, MARGIN * 2 + text_width(title)), TOP + height + MARGIN)
    canvas.text(MARGIN, 24, title, TITLE)
    pins, markers = Pins(tab), {}
    for (key, name, spots), (px, py), (w, h) in zip(members, places, sizes, strict=True):
        x, y = MARGIN + px, TOP + py
        canvas.box(x, y, w, h)
        canvas.text(x + 12, y + (h - TEXT_HEIGHT) // 2, name, TEXT)
        if spots:
            pins.add(name, spots, x + w - 20, y + h // 2, 'location')
        markers[key] = (x + w - 4, y + 4)
    return canvas, pins, markers


def build(data_tables, enemysanity, regions):
    """Every file's contents: {path under ut_map: bytes}."""
    areas_of = data_tables.MAP_AREA_IDS
    area_names = enemysanity.AREA_NAMES
    spots_all = [loc for loc in data_tables.LOCATIONS if not loc.pending]
    titles = enemysanity.room_names(data_tables.LOCATIONS)
    entrances = [e for e in regions.logic_entrances() if e.from_map != regions.MENU]
    edge_names = {}
    for area in data_tables.MAP_AREAS:
        joined = area.to or area.map
        edge_names[f'{joined} to {area.region}'] = (joined, area.region)
        edge_names[f'{area.region} to {joined}'] = (joined, area.region)
    files, maps, nodes, follow = {}, [], [], {}
    layout_areas = {a: [] for a in area_names}
    order = sorted(data_tables.MAPS, key=lambda m: (areas_of[m], titles.get(m, m)))
    room_pins = {}
    for room in order:
        boxes = [room, *(a.region for a in data_tables.MAP_AREAS if a.map == room)]
        label = {room: 'Main', **{a.region: a.name for a in data_tables.MAP_AREAS if a.map == room}}
        spots = {r: [] for r in boxes}
        for loc in spots_all:
            if loc.region == room:
                spots[f'{room} ({loc.area})' if loc.area else room].append(loc.name)
        doors = {r: [] for r in boxes}
        others = {r: [] for r in boxes}
        pairs = {}
        for e in entrances:
            if map_of(e.from_map) != room or e.from_map not in doors:
                continue
            if e.name in edge_names:
                pairs.setdefault(edge_names[e.name], []).append(e.name)
            elif e.door:
                doors[e.from_map].append(e.name)
            else:
                others[e.from_map].append(e.name)
        edges = [(a, b, sorted(names)) for (a, b), names in pairs.items() if a in doors and b in doors]
        title = titles.get(room, f'{area_names[areas_of[room]]}: {room}')
        canvas, pins, marker = room_tab(room, title, boxes, label, spots, doors, others, edges)
        files[f'images/room_{room}.png'] = canvas.png(PALETTE)
        maps.append({'name': room, 'img': f'images/room_{room}.png', **MAP_SETTINGS})
        nodes.append({'name': title, 'children': pins.groups})
        follow[room] = {room: list(marker)}
        room_pins[room] = [loc for r in boxes for loc in spots[r]]
        layout_areas[areas_of[room]].append(
            {'title': title.split(': ', 1)[-1], 'content': {'type': 'map', 'maps': [room]}})
    area_tabs = []
    for area, name in area_names.items():
        rooms = [m for m in order if areas_of[m] == area]
        if not rooms:
            continue
        tab = f'Area: {name}'
        members = [(m, titles.get(m, f'{name}: {m}').split(': ', 1)[-1], room_pins[m]) for m in rooms]
        canvas, pins, markers = group_tab(tab, name, members, 4)
        files[f'images/area_{area:02}.png'] = canvas.png(PALETTE)
        maps.append({'name': tab, 'img': f'images/area_{area:02}.png', **MAP_SETTINGS})
        nodes.append({'name': tab, 'children': pins.groups})
        follow[tab] = {m: list(xy) for m, xy in markers.items()}
        area_tabs.append({'title': name, 'content': {'type': 'tabbed', 'tabs': [
            {'title': 'All rooms', 'content': {'type': 'map', 'maps': [tab]}}, *layout_areas[area]]}})
    members = [(area, name, [loc for m in order if areas_of[m] == area for loc in room_pins[m]])
               for area, name in area_names.items() if any(areas_of[m] == area for m in order)]
    canvas, pins, markers = group_tab('World', 'Bug Fables', members, 5)
    files['images/world.png'] = canvas.png(PALETTE)
    maps.insert(0, {'name': 'World', 'img': 'images/world.png', **MAP_SETTINGS})
    nodes.insert(0, {'name': 'World', 'children': pins.groups})
    follow['World'] = {m: list(markers[areas_of[m]]) for m in order}
    you = Canvas(32, 32, 0)
    you.fill(4, 4, 24, 24, 1)
    you.fill(8, 8, 16, 16, 2)
    files['images/you.png'] = you.png(MARKER_PALETTE, see_through=0)
    layout = {'tracker_map': {'type': 'tabbed', 'tabs': [
        {'title': 'World', 'content': {'type': 'map', 'maps': ['World']}}, *area_tabs]}}
    for name, value in (('maps.json', maps), ('locations.json', nodes), ('layout.json', layout),
                        ('follow.json', follow)):
        files[name] = (json.dumps(value, indent=1) + '\n').encode('utf-8')  # non-ASCII escaped, as the data files
    return files


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    ap.add_argument('--archipelago', required=True, type=Path, help='your Archipelago checkout, the world linked in')
    ap.add_argument('--check', action='store_true', help='change nothing; fail if a file differs from the drawing')
    args = ap.parse_args()
    files = build(*load(args.archipelago.resolve()))
    on_disk = {p.relative_to(OUT).as_posix() for p in OUT.rglob('*') if p.is_file()} if OUT.exists() else set()
    differ, stale = [], sorted(on_disk - set(files))
    for name, data in sorted(files.items()):
        path = OUT / name
        old = path.read_bytes() if path.exists() else None
        same = old == data or (old is not None and name.endswith('.png') and pixels_of(old) == pixels_of(data))
        if same:
            continue
        differ.append(name)
        if not args.check:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)
    if not args.check:
        for name in stale:
            (OUT / name).unlink()
    pngs = [n for n in files if n.endswith('.png')]
    print(f'ut-map: {len(files)} files ({len(pngs)} diagrams, {sum(len(files[n]) for n in pngs) // 1024} KB); '
          f'{len(differ)} {"differ" if args.check else "written"}, {len(stale)} stale'
          f'{"" if args.check else " removed"}')
    for name in [*differ, *stale] if args.check else []:
        print(f'  differs: {name}' if name in differ else f'  stale: {name}')
    return 1 if args.check and (differ or stale) else 0


if __name__ == '__main__':
    sys.exit(main())
