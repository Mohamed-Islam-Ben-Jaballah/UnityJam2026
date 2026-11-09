# 2026-11-10 Drift Report

**Scope:** design documents in `Docs/` plus `README.md`; implementation under
`Assets/_Game/`, `Assets/Plugins/FMOD/`, and `FMOD/`. Emphasis on files merged
from `origin/MazenUI` / `origin/SoundDesign` and scripts added by other
contributors (`QA/`, `Editor/`, `Board/`, `Lighting/`, `DiceSelectionGate`).

**Working-tree note:** `git status` currently reports 2,870 deleted paths (old
asset trees) with one untracked document. `HEAD` tracks both the old and the
reorganised asset paths, so duplicate asset trees still exist in history. Path
findings below describe the current working tree.

---

## README.md

- Treasure is specified as "1–2 cards" (lines 43-48, 105-118) but the code
  grants only the cards authored in `MapData.TreasureSpawn.cards`
  (`Assets/_Game/Scripts/Grid/MapData.cs:22-27`,
  `Assets/_Game/Scripts/Turn/GameSession.cs:1135-1150`). `QA_Level.asset` has
  `cards: []` on all three treasures, so treasure currently grants 0 cards.
- "Completing a level grants a pack" and the between-map pack/bonus flow
  (lines 48, 115-118) are not implemented anywhere in `Assets/_Game/Scripts`.
  `RunConfig` has no pack or bonus tables.
- "You start with one d6 and add more d6s on some maps" (line 36) has no
  implementation; `RunConfig.startingDice` is fixed and there is no per-map
  reward path.
- The preview description "Grey — unreachable" (lines 60-62) cannot be
  reproduced as a distinct state: `QaTargetingController.UpdateHover` uses one
  grey (`BlockedColor`) for both invalid targets and unreachable tiles, and it
  never displays the path HP cost (`QaTargetingController.cs:47-97`).
- Same reroll edge as `Game-Design.md`: the code blocks rerolling when
  `HasPlayableCard()` is true, but that method also requires a valid target
  (`GameSession.cs:159-178`), so a type-playable card with no legal target
  allows a reroll even though README line 36 says a playable card forces a play.

## Docs/Game-Design.md

- Treasure cards are not "1–2 random cards" (§2, §4.5, §6.5, §13). They are
  fixed authored arrays on `TreasureSpawn` and `QA_Level` supplies none.
- The between-map pack and bonus flow (§2, §7, §9.10, §9.12) is absent:
  no pack choices, no bonus choices, no option reroll, no removal reward, no
  extra-die schedule. `RunConfig` only has `startingDeck`, `startingDice`,
  `maxHp`, `handSize`, `handCap`, `freeRerollsPerMap`, and `torchSeconds`
  (`Assets/_Game/Scripts/Core/RunConfig.cs:10-19`).
- "Claimed treasure ... merge into the run deck on completion" and the
  `RunDeck` / `LevelSnapshot` model (§4.5, §5) are not implemented.
  `GameSession` only keeps `_levelStartDeckOrder`, mutates it when treasure is
  opened (`GameSession.cs:1147`), and rebuilds from it on retry
  (`GameSession.cs:1185`). `LevelSnapshot` is never referenced.
- The torch is specified to pause in the menu only (§6.4, §9.9). There is no
  pause/menu integration: `QaBootstrap.Update` calls
  `TickTorch(Time.unscaledDeltaTime)` every frame (`QaBootstrap.cs:63-68`), and
  `TorchTimer.Pause` is only called on run-over, level-complete, and retry.
- Path preview (§5.3) is incomplete: no HP-cost display, no slow-enemy
  telegraph overlay, and no distinct unreachable grey
  (`QaTargetingController.cs:47-97`, `202-232`).
- Jump distance is hardcoded to 2 in `GameSession.IsValidJump`
  (`GameSession.cs:297-352`) and ignores `CardData.jumpDistance` and
  `rangeValue`; the §4.4 card range model is not enforceable from data.
- `CardData.art` is null on all 12 card assets, so the planned
  `CardData.art` → `CardInfo.artwork` bridge would render blank cards
  (`Assets/_Game/Data/Cards/*.asset`).
- `MelowCart` is only considered playable when a hole, damaging tile, or
  non-flying enemy is adjacent (`GameSession.cs:985-1010`), while §4.3 presents
  it as a terrain reshape with no target requirement.
- Map size tiers 12×12 and 38×38 (§7, §8) have no map assets; only an 8×8 QA
  map exists (`Assets/_Game/Data/Maps/QA_Level.asset`).

