# Architecture & Technical Design

> Unity 6 (6000.6.8f1), URP 2D, Input System, DOTween, FMOD, Tilemap available.
> Scripts live in two assemblies: `_Game.Runtime` and `_Game.Editor`.

---

## 1. Design principles

1. **Data-driven** — cards, types, dice, enemies, maps are ScriptableObject
   assets; gameplay code reads data, never hard-codes content.
2. **Simulation / view separation** — the grid, deck, and turn state are pure
   C# (no `MonoBehaviour`) so they can be unit-tested without a scene.
3. **Event-decoupled** — simulation emits events; views subscribe and render.
   Views never mutate simulation state directly; they dispatch commands.
4. **Strategy pattern for effects** — each card effect is a small class
   implementing a common interface, selected by the card's data.
5. **Single source of truth** — grid state and game state live in the domain
   layer; everything else is a projection.
6. **One clock per concern** — the turn state machine advances on player
   actions; the torch advances on real time. They meet only through the
   `LevelRestart` event.

---

## 2. Layered overview

```
┌─────────────────────────────────────────────────────────────┐
│ Presentation (MonoBehaviours)                                │
│  CardSelectionService, HandView, CardView, CardRevealView,   │
│  DiceView, TileView, DragDrop, PathPreview, RangeHighlight,  │
│  HUD, TorchView, CardSelectionAudio                          │
│  DOTween, UGUI + TextMesh Pro, FMOD events                   │
└──────────────▲───────────────────────────────┬───────────────┘
               │ events (subscribe)            │ commands (dispatch)
┌──────────────┴───────────────────────────────▼───────────────┐
│ Domain / Simulation (pure C#)                                 │
│  Dice, DiceSet, Deck, Hand, SpentPile, Grid, Pathfinder,      │
│  GameSession (turn state machine), TorchTimer,                │
│  CardEffect strategies                                        │
└──────────────▲───────────────────────────────┬───────────────┘
               │ reads                         │ reads
┌──────────────┴───────────────────────────────▼───────────────┐
│ Data (ScriptableObjects)                                      │
│  CardData, CardTypeData, DiceData, EnemyData, MapData,        │
│  RunConfig                                                    │
└───────────────────────────────────────────────────────────────┘

Coordination: GameSession (turn flow, pure C#) + a thin presentation adapter
(TurnManager) + EventBus.
```

---

## 3. Folder structure

```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Core/           RunConfig, RunState, event bus, scene loading
│   │   ├── Turn/           GameSession, TurnPhase, ActionResult, TorchTimer, LevelSnapshot
│   │   ├── Dice/           Die, DiceSet, DiceData
│   │   ├── Cards/          CardData, CardTypeData, CardInstance, Deck, Hand, SpentPile,
│   │   │                   CardInfo, CardSelection, CardSelectionService,
│   │   │                   CardSelectionAudio, PlaceholderCardSource
│   │   ├── Grid/           GridMap, Tile, TileType, Pathfinder, MapData
│   │   ├── Entities/       PlayerState, EnemyState, EnemyData
│   │   ├── Interaction/    DragDrop, TargetSelector, RangeHighlighter, PathPreview
│   │   ├── UI/             CardView, HandView, HandLayout, CardRevealView
│   │   └── Editor/         _Game.Editor.asmdef (editor tooling)
│   ├── Data/
│   │   ├── Cards/          CardData assets
│   │   ├── Dice/           DiceData assets
│   │   ├── Enemies/        EnemyData assets
│   │   └── Types/          CardTypeData assets
│   ├── Prefabs/UI/         CardView.prefab
│   └── Scenes/             game scenes (planned)
├── Scenes/                 Cards_Dice_UI.unity, SoundDesign.unity, SampleScene.unity
├── UI/Placeholders/        card, dice, and background images
├── TextMesh Pro/           TMP essential resources
├── Plugins/FMOD/           FMOD Unity plugin
└── Art/, Audio/            sprites, animations, materials, audio

FMOD/Test/                  FMOD Studio project and built Master bank
```

