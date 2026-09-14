"""Author the prototype GameInstance and SaveGame as compiled Blueprints.

This module executes in the Unreal editor only. All authored save, load, harvest,
checkpoint and event-log behavior executes through native/Blueprint runtime nodes.
The single checkpoint is deliberately not updated during restaurant service.
"""
from __future__ import annotations

import unreal

from bp_helpers import (
    B, ASSETS, G, ensure_bp, add_vars, declare_function, compile_bp, save_bp,
)

ROOT = "/Game/Jonggu/Prototype"
SESSION_PATH = ROOT + "/BP_PrototypeSession"
SAVE_PATH = ROOT + "/BP_PrototypeSave"
SESSION_CLASS = SESSION_PATH + ".BP_PrototypeSession_C"
SAVE_CLASS = SAVE_PATH + ".BP_PrototypeSave_C"
GAMEPLAY = "/Script/Engine.GameplayStatics."
SYSTEM = "/Script/Engine.KismetSystemLibrary."
STRING = "/Script/Engine.KismetStringLibrary."
SLOT = "JongguPrototypeCheckpoint"
SCHEMA_VERSION = 1

# SaveGameToSlot serializes all non-transient fields; the SaveGame flag is not
# required by this engine API. SaveGame contains no world objects or live orders.
PERSISTED_FIELDS = [
    ("day", "int", 1),
    ("clams", "int", 0),
    ("harvested_mask", "int", 0),
    ("menu_mask", "int", 3),
    ("guest_target", "int", 4),
    ("total_revenue", "int", 0),
    ("checkpoint_phase", "int", 0),
    ("checkpoint_map", "string", "L_Hub"),
    ("entry_marker", "string", "HubArrival"),
    ("summary_served", "int", 0),
    ("summary_base", "int", 0),
    ("summary_bonus", "int", 0),
    ("summary_clams_used", "int", 0),
    ("summary_special_served", "int", 0),
]
SESSION_ONLY_FIELDS = [
    # Presentation preference lives only in the running GameInstance.
    ("hud_expanded_mask", "int", 0),
    ("initialized", "bool", False),
    ("save_ok", "bool", True),
    ("save_slot", "string", SLOT),
    ("event_log", "string", ""),
    ("event_count", "int", 0),
]
FUNCTIONS = {
    "InitializeSession": ({}, {}),
    "SaveCheckpoint": ({}, {"success": "bool"}),
    "LoadCheckpoint": ({}, {"success": "bool"}),
    "CollectClam": ({"point_bit": "int"}, {"success": "bool"}),
    "BeginNextDay": ({}, {}),
    "LogEvent": ({"event_text": "string"}, {}),
}


def _self_call(g, name, **inputs):
    return g.call(SESSION_CLASS + "." + name, **inputs)


def _int_string(g, value):
    return g.pure(STRING + "Conv_IntToString", InInt=value)


def _valid(g, value):
    return g.pure(SYSTEM + "IsValid", Object=value)


def _checkpoint_cast(g, obj):
    snapshot = g.cast(obj, SAVE_CLASS)
    # The shared helper emits an impure cast. Both outcomes must reach the
    # validity branch so a missing/wrong-class save still logs and returns false.
    failed = snapshot.get_owning_node().find_output_pin("CastFailed")
    if failed.is_valid():
        g.tails.append(failed)
    return snapshot


def _log(g, text):
    return _self_call(g, "LogEvent", event_text=text)


def _build_log(bp):
    g = G(bp, "LogEvent")
    g.set("event_count", g.math("Add_IntInt", A=g.get("event_count"), B=1))
    line = g.join(
        "[JongguPrototype] #", _int_string(g, g.get("event_count")),
        " day=", _int_string(g, g.get("day")),
        " clams=", _int_string(g, g.get("clams")),
        " revenue=", _int_string(g, g.get("total_revenue")),
        " menu_mask=", _int_string(g, g.get("menu_mask")),
        " guests=", _int_string(g, g.get("guest_target")),
        " event=", g.param("event_text"),
    )
    # Keep a session audit independent of checkpoint rollback. PrintString's log
    # output is also written to Saved/Logs/projectJ.log for durable QA export.
    g.set("event_log", g.join(g.get("event_log"), line, "\n"))
    g.call(SYSTEM + "PrintString", InString=line, bPrintToScreen=False,
           bPrintToLog=True)
    g.ret()


def _build_save(bp):
    g = G(bp, "SaveCheckpoint")
    g.set("save_ok", False)
    created = g.call(GAMEPLAY + "CreateSaveGameObject", SaveGameClass=SAVE_CLASS)
    snapshot = _checkpoint_cast(g, created.find_output_pin("ReturnValue"))

    def save_valid(_):
        g.set("schema_version", SCHEMA_VERSION, obj=snapshot, cls=SAVE_CLASS)
        for name, _, _ in PERSISTED_FIELDS:
            g.set(name, g.get(name), obj=snapshot, cls=SAVE_CLASS)
        write = g.call(GAMEPLAY + "SaveGameToSlot", SaveGameObject=snapshot,
                       SlotName=g.get("save_slot"), UserIndex=0)
        g.set("save_ok", write.find_output_pin("ReturnValue"))

    g.when(_valid(g, snapshot), save_valid)

    def save_failed(_):
        _log(g, "checkpoint_save_failed")
        g.call(SYSTEM + "PrintString",
               InString="저장하지 못했습니다. 현재 플레이는 유지됩니다.",
               bPrintToScreen=True, bPrintToLog=True, Duration=8.0)

    g.when(g.get("save_ok"), lambda _: _log(g, "checkpoint_saved"), save_failed)
    g.ret(success=g.get("save_ok"))


