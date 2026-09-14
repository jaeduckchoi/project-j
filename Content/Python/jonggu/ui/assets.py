"""Project-owned pixel UI assets; importing this module never invokes Unreal."""
import json

from jonggu.assets import asset_path, object_path
from jonggu.paths import FONT_SOURCE_DIR

FRAME_TEXTURE = object_path("T_UI_PopupFrame")
CARD_TEXTURE = object_path("T_UI_PopupCard")
CLOSE_TEXTURE = object_path("T_UI_Close")
HUD_FRAME_TEXTURE = object_path("T_UI_HUDFrame")
HUD_CARD_TEXTURE = object_path("T_UI_HUDCard")
HUD_PROMPT_TEXTURE = object_path("T_UI_HUDPrompt")
HUD_COIN_TEXTURE = object_path("T_UI_Coin")
FOOD_TEXTURES = {
    0: object_path("T_UI_KimchiFriedRice"),
    1: object_path("T_UI_KimchiStew"),
    2: object_path("T_UI_KimchiPancake"),
}
POPUP_FONT_REGULAR = object_path("F_Galmuri11_Regular")
POPUP_FONT_BOLD = object_path("F_Galmuri11_Bold")
TEXTURE_SIZE = (32, 32)
SOURCE_BORDER = 8
FONT_NOMINAL_PIXELS = 32

TEXTURE_SOURCES = {
    "T_UI_HUDFrame": "/Game/Jonggu/Textures/UI/Panels/T_dark_thin_outline_panel_0220e90d0a44",
    "T_UI_HUDCard": "/Game/Jonggu/Textures/UI/Panels/T_dark_solid_panel_3bcc7946f228",
    "T_UI_HUDPrompt": "/Game/Jonggu/Textures/UI/MessageBoxes/T_system_text_box_c59eac847b90",
    "T_UI_Coin": "/Game/Jonggu/Textures/Shared/T_coin_3b8e77a5bcc8",
    "T_UI_PopupFrame": "/Game/Jonggu/Textures/UI/Panels/T_light_outline_panel_5a45846ce878",
    "T_UI_PopupCard": "/Game/Jonggu/Textures/UI/Panels/T_light_solid_panel_f073c0237f48",
    "T_UI_Close": "/Game/Jonggu/Textures/UI/Buttons/T_x_button_2b90c4ed2ce3",
    "T_UI_KimchiFriedRice": "/Game/Jonggu/Textures/Item/Food/T_food_001_6d73241bb8b1",
    "T_UI_KimchiStew": "/Game/Jonggu/Textures/Item/Food/T_food_002_70895c6aba72",
    "T_UI_KimchiPancake": "/Game/Jonggu/Textures/Item/Food/T_food_003_f4f73baecaf9",
}
FONT_SOURCES = (
    ("Galmuri11.ttf", "FF_Galmuri11_Regular", "F_Galmuri11_Regular"),
    ("Galmuri11-Bold.ttf", "FF_Galmuri11_Bold", "F_Galmuri11_Bold"),
)


def _import_font(unreal, assets, asset_tools, source_file, face_name, font_name):
    """Import a FontFace and normalize the factory's composite-font name.

    FontFileImportFactory creates `<face>_Font` automatically. The public
    composite Font has its own F_ registry entry rather than inheriting FF_.
    Existing canonical fonts bypass this path, including commandlet rebuilds.
    """
    if "-run=" in unreal.SystemLibrary.get_command_line().lower():
        raise RuntimeError(
            "First font import requires the interactive editor. "
            "Run jonggu.ui.assets.build_popup_assets() in the editor Python console."
        )
    if not source_file.is_file():
        raise RuntimeError("UI font source missing: " + str(source_file))
    face_path = asset_path(face_name)
    font_path = asset_path(font_name)
    task = unreal.AssetImportTask()
    task.set_editor_property("filename", str(source_file))
    task.set_editor_property("destination_path", face_path.rsplit("/", 1)[0])
    task.set_editor_property("destination_name", face_name)
    task.set_editor_property("automated", True)
    task.set_editor_property("replace_existing", True)
    task.set_editor_property("save", True)
    factory = unreal.FontFileImportFactory()
    factory.set_editor_property("batch_create_font_asset", unreal.BatchCreateFontAsset.YES)
    task.set_editor_property("factory", factory)
    asset_tools.import_asset_tasks([task])
    generated_font = face_path + "_Font"
    if not assets.does_asset_exist(font_path):
        if not isinstance(assets.load_asset(generated_font), unreal.Font):
            raise RuntimeError("UI composite font was not generated: " + generated_font)
        if not assets.rename_asset(generated_font, font_path):
            raise RuntimeError("Cannot name UI composite font: " + font_path)


