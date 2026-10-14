"""
Generates every image used by the MuseAI website.

All artwork on the site is original, procedurally generated media created for
this project (no stock photos, logos or third-party images are used), which
keeps the site within the assessment rule that media must be copyright-free
or created by the author.

Usage (from the repository root):

    python3 tools/generate_art.py

Requirements: Python 3.10+, numpy, Pillow.
Fonts are loaded from the self-hosted copies in wwwroot/lib/fonts (SIL OFL).
Every generator uses a fixed random seed, so the output is reproducible.
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parent.parent
WWWROOT = ROOT / "AICreativeArts" / "wwwroot"
IMG = WWWROOT / "img"
FONTS = WWWROOT / "lib" / "fonts"


# --------------------------------------------------------------------------
# Helpers
# --------------------------------------------------------------------------

def hex_rgb(value: str) -> np.ndarray:
    value = value.lstrip("#")
    return np.array([int(value[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.float32) / 255.0


def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONTS / name), size)


def to_image(arr: np.ndarray) -> Image.Image:
    return Image.fromarray((np.clip(arr, 0, 1) * 255).astype(np.uint8), "RGB")


def to_array(img: Image.Image) -> np.ndarray:
    return np.asarray(img.convert("RGB"), dtype=np.float32) / 255.0


def value_noise(h: int, w: int, cells: int, rng: np.random.Generator) -> np.ndarray:
    """Smooth value noise in the range 0..1."""
    cells_y = max(1, round(cells * h / w))
    grid = rng.random((cells_y + 2, cells + 2)).astype(np.float32)
    ys = np.linspace(0, cells_y, h, endpoint=False, dtype=np.float32)
    xs = np.linspace(0, cells, w, endpoint=False, dtype=np.float32)
    y0, x0 = ys.astype(int), xs.astype(int)
    ty, tx = ys - y0, xs - x0
    ty = ty * ty * (3 - 2 * ty)
    tx = tx * tx * (3 - 2 * tx)
    g00 = grid[np.ix_(y0, x0)]
    g01 = grid[np.ix_(y0, x0 + 1)]
    g10 = grid[np.ix_(y0 + 1, x0)]
    g11 = grid[np.ix_(y0 + 1, x0 + 1)]
    top = g00 + (g01 - g00) * tx[None, :]
    bottom = g10 + (g11 - g10) * tx[None, :]
    return top + (bottom - top) * ty[:, None]


def fbm(h: int, w: int, cells: int, octaves: int, rng: np.random.Generator) -> np.ndarray:
    """Fractal (multi-octave) noise normalised to 0..1."""
    total = np.zeros((h, w), dtype=np.float32)
    amplitude, norm = 1.0, 0.0
    for octave in range(octaves):
        total += value_noise(h, w, cells * 2 ** octave, rng) * amplitude
        norm += amplitude
        amplitude *= 0.5
    total /= norm
    return (total - total.min()) / (total.max() - total.min() + 1e-6)


def vertical_gradient(h: int, w: int, top: str, bottom: str) -> np.ndarray:
    t = np.linspace(0, 1, h, dtype=np.float32)[:, None, None]
    return hex_rgb(top) * (1 - t) + hex_rgb(bottom) * t * np.ones((h, w, 3), dtype=np.float32)


def vignette(h: int, w: int, strength: float) -> np.ndarray:
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.sqrt(((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2) / math.sqrt(2)
    return (1 - strength * d ** 1.8)[:, :, None]


def flow_field(w: int, h: int, particles: int, steps: int, palette: list[str],
               rng: np.random.Generator, cells: int = 3, turns: float = 2.0,
               step: float = 1.0) -> np.ndarray:
    """Particles traced through a noise vector field; returns light density."""
    angle = fbm(h, w, cells, 3, rng) * math.tau * turns
    colours = np.stack([hex_rgb(c) for c in palette])[rng.integers(len(palette), size=particles)]
    px = rng.random(particles, dtype=np.float32) * w
    py = rng.random(particles, dtype=np.float32) * h
    canvas = np.zeros((3, h * w), dtype=np.float32)
    for _ in range(steps):
        xi = px.astype(int)
        yi = py.astype(int)
        idx = yi * w + xi
        for c in range(3):
            canvas[c] += np.bincount(idx, weights=colours[:, c], minlength=h * w)
        a = angle[yi, xi]
        px += np.cos(a) * step
        py += np.sin(a) * step
        lost = (px < 0) | (px >= w) | (py < 0) | (py >= h)
        px[lost] = rng.random(lost.sum(), dtype=np.float32) * w
        py[lost] = rng.random(lost.sum(), dtype=np.float32) * h
    return canvas.reshape(3, h, w).transpose(1, 2, 0)


def tone_map(density: np.ndarray, exposure: float) -> np.ndarray:
    return 1 - np.exp(-density * exposure)


def glow(img: Image.Image, radius: float, amount: float) -> Image.Image:
    base = to_array(img)
    blurred = to_array(img.filter(ImageFilter.GaussianBlur(radius)))
    return to_image(1 - (1 - base) * (1 - blurred * amount))


def draw_centered_text(draw: ImageDraw.ImageDraw, box_w: int, y: int, text: str,
                       fnt: ImageFont.FreeTypeFont, fill, shadow=None) -> None:
    left, top, right, bottom = draw.textbbox((0, 0), text, font=fnt)
    x = (box_w - (right - left)) / 2 - left
    if shadow:
        draw.text((x + 3, y + 3), text, font=fnt, fill=shadow)
    draw.text((x, y), text, font=fnt, fill=fill)


def save_jpg(img: Image.Image, path: Path, quality: int = 84) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.convert("RGB").save(path, "JPEG", quality=quality, optimize=True, progressive=True)
    print(f"  {path.relative_to(ROOT)}  ({path.stat().st_size // 1024} KB)")


# --------------------------------------------------------------------------
# Parallax backgrounds
# --------------------------------------------------------------------------

def hero_background() -> Image.Image:
    """Golden and cyan 'neural brush strokes' drifting across a midnight sky."""
    w, h = 1920, 1080
    rng = np.random.default_rng(7)
    density = flow_field(w, h, 9000, 260, ["#f8bd08", "#ff9933", "#3fd0ff", "#ffe6a3", "#c27bff"],
                         rng, cells=3, turns=1.6, step=1.1)
    strands = tone_map(density, 0.18)
    base = vertical_gradient(h, w, "#040917", "#0b1f3f")
    img = base * (1 - strands) + strands * 0.92
    img *= vignette(h, w, 0.55)
    return glow(to_image(img), 14, 0.55)


def section_background() -> Image.Image:
    """A dark, out-of-focus artist's studio: paint blooms and bokeh light."""
    w, h = 1920, 1280
    rng = np.random.default_rng(21)
    canvas = Image.fromarray((vertical_gradient(h, w, "#07101f", "#120a24") * 255).astype(np.uint8))

    blooms = Image.new("RGB", (w, h), (0, 0, 0))
    d = ImageDraw.Draw(blooms)
    palette = ["#1d4f91", "#7a2d8f", "#f8bd08", "#ff7a1a", "#0f8f8f", "#3333ff"]
    for _ in range(46):
        cx, cy = rng.random() * w, rng.random() * h
        rx, ry = 120 + rng.random() * 360, 80 + rng.random() * 240
        colour = tuple(int(v * 255 * (0.25 + rng.random() * 0.35)) for v in hex_rgb(palette[rng.integers(6)]))
        d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=colour)
    blooms = blooms.filter(ImageFilter.GaussianBlur(110))

    strokes = Image.new("RGB", (w, h), (0, 0, 0))
    d = ImageDraw.Draw(strokes)
    for _ in range(26):
        x0, y0 = rng.random() * w, rng.random() * h
        pts = []
        heading = rng.random() * math.tau
        for i in range(40):
            heading += (rng.random() - 0.5) * 0.35
            pts.append((x0 + math.cos(heading) * i * 22, y0 + math.sin(heading) * i * 22))
        colour = tuple(int(v * 255 * 0.55) for v in hex_rgb(palette[rng.integers(6)]))
        d.line(pts, fill=colour, width=int(10 + rng.random() * 30), joint="curve")
    strokes = strokes.filter(ImageFilter.GaussianBlur(26))

    bokeh = Image.new("RGB", (w, h), (0, 0, 0))
    d = ImageDraw.Draw(bokeh)
    for _ in range(170):
        cx, cy, r = rng.random() * w, rng.random() * h, 6 + rng.random() ** 2 * 70
        tint = hex_rgb(["#ffe6a3", "#f8bd08", "#9fd8ff", "#ffb380", "#d7b8ff"][rng.integers(5)])
        level = 0.12 + rng.random() * 0.3
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=tuple(int(v * 255 * level) for v in tint))
    bokeh = bokeh.filter(ImageFilter.GaussianBlur(3))

    arr = to_array(canvas)
    for layer in (blooms, strokes, bokeh):
        arr = 1 - (1 - arr) * (1 - to_array(layer))
    arr *= vignette(h, w, 0.5) * 0.92
    return to_image(arr)


