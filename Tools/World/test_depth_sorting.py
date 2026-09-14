"""Host checks for the shared player/prop feet draw-order contract."""
import argparse
import copy
import json
from pathlib import Path
import unittest

import collision_geometry
import depth_sorting
from test_collision_geometry import intersects


MANIFEST_PATH = None
RULES_ROOT = Path(__file__).resolve().parent / "Data"


class DepthFormulaTests(unittest.TestCase):
    def test_matches_blueprint_floor_plus_half_at_negative_ties(self):
        self.assertEqual(depth_sorting.player_sort_priority(0), 0)
        self.assertEqual(depth_sorting.player_sort_priority(.5), -1)
        self.assertEqual(depth_sorting.player_sort_priority(-.5), 0)
        self.assertEqual(depth_sorting.player_sort_priority(-1.5), 1)
        self.assertEqual(depth_sorting.player_sort_priority(370), -370)
        self.assertEqual(depth_sorting.player_sort_priority(-415), 415)

    def test_nonfinite_position_is_rejected(self):
        for value in (float("nan"), float("inf"), -float("inf")):
            with self.assertRaises(ValueError):
                depth_sorting.player_sort_priority(value)


class AuthoredDepthTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        if not MANIFEST_PATH:
            raise unittest.SkipTest("Supply --manifest to validate current source draw order")
        cls.manifest = json.loads(Path(MANIFEST_PATH).read_text(encoding="utf-8"))
        cls.rules = collision_geometry.load_rules(RULES_ROOT)
        cls.scenes = {scene["name"]: scene for scene in cls.manifest["scenes"]}
        cls.specs = {name: {spec["group_id"]: spec for spec in depth_sorting.build_depth_sort_specs(
            scene, cls.manifest, cls.rules)} for name, scene in cls.scenes.items()}
        cls.collisions = {name: collision_geometry.build_collision_specs(scene, cls.manifest, cls.rules)
                          for name, scene in cls.scenes.items()}

    def test_all_render_groups_classified_and_no_source_mutation(self):
        before = json.dumps(self.manifest, sort_keys=True)
        for name, scene in self.scenes.items():
            self.assertEqual(set(self.specs[name]), {group["id"] for group in scene["groups"]})
            self.assertEqual(list(self.specs[name].values()),
                             depth_sorting.build_depth_sort_specs(scene, self.manifest, self.rules))
        self.assertEqual(before, json.dumps(self.manifest, sort_keys=True))

    def test_counter_split_and_board_labels_share_their_physical_base(self):
        hub = self.specs["Hub"]
        counter_parts = [hub[key] for key in ("Hub:2031000003", "Hub:2031000013",
                                             "Hub:2031000033", "Hub:2031000043", "Hub:1531594859")]
        self.assertTrue(all(abs(row["anchor_z_cm"] - 325.87965) < 1e-5 for row in counter_parts))
        self.assertEqual(hub["Hub:2031000013"]["priority"] - hub["Hub:2031000003"]["priority"], 2)
        for food in ("Hub:1526965162", "Hub:1723958469", "Hub:412686207"):
            self.assertEqual(hub[food]["anchor_z_cm"], hub["Hub:210100033"]["anchor_z_cm"])
            self.assertEqual(hub[food]["priority"] - hub["Hub:210100033"]["priority"], 2)
        for gauge in ("Hub:1026610951", "Hub:1060963302", "Hub:770303012", "Hub:969813992"):
            self.assertEqual(hub[gauge]["anchor_z_cm"], hub["Hub:210100003"]["anchor_z_cm"])
            self.assertEqual(hub[gauge]["priority"] - hub["Hub:210100003"]["priority"], 4)

    def test_front_and_behind_are_reachable_and_switch_occlusion(self):
        cases = [
            ("Hub", "Hub:210100033", (542, 235), (542, 370), [(750, 235), (750, 370)]),
            ("Hub", "Hub:2031000003", (-700, 235), (-700, 420),
             [(-700, 100), (-1410, 100), (-1410, 420)]),
            ("Hub", "Hub:1336547639", (0, -365), (0, -165), [(310, -365), (310, -165)]),
            ("Beach", "Beach:781787690", (833, 1040), (833, 1212), [(930, 1040), (930, 1212)]),
        ]
        for name, group_id, front, behind, via in cases:
            static_priority = self.specs[name][group_id]["priority"]
            self.assertGreater(depth_sorting.player_sort_priority(front[1]), static_priority)
            self.assertLess(depth_sorting.player_sort_priority(behind[1]), static_priority)
            points = [front] + via + [behind]
            for (x0, z0), (x1, z1) in zip(points, points[1:]):
                count = max(1, int(max(abs(x1 - x0), abs(z1 - z0)) / 5) + 1)
                for step in range(count + 1):
                    x = x0 + (x1 - x0) * step / count
                    z = z0 + (z1 - z0) * step / count
                    blocked = [row["id"] for row in self.collisions[name] if intersects(row, x, z, 26)]
                    self.assertFalse(blocked, f"{group_id} route ({x},{z}) blocked by {blocked}")

    def test_ground_is_below_gameplay_and_outline_preserves_composition(self):
        hub = self.specs["Hub"]
        beach = self.specs["Beach"]
        self.assertEqual(hub["Hub:27645660"]["priority"], -29997)
        self.assertLess(hub["Hub:218489564"]["priority"], hub["Hub:210100013"]["priority"])
        self.assertLess(hub["Hub:210100013"]["priority"], hub["Hub:210100023"]["priority"])
        self.assertLess(beach["Beach:41280233"]["priority"], beach["Beach:2049905414"]["priority"])
        for name, specs in self.specs.items():
            for row in specs.values():
                if row["category"] == "background":
                    self.assertLess(row["priority"], depth_sorting.player_sort_priority(1850))
        self.assertEqual(hub["Hub:655320554"]["priority"], 415)
        self.assertEqual(beach["Beach:100512"]["priority"], -1200)

    def test_new_visible_group_requires_explicit_classification(self):
        rules = copy.deepcopy(self.rules)
        del rules["depth_sorting"]["scenes"]["Hub"]["groups"]["Hub:210100033"]
        with self.assertRaisesRegex(ValueError, "Unclassified visible depth"):
            depth_sorting.build_depth_sort_specs(self.scenes["Hub"], self.manifest, rules)

    def test_editing_formula_constants_cannot_silently_diverge_from_player(self):
        for field, value in (("gameplay_base", 2000000), ("units_per_cm", 100)):
            rules = copy.deepcopy(self.rules)
            rules["depth_sorting"][field] = value
            with self.assertRaisesRegex(ValueError, "match the player Blueprint"):
                depth_sorting.build_depth_sort_specs(self.scenes["Hub"], self.manifest, rules)
            with self.assertRaisesRegex(ValueError, "match the player Blueprint"):
                depth_sorting.player_sort_priority(370, rules)

    def test_all_authored_priorities_fit_native_scene_proxy_int16(self):
        for rows in self.specs.values():
            for row in rows.values():
                self.assertGreaterEqual(row["priority"], -32768)
                self.assertLessEqual(row["priority"], 32767)
        for z in (-1750, 1850):
            self.assertTrue(-32768 <= depth_sorting.player_sort_priority(z) <= 32767)
        with self.assertRaisesRegex(ValueError, "signed int16"):
            depth_sorting.player_sort_priority(-40000)
        rules = copy.deepcopy(self.rules)
        rules["depth_sorting"]["background_base"] = -1000000
        with self.assertRaisesRegex(ValueError, "background tier"):
            depth_sorting.build_depth_sort_specs(self.scenes["Hub"], self.manifest, rules)
        scene = copy.deepcopy(self.scenes["Hub"])
        next(group for group in scene["groups"] if group["id"] == "Hub:218489564")["sorting_order"] = -5000
        with self.assertRaisesRegex(ValueError, "signed int16"):
            depth_sorting.build_depth_sort_specs(scene, self.manifest, self.rules)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest")
    parser.add_argument("--rules-root", type=Path, default=RULES_ROOT)
    args, remaining = parser.parse_known_args()
    MANIFEST_PATH = args.manifest
    RULES_ROOT = args.rules_root
    unittest.main(argv=[__file__] + remaining)