## Docs/Architecture.md

- §3 folder map is stale:
  - `Interaction/` contains only `.gitkeep`; `DragDrop`, `TargetSelector`,
    `RangeHighlighter`, and `PathPreview` do not exist. `QA/QaTargetingController`
    is the only targeting code.
  - `Core/` has no documented event bus or scene-loading code; only
    `RunConfig.cs` and `RunState.cs` exist.
  - Scenes are under `Assets/_Game/Scenes/`, not `Assets/Scenes/`;
    `Assets/Scenes` does not exist. `Level_Castle.unity` is not documented.
  - `Assets/TextMesh Pro` is now `Assets/Resources/TextMesh Pro`, and
    `Assets/UI/Placeholders` is now `Assets/_Game/UI/Placeholders`. §3, §11, and
    §12 still use the old paths. `HEAD` also still tracks the old trees, so the
    duplicate-TMP risk in §11 is live in history.
- §3 assemblies line omits two references actually present in
  `_Game.Runtime.asmdef`: `Unity.RenderPipelines.Universal.Runtime` and
  `Unity.RenderPipelines.Universal.2D.Runtime` (required by
  `Lighting/TorchFlicker.cs`).
- §4 data model divergences:
  - `CardData` has no `params` struct and no `walkMultiplier`; it uses separate
    `damage`, `heal`, `drawCount`, and `jumpDistance` fields
    (`Cards/CardData.cs:22-37`). `rangeType` and `rangeValue` are stored but not
    read by gameplay logic.
  - `DiceData.sides` defaults to 4 (`Dice/DiceData.cs:8`) while §4 says 6 and
    Game-Design says d6 only. Only `Assets/_Game/Data/Dice/d6.asset` exists.
  - `EnemyData` has no animation reference (§4 lists "sprite / animation refs";
    only `sprite` exists, `Entities/EnemyData.cs:30-38`).
  - `RunConfig` has no `packChoices` or `bonusChoices`.
  - `MapData.exitPos` is authored but never read; runtime uses the Exit tile
    type instead (`Grid/MapData.cs:17-27`, `Turn/GameSession.cs:803-806`).
- §5 domain class drift:
  - `Dice` is implemented as `Die`; `Grid` is `GridMap`; there is no `RunDeck`
    class. `RunState.RunDeck` exists but is unused, and `GameSession.RunDeck`
    returns the level draw order (`GameSession.cs:58`), not a run-persistent
    collection.
  - `LevelSnapshot` exists but is never used; retry is rebuilt from
    `_levelStartDeckOrder` and `_entryHp` (`GameSession.cs:1169-1201`).
  - `Deck.DeckExhausted` is raised (`Cards/Deck.cs:33-45`) but never
    subscribed; exhaustion retry is handled inline in `GameSession.DrawCards`
    (`GameSession.cs:498-518`).
  - `Grid.IsWalkable(coord)` does not exist; occupancy is split across
    `Tile.EnemyIds/HasPlayer`, `EnemyData.BlocksMovement`, and
    `GameSession.BlocksWalk` (`Grid/Tile.cs:8-13`, `Turn/GameSession.cs:900-911`).
  - `Tile` keeps both `TileType.Sand` and the `HasSand` overlay
    (`Grid/Tile.cs:8-32`), the ambiguity the concepts brief warned against.
- §7 turn state machine names do not exist as phases: `RollDie`, `ChooseCard`,
  `Reroll`, `ResolveCard`, `NextDieOrEndTurn`, `DrawPhase`, and `TurnEnd` are
  collapsed into `AwaitingRoll`, `AwaitingCard`, and `Drawing`
  (`Turn/TurnPhase.cs`).
- §8 event flow diverges:
  - `RollEvent(number, unlocked types)` is `DiceRolled(int)`; unlocked types
    are never published (`GameSession.cs:62,155`).
  - Missing events: `HandChanged`, `DeckChanged`, `GridChanged` / `TileConverted`,
    `PlayerMoved`, `HealthChanged`, `EnemyTelegraphed`, `CardAdded`,
    `TorchChanged`, `Burnout` (not surfaced), `Win`, `Lose`, and
    `LevelRestart` (only `LevelRetried` exists). `EnemyAttacked` carries an
    `EnemyState`, damage, and timing instead of `(enemyId, timing)`.
  - The documented view flow (`TargetSelector`, `PathPreview`,
    `RangeHighlighter`) is absent; `QaTargetingController` is a QA stand-in.
