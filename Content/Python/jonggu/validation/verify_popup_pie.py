"""Capture the real PIE HUD/popups at exact floating-window viewport sizes.

Launch a fresh editor with -JongguPopupQA and -ExecutePythonScript=<this file>.
Use -JongguHUDQA for the main HUD matrix under Saved/MainHUDQA; popup mode is
unchanged. HUD mode includes a real Blueprint travel to the beach map.
Use -JongguAdaptiveHUDQA for real fold/bubble/occlusion evidence under
Saved/AdaptiveHUDQA, including temporary actual Pawn projection poses.
Add -AdaptiveCoverageOnly for eight remaining groups fade/restore (16 cases
per size), plus one compact cooking showcase at the first requested size.
Coverage output and latest_coverage_report.json preserve the main matrix.
Optional: -PopupSizes=1280x720,1920x1080,1189x862 (default is all three).
Use -RenderOffscreen on a display whose work area cannot fit the largest PIE
window. Unreal then uses its native null Slate application and the real RHI;
this driver grants that virtual desktop 4096x4096 before opening the PIE window.
Uses ToolsetRegistry's Blueprint-callable JSON wrapper for native StartPIE;
EditorAppToolset itself and FPIESessionOptions need no Python wrapper.
"""
from pathlib import Path
from datetime import datetime, timezone
import json, re, struct, sys, time, traceback, uuid
import unreal

from jonggu.paths import ROOT, require_active_project
from jonggu.assets import asset_path, generated_class_path
require_active_project()
from jonggu.validation.verify_popups import PopupFixture, button_center, validate_shared_geometry

LINE = unreal.SystemLibrary.get_command_line()
COVERAGE_MODE = "-adaptivecoverageonly" in LINE.lower()
ADAPTIVE_MODE = "-jongguadaptivehudqa" in LINE.lower() or COVERAGE_MODE
HUD_MODE = "-jongguhudqa" in LINE.lower() or ADAPTIVE_MODE
OFFSCREEN = "-renderoffscreen" in LINE.lower()
ORIGINAL_SYSTEM_RESOLUTION = unreal.SystemLibrary.get_console_variable_string_value("r.SetRes") if OFFSCREEN else None
if not HUD_MODE and "-JongguPopupQA" not in LINE and "-JongguPrototypeQA" not in LINE:
    raise RuntimeError("Popup capture requires a fresh marked QA editor")
match = re.search(r"-PopupSizes=([0-9xX,]+)", LINE)
SIZES = [tuple(int(n) for n in item.lower().split("x")) for item in
         (match.group(1) if match else "1280x720,1920x1080,1189x862").split(",")]
if any(w < 320 or h < 240 or w > 4096 or h > 4096 for w, h in SIZES):
    raise ValueError("Popup capture dimensions are outside the bounded QA range")
RUN_ID = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S_%fZ")
QA_ROOT = ROOT / ("Saved/AdaptiveHUDQA" if ADAPTIVE_MODE else "Saved/MainHUDQA" if HUD_MODE else "Saved/PopupDesignQA")
OUTPUT = QA_ROOT / ("Coverage" if COVERAGE_MODE else "After") / RUN_ID
OUTPUT.mkdir(parents=True, exist_ok=True)
# Floating PIE SaveConfig persists its window dimensions when each session stops.
# Preserve exact original bytes for the launching process to restore after exit;
# in-process CDO restoration below cannot guarantee a later config-cache flush.
PREFERENCES = ROOT / "Saved/Config/WindowsEditor/EditorPerProjectUserSettings.ini"
PREFERENCES_BACKUP = OUTPUT / "EditorPerProjectUserSettings.before.ini"
if PREFERENCES.exists(): PREFERENCES_BACKUP.write_bytes(PREFERENCES.read_bytes())
LEVELS = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
SETTINGS = unreal.get_default_object(unreal.load_class(None, "/Script/UnrealEd.LevelEditorPlaySettings"))
# This class has no Python glue: use native reflected names. Config-only
# LastSize/MultipleInstancePositions/LastExecuted* lack CPF_Edit and cannot be
# read through get_editor_property. The five editable settings below suffice;
# primary floating PIE always derives its size from NewWindowWidth/Height.
SETTING_NAMES = ["NewWindowWidth", "NewWindowHeight", "NewWindowPosition", "CenterNewWindow",
                 "GameGetsMouseControl"]