Assemblies: `Scripts/_Game.Runtime.asmdef` (references Unity.InputSystem,
UnityEngine.UI, Unity.TextMeshPro, FMODUnity) and `Scripts/Editor/_Game.Editor.asmdef` (references
`_Game.Runtime`, Editor-only).

---

## 4. Data model (ScriptableObjects)

### CardTypeData
| Field | Type | Notes |
|-------|------|-------|
| typeId | enum | Normal, Elvish, Devilish, Sinitic, Loneliness |
| luckyNumber | int | 0 for Normal (always playable) |
| displayName / icon | string / sprite | UI |

### CardData
| Field | Type | Notes |
|-------|------|-------|
| cardName | string | "Walk", "Leapol", … |
| cardType | CardTypeData | gates playability |
| effect | CardEffectType enum | Walk, Jump, Damage, Heal, RemoveObstacle, Draw, CrossDamage, Flood |
| rangeType | enum | Self, Melee, Move, Jump |
| rangeValue | int | distance (melee=1, move=rolled dice, jump=2) |
| params | struct | walkMultiplier, jumpDistance, damage, heal, drawCount |
| art / description | sprite / string | UI |

### DiceData
| Field | Type |
|-------|------|
| sides | int (6) |

### EnemyData
| Field | Type | Notes |
|-------|------|-------|
| attackType | enum | Melee (own tile) or Ranged (adjacent, 8-way) |
| speedType | enum | Fast (attacks on entry) or Slow (attacks at next TurnStart) |
| bodyType | enum | Normal, Tank, Flying |
| maxHp | int | 1 normal/flying, 2 tank |
| damage | int | always 1 |
| sprite / animation | refs |

Enemy range is derived: melee = own tile, ranged = adjacent (Chebyshev 1; a
diagonal counts as 1 range). Attack timing is
derived from speed: fast attacks on entry, slow attacks at the start of the next
turn if the player is still in range. Movement blocking is derived: ranged
blocks, melee does not. `canBeJumpedOver` is true except for Flying;
`ignoresSand` is true only for Flying.

### MapData
| Field | Type |
|-------|------|
| schemaVersion / mapName | int / string |
| mapKind | `Grid` or `Linear` |
| width / height | int, 4 to 64 per side |
| tiles | flat `TileType[]` terrain, row-major |
| pathTiles | `bool[]` path mask for Linear maps; ignored by Grid maps |
| branchTiles | `GridCoord[]` marked branch junctions on Linear maps |
| hasPlayerSpawn / playerSpawn | bool / coordinate |
| exits | `ExitSpawn[] { GridCoord coord; MapDestination destination; }` |
| enemySpawns | `EnemySpawn[] { EnemyData data; GridCoord coord; }` |
| treasureSpawns | `TreasureSpawn[] { GridCoord coord; TreasureData data; }` |
| rewardPool | `Card3Tuple[]` (at least six for the reward screen) |
| theme | `ThemeData` (optional) |

Legacy `exitPos`, `TreasureSpawn.gold`, and `TreasureSpawn.cards` remain for
compatibility with old assets. New maps use the object layer.

**Map kind rules**

- **Grid:** the whole terrain array is traversable. Walk uses `Pathfinder` and
  the player selects a destination.
- **Linear:** only `pathTiles` are traversable. The map has one start at a path
  endpoint; every other dead end is a terminal exit. Branch tiles are marked.
  Merges are inferred. A path tile may carry one overlay or marker, not several.
  Walls and sand are invalid.
- **Linear card deck:** the level deck is built from the run deck with
  `RemoveObstacle`, `CrossDamage`, and `Flood` filtered out. The run deck keeps
  them for the next Grid map. Linear treasure and reward pools are restricted to
  Walk, Jump, Damage, Heal, and Draw.

### PathGraph

`PathGraph` is built from `MapData` at level start. It is a pure C# helper in
`Grid/MapData.cs`.