# --------------------------------------------------------------------------
# Square tool tiles (original illustrations - not product logos)
# --------------------------------------------------------------------------

TILE = 600


def tile_wordmark(img: Image.Image, name: str, tagline: str, colour="#ffffff",
                  tag_colour="#f8bd08", y: int = 420) -> Image.Image:
    d = ImageDraw.Draw(img)
    draw_centered_text(d, TILE, y, name, font("montserrat-latin-700-normal.woff2", 62), colour,
                       shadow=(0, 0, 0))
    draw_centered_text(d, TILE, y + 84, tagline.upper(), font("montserrat-latin-500-normal.woff2", 22),
                       tag_colour)
    return img


def tile_midjourney() -> Image.Image:
    rng = np.random.default_rng(101)
    density = flow_field(TILE, TILE, 2600, 180, ["#cfe3ff", "#6fa8ff", "#ffffff", "#f8bd08"],
                         rng, cells=2, turns=1.2)
    strands = tone_map(density, 0.25)
    base = vertical_gradient(TILE, TILE, "#0a1030", "#1b1450")
    img = to_image((base * (1 - strands) + strands) * vignette(TILE, TILE, 0.6))
    return tile_wordmark(glow(img, 8, 0.5), "Midjourney", "text-to-image art")


def tile_firefly() -> Image.Image:
    rng = np.random.default_rng(102)
    y, x = np.mgrid[0:TILE, 0:TILE].astype(np.float32)
    r = np.sqrt((x - 300) ** 2 + (y - 230) ** 2) / 420
    arr = hex_rgb("#5a0d14") * (1 - r[:, :, None]).clip(0, 1) + hex_rgb("#120306") * r[:, :, None].clip(0, 1)
    lights = Image.new("RGB", (TILE, TILE))
    d = ImageDraw.Draw(lights)
    for _ in range(140):
        cx, cy, rad = rng.random() * TILE, rng.random() * 400, 2 + rng.random() ** 3 * 16
        level = 0.5 + rng.random() * 0.5
        colour = tuple(int(v * 255 * level) for v in hex_rgb(["#ffd84d", "#ff9933", "#fff1b8"][rng.integers(3)]))
        d.ellipse([cx - rad, cy - rad, cx + rad, cy + rad], fill=colour)
    lights = glow(lights, 10, 1.0)
    arr = 1 - (1 - arr) * (1 - to_array(lights))
    return tile_wordmark(to_image(arr), "Adobe Firefly", "generative image fill")