def _build_load(bp):
    g = G(bp, "LoadCheckpoint")
    g.set("save_ok", False)
    loaded = g.call(GAMEPLAY + "LoadGameFromSlot", SlotName=g.get("save_slot"), UserIndex=0)
    snapshot = _checkpoint_cast(g, loaded.find_output_pin("ReturnValue"))

    def loaded_valid(_):
        version_ok = g.math("EqualEqual_IntInt",
                            A=g.get("schema_version", obj=snapshot, cls=SAVE_CLASS),
                            B=SCHEMA_VERSION)

        def restore(_):
            for name, _, _ in PERSISTED_FIELDS:
                g.set(name, g.get(name, obj=snapshot, cls=SAVE_CLASS))
            g.set("save_ok", True)

        g.when(version_ok, restore)

    g.when(_valid(g, snapshot), loaded_valid)

    def load_failed(_):
        _log(g, "checkpoint_load_failed_state_preserved")
        g.call(SYSTEM + "PrintString",
               InString="저장을 불러오지 못했습니다. 현재 상태를 유지합니다.",
               bPrintToScreen=True, bPrintToLog=True, Duration=8.0)

    g.when(g.get("save_ok"), lambda _: _log(g, "checkpoint_loaded"), load_failed)
    g.ret(success=g.get("save_ok"))


def _build_initialize(bp):
    g = G(bp, "InitializeSession")

    def initialize(_):
        # Set first so manager BeginPlay and a future Init event can both call
        # this entry point safely without a second load after map travel.
        g.set("initialized", True)
        exists = g.call(GAMEPLAY + "DoesSaveGameExist", SlotName=g.get("save_slot"), UserIndex=0)
        g.when(exists.find_output_pin("ReturnValue"),
               lambda _: _self_call(g, "LoadCheckpoint"),
               lambda _: _self_call(g, "SaveCheckpoint"))
        _log(g, "session_initialized")

    g.when(g.math("Not_PreBool", A=g.get("initialized")), initialize)
    g.ret()


def _build_collect(bp):
    g = G(bp, "CollectClam")
    # Only the three authored IDs are accepted. Calling this twice in one frame
    # cannot grant the same clam twice because the mask is set synchronously.
    point = g.param("point_bit")
    is_one = g.math("EqualEqual_IntInt", A=point, B=1)
    is_two = g.math("EqualEqual_IntInt", A=point, B=2)
    is_four = g.math("EqualEqual_IntInt", A=point, B=4)
    point_valid = g.math("BooleanOR", A=g.math("BooleanOR", A=is_one, B=is_two), B=is_four)
    not_harvested = g.math("EqualEqual_IntInt",
                           A=g.math("And_IntInt", A=g.get("harvested_mask"), B=point), B=0)
    valid = g.math("BooleanAND", A=point_valid, B=not_harvested)

    def collect(_):
        g.set("harvested_mask", g.math("Or_IntInt", A=g.get("harvested_mask"), B=point))
        g.set("clams", g.math("Add_IntInt", A=g.get("clams"), B=1))
        _log(g, g.join("clam_collected point=", _int_string(g, point)))
        g.ret(success=True)

    g.when(valid, collect)
    g.ret(success=False)


def _build_next_day(bp):
    g = G(bp, "BeginNextDay")
    # Summary is the only valid source: duplicate button input cannot advance
    # multiple days or regenerate beach nodes before settlement.
    def advance(_):
        g.set("checkpoint_phase", 0)
        g.set("day", g.math("Add_IntInt", A=g.get("day"), B=1))
        g.set("harvested_mask", 0)
        g.set("checkpoint_map", "L_Hub")
        g.set("entry_marker", "HubArrival")
        for name, _, _ in PERSISTED_FIELDS:
            if name.startswith("summary_"):
                g.set(name, 0)
        _log(g, "next_day_preparation")
        _self_call(g, "SaveCheckpoint")

    g.when(g.math("EqualEqual_IntInt", A=g.get("checkpoint_phase"), B=2), advance)
    g.ret()


def build_session():
    """Create/rebuild prototype-owned assets; does not touch maps or config."""
    save = ensure_bp("BP_PrototypeSave", unreal.SaveGame)
    add_vars(save, [("schema_version", "int", SCHEMA_VERSION)] + PERSISTED_FIELDS)
    compile_bp(save)
    save_bp(save)

    session = ensure_bp("BP_PrototypeSession", unreal.GameInstance)
    add_vars(session, PERSISTED_FIELDS + SESSION_ONLY_FIELDS)
    for name, (inputs, outputs) in FUNCTIONS.items():
        declare_function(session, name, inputs, outputs)
    compile_bp(session)
    for build in (_build_log, _build_save, _build_load, _build_initialize,
                  _build_collect, _build_next_day):
        build(session)
    init_event = G.event(session, "ReceiveInit")
    _self_call(init_event, "InitializeSession")
    compile_bp(session)
    ASSETS.set_metadata_tag(session, "PrototypeRuntime", "Compiled Blueprint only")
    ASSETS.set_metadata_tag(session, "PrototypeSaveSchema", str(SCHEMA_VERSION))
    save_bp(session)
    return session, save



