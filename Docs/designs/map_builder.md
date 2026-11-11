# Map Builder Tool and Map Transition

* **Author:** Project team
* **Status:** Implemented, extended with Grid and Linear map modes
* **Date:** 2026-11-10 (updated 2026-11-11)

--------------------------------------------------------------------------------

## Overview

The project needs two connected pieces of editor and runtime work: a map authoring
tool and a run transition that moves the player from one map to the next.

The current authoring path is a 299-line custom Inspector. It paints one cell per
click, exposes raw spawn arrays, has no drag painting, no exit destinations, no
reward authoring, no scene preview, and no validation beyond resizing. `MapData`
also stores exits and treasures as terrain values, so the game cannot describe a
run graph, and `GameSession` is level-scoped, so there is nowhere to carry HP,
gold, or the owned deck across a map change.

The design replaces that path with a dockable `MapBuilderWindow` that edits
`MapData` assets through terrain and object-marker layers, plus an optional path
layer for Linear maps. It adds
`TreasureData`, `ThemeData`, `Card3Tuple` reward pools, numbered exits with
destinations, a same-scene transition with a loading overlay, a reward screen,
soft editor alerts, and a generated-subtree scene preview. It also fixes the
failure model: HP 0, deck exhaustion, and torch burnout reset the current level
instead of ending the run.

The target user is the level designer. The player-facing outcome is a run where
taking an exit is a deliberate choice, the reward pool shapes the deck, and a
failed attempt restarts the map with the state recorded at entry.

## Implementation update: Grid and Linear modes

The shipped builder supports **two map kinds on the same 4–64 per-axis canvas**:

- **Grid** keeps the existing terrain layer: Floor, Hole, Damaging, Wall, plus
  object markers.
- **Linear** adds a painted path layer. Only path tiles are traversable. Path
  tiles may carry one overlay or marker: Hole, Damaging, enemy, treasure, exit,
  or a branch marker. Walls and sand are rejected. Exactly one start sits at a
  path endpoint, and every other dead end must be a terminal exit.

The builder adds `New Linear`, `Path`, and `Branch` tools. Branch arrows are
derived from the path graph, so the designer only marks the junction. Merge tiles
are **not marked**: the builder infers them from the path graph, using the marked
branches and distance to terminal exits. The Soft Alerts panel reports path tile
counts, inferred branches, inferred merges, terminal exits, disconnected path
tiles, missing branch markers, dead ends without exits, and directed cycles.

Runtime rules for Linear maps:

- Walk spends the full roll forward along the path. Holes kill and reset the
  level. Damaging, treasure, and enemy entry effects resolve per tile.
- Interior exits pause the walk for Continue or Enter. Continue forfeits that
  exit and spends the remaining steps. Terminal exits auto-take.
- Branch tiles pause the walk for a two- or three-arrow choice. The chosen
  direction is locked until the player moves. Merges continue automatically.
- Jump moves two path steps and is unavailable when the first step is a branch.
- Damage hits the first enemy ahead within range, counted in path steps.
- Only Walk, Jump, Damage, Heal, and Draw are shuffled into a Linear level deck.
  Complex cards stay in the run deck for the next Grid map.
- The camera frames the path bounds, and the torch keeps running during prompts.

The 4–64 range is an authoring cap, not a balance promise. The level designer
chooses path length, treasure density, and torch tuning together. The first
playable build still uses 8×8 and 12×12 Grid maps.

## The Problem

The current tool and runtime make the intended design hard to author and
impossible to play as specified.

- **Authoring is slow and error-prone.** The current grid is a matrix of fixed
  28-pixel buttons. There is no drag painting, no erase tool, no bulk fill, no
  zoom, no hover feedback, and no per-object list. Spawn data is edited through
  raw arrays, and the Inspector discards those edits because
  `serializedObject.Update()` runs immediately before
  `ApplyModifiedProperties()`.
- **The map model cannot describe a run.** `TileType` contains `Exit` and
  `Treasure`. `MapData.exitPos` holds one coordinate, and `GameSession` reads
  exits and treasures from terrain values. Multiple exits, destinations, and
  data-driven treasure are not representable.
- **There is no run state.** `GameSession.StartLevel` resets gold to zero,
  rebuilds the deck from `RunConfig.startingDeck`, and takes max HP from the
  config. `RunState` exists but is unused. A transition cannot carry HP, gold, the
  owned deck, or claimed cards.
- **The documented failure model no longer matches the design.** `Game-Design.md`
  and `README.md` say HP 0 ends the run. The design now resets the level to its
  entry state and keeps the run going.
- **The docs and code disagree on treasure.** The docs say treasure gives one or
  two random cards. The code grants authored `CardData[]` entries, and the QA map
  has empty arrays, so treasure grants nothing.
- **Map size, rewards, and progression are stale or missing.** The docs promise
  38x38 maps, a between-map pack and bonus, and a pack reroll. The design needs
  8x32 maps, a `Card3Tuple` reward pool, and per-option rerolls.

The proposed solution is a single authoring surface, a single runtime map
resolver, one run state owner, a documented failure model, and a matching
documentation update. The work is staged so the runtime object layer and failure
model are proven before the window is built.

## Goals

1. **Author maps in a dockable window.** Create, open, name, and edit `MapData`
   assets with X and Y sliders from 4 to 64, 8x8 and 12x12 presets, non-square
   sizes, and a single size source used by the tool, runtime, and docs.
2. **Paint two layers.** A terrain layer of `Floor`, `Hole`, `Damaging`, and
   `Wall`, and an object layer with one player start, numbered exits, `EnemyData`
   spawns, and `TreasureData` treasures. Object markers sit on Floor cells, one
   marker per cell. `Sand` remains runtime-only.
3. **Make painting fast and recoverable.** Click-drag painting with line
   interpolation, one Undo entry per stroke, erase, bulk fill, a rule for terrain
   painted under a marker, per-object list and delete, zoom and pan, hover cell
   readout, a legend, keyboard shortcuts, persisted tool state, and save/dirty
   handling.
4. **Move exits and treasures into the object layer.** `MapData` v2 stores a flat
   terrain array, one player start, `ExitSpawn[]` with destinations, enemy spawns,
   treasure spawns with `TreasureData`, and a `Card3Tuple[]` reward pool. It adds
   `schemaVersion` and `mapName`.
5. **Define destination data once.** An exit destination is a prefab, a
   `MapData`, or a `MapData` plus `ThemeData`. A prefab destination exposes a
   `MapView` component with a `MapData` reference. The prefab is a visual shell;
   `MapData` drives gameplay.
6. **Prototype the run transition.** The player stays in the same scene. Standing
   on an exit shows a persistent Take Exit button. Taking it heals 1 HP, opens a
   reward screen, runs a loading overlay for at least one second, and starts the
   destination map. HP, max HP, gold, owned cards, claimed cards, and level number
   carry. The torch resets, the deck restocks and shuffles, the hand resets to 5,
   and the free reroll resets. The old map state is discarded.
7. **Add a data-driven reward screen.** Each map owns a pool of at least six
   `Card3Tuple` entries. Three distinct tuples are shown. Each option has one
   reroll that must return a tuple not already shown. The player picks one option
   and receives all three cards. Tuple identity is an unordered multiset. There
   is no meta reward stage yet.
8. **Fix the failure model.** HP 0, deck exhaustion, and torch burnout reset the
   current level to its entry state. The run does not end. The docs must say this.
9. **Add soft validation and runtime guards.** The editor logs non-blocking alerts
   for missing starts, missing exits, missing destinations, out-of-bounds
   markers, duplicate coordinates, markers on non-Floor cells, invalid tile array
   lengths, null references, reward pools below six, identical tuples, and
   destination problems. The runtime logs and stays on the current map instead of
   throwing.
10. **Preview in the scene.** An edit-mode preview rebuilds only a generated
    subtree from `MapData` plus an optional `ThemeData`, preserves manual
    decorations under a separate parent, and disables itself in play mode.
