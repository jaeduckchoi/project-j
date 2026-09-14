"""Author non-destructive PaperSprite regions for the eight-pose walk.

No new raster art is generated: all UVs reference the original idle textures.
Region pivots share the original 61x82 canvas pivot, so neutral parts align.
"""
from __future__ import annotations

import unreal

from world_paths import CONTENT_ROOT

PART_NAMES = ("head", "body", "left_foot", "right_foot")
ROOT = CONTENT_ROOT + "/Sprites/Player/WalkParts"
REGIONS = {
    "front": ((0, 0, 61, 56), (0, 54, 61, 22), (18, 74, 12, 8), (30, 74, 12, 8)),
    "back": ((0, 0, 61, 54), (0, 52, 61, 24), (21, 74, 12, 8), (33, 74, 12, 8)),
    "side": ((0, 0, 61, 56), (0, 54, 61, 22), (18, 74, 14, 8), (18, 74, 14, 8)),
}


def part_path(direction, name):
    return ROOT + "/" + direction.title() + "/S_Walk_" + direction.title() + "_" + name


def load_walk_parts():
    return {d: [unreal.load_asset(part_path(d, n)) for n in PART_NAMES] for d in REGIONS}


def apply_walk_defaults(pawn, parts=None):
    parts = parts if parts is not None else load_walk_parts()
    for direction, sprites in parts.items():
        if len(sprites) != 4 or any(s is None for s in sprites):
            raise RuntimeError("Missing original-texture walk parts for " + direction)
        pawn.set_editor_property(direction + "_walk_parts", sprites)


def ensure_walk_parts(frames, material):
    assets = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    tools = unreal.AssetToolsHelpers.get_asset_tools()
    parts = {}
    for direction, regions in REGIONS.items():
        texture = frames[direction][0].get_editor_property("source_texture")
        parts[direction] = []
        for name, (x, y, width, height) in zip(PART_NAMES, regions):
            path = part_path(direction, name)
            sprite = assets.load_asset(path) if assets.does_asset_exist(path) else None
            if sprite is None:
                sprite = tools.create_asset(path.rsplit("/", 1)[1], path.rsplit("/", 1)[0], unreal.PaperSprite, unreal.PaperSpriteFactory())
            if not isinstance(sprite, unreal.PaperSprite):
                raise RuntimeError("Invalid walk sprite: " + path)
            sprite.set_editor_property("source_texture", texture)
            sprite.set_editor_property("source_uv", unreal.Vector2D(x, y))
            sprite.set_editor_property("source_dimension", unreal.Vector2D(width, height))
            sprite.set_editor_property("pixels_per_unreal_unit", 0.8)
            sprite.set_editor_property("snap_pivot_to_pixel_grid", False)
            sprite.set_editor_property("pivot_mode", unreal.SpritePivotMode.CUSTOM)
            sprite.set_editor_property("custom_pivot_point", unreal.Vector2D(30.5, 75.44))
            # PaperSprite custom pivot is an absolute point in source texture
            # coordinates (not relative to this region's SourceUV).
            sprite.set_editor_property("sprite_collision_domain", unreal.SpriteCollisionMode.NONE)
            geometry = sprite.get_editor_property("render_geometry")
            geometry.set_editor_property("geometry_type", unreal.SpritePolygonMode.SOURCE_BOUNDING_BOX)
            sprite.set_editor_property("render_geometry", geometry)
            sprite.set_editor_property("default_material", material)
            assets.set_metadata_tag(sprite, "PlayerWalkSource", frames[direction][0].get_path_name())
            assets.set_metadata_tag(sprite, "PlayerWalkPart", name)
            if not assets.save_loaded_asset(sprite, only_if_is_dirty=False):
                raise RuntimeError("Cannot save walk region " + path)
            parts[direction].append(sprite)
    return parts
