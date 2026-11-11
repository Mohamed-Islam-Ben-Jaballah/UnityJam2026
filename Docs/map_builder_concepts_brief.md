# Map Builder Tool and Map Transition

* **Author:** Project team (EGM Brainstorm)
* **Date:** 2026-11-10
* **Status:** Concepts brief after two Goldfish review rounds and three clarification rounds

## Overview

The map builder is a dockable Unity editor window that creates and edits `MapData`
assets. It paints a terrain layer (`Floor`, `Hole`, `Damaging`, `Wall`) and an object
layer (one player start, numbered exits, `EnemyData` spawns, `TreasureData` treasures)
on Floor cells, with click-drag painting, a live grid, and a scene preview that rebuilds
only a generated subtree from `MapData` plus an optional `ThemeData`.

The tool is also the authoring surface for the run graph. Each exit carries a
destination. Standing on an exit shows a persistent Take Exit button; taking it heals 1
HP, opens a reward screen, then a loading overlay, then the destination map in the same
scene. The run keeps current HP, max HP, gold, owned cards, and claimed cards; the torch
resets on level start; the deck is restocked and shuffled; the old map state is
discarded.

Failure no longer ends the run. HP 0, deck exhaustion, and torch burnout reset the
current map to the state recorded at level entry. The source-of-truth docs are updated
to match.

## Analysis

### A. Reset replaces migration (resolved)

The old maps are deleted. `QA_Level` is rebuilt with the new schema. Scene scripts are
updated. `ThemeData` is not used on `QA_Level` because it is a state-machine test scene.
There is no migration and no backward compatibility.

Consequences that still need handling:

- Deleting `QA_Level.asset` breaks the GUID reference in `GameSessionQA.unity`. The scene
  must be re-linked or regenerated after the new map exists. Build the new map and scene
  first, then delete the old asset, or write a small editor step that preserves the
  reference.
- `TileType` loses `Exit` and `Treasure`. Every consumer must change in the same commit:
  `MapDataEditor`, `Tile`, `GameSession`, `QaBootstrap`, `QaTargetingController`, and
  `QaSceneBuilder`.
- `QaSceneBuilder` currently overwrites `QA_Level` with a different layout and writes to
  `Assets/Scenes/GameSessionQA.unity` while the committed scene is under
  `Assets/_Game/Scenes/`. Retire it, guard it, or rewrite it as part of the reset.
- Add `schemaVersion` for the next schema change, even though there is no old data to
  migrate now.

### B. The map is now a run graph (Blocker)

Exits stop being terrain. An exit is a marker with a display number and a destination.
Auto-number exits in creation order and reorder on delete; the number is display only,
while the destination reference carries the link. Every exit must have a destination; an
exit without one is a soft alert.

A destination can be:

- a prefab,
- a `MapData`,
- a `MapData` plus a `ThemeData`.

A Unity prefab cannot contain a `ScriptableObject` directly. A prefab destination must
expose a `MapView` `MonoBehaviour` with a `MapData` reference somewhere in its hierarchy.
If that reference is missing, the exit is invalid and the builder logs a soft alert. The
same component exposes the optional `ThemeData`.

The prefab is a visual shell. `MapData` drives gameplay. The runtime builds the
destination grid, markers, and spawns from the resolved `MapData`; prefab decorations are
preserved and must not duplicate generated gameplay tiles. The prefab's `MapView` must
reference the same `MapData` asset the builder previews.

The transition rules:

- The player stays in the same scene. A loading overlay with a rotating icon runs for at
  least one second and until the destination is ready.
- Current HP and max HP carry over. Taking an exit heals 1 HP, capped at max.
- The torch resets at the destination level start.
- The deck is restocked: all used and discarded cards return, the chosen pack is added
  before the restock, and the combined deck is shuffled. The hand resets to 5 and the
  free reroll resets. Gold persists.
- Claimed treasure cards are owned and survive the transition.
- The exited map's state is discarded. The player cannot return to it. If the same map
  asset is used again later, it starts as a fresh map.