11. **Reset the QA content and the docs.** Rebuild `QA_Level` on the new schema,
    re-link the QA scene, retire or guard `QaSceneBuilder`, update the tests, and
    update `Game-Design.md`, `Architecture.md`, `Roadmap.md`, and `README.md`.
12. **Keep the final document clean and organized.** The design must be readable
    as a standalone document, with consistent headings, defined terms, and no
    stale contradictory rules.

## Non-Goals

- Migrating old map assets. The old maps are deleted and `QA_Level` is rebuilt.
- Authoring 38x38 maps. The docs change to 4 to 64.
- Meta upgrades after the reward screen. The stage is skipped until defined.
- The end-of-run screen, boss defeat, and run completion. Deferred and custom.
- Per-spawn treasure overrides. One `TreasureData` asset grants the same reward at
  every placement.
- Rarity as a gameplay or loot field. Marker codes are display only.
- Returning to a visited map. Reusing a map asset resets it.
- Import or export to Tiled, CSV, or JSON.
- Multi-user or multi-window editing of one asset.
- Decoration authoring inside the map builder. Decorations remain scene-side.
- Production implementation code in this design phase. The draft may contain
  signatures, schemas, and pseudocode, but no production files.

## Open Questions

- **Dice across maps:** Do dice carry as run state, or reset to
  `RunConfig.startingDice` at each map? The plan assumes they carry.
- **Run terminal:** The run has no success or failure terminal until the custom
  end-of-run map. Confirm that cycles and indefinitely reused maps are acceptable
  until then.
- **Map graph validation:** Should soft alerts warn on cycles, dead ends, and
  unreachable destinations, or is the graph entirely the level designer's
  responsibility?

## Critical User Journeys

### CUJ 1: Create and paint a map

- **Given:** No map asset exists.
- **When:** The designer creates a map named `Level_01`, sets 12x9, paints Floor,
  Hole, Damaging, and Wall, places a player start, two exits, three enemies, and
  two treasures, then assigns each exit a destination and adds six reward tuples.
- **Then:** `MapData` v2 is saved in the default map folder, the grid shows the
  markers and codes, soft alerts list any problems, the scene preview rebuilds,
  and reopening the asset restores the same state.

### CUJ 2: Take an exit and select a reward

- **Given:** The player is standing on exit 1 of a map with at least six reward
  tuples.
- **When:** The player clicks the persistent Take Exit button.
- **Then:** The player heals 1 HP (capped at max), the reward screen shows three
  distinct options of three cards each, each option can be rerolled once for an
  unseen tuple, the player picks one option and receives all three cards, the
  loading overlay runs for at least one second, and the destination map starts.
  HP, max HP, gold, owned cards, claimed cards, and level number carry; the torch
  resets; the deck restocks and shuffles; the hand is 5; the free reroll resets.

### CUJ 3: Fail and reset the level

- **Given:** The player is inside a map.
- **When:** HP reaches 0, the deck empties, or the torch burns out.
- **Then:** The map resets to its entry state: entry HP, restocked deck and hand,
  free reroll, torch, enemies, and unopened treasure. The run continues. The docs
  describe the same rule.

### CUJ 4: Author exit destinations and rewards

- **Given:** A `MapData` asset is open in the builder.
- **When:** The designer adds an exit and chooses a prefab, a `MapData`, or a
  `MapData` plus `ThemeData`, then adds reward tuples.
- **Then:** The exit is auto-numbered and reorders on delete, the destination
  fields are shown for the chosen kind, a prefab without `MapView`/`MapData` logs
  a soft alert, and the reward pool warns below six tuples or on identical
  unordered tuples.

### CUJ 5: Preview a map

- **Given:** A `MapData` and an optional `ThemeData` are assigned to a scene
  object.
- **When:** The map changes or the designer presses refresh.
- **Then:** Only the generated subtree rebuilds, manual decoration objects remain,
  the preview is disabled in play mode, and the scene is not marked dirty when the
  content did not change.

--------------------------------------------------------------------------------

## Technical Plan (Architecture)

### 1. System shape

The design has four layers.

| Layer | Responsibility | Main types |
|-------|----------------|------------|
| Data | Serialized content and value types | `MapData`, `TreasureData`, `ThemeData`, `EnemyData`, `CardData`, `Card3Tuple`, `ExitSpawn`, `MapDestination` |
| Domain | Pure map, run, reward, and validation logic | `MapModel`, `GameSession`, `RewardSelection`, `MapValidator`, `GridMap`, `Pathfinder` |
| Presentation | Editor authoring, scene preview, runtime UI | `MapBuilderWindow`, `MapGridCanvas`, `MapView`, `TakeExitButtonView`, `RewardScreenView`, `LoadingOverlayView`, `RunHud` |
| Tests | Edit-mode and play-mode verification | `_Game.Tests` |

```text
MapBuilderWindow
  -> MapData v2 (asset)
       -> MapView preview (editor, generated subtree)
       -> MapModel (runtime snapshot)
            -> GridMap + markers + reward pool
            -> GameSession (run-scoped)
                 -> RunFlowState: Playing -> Reward -> Loading -> Playing
                 -> RewardSelection (pure)
                 -> UI: TakeExitButton, RewardScreen, LoadingOverlay, HUD
                 -> next MapData destination
```

The run-state owner is the promoted `GameSession`, as chosen by the owner.
`GameSession` survives map swaps and receives a new map through one level-entry
path. The class growth is contained with partial files and pure helpers:
`GameSession.LevelEntry.cs`, `GameSession.Transition.cs`, `GameSession.Reward.cs`,
and `GameSession.Failure.cs`. `RewardSelection`, `MapValidator`, and `MapModel`
stay pure C# so they can be tested without a scene.

### 2. Data model

#### 2.1 `MapData` v2

| Field | Type | Notes |
|-------|------|-------|
| `schemaVersion` | `int` | Value `3`. Older assets load with default Grid kind and no path layer. |
| `mapName` | `string` | Kept in sync with the asset filename. The filename is the source of truth. |
| `mapKind` | `MapKind` | `Grid` or `Linear`. Defaults to Grid for old assets. |
| `width`, `height` | `int` | 4 to 64. One `MapSizeLimits` source with 8x8 and 12x12 presets. |
| `tiles` | `TileType[]` | Flat row-major terrain. No exits or treasures. |
| `pathTiles` | `bool[]` | Linear path mask. Ignored by Grid maps. |
| `branchTiles` | `GridCoord[]` | Marked branch junctions on Linear maps. Merges are inferred, not stored. |
| `hasPlayerSpawn` | `bool` | Separates "unset" from a legal `(0,0)` start. |
| `playerSpawn` | `GridCoord` | Used only when `hasPlayerSpawn` is true. |
| `exits` | `ExitSpawn[]` | Number is the array index plus one and is display only. |
| `enemySpawns` | `EnemySpawn[]` | `{ GridCoord coord; EnemyData data; }`. |
| `treasureSpawns` | `TreasureSpawn[]` | `{ GridCoord coord; TreasureData data; }`. |
| `rewardPool` | `Card3Tuple[]` | At least six entries; soft alert below six. |
| `theme` | `ThemeData` | Optional. A destination may override it. |

`exitPos` is removed. `MapData.BuildModel()` replaces `CreateGrid()` and returns
a `MapModel` plus a `MapIssue[]`. It must not silently truncate a mismatched
tile array. The builder and runtime both call it.

#### 2.2 Terrain

`TileType` becomes terrain only, with explicit values pinned for future
serialization:

| Value | Type | Painter | Notes |
|-------|------|---------|-------|
| 0 | `Floor` | yes | Default. |
| 1 | `Hole` | yes | Grid: not walkable, jumpable. Linear: lethal to Walk; jumpable. |
| 2 | `Damaging` | yes | Walkable at 1 HP per tile. |
| 3 | `Wall` | Grid only | Not walkable; not jumpable. Rejected on Linear path tiles. |
| 4 | `Sand` | no | Runtime-only, created by MelowCart. |