def tile_suno() -> Image.Image:
    rng = np.random.default_rng(103)
    img = to_image(vertical_gradient(TILE, TILE, "#14051f", "#2a0a3a"))
    d = ImageDraw.Draw(img)
    bars, gap = 38, 4
    bar_w = (TILE - 60 - gap * (bars - 1)) / bars
    for i in range(bars):
        t = i / (bars - 1)
        envelope = math.sin(math.pi * t) ** 0.8
        height = 30 + envelope * (120 + rng.random() * 110)
        colour = tuple(int(v * 255) for v in hex_rgb("#ff3d8b") * (1 - t) + hex_rgb("#ffb347") * t)
        x0 = 30 + i * (bar_w + gap)
        d.rounded_rectangle([x0, 230 - height / 2, x0 + bar_w, 230 + height / 2], radius=bar_w / 2, fill=colour)
    img = glow(img, 12, 0.6)
    return tile_wordmark(img, "Suno", "songs from a prompt", tag_colour="#ffb347")


def tile_aiva() -> Image.Image:
    rng = np.random.default_rng(104)
    img = to_image(vertical_gradient(TILE, TILE, "#06162b", "#0d2f4f"))
    d = ImageDraw.Draw(img)
    top, spacing = 120, 28
    for i in range(5):
        pts = [(x, top + i * spacing + math.sin(x / 70 + i * 0.4) * 14) for x in range(20, TILE - 19, 6)]
        d.line(pts, fill=(170, 205, 235), width=3)
    for k in range(9):
        x = 70 + k * 58
        line = rng.integers(0, 9)
        yc = top + line * spacing / 2 + math.sin(x / 70) * 14
        d.ellipse([x - 15, yc - 11, x + 15, yc + 11], fill=(248, 189, 8))
        d.line([(x + 13, yc), (x + 13, yc - 70)], fill=(248, 189, 8), width=4)
    img = glow(img, 6, 0.5)
    return tile_wordmark(img, "AIVA", "ai soundtrack composer")