- §8/§9/§12 card UI integration: the planned `HandPresenter` does not exist.
  The only `GameSession` ↔ hand integration is `QA/QaBootstrap` +
  `QA/QaCardView` + `QA/QaTargetingController`, which bypasses
  `CardSelection`, `HandView`, and `CardView` entirely rather than feeding
  `CardSelection.SetOffer`.
- §9 module table lists an Interaction module and an Audio module; neither
  exists as code/assemblies. `CardSelectionAudio` remains in `Cards` inside
  `_Game.Runtime`, so the §11 coupling risk is still real.
- §10 testing strategy has no implementation: there is no `_Game.Tests`
  assembly or test source file.
- §12 merge status still says scenes live at
  `Assets/Scenes/Cards_Dice_UI.unity` / `Assets/Scenes/SoundDesign.unity`
  (actual: `Assets/_Game/Scenes/`), and says the UI bridge is pending while a
  QA-only bridge now exists.

## Docs/Roadmap.md

- The M0–M5 checkboxes are all unchecked (`Roadmap.md:8-82`) while the
  implementation-status table (`Roadmap.md:88-96`) marks core simulation, the
  balancing assets, and most of M0–M4 as implemented.
- "Integration | Pending | HandPresenter bridge" (`Roadmap.md:94`) is stale:
  a bridge exists, but only as QA code, and it does not use `CardSelection`.
- "Next steps 1. Add a HandPresenter bridge" (`Roadmap.md:102`) is not done;
  step 5 (between-map meta) is also not done.
- Workflow paths are stale: `Assets/TextMesh Pro` (`Roadmap.md:147`) is now
  `Assets/Resources/TextMesh Pro`; the scene/`Assets/Scenes` references are also
  stale; `_Game.Runtime` additionally references URP.
- Repository hygiene contradicts the stated workflow: the working tree has
  2,870 uncommitted deletions and `HEAD` still tracks duplicate old/new asset
  trees, including two TMP trees.
- `Tests | Pending` (`Roadmap.md:95`) is accurate, but no test scaffold or
  assembly has been created toward it.

## Docs/Branch-Merge-Review.md

- The §8 integration plan was not implemented as written. There is no
  `HandPresenter`, no `CardInstance` → `CardInfo` mapping, no
  `CardSelection.SetOffer` feed, and no routing of `CardSelection.CardSelected`
  into `GameSession.PlayCard`. Instead, `QaBootstrap`/`QaCardView`/
  `QaTargetingController` implement a separate hand and drag path.
- `CardTargetingController` from §8 is not present; `QaTargetingController` is a
  QA-only equivalent.
- §8 says to disable `PlaceholderCardSource` in the gameplay scene; no gameplay
  scene uses it, and the sandbox scene still does. The sandbox scene contains
  four placeholder cards (`placeholder-01` … `placeholder-04`), not the three
  described in §3.
- Scene paths in §2/§7 (`Assets/Scenes/...`) are wrong; the scenes are under
  `Assets/_Game/Scenes/`.
- The §5 resolution text for `Docs/Architecture.md` lists InputSystem, UI, TMP,
  and FMODUnity, but the merged `_Game.Runtime.asmdef` also needs the two URP
  references listed above.
- §7 item 4 still describes the TMP tree as a single `Assets/TextMesh Pro`
  path; the working tree now has `Assets/Resources/TextMesh Pro` and `HEAD`
  still tracks the old tree as well.

## Docs/Game-Loop-Audit.md

- The code-status statement (`Game-Loop-Audit.md:5`) is false now:
  `Assets/_Game/Scripts` contains the core loop, QA, editor, board, and
  lighting code, not only `.gitkeep`.
- C1's resolution ("treasure 1–2 cards and the per-level pack grow the deck")
  is only partial: treasure is fixed authored data and the pack does not exist.
- C2's resolution ("1 of 3 packs, removal via the bonus") is not implemented.
- H2 still describes d4/d6/d8 and Desert/Devil/China; the current design and
  data use only d6 and Elvish/Devilish/Sinitic. The audit text is stale.
- The "still open" list is partly stale: hole death, retry HP, melee contact,
  and jump landing are resolved in the later design and code. Melee self-tile
  targeting, deck floor, terminal priority, and run length remain open.
- The audit's observability items remain absent: no test assembly, no state
  dump, no map validator, no published seed policy. `QaBootstrap.seed` is the
  only seed input.

## Docs/Game-Loop-Decision-Points.md

- Final resolved model item 9 ("torch pauses in the menu only") has no
  implementation (see `Docs/Game-Design.md`).
- Final resolved model item 10 (between-map packs and bonuses) has no
  implementation.