`Exit` and `Treasure` are removed. The old numeric values are not reused for new
meaning in any surviving asset. Old maps are deleted, so there is no migration.

#### 2.3 Exits, destinations, and the prefab contract

```text
[Serializable] struct ExitSpawn {
    GridCoord coord;
    MapDestination destination;
}

[Serializable] struct MapDestination {
    DestinationKind kind;   // Prefab | Map | MapAndTheme
    GameObject prefab;
    MapData map;
    ThemeData theme;
}
```

- **Prefab:** `prefab` is required and must contain a `MapView` in its hierarchy.
  `MapView.mapData` supplies the gameplay data. `MapView.themeData` is optional.
- **Map:** `map` is required. No theme is used; asset sprites and color
  placeholders apply.
- **MapAndTheme:** `map` and `theme` are both required.
- A prefab is a visual shell. Its decorations live under a decoration root. The
  runtime builds gameplay from `MapData` under a generated root. `MapData` wins on
  play.
- Lookup uses `GetComponentInChildren<MapView>(true)`. If more than one exists,
  the first with a non-null `MapData` is used and a soft alert is logged.

`MapView` is the only prefab contract:

```text
MapView : MonoBehaviour {
    MapData mapData;
    ThemeData themeData;
}
```

#### 2.4 Treasure

```text
[Serializable] struct TreasureSpawn {
    GridCoord coord;
    TreasureData data;
}

TreasureData : ScriptableObject {
    string shortCode;
    Sprite sprite;
    int gold;
    int heal;
    int cardCount;
    WeightedCard[] cards;
}

[Serializable] struct WeightedCard {
    CardData card;
    int weight;   // default 1
}
```

On open, `TreasureData` draws `cardCount` cards with replacement from the weighted
list, adds them to the run deck, grants `gold`, and heals up to max HP. The
treasure opens once; claimed cards are owned and survive a level reset. A
hand-edited asset with a null card, a non-positive weight, or a negative count
logs a soft alert and skips the invalid entry.

`EnemyData` gains `shortCode`.

#### 2.5 Reward pool

```text
[Serializable] struct Card3Tuple {
    CardData a;
    CardData b;
    CardData c;
}
```

- Identity is an unordered multiset of `CardData` asset identities. `(A, B, C)`
  equals `(C, B, A)`; `(A, A, B)` does not equal `(A, B, B)`.
- Duplicates inside one tuple are allowed. Identical tuples in the pool log a
  soft console error but are allowed.
- `RewardSelection` is a pure object: pool, shown options, ever-shown set,
  per-option reroll flags, and an injected `System.Random`.
- `Reroll(index)` replaces only that option with an unseen tuple. `Choose(index)`
  returns the chosen tuple. `CanReroll(index)` is false when the option already
  rerolled or no unseen tuple remains.
- The pool is owned by the map being exited. The destination's pool is not used.
- X below six logs a soft alert; the runtime shows the available options and
  disables rerolls when no unseen tuple remains.

#### 2.6 Theme

`ThemeData` is a runtime asset used by the preview and by destination setup:

| Field | Type | Notes |
|-------|------|-------|
| `terrain` | `TerrainVisual[]` | One per `TileType`, including Floor, Hole, Damaging, Wall. Sand uses a runtime fallback. |
| `playerMarker` | `Sprite` | Player start marker. |
| `exitMarker` | `Sprite` | Exit marker base; the number is text. |
| `enemies` | `EnemyVisual[]` | `{ EnemyData data; Sprite sprite; }`. |
| `treasures` | `TreasureVisual[]` | `{ TreasureData data; Sprite sprite; }`. |

Override lookup is `theme entry -> asset sprite -> color placeholder`. Duplicate
or missing keys log a soft alert. `ThemeResolver` is the single lookup used by the
builder, preview, and runtime.

### 3. Runtime flow and state ownership

#### 3.1 Run-scoped `GameSession`

`GameSession` owns both run and level state after the owner decision.

| Scope | Fields |
|-------|--------|
| Run | current HP, max HP, gold, run deck (owned cards), claimed cards, dice set, level number |
| Level | current `MapData`, `MapModel`, `GridMap`, enemies, player position, hand, spent pile, level deck, opened treasure, torch, free rerolls, `RunFlowState` |

`EnterLevel(LevelEntryContext)` is the only level-entry path. `StartLevel`,
`RetryLevel`, failure reset, and transition call it with different flags.

```text
[Serializable] enum EntryReason { RunStart, Transition, Retry, FailureReset }

LevelEntryContext {
    MapData map;
    EntryReason reason;
    bool carryHp;
    bool restockDeck;
    bool resetTorch;
    bool keepOpenedTreasure;
    bool incrementLevel;
}
```

#### 3.2 Level entry

1. Resolve `MapModel` from `MapData`. If it is invalid, log and abort. On a
   transition, the old level stays active.
2. Build the `GridMap`, player, enemies, and treasure markers.
3. Build the level deck. Restock means: start from the run deck, add the chosen
   reward tuple if this is a transition, shuffle once, and draw 5. Retry and
   failure reset restock from the run deck and draw 5.
4. Reset the torch. It remains idle while `RunFlowState` is `Reward` or `Loading`.
5. Apply HP: carry for a transition; entry HP for a failure reset; run start HP
   for a new run.
6. Reset the free reroll.
7. Keep opened treasure on retry and failure reset. A transition starts a fresh
   destination map.
8. Increment the level number only on a transition.

#### 3.3 Failure reset

HP 0, deck exhaustion, and torch burnout all call
`EnterLevel(FailureReset)` with the same map. The result is entry HP, a restocked
level deck from the run deck, a 5-card hand, a reset free reroll, a reset torch,
respawned enemies, and unopened treasure. Claimed treasure remains owned because
it is already in the run deck. The level number does not change. The run does not
end.

Deck exhaustion is still a real failure inside a map. The restock happens only at
map entry, never during play.

#### 3.4 Take Exit and reward flow

1. `TakeExitButtonView` shows while the player stands on an exit and the phase is
   `Playing`. The torch keeps ticking.
2. Clicking the button calls `GameSession.RequestExit(exit)`.
3. The destination is resolved and validated before any state changes. On failure,
   log, keep the player on the map, and grant no heal, reward, or restock.
4. On success, heal 1 HP capped at max, set `RunFlowState.Reward`, and idle the
   torch.
5. `RewardSelection` shows three tuples. The player may reroll each option once for
   an unseen tuple, then selects one option and confirms. The chosen three cards
   are added to the run deck.
6. Set `RunFlowState.Loading`. Show a rotating loading icon for at least one second
   and until the destination is ready. The torch stays idle and input is blocked.
7. Dispose the old level view and state. Resolve the destination. Call
   `EnterLevel(Transition)` with carried HP, max HP, gold, run deck, claimed cards,
   and level number plus one.
8. The destination starts with a restocked and shuffled deck, a 5-card hand, a
   reset free reroll, and a fresh torch. Set `RunFlowState.Playing`.

Crossing an exit during a Walk path does not show the button. The button appears
only when the player is standing on the exit after movement resolves.

#### 3.5 Torch and terminal rules

| Phase | Torch | Input | Exit button |
|-------|-------|-------|-------------|
| Playing | ticks | cards, drag, exit button | visible when the player stands on an exit |
| Reward | idle | reward screen only | hidden |
| Loading | idle | blocked | hidden |
| Failure reset | reset | blocked during reset | hidden |

There is no run-over state. A level reset is the only terminal for a level. Damage,
treasure, and enemy contact resolve on entry before the player can press Take Exit.
A reward screen opens only after the destination validates.

### 4. Map resolution, validation, and runtime guards

`MapModel` is a pure snapshot of a `MapData`: terrain lookup, player start, exits,
enemy spawns, treasure spawns, reward pool, and theme. It exposes
`TryGetExitAt`, `TryGetTreasureAt`, and `GetEnemiesAt`. The builder, preview,
validator, and `GameSession` all consume the same model.

