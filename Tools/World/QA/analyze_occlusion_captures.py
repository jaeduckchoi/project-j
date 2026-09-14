"""Compare actual Unreal renders on pixels shared by player and target prop.

Run with host Python (Pillow/numpy) after verify_occlusion_pie.py. An overlapping
pixel in front must match the player-without-target render, and a pixel behind
must match the world-only render. Nothing is inferred from the sort formula.
"""
from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from world_paths import QA_ROOT

import numpy as np
from PIL import Image


def analyze(report_path):
    runtime = json.loads(report_path.read_text(encoding="utf-8"))
    report = {"schema_version": 1, "success": False, "runtime_report": str(report_path),
              "runtime_success": runtime.get("success", False), "views": [], "failures": []}
    for case in runtime.get("cases", []):
        for capture in case["captures"]:
            images = {name: np.asarray(Image.open(path).convert("RGB"), dtype=np.int16)
                      for name, path in capture["variants"].items()}
            actual, world, player, background = [images[n] for n in
                ("actual", "world_only", "player_without_target", "background")]
            # Strong differences exclude transparent margins and equal-color
            # overlaps where neither ordering could be visually distinguished.
            target_mask = np.max(np.abs(world - background), axis=2) >= 24
            player_mask = np.max(np.abs(player - background), axis=2) >= 24
            distinct = np.max(np.abs(world - player), axis=2) >= 24
            overlap = target_mask & player_mask & distinct
            count = int(np.count_nonzero(overlap))
            expected = player if capture["view"] == "front" else world
            unexpected = world if capture["view"] == "front" else player
            expected_error = np.mean(np.abs(actual - expected), axis=2)[overlap]
            other_error = np.mean(np.abs(actual - unexpected), axis=2)[overlap]
            winner_fraction = float(np.mean(expected_error + 3 < other_error)) if count else 0.0
            mean_error = float(np.mean(expected_error)) if count else None
            passed = count >= 50 and winner_fraction >= 0.90 and mean_error <= 8.0
            rgb_path = Path(capture["variants"]["actual"]).with_name(case["name"] + "_" + capture["view"] + "_RGB.png")
            Image.fromarray(actual.astype(np.uint8)).save(rgb_path)
            row = {"case": case["name"], "view": capture["view"], "overlapping_pixels": count,
                   "expected_visible_layer": "player" if capture["view"] == "front" else "prop",
                   "expected_layer_wins_fraction": winner_fraction, "mean_expected_rgb_error": mean_error,
                   "success": passed, "image": str(rgb_path), "position": capture["position"]}
            report["views"].append(row)
            if not passed:
                report["failures"].append(row)
    report["success"] = report["runtime_success"] and len(report["views"]) == 8 and not report["failures"]
    output = report_path.with_name("occlusion_pixels_report.json")
    output.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({"success": report["success"], "views": len(report["views"]),
                      "failures": len(report["failures"]), "report": str(output)}))
    return report["success"]


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", type=Path, default=QA_ROOT / "occlusion_pie_report.json")
    args = parser.parse_args()
    raise SystemExit(0 if analyze(args.report) else 1)
