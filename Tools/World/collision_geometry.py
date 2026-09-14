"""Deterministic, stdlib-only compilation of authored top-down collision rules.

Input matrices and footprint coordinates use Unity XY world units. Output is
Unreal XZ centimetres; render depth is deliberately excluded from physics.
This module has no Unreal dependency and does not modify its inputs.
"""
from __future__ import annotations

import hashlib
import json
import math
from pathlib import Path


def load_rules(data_root):
    with (Path(data_root) / "collision_rules.json").open(encoding="utf-8") as stream:
        rules = json.load(stream)
    if rules.get("schema_version") != 1:
        raise ValueError("Unsupported collision rules schema")
    return rules


def rules_hash(rules):
    return hashlib.sha256(json.dumps(rules, sort_keys=True, separators=(",", ":"),
                                     ensure_ascii=False).encode("utf-8")).hexdigest()


def _point(matrix, x, y):
    return (matrix[0][0] * x + matrix[0][1] * y + matrix[0][3],
            matrix[1][0] * x + matrix[1][1] * y + matrix[1][3])


def _round(value):
    value = round(float(value), 6)
    return 0.0 if abs(value) < 0.0000005 else value


def _base_spec(identity, parent_id, category, label, shape, center, rotation=0.0):
    return {"id": identity, "parent_id": parent_id, "category": category,
            "label": label, "shape": shape,
            "center_cm": [_round(center[0] * 100), 0.0, _round(center[1] * 100)],
            "rotation_degrees": _round(rotation)}


def _footprint_specs(identity, parent_id, matrix, rule, depth):
    sx = math.hypot(matrix[0][0], matrix[1][0])
    sy = math.hypot(matrix[0][1], matrix[1][1])
    if sx <= 1e-9 or sy <= 1e-9:
        raise ValueError("Zero-scale collision footprint: " + identity)
    dot = matrix[0][0] * matrix[0][1] + matrix[1][0] * matrix[1][1]
    if abs(dot) > sx * sy * 1e-5:
        raise ValueError("Sheared collision footprint is unsupported: " + identity)
    rotation = math.degrees(math.atan2(matrix[1][0], matrix[0][0]))
    result = []
    for index, footprint in enumerate(rule.get("footprints", [])):
        center = _point(matrix, *footprint.get("center", [0, 0]))
        shape = footprint["shape"]
        spec = _base_spec(identity + ":" + str(index), parent_id,
                          rule["category"], rule["label"], shape, center, rotation)
        if shape == "sphere":
            if abs(sx - sy) > max(sx, sy) * 1e-5:
                raise ValueError("Sphere requires uniform XY scale: " + identity)
            spec["radius_cm"] = _round(footprint["radius"] * sx * 100)
            spec["rotation_degrees"] = 0.0
        elif shape == "box":
            width, height = footprint["size"]
            if width <= 0 or height <= 0:
                raise ValueError("Non-positive collision footprint: " + identity)
            spec["extent_cm"] = [_round(width * sx * 50), depth,
                                 _round(height * sy * 50)]
        else:
            raise ValueError("Unsupported footprint shape: " + shape)
        result.append(spec)
    return result


def _sprite(manifest, key):
    try:
        return manifest["sprites"][key]
    except KeyError as exc:
        raise ValueError("Missing sprite metadata: " + str(key)) from exc


def _uv_rect_world(instance, sprite, rect):
    """A UV patch uses bottom-left origin and follows the full instance matrix."""
    width, height = sprite["rect"][2:4]
    ppu = sprite.get("ppu", sprite.get("pixels_per_unit"))
    if not ppu or ppu <= 0:
        raise ValueError("Invalid sprite pixels-per-unit")
    px, py = sprite["pivot"]
    x0, y0, x1, y1 = rect
    if not (0 <= x0 < x1 <= 1 and 0 <= y0 < y1 <= 1):
        raise ValueError("UV patch must be a positive rectangle inside [0,1]")
    matrix = instance["world_matrix"]
    return [_point(matrix, (x - px) * width / ppu, (y - py) * height / ppu)
            for x, y in [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]]