`MapValidator` returns structured issues:

```text
MapIssue {
    Severity severity;   // Info | Warning | Error
    string code;         // stable rule id
    UnityEngine.Object asset;
    GridCoord? coord;
    string message;
}
```

Rules:

- no player start or more than one start,
- zero exits,
- an exit without a destination,
- a prefab destination without `MapView`/`MapData`,
- destination without a player start,
- markers outside the grid,
- duplicate marker coordinates,
- a marker on a non-Floor cell,
- `tiles.Length != width * height`,
- null `EnemyData`, `TreasureData`, `CardData`, or `ThemeData`,
- reward pool below six,
- identical reward tuples,
- a reward tuple with a null card,
- treasure weights at or below zero, or a negative card count.

Linear-only rules:

- `pathTiles.Length != width * height`,
- a path tile marked Wall or Sand,
- a marker or overlay stacked on a non-Floor path tile,
- a marker off the painted path,
- the start is missing, off the path, not a path endpoint, or also an exit,
- a path dead end other than the start has no exit,
- a path tile cannot reach a terminal exit through the inferred forward flow,
- a junction with multiple forward paths has no branch marker,
- a branch marker has fewer than two forward options or sits on a merge,
- two adjacent path tiles create an ambiguous flow or a directed cycle,
- a Linear treasure or reward pool contains a complex card.

The window shows the issues in a panel and writes them to the console. The runtime
logs the same issues and stays on the current map. Guards cover every coordinate
read: bounds, null, missing player start, and unresolved destination.

### 5. Editor tool

`MapBuilderWindow : EditorWindow` is the only authoring path. It is dockable and
has five regions:

| Region | Contents |
|--------|----------|
| Toolbar | map asset, name, X/Y size, save, validate, preview refresh |
| Palette | terrain brushes, path, branch, player, exit, enemy, treasure, erase |
| Canvas | zoomable grid with hover, click-drag, line interpolation |
| Inspector | selected marker, exit destination, reward pool, theme, alerts |
| Status | coordinates, counts, active brush, last alert |

`MapGridCanvas` draws the grid itself instead of using one button per cell. It
supports zoom, pan, hover, click-drag, line interpolation between cells, and
rasterized strokes. One stroke is one Undo group. Erase and right-click erase are
supported. A terrain brush on a cell that holds a marker is blocked with a status
message and an alert; the marker must be deleted explicitly. This prevents silent
data loss.

`PaintTileType` is a separate enum or filtered list that excludes `Sand`, `Exit`,
and `Treasure`. The object brush places one marker per cell. The exit editor shows
the derived number, the destination kind, and the destination fields. It detects a
prefab's `MapView` and shows whether `MapData` and `ThemeData` were found. The
reward editor lists `Card3Tuple` entries, each with three `CardData` fields, and
shows the tuple count, duplicate-tuple alerts, and the below-six warning.

Resize uses numeric fields and sliders from 4 to 64, with 8x8 and 12x12 presets. A
shrink that pushes player, exit, enemy, or treasure markers out of bounds opens a
confirmation with two choices: delete the affected markers or block the resize.
Resize and marker repair are one Undo group. `MapSizeLimits` feeds the window,
`MapData`, the validator, and the docs.

Asset creation uses default folders `Assets/_Game/Data/Maps`,
`Assets/_Game/Data/Treasures`, and `Assets/_Game/Data/Themes`. A new map template
contains one player start and one exit. The asset filename is the source of truth;
`mapName` is synchronized through `AssetDatabase`. Rename goes through
`AssetDatabase` so GUID references survive. Overwrite and duplicate names require
confirmation.

`MapDataEditor` is reduced to a lightweight Inspector that opens the window and
shows read-only debug fields. The old grid and spawn property fields are removed,
so there is only one writer of `MapData`.

### 6. Scene preview and prefab destinations

`MapView` is runtime-safe and can render in edit mode. An editor-only
`MapPreviewDriver` subscribes to `Undo.undoRedoPerformed` and asset refresh,
debounces rebuilds, and calls `MapView.Rebuild`. The preview:

- rebuilds only a generated root marked by `MapViewGeneratedRoot` and per-cell
  `MapTileView` components,
- leaves manual decorations under a separate `MapDecorationRoot` untouched,
- marks generated objects with `HideFlags` or strips them before save,
- disables itself in play mode so `QaBootstrap` does not double-render,
- refreshes only when a map or theme content hash changes,
- uses the same coordinate conversion and `ThemeResolver` as the runtime.

A prefab destination is a visual shell with `MapView`. The runtime resolves its
`MapData`, builds gameplay under the generated root, and preserves prefab
decorations. The prefab's `MapView.mapData` must be the same asset the builder
previews; a mismatch logs a soft alert. `QA_Level` uses color placeholders and no
`ThemeData`.

### 7. UI surfaces

The runtime UI adds four surfaces:

| Surface | Behavior |
|---------|----------|
| `TakeExitButtonView` | Appears over the exit marker while the player stands on it. Torch keeps running. Click opens reward selection. |
| `RewardScreenView` | Three options of three cards. One reroll per option. Select one, then confirm. No skip. No meta stage. |
| `LoadingOverlayView` | Rotating icon, at least one second and until the destination is ready. Blocks input. |
| `RunHud` | HP, max HP, gold, deck count, torch, level number, and a short post-arrival summary. |

Reward cards use a new `RewardCardView`; the existing `CardSelection` is not reused
because it deduplicates ids and supports one selection. Input blocking is a single
gate owned by `RunFlowState`, not per-view flags.

### 8. Assembly boundaries and file ownership

| Assembly | Owns | Must not contain |
|----------|------|------------------|
| `_Game.Runtime` | Data assets, `GameSession`, `MapModel`, `MapValidator`, `RewardSelection`, `MapView`, UI views | `UnityEditor` calls without `#if UNITY_EDITOR` |
| `_Game.Editor` | `MapBuilderWindow`, `MapGridCanvas`, `MapPreviewDriver`, asset factory, alert panel, lightweight Inspector | Runtime-only build references |
| `_Game.Tests` | Edit-mode and play-mode tests | Production logic |

`TreasureData`, `ThemeData`, `Card3Tuple`, `MapDestination`, and `MapView` live in
`_Game.Runtime` because the editor assembly only references the runtime assembly.
The preview driver and any `AssetDatabase` or `Undo` use live in `_Game.Editor`.

### 9. QA reset and content

1. Build the new `QA_Level` v2 and a two-map transition smoke scene first.
2. Update `QaBootstrap`, `QaTargetingController`, and `QaSceneBuilder` to the new
   model. Retire `QaSceneBuilder` or rewrite it to target
   `Assets/_Game/Scenes/GameSessionQA.unity` and never overwrite an existing map.
3. Re-link `GameSessionQA.unity` to the new `QA_Level`.
4. Delete the old maps. There is no migration.
5. `QA_Level` uses no `ThemeData`; the preview and runtime use color placeholders.
6. Add a "Play this map" and "start at this exit" debug entry for transition tests.

### 10. Trade-offs and risks

- **Promoted `GameSession`:** the class grows. Use the partial files and pure
  helpers in section 1, and keep event subscription and disposal in the
  transition path. Test two transitions in a row for single event delivery.
- **New UI surfaces:** the reward screen, Take Exit button, and loading overlay
  are new work; the project currently has only the QA hand UI.
- **Soft alerts:** they do not prevent crashes from hand-edited assets. The
  runtime guards in section 4 are mandatory.
- **No run terminal:** cycles and reused maps can be farmed until the custom
  end-of-run map exists. This is accepted for now.
- **Theme drift:** per-asset overrides are a second visual source. The
  `ThemeResolver` and validator reduce drift but do not remove it.
- **Performance:** 64x64 painting and preview rebuilds need a measured target.
  Update dirty cells, cache marker lookups, and debounce the preview.