ORIGINAL_SETTINGS = {name: SETTINGS.get_editor_property(name) for name in SETTING_NAMES}
SESSION_CDO = unreal.get_default_object(unreal.load_asset(asset_path("BP_RestaurantGameInstance")).generated_class())
ORIGINAL_SLOT = SESSION_CDO.get_editor_property("save_slot")
SLOT = ("JongguAdaptiveHUDCapture_" if ADAPTIVE_MODE else "JongguHUDCapture_" if HUD_MODE else "JongguPopupCapture_") + uuid.uuid4().hex
TOOLSET_CLASS = unreal.load_class(None, "/Script/EditorToolset.EditorAppToolset")
TOOLSET_SCHEMA = json.loads(unreal.ToolsetRegistry.get_toolset_json_schema(TOOLSET_CLASS))
TOOLSET_NAME = TOOLSET_SCHEMA["name"]
(OUTPUT / "native_start_pie_schema.json").write_text(json.dumps(TOOLSET_SCHEMA, ensure_ascii=False, indent=2), encoding="utf8")
REPORT = {"success": False, "capture_mode": "adaptive_coverage" if COVERAGE_MODE else "adaptive_hud" if ADAPTIVE_MODE else "main_hud" if HUD_MODE else "popup", "generated_at_utc": datetime.now(timezone.utc).isoformat(),
          "requested_sizes": SIZES, "captures": [], "checks": [], "viewport_calibration": [], "max_size_attempts": 3,
          "geometry": validate_shared_geometry(),
          "evidence": "Actual Unreal PIE screenshots; no resized images or preview mockups",
          "native_offscreen_rendering": OFFSCREEN,
          "virtual_desktop": [4096, 4096] if OFFSCREEN else None,
          "virtual_desktop_scope": "reset before each StartPIE; native PIE changes it again after constructing its window",
          "virtual_desktop_resets": [],
          "production_checkpoint_untouched": True, "output_directory": str(OUTPUT),
          "editor_preferences_file": str(PREFERENCES),
          "editor_preferences_backup": str(PREFERENCES_BACKUP) if PREFERENCES_BACKUP.exists() else None,
          "editor_preferences_originally_existed": PREFERENCES_BACKUP.exists(),
          "editor_preferences_restore": "Restore the recorded original file after this QA editor exits; floating PIE writes its settings on stop"}
STATE = {"stage": "offscreen_setup" if OFFSCREEN else "start", "size_index": 0, "case_index": 0, "wait": 0,
         "started": time.monotonic(), "fixture": None, "busy": False,
         "configured_sizes": list(SIZES), "size_attempts": [0] * len(SIZES)}
HANDLE = None
CASES = ["00_hud", "01_menu_selected", "02_pan", "03_pot", "04_pause", "05_summary",
         "06_pan_hover", "07_pan_pressed", "08_pot_no_clams", "09_pot_busy", "10_pot_leftovers"]
if HUD_MODE:
    from jonggu.validation.verify_main_hud import HUD_CASES, stage_main_hud, main_hud_metadata, validate_main_hud_runtime
    CASES = HUD_CASES
if ADAPTIVE_MODE:
    from jonggu.validation.verify_adaptive_hud import (ADAPTIVE_CASES, stage_adaptive_hud, adaptive_metadata,
        validate_adaptive_runtime, sample_frame, click_faded_inventory)
    CASES = ADAPTIVE_CASES
if COVERAGE_MODE:
    from jonggu.validation.verify_adaptive_hud import COVERAGE_CASES, stage_coverage_hud, coverage_metadata
    CASES = COVERAGE_CASES