- The level number increments on each transition so paid rerolls unlock from level 2.
- If a destination is null, missing, or has no player start at runtime, stay on the
  current map and log an error. Resolve and validate the destination before the reward
  screen or any run-state change, so a failed transition grants no reward and changes
  nothing.
- End-of-run is not an exit. A custom end-of-run map will trigger the end screen after a
  boss defeat later. Until then the run has no success terminal.

The exit interaction is a persistent Take Exit button over the exit marker while the
player stands on it. There is no yes/no popup. The torch keeps running while the button
is visible. Clicking it goes straight to reward selection. The button replaces the
step-off-and-re-enter rule and removes the soft-lock where the player says No and cannot
leave the tile. The prompt appears only when the player is standing on the exit; crossing
an exit during a Walk path does not trigger it.

### C. Reward selection is a new system (Major)

A `Card3Tuple` is three `CardData` references. Duplicates inside one tuple are allowed.
Two tuples are identical when their cards match as an unordered multiset: `(A, B, C)` and
`(C, B, A)` are the same, while `(A, A, B)` and `(A, B, B)` are different. Identical
tuples in one map's reward pool log a soft console error but are allowed.

The map being exited owns a reward pool of X tuples, with X always 6 or more. The builder
must edit this pool and warn when X is below 6.

At the reward screen:

- Choose three distinct tuples from the pool, without replacement.
- Show three options. Each option displays its three cards.
- Each option has one reroll. A reroll replaces only that option and must return a tuple
  not already shown. With X at least 6 there are enough unseen tuples for three initial
  options and three single rerolls.
- The player picks one option and receives all three cards. Define whether the pick is
  immediate or select-then-confirm.
- No meta reward stage exists yet; skip it until it is defined.
- The torch is idle during reward selection. The player sees a reward screen, not the
  character.
- Chosen cards are added to the run deck before the restock and shuffled with it.

Runtime fallback for a hand-edited asset below six: log, show the available options, and
disable rerolls when no unseen tuple remains.

### D. The run state and failure model (Blocker)

`GameSession` is level-scoped. `RunState` exists but is dead code. The transition needs a
run owner that survives the map swap and holds at least current HP, max HP, owned deck,
gold, claimed cards, and level number. Decide whether that owner is a new run controller,
`RunState`, or `GameSession` promoted to run scope. Recommended: a `RunController` owns a
`RunState`; `GameSession` becomes map-scoped and accepts an entry context.

Map assets stay immutable. Opened treasure, the current map, and the destination are run
or session state. The same map reused later must be independent.

Failure model, as decided:

- HP 0 does not end the run. It resets the current level to the state recorded at level
  entry: entry HP, restocked deck and hand, free reroll, torch, enemies, and unopened
  treasure.
- Deck exhaustion before reaching an exit also resets the current level. The restock at
  map finish does not remove the in-map exhaustion failure.
- Torch burnout resets the current level.
- Claimed treasure remains owned and opened treasure stays opened under the earlier rule.
  If a pure snapshot reset is wanted, this is an open decision.
- There is no run failure terminal until the custom end-of-run map exists.
- `Game-Design.md`, `Architecture.md`, `Roadmap.md`, and `README.md` are updated: HP 0
  resets the level and does not end the run.

### E. Map size is now 8 to 32 (resolved)

Change the docs from the 8x8, 12x12, and 38x38 tiers to a single 8 to 32 range with 8x8
and 12x12 presets. One `MapSizeLimits` or tier table must feed the builder, `MapData`,
validation, and the docs. Non-square maps are allowed. The builder no longer needs to
author 38x38.

### F. Validation is soft alerts, not core blocking (Major)

The level designer owns map correctness. The editor logs non-blocking alerts and does not
prevent save. Recommended soft alerts:

- no player start, or more than one player start,
- zero exits,
- an exit with no destination,
- a prefab destination without a `MapView`/`MapData`,
- markers outside the grid,
- duplicate marker coordinates,
- a marker on a non-Floor cell,
- `tiles.Length != width * height`,
- null `EnemyData`, `TreasureData`, `CardData`, or `ThemeData` references,
- reward pool below six tuples,
- identical reward tuples,
- a destination map with no player start,
- a reward tuple with a null card.