- **QA reset:** deleting assets can break scene references. Build the new content
  first and re-link the scene in the same change.
- **Docs drift:** the design changes failure, treasure, exits, map size, and
  rewards. The docs update is part of the implementation, not a follow-up.

### 11. Resolved technical decisions

- **Run-state owner:** promoted `GameSession`, as chosen by the owner.
- **Destination model:** one discriminated `MapDestination` with a kind and its
  payload.
- **Prefab contract:** `MapView` with `MapData` and optional `ThemeData`;
  `MapData` drives gameplay and the prefab is a visual shell.
- **Builder:** a dockable window with a custom zoomable canvas, not a button grid.
- **Map name:** the asset filename is the source of truth; `mapName` is synced.
- **Soft alerts:** window panel plus console, with a jump-to-cell action.
- **Reward pick:** select one option, then confirm.
- **Treasure on failure:** claimed cards are already in the run deck and survive
  the level reset; the level deck is rebuilt from the run deck.
- **Level number:** increments by one on each transition.
- **Dice:** assumed to carry as run state; see open questions.

### 12. Open technical questions

- **Dice across maps:** carry or reset to `RunConfig.startingDice`?
- **Run terminal:** is the absence of a success or failure terminal acceptable
  until the custom end-of-run map exists?
- **Map graph validation:** should soft alerts cover cycles, dead ends, and
  unreachable destinations, or is the graph entirely the level designer's
  responsibility?

--------------------------------------------------------------------------------

## Alternatives Considered

### Alternative A: Migrate the old maps

- **Pros:** Preserves the existing QA asset and its GUID reference; no manual
  rebuild.
- **Cons:** Removing `Exit` and `Treasure` renumbers the serialized `TileType`
  array; `exitPos` becomes `exits[]`; inline treasure cards become
  `TreasureData`; the QA treasure arrays are empty. It also needs a schema
  version, a migration tool, a backup, and a test fixture.
- **Reason for rejection:** The only existing map is a QA fixture. Deleting and
  rebuilding it is cheaper and removes stale data. The design accepts a one-time
  content reset.

### Alternative B: A new `RunController` owns run state

- **Pros:** Clean separation; `GameSession` stays map-scoped; run logic is easy to
  test on its own.
- **Cons:** Adds a lifecycle owner, event wiring, a scene bootstrap, and a second
  state access path; more new code to coordinate.
- **Reason for rejection:** `GameSession` already holds most run fields. Promoting
  it avoids a second transition layer. Class growth is contained with partial
  files and pure helpers.

### Alternative C: A scene `RunBootstrap` owns run state

- **Pros:** Smallest change; the scene owns persistence; the session reads shared
  state.
- **Cons:** Ownership is split between the scene and the session; two truths are
  possible; a scene reload loses state; harder to test.
- **Reason for rejection:** One owner is required. The owner chose the promoted
  `GameSession`.

### Alternative D: Separate destination types

- **Pros:** Each destination kind exposes only its valid fields; compile-time type
  safety.
- **Cons:** Unity serialization of polymorphic destination types is awkward;
  consumers need type switches; adding a kind touches many files.
- **Reason for rejection:** One discriminated `MapDestination` with a kind and a
  payload is simpler and still validated.

### Alternative E: `[ExecuteAlways]`-only preview or a one-shot generated prefab

- **Pros:** Immediate feedback for `[ExecuteAlways]`; no editor driver for a
  one-shot generation.
- **Cons:** `[ExecuteAlways]` creates feedback loops, dirty scenes, domain-reload
  rebuilds, and double rendering with `QaBootstrap`. A one-shot loses live
  preview.
- **Reason for rejection:** Use a runtime-safe `MapView` plus a debounced
  editor-only driver. It disables in play mode and rebuilds only the generated
  subtree.

### Alternative F: Improve the Inspector or use Tilemap/Tile Palette

- **Pros:** Reuses the existing Inspector; Tilemap provides built-in painting,
  erase, fill, and a familiar artist workflow.
- **Cons:** A 64x64 grid does not fit an Inspector, and the current button matrix
  has no zoom, pan, or drag. Tilemap stores visuals, not gameplay logic, so it
  would need an importer and would create two sources of truth.
- **Reason for rejection:** A dockable window with a custom canvas is the logical
  authoring source. Tilemap can become a visual theme later.

### Alternative G: Use `TreasureData` for reward packs

- **Pros:** One reward asset; reuses the weighted draw, gold, and heal fields.
- **Cons:** Treasure rolls an unknown reward, so the player would pick a random
  roll instead of a known three-card pack. The pack choice is lost, and gold or
  heal inside a pack is odd.
- **Reason for rejection:** Rewards are known `Card3Tuple` packs. `TreasureData`
  stays for map treasure.

### Alternative H: Keep the yes/no exit popup

- **Pros:** Explicit confirmation; simple to implement.
- **Cons:** The prompt has no turn phase; the torch keeps running; saying No can
  strand the player on the exit; step-off and re-entry can soft-lock.
- **Reason for rejection:** A persistent Take Exit button is explicit,
  recoverable, and works while the torch runs.

### Alternative I: Hard-blocking validation

- **Pros:** Prevents invalid maps from shipping; no runtime guards needed.
- **Cons:** Blocks work-in-progress maps; the level designer owns correctness;
  hand-edited assets bypass the editor anyway.
- **Reason for rejection:** Soft alerts plus runtime guards. Save is not blocked.

### Alternative J: Ordered tuple identity

- **Pros:** Simple sequence comparison; order can carry meaning.
- **Cons:** `(A, B, C)` and `(C, B, A)` would be different packs, which contradicts
  the owner's rule; duplicate detection misses reorderings.
- **Reason for rejection:** Unordered multiset identity.

### Alternative K: Per-spawn treasure overrides

- **Pros:** Each treasure can vary its reward without cloning an asset.
- **Cons:** Duplicates reward data in the map; more editor UI; two sources of
  truth.
- **Reason for rejection:** One `TreasureData` asset grants the same reward at
  every placement.

### Alternative L: Keep HP 0 as the run end

- **Pros:** Higher stakes; permadeath; roguelike tension.
- **Cons:** Contradicts the owner's decision; a single mistake ends the run; there
  is no run terminal until the custom end map anyway.
- **Reason for rejection:** HP 0 resets the current level and the run continues.

### Alternative M: Keep 38x38 maps

- **Pros:** Matches the old docs; larger endgame layouts.
- **Cons:** Slow authoring and preview; pathfinding cost; no assets at that size;
  contradicts the 4-64 decision.
- **Reason for rejection:** The docs and the builder move to 4-64.

### Alternative N: Track visited maps and support backtracking

- **Pros:** Prevents farming; supports return paths.
- **Cons:** Adds run state; complicates the graph; the design already uses a
  one-way graph and resets a reused map.
- **Reason for rejection:** No return. Reusing a map asset starts it fresh.

--------------------------------------------------------------------------------

## Non-Functional Requirements

- **Performance:** A full 64x64 drag stroke paints without skipped cells and with one
  Undo entry. A 64x64 preview rebuild completes in under 100 ms on the development
  machine. A transition never renders the old and new maps at the same time.
- **Edit-mode safety:** The preview disables in play mode, does not mark the scene
  dirty when content is unchanged, and does not run during compile, import, or
  domain reload. Generated objects are not saved as scene content.
- **Determinism:** Reward selection, dice, deck shuffle, and treasure draws use the
  injected `System.Random` stream. `UnityEngine.Random` is not used for game logic.
  Tests seed the stream.
- **Data safety:** `MapData` and the data assets are immutable at runtime. The builder
  records one Undo group per stroke and never silently deletes markers. A mismatched
  tile array produces an alert, not a silent truncation.
- **Rollout and rollback:** There is no migration. The old maps are deleted after the
  new QA map and scene reference are verified. A Git checkpoint precedes the reset.
  Reverting restores the old code and assets and re-links the scene.