def active_cases():
    # Keep a single showcase image while testing all eight groups at every size.
    return CASES[1:] if COVERAGE_MODE and STATE["size_index"] > 0 else CASES


def write():
    REPORT["stage"] = STATE["stage"]
    text = json.dumps(REPORT, ensure_ascii=False, indent=2)
    (OUTPUT / "capture_report.json").write_text(text, encoding="utf8")
    (QA_ROOT / ("latest_coverage_report.json" if COVERAGE_MODE else "latest_capture_report.json")).write_text(text, encoding="utf8")


def stage(name, delay=0.15):
    STATE["stage"], STATE["wait"] = name, time.monotonic() + delay
    write()


def current():
    world = unreal.EditorLevelLibrary.get_game_world()
    manager = unreal.GameplayStatics.get_actor_of_class(world, unreal.load_class(None,
        generated_class_path("BP_RestaurantManager"))) if world else None
    return world, manager


def attach_fixture(manager):
    pc = unreal.GameplayStatics.get_player_controller(manager, 0)
    STATE["fixture"] = PopupFixture(manager)
    STATE["controller_tick"], STATE["manager_tick"] = pc.is_actor_tick_enabled(), manager.is_actor_tick_enabled()
    # Hold scripted input/simulation state while HUD DrawHUD continues to render.
    pc.set_actor_tick_enabled(False)
    manager.set_actor_tick_enabled(False)
    pc.set_mouse_location(0, 0)


def release_fixture():
    fixture = STATE.get("fixture")
    if fixture:
        fixture.restore()
        fixture.pc.set_actor_tick_enabled(STATE["controller_tick"])
        fixture.manager.set_actor_tick_enabled(STATE["manager_tick"])
        STATE["fixture"] = None


def fail(error):
    REPORT["error"] = error
    try: release_fixture()
    except Exception: REPORT["restore_error"] = traceback.format_exc()
    if LEVELS.is_in_play_in_editor(): LEVELS.editor_request_end_play()
    stage("finalize", 0.3)


def finalize():
    global HANDLE
    if LEVELS.is_in_play_in_editor(): return
    for name, value in ORIGINAL_SETTINGS.items(): SETTINGS.set_editor_property(name, value)
    SESSION_CDO.set_editor_property("save_slot", ORIGINAL_SLOT)
    if OFFSCREEN and ORIGINAL_SYSTEM_RESOLUTION:
        unreal.SystemLibrary.execute_console_command(None, "r.SetRes " + ORIGINAL_SYSTEM_RESOLUTION)
        REPORT["restored_system_resolution_in_memory"] = ORIGINAL_SYSTEM_RESOLUTION
    if unreal.GameplayStatics.does_save_game_exist(SLOT, 0): unreal.GameplayStatics.delete_game_in_slot(SLOT, 0)
    expected_count = len(SIZES) * (len(CASES) - 1) + 1 if COVERAGE_MODE else len(SIZES) * len(CASES)
    REPORT["expected_capture_count"] = expected_count
    REPORT["success"] = "error" not in REPORT and len(REPORT["captures"]) == expected_count
    REPORT["elapsed_seconds"] = time.monotonic() - STATE["started"]
    REPORT["restored_editor_settings_in_memory"] = SETTING_NAMES
    STATE["stage"] = "complete" if REPORT["success"] else "failed"
    write()
    if HANDLE: unreal.unregister_slate_post_tick_callback(HANDLE); HANDLE = None
    unreal.SystemLibrary.quit_editor()