The runtime still needs guards so a bad asset cannot throw. `BuildWorld` calls
`GridMap.Get` without a bounds check. Missing destinations and missing player starts must
log and keep the player on the current map.

`playerSpawn` is a non-nullable `GridCoord` defaulting to `(0,0)`, which is a legal cell.
Add a presence flag or sentinel so "unset" can be detected and warned about.

Alerts should appear in a window panel and in the console, with a jump-to-cell action.

### G. Builder UX and the new editors (Major)

Keep the earlier authoring requirements: a custom zoomable grid instead of a button
matrix, click-drag painting with line interpolation, one Undo entry per stroke, erase,
a rule for terrain painted under a marker, per-object list and delete, coordinate readout,
legend, keyboard shortcuts, persisted tool state, save/dirty handling, and bulk tools.

New editors the run graph adds:

- **Exit list:** number, cell, destination kind, destination references. Auto-number and
  reorder on delete. Show the number in the cell. Detect a prefab destination's `MapView`
  and its `MapData`.
- **Reward pool:** add, remove, and edit `Card3Tuple` entries, each with three `CardData`
  fields. Show the tuple count and a soft warning below six. Detect identical tuples as
  unordered multisets and log the console error, but allow them.
- **Map name and size:** name is the filename and a `mapName` field; define which side
  wins or enforce sync through `AssetDatabase`.
- **Preview:** optional `ThemeData`; `QA_Level` uses color placeholders.

Marker text: exit shows its number, treasure shows `TreasureData.shortCode`, enemy shows
`EnemyData.shortCode`. Define uniqueness, length, reserved codes (`P` for player), and a
fallback from the asset name.

### H. Theme and preview (Major)

`ThemeData` is a runtime asset used by both the scene preview and destination setup. It
maps terrain tiles and markers and holds per-`EnemyData` and per-`TreasureData` sprite
entries. It overrides asset sprites; without a theme, use `EnemyData.sprite` or
`TreasureData.sprite`; terrain falls back to color placeholders. Per-asset override lists
need a serializable pair array, duplicate detection, and a missing-asset warning.

Preview rules:

- Edit mode only; disable in play mode so `QaBootstrap` does not double-render.
- Rebuild only a generated subtree identified by marker components, not by the name
  `Generated`. Manually added decorations live under a separate documented parent.
- Generated objects should not be saved as scene content. Use `HideFlags` or strip them
  before save.
- Rebuild only when map or theme content changes; debounce, guard re-entry, and refresh
  on Undo.
- Use one coordinate conversion and one visual resolver in the builder, the preview, and
  the runtime.
- `MapBuilderWindow` lives in `_Game.Editor`; `TreasureData`, `ThemeData`, `MapView`, and
  `Card3Tuple` live in `_Game.Runtime`; guard `UnityEditor` code.

### I. Asset lifecycle, QA reset, and integration (Major)

- Delete the old map assets after building the new `QA_Level`. Re-link
  `GameSessionQA.unity` and update `QaBootstrap`, `QaTargetingController`, and
  `QaSceneBuilder` to the new model.
- Default folders: `Assets/_Game/Data/Maps`, `Assets/_Game/Data/Treasures`, and
  `Assets/_Game/Data/Themes`. Add `CreateAssetMenu` types and inline create in brush
  pickers.
- Map name is the filename and a field. Use `AssetDatabase` for rename and creation so
  GUID references survive. Define duplicate, overwrite, and invalid-character behavior.
- The exit destinations form a one-way graph with no return. Reusing a map resets it.
  There is no visited-set state to keep.
- The builder output needs a start map and a "Play this map" path. A full progression
  catalog can wait, because exits reference destinations directly.
- Add a "Play from this map" and "start at this exit" debug entry so the author can test
  the transition without replaying the run.

### J. Testing and documentation (Major)

There is still no `_Game.Tests` assembly. Add one and cover:

