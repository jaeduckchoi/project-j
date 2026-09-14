"""Host geometry checks. Run with --manifest and --rules-root for map checks."""
import argparse
import copy
import json
import math
from pathlib import Path
import random
import unittest

import collision_geometry as geometry


MANIFEST_PATH = None
RULES_ROOT = Path(__file__).resolve().parent / "Data"
IDENTITY = [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]


def intersects(spec, x_cm, z_cm, radius_cm=26):
    """Independent circle/primitive overlap, in output centimetres."""
    dx, dz = x_cm - spec["center_cm"][0], z_cm - spec["center_cm"][2]
    if spec["shape"] == "sphere":
        return dx * dx + dz * dz < (radius_cm + spec["radius_cm"]) ** 2 - 1e-6
    angle = math.radians(spec.get("rotation_degrees", 0))
    local_x = math.cos(angle) * dx + math.sin(angle) * dz
    local_z = -math.sin(angle) * dx + math.cos(angle) * dz
    near_x = max(abs(local_x) - spec["extent_cm"][0], 0)
    near_z = max(abs(local_z) - spec["extent_cm"][2], 0)
    return near_x * near_x + near_z * near_z < radius_cm * radius_cm - 1e-6


class GeometryUnitTests(unittest.TestCase):
    def test_rotated_scaled_offset_box_and_reflection(self):
        matrix = [[0, -3, 0, 10], [2, 0, 0, -4], [0, 0, 1, 77], [0, 0, 0, 1]]
        rule = {"category": "furniture", "label": "test", "footprints": [
            {"shape": "box", "center": [1, 2], "size": [4, 6]}]}
        result = geometry._footprint_specs("collision:test", "source", matrix, rule, 100)[0]
        self.assertEqual(result["center_cm"], [400, 0, -200])
        self.assertEqual(result["extent_cm"], [400, 100, 900])
        self.assertEqual(result["rotation_degrees"], 90)
        matrix[1][0] = -2
        result = geometry._footprint_specs("collision:test", "source", matrix, rule, 100)[0]
        self.assertEqual(result["center_cm"], [400, 0, -600])
        self.assertEqual(result["extent_cm"], [400, 100, 900])
        self.assertEqual(result["rotation_degrees"], -90)

    def test_nonuniform_sphere_fails_instead_of_silent_shape_change(self):
        matrix = copy.deepcopy(IDENTITY)
        matrix[0][0] = 2
        rule = {"category": "nature", "label": "test", "footprints": [
            {"shape": "sphere", "center": [0, 0], "radius": .5}]}
        with self.assertRaisesRegex(ValueError, "uniform"):
            geometry._footprint_specs("test", "source", matrix, rule, 100)

    def test_partial_uv_patch_respects_pivot_ppu_and_rotation(self):
        sprite = {"rect": [7, 11, 32, 64], "pivot": [.25, .75], "ppu": 32}
        instance = {"world_matrix": [[0, -2, 0, 3], [2, 0, 0, 4],
                                      [0, 0, 1, 0], [0, 0, 0, 1]]}
        polygon = geometry._uv_rect_world(instance, sprite, [.5, 0, 1, .5])
        self.assertEqual(polygon, [(6, 4.5), (6, 5.5), (4, 5.5), (4, 4.5)])

    def test_merge_exactly_partitions_all_blocked_cells(self):
        rng = random.Random(2319)
        for _ in range(40):
            rows = [bytearray(rng.randrange(2) for _ in range(19)) for _ in range(13)]
            rectangles = geometry.merge_blocked_rectangles(rows)
            counts = [[0] * 19 for _ in range(13)]
            for x0, y0, x1, y1 in rectangles:
                for iy in range(y0, y1):
                    for ix in range(x0, x1):
                        counts[iy][ix] += 1
            for iy in range(13):
                for ix in range(19):
                    self.assertEqual(counts[iy][ix], 1 - rows[iy][ix])
        self.assertEqual(geometry.merge_blocked_rectangles([bytearray(12) for _ in range(9)]),
                         [(0, 0, 12, 9)])

    def test_terrain_unions_transformed_offset_dock_and_land(self):
        sprite = {"rect": [0, 0, 32, 32], "pivot": [.5, .5], "ppu": 32}
        land_matrix = copy.deepcopy(IDENTITY)
        dock_matrix = [[0, -1, 0, 1], [1, 0, 0, .4], [0, 0, 1, 0], [0, 0, 0, 1]]
        scene = {"name": "Test", "groups": [{"id": "tiles", "instances": [
            {"sprite_key": "sand", "world_matrix": land_matrix},
            {"sprite_key": "dock", "world_matrix": dock_matrix},
            {"sprite_key": "support", "world_matrix": IDENTITY}]}]}
        rules = {"scenes": {"Test": {"terrain": {"bounds": [-1, -1, 2, 2],
                 "grid_step": .1, "group_ids": ["tiles"]}}}, "terrain_sprite_rules": {
            "sand": {"walkable_uv": [[0, 0, 1, 1]]},
            "dock": {"walkable_uv": [[0, 0, 1, 1]]},
            "support": {"walkable_uv": []}}}
        rows, bounds, step = geometry.build_walkable_grid(scene, {"sprites": {
            "sand": sprite, "dock": sprite, "support": sprite}}, rules)
        def walkable(x, y):
            return bool(rows[int((y - bounds[1]) / step)][int((x - bounds[0]) / step)])
        self.assertTrue(walkable(.05, .05))  # dry sand
        self.assertTrue(walkable(1.05, .85))  # rotated dock translated by 40cm
        self.assertFalse(walkable(1.05, -.25))  # outside translated dock
        self.assertFalse(walkable(-.75, .75))  # surrounding water
        bad = copy.deepcopy(scene)
        bad["groups"][0]["instances"][0]["sprite_key"] = "new_unclassified"
        with self.assertRaisesRegex(ValueError, "Unclassified"):
            geometry.build_walkable_grid(bad, {"sprites": {}}, rules)


class CurrentMapTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not MANIFEST_PATH:
            raise unittest.SkipTest("Supply --manifest to validate current map assets")
        cls.manifest = json.loads(Path(MANIFEST_PATH).read_text(encoding="utf-8"))
        cls.rules = geometry.load_rules(RULES_ROOT)
        cls.scenes = {s["name"]: s for s in cls.manifest["scenes"]}
        cls.specs = {name: geometry.build_collision_specs(scene, cls.manifest, cls.rules)
                     for name, scene in cls.scenes.items()}

    def clear(self, scene, x, z, radius=26):
        return not any(intersects(spec, x, z, radius) for spec in self.specs[scene])

    def assert_route_clear(self, scene, points):
        for (x0, z0), (x1, z1) in zip(points, points[1:]):
            steps = max(1, math.ceil(math.hypot(x1 - x0, z1 - z0) / 5))
            for index in range(steps + 1):
                x = x0 + (x1 - x0) * index / steps
                z = z0 + (z1 - z0) * index / steps
                blocking = [s["id"] for s in self.specs[scene] if intersects(s, x, z)]
                self.assertFalse(blocking, f"{scene} route blocked at ({x:.1f},{z:.1f}): {blocking}")

    def test_maps_are_deterministic_and_inputs_unchanged(self):
        original = json.dumps(self.manifest, sort_keys=True)
        for name, scene in self.scenes.items():
            self.assertEqual(self.specs[name], geometry.build_collision_specs(scene, self.manifest, self.rules))
            self.assertEqual(len({s["id"] for s in self.specs[name]}), len(self.specs[name]))
        self.assertEqual(original, json.dumps(self.manifest, sort_keys=True))
        self.assertLess(len(self.specs["Beach"]), 150)

    def test_hub_spawn_table_passage_kitchen_and_exit_approach(self):
        self.assert_route_clear("Hub", [(-715, -415), (-400, -415), (-400, 100),
                                         (-1410, 100), (-1410, 450), (-700, 450)])
        self.assert_route_clear("Hub", [(-715, -415), (-1300, -415), (-1300, -690)])
        self.assertFalse(self.clear("Hub", -880, -262))
        self.assertFalse(self.clear("Hub", 542, 299))

    def test_beach_spawn_sand_to_connected_dock_and_water(self):
        self.assert_route_clear("Beach", [(1750, 1200), (1750, 500), (-500, 500),
                                           (-500, 1700), (-2200, 1700)])
        self.assert_route_clear("Beach", [(-500, 500), (-2500, 500), (-2500, 700)])
        self.assertFalse(self.clear("Beach", 0, 0))
        self.assertFalse(self.clear("Beach", -2500, 925))
        self.assertFalse(self.clear("Beach", 833, 1128))
        self.assertTrue(self.clear("Beach", 833, 1400))
        # The independently authored dark front support tile is not a dry deck.
        self.assertFalse(self.clear("Beach", -150, 340, 1))

    def test_external_edges_block_escape(self):
        for scene, x, z in [("Hub", 0, -910), ("Hub", 1610, 0),
                            ("Beach", 3210, 1200), ("Beach", 1750, 1860)]:
            self.assertFalse(self.clear(scene, x, z))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest")
    parser.add_argument("--rules-root", type=Path, default=RULES_ROOT)
    args, remaining = parser.parse_known_args()
    MANIFEST_PATH = args.manifest
    RULES_ROOT = args.rules_root
    unittest.main(argv=[__file__] + remaining)
