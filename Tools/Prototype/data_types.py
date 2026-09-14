"""Typed prototype data assets and the authored recipe balance table.

Unreal's installed BlueprintEditorLibrary exposes class/variable authoring but
not FStructureEditorUtils mutation. These Blueprint UObject types provide the
same explicit dish/order/recipe contracts without adding a C++ runtime module.
Recipe class default objects are immutable data references usable directly by
manager Blueprint variables; no runtime object construction is necessary.
"""
from __future__ import annotations

import unreal
from bp_helpers import B, ASSETS, ensure_bp, add_vars, compile_bp, save_bp

PREFIX = "/Game/Jonggu/Prototype"
RECIPE_CLASS = PREFIX + "/BP_RecipeDefinition.BP_RecipeDefinition_C"
ORDER_CLASS = PREFIX + "/BP_PrototypeOrder.BP_PrototypeOrder_C"
DISH_CLASS = PREFIX + "/BP_PrototypeDish.BP_PrototypeDish_C"

# dish_id corresponds to the menu bit (1 << dish_id).
# recipe_id 3 is a cooking variant of dish 1, never a fourth customer order.
# Modify balance here and rerun build_prototype.py to regenerate the data assets.
RECIPES = [
    {"asset": "BP_RecipeRice", "recipe_id": 0, "dish_id": 0,
     "display_name": "김치볶음밥", "appliance": 0,
     "cook_seconds": 8.0, "portions": 1, "clam_cost": 0},
    {"asset": "BP_RecipeSoup", "recipe_id": 1, "dish_id": 1,
     "display_name": "김치찌개", "appliance": 1,
     "cook_seconds": 16.0, "portions": 1, "clam_cost": 0},
    {"asset": "BP_RecipePancake", "recipe_id": 2, "dish_id": 2,
     "display_name": "김치전", "appliance": 0,
     "cook_seconds": 12.0, "portions": 1, "clam_cost": 0},
    {"asset": "BP_RecipeSpecialSoup", "recipe_id": 3, "dish_id": 1,
     "display_name": "조개 특선 찌개", "appliance": 1,
     "cook_seconds": 16.0, "portions": 2, "clam_cost": 1},
]

RECIPE_FIELDS = [
    ("recipe_id", "int", 0), ("dish_id", "int", 0),
    ("display_name", "string", ""), ("appliance", "int", 0),
    ("cook_seconds", "real", 8.0), ("portions", "int", 1),
    ("clam_cost", "int", 0),
]
ORDER_FIELDS = [
    ("dish_id", "int", -1), ("placed_at", "real", 0.0),
    ("active", "bool", False), ("customer_index", "int", 0),
]
DISH_FIELDS = [("dish_id", "int", -1), ("is_special", "bool", False)]


def build_data_types():
    """Return classes and immutable recipe defaults for manager wiring."""
    typed = {}
    for name, fields in (("BP_RecipeDefinition", RECIPE_FIELDS),
                         ("BP_PrototypeOrder", ORDER_FIELDS),
                         ("BP_PrototypeDish", DISH_FIELDS)):
        bp = ensure_bp(name, unreal.Object)
        add_vars(bp, fields)
        for field, _, _ in fields:
            B.set_blueprint_variable_instance_editable(bp, field, True)
            B.set_blueprint_variable_category(bp, field, "Prototype data")
        compile_bp(bp)
        save_bp(bp)
        typed[name] = bp
    recipes = []
    for definition in RECIPES:
        bp = ensure_bp(definition["asset"], typed["BP_RecipeDefinition"].generated_class())
        compile_bp(bp)
        defaults = unreal.get_default_object(bp.generated_class())
        for field, _, _ in RECIPE_FIELDS:
            defaults.set_editor_property(field, definition[field])
        ASSETS.set_metadata_tag(bp, "PrototypeData", "Authored recipe; runtime read only")
        save_bp(bp)
        recipes.append(unreal.get_default_object(bp.generated_class()))
    typed["recipes"] = recipes
    return typed

