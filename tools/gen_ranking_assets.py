# -*- coding: utf-8 -*-
import os
from PIL import Image, ImageDraw

out = r"E:\AIProject\DM_CK\Assets\Art\UI\Rankings"
if not os.path.exists(out):
    os.makedirs(out)

def round_rect(draw, x0, y0, x1, y1, radius, fill, outline=None, width=2):
    draw.rectangle([x0+radius, y0, x1-radius, y1], fill=fill)
    draw.rectangle([x0, y0+radius, x1, y1-radius], fill=fill)
    draw.ellipse([x0, y0, x0+2*radius, y0+2*radius], fill=fill)
    draw.ellipse([x1-2*radius, y0, x1, y0+2*radius], fill=fill)
    draw.ellipse([x0, y1-2*radius, x0+2*radius, y1], fill=fill)
    draw.ellipse([x1-2*radius, y1-2*radius, x1, y1], fill=fill)
    if outline:
        draw.arc([x0, y0, x0+2*radius, y0+2*radius], 180, 270, fill=outline, width=width)
        draw.arc([x1-2*radius, y0, x1, y0+2*radius], 270, 360, fill=outline, width=width)
        draw.arc([x0, y1-2*radius, x0+2*radius, y1], 90, 180, fill=outline, width=width)
        draw.arc([x1-2*radius, y1-2*radius, x1, y1], 0, 90, fill=outline, width=width)
        draw.line([x0+radius, y0, x1-radius, y0], fill=outline, width=width)
        draw.line([x0+radius, y1, x1-radius, y1], fill=outline, width=width)
        draw.line([x0, y0+radius, x0, y1-radius], fill=outline, width=width)
        draw.line([x1, y0+radius, x1, y1-radius], fill=outline, width=width)

# ── ranking_panel_bg.png (640x760) ─────────────────────────────────────────
W, H = 640, 760
img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)
r = 20
bg = (8, 20, 52, 230)
bd = (70, 170, 255, 200)
round_rect(draw, 2, 2, W-3, H-3, r, bg, bd, 3)

# header accent bar below title area
for i in range(4):
    alpha = 160 - i*35
    draw.rectangle([20, 80+i, W-20, 81+i], fill=(70, 170, 255, alpha))

img.save(os.path.join(out, "ranking_panel_bg.png"))
print("ranking_panel_bg.png done")

# ── ranking_row_bg.png (500x52) ────────────────────────────────────────────
RW, RH = 500, 52
row = Image.new("RGBA", (RW, RH), (0, 0, 0, 0))
rdraw = ImageDraw.Draw(row)
round_rect(rdraw, 0, 2, RW-1, RH-3, 8, (20, 50, 110, 120))
row.save(os.path.join(out, "ranking_row_bg.png"))
print("ranking_row_bg.png done")

# ── ranking_row_top1/2/3_bg.png ────────────────────────────────────────────
top3_colors = [
    (180, 140, 20, 150),
    (150, 160, 170, 130),
    (160, 100, 50, 120),
]
for i, col in enumerate(top3_colors):
    ri = Image.new("RGBA", (RW, RH), (0, 0, 0, 0))
    rd = ImageDraw.Draw(ri)
    round_rect(rd, 0, 2, RW-1, RH-3, 8, col)
    name = "ranking_row_top%d_bg.png" % (i+1)
    ri.save(os.path.join(out, name))
    print(name + " done")

# ── overlay_mask.png (4x4 semi-transparent black) ──────────────────────────
mask = Image.new("RGBA", (4, 4), (0, 0, 0, 160))
mask.save(os.path.join(out, "overlay_mask.png"))
print("overlay_mask.png done")
