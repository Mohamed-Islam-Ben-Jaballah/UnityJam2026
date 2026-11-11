# Consolidated Goldfish Review Report

**Target:** `Docs/designs/map_builder.md`
**Mode:** Consolidated review, stages 5-9
**Date:** 2026-11-10

## Verdict

The design is not implementation-ready as written. The architecture is coherent,
but the document misdiagnoses the current failure-model drift, duplicates the
reward grant, tears down the old level before the destination is proven, and
carries a scope far beyond the first playable transition. The implementation
should proceed as a vertical slice with the corrections below, not as the full
file list in the draft.

## Blockers

### B1. The problem statement is stale
`Game-Design.md`, `Architecture.md`, `Roadmap.md`, and `README.md` already say HP 0
resets the current level. The code still implements run-over:
`GameSession.DamagePlayer` sets `_runOver`, `TurnPhase.RunOver` exists,
`RunEnded` fires, and `QaBootstrap` renders "RUN OVER". Rewrite the problem around
the code, not the docs. The remaining doc drift is the between-map pack/bonus,
`38×38`, and `Exit`/`Treasure` terrain rows.

### B2. The reward cards are added twice
The confirm step adds the chosen tuple to the run deck, and the restock step adds
it again. `LevelEntryContext` has no chosen-tuple field. Add the tuple once, in
the reward flow, and make restock read the run deck without adding it again.

### B3. The transition tears down before validation
The flow resolves the destination, disposes the old level, then calls
`EnterLevel`, which validates again. A failure after teardown leaves no active
level and a granted reward. Resolve and build the destination model first, keep
the old level alive until the new one is constructible, and define rollback.

### B4. `RunFlowState` and `TurnPhase` conflict
The design adds `Playing`/`Reward`/`Loading` but leaves `TurnPhase`,
`TurnPhase.RunOver`, `GameSession.IsRunOver`, `RunEnded`, and the QA "RUN OVER"
text in place. Pick one phase owner and remove the run-over path in the same
change. `TurnPhase` should carry `Reward` and `Loading`, or `RunFlowState` should
replace it.

### B5. Failure reset is not safe to call mid-resolution
HP 0, deck exhaustion, and torch burnout can call `EnterLevel` from inside
`DamagePlayer`, `DrawCards`, or the torch tick while a card is resolving. Queue
one post-action reset, guard re-entry, and define the priority when several
triggers occur together.

### B6. The reward pool can soft-lock the run
Three options plus one reroll each needs six distinct unseen tuples. Duplicates
reduce the usable count; a new map has no pool; the reward screen has no skip.
Require six distinct tuples, seed the template, or define a degraded path and
gate the Take Exit button on a usable pool.

### B7. Run deck, level deck, and claimed cards have no single owner
Treasure adds cards to the run deck, but the current level deck was built at
entry, so the card is not drawable in the map where it was found. `RunState` uses
`CardInstance`; `GameSession.RunDeck` returns `CardData`. Define one card
representation and one restock rule.

### B8. `MapView` has two incompatible roles
It is the prefab gameplay contract, the edit-mode preview renderer, and a
component that disables itself in play mode so `QaBootstrap` does not
double-render. Choose one runtime renderer and define what `MapView` does in
play mode.

### B9. The test assembly and destination content are not buildable
One Editor-only test asmdef cannot host PlayMode tests. The transition test has
no destination map asset. Add the exact asmdef contents and a second small map (or
a stated self-destination rule).

### B10. The compile gates are impossible
Step 2 removes `TileType.Exit`/`Treasure` while `GameSession`, `MapDataEditor`,
`QaBootstrap`, and `QaTargetingController` still reference them and are updated in
later steps. Reorder so each compile checkpoint is actually compilable.

## Major Findings

- `ThemeData` and the scene preview are a second visual pipeline beside
  `ShapeSpriteFactory` and `QaBootstrap`; defer or scope them explicitly.
- The builder canvas is over-built for 8-12 cell maps; ship paint, erase, resize,
  markers, exits, and reward authoring first.
- `QaBootstrap` and `RunFlowController` both claim the UI and event subscriptions.
  Use one adapter.
- `MapData.BuildModel` and `MapValidator` both produce issues. Make `MapValidator`
  the single issue producer.
- The validator has impossible rules ("more than one start"), missing width/height
  and `schemaVersion` rules, and destination null/precedence gaps.
- Destination kinds, theme precedence, and prefab `MapView` lookup are
  underspecified.
- Sand is both a base type and an overlay; define which one the builder and
  runtime use.
- RNG ownership for rewards, dice, shuffle, and treasure is undefined.
- Dice carry/reset is an open question but dice are listed as run state.
- The transition increments the level number and silently unlocks paid rerolls.
- The one-way graph has no terminal and no reachability warning.
- The reward economy has no deck-growth or healing analysis.
- The Alternatives section omits same-scene swap versus additive scene load.
- The document repeats the filename rule, prefab-shell rule, and no-migration rule
  in several places.
- Open Questions is duplicated at the top and in the Technical Plan.
- Several terms are undefined: "marker codes", "rarity", "run graph", "entry
  state", "soft alert", "owner", "promoted", "run deck" versus "level deck".
- The verification searches for strings that no longer exist ("HP 0 ends the run",
  "38x38") and will pass vacuously.
- The test plan does not define test seams for HP 0, deck exhaustion, torch
  burnout, canvas strokes, or the reward screen.
- `TorchTimer` already has `Start`, `Pause`, `Resume`, and `Reset`.
- `LevelSnapshot` is dead code but the plan keeps and expands it.
- `CardInstance` id reset is a hidden global mutation; use a run-owned counter or
  leave ids monotonic.
- `Hand.Clear` is never called; the proposed `_order` reset has no consumer.
- `QaSceneBuilder` writes the wrong scene path and overwrites the map.
- `map_builder_concepts_brief.md` is referenced without its repository path.

## Nits

- Replace the dangling "X below six" placeholder.
- CUJ 3 and §3.2 disagree on whether opened treasure is restored.
- "The owner chose" and "promoted" are unverifiable.
- "Free reroll" singular and "one reroll per option" are confused.
- "Soft console error", "soft alert", and `Error` severity are used for the same
  class.
- The `RunFlowState` enum omits failure reset.
- The 100 ms preview target is stated as both open and fixed.
- "MelowCart" and "Walk" are undefined.
- "Tile" and "cell" are used interchangeably.
- Heading capitalization and separators are inconsistent.

## Implementation Decision

Build a vertical slice, not the full plan:

1. **Data:** `MapData` v2 with terrain, one player start, exits with a `MapData`
   destination, enemy spawns, treasure spawns with `TreasureData`, and a
   `Card3Tuple` reward pool. Add `MapModel` and `MapValidator`.
2. **Runtime:** promote `GameSession` to run scope, add `EnterLevel`, fix HP 0 to
   reset the level, add the exit marker and Take Exit button, add the reward and
   loading flow, and remove the run-over path. Use one phase owner.
3. **Editor:** add a dockable map builder with 8-32 size controls, terrain and
   marker painting, exit destination editing, reward-pool editing, and soft
   alerts. Reuse the existing fill/clear behavior where possible.
4. **QA:** rebuild `QA_Level`, add one second small map for the transition test,
   update `QaBootstrap`, `QaTargetingController`, and `QaSceneBuilder`, and
   re-link the scene.
5. **Tests:** one EditMode assembly for the domain and one PlayMode assembly for
   the transition and failure reset.

Defer: full `ThemeData` overrides, prefab destination validation, decoration
preservation, zoom/pan, bulk fill, and the post-arrival summary. Add them after
the slice plays.