- Round 4 answer H5b ("burnout fully resets HP") conflicts with
  `Game-Design.md` §6.4/§9.9 and with the code, which restores `_entryHp`
  (`GameSession.cs:1182`). The documents disagree; the code follows
  Game-Design.
- The Round 3 resolution "`RunDeck` persistent, each level builds a `LevelDeck`,
  retry rebuilds it from the snapshot" is not implemented; see the
  `Game-Design.md` and `Architecture.md` findings.
- Terminal priority and the deck floor remain undefined in code; the current
  outcome order is implicit in `DamagePlayer`, `CompleteLevel`, and
  `RetryLevel` with no tests.

## Docs/concepts_brief.md

- The brief's codebase observations are now partly stale: `Tile` uses layered
  occupancy (`EnemyIds` + `HasPlayer`), `MapData` uses `TileType[]` plus typed
  spawn structs, `CardInstance`/`LevelSnapshot`/`RunState` exist, and
  `torchSeconds` exists only in `RunConfig`. Those items are no longer drift.
- The recommended `RunDeck` / `LevelDeck` split and treasure merge on completion
  are still not implemented (same as `Game-Design.md`).
- B11 remains unaddressed: no `_Game.Tests`, map validator, state dump, or
  documented seed policy.
- B4 remains unaddressed: no explicit terminal priority among HP 0, exit, deck
  exhaustion, and burnout; the order is implicit in code.
- The brief's §H warning about sand is still live: `Tile` keeps both
  `TileType.Sand` and the `HasSand` overlay (`Grid/Tile.cs:8-32`).
- The deck-floor recommendation for the removal bonus is moot until the meta is
  implemented, but no floor or validator exists.

---

## Good Coding Practice — Merged & Contributed Files

### Merged files (MazenUI / SoundDesign)

- `Cards/CardSelectionAudio.cs:15-21`, `Cards/DiceSelectionGate.cs:15-29`, and
  `UI/HandView.cs:26-34` dereference `service.Selection` and their other
  serialized references in `OnEnable` without null checks. A missing Inspector
  link throws during scene load. `CardSelectionAudio` also calls
  `RuntimeManager.PlayOneShot` with no FMOD-initialisation guard.
- `Cards/CardSelectionService.cs:12` defaults `logSelections` to `true`; every
  selection logs in play mode. The service also attaches an anonymous handler
  to `CardSelected` (lines 20-27) that is never detached.
- `UI/HandView.cs:37-90` builds a `Dictionary` from `view.Card.Id` without
  guard; a null card or duplicate id throws. `DealInStart` calls
  `views.IndexOf` inside a loop, making refresh O(n²).
- `UI/CardView.cs:48-51` and `UI/CardRevealView.cs:25-46` assume `artwork`,
  `visual`, `backdrop`, and `stage` are assigned; there is no validation and a
  missing reference produces a `NullReferenceException` rather than a clear
  error. `CardView.Bind` assigns `artwork.sprite` before checking that
  `artwork` exists.
- `Cards/CardSelection.cs:40-61` clears `offered` and then throws from inside
  the enumeration on a bad card, leaving the offer partially updated. The
  `CardInfo` constructor does not validate a null/empty id.
- `Cards/PlaceholderCardSource.cs:19` and
  `Dice/PlaceholderDiceRoller.cs:21-25` dereference `service.Selection`,
  `view`, and `die` without null checks. These are scene-authored components,
  so a broken link fails only at runtime.
- `UI/DiceView.cs:50-59` calls `StopAllCoroutines()` before rolling; this is
  safe today but would cancel unrelated coroutines if the component gains any.
- `CardSelectionAudio` lives in `Cards` and in `_Game.Runtime`, not in a
  separate audio assembly, keeping core runtime coupled to FMOD (the risk was
  already noted in `Architecture.md` §11).

### Contributed files

- `QA/QaBootstrap.cs` is a 600-line MonoBehaviour that builds the camera, UI,
  world, session, input module, and formatting. It duplicates the merged
  `HandView`/`CardView` path instead of using it. `RefreshWorld` (line 207)
  destroys and recreates every tile sprite and label on each damage, treasure,
  death, or play; `RefreshHand` (line 301) destroys and recreates every card
  object on each draw or discard. The deferred `Destroy` calls can leave
  duplicate objects for a frame and cause allocation churn.
- `QA/QaBootstrap.cs:63-68` ticks the torch with `Time.unscaledDeltaTime`
  unconditionally, with no pause/menu gate.
