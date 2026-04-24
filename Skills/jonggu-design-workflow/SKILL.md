---
name: jonggu-design-workflow
description: Design workflow for Jonggu's Restaurant at D:\project-j. Use when Codex plans, reviews, or changes gameplay feel, UX flow, UI presentation, scene composition, whitebox layout, visual hierarchy, readability, design docs, or art-facing implementation notes for this Unity 2D top-down restaurant prototype.
---

# Jonggu Design Workflow

Use this skill for design-facing work in `D:\project-j`, especially when a task changes how the game reads, feels, flows, or is presented to the player. Respond in Korean by default.

## Start

1. Read `AGENTS.md`.
2. Read `Docs/README.md`.
3. Read `Docs/Project/GAME_ASSISTANT_RULES.md`.
4. Read `Docs/Project/GAME_DOCS_INDEX.md`.
5. Read `Docs/Project/SOURCE_OF_TRUTH.md` before touching code, scenes, generated assets, Unity metadata, or behavior docs.
6. Use `references/design-doc-map.md` to choose only the 1-2 design docs that match the current request.

## Design Workflow

- State the design intent first: player goal, screen/scene, current friction, and desired player-readable outcome.
- Separate intent from implementation. Keep design decisions in canonical docs; keep scene serialization, generated resources, and code in their existing source-of-truth paths.
- Preserve the current prototype contract unless the user asks to change it: `FrontCounter -> BackCounter -> FrontCounter`, `Open/Close`, one-screen hub readability, and explicit UI binding ownership.
- For visual or layout changes, check the expected camera/screen contract, managed object names, Canvas grouping, and editor preview path together.
- For art-facing requests, treat `Design/` as reference material and avoid broad reads of binary source assets unless the task requires a specific asset.
- If a design change affects gameplay behavior, update the relevant gameplay doc in the same change.

## Output Shape

When planning or reviewing design work, include:

- the player-facing design goal
- the canonical docs consulted
- the implementation surface likely affected
- validation that would prove the design reads correctly in Unity

When implementing design work, keep changes narrow and verify the smallest useful slice. If Unity execution or compilation is not run, state the remaining verification step in the final response.