def tile_sudowrite() -> Image.Image:
    rng = np.random.default_rng(105)
    img = to_image(vertical_gradient(TILE, TILE, "#2b1055", "#13072a"))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([110, 50, 490, 390], radius=18, fill=(250, 244, 230))
    y = 90
    for line in range(8):
        x = 145
        while True:
            word = 20 + rng.random() * 60
            if x + word > 455:
                break
            d.rounded_rectangle([x, y, x + word, y + 12], radius=6, fill=(120, 96, 160))
            x += word + 12
        if line == 7:
            d.rectangle([x, y - 6, x + 5, y + 18], fill=(248, 189, 8))
        y += 36
    return tile_wordmark(img, "Sudowrite", "fiction writing partner", tag_colour="#ffd36b")


def tile_novelai() -> Image.Image:
    rng = np.random.default_rng(106)
    arr = vertical_gradient(TILE, TILE, "#031a1f", "#0a3340")
    stars = Image.new("RGB", (TILE, TILE))
    d = ImageDraw.Draw(stars)
    for _ in range(220):
        cx, cy, rad = rng.random() * TILE, rng.random() * TILE, rng.random() ** 4 * 3 + 0.6
        level = 0.4 + rng.random() * 0.6
        d.ellipse([cx - rad, cy - rad, cx + rad, cy + rad], fill=tuple(int(255 * level) for _ in range(3)))
    # An open book drawn as a constellation.
    book = [(130, 330), (300, 360), (470, 330), (470, 130), (300, 160), (130, 130), (130, 330)]
    d.line(book, fill=(110, 210, 220), width=3)
    d.line([(300, 160), (300, 360)], fill=(110, 210, 220), width=3)
    for i, (sx, sy) in enumerate([(165, 175), (205, 205), (250, 185), (350, 190), (400, 220), (440, 180)]):
        d.line([(sx, sy), (sx + 25, sy + 40)], fill=(70, 150, 170), width=2)
    for px, py in book[:-1] + [(300, 160), (300, 360)]:
        d.ellipse([px - 7, py - 7, px + 7, py + 7], fill=(248, 189, 8))
    stars = glow(stars, 5, 0.8)
    arr = 1 - (1 - arr) * (1 - to_array(stars))
    return tile_wordmark(to_image(arr), "NovelAI", "ai storytelling", tag_colour="#7fe3ee")


# --------------------------------------------------------------------------
# Sample gallery artworks (seed data for the AI Images list)
# --------------------------------------------------------------------------

ART = 800


def art_golden_currents() -> Image.Image:
    rng = np.random.default_rng(201)
    density = flow_field(ART, ART, 5200, 240, ["#f8bd08", "#ffcf4d", "#ff9933", "#fff3c4"], rng,
                         cells=2, turns=1.0, step=1.0)
    strands = tone_map(density, 0.22)
    base = vertical_gradient(ART, ART, "#050c22", "#102a4c")
    return glow(to_image(base * (1 - strands) + strands), 10, 0.55)