- `QA/QaTargetingController.cs:145-172` allocates a new `Material` per line
  and never destroys it. `UpdateHover` (47-97) discards `PathResult.HpCost`,
  has no slow-enemy telegraph overlay, and reuses the same grey for invalid and
  unreachable targets.
- `QA/ShapeSpriteFactory.cs:38-62` creates runtime `Texture2D` assets and never
  destroys them; `ResetCache` (15-21) only nulls the static references, so play
  sessions leak textures.
- `Editor/MapDataEditor.cs:98-99` calls `serializedObject.Update()` immediately
  before `ApplyModifiedProperties()`, which discards the edits made by the
  `PropertyField`s for `playerSpawn`, `exitPos`, `enemySpawns`, and
  `treasureSpawns` (drawn at lines 67-70). Painting a new Exit tile also does
  not clear the previous Exit tile, and `exitPos` is never read at runtime.
- `Editor/QaSceneBuilder.cs:19,26,61-66` writes
  `Assets/Scenes/GameSessionQA.unity`, but the committed scene is
  `Assets/_Game/Scenes/GameSessionQA.unity`. Running the menu item creates a
  second scene and adds it to build settings.
- `Editor/QaSceneBuilder.cs:69-102` generates a map with player `(0,0)`, exit
  `(7,7)`, four enemies, and two card-bearing treasures. The committed
  `QA_Level.asset` has player `(2,0)`, exit `(4,7)`, 11 enemies, and three
  treasures with empty card lists. The generator and the asset disagree.
- `Turn/GameSession.cs` mixes the turn state machine and every effect
  implementation in one 1,213-line class (`ExecuteEffect`, lines 644-673).
  `Architecture.md` §6 acknowledges the planned `ICardEffect` split, but the
  design principle "Strategy pattern for effects" (§1.4) is not realised.
- `Turn/GameSession.cs:58` exposes `RunDeck` as the level draw order and mutates
  that list from `OpenTreasure` (line 1147). `LevelSnapshot` and `RunState` are
  dead code, so the documented run-persistent deck has no implementation.
- `Turn/GameSession.cs:297-352` hardcodes the jump distance and ignores
  `CardData.jumpDistance`/`rangeValue`; `MapData.exitPos`, `CardTypeData.icon`,
  and `CardData.description` are also unused. This is incomplete data-driven
  modelling, contrary to `Architecture.md` §1.1.
- `Turn/GameSession.cs:159-178` and `:535-558` call `GetWalkPaths()` once per
  Walk card in the hand, and `QaBootstrap.IsCardPlayable` calls
  `GetPlayableCards()` once per card during refresh. `Pathfinder` uses a list
  frontier with `Contains`/`RemoveAt` (`Grid/Pathfinder.cs`), so refresh cost
  grows quadratically. The 8×8 QA map hides this; the documented 38×38 tier
  will not.
- `Cards/Deck.cs:33-45` raises `DeckExhausted` but no subscriber exists;
  `GameSession` handles exhaustion inline. This breaks the documented
  event-decoupled flow in `Architecture.md` §8.
- `Cards/Hand.cs:21-40` increments `_order` but never resets it on `Clear`, and
  `CardInstance.HandOrder` is never read. `Cards/CardInstance.cs:5` keeps a
  static `_nextId` that is never reset, so ids grow across sessions and retries.
- `Grid/Tile.cs:8-32` keeps both a `Sand` base type and a sand overlay; the
  concepts brief explicitly asked for one representation.
- No `_Game.Tests` assembly, test source, state dump, map validator, or scripted
  seed policy exists, so the testing strategy in `Architecture.md` §10 cannot
  run.
- Repository hygiene: `HEAD` contains both `Assets/TextMesh Pro/...` and
  `Assets/Resources/TextMesh Pro/...` (and other old/new asset pairs), while
  the working tree deletes the old paths. This is an uncommitted 2,870-path
  reorganisation and a duplicate-asset risk, not a clean merged state.

---

## Highest-Priority Drift

1. The between-map pack/bonus meta and run-persistent deck are specified in
   `README.md`, `Game-Design.md`, and `Roadmap.md` but absent from code.
2. Treasure does not generate 1–2 random cards; it grants authored cards, and
   the committed QA map grants none.
3. The documented `HandPresenter` bridge is absent; the only
   `GameSession` ↔ UI integration is a separate QA hand that bypasses
   `CardSelection`/`HandView`.
4. The torch has no menu-pause path and is ticked unconditionally by QA.
5. Documented paths (`Assets/Scenes`, `Assets/TextMesh Pro`,
   `Assets/UI/Placeholders`) are stale after the folder reorganisation, and
   duplicate asset trees remain in `HEAD`.
