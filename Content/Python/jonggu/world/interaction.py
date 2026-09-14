"""Reusable interaction-marker Blueprint and its readable world label material."""
from jonggu.assets import asset_path


def _marker_text_material():
    import unreal
    from jonggu.blueprints.graph import ASSETS
    path = asset_path('M_InteractionText')
    version = '1_unlit_vertex_color'
    material = ASSETS.load_asset(path) if ASSETS.does_asset_exist(path) else None
    if material is not None and ASSETS.get_metadata_tag(material, 'PrototypeTextVersion') == version:
        return material
    if material is None:
        material = ASSETS.duplicate_asset('/Engine/EngineMaterials/DefaultTextMaterialTranslucent', path)
    if not isinstance(material, unreal.Material):
        raise RuntimeError('Cannot create project-owned marker text material')
    edit = unreal.MaterialEditingLibrary
    base_color = unreal.MaterialProperty.MP_BASE_COLOR
    color_node = edit.get_material_property_input_node(material, base_color)
    if color_node is None: raise RuntimeError('Text material base-color input is missing')
    output_name = edit.get_material_property_input_node_output_name(material, base_color)
    if not edit.connect_material_property(color_node, output_name, unreal.MaterialProperty.MP_EMISSIVE_COLOR):
        raise RuntimeError('Cannot connect marker text color to emissive')
    material.set_editor_property('shading_model', unreal.MaterialShadingModel.MSM_UNLIT)
    errors = edit.recompile_material(material)
    if errors: raise RuntimeError('Marker text material compile failed: ' + '; '.join(str(e) for e in errors))
    ASSETS.set_metadata_tag(material, 'PrototypeTextVersion', version)
    if not ASSETS.save_loaded_asset(material, only_if_is_dirty=False):
        raise RuntimeError('Cannot save marker text material')
    return material


def configure_interaction_label(label, text='E', gather=False, relative_y=8.0):
    import unreal
    # TextRender requires an OFFLINE font atlas. Its native opaque material
    # ignores translucent sort priority and can be covered by the source sprites.
    font = unreal.load_asset('/Engine/EngineFonts/RobotoDistanceField')
    material = _marker_text_material()
    if font is None or material is None: raise RuntimeError('Marker font/material is missing')
    label.set_mobility(unreal.ComponentMobility.MOVABLE)
    label.set_collision_enabled(unreal.CollisionEnabled.NO_COLLISION)
    label.set_cast_shadow(False)
    label.set_visibility(True, True)
    label.set_hidden_in_game(False, True)
    label.set_font(font)
    label.set_text_material(material)
    label.set_text(text)
    label.set_world_size(80.0)
    label.set_horizontal_alignment(unreal.HorizTextAligment.EHTA_CENTER)
    label.set_vertical_alignment(unreal.VerticalTextAligment.EVRTA_TEXT_CENTER)
    # Native TextRender local +X normal faces camera +Y after +90 yaw.
    label.set_relative_location(unreal.Vector(0.0, relative_y, 28.0), False, False)
    label.set_relative_rotation(unreal.Rotator(pitch=0.0, yaw=90.0, roll=0.0), False, False)
    label.set_text_render_color(unreal.Color(166, 239, 225, 255) if gather else unreal.Color(255, 226, 160, 255))
    label.set_translucent_sort_priority(20000)


def build_interactable():
    import unreal
    from jonggu.blueprints.graph import G, B, ensure_bp, add_vars, declare_function, compile_bp, save_bp
    # Shared Blueprint base with a public interaction contract. UE5.8 Python
    # does not expose a supported API for editing implemented BPI interfaces.
    bp = ensure_bp('BP_RestaurantInteractable', unreal.Actor)
    add_vars(bp, [('interaction_id', 'int', -1)])
    B.set_blueprint_variable_instance_editable(bp, 'interaction_id', True)
    B.set_blueprint_variable_category(bp, 'interaction_id', 'Prototype')
    declare_function(bp, 'GetInteractionId', outputs={'id': 'int'}, pure=True)
    compile_bp(bp)
    getter = G(bp, 'GetInteractionId')
    getter.ret(id=getter.get('interaction_id'))

    subsystem = unreal.get_engine_subsystem(unreal.SubobjectDataSubsystem)
    library = unreal.SubobjectDataBlueprintFunctionLibrary
    handles = subsystem.k2_gather_subobject_data_for_blueprint(bp)
    context = handles[0]
    found = {}
    for handle in handles:
        obj = library.get_object_for_blueprint(library.get_data(handle), bp)
        if obj:
            for name in ('prototype_root', 'prototype_label'):
                if obj.get_name().startswith(name): found[name] = (handle, obj)

    def component(name, cls, parent):
        if name in found: return found[name]
        handle, reason = subsystem.add_new_subobject(unreal.AddNewSubobjectParams(
            parent_handle=parent, new_class=cls, blueprint_context=bp))
        if str(reason): raise RuntimeError('Cannot add marker component: ' + str(reason))
        subsystem.rename_subobject_member_variable(bp, handle, name)
        obj = library.get_object_for_blueprint(library.get_data(handle), bp)
        return handle, obj

    root_handle, root = component('prototype_root', unreal.SceneComponent, context)
    if 'prototype_root' not in found and not subsystem.make_new_scene_root(context, root_handle, bp):
        raise RuntimeError('Cannot set marker scene root')
    _, label = component('prototype_label', unreal.TextRenderComponent, root_handle)
    root.set_mobility(unreal.ComponentMobility.MOVABLE)
    configure_interaction_label(label)
    save_bp(bp)
    return bp