def art_stained_glass() -> Image.Image:
    rng = np.random.default_rng(202)
    points = rng.random((70, 2)).astype(np.float32) * ART
    palette = [hex_rgb(c) for c in ["#1d4f91", "#7a2d8f", "#f8bd08", "#d1495b", "#0f8f8f", "#3333ff", "#ff9933", "#2e8b57"]]
    colours = np.stack([palette[i] for i in rng.integers(len(palette), size=len(points))])
    y, x = np.mgrid[0:ART, 0:ART].astype(np.float32)
    best = np.full((ART, ART), np.inf, dtype=np.float32)
    second = np.full((ART, ART), np.inf, dtype=np.float32)
    owner = np.zeros((ART, ART), dtype=np.int32)
    for i, (px, py) in enumerate(points):
        dist = np.sqrt((x - px) ** 2 + (y - py) ** 2)
        closer = dist < best
        second = np.where(closer, best, np.minimum(second, dist))
        owner = np.where(closer, i, owner)
        best = np.where(closer, dist, best)
    shade = (1 - (best / 140).clip(0, 0.55))[:, :, None]
    arr = colours[owner] * shade + 0.08
    lead = (second - best) < 5
    arr[lead] = hex_rgb("#141414")
    light = np.exp(-(((x - 260) ** 2 + (y - 220) ** 2) / (2 * 320 ** 2)))[:, :, None]
    return to_image(arr * (0.65 + 0.55 * light))


def art_harmonograph() -> Image.Image:
    t = np.linspace(0, 260, 260000, dtype=np.float64)
    x = np.sin(2.01 * t + 0.0) * np.exp(-0.004 * t) + np.sin(3.0 * t + 1.5) * np.exp(-0.0065 * t)
    y = np.sin(3.02 * t + 0.7) * np.exp(-0.005 * t) + np.sin(2.0 * t + 0.0) * np.exp(-0.0045 * t)
    px = ((x / 2.1) * 0.46 + 0.5) * ART
    py = ((y / 2.1) * 0.46 + 0.5) * ART
    hue = (t / t.max())[:, None]
    colours = hex_rgb("#3fd0ff") * (1 - hue) + hex_rgb("#ff3d8b") * hue
    idx = py.astype(int).clip(0, ART - 1) * ART + px.astype(int).clip(0, ART - 1)
    density = np.stack([np.bincount(idx, weights=colours[:, c], minlength=ART * ART) for c in range(3)], axis=1)
    img = tone_map(density.reshape(ART, ART, 3), 0.9)
    return glow(to_image(img + hex_rgb("#05030d")), 6, 0.8)


def art_interference() -> Image.Image:
    y, x = np.mgrid[0:ART, 0:ART].astype(np.float32)
    field = np.zeros((ART, ART), dtype=np.float32)
    for sx, sy, k in [(180, 220, 0.085), (620, 300, 0.07), (400, 690, 0.078)]:
        field += np.sin(np.sqrt((x - sx) ** 2 + (y - sy) ** 2) * k)
    t = ((field / 3) + 1) / 2
    stops = [hex_rgb(c) for c in ["#0b0b3b", "#3333ff", "#c2185b", "#ff9933", "#f8bd08", "#fff4cf"]]
    pos = t * (len(stops) - 1)
    i = pos.astype(int).clip(0, len(stops) - 2)
    f = (pos - i)[:, :, None]
    stops_arr = np.stack(stops)
    return to_image(stops_arr[i] * (1 - f) + stops_arr[i + 1] * f)


def art_circle_garden() -> Image.Image:
    rng = np.random.default_rng(205)
    scale = 2
    size = ART * scale
    img = Image.new("RGB", (size, size), (247, 239, 222))
    d = ImageDraw.Draw(img)
    palette = ["#ff9933", "#ffcc80", "#3333ff", "#ccccff", "#d1495b", "#0f8f8f", "#f8bd08", "#7a2d8f"]
    circles: list[tuple[float, float, float]] = []
    for _ in range(9000):
        cx, cy = rng.random() * size, rng.random() * size
        max_r = min(cx, cy, size - cx, size - cy, 190.0)
        for ox, oy, orad in circles:
            max_r = min(max_r, math.hypot(cx - ox, cy - oy) - orad - 6)
            if max_r < 8:
                break
        if max_r >= 8:
            r = max_r * (0.7 + rng.random() * 0.3)
            circles.append((cx, cy, r))
            colour = palette[rng.integers(len(palette))]
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=colour)
            if r > 40:
                inner = r * 0.55
                d.ellipse([cx - inner, cy - inner, cx + inner, cy + inner], fill=(247, 239, 222))
                core = r * 0.3
                d.ellipse([cx - core, cy - core, cx + core, cy + core], fill=palette[rng.integers(len(palette))])
    return img.resize((ART, ART), Image.LANCZOS)