- Tuple identity as an unordered multiset; identical tuple soft error; X below six.
- Reward selection without replacement, per-option reroll, unseen replacement, and the
  fallback when the pool is too small.
- Deck restock: used and discarded cards return, chosen pack is added before the shuffle,
  hand resets to 5, free reroll resets, gold persists, claimed treasure survives.
- Transition: HP and max HP carry, taking an exit heals 1, torch resets, old map state is
  discarded, reusing a map resets it, level number increments.
- Failure: HP 0, deck exhaustion, and torch burnout reset the level to its entry state
  and do not end the run.
- Take Exit button: appears while standing on the exit, torch keeps running, clicking
  goes to reward selection, crossing an exit during a Walk path does not trigger it.
- Destination validation before reward: invalid destination grants no reward, changes no
  state, and keeps the player on the current map.
- Destination resolution: prefab with `MapView`/`MapData`, prefab without it, `MapData`
  alone, `MapData` plus `ThemeData`, missing destination, destination without player
  start.
- Loading overlay lasts at least one second and until the destination is ready.
- Builder: drag stroke Undo, resize with out-of-bounds markers, reward and exit editors,
  soft alerts.
- Preview: decoration preservation, no double render in play mode, no scene dirtying.

Docs to update in the same change: `Game-Design.md` §2, §5.1, §6.3, §6.4, §6.5, §7,
§9.13; `Architecture.md` §3, §4, §5, §7, §8, §10; `Roadmap.md` size, failure model, and
status; `README.md` treasure, exit, map-size, and failure text. Add `TreasureData`,
`ThemeData`, `Card3Tuple`, `ExitSpawn`, `MapView`, `shortCode`, and the run-transition and
failure rules. Confirm no source-of-truth doc says HP 0 ends the run. Mark the
between-map pack/bonus text as superseded by the `Card3Tuple` reward screen.

### K. Remaining risks

- A prefab cannot contain a ScriptableObject directly. The `MapView` contract must be
  explicit or prefab destinations cannot be validated.
- The reward screen, Take Exit button, and loading overlay are new UI surfaces. The
  project currently has only the QA hand UI.
- Soft alerts do not prevent crashes from hand-edited assets. Runtime guards are still
  required.
- The one-way graph has no cycle or dead-end validation. A branch can strand the run.
  This is accepted as the level designer's responsibility.
- There is no run failure or success terminal until the custom end map. A cycle can be
  farmed, and HP attrition has no run-ending consequence.
- Per-asset theme overrides create a second visual source and can drift from the data
  asset sprites.
- 32x32 painting and preview rebuilds need a measured performance target.
- Deleting `QA_Level` and rebuilding the QA scene can leave stale scene references if the
  reset is not atomic.
- The exact failure-reset treatment of treasure claimed during a failed attempt is still
  ambiguous: "same state you entered" suggests a snapshot, while earlier rules keep
  claimed treasure.

## Goals vs. Non-Goals

### In-scope goals

- A dockable `MapBuilderWindow` that creates, opens, names, and edits `MapData` assets.
- X and Y sliders from 8 to 32, with a single size source and 8x8/12x12 presets.
- Terrain layer: Floor, Hole, Damaging, Wall. Sand stays runtime-only.
- Object layer: one player start, numbered exits with destinations, `EnemyData` spawns,
  `TreasureData` treasures, one marker per Floor cell.
- Click-drag painting with line interpolation, per-stroke Undo, erase, and a terrain
  under-marker rule.
- `TreasureData` with weighted cards, gold, heal, card count, `sprite`, and `shortCode`.
  `EnemyData.shortCode`.
- Numbered `ExitSpawn` entries with a prefab, `MapData`, or `MapData` plus `ThemeData`
  destination.
- `Card3Tuple` reward pools on each map, with three options, per-option reroll, and
  unordered multiset identity.
- A run transition that carries HP and max HP, heals 1 on exit, resets the torch,
  restocks and shuffles the deck, resets the hand and free reroll, keeps gold and claimed
  cards, discards the old map, increments the level number, and runs a minimum one-second
  loading overlay.
