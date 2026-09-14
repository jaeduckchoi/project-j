"""Shared feet-based draw order for editable static sprites and the player.

Only priorities are compiled here. Sprite geometry, transform, materials,
visibility, source artwork, and collision footprints are never changed.
"""
from __future__ import annotations

import math


GAMEPLAY_BASE = 0
UNITS_PER_CM = 1
BACKGROUND_BASE = -30_000
ENGINE_PRIORITY_MIN = -32_768
ENGINE_PRIORITY_MAX = 32_767


def _validate_config(config):
    if config.get("schema_version") != 1 or config.get("rounding") != "floor_plus_half":
        raise ValueError("Unsupported depth-sorting contract")
    if config.get("gameplay_base") != GAMEPLAY_BASE or config.get("units_per_cm") != UNITS_PER_CM:
        raise ValueError("Depth-sorting constants must match the player Blueprint: base=0, units_per_cm=1")
    if config.get("background_base") != BACKGROUND_BASE:
        raise ValueError("Depth-sorting background tier must remain -30000")
    if config.get("engine_priority_min") != ENGINE_PRIORITY_MIN or \
            config.get("engine_priority_max") != ENGINE_PRIORITY_MAX:
        raise ValueError("Depth-sorting engine range must match signed int16 scene-proxy storage")


def _checked_priority(priority):
    if not ENGINE_PRIORITY_MIN <= priority <= ENGINE_PRIORITY_MAX:
        raise ValueError("Translucent priority exceeds the renderer's signed int16 range: " + str(priority))
    return int(priority)


def player_sort_priority(feet_z_cm, rules=None):
    """Match Blueprint -FFloor(Z + 0.5), including negative coordinates."""
    config = rules.get("depth_sorting", {}) if rules is not None else {}
    if config:
        _validate_config(config)
    base = config.get("gameplay_base", GAMEPLAY_BASE)
    units = config.get("units_per_cm", UNITS_PER_CM)
    if not math.isfinite(feet_z_cm):
        raise ValueError("Draw-order feet position must be finite")
    return _checked_priority(base - math.floor(feet_z_cm * units + 0.5))


def _anchor_z(matrix, local_xy):
    return 100 * (matrix[1][0] * local_xy[0] + matrix[1][1] * local_xy[1] + matrix[1][3])


def build_depth_sort_specs(scene, manifest, rules):
    """Return one priority spec per source render group, sorted by group ID.

    A depth group may share another render group's anchor, keeping a counter's
    split pieces or a board's labels together while retaining authored suborder.
    Player source markers use their parent player's root transform, not the
    higher visual origin. Hidden source render groups keep their source order.
    """
    config = rules["depth_sorting"]
    _validate_config(config)
    settings = config["scenes"][scene["name"]]["groups"]
    groups = {group["id"]: group for group in scene["groups"]}
    objects = {obj["id"]: obj for obj in scene["objects"]}
    layers = {value: index for index, value in enumerate(sorted({
        group.get("sorting_layer_id", 0) for group in scene["groups"]}))}
    result = []
    for group_id, group in sorted(groups.items()):
        rule = settings.get(group_id)
        if rule is None:
            if group.get("status", "visible") == "visible":
                raise ValueError("Unclassified visible depth group: " + group_id)
            rule = {"category": "source"}
        category = rule["category"]
        source_priority = layers[group.get("sorting_layer_id", 0)] * 100000 + group["sorting_order"]
        suborder = int(rule.get("suborder", 0))
        anchor_group_id = rule.get("anchor_group_id")
        anchor_object_id = rule.get("anchor_object_id")
        local_xy = rule.get("anchor_local_xy")
        anchor_z_cm = None
        if category in ("depth", "player"):
            if bool(anchor_group_id) == bool(anchor_object_id):
                raise ValueError("Specify exactly one group/object depth anchor: " + group_id)
            if not isinstance(local_xy, list) or len(local_xy) != 2:
                raise ValueError("Missing explicit local feet anchor: " + group_id)
            if abs(suborder) > 4:
                raise ValueError("Static suborder exceeds the 4cm tie band: " + group_id)
            try:
                matrix = (groups[anchor_group_id] if anchor_group_id else
                          objects[anchor_object_id])["world_matrix"]
            except KeyError as exc:
                raise ValueError("Missing draw-order anchor for " + group_id) from exc
            anchor_z_cm = _anchor_z(matrix, local_xy)
            priority = player_sort_priority(anchor_z_cm, rules) + suborder
        elif category in ("background", "source"):
            # Preserve ALL original background/rim/source interleaving within
            # one safe tier, rather than sending some below int16 storage.
            priority = int(config.get("background_base", BACKGROUND_BASE) + source_priority)
        else:
            raise ValueError("Unsupported draw-order category: " + category)
        result.append({"group_id": group_id, "object_id": group["object_id"],
                       "category": category, "priority": _checked_priority(priority),
                       "source_priority": int(source_priority),
                       "anchor_z_cm": anchor_z_cm, "suborder": suborder,
                       "anchor_group_id": anchor_group_id,
                       "anchor_object_id": anchor_object_id,
                       "anchor_local_xy": local_xy})
    return result
