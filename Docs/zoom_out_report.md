# Goldfish Review — Stage 5: Initial Zoom Out

**Target:** `Docs/designs/map_builder.md` (full file, lines 1-1060)
**Standard:** `resources/document_standards/zoom-out-standard.md`

## Findings

### 1. The problem statement is stale — Blocker
The document says `Game-Design.md` and `README.md` claim HP 0 ends the run. Those
docs now say HP 0 resets the level. The real drift is the code path:
`GameSession.DamagePlayer` sets `_runOver`, `TurnPhase.RunOver` exists,
`RunEnded` fires, and `QaBootstrap` renders "RUN OVER". Rewrite the problem
statement around the code, and remove the doc-rewrite work for a rule the docs
already state.

### 2. The plan is far larger than the root problem — Blocker
The root goal is "author a map and move to the next map." The plan adds a
dockable canvas with zoom, pan, interpolation, persisted state, a theme system,
prefab destinations, a scene-preview driver, four UI surfaces, two reward models,
14 validator rules, 13 test files, and about 45 new files. Stage a vertical slice
and defer the rest.

### 3. Several components are speculative — Major
- `ThemeData` and `ThemeResolver` duplicate the existing `ShapeSpriteFactory`
  and inline colors, and `QA_Level` does not use them.
- `MapView` prefab destinations have no existing prefab map asset.
- `MapModel` adds a third map representation (`MapData` → `MapModel` →
  `GridMap`) where `MapData.BuildGrid()` and `GridMap` already answer the queries.
- `RunFlowState` is a second state machine beside `TurnPhase`.
- `LevelSnapshot` is dead code; the plan keeps and expands it.
- The `TorchTimer` change is unnecessary; `Start`, `Pause`, `Resume`, and `Reset`
  already exist.

### 4. The builder canvas is over-built for 8-12 cell maps — Major
The plan promises zoom, pan, hover readout, click-drag interpolation, bulk fill,
erase, legend, keyboard shortcuts, persisted tool state, and per-stroke Undo.
Ship click-drag, erase, resize, and a marker list first. Re-evaluate the
hand-rolled canvas against the already-installed `com.unity2d.tilemap` package.

### 5. Presentation is duplicated — Major
`QaBootstrap` already owns the world, the hand, the dice, the HUD, and input.
`RunFlowController` becomes a second adapter for the same events, and `RunHud`
duplicates the status text. Promote `QaBootstrap` into the single adapter or
replace it, and extend the existing HUD.

### 6. Staging does not reduce risk — Major
All production code lands before the first runnable scene, and all tests land
last. Re-stage as verified slices: (a) `MapData` v2 plus `GridMap` plus validator
plus EditMode tests; (b) one map and one exit transition plus a PlayMode smoke
test; (c) builder window; (d) preview, theme, and reward polish.

### 7. Test and NFR scope is over-built — Major
The plan proposes separate Editor and PlayMode test assemblies in one assembly
that cannot run PlayMode tests in a player. It also sets an unmeasured 100 ms
preview target and a one-second loading minimum. Split the test assemblies, test
the pure domain first, and drop unmeasured targets until profiling exists.

### 8. Redundant serialized fields — Nit
`schemaVersion` has no migration, `mapName` duplicates the asset filename, and
`theme` duplicates the destination theme. Remove each until a second reader
exists.

### 9. Small-map fixtures — Nit
The 8-cell minimum blocks 2x2 and 4x4 test maps. Keep 8-32 as editor guidance,
not a domain invariant.

### 10. Dice is still an open question — Nit
The run field table lists dice as run-scoped, but the open questions leave carry
versus reset undecided. Resolve it or mark it level-scoped.

## High-Priority Recommendation

Cut the first implementation to a playable slice: one `MapData` v2, one exit to
one `MapData` destination, one reward grant, one failure reset, one QA scene.
Add the builder window next. Defer `ThemeData`, the scene preview, prefab
destinations, and the full validator until the slice plays.