def start_size():
    index = STATE["size_index"]
    STATE["size_attempts"][index] += 1
    width, height = STATE["configured_sizes"][index]
    if OFFSCREEN:
        resolution = unreal.SystemLibrary.get_console_variable_string_value("r.SetRes")
        if resolution != "4096x4096w":
            raise RuntimeError("Virtual desktop reset did not reach StartPIE: " + resolution)
    for name, value in (("NewWindowWidth", width), ("NewWindowHeight", height),
                         ("CenterNewWindow", True), ("NewWindowPosition", unreal.IntPoint(-1, -1)),
                         ("GameGetsMouseControl", False)):
        SETTINGS.set_editor_property(name, value)
    SESSION_CDO.set_editor_property("save_slot", SLOT)
    # HUD mode ends each size in the real beach map. Its capture-only checkpoint
    # must not redirect the next size's Hub start back to that beach.
    if HUD_MODE and unreal.GameplayStatics.does_save_game_exist(SLOT, 0):
        if not unreal.GameplayStatics.delete_game_in_slot(SLOT, 0):
            raise RuntimeError("Unable to clear this run's isolated HUD checkpoint")
    if not LEVELS.load_level("/Game/Jonggu/Maps/L_Hub"):
        raise RuntimeError("Cannot load Hub for popup captures")
    # ObjectFunctionToolCall validates required top-level keys case-sensitively
    # against its emitted JSON schema before FJsonObjectConverter runs.
    options = {"options": {"bSimulate": False, "playMode": "PlayMode_InEditorFloating", "warmupSeconds": 0.1}}
    REPORT["native_start_pie_input"] = options
    STATE["start_result"] = unreal.ToolsetRegistry.execute_tool(TOOLSET_NAME, "StartPIE", json.dumps(options))
    stage("ready", 0.5)


def stage_case():
    fixture = STATE["fixture"]
    case = active_cases()[STATE["case_index"]]
    if HUD_MODE:
        if case.endswith("_beach") and not fixture.m("is_beach"):
            manager = fixture.manager
            release_fixture()
            # Restored outer checkpoint still uses this run's unique QA slot.
            # Exercise the actual saved Blueprint travel path into L_Beach.
            if ADAPTIVE_MODE:
                session = manager.get_editor_property("session")
                session.set_editor_property("hud_expanded_mask", 5)
                session.set_editor_property("clams", 0)
            manager.call_method("Travel", (True,))
            stage("beach_ready", 0.5)
            return
        if COVERAGE_MODE:
            capture_delay = stage_coverage_hud(fixture, case)
        elif ADAPTIVE_MODE:
            capture_delay = stage_adaptive_hud(fixture, case)
        else:
            stage_main_hud(fixture, case)
    elif case == "00_hud": fixture.reset()
    elif case.startswith("01_"): fixture.stage(1)
    elif case in ("02_pan", "06_pan_hover", "07_pan_pressed"): fixture.stage(2)
    elif case == "04_pause": fixture.stage(4)
    elif case == "05_summary": fixture.stage(5)
    else:
        fixture.stage(3)
        if case == "08_pot_no_clams": fixture.put(fixture.session, "clams", 0)
        elif case in ("09_pot_busy", "10_pot_leftovers"):
            fixture.call("StartCook", 3)
            fixture.call("Advance", 4.0 if case == "09_pot_busy" else 16.0)
            fixture.call("SetScreen", 3)
    if not ADAPTIVE_MODE:
        fixture.call("UpdatePointer", -1.0, -1.0, False)
    if case in ("06_pan_hover", "07_pan_pressed"):
        fixture.call("UpdatePointer", *button_center(201), case == "07_pan_pressed")
    fixture.call("RefreshHUD")
    width, height = SIZES[STATE["size_index"]]
    path = OUTPUT / (str(width) + "x" + str(height)) / (case + ".png")
    path.parent.mkdir(parents=True, exist_ok=True)
    STATE["capture_path"], STATE["capture_case"] = path, case
    stage("adaptive_click" if ADAPTIVE_MODE and case == "15_inventory_faded_click" else "capture",
          capture_delay if ADAPTIVE_MODE else 0.25)