def _inside_convex(x, y, polygon):
    sign = 0
    for index, (ax, ay) in enumerate(polygon):
        bx, by = polygon[(index + 1) % len(polygon)]
        cross = (bx - ax) * (y - ay) - (by - ay) * (x - ax)
        if abs(cross) <= 1e-8:
            continue
        current = 1 if cross > 0 else -1
        if sign and current != sign:
            return False
        sign = current
    return True


def _paint_patch(rows, polygon, bounds, step):
    """Rasterize only intersecting cells, never scan the entire map per tile."""
    xmin, ymin, xmax, ymax = bounds
    height, width = len(rows), len(rows[0])
    xs, ys = zip(*polygon)
    ix0 = max(0, math.floor((min(xs) - xmin) / step))
    ix1 = min(width, math.ceil((max(xs) - xmin) / step))
    iy0 = max(0, math.floor((min(ys) - ymin) / step))
    iy1 = min(height, math.ceil((max(ys) - ymin) / step))
    for iy in range(iy0, iy1):
        y = ymin + (iy + 0.5) * step
        row = rows[iy]
        for ix in range(ix0, ix1):
            x = xmin + (ix + 0.5) * step
            if _inside_convex(x, y, polygon):
                row[ix] = 1


def build_walkable_grid(scene, manifest, rules):
    """Return (rows, bounds, step) for the scene's authored dry-ground union.

    rows are bytearrays indexed [world_y][world_x]; 1 means walkable. Cell-center
    rasterization has <= step/2 edge error. Dock and land patches are UNIONED in
    world coordinates, so a dock can cover water at any authored offset.
    """
    config = rules["scenes"][scene["name"]]
    terrain = config.get("terrain")
    if not terrain:
        return None
    bounds = terrain["bounds"]
    step = terrain["grid_step"]
    xmin, ymin, xmax, ymax = bounds
    if step <= 0 or xmax <= xmin or ymax <= ymin:
        raise ValueError("Invalid terrain grid bounds")
    width = round((xmax - xmin) / step)
    height = round((ymax - ymin) / step)
    if width * height > 2_000_000:
        raise ValueError("Terrain grid is too large")
    if not math.isclose(width * step, xmax - xmin, abs_tol=1e-7) or not math.isclose(
            height * step, ymax - ymin, abs_tol=1e-7):
        raise ValueError("Terrain bounds must align with grid step")
    rows = [bytearray(width) for _ in range(height)]
    tile_rules = rules["terrain_sprite_rules"]
    group_ids = set(terrain["group_ids"])
    found = set()
    for group in scene["groups"]:
        if group["id"] not in group_ids:
            continue
        found.add(group["id"])
        for instance in group.get("instances", []):
            key = instance["sprite_key"]
            if key not in tile_rules:
                raise ValueError("Unclassified terrain sprite: " + key)
            sprite = _sprite(manifest, key)
            for patch in tile_rules[key].get("walkable_uv", []):
                _paint_patch(rows, _uv_rect_world(instance, sprite, patch), bounds, step)
    if found != group_ids:
        raise ValueError("Missing terrain groups: " + ", ".join(sorted(group_ids - found)))
    return rows, bounds, step


def merge_blocked_rectangles(rows):
    """Partition zero cells into exact non-overlapping rectangles, bottom to top.

    Runs with identical x extents are extended across successive rows. No gap,
    overlap or expansion is introduced by rectangle merging.
    """
    active = {}
    completed = []
    for iy, row in enumerate(rows):
        runs = []
        ix = 0
        while ix < len(row):
            if row[ix]:
                ix += 1
                continue
            start = ix
            while ix < len(row) and not row[ix]:
                ix += 1
            runs.append((start, ix))
        next_active = {}
        for run in runs:
            next_active[run] = active.pop(run, (run[0], iy, run[1], iy))[:3] + (iy + 1,)
        completed.extend(active.values())
        active = next_active
    completed.extend(active.values())
    return sorted(completed, key=lambda rect: (rect[1], rect[0], rect[3], rect[2]))


