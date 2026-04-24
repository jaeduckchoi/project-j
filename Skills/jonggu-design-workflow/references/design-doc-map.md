# Design Doc Map

Use this map after reading `Docs/Project/GAME_DOCS_INDEX.md`. Load only the rows needed for the current task.

| Design task | First canonical doc | Usually pair with |
| --- | --- | --- |
| Overall game intent, pitch, or player loop | `Docs/Gameplay/GAME_DESIGN_OVERVIEW.md` | `Docs/Gameplay/GAMEPLAY_CORE_LOOP.md` |
| Hub restaurant operation, cooking flow, serving readability | `Docs/Gameplay/GAMEPLAY_CORE_LOOP.md` | `Docs/Gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md` |
| Restaurant growth, menu pressure, reward framing | `Docs/Gameplay/GAMEPLAY_RESTAURANT_AND_GROWTH.md` | `Docs/Gameplay/GAMEPLAY_CORE_LOOP.md` |
| Exploration, island progression, gathering feel | `Docs/Gameplay/GAMEPLAY_EXPLORATION.md` | `Docs/Scene/BEACH_WHITEBOX.md` |
| Hub composition, camera, one-screen layout | `Docs/Scene/HUB_WHITEBOX.md` | `Docs/Scene/GAME_SCENE_AND_SETUP.md` |
| Beach or exploration whitebox composition | `Docs/Scene/BEACH_WHITEBOX.md` | `Docs/Scene/GAME_SCENE_AND_SETUP.md` |
| Scene hierarchy, authored helper objects, serialization contract | `Docs/Scene/GAME_SCENE_AND_SETUP.md` | `Docs/Scene/SCENE_HIERARCHY_GROUPING_RULES.md` |
| HUD, popup, text, font, readability, or UI style | `Docs/UI/UI_AND_TEXT_GUIDE.md` | `Docs/UI/UI_GROUPING_RULES.md` |
| Generated UI bindings, popup bindings, or import metadata | `Docs/Project/SOURCE_OF_TRUTH.md` | `Docs/Build/GAME_BUILD_GUIDE.md` |

## Design Folder Use

- Use `Design/Button`, `Design/Character`, `Design/Frame`, `Design/Item`, `Design/Object`, `Design/Tile set`, and `Design/Wall set` as art/reference buckets.
- Do not treat binary design files as broad reading targets.
- When a specific art asset must be inspected, read only that asset or its metadata and record why it was necessary.