def tick(_dt):
    if STATE["busy"]: return
    STATE["busy"] = True
    try:
        if ADAPTIVE_MODE and STATE.get("fixture"):
            sample_frame(STATE["fixture"])
        if time.monotonic() - STATE["started"] > (600 if ADAPTIVE_MODE else 300):
            raise TimeoutError("Popup capture timeout at " + STATE["stage"])
        if time.monotonic() < STATE["wait"]: return
        state = STATE["stage"]
        if state == "finalize": finalize(); return
        if state == "offscreen_setup":
            # On Windows, -RenderOffscreen creates FNullApplication. Its
            # OnSystemResolutionChanged listener updates virtual display metrics,
            # bypassing the physical work-area clamp in SWindow construction.
            # PlayLevel.cpp resets r.SetRes to WindowSize after every PIE window
            # construction, so this stage must precede every start and retry.
            previous = unreal.SystemLibrary.get_console_variable_string_value("r.SetRes")
            unreal.SystemLibrary.execute_console_command(None, "r.SetRes 4096x4096w")
            REPORT["virtual_desktop_resets"].append({"size_index": STATE["size_index"],
                "next_attempt": STATE["size_attempts"][STATE["size_index"]] + 1,
                "previous": previous, "requested": "4096x4096w"})
            stage("start", 0.3)
            return
        if state == "start": start_size(); return
        if state == "calibration_stopped":
            if LEVELS.is_in_play_in_editor(): return
            stage("offscreen_setup" if OFFSCREEN else "start")
            return
        if state == "stopped":
            if LEVELS.is_in_play_in_editor(): return
            STATE["size_index"] += 1
            STATE["case_index"] = 0
            if STATE["size_index"] < len(SIZES):
                stage("offscreen_setup" if OFFSCREEN else "start")
            else:
                stage("finalize")
            return
        if state == "ready":
            result = STATE["start_result"]
            if result.get_editor_property("is_complete") and result.get_editor_property("error"):
                raise RuntimeError("Native StartPIE failed: " + str(result.get_editor_property("error")))
            if not LEVELS.is_in_play_in_editor(): return
            world, manager = current()
            if not manager or not manager.get_editor_property("carried"): return
            pc = unreal.GameplayStatics.get_player_controller(world, 0)
            actual = tuple(int(v) for v in pc.get_viewport_size())
            index = STATE["size_index"]
            requested = SIZES[index]
            configured = STATE["configured_sizes"][index]
            attempt = STATE["size_attempts"][index]
            observation = {"requested_viewport": list(requested), "configured_window": list(configured),
                           "actual_viewport": list(actual), "attempt": attempt, "matched": actual == requested}
            REPORT["viewport_calibration"].append(observation)
            if actual != requested:
                if attempt >= REPORT["max_size_attempts"]:
                    raise RuntimeError("Native viewport calibration exhausted " + str(attempt)
                                       + " attempts for " + str(requested) + "; actual=" + str(actual))
                corrected = tuple(configured[axis] + requested[axis] - actual[axis] for axis in (0, 1))
                if any(value < 1 or value > 8192 for value in corrected):
                    raise RuntimeError("Native viewport correction outside bounded range: " + str(corrected))
                observation["next_configured_window"] = list(corrected)
                STATE["configured_sizes"][index] = corrected
                # Recreate a native PIE window. No screenshot is taken until the
                # measured viewport matches the requested dimensions exactly.
                LEVELS.editor_request_end_play()
                stage("calibration_stopped", 0.4)
                return
            REPORT["checks"].append("actual floating PIE viewport matches " + str(actual))
            if ADAPTIVE_MODE:
                mask = manager.get_editor_property("session").get_editor_property("hud_expanded_mask")
                if mask != 0:
                    raise AssertionError("New PIE must begin with compact session HUD, observed=" + str(mask))
                REPORT["checks"].append("fresh PIE fold mask is zero at " + str(actual))
            attach_fixture(manager)
            if HUD_MODE and not COVERAGE_MODE and "hud_runtime" not in REPORT:
                REPORT["hud_runtime"] = validate_main_hud_runtime(STATE["fixture"])
            if ADAPTIVE_MODE and not COVERAGE_MODE and "adaptive_runtime" not in REPORT:
                REPORT["adaptive_runtime"] = validate_adaptive_runtime(STATE["fixture"])
            stage_case(); return
        if state == "beach_ready":
            if not LEVELS.is_in_play_in_editor(): return
            world, manager = current()
            if not manager or not manager.get_editor_property("carried") or not manager.get_editor_property("is_beach"):
                return
            actual = tuple(int(v) for v in unreal.GameplayStatics.get_player_controller(world, 0).get_viewport_size())
            if actual != SIZES[STATE["size_index"]]:
                raise RuntimeError("Native beach travel changed capture viewport: " + str(actual))
            REPORT["checks"].append("actual Blueprint Travel reached beach at " + str(actual))
            attach_fixture(manager)
            stage_case(); return
        if state == "case": stage_case(); return
        if state == "adaptive_click":
            click_faded_inventory(STATE["fixture"])
            stage("capture", 0.5)
            return
        if state == "capture":
            world, manager = current()
            fixture = STATE["fixture"]
            path = STATE["capture_path"]
            fixture.call("RefreshHUD")
            STATE["capture_metadata"] = {
                "case": STATE["capture_case"], "screen_id": fixture.m("screen_id"),
                "viewport": list(fixture.pc.get_viewport_size()),
                "configured_window": list(STATE["configured_sizes"][STATE["size_index"]]),
                "size_attempt": STATE["size_attempts"][STATE["size_index"]],
                "hud_canvas": [fixture.h("draw_size_x"), fixture.h("draw_size_y")],
                "hud_scale": [fixture.h("ui_scale_x"), fixture.h("ui_scale_y")],
                "hovered_action": fixture.h("hovered_action"), "pressed_action": fixture.h("pressed_action"),
                "pan_remaining": fixture.m("pan_remaining"), "pot_remaining": fixture.m("pot_remaining"),
                "pan_portions": fixture.m("pan_portions"), "pot_portions": fixture.m("pot_portions"),
                "clams": fixture.s("clams"), "file": str(path),
                "simulation_frozen_for_repeatable_capture": True,
            }
            if COVERAGE_MODE:
                STATE["capture_metadata"].update(coverage_metadata(fixture, STATE["capture_case"]))
            elif ADAPTIVE_MODE:
                STATE["capture_metadata"].update(adaptive_metadata(fixture, STATE["capture_case"]))
            elif HUD_MODE:
                STATE["capture_metadata"].update(main_hud_metadata(fixture))
            unreal.SystemLibrary.execute_console_command(world,
                'Shot -nosuffix filename="' + str(path).replace('\\', '/') + '"', fixture.pc)
            stage("image", 0.15); return
        if state == "image":
            path = STATE["capture_path"]
            if not path.exists() or path.stat().st_size < 24: return
            header = path.read_bytes()[:24]
            if header[:8] != b'\x89PNG\r\n\x1a\n': raise RuntimeError("Engine capture is not PNG")
            actual = struct.unpack(">II", header[16:24])
            requested = SIZES[STATE["size_index"]]
            if actual != requested: raise RuntimeError("PNG dimensions differ from actual requested viewport: " + str(actual))
            metadata = STATE["capture_metadata"]
            metadata["png_size"] = list(actual)
            REPORT["captures"].append(metadata)
            STATE["case_index"] += 1
            if STATE["case_index"] < len(active_cases()): stage("case")
            else:
                release_fixture()
                LEVELS.editor_request_end_play()
                stage("stopped", 0.4)
    except Exception:
        if STATE["stage"] == "finalize":
            REPORT["finalize_error"] = traceback.format_exc(); write()
            if HANDLE: unreal.unregister_slate_post_tick_callback(HANDLE)
            unreal.SystemLibrary.quit_editor()
        else: fail(traceback.format_exc())
    finally:
        STATE["busy"] = False


unreal.EditorPythonScripting.set_keep_python_script_alive(True)
HANDLE = unreal.register_slate_post_tick_callback(tick)
write()