- **Accessibility:** Terrain and markers use a letter or glyph in addition to color.
  The palette is color-blind-safe, and the marker codes are readable at the minimum
  cell size.
- **Security and privacy:** Not applicable. The game is a local single-player build
  with no network or personal data.

## Additional Resources

- `map_builder_concepts_brief.md` — the predecessor brief for this design.
- `Docs/Game-Design.md` — current game rules, terrain, treasure, enemies, and map
  sizes.
- `Docs/Architecture.md` — current data model, assemblies, and test strategy.
- `Docs/Roadmap.md` — milestone and status context.
- `README.md` — player-facing description of the loop.
- `Docs/2026-11-10_DRIFT_REPORT.md` — the code-versus-docs audit that motivated this
  work.
- Unity 6000.6.8f1, UGUI, TextMesh Pro, and the Unity Test Framework.

## Future Improvements

- The end-of-run map, boss defeat, and run completion screen.
- The meta upgrade stage after the reward screen.
- A map catalog, run length, and progression graph tooling.
- Tilemap or Tile Palette visual import for artists.
- Per-spawn treasure overrides if shared `TreasureData` assets become too rigid.
- Soft map-graph validation for cycles, dead ends, and unreachable destinations.
- The dice progression schedule.
- Run save and load.

--------------------------------------------------------------------------------

## Detailed Implementation Plan

### 1. Implementation sequence

1. Add the test package and test assembly. Freeze `MapData` v2 and the shared value
   types. Compile.
2. Replace `TileType`, `MapData`, `GridMap`, and `Tile` with the terrain-only
   model, `MapModel`, and `MapValidator`. Compile.
3. Promote `GameSession` to run scope and add the entry, failure, transition, and
   reward partials. Compile.
4. Add the runtime UI views, prefabs, and `RunFlowController`.
5. Add the dockable map builder, canvas, asset factory, preview driver, and
   soft-alert panel.
6. Rebuild `QA_Level`, update the QA scripts and scene, and retire the old map
   generation path.
7. Add the EditMode and PlayMode tests.
8. Update the design docs and README.

### 2. Packages and assemblies

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Packages/manifest.json` | Modify | Add `com.unity.test-framework` so the new test assembly can run. The editor resolves the version for Unity 6 and records it in the lock file. |
| `Packages/packages-lock.json` | Modify | Regenerated by the package manager when the test framework is added. Commit the resolved lock. |
| `Assets/_Game/Tests/_Game.Tests.asmdef` | Create | New test assembly. References `_Game.Runtime` and `_Game.Editor`, includes the Editor platform, and uses the `UNITY_INCLUDE_TESTS` constraint. |

All new files and folders get Unity `.meta` files. Commit them.

### 3. Runtime data and domain files

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Assets/_Game/Scripts/Grid/TileType.cs` | Modify | Terrain-only enum with pinned values: `Floor=0`, `Hole=1`, `Damaging=2`, `Wall=3`, `Sand=4`. Remove `Exit` and `Treasure`. `Sand` stays runtime-only. |
| `Assets/_Game/Scripts/Grid/MapData.cs` | Modify | `MapData` v3: `schemaVersion`, `mapName`, `mapKind`, `width`, `height`, `tiles`, `pathTiles`, `branchTiles`, `hasPlayerSpawn`, `playerSpawn`, `exits`, `enemySpawns`, `treasureSpawns`, `rewardPool`, `theme`. Contains `PathGraph` for Linear flow, branch detection, inferred merges, and terminal exits. |
| `Assets/_Game/Scripts/Grid/MapModel.cs` | Create | Pure snapshot of terrain, player start, exits, enemies, treasures, reward pool, and theme. Adds `TryGetExitAt`, `TryGetTreasureAt`, and `GetEnemiesAt`. One model for runtime, preview, and validation. |
| `Assets/_Game/Scripts/Grid/MapValidator.cs` | Create | Pure validator returning `MapIssue[]` with stable rule ids. Covers bounds, start count, exit count, destinations, markers on non-Floor, duplicate coordinates, tile length, null references, reward count, identical tuples, and treasure weights. |
| `Assets/_Game/Scripts/Grid/MapSizeLimits.cs` | Create | One source for the 4-64 range and the 8x8 and 12x12 presets. Used by `MapData`, the builder, the validator, and the docs. |
| `Assets/_Game/Scripts/Grid/ExitSpawn.cs` | Create | Serialized `ExitSpawn`, `MapDestination`, and `DestinationKind`. One discriminated destination with a prefab, `MapData`, and `ThemeData` payload. |
| `Assets/_Game/Scripts/Grid/Card3Tuple.cs` | Create | Serialized three-card tuple plus the unordered-multiset comparer. The comparer is the only identity path used by validation, selection, and tests. |
| `Assets/_Game/Scripts/Grid/TreasureData.cs` | Create | `TreasureData` with `shortCode`, `sprite`, `gold`, `heal`, `cardCount`, and `WeightedCard[]`. Adds the weighted draw helper used by `GameSession`. |
| `Assets/_Game/Scripts/Grid/ThemeData.cs` | Create | `ThemeData` with terrain visuals, player and exit markers, per-enemy sprites, and per-treasure sprites as serializable pair arrays. |
| `Assets/_Game/Scripts/Grid/ThemeResolver.cs` | Create | Single lookup: theme entry, then asset sprite, then color placeholder. Used by the builder, preview, and runtime. |
| `Assets/_Game/Scripts/Grid/MapView.cs` | Create | Runtime-safe `MonoBehaviour` with `mapData` and `themeData`. Builds the generated gameplay root and supports prefab destinations. |
| `Assets/_Game/Scripts/Grid/MapViewMarkers.cs` | Create | Marker components `MapViewGeneratedRoot`, `MapTileView`, and `MapDecorationRoot`. Ownership is by component, not by object name. |
| `Assets/_Game/Scripts/Grid/GridMap.cs` | Modify | Add a factory from `MapModel` and keep bounds and terrain queries. Remove the `Exit` and `Treasure` walkability assumptions. |
| `Assets/_Game/Scripts/Grid/Tile.cs` | Modify | `IsTerrainWalkable` covers Floor, Sand, and Damaging only. Remove `Exit` and `Treasure`. Marker occupancy is resolved by `MapModel`, not by terrain. |
| `Assets/_Game/Scripts/Entities/EnemyData.cs` | Modify | Add `shortCode` for the map marker. |
| `Assets/_Game/Scripts/Cards/CardInstance.cs` | Modify | Add a run-scoped id reset or a run-owned counter so tests are deterministic and ids do not grow across runs. |
| `Assets/_Game/Scripts/Cards/Hand.cs` | Modify | Reset `_order` in `Clear` so hand order is deterministic after a restock. |
| `Assets/_Game/Scripts/Core/RunConfig.cs` | Modify | Add `MapData startMap` and the starting level number. Keep the starting deck, dice, max HP, hand size, free rerolls, and torch seconds. |
| `Assets/_Game/Scripts/Core/RunState.cs` | Delete | The promoted `GameSession` owns run state. Delete the dead class so there is no second truth. |
| `Assets/_Game/Scripts/Turn/LevelSnapshot.cs` | Modify | Reduce to the level-entry snapshot used by failure reset: entry HP, entry max HP, free rerolls, and opened treasure. The run deck is owned by `GameSession`. |
| `Assets/_Game/Scripts/Turn/LevelEntryContext.cs` | Create | `EntryReason` and the level-entry context: map, carry HP, restock deck, reset torch, keep opened treasure, increment level. |
| `Assets/_Game/Scripts/Turn/RunFlowState.cs` | Create | `Playing`, `Reward`, and `Loading`. Used to gate input, torch ticks, and the Take Exit button. |
| `Assets/_Game/Scripts/Turn/RewardSelection.cs` | Create | Pure reward selection: pool, three shown options, ever-shown set, per-option reroll flags, injected `System.Random`. Enforces unseen replacements and the small-pool fallback. |
| `Assets/_Game/Scripts/Turn/GameSession.cs` | Modify | Promote to run scope. Remove the readonly map and the run-over path. Add run fields, `FlowState`, `EnterLevel`, `RequestExit`, reward APIs, and failure reset. Keep the existing turn and card logic. |
| `Assets/_Game/Scripts/Turn/GameSession.LevelEntry.cs` | Create | Partial file for `EnterLevel`, restock, snapshot capture, and HP and torch policy. |
| `Assets/_Game/Scripts/Turn/GameSession.Transition.cs` | Create | Partial file for destination resolution, Take Exit handling, heal 1, loading handoff, and level-number increment. |
| `Assets/_Game/Scripts/Turn/GameSession.Reward.cs` | Create | Partial file for the reward pool, reroll, confirm, and adding the chosen cards to the run deck before the restock. |
| `Assets/_Game/Scripts/Turn/GameSession.Failure.cs` | Create | Partial file for HP 0, deck exhaustion, and torch burnout. Each calls `EnterLevel(FailureReset)` and does not end the run. |
| `Assets/_Game/Scripts/Turn/RunFlowController.cs` | Create | Scene `MonoBehaviour` that owns the UI references, the loading coroutine, the input gate, and the bridge between `GameSession` events and the views. It does not own game state. |
| `Assets/_Game/Scripts/Turn/TorchTimer.cs` | Modify | Add an explicit reset and pause/resume path so the torch is idle during Reward and Loading and resets at level start. |