def _boundary_specs(scene, config, depth):
    xmin, ymin, xmax, ymax = config["bounds"]
    thick = config.get("boundary_thickness", 0.5)
    # Boundaries sit outside the visible floor, leaving its interior usable.
    faces = [("left", xmin - thick / 2, (ymin + ymax) / 2, thick, ymax - ymin + 2 * thick),
             ("right", xmax + thick / 2, (ymin + ymax) / 2, thick, ymax - ymin + 2 * thick),
             ("bottom", (xmin + xmax) / 2, ymin - thick / 2, xmax - xmin, thick),
             ("top", (xmin + xmax) / 2, ymax + thick / 2, xmax - xmin, thick)]
    result = []
    for face, x, y, width, height in faces:
        spec = _base_spec("collision:" + scene["name"] + ":boundary:" + face,
                          config["bounds_parent_id"], "boundary", "Map boundary " + face,
                          "box", (x, y))
        spec["extent_cm"] = [_round(width * 50), depth, _round(height * 50)]
        result.append(spec)
    return result


def build_collision_specs(scene, manifest, rules):
    """Compile immutable inputs to deterministic, unit-world-scale primitives."""
    scene_name = scene["name"]
    config = rules["scenes"][scene_name]
    depth = rules.get("default_depth_half_extent_cm", 100.0)
    result = _boundary_specs(scene, config, depth)
    objects = {obj["id"]: obj for obj in scene["objects"]}
    object_rules = rules.get("object_rules", {})
    for object_id in sorted(config.get("object_rule_ids", [])):
        obj = objects.get(object_id)
        if obj is None:
            raise ValueError("Missing authored collision object: " + object_id)
        if not obj.get("active_in_hierarchy", True):
            continue
        result.extend(_footprint_specs("collision:" + object_id, object_id,
                                       obj["world_matrix"], object_rules[object_id], depth))
    sprite_rules = rules.get("sprite_rules", {})
    for group in scene["groups"]:
        if group.get("kind") != "sprite" or group.get("status") not in (None, "visible"):
            continue
        key = group.get("source_sprite_key", group.get("sprite_key"))
        rule = sprite_rules.get(key)
        if not rule or scene_name not in rule.get("scenes", [scene_name]):
            continue
        obj = objects[group["object_id"]]
        if not obj.get("active_in_hierarchy", True):
            continue
        result.extend(_footprint_specs("collision:" + group["id"], group["object_id"],
                                       group["world_matrix"], rule, depth))
    grid = build_walkable_grid(scene, manifest, rules)
    if grid:
        rows, bounds, step = grid
        terrain = config["terrain"]
        rectangles = merge_blocked_rectangles(rows)
        if len(rectangles) > terrain.get("max_rectangles", 1000):
            raise ValueError("Terrain collision rectangle budget exceeded")
        for ix0, iy0, ix1, iy1 in rectangles:
            center = (bounds[0] + (ix0 + ix1) * step / 2,
                      bounds[1] + (iy0 + iy1) * step / 2)
            suffix = ":".join(map(str, (ix0, iy0, ix1, iy1)))
            spec = _base_spec("collision:" + scene_name + ":water:" + suffix,
                              terrain["parent_id"], "water", "Water and shore edge", "box", center)
            spec["extent_cm"] = [_round((ix1 - ix0) * step * 50), depth,
                                 _round((iy1 - iy0) * step * 50)]
            result.append(spec)
    identities = [spec["id"] for spec in result]
    if len(set(identities)) != len(identities):
        raise ValueError("Duplicate collision primitive identity")
    return sorted(result, key=lambda spec: spec["id"])
