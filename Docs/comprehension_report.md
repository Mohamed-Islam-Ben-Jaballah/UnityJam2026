# Goldfish Review — Stage 6: Comprehension Test

**Target:** `Docs/designs/map_builder.md` (full file)
**Standard:** `resources/document_standards/re-implementation-standard.md`

## Blockers

1. **No entry-flag matrix.** The document lists six `LevelEntryContext` booleans
   but never says which are true for `RunStart`, `Transition`, `Retry`, or
   `FailureReset`. `Retry` is never described, and the snapshot capture point is
   undefined. Add a reason → flags → HP source table.

2. **Reward cards are added twice.** §3.4 adds the chosen tuple to the run deck on
   confirm; §3.2 restock adds "the chosen reward tuple if this is a transition"
   again. Add once and define restock as "includes" the tuple.

3. **Exit entry is undefined.** With exits as Floor markers, entering an exit no
   longer completes the level. The document never says what happens to
   `CompleteLevel`, `LevelCompleted`, `TurnPhase.LevelComplete`, or `Win` when the
   player stands on an exit. State that only `RequestExit` starts the transition.

4. **Destination is resolved twice.** §3.4 resolves before state changes and again
   after the old level is disposed. A second failure leaves no active level. Resolve
   once, cache the result, and tear down only after `EnterLevel` succeeds.

5. **Theme ownership is contradictory.** `MapData.theme`, `MapView.themeData`, and
   `MapAndTheme` overlap. A `Map` destination says no theme is used, while a
   `MapAndTheme` destination needs an override without mutating `MapData`. Define
   one precedence chain.

6. **Multiple player starts cannot occur.** `MapData` v2 has one `playerSpawn` and
   `hasPlayerSpawn`. Remove the "more than one start" validator rule or explain the
   representation.

7. **Terminal precedence is undefined.** HP 0, deck exhaustion, torch burnout,
   damaging entry, treasure, and exit can co-occur. Define the total resolution
   order and where `RequestExit` sits.

8. **The transition test has no destination map.** Only `QA_Level` is created. Add
   a second map, a prefab destination, or a stated self-destination/reset rule.

9. **The compile gates are impossible.** Step 2 removes `TileType.Exit`/`Treasure`
   while `GameSession`, `MapDataEditor`, `QaBootstrap`, and `QaTargetingController`
   still reference them and are updated later. Reorder or merge the steps.

10. **Fewer than three distinct tuples is undefined.** A pool with zero, one, or
    two distinct tuples leaves no legal choice, and the reward screen has no skip.
    Define the fallback and gate Take Exit on a usable pool.

## Majors

- Map `MapIssue.Severity` to each rule and list stable rule ids. Define "soft
  alert" once.
- Define "invalid map" in `EnterLevel`, and define the run-start fallback when no
  old level exists.
- Name who instantiates and binds `TakeExitButtonView`, `RewardScreenView`,
  `LoadingOverlayView`, and `RunHud`. `RunFlowController`, `QaBootstrap`, and the
  scene currently all claim wiring.
- Resolve dice carry/reset. A run-scoped field cannot be an open question.
- Define whether claimed cards are a separate run field or merely a subset of the
  run deck. Treasure now joins the run deck immediately, contradicting
  `Game-Design.md` §4.5.
- Add the "paid reroll at 1 HP kills" rule to the failure-model doc update.
- Correct the problem statement: docs already say HP 0 resets. The code still
  implements run-over.
- Correct the baseline: `MapDataEditor` already has "Fill With Floor".
- Specify pan/zoom gestures, shortcuts, right-click behavior, bulk fill, legend,
  persisted state, and save/dirty behavior.
- State the Undo contract for destination, reward-pool, theme, and name edits.
- Specify resize anchoring, new-cell type, clamp behavior, and whether Undo
  restores deleted markers.
- Add validator rules for width/height bounds, zero dimensions, and
  `schemaVersion`.
- Specify destination payload precedence and null rules for each kind.
- Clarify that one-marker-per-cell is authoring-only; runtime allows layered
  occupancy and multiple enemy ids.
- Separate the edit-mode preview driver from the runtime `MapView`, and publish
  the full `MapView` API.
- Define Sand: base type or overlay, and how it changes `IsDamaging`.
- Define one RNG ownership and consumption order for rewards, dice, shuffle, and
  treasure.
- Define the relation between `TurnPhase` and `RunFlowState`, including failure
  reset.
- Define "destination ready" and who owns input blocking.
- Use `RunConfig.handSize` and `handCap`, not hardcoded 5 and 8.
- Pick one of each either/or: `PaintTileType` enum versus filtered list,
  `HideFlags` versus strip-before-save, id reset versus run counter.
- Define map-name editing and what "duplicate names" means.
- Define "marker codes", "rarity", "run graph", "entry state", "soft alert", and
  "run deck" versus "level deck".

## Nits

- `WeightedCard.weight` cannot default to 1 with plain Unity serialization.
- The concepts brief path is wrong; it is at the repository root.
- Define "first" `MapView` deterministically.
- Replace "8x32 maps" with "8 to 32 in each dimension".
- Specify the content-hash inputs.
- Name one validation entry point (`BuildModel` versus `MapValidator`).