### 4. Runtime UI files

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Assets/_Game/Scripts/UI/TakeExitButtonView.cs` | Create | Shows a button over the exit marker while the player stands on it. Torch keeps running. Click calls `GameSession.RequestExit`. |
| `Assets/_Game/Scripts/UI/RewardScreenView.cs` | Create | Full-screen reward overlay with three options, three cards each, a per-option reroll, select-then-confirm, and no skip. |
| `Assets/_Game/Scripts/UI/RewardCardView.cs` | Create | A reward card view. Handles null art with a text fallback. Does not reuse `CardSelection`, which deduplicates ids and supports only one choice. |
| `Assets/_Game/Scripts/UI/LoadingOverlayView.cs` | Create | Rotating icon, minimum one second, waits for the destination, and blocks input. |
| `Assets/_Game/Scripts/UI/RunHud.cs` | Create | HP, max HP, gold, deck count, torch, level number, and a short post-arrival summary. |

### 5. Runtime UI prefabs

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Assets/_Game/Prefabs/UI/TakeExitButton.prefab` | Create | World-space or screen-space button bound to `TakeExitButtonView`. |
| `Assets/_Game/Prefabs/UI/RewardScreen.prefab` | Create | Canvas overlay with three `RewardCard` slots, reroll buttons, and a confirm button. |
| `Assets/_Game/Prefabs/UI/RewardCard.prefab` | Create | Card art, name, and disabled state used by `RewardCardView`. |
| `Assets/_Game/Prefabs/UI/LoadingOverlay.prefab` | Create | Full-screen dark panel with a rotating icon. |
| `Assets/_Game/Prefabs/UI/RunHud.prefab` | Create | HP, gold, deck, torch, and level labels. |

### 6. Editor tool files

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Assets/_Game/Scripts/Editor/MapBuilderWindow.cs` | Create | Dockable `EditorWindow` with toolbar, palette, canvas, inspector, and status. The only authoring path for `MapData` v2. |
| `Assets/_Game/Scripts/Editor/MapGridCanvas.cs` | Create | Custom zoomable grid with hover, click-drag, line interpolation, one Undo group per stroke, erase, and coordinate readout. |
| `Assets/_Game/Scripts/Editor/MapPreviewDriver.cs` | Create | Editor-only preview driver. Subscribes to Undo and asset refresh, debounces rebuilds, and calls `MapView.Rebuild`. |
| `Assets/_Game/Scripts/Editor/MapAssetFactory.cs` | Create | Creates maps, treasures, and themes in the default folders, applies the new-map template, and enforces unique names and overwrite confirmation. |
| `Assets/_Game/Scripts/Editor/MapAlertPanel.cs` | Create | Renders `MapIssue` entries with severity, message, and jump-to-cell. Writes the same issues to the console. |
| `Assets/_Game/Scripts/Editor/MapDataEditor.cs` | Modify | Reduce to a lightweight Inspector that opens the window and shows read-only debug fields. Remove the old grid and the broken property-field order. |
| `Assets/_Game/Scripts/Editor/QaSceneBuilder.cs` | Modify | Rewrite for the new schema and the committed scene path. Never overwrite an existing `QA_Level`; create it only when missing. |
| `Assets/_Game/Scripts/Editor/RunConfigEditor.cs` | Modify | Expose the new `startMap` and starting level number. |

### 7. QA and scene files

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Assets/_Game/Scripts/QA/QaBootstrap.cs` | Modify | Use the new schema and the run-scoped session. Delegate world construction to `MapView`. Wire the Take Exit, reward, loading, and HUD views. Keep the QA hand and dice controls. |
| `Assets/_Game/Scripts/QA/QaTargetingController.cs` | Modify | Resolve exits and treasures through `MapModel`, not `TileType.Exit` or `TileType.Treasure`. Coordinate with `TakeExitButtonView` and block targeting during Reward and Loading. |
| `Assets/_Game/Scripts/QA/ShapeSpriteFactory.cs` | Modify | Add marker glyphs and color placeholders for terrain, player start, exits, enemies, and treasures. Destroy runtime textures when they are no longer used. |
| `Assets/_Game/Scenes/GameSessionQA.unity` | Modify | Re-link the rebuilt `QA_Level`, add `MapView` and `RunFlowController`, and place the Take Exit, reward, loading, and HUD prefabs. This is the state-machine test scene and uses no `ThemeData`. |

`Assets/_Game/Scenes/Level_Castle.unity` is not changed in this phase. It is the
follow-up gameplay scene once the QA transition path is proven.