- Stores 4-way path neighbors, forward edges, incoming edges, terminal exits,
  reachability, and a directed-cycle flag.
- Seeds distance-to-terminal-exit from dead-end exits.
- Walks forward from the start. A marked branch sends every candidate forward.
  An unmarked junction is a merge: only lower-distance neighbors are forward.
  This infers merges without a merge marker.
- `GetForwardOptions` returns the next path steps. `GetIncomingOptions` exposes
  merges for validation. `IsTerminalExit` identifies auto-take exits.
- The builder and `MapValidator` use the same graph, so authoring preview,
  validation, and runtime agree.

### TreasureData
| Field | Type |
|-------|------|
| shortCode / sprite | string / Sprite |
| gold / heal / cardCount | int |
| cards | `WeightedCard[] { CardData card; int weight; }` |

When a treasure opens, the game draws `cardCount` cards with replacement from
the weighted list, grants gold, and heals up to max HP. Claimed cards join the
run deck.

### Card3Tuple
`Card3Tuple { CardData a; CardData b; CardData c; }` with an unordered multiset
comparer. The map reward pool holds tuples; the reward screen shows three
options, rerolls each once for an unseen tuple, and grants the chosen three
cards.

### Exit, destination, and map view
`ExitSpawn { GridCoord coord; MapDestination destination; }`.
`MapDestination { DestinationKind kind; MapData map; ThemeData theme; GameObject prefab; }`.
A prefab destination must contain a `MapView` with a `MapData`; the prefab is a
visual shell and `MapData` drives gameplay.

### RunConfig
| Field | Type |
|-------|------|
| startingDeck | CardData[] |
| startingDice | DiceData[] |
| maxHp, handSize, handCap | int |
| freeRerollsPerMap | int (default 1) |
| torchSeconds | float (the single torch duration source) |
| packChoices, bonusChoices | typed choice tables |

### CardInfo (presentation)
| Field | Type | Notes |
|-------|------|-------|
| id | string | Stable selection id used by `CardSelection` |
| artwork | Sprite | Finished card image shown by `CardView` |

`CardInfo` is the UI view model. It is separate from `CardData`. A planned
bridge maps `CardInstance.Id` and `CardData.art` to `CardInfo`; the gameplay
model remains the source of truth.

---

## 5. Domain classes

```
Dice            — sides, Roll() → int (also drives roll animation via events)
DiceSet         — ordered list of Dice; NextDie(), consumed per roll
RunDeck         — run-persistent card collection; grows from treasure and packs
LevelSnapshot   — level-entry HP, max HP, deck order, claimed treasure, opened treasure, dice set
Deck            — level draw pile built from RunDeck; finite; no reshuffle; raises DeckExhausted
Hand            — cards in hand; cap 8; discards the oldest on overflow
SpentPile       — played and overflow cards; does not return this level
Grid            — Tile[,]; occupancy; IsWalkable(coord); tile predicates
Tile            — base TileType, sand overlay, enemy occupants, player occupant
Pathfinder      — weighted BFS/Dijkstra; returns PathResult for Grid maps
PathGraph       — Linear path topology: forward edges, branches, inferred merges, terminal exits
GameSession     — run-scoped turn loop; level entry, transition, reward selection, failure reset, free-reroll budget; owns resumable Linear movement
TorchTimer      — real-time countdown; pauses with the menu; raises Burnout
ICardEffect     — planned strategy interface; current code uses a switch
PlayerState     — hp, position
EnemyState      — hp, attackType, speedType, bodyType, position, telegraphTurn (slow)
```

All domain objects are plain C#. `GameSession` drives them; a thin
`TurnManager` MonoBehaviour will forward input commands and presentation
events.

**Presentation and UI classes (merged from MazenUI)**
- `CardInfo` — view model: stable id + artwork.
- `CardSelection` — offered/selected card lists; `CardSelected` and
  `OfferChanged` events; plain C#.
- `CardSelectionService` — scene access point; exposes `Selection`.
- `CardView` — card image, hover, click; `HandView` — mirrors
  `CardSelection.Offered`; `HandLayout` — fan math; `CardRevealView` — centered
  reveal.
