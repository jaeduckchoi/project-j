"""Pure color conversion and pixel-frame geometry for Canvas UI."""

def popup_color(hex_color, alpha=1.0):
    """Convert authored sRGB popup palette to linear Canvas vertex colors."""
    channels = [int(hex_color.lstrip("#")[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    linear = [value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4 for value in channels]
    return "(R=%.8f,G=%.8f,B=%.8f,A=%.8f)" % (*linear, alpha)

def nine_slice_patches(x, y, width, height, source_size=32, border=8):
    """Nine destination rectangles and normalized UVs; corners never stretch."""
    if width < border * 2 or height < border * 2 or source_size <= border * 2:
        raise ValueError("Nine-slice dimensions must leave a positive centre")
    positions_x, positions_y = (x, x + border, x + width - border), (y, y + border, y + height - border)
    sizes_x, sizes_y = (border, width - border * 2, border), (border, height - border * 2, border)
    uv = (0.0, border / source_size, 1.0 - border / source_size)
    uv_sizes = (border / source_size, 1.0 - 2.0 * border / source_size, border / source_size)
    return [(positions_x[col], positions_y[row], sizes_x[col], sizes_y[row], uv[col], uv[row], uv_sizes[col], uv_sizes[row])
            for row in range(3) for col in range(3)]