### 8. Data assets

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Assets/_Game/Data/Maps/QA_Level.asset` | Delete and recreate | Rebuild on `MapData` v2 with one player start, numbered exits with destinations, enemies, a `TreasureData` treasure, and a reward pool of at least six `Card3Tuple` entries. The old asset is deleted because the schema is incompatible. |
| `Assets/_Game/Data/Maps/QA_Level.asset.meta` | Delete and recreate | The new asset gets a new GUID. `GameSessionQA.unity` must be re-linked in the same change. |
| `Assets/_Game/Data/Treasures/Treasure_Default.asset` | Create | A sample `TreasureData` with a `shortCode`, a sprite fallback, gold, heal, card count, and weighted card entries. Gives the QA map and the builder a working treasure asset. |
| `Assets/_Game/Data/Themes/Theme_Default.asset` | Create | A sample `ThemeData` for terrain and marker visuals. Used to exercise the theme path in the builder and preview; `QA_Level` does not use it. |
| `Assets/_Game/Data/Enemies/Anchor.asset` | Modify | Add a unique `shortCode`. |
| `Assets/_Game/Data/Enemies/Archer.asset` | Modify | Add a unique `shortCode`. |
| `Assets/_Game/Data/Enemies/Brute.asset` | Modify | Add a unique `shortCode`. |
| `Assets/_Game/Data/Enemies/Sentry.asset` | Modify | Add a unique `shortCode`. |
| `Assets/_Game/Data/Enemies/Spitter.asset` | Modify | Add a unique `shortCode`. |
| `Assets/_Game/Data/Enemies/Stalker.asset` | Modify | Add a unique `shortCode`. |
| `Assets/_Game/Data/RunConfig_QA.asset` | Modify | Assign `startMap` to `QA_Level` and set the starting level number so the QA scene starts from the new model. |

### 9. Tests

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Assets/_Game/Tests/EditMode/TestMapFactory.cs` | Create | Builds temporary `MapData`, `TreasureData`, and `ThemeData` instances for tests. Keeps test maps out of the committed data folders. |
| `Assets/_Game/Tests/EditMode/MapDataTests.cs` | Create | Verifies `MapData` v2 defaults, `BuildModel`, the 4-64 limits, and the new-map template. |
| `Assets/_Game/Tests/EditMode/MapValidatorTests.cs` | Create | Covers every soft-alert rule, including missing start, zero exits, missing destination, prefab without `MapView`, out-of-bounds markers, duplicate coordinates, marker on non-Floor, tile-length mismatch, null references, reward count, and identical tuples. |
| `Assets/_Game/Tests/EditMode/Card3TupleTests.cs` | Create | Verifies unordered multiset identity, in-tuple duplicates, and ordered permutation equality. |
| `Assets/_Game/Tests/EditMode/RewardSelectionTests.cs` | Create | Verifies three distinct options, without-replacement selection, one reroll per option, unseen replacements, and the below-six fallback. Uses a fixed seed. |
| `Assets/_Game/Tests/EditMode/MapDestinationTests.cs` | Create | Resolves prefab, `MapData`, and `MapData` plus `ThemeData` destinations. Covers a prefab without `MapView`, a prefab with multiple `MapView`s, and a destination without a player start. |
| `Assets/_Game/Tests/EditMode/GameSessionEntryTests.cs` | Create | Verifies `EnterLevel` carries HP, max HP, gold, run deck, claimed cards, and level number on a transition; restocks and shuffles the deck; resets the hand, free reroll, and torch. |
| `Assets/_Game/Tests/EditMode/GameSessionFailureTests.cs` | Create | Verifies HP 0, deck exhaustion, and torch burnout reset the level to the entry state and never end the run. |
| `Assets/_Game/Tests/EditMode/ThemeResolverTests.cs` | Create | Verifies theme override, asset-sprite fallback, and color placeholder order. |
| `Assets/_Game/Tests/EditMode/MapBuilderTests.cs` | Create | Verifies one Undo per drag stroke, erase, resize with out-of-bounds markers, reward and exit editor validation, and the soft-alert panel. References `_Game.Editor`. |
| `Assets/_Game/Tests/PlayMode/TransitionSmokeTests.cs` | Create | Loads `GameSessionQA.unity`, stands on an exit, clicks Take Exit, verifies the heal, reward screen, per-option reroll, loading overlay of at least one second, destination map, carried state, reset torch, and level number increment. |
| `Assets/_Game/Tests/PlayMode/FailureResetSmokeTests.cs` | Create | Forces HP 0, deck exhaustion, and torch burnout and verifies the level reset and the continuing run. |
| `Assets/_Game/Tests/PlayMode/ExitButtonTests.cs` | Create | Verifies the button appears only while standing on an exit, the torch keeps ticking, crossing an exit during a Walk path does not show it, and clicking goes to reward selection. |

### 10. Documentation

| File | Action | Change and rationale |
|------|--------|----------------------|
| `Docs/Game-Design.md` | Modify | Replace the failure model with level reset, remove `Exit` and `Treasure` from the terrain list, add `TreasureData`, the `Card3Tuple` reward screen, exit destinations, the Take Exit button, the transition rules, and the 4-64 map size. Add the Grid/Linear map-kind section, branch/merge rules, lethal Linear holes, auto-target cards, and deck filtering. Remove the between-map pack and bonus text or mark it superseded. |
| `Docs/Architecture.md` | Modify | Update the folder map, data model, `GameSession` run scope, `MapModel`, `MapValidator`, `RewardSelection`, `MapView`, the new UI, the event flow, the assembly boundaries, the test strategy, and the scene paths. |
| `Docs/Roadmap.md` | Modify | Update the failure model, map size, M0-M5 status, the exit and reward work, and the QA scene status. |
| `README.md` | Modify | Update HP 0, treasure, exits, map size, and the reward and transition description. |

The historical documents `Docs/concepts_brief.md`, `Docs/Game-Loop-Audit.md`,
`Docs/Game-Loop-Decision-Points.md`, and `Docs/2026-11-10_DRIFT_REPORT.md` are
records of earlier rounds. They are not rewritten. `map_builder_concepts_brief.md`
remains as the predecessor to this draft.

### 11. Verification plan

**Compilation**

- Open the project in Unity 6000.6.8f1. `_Game.Runtime`, `_Game.Editor`, and
  `_Game.Tests` compile with no `error CS`.
- `GameSessionQA.unity` loads with no missing script or missing asset reference.

**EditMode tests**

- `MapDataTests`, `MapValidatorTests`, `Card3TupleTests`, `RewardSelectionTests`,
  `MapDestinationTests`, `GameSessionEntryTests`, `GameSessionFailureTests`,
  `ThemeResolverTests`, and `MapBuilderTests` pass.
- The tuple test proves `(A, B, C) == (C, B, A)` and
  `(A, A, B) != (A, B, B)`.
- The reward test proves a reroll never returns a tuple already shown.
- The entry test proves gold, HP, max HP, run deck, claimed cards, and level number
  survive a transition.
- The failure test proves HP 0, deck exhaustion, and torch burnout reset the level
  and do not end the run.

**PlayMode tests**

- `TransitionSmokeTests` passes on `GameSessionQA.unity`: Take Exit, heal 1, reward
  screen, reroll, confirm, loading overlay of at least one second, destination map,
  carried state, reset torch, hand 5, free reroll reset, level number increment.
- `FailureResetSmokeTests` passes for all three failure triggers.
- `ExitButtonTests` passes for visibility, torch ticking, crossing an exit without a
  trigger, and the click path.

**Manual checks**

- Create a 64x64 map in the builder. Paint a full drag stroke, erase a cell, undo the
  stroke once, and confirm the whole stroke reverts.
- Add an exit, assign each destination kind, and confirm the prefab `MapView`
  detection and the soft alert for a prefab without `MapView`.
- Add six reward tuples, confirm the count, add a duplicate tuple, and confirm the
  soft error is logged but the asset saves.
- Resize a map down with markers outside the bounds. Confirm the delete-or-block
  choice and the single Undo step.
- Preview a map with and without `ThemeData`. Confirm the generated subtree
  rebuilds, decorations remain, play mode does not double render, and the scene is
  not marked dirty when content is unchanged.
- Take an exit with the Take Exit button while the torch runs. Confirm the torch
  keeps ticking, the reward screen idles it, the loading overlay blocks input, and
  the destination starts with a reset torch.
- Trigger HP 0, deck exhaustion, and torch burnout. Confirm each resets the level and
  the run continues.

**Performance**

- A full 64x64 drag stroke paints without skipped cells and creates one Undo entry.
- A 64x64 preview rebuild completes in under 100 ms on the development machine.
- A transition never renders both the old and new map at the same time.

**Documentation checks**

- Search `Docs/Game-Design.md`, `Docs/Architecture.md`, `Docs/Roadmap.md`, and
  `README.md` for "HP 0 ends the run", "38x38", "TileType.Exit",
  "TileType.Treasure", and "pack and bonus". Resolve every match.
- Confirm the docs describe the 4-64 range, Grid and Linear map kinds, the path
  layer, branch markers, inferred merges, lethal Linear holes, the branch and
  exit prompts, Linear deck filtering, the new failure model, the exit
  destinations, the Take Exit button, `TreasureData`, `Card3Tuple`, and the
  transition carry rules.

### 12. Rollback and content reset

- Take a Git checkpoint before deleting the old maps and before the QA scene
  re-link.
- Build the new `QA_Level` and the transition fixture first. Delete the old map only
  after the new scene reference is verified.
- There is no migration path. Reverting requires restoring the old code and the old
  map assets from the checkpoint and re-linking the scene.
- The test maps are created at runtime by `TestMapFactory`; they are not committed
  assets, so tests do not depend on the deleted content.
- Keep the historical design records unchanged so the decision trail remains
  readable.