- `CardSelectionAudio` — subscribes to `CardSelected` and plays an FMOD event.
- These are presentation-only. They are not yet wired to `GameSession`; a
  `HandPresenter` bridge is the next step.

**PathResult** — `{ State: Safe | Damaging | Unreachable, Tiles: coord[], HpCost: int, Steps: int }`.
`Steps` is the movement cost: a diagonal step costs 2, an orthogonal step costs 1.
The pathfinder prefers a safe path within range. If none exists, it returns the
lowest-HP damaging path within range. If neither exists, it returns
`Unreachable`. This is the Grid map movement path.

**Linear movement** does not use `Pathfinder`. `GameSession` advances one path
step at a time with `PathGraph`. It stores the remaining steps, the pending
branch/exit prompt, and the chosen branch direction. Walk spends `CurrentRoll`
steps; Jump spends two path steps. A hole kills and resets the level. Interior
exits and branch tiles pause the movement; the phase becomes
`AwaitingExitChoice` or `AwaitingPathChoice`. The torch keeps ticking in both
phases. Choosing a branch or Continue resumes the remaining steps; Enter starts
the reward transition.

**Deck and Hand rules**

- `Deck` is finite for the level. It never reshuffles.
- `RunDeck` is the run-persistent collection. A level's `Deck` is built from
  `RunDeck` at level start.
- `ClaimedTreasure` records cards taken from opened treasure. They enter the
  level deck immediately, are kept across retries, and merge into `RunDeck` on
  completion.
- `LevelSnapshot` records level-entry HP, max HP, deck order, claimed treasure,
  opened treasure coordinates, and the dice set. A retry restores the level from
  this snapshot. Opened treasure does not respawn.
- `Deck.Draw(n)` raises `DeckExhausted` if the deck cannot supply the draw. The
  level then retries.
- `Hand.Add` beyond `handCap` discards the oldest card to `SpentPile`.
- Played cards go to `SpentPile` and are not returned.

---

## 6. Card effect strategy pattern

```csharp
public interface ICardEffect {
    bool CanPlay(Grid grid, PlayerState player, DiceRoll roll);
    void Execute(Grid grid, PlayerState player, EnemyState[] enemies, EffectContext ctx);
}
```

The current implementation dispatches by `CardEffectType` inside
`GameSession.ExecuteEffect`. The `ICardEffect` strategy interface shown above is
the planned extraction; it is not implemented yet. The switch keeps the first
version small; the strategy split is the next refactor if the effect count
grows.

Effect implementations:
- `WalkEffect` — query `Pathfinder` for a `PathResult` within the rolled movement
  budget (diagonal 2, orthogonal 1);
  move along the chosen path; apply `HpCost` damage. Damaging tiles and **fast**
  enemy range entries count toward `HpCost`. Slow enemies telegraph instead.
  Holes are not walkable.
- `JumpEffect` — validate a 2-tile leap (8 directions); holes, damaging tiles,
  and non-flying enemies may be skipped; walls and flying enemies block; landing
  must be walkable. Landing in a fast enemy's range deals immediate damage;
  landing in a slow enemy's range telegraphs it. Landing on a ranged enemy tile
  is blocked.
- `DamageEffect` — damage an enemy on the target tile within melee range.
- **Linear variants** — `GameSession.PlayCardAuto` is the click-to-play entry
  point. Walk/Jump use the forward path, Damage uses
  `FindLinearDamageTarget(range)` along the chosen forward direction, and
  Heal/Draw are self. `CanPlayAuto` is the linear playability check.
- `HealEffect` — heal self.
- `RemoveObstacleEffect` — remove a damaging obstacle on an adjacent tile;
  walls and holes are unaffected.
- `DrawEffect` — draw N cards; hand cap and spent-pile rules apply.
- `CrossDamageEffect` — damage in a + shape centered on the player.
- `FloodEffect` — convert 8 neighbors to sand (MelowCart tree roots): fill holes, cover damaging tiles, damage enemies. Walls unaffected; flying enemies ignore sand.

