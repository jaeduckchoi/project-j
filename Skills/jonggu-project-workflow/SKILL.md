---
name: jonggu-project-workflow
description: Work safely in the Jonggu's Restaurant Unity project at D:\project-j. Use when Codex edits, reviews, or plans changes for this repo, especially gameplay rules, Unity scenes, scene serialization, UI layout/bindings, generated game data, authored CSVs, project structure, or validation steps.
---

# Jonggu Project Workflow

Use this skill when working in `D:\project-j` on "종구의 식당", a Unity 2D top-down restaurant management prototype. Respond in Korean by default.

## Startup

Before changing or reviewing code, scenes, generated assets, or docs:

1. Read `AGENTS.md`.
2. Read `Docs/README.md`.
3. Read `Docs/Project/GAME_ASSISTANT_RULES.md`.
4. Read `Docs/Project/GAME_DOCS_INDEX.md`.
5. Choose only the 1-2 canonical docs needed for the task.
6. Read `Docs/Project/SOURCE_OF_TRUTH.md` before touching code, scenes, generated assets, Unity metadata, or behavior docs.

## Canonical Docs

Use the repo docs as the source of truth; do not duplicate their contents into this skill.

- Gameplay overview or core restaurant loop: read `Docs/Gameplay/GAME_DESIGN_OVERVIEW.md` and usually `Docs/Gameplay/GAMEPLAY_CORE_LOOP.md`.
- Restaurant growth, menus, rewards, or hub operation: read `Docs/Gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md` with `GAMEPLAY_CORE_LOOP.md`.
- Exploration, beach, sea, or island progression: read `Docs/Gameplay/GAMEPLAY_EXPLORATION.md` and the relevant scene whitebox doc.
- Scene hierarchy, authored helper objects, serialization, or layout placement: read `Docs/Scene/GAME_SCENE_AND_SETUP.md` and `Docs/Scene/SCENE_HIERARCHY_GROUPING_RULES.md`.
- Hub or beach spatial work: read `Docs/Scene/HUB_WHITEBOX.md` or `Docs/Scene/BEACH_WHITEBOX.md`.
- UI structure, popup text, Canvas grouping, layout bindings, or editor preview: read `Docs/UI/UI_AND_TEXT_GUIDE.md` and `Docs/UI/UI_GROUPING_RULES.md`.
- Generated resources, CSV game data, import metadata, or build paths: read `Docs/Build/GAME_BUILD_GUIDE.md` and `Docs/Project/SOURCE_OF_TRUTH.md`.
- Project structure, moved files, or asmdef boundaries: read `Docs/Project/GAME_PROJECT_STRUCTURE.md` and `Docs/Project/SOURCE_OF_TRUTH.md`.

## Guardrails

- Treat `Assets/Level/Scenes/*` as the canonical scene serialization source. Runtime code should only fill missing references or helper values minimally.
- Do not directly edit generated output bodies as the primary fix. Change the authored source, generator path, or canonical code first.
- Treat `scene-integrated metadata` as part of the scene/prefab contract when Unity serialization depends on it.
- For UI changes, check managed object names, layout binding assets, and editor preview paths together.
- If behavior and docs change together, update the relevant canonical docs in the same change.
- Keep PowerShell text I/O and saved text files UTF-8 compatible.
- Exclude Unity caches, build outputs, logs, generated solution/project files, IDE locals, archives, and binary source assets from broad reads unless the task requires them.

## Verification

Before finishing, run the narrowest useful validation available.

- For code changes, prefer Unity compile/tests when available.
- For scene or editor preview changes, verify scene reopen cleanliness and rebuild consistency when possible.
- For UI changes, verify managed names, binding assets, and editor preview paths.
- For generated data changes, verify authored CSVs, generated GameData, and fallback behavior stay aligned.
- If Unity execution or compilation was not run, state the remaining verification step in the final response.
