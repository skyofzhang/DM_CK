# gen_tab_assets.py
# Generates tab_active.png and tab_inactive.png for the Rankings UI.
# Output directory: E:/AIProject/DM_CK/Assets/Art/UI/Rankings/

from PIL import Image, ImageDraw, ImageFilter
import os

OUTPUT_DIR = r"E:\AIProject\DM_CK\Assets\Art\UI\Rankings"
W, H = 240, 52


def round_rect_mask(draw, xy, radius, fill):
    """Draw a rounded rectangle using arcs and rectangles (no rounded_rectangle API)."""
    x0, y0, x1, y1 = xy
    r = radius

    # Four corner circles
    draw.ellipse([x0, y0, x0 + 2*r, y0 + 2*r], fill=fill)
    draw.ellipse([x1 - 2*r, y0, x1, y0 + 2*r], fill=fill)
    draw.ellipse([x0, y1 - 2*r, x0 + 2*r, y1], fill=fill)
    draw.ellipse([x1 - 2*r, y1 - 2*r, x1, y1], fill=fill)

    # Three rectangles to fill the body
    draw.rectangle([x0 + r, y0, x1 - r, y1], fill=fill)
    draw.rectangle([x0, y0 + r, x1, y1 - r], fill=fill)


def make_active_tab():
    """240x52 ice-blue pill (#3A80E8, alpha=230) with lighter blue inner glow."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    radius = H // 2  # full pill shape

    # Base fill
    base_color = (0x3A, 0x80, 0xE8, 230)
    round_rect_mask(draw, (0, 0, W, H), radius, base_color)

    # Inner glow layer: slightly lighter blue, narrower, blurred then composited
    glow_img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow_img)
    glow_color = (0x7A, 0xB8, 0xFF, 90)  # lighter ice-blue, semi-transparent
    pad = 4
    round_rect_mask(glow_draw, (pad, pad, W - pad, H - pad), max(radius - pad, 0), glow_color)

    # Blur the glow slightly for softness
    glow_img = glow_img.filter(ImageFilter.GaussianBlur(radius=3))

    # Composite glow onto base
    img = Image.alpha_composite(img, glow_img)

    # Thin highlight stripe at top (frosted glass feel)
    highlight = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    h_draw = ImageDraw.Draw(highlight)
    h_color = (0xFF, 0xFF, 0xFF, 40)
    round_rect_mask(h_draw, (6, 3, W - 6, H // 2), max(radius - 6, 0), h_color)
    highlight = highlight.filter(ImageFilter.GaussianBlur(radius=2))
    img = Image.alpha_composite(img, highlight)

    return img


def make_inactive_tab():
    """240x52 dark navy pill (#0E1E4A, alpha=200)."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    radius = H // 2  # full pill shape
    base_color = (0x0E, 0x1E, 0x4A, 200)
    round_rect_mask(draw, (0, 0, W, H), radius, base_color)

    return img


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    active = make_active_tab()
    active_path = os.path.join(OUTPUT_DIR, "tab_active.png")
    active.save(active_path)
    print(f"Saved: {active_path}  size={active.size}  mode={active.mode}")

    inactive = make_inactive_tab()
    inactive_path = os.path.join(OUTPUT_DIR, "tab_inactive.png")
    inactive.save(inactive_path)
    print(f"Saved: {inactive_path}  size={inactive.size}  mode={inactive.mode}")

    print("Done.")


if __name__ == "__main__":
    main()