---

## 7. Turn state machine

States:
1. **TurnStart** — resolve slow enemy attacks (melee and ranged) before the
   first die. Fast enemy attacks resolve during movement, not here.
2. **RollDie** — roll the next die; publish `RollEvent(number)`.
3. **ChooseCard** — highlight playable cards. If a playable card exists, the
   player must play one. If none exists, go to **Reroll**.
4. **Reroll** — one free reroll per map, reset at map entry and level reset.
   After it, paid rerolls cost 1 HP and unlock on map 2. A paid reroll at 1 HP
   kills the player. On map 1, an unplayable die is skipped. After a reroll,
   return to ChooseCard.
5. **ResolveCard** — execute the effect; check win/lose. On a Linear map, Walk
   and Jump can pause here.
   - **AwaitingPathChoice** — a branch tile was reached. Show two or three arrow
     options. Choosing stores the direction for the next movement and resumes
     any remaining steps. The choice is locked until the player moves.
   - **AwaitingExitChoice** — an interior exit was reached. Continue forfeits the
     exit and resumes; Enter starts the reward transition. The torch keeps
     ticking in both prompt phases.
6. **NextDieOrEndTurn** — if dice remain, go to RollDie; otherwise end the turn.
7. **DrawPhase** — draw 1 card per die rolled. Kill draws happen immediately
   during ResolveCard (1 card; 2 for a tank).
8. **TurnEnd** — loop to TurnStart (next turn).

Outside the turn machine:
- **TorchTimer** ticks in real time. On zero it raises `Burnout`; the level
  retries (HP, deck, hand, enemies, treasure, torch).
- **DeckExhausted** also retries the level.
- **HP 0** resets the current level to its entry state (entry HP, restocked deck and
  hand, free reroll, torch, enemies, unopened treasure). It does not end the run.

Transitions are validated by `GameSession`; illegal drops are rejected without
state change. The concrete implementation is `Scripts/Turn/GameSession.cs`.
`TurnPhase`, `ActionResult`, `DamageSource`, and `EnemyAttackTiming` live in the
same folder.

---

## 8. Event flow

Events (C# `Action`/`event` or SO events) carry the simulation into the view:
- `RollEvent(int number, unlocked types)`
- `HandChanged`, `CardPlayed`, `CardDrawn`, `CardDiscarded`
- `DeckExhausted`, `DeckChanged`
- `GridChanged(coord, tile)` / `TileConverted`
- `PlayerMoved`, `HealthChanged`, `EnemyDied`, `EnemyAttacked(enemyId, timing)`,
  `EnemyTelegraphed(enemyId, attackTurn)`
- `TreasureOpened(coord, cards, gold)`, `CardAdded(cardInstanceId, source)`
- `TorchChanged(float remaining)`, `Burnout`
- `Win`, `Lose`, `LevelRestart`

View flow for a card play:
1. Player starts drag → `TargetSelector` asks `Pathfinder`/`Grid` for valid tiles.
2. Valid tiles are highlighted green (safe), orange/red (damaging), or grey
   (unreachable) via `PathPreview`.
3. Drop → `GameSession.PlayCard(card, coord)` → validates → runs effect.
4. Simulation publishes events → views animate (DOTween) and update HUD.
5. **Linear maps:** click → `QaCardView` → `QaBootstrap.PlayCardAuto` →
   `GameSession.PlayCardAuto`. The movement pauses on a branch or interior exit
   and `QaBootstrap` shows the arrow or Continue/Enter panel. Neither prompt uses
   drag targeting.

Enemy attack flow:
1. Player enters a fast enemy's range → `EnemyAttacked(timing: Entry)`.
2. Player enters a slow enemy's range → `EnemyTelegraphed(attackTurn)`.
3. At the next `TurnStart`, if the player is still in range,
   `EnemyAttacked(timing: TurnStart)`.
4. If the player leaves range, the slow telegraph is cleared.
5. A slow enemy attack repeats each `TurnStart` while the player stays.

Card selection UI flow:
1. `CardSelectionService.Selection` is the source of truth.
2. `HandView` subscribes to `OfferChanged` and rebuilds `CardView`s.
3. A card click calls `CardSelection.TrySelect`.
4. `CardSelection.CardSelected` drives `CardRevealView.Play` and
   `CardSelectionAudio` (FMOD).
5. A planned `HandPresenter` will feed `CardSelection.SetOffer` from
   `GameSession.PlayerHand` and route selection into `GameSession.PlayCard`.

---

## 9. Module responsibilities

| Module | Responsibility |
|--------|----------------|
| Core | bootstrap, run lifecycle, scene loading, global config |
| Turn | state machine, phase orchestration, turn flow, torch timer |
| Dice | dice data, rolling, set management, roll events, rerolls |
| Cards | card data, deck/hand/spent pile, effect strategies, card view model and selection |
| Grid | tile grid, terrain rules, occupancy, pathfinding, Grid/Linear map data, path graph |
| Entities | player/enemy state, health/damage, attack/body types, contact and telegraph |
| Interaction | drag & drop and path preview for Grid; click-to-play, branch arrows, and exit prompt for Linear |
| UI | card hand, card views, hand layout, reveal, HUD, TMP |
| Audio | FMOD settings, Master bank, card-selection event adapter |

---

## 10. Testing strategy

- **Domain unit tests** — pure C# (Grid, Pathfinder, Deck, Hand, SpentPile,
  DiceSet, effects, state machine, torch timer).
  Use a test assembly (`_Game.Tests`) with NUnit/Unity Test Framework.
