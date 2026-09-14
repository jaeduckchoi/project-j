"""Author a camera-scoped SDR look for the migrated Paper2D maps (Unreal 5.8).

Import configure_preview_camera from the migration importer, or execute this
file as an Unreal Python commandlet to update the two already saved maps.
No global console variables, project settings, or template assets are changed.

Implementation checked against the installed UE 5.8.2 sources:
  Engine/Classes/Camera/CameraComponent.h: AddOrUpdateBlendable
  Engine/Classes/Engine/Scene.h: FPostProcessSettings exposure/optical fields
  Engine/Classes/Engine/BlendableInterface.h: BL_ReplacingTonemapper
  Renderer/Private/PostProcess/PostProcessMaterial.cpp: replacement passes use
      POST_PROCESS_MATERIAL_BEFORE_TONEMAP=0 (no automatic pre-exposure undo)
  Renderer/Private/PostProcess/PostProcessing.cpp: replacement material consumes
      the scene color in place of the default filmic tonemapper
  Engine/Shaders/Private/PostProcessMaterialShaders.usf: emissive becomes output

The current QA reference captures use display gamma 2.2. The replacement pass
undoes scene pre-exposure and applies that display gamma without a film curve.
It targets SDR desktop previews; HDR output is outside this migration scope.

For offscreen verification, a SceneCapture2D with capture_every_frame=False must
set always_persist_rendering_state=True before capture. In UE 5.8 Material.cpp,
UMaterialInterface::OverrideBlendableSettings returns immediately if View.State
is null, so scalar settings can work while the replacement material is skipped.
The normal interactive editor/game camera already has a persistent view state.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path
import unreal

if str(Path(__file__).resolve().parent) not in sys.path:
    sys.path.insert(0, str(Path(__file__).resolve().parent))
from world_paths import CONTENT_ROOT, QA_ROOT, require_unreal_project

ROOT = CONTENT_ROOT
MATERIAL_PATH = ROOT + '/Materials/M_Jonggu_PreviewDisplay'
VERSION = '1_gamma22_unexposed_scene'
LOOK = 'JongguPreviewLookVersion'


def ensure_preview_material():
    """Create/update only our post-process material; preserve identity on rerun."""
    assets = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    material = assets.load_asset(MATERIAL_PATH) if assets.does_asset_exist(MATERIAL_PATH) else None
    if material and assets.get_metadata_tag(material, LOOK) == VERSION:
        return material
    if material is None:
        tools = unreal.AssetToolsHelpers.get_asset_tools()
        material = tools.create_asset('M_Jonggu_PreviewDisplay', ROOT + '/Materials',
                                      unreal.Material, unreal.MaterialFactoryNew())
    if not isinstance(material, unreal.Material):
        raise RuntimeError('Preview material path contains another asset class: ' + MATERIAL_PATH)

    edit = unreal.MaterialEditingLibrary
    edit.delete_all_material_expressions(material)
    material.set_editor_property('material_domain', unreal.MaterialDomain.MD_POST_PROCESS)
    material.set_editor_property('blendable_location', unreal.BlendableLocation.BL_REPLACING_TONEMAPPER)
    material.set_editor_property('blendable_priority', 1000)
    material.set_editor_property('blend_mode', unreal.BlendMode.BLEND_OPAQUE)

    source = edit.create_material_expression(material, unreal.MaterialExpressionSceneTexture, -500, 0)
    source.set_editor_property('scene_texture_id', unreal.SceneTextureId.PPI_POST_PROCESS_INPUT0)
    source.set_editor_property('filtered', False)
    display = edit.create_material_expression(material, unreal.MaterialExpressionCustom, -250, 0)
    display.set_editor_property('description', 'Unexpose scene and encode SDR gamma 2.2; no film curve')
    display.set_editor_property('output_type', unreal.CustomMaterialOutputType.CMOT_FLOAT3)
    pin = unreal.CustomInput()
    pin.set_editor_property('input_name', 'SceneColor')
    display.set_editor_property('inputs', [pin])
    display.set_editor_property('code', (
        'float3 Unexposed = max(SceneColor.rgb * View.OneOverPreExposure, 0.0);\n'
        'return pow(saturate(Unexposed), 1.0 / 2.2);'
    ))
    if not edit.connect_material_expressions(source, 'Color', display, 'SceneColor'):
        raise RuntimeError('Could not connect preview SceneTexture to display transform')
    if not edit.connect_material_property(display, '', unreal.MaterialProperty.MP_EMISSIVE_COLOR):
        raise RuntimeError('Could not connect preview display transform to emissive output')
    errors = edit.recompile_material(material)
    if errors:
        raise RuntimeError('Preview display material compile failed: ' + '; '.join(str(x) for x in errors))
    assets.set_metadata_tag(material, LOOK, VERSION)
    assets.set_metadata_tag(material, 'JongguPreviewLook', 'Camera-scoped SDR gamma 2.2; removes pre-exposure and filmic curve')
    if not assets.save_loaded_asset(material, only_if_is_dirty=False):
        raise RuntimeError('Could not save preview display material')
    return material


def configure_preview_camera(camera_component, material=None):
    """Assign fixed exposure and one replacement blendable, leaving map save to caller."""
    if not camera_component.get_path_name().startswith(ROOT + '/Maps/'):
        raise ValueError('Refusing to configure a camera outside the migration maps')
    material = material or ensure_preview_material()
    settings = camera_component.get_editor_property('post_process_settings')
    values = {
        'auto_exposure_method': unreal.AutoExposureMethod.AEM_MANUAL,
        'auto_exposure_bias': 0.0,
        'auto_exposure_apply_physical_camera_exposure': False,
        'auto_exposure_bias_curve': None,
        'bloom_intensity': 0.0,
        'motion_blur_amount': 0.0,
        'vignette_intensity': 0.0,
        'lens_flare_intensity': 0.0,
        'scene_fringe_intensity': 0.0,
        'film_grain_intensity': 0.0,
        'sharpen': 0.0,
        'local_exposure_highlight_contrast_scale': 1.0,
        'local_exposure_shadow_contrast_scale': 1.0,
    }
    for name, value in values.items():
        settings.set_editor_property('override_' + name, True)
        settings.set_editor_property(name, value)
    camera_component.set_editor_property('post_process_settings', settings)
    camera_component.set_post_process_blend_weight(1.0)
    # Native implementation updates the existing material entry instead of
    # appending another weighted blendable every time the importer is run.
    camera_component.add_or_update_blendable(material, 1.0)
    return validate_preview_camera(camera_component, material)


def validate_preview_camera(camera_component, material=None):
    """Read back persisted authoring fields; image comparison is a separate test."""
    material = material or unreal.load_asset(MATERIAL_PATH)
    settings = camera_component.get_editor_property('post_process_settings')
    weighted = settings.get_editor_property('weighted_blendables').get_editor_property('array')
    ours = [entry for entry in weighted if entry.get_editor_property('object') == material]
    if len(ours) != 1 or abs(ours[0].get_editor_property('weight') - 1.0) > 1e-6:
        raise RuntimeError('Expected exactly one enabled preview display material on camera')
    if settings.get_editor_property('auto_exposure_method') != unreal.AutoExposureMethod.AEM_MANUAL:
        raise RuntimeError('Preview camera exposure did not remain manual')
    if not settings.get_editor_property('override_auto_exposure_method'):
        raise RuntimeError('Preview camera exposure override is disabled')
    return {'camera': camera_component.get_path_name(), 'material': material.get_path_name(),
            'display_gamma': 2.2, 'exposure': 'manual', 'film_curve': 'replaced',
            'matching_blendables': len(ours), 'validation': 'authoring_fields_only'}


def main():
    """Update/reopen only the two migrated maps when explicitly executed."""
    require_unreal_project()
    levels = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
    actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
    material = ensure_preview_material()
    results = []
    for name in ('Hub', 'Beach'):
        level_path = ROOT + '/Maps/L_' + name
        if not levels.load_level(level_path):
            raise RuntimeError('Cannot open ' + level_path)
        cameras = [actor for actor in actors.get_all_level_actors()
                   if isinstance(actor, unreal.CameraActor) and actor.get_actor_label() == 'SourceCamera']
        if len(cameras) != 1:
            raise RuntimeError('Expected one SourceCamera in ' + level_path)
        result = configure_preview_camera(cameras[0].camera_component, material)
        configure_preview_camera(cameras[0].camera_component, material)
        result['repeat_assignment_verified'] = True
        if not levels.save_current_level():
            raise RuntimeError('Cannot save ' + level_path)
        results.append(result)
    for name in ('Hub', 'Beach'):
        if not levels.load_level(ROOT + '/Maps/L_' + name):
            raise RuntimeError('Cannot reopen migration map')
        camera = next(actor for actor in actors.get_all_level_actors()
                      if isinstance(actor, unreal.CameraActor) and actor.get_actor_label() == 'SourceCamera')
        validate_preview_camera(camera.camera_component, material)
    report = {'success': True, 'reopen_verified': True, 'cameras': results,
              'render_verification': 'Requires saved camera post-process settings, Tonemapper enabled, and SceneCapture always_persist_rendering_state=True'}
    QA_ROOT.mkdir(parents=True, exist_ok=True)
    (QA_ROOT / 'preview_look_report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    unreal.log('JONGGU_PREVIEW_LOOK_SUCCESS')


if __name__ == '__main__':
    main()
