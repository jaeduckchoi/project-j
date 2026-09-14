"""Invoke the compiled session Blueprints against an isolated QA save slot.

Run only from Unreal editor authoring/QA scripts. No player checkpoint, map,
project config or content asset is modified by these tests.
"""
from __future__ import annotations

import uuid
import unreal
from jonggu.assets import generated_class_path


def validate_session_runtime(session):
    """Validate real Blueprint execution, returning a compact inspectable report."""
    instance = unreal.new_object(session.generated_class())
    slot = "JongguPrototypeQA_" + uuid.uuid4().hex
    instance.set_editor_property("save_slot", slot)
    checks = []
    extra_slots = []

    def get(name):
        return instance.get_editor_property(name)

    def set_value(name, value):
        instance.set_editor_property(name, value)

    def call(name, *args):
        return instance.call_method(name, args)

    def check(label, passed):
        if not passed:
            raise AssertionError("Prototype session QA: " + label)
        checks.append(label)

    try:
        call("InitializeSession")
        check("missing slot creates a prep checkpoint", get("initialized") and get("save_ok")
              and unreal.GameplayStatics.does_save_game_exist(slot, 0)
              and get("day") == 1 and get("checkpoint_phase") == 0)
        check("first harvest grants exactly one clam", call("CollectClam", 1)
              and get("clams") == 1 and get("harvested_mask") == 1)
        check("duplicate harvest does not grant another clam", not call("CollectClam", 1)
              and get("clams") == 1 and get("harvested_mask") == 1)
        check("non-authored harvest ID is rejected", not call("CollectClam", 3)
              and get("clams") == 1 and get("harvested_mask") == 1)
        check("checkpoint save reports success", call("SaveCheckpoint") and get("save_ok"))
        audit_before = get("event_count")
        set_value("clams", 0)
        set_value("total_revenue", 123)
        set_value("menu_mask", 4)
        set_value("guest_target", 6)
        check("load reports success", call("LoadCheckpoint") and get("save_ok"))
        check("retry restores inventory money menu and guest count together",
              get("clams") == 1 and get("total_revenue") == 0
              and get("menu_mask") == 3 and get("guest_target") == 4)
        check("load preserves harvest mask and running audit", get("harvested_mask") == 1
              and get("event_count") > audit_before
              and "checkpoint_loaded" in get("event_log"))
        # Reinitialization during OpenLevel must never overwrite current GI state.
        set_value("clams", 5)
        call("InitializeSession")
        check("repeated initialization does not reload checkpoint", get("clams") == 5)
        set_value("checkpoint_phase", 2)
        for name in ("summary_served", "summary_base", "summary_bonus",
                     "summary_clams_used", "summary_special_served"):
            set_value(name, 7)
        call("BeginNextDay")
        check("next day keeps raw clams and resets harvest and summary",
              get("day") == 2 and get("clams") == 5 and get("harvested_mask") == 0
              and get("checkpoint_phase") == 0 and get("summary_served") == 0
              and get("summary_base") == 0 and get("summary_bonus") == 0
              and get("summary_clams_used") == 0 and get("summary_special_served") == 0
              and get("checkpoint_map") == "L_Hub" and get("save_ok"))
        call("BeginNextDay")
        check("duplicate next-day action advances once", get("day") == 2)
        check("harvest resets next day", call("CollectClam", 1)
              and get("clams") == 6 and get("harvested_mask") == 1)
        set_value("save_slot", slot + "_missing")
        check("failed load preserves current state", not call("LoadCheckpoint")
              and not get("save_ok") and get("day") == 2 and get("clams") == 6)
        check("missing save reaches the failure-notification branch",
              "checkpoint_load_failed_state_preserved" in get("event_log"))
        check("audit records selected menu and guest count",
              "menu_mask=3 guests=4" in get("event_log"))
        # Native Enhanced Input settings are a concrete SaveGame subclass already
        # available in UE. No input subsystem or user settings instance is used.
        wrong_class = unreal.load_class(None, "/Script/EnhancedInput.EnhancedInputUserSettings")
        if wrong_class is None:
            raise RuntimeError("Native wrong-class QA fixture is unavailable")
        wrong_save = unreal.GameplayStatics.create_save_game_object(wrong_class)
        wrong_slot = slot + "_wrong_class"
        extra_slots.append(wrong_slot)
        check("wrong-class QA fixture saves", unreal.GameplayStatics.save_game_to_slot(wrong_save, wrong_slot, 0))
        set_value("save_slot", wrong_slot)
        failures = get("event_log").count("checkpoint_load_failed_state_preserved")
        check("wrong-class checkpoint preserves state and reports failure",
              not call("LoadCheckpoint") and not get("save_ok")
              and get("day") == 2 and get("clams") == 6
              and get("event_log").count("checkpoint_load_failed_state_preserved") == failures + 1)
        # A well-formed save with an unsupported schema takes the validation
        # branch rather than the cast-failure branch, and must behave identically.
        schema_class = unreal.load_class(None, generated_class_path("BP_RestaurantSaveGame"))
        schema_save = unreal.GameplayStatics.create_save_game_object(schema_class)
        schema_save.set_editor_property("schema_version", 999)
        schema_slot = slot + "_unsupported_schema"
        extra_slots.append(schema_slot)
        check("unsupported-schema QA fixture saves", unreal.GameplayStatics.save_game_to_slot(schema_save, schema_slot, 0))
        set_value("save_slot", schema_slot)
        failures = get("event_log").count("checkpoint_load_failed_state_preserved")
        check("unsupported schema preserves state and reports failure",
              not call("LoadCheckpoint") and not get("save_ok")
              and get("day") == 2 and get("clams") == 6
              and get("event_log").count("checkpoint_load_failed_state_preserved") == failures + 1)
        return {"success": True, "checks": checks, "slot": slot,
                "production_checkpoint_untouched": True,
                "implementation": "Executed compiled Blueprint functions"}
    finally:
        # UUID-scoped slot belongs only to this test run; never delete production.
        for qa_slot in [slot] + extra_slots:
            if unreal.GameplayStatics.does_save_game_exist(qa_slot, 0):
                if not unreal.GameplayStatics.delete_game_in_slot(qa_slot, 0):
                    unreal.log_warning("Unable to remove isolated QA slot: " + qa_slot)