- **Pathfinding tests** — safe path preferred over a shorter damaging path;
  damaging path HP cost; unreachable path; holes excluded from Walk; Jump over
  holes and non-flying enemies; walls and flying enemies block.
- **Deck tests** — cards are consumable; deck exhaustion raises `DeckExhausted`;
  hand overflow discards the oldest; no reshuffle.
- **Reroll tests** — one free reroll per map; reset on map entry and retry; paid
  rerolls unlock on map 2 and cost 1 HP; paid reroll at 1 HP kills; reroll only
  when no playable card.
- **Enemy tests** — fast attack on entry; slow attack at next `TurnStart`;
  leaving range cancels; staying in range attacks every turn; tank/flying
  traits; layered occupancy; fast enemy re-entry.
- **Deck growth tests** — treasure grants cards from `TreasureData`; completion merges them into
  the run deck; retry keeps claimed cards and removes opened treasure.
- **Retry tests** — retry HP equals level-entry HP; opened treasure does not
  respawn; no duplicate claimed cards.
- **Torch tests** — real-time tick, pause with menu, burnout retries the level.
- **UI tests** — `Cards_Dice_UI.unity` smoke test: cards deal in, hover, select,
  reveal; `HandView` service/prefab references are present.
- **Audio tests** — `SoundDesign.unity` smoke test: `Master` bank loads; no
  missing FMOD event; `CardSelectionAudio` is silent while unassigned.
- **Integration tests** — planned bridge from `GameSession.PlayerHand` to
  `CardSelection` and back into `PlayCard`.
- **Editor tests** — validate ScriptableObject data (unique lucky numbers, valid
  params, valid enemy traits).
- **PlayMode smoke tests** — boot scene, roll, play a Walk card, reach exit,
  empty the deck, burn the torch.

---

## 11. Technical risks / notes

- **Real-time torch in a turn-based game** — the timer must pause cleanly with
  the menu, survive scene reloads, and not tick during a level retry. Use
  `Time.unscaledDeltaTime` or a dedicated clock.
- **32×32 pathfinding** — weighted BFS/Dijkstra must run per drag. Cache the
  grid and invalidate on tile change; preview only the reachable region.
- **DOTween has no `.asmdef`** — lives in `Assembly-CSharp-firstpass`; to use it
  from `_Game.Runtime`, add `DOTween.asmdef` (+ a `DOTween.Editor` asmdef) or
  keep tweening in a non-asmdef area.
