"""Compare actual source/current GPU images; alpha is render-target bookkeeping."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

import sys
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from world_paths import QA_ROOT
HERE = QA_ROOT
source = json.loads((HERE / "static_depth_render_report.json").read_text(encoding="utf-8"))
report = {"success": False, "method": "Same scene/camera/materials, original vs final sort priorities", "scenes": []}
for row in source["scenes"]:
    current = np.asarray(Image.open(row["files"]["current"]).convert("RGB"))
    baseline = np.asarray(Image.open(row["files"]["source_order"]).convert("RGB"))
    difference = np.abs(current.astype(np.int16) - baseline.astype(np.int16))
    changed = int(np.count_nonzero(np.any(difference, axis=2)))
    rgb_path = Path(row["files"]["current"]).with_name(row["name"] + "_current_RGB.png")
    Image.fromarray(current).save(rgb_path)
    report["scenes"].append({"name": row["name"], "changed_pixels": changed,
                              "rgb_mean_absolute_error": float(difference.mean()),
                              "color_variation": float(current.std()), "rgb_file": str(rgb_path),
                              "success": changed == 0 and float(current.std()) > 8})
report["success"] = source["success"] and len(report["scenes"]) == 2 and all(s["success"] for s in report["scenes"])
(HERE / "static_depth_pixels_report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report))
raise SystemExit(0 if report["success"] else 1)