def art_dusk_ridges() -> Image.Image:
    rng = np.random.default_rng(206)
    scale = 2
    size = ART * scale
    sky = vertical_gradient(size, size, "#1b1150", "#ff9966")
    img = to_image(sky)
    d = ImageDraw.Draw(img)
    d.ellipse([size * 0.62, size * 0.1, size * 0.76, size * 0.24], fill=(255, 228, 160))
    img = glow(img, 30, 0.6)
    d = ImageDraw.Draw(img)
    layers = 7
    for layer in range(layers):
        t = layer / (layers - 1)
        base = size * (0.5 + t * 0.42)
        ridge = fbm(1, size, 3 + layer, 4, rng)[0]
        heights = base - ridge * size * (0.2 - t * 0.1)
        colour = hex_rgb("#7a5aa6") * (1 - t) + hex_rgb("#120a24") * t
        pts = [(0, size)] + [(xi, float(heights[xi])) for xi in range(0, size, 4)] + [(size, size)]
        d.polygon(pts, fill=tuple(int(v * 255) for v in colour))
    return img.resize((ART, ART), Image.LANCZOS)


# --------------------------------------------------------------------------
# UI images
# --------------------------------------------------------------------------

def upload_placeholder(title: str, subtitle: str) -> Image.Image:
    img = to_image(vertical_gradient(TILE, TILE, "#0b1f3f", "#040917"))
    d = ImageDraw.Draw(img)
    for i in range(0, 4 * 520, 26):
        # Dashed golden frame.
        side, offset = divmod(i, 520)
        a, b = offset + 40, min(offset + 54, 560)
        segment = [[(a, 40), (b, 40)], [(560, a), (560, b)], [(a, 560), (b, 560)], [(40, a), (40, b)]][side]
        d.line(segment, fill=(248, 189, 8), width=4)
    d.rounded_rectangle([220, 150, 380, 270], radius=14, outline=(248, 189, 8), width=6)
    d.polygon([(236, 256), (285, 196), (318, 236), (338, 214), (366, 256)], fill=(248, 189, 8))
    d.ellipse([336, 168, 358, 190], fill=(255, 255, 255))
    draw_centered_text(d, TILE, 320, title, font("anton-latin-400-normal.woff2", 54), (248, 189, 8),
                       shadow=(0, 0, 60))
    draw_centered_text(d, TILE, 405, subtitle, font("montserrat-latin-500-normal.woff2", 24), (255, 255, 255))
    return img


def favicon() -> Image.Image:
    size = 256
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, size - 1, size - 1], radius=56, fill=(8, 20, 48, 255))
    d.arc([28, 28, 228, 228], start=200, end=340, fill=(248, 189, 8, 255), width=18)
    draw_centered_text(d, size, 58, "M", font("anton-latin-400-normal.woff2", 150), (248, 189, 8, 255))
    return img


def main() -> None:
    print("Backgrounds")
    save_jpg(hero_background(), IMG / "backgrounds" / "hero-neural-brush.jpg", 82)
    save_jpg(section_background(), IMG / "backgrounds" / "studio-bokeh.jpg", 80)

    print("Tool tiles")
    for name, fn in [("midjourney", tile_midjourney), ("adobe-firefly", tile_firefly),
                     ("suno", tile_suno), ("aiva", tile_aiva),
                     ("sudowrite", tile_sudowrite), ("novelai", tile_novelai)]:
        save_jpg(fn(), IMG / "tools" / f"{name}.jpg", 86)

    print("Sample gallery artworks")
    for name, fn in [("golden-currents", art_golden_currents), ("stained-glass", art_stained_glass),
                     ("harmonograph", art_harmonograph), ("interference", art_interference),
                     ("circle-garden", art_circle_garden), ("dusk-ridges", art_dusk_ridges)]:
        save_jpg(fn(), IMG / "samples" / f"sample-{name}.jpg", 85)

    print("UI images")
    save_jpg(upload_placeholder("Your AI Artwork", "Preview appears here"), IMG / "upload-placeholder.jpg", 88)
    save_jpg(upload_placeholder("Image Unavailable", "The file could not be loaded"), IMG / "image-unavailable.jpg", 88)
    icon = favicon()
    icon.save(WWWROOT / "favicon.ico", sizes=[(16, 16), (32, 32), (48, 48), (64, 64)])
    icon.resize((180, 180), Image.LANCZOS).save(IMG / "apple-touch-icon.png")
    print("  favicon.ico, apple-touch-icon.png")


if __name__ == "__main__":
    main()