- **FMOD** — the SoundDesign branch is merged. `FMODStudioSettings.asset` points
  to `FMOD/Test/Test.fspro` and `FMOD/Test/Build`, with `Master` as the master
  bank. `FMODUnity.asmdef` still lists optional Addressables/Timeline references
  that are not in the manifest; the project compiles because those code paths are
  guarded by version defines. If a build reports missing references, add the
  packages or remove the optional references.
- **LFS** — banks, fonts, and placeholder images are Git LFS objects. Run
  `git lfs fetch` before opening the merged scenes; pointer files break FMOD and
  TMP.
- **UI coupling** — `_Game.Runtime` now references `UnityEngine.UI`,
  `Unity.TextMeshPro`, and `FMODUnity`. The simulation is still pure C#; the UI
  and audio adapters sit in the same assembly. If the core must stay audio-free,
  move `CardSelectionAudio` to a separate `_Game.Audio` assembly.
- **UI integration** — the merged card UI is a standalone selection demo. It is
  not yet wired to `GameSession`; a `HandPresenter` bridge is required.
- **TMP** — the TMP essential resources are merged under `Assets/TextMesh Pro`.
  Import time is significant. Avoid duplicate `TMP Settings.asset` copies.
- **Determinism** — keep RNG (dice/deck shuffle) behind an interface so runs can
  be seeded for testing. The torch is real time and is not deterministic.
- **Consumable deck tuning** — deck size, draw per die, and dice count must be
  tuned together. A larger dice set spends the deck faster.
- **Melee targeting** — the melee enemy shares the player's tile; define whether
  a melee card can target the player's own tile. Current assumption: step off
  first or use a self-centered effect.
- **Treasure density** — treasure cards are the action-budget refill; map size,
  treasure count, and deck size must be tuned together.
- **Retry economics** — retry HP is the level-entry HP, so the free-heal loop is
  removed. Opened treasure is consumed permanently; claimed cards are kept.
- **Enemy speed** — fast enemies attack only on entry; slow enemies attack at
  `TurnStart` while the player stays. Tune the speed mix and the re-entry rule.
- **Linear path inference** — merges are inferred from branch markers and
  distance-to-terminal-exit. Missing branch markers, accidental orthogonal
  touching, disconnected path tiles, and dead ends without exits are soft
  validation alerts. Runtime still needs the prompt/retry guards.
- **Resumable Linear movement** — Walk/Jump can pause mid-card. The pending
  movement must be cancelled by retry, transition, or burnout; the torch keeps
  running during branch and exit prompts.
- **Linear deck filtering** — complex cards are removed only from the level deck.
  The run deck keeps them, so transition and retry must rebuild the level deck
  from the run deck instead of mutating the run deck.
- **Large maps** — 64×64 is an authoring cap. Grid rendering is per-cell; Linear
  rendering skips non-path tiles. Measure tile counts, preview rebuilds, and
  camera framing before shipping large maps.

---

## 12. Merge integration status

- `origin/SoundDesign` merged at `60a5897`.
- `origin/MazenUI` merged at `868fa38`.
- Backup branch: `pre-merge-backup`.
- LFS objects fetched; `Master.bank` 121,888 bytes, `LiberationSans.ttf`
  350,200 bytes, `Card_01.png` 312,553 bytes.
- `_Game.Runtime.asmdef` references `UnityEngine.UI`, `Unity.TextMeshPro`, and
  `FMODUnity`.
- Scenes: `Assets/Scenes/Cards_Dice_UI.unity`,
  `Assets/Scenes/SoundDesign.unity`, `Assets/Scenes/SampleScene.unity`.
- FMOD Studio project: `FMOD/Test`.
- Unity compiled (`Tundra build success`), with no `error CS` in the editor log.
- Pending: UI ↔ `GameSession` bridge; FMOD card-select event assignment; runtime
  scene verification; tests.