- A persistent Take Exit button, a reward screen, and runtime guards for missing
  destinations.
- A failure model where HP 0, deck exhaustion, and torch burnout reset the current level
  to its entry state and do not end the run.
- Soft editor alerts for the listed map errors.
- A generated-subtree scene preview with optional `ThemeData` and manual decorations.
- Reset of `QA_Level` and the QA scene scripts to the new model.
- Tests and matching documentation.

### Out-of-scope non-goals

- Migrating old maps. They are deleted.
- Authoring 38x38 maps. The docs change to 8 to 32.
- Meta upgrades after the reward screen. Skipped until defined.
- The end-of-run screen and boss defeat. Custom and later.
- Per-spawn treasure overrides. One `TreasureData` asset gives the same reward everywhere.
- Rarity as a gameplay or loot field.
- Returning to a visited map. Reusing a map resets it.
- Import or export to Tiled, CSV, or JSON.
- Multi-user or multi-window editing.
- Decoration authoring inside the map builder.

## Key Decisions & Staging for Next Steps

### Resolved

1. **Reset, not migration.** Delete old maps, rebuild `QA_Level`, update scene scripts,
   and add no backward-compatibility path.
2. **Map size 8 to 32.** Update the docs; drop 38x38.
3. **Soft validation.** The editor logs non-blocking alerts; the runtime stays on the
   current map and logs.
4. **Exit destinations are required.** Auto-number exits and reorder on delete. A prefab
   destination must expose a `MapView` with `MapData`. The prefab is a visual shell;
   `MapData` drives gameplay.
5. **Transition.** Same scene, loading overlay, carry HP and max HP, heal 1 on exit, reset
   torch, restock and shuffle the deck, reset hand and free reroll, keep gold and claimed
   cards, discard the old map, no return, increment the level number.
6. **Rewards.** `Card3Tuple` pool on the map, X at least six, three distinct options,
   one reroll per option, unordered multiset identity, no meta stage.
7. **Exit interaction.** A persistent Take Exit button over the exit marker; no popup;
   torch keeps running; clicking goes to reward selection.
8. **Failure model.** HP 0, deck exhaustion, and torch burnout reset the current level to
   its entry state; no run end until the custom end map.
9. **Transition order.** Validate the destination before the reward screen or run-state
   changes; a failed transition grants no reward and changes nothing.

### Still to specify before coding

1. The exact `MapData` v2 fields and the `ExitSpawn`/`MapDestination`/`MapView` shapes.
2. The run-state owner and the split between run state and level state.
3. The reward-screen layout and whether a pick is immediate or select-then-confirm.
4. Whether treasure claimed during a failed attempt survives the level reset. The "same
   state" wording suggests a snapshot; earlier rules keep claimed cards.
5. Whether dice are run state that carry across maps.
6. The soft-alert panel and console policy.
7. The default folders, creation templates, and rename/overwrite policy.
8. The full docs update checklist and the test plan.

### Staging

0. Reset the data and scenes: build the new `QA_Level` and transition smoke scene first,
   update `TileType` and every consumer, retire `QaSceneBuilder`, re-link
   `GameSessionQA.unity`, then delete the old maps.
1. Add the run state and the runtime object layer. Implement exits as markers,
   destination resolution, the failure resets, the transition, deck restock, reward
   selection, the Take Exit button, and the loading overlay.
2. Build the window core: create/open/name, size sliders, terrain and object brushes,
   drag paint, Undo, erase, exit and reward editors, and the soft-alert panel.
3. Add the scene preview and `ThemeData`, with the prefab `MapView` contract and the
   generated/decoration split.
4. Add tests, polish, bulk tools, and the full documentation update.

### What survives the review

The two-layer split, Floor-only markers, shared `TreasureData` assets, soft validation,
and color placeholders are workable. The exit graph is simple once the run state and
`MapView` contract exist. The `Card3Tuple` reward pool fits the existing card data. The
persistent Take Exit button removes the prompt soft-lock. The one-way transition removes
the need for visited-map state.
