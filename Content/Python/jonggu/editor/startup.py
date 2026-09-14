"""Frame the migrated Hub once when this project opens in the interactive editor."""
import os
import time
import re
import unreal

from jonggu.paths import ROOT

# ConPTY inherits this editor-process value; nothing is written to user/global settings.
os.environ['JONGGU_PROJECT_DIR'] = str(ROOT)

_command_line = unreal.SystemLibrary.get_command_line().lower()
_jonggu_automated = re.search(r'(?:^|\s)-(?:run|executepythonscript|game|nullrhi|unattended)(?:=|\s|$)', _command_line)
if not _jonggu_automated:
    _jonggu_deadline = time.monotonic() + 120
    _jonggu_handle = None
    _jonggu_next_attempt = 0.0
    _jonggu_last_error = None

    def _jonggu_stop():
        global _jonggu_handle
        if _jonggu_handle is not None:
            unreal.unregister_slate_post_tick_callback(_jonggu_handle)
            _jonggu_handle = None

    def _jonggu_initial_view(delta_seconds):
        global _jonggu_next_attempt, _jonggu_last_error
        now = time.monotonic()
        if now < _jonggu_next_attempt:
            return
        _jonggu_next_attempt = now + 0.25
        try:
            world = unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()
            if world and world.get_path_name().startswith("/Game/Jonggu/Maps/L_Hub."):
                actors = unreal.get_editor_subsystem(unreal.EditorActorSubsystem).get_all_level_actors()
                camera = next((a for a in actors if isinstance(a, unreal.CameraActor) and any(str(t).startswith("JongguMigration:camera:") for t in a.tags)), None)
                if camera:
                    levels = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
                    viewport = levels.get_active_viewport_config_key()
                    # These editor APIs silently do nothing before a viewport
                    # exists. Confirm on a later tick before ending the retry.
                    if str(viewport) not in ("", "None"):
                        if (levels.get_pilot_level_actor(viewport) == camera
                                and levels.get_exact_camera_view(viewport)
                                and levels.editor_get_game_view(viewport)):
                            unreal.log("JONGGU: Hub source camera framed. Stop piloting to edit the world.")
                            _jonggu_stop()
                            return
                        levels.set_exact_camera_view(True, viewport)
                        levels.editor_set_game_view(True, viewport)
                        levels.pilot_level_actor(camera, viewport)
        except Exception as exc:
            # Early editor subsystem/layout initialization can still be in
            # progress. Preserve the bounded retry rather than abandoning it.
            _jonggu_last_error = str(exc)
        if now > _jonggu_deadline:
            if _jonggu_last_error:
                unreal.log_warning("JONGGU startup camera: " + _jonggu_last_error)
            _jonggu_stop()

    _jonggu_handle = unreal.register_slate_post_tick_callback(_jonggu_initial_view)
