# Goldfish Review — Stage 7: Critic Review

**Target:** `Docs/designs/map_builder.md` (FeatureRange lines 1-828)
**Reviewers:** design critic, structural, style
**Standard:** `resources/document_standards/feature-spec-standard.md`

## Blockers

1. **Transition disposes before destination validation.** §3.4 resolves the
   destination, then disposes the old level, then `EnterLevel` validates again. A
   failure after teardown leaves no active level and a granted reward. Resolve and
   build the destination model before any teardown, and define rollback after the
   reward is chosen.

2. **Reward cards are added twice.** Confirm adds the tuple to the run deck; the
   restock step adds it again. `LevelEntryContext` has no chosen-tuple field, so the
   entry path cannot know what to add. Add once and pass it explicitly.

3. **`RunFlowState` versus `TurnPhase` is unresolved.** `TurnPhase.RunOver`,
   `GameSession.IsRunOver`, `RunEnded`, and the QA "RUN OVER" text still exist. Two
   phase sources must stay in sync. Extend `TurnPhase` or make `RunFlowState` the
   only phase, and remove the run-over path in the same change.

4. **Failure reset re-enters while resolution is running.** HP 0, deck exhaustion,
   and torch burnout can call `EnterLevel` from inside `DamagePlayer`, `DrawCards`,
   or the torch tick while `ExecuteWalk` or `ResolveEntry` is iterating. Queue one
   post-action reset, guard re-entry, and define simultaneous-trigger priority.

5. **Failure reset lacks the entry-HP contract.** The level-state table has no
   entry-HP field, and `carryHp` does not supply the restore value. Record entry HP
   after the transition heal and before the destination starts.

6. **A reward pool with fewer than six distinct tuples soft-locks the run.** Three
   options plus six rerolls need six distinct unseen tuples. Duplicates reduce the
   usable count, a new map template has no pool, and the reward screen has no skip.
   Require six distinct tuples, seed the template, or define the degraded path.

7. **Run deck, level deck, and "claimed cards" have no single owner.** Treasure adds
   cards to the run deck, but the current level deck was built at entry, so the card
   is not drawable in the map where it was found, contradicting the docs. `RunState`
   uses `CardInstance`, `GameSession.RunDeck` returns `CardData`. Define the
   instance model, the level-deck derivation, and whether treasure mutates both.

8. **`MapView` has two incompatible roles.** It is the prefab gameplay contract, an
   edit-mode preview renderer, and a component that disables itself in play mode so
   `QaBootstrap` does not double-render. Choose one runtime renderer and define what
   `MapView` does in play mode.

9. **The test assembly and transition content are not buildable as written.** One
   test assembly cannot serve both EditMode and PlayMode. The transition test has no
   destination map asset.

10. **The problem statement is stale.** The docs already say HP 0 resets the level;
    the code still implements run-over. Rewrite the problem and scope the doc change
    to the remaining drift.

## Majors

- Theme precedence is undefined across `MapData.theme`, destination theme, and
  prefab `MapView.themeData`.
- `MapData.BuildModel` and `MapValidator` both return issues; define one pipeline.
- Define what happens when the current map is unloadable at run start or after a
  failure reset.
- The one-way graph has no terminal and no reachability check. A map whose only
  exit points to a dead end strands the run.
- The reward economy has no analysis: infinite deck growth, healing on every exit,
  no removal, and unlimited torch-idle reward/loading pauses.
- The Alternatives section omits same-scene swap versus additive scene load or a
  persistent run bootstrap, the decision most likely to break state ownership.
- Dice carry is an open question but is listed as a run field. `startingDeck` and
  `startingDice` construction are unstated.
- Transition increments the level number, which silently unlocks paid rerolls at
  the first transition. State the gameplay meaning.
- Several validator rules do not match the schema: multiple starts, missing
  width/height bounds, missing `schemaVersion`, missing authored `Sand` rejection,
  and missing destination null rules.
- The document uses present tense for both current and target behavior. Adopt a
  consistent "today" versus "after this change" convention.
- Several terms are undefined: "marker codes", "rarity", "run graph", "entry
  state", "soft alert", "the owner", "promoted", "run deck" versus "level deck".
- Goal 12 is a document-quality goal, not a product goal.
- The draft is about 45 files; it needs a vertical-slice cut.

## Structural

- Open Questions is duplicated at the top and as Technical Plan §12. Keep one.
- `### 10. Trade-offs and risks` is buried under the architecture H2; promote it to
  a top-level Risks & Mitigations section.
- Technical Plan mixes architecture, risks, decisions, and open questions under one
  heading. Split or rename it.
- Bare "section 1" and "section 4" references are ambiguous because the Detailed
  Implementation Plan reuses 1-12 numbering.
- Add a terminology section or define terms inline.
- Separator rules and heading capitalization are inconsistent.
- `ExitSpawn`, `MapDestination`, `TreasureSpawn`, `WeightedCard`, and `Card3Tuple`
  have struct blocks; `EnemySpawn` and the theme visual types do not.

## Style

- Replace the dangling "X below six" placeholder.
- CUJ 3 says the reset restores unopened treasure while §3.2 says opened treasure is
  kept. State the rule once in behavioral terms.
- "The owner chose" and "promoted" are unverifiable. Name the decision record and
  define "promoted".
- "Free reroll" singular and "one reroll per option" are confused. Define each.
- "Soft console error", "soft alert", "non-blocking alert", and `Error` severity
  describe the same class. Define the vocabulary once.
- `RunFlowState` lists three values but the table includes a failure-reset phase.
- 32x32 preview has both "needs a measured target" and "under 100 ms". Reconcile.
- "MelowCart" and "Walk" are undefined proper nouns/terms.
- "TreasureData draws cards" attributes an action to an asset. Say "when the player
  opens a treasure, the game draws..."
- Current and target behavior use the same present tense.
- Repetition of the filename rule, the prefab-shell rule, and the no-migration rule
  invites drift. Keep one normative statement each.
- "Tile" and "cell" are used interchangeably. Define the convention.