def build_popup_assets():
    """Build both normal HUD and popup textures/fonts at canonical paths."""
    import unreal
    assets = unreal.get_editor_subsystem(unreal.EditorAssetSubsystem)
    asset_tools = unreal.AssetToolsHelpers.get_asset_tools()
    report = {"textures": {}, "fonts": {}, "texture_size": TEXTURE_SIZE,
              "source_border": SOURCE_BORDER, "font_nominal_pixels": FONT_NOMINAL_PIXELS}
    for name, source in TEXTURE_SOURCES.items():
        destination = asset_path(name)
        texture = assets.load_asset(destination) if assets.does_asset_exist(destination) else assets.duplicate_asset(source, destination)
        if texture is None:
            raise RuntimeError("Cannot create UI texture: " + destination)
        texture.set_editor_property("filter", unreal.TextureFilter.TF_NEAREST)
        texture.set_editor_property("mip_gen_settings", unreal.TextureMipGenSettings.TMGS_NO_MIPMAPS)
        texture.set_editor_property("lod_group", unreal.TextureGroup.TEXTUREGROUP_UI)
        texture.set_editor_property("compression_settings", unreal.TextureCompressionSettings.TC_EDITOR_ICON)
        texture.set_editor_property("srgb", True)
        texture.set_editor_property("never_stream", True)
        if not assets.save_loaded_asset(texture, only_if_is_dirty=False):
            raise RuntimeError("Cannot save UI texture: " + destination)
        report["textures"][name] = {
            "path": texture.get_path_name(), "source": source,
            "source_size": (34, 34) if name == "T_UI_HUDPrompt" else (7, 7) if name == "T_UI_Coin" else TEXTURE_SIZE,
        }

    for source_name, face_name, font_name in FONT_SOURCES:
        face_path = asset_path(face_name)
        font_path = asset_path(font_name)
        source_file = FONT_SOURCE_DIR / source_name
        if not assets.does_asset_exist(font_path):
            _import_font(unreal, assets, asset_tools, source_file, face_name, font_name)
        font = assets.load_asset(font_path)
        if not isinstance(font, unreal.Font):
            raise RuntimeError("UI composite font is missing: " + font_path)
        face = assets.load_asset(face_path)
        if not isinstance(face, unreal.FontFace):
            raise RuntimeError("UI font face is missing: " + face_path)
        font.set_editor_property("font_cache_type", unreal.FontCacheType.RUNTIME)
        font.set_editor_property("runtime_font_source", unreal.RuntimeFontSource.ASSET)
        # Canvas uses point-sized legacy fonts: 24pt = 32px at 96 DPI.
        font.set_editor_property("legacy_font_size", 24)
        if not assets.save_loaded_asset(font, only_if_is_dirty=False):
            raise RuntimeError("Cannot save UI font: " + font_path)
        report["fonts"][font_name] = {
            "font": font.get_path_name(), "face": face_path,
            "source": str(source_file), "legacy_font_size": 24,
        }
    return report


if __name__ == "__main__":
    print(json.dumps(build_popup_assets(), ensure_ascii=False, indent=2))
