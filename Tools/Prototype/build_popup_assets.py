"""Project-owned pixel UI textures and Galmuri fonts; editor authoring only."""
from pathlib import Path
import json

UI_ROOT = '/Game/Jonggu/Prototype/UI'
FRAME_TEXTURE = UI_ROOT + '/T_PopupFrame.T_PopupFrame'
CARD_TEXTURE = UI_ROOT + '/T_PopupCard.T_PopupCard'
CLOSE_TEXTURE = UI_ROOT + '/T_PopupClose.T_PopupClose'
HUD_FRAME_TEXTURE = UI_ROOT + '/T_HUDFrame.T_HUDFrame'
HUD_CARD_TEXTURE = UI_ROOT + '/T_HUDCard.T_HUDCard'
HUD_PROMPT_TEXTURE = UI_ROOT + '/T_HUDPrompt.T_HUDPrompt'
HUD_COIN_TEXTURE = UI_ROOT + '/T_HUDCoin.T_HUDCoin'
FOOD_TEXTURES = {i: UI_ROOT + f'/T_PopupFood{i}.T_PopupFood{i}' for i in range(3)}
POPUP_FONT_REGULAR = UI_ROOT + '/Galmuri11_Font.Galmuri11_Font'
POPUP_FONT_BOLD = UI_ROOT + '/Galmuri11_Bold_Font.Galmuri11_Bold_Font'
TEXTURE_SIZE = (32, 32)
SOURCE_BORDER = 8
FONT_NOMINAL_PIXELS = 32

TEXTURE_SOURCES = {
    'T_HUDFrame': '/Game/Jonggu/Textures/UI/Panels/T_dark_thin_outline_panel_0220e90d0a44',
    'T_HUDCard': '/Game/Jonggu/Textures/UI/Panels/T_dark_solid_panel_3bcc7946f228',
    'T_HUDPrompt': '/Game/Jonggu/Textures/UI/MessageBoxes/T_system_text_box_c59eac847b90',
    'T_HUDCoin': '/Game/Jonggu/Textures/Shared/T_coin_3b8e77a5bcc8',
    'T_PopupFrame': '/Game/Jonggu/Textures/UI/Panels/T_light_outline_panel_5a45846ce878',
    'T_PopupCard': '/Game/Jonggu/Textures/UI/Panels/T_light_solid_panel_f073c0237f48',
    'T_PopupClose': '/Game/Jonggu/Textures/UI/Buttons/T_x_button_2b90c4ed2ce3',
    'T_PopupFood0': '/Game/Jonggu/Textures/Item/Food/T_food_001_6d73241bb8b1',
    'T_PopupFood1': '/Game/Jonggu/Textures/Item/Food/T_food_002_70895c6aba72',
    'T_PopupFood2': '/Game/Jonggu/Textures/Item/Food/T_food_003_f4f73baecaf9',
}


def build_popup_assets():
    import unreal
    assets = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    asset_tools = unreal.AssetToolsHelpers.get_asset_tools()
    report = {'textures': {}, 'fonts': {}, 'texture_size': TEXTURE_SIZE,
              'source_border': SOURCE_BORDER, 'font_nominal_pixels': FONT_NOMINAL_PIXELS}
    for name, source in TEXTURE_SOURCES.items():
        destination = UI_ROOT + '/' + name
        texture = assets.load_asset(destination) if assets.does_asset_exist(destination) else assets.duplicate_asset(source, destination)
        if texture is None:
            raise RuntimeError('Cannot create popup texture: ' + destination)
        texture.set_editor_property('filter', unreal.TextureFilter.TF_NEAREST)
        texture.set_editor_property('mip_gen_settings', unreal.TextureMipGenSettings.TMGS_NO_MIPMAPS)
        texture.set_editor_property('lod_group', unreal.TextureGroup.TEXTUREGROUP_UI)
        texture.set_editor_property('compression_settings', unreal.TextureCompressionSettings.TC_EDITOR_ICON)
        texture.set_editor_property('srgb', True)
        texture.set_editor_property('never_stream', True)
        if not assets.save_loaded_asset(texture, only_if_is_dirty=False):
            raise RuntimeError('Cannot save popup texture: ' + destination)
        report['textures'][name] = {'path': texture.get_path_name(), 'source': source,
            'source_size': (34, 34) if name == 'T_HUDPrompt' else (7, 7) if name == 'T_HUDCoin' else TEXTURE_SIZE}

    resources = Path(__file__).parent / 'Resources' / 'Fonts'
    for source_name, name in [('Galmuri11.ttf', 'Galmuri11'), ('Galmuri11-Bold.ttf', 'Galmuri11_Bold')]:
        face_path = UI_ROOT + '/' + name
        font_path = face_path + '_Font'
        if not assets.does_asset_exist(font_path):
            if '-run=' in unreal.SystemLibrary.get_command_line().lower():
                raise RuntimeError('First font import requires the interactive editor. Run build_prototype.py in the editor Output Log.')
            source_file = resources / source_name
            if not source_file.is_file():
                raise RuntimeError('Popup font source missing: ' + str(source_file))
            task = unreal.AssetImportTask()
            task.set_editor_property('filename', str(source_file))
            task.set_editor_property('destination_path', UI_ROOT)
            task.set_editor_property('destination_name', name)
            task.set_editor_property('automated', True)
            task.set_editor_property('replace_existing', True)
            task.set_editor_property('save', True)
            factory = unreal.FontFileImportFactory()
            factory.set_editor_property('batch_create_font_asset', unreal.BatchCreateFontAsset.YES)
            task.set_editor_property('factory', factory)
            asset_tools.import_asset_tasks([task])
        font = assets.load_asset(font_path)
        if not isinstance(font, unreal.Font):
            raise RuntimeError('Popup composite font was not generated: ' + font_path)
        font.set_editor_property('font_cache_type', unreal.FontCacheType.RUNTIME)
        font.set_editor_property('runtime_font_source', unreal.RuntimeFontSource.ASSET)
        # Canvas uses point-sized legacy fonts: 24pt = 32px at the engine's 96DPI.
        font.set_editor_property('legacy_font_size', 24)
        if not assets.save_loaded_asset(font, only_if_is_dirty=False):
            raise RuntimeError('Cannot save popup font: ' + font_path)
        report['fonts'][name] = {'font': font.get_path_name(), 'face': face_path,
                                 'source': str(resources / source_name), 'legacy_font_size': 24}
    return report


if __name__ == '__main__':
    print(json.dumps(build_popup_assets(), ensure_ascii=False, indent=2))
