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
│  CardView, DiceView, TileView, DragDrop, PathPreview,        │
│  RangeHighlight, HUD, TorchView                              │
│  DOTween animations, UGUI panels                             │
└──────────────▲───────────────────────────────┬───────────────┘
               │ events (subscribe)            │ commands (dispatch)
┌──────────────┴───────────────────────────────▼───────────────┐
│ Domain / Simulation (pure C#)                                 │
│  Dice, DiceSet, Deck, Hand, SpentPile, Grid, Pathfinder,      │
│  TurnStateMachine, CombatResolver, TorchTimer,                │
│  CardEffect strategies                                        │
└──────────────▲───────────────────────────────┬───────────────┘
               │ reads                         │ reads
┌──────────────┴───────────────────────────────▼───────────────┐
│ Data (ScriptableObjects)                                      │
│  CardData, CardTypeData, DiceData, EnemyData, MapData,        │
│  RunConfig                                                    │
└───────────────────────────────────────────────────────────────┘

Coordination: GameManager (bootstrap) + TurnManager (flow) + EventBus.
```

---

## 3. Folder structure

```
Assets/_Game/
├── Scripts/
│   ├── Core/           GameManager, RunManager, EventBus, SceneLoader
│   ├── Turn/           TurnManager, TurnStateMachine, phases, TorchTimer
│   ├── Dice/           Dice, DiceSet, DiceRoller, roll events
│   ├── Cards/          Card, CardEffect (base + subclasses), Deck/Hand/SpentPile
│   ├── Grid/           GridMap, Tile, TileType, Pathfinder
│   ├── Entities/       Player, Enemy, Health/Damage, EnemyAttackType, EnemyBodyType
│   ├── Interaction/    DragDrop, TargetSelector, RangeHighlighter, PathPreview
│   ├── UI/             HandView, DiceView, CardView, HUD, TorchView, tooltips
│   └── Editor/         _Game.Editor.asmdef (editor tooling)
├── Data/
│   ├── Cards/          CardData assets
│   ├── Dice/           DiceData assets
│   ├── Enemies/        EnemyData assets
│   └── Types/          CardTypeData assets
├── Prefabs/
├── Scenes/
├── Art/  (Sprites, Animations, Materials)
└── Audio/ (FMOD)
```

Assemblies: `Scripts/_Game.Runtime.asmdef` (references Unity.InputSystem,
UnityEngine.UI, Unity.TextMeshPro, FMODUnity) and `Scripts/Editor/_Game.Editor.asmdef` (references
`_Game.Runtime`, Editor-only).

---

## 4. Data model (ScriptableObjects)

### CardTypeData
| Field | Type | Notes |
|-------|------|-------|
| typeId | enum | Normal, Elf, Devil, China, Angel, Desert |
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
| sides | int (4/6/8) |

### EnemyData
| Field | Type | Notes |
|-------|------|-------|
| attackType | enum | Melee (own tile) or Ranged (adjacent) |
| speedType | enum | Fast (attacks on entry) or Slow (attacks at next TurnStart) |
| bodyType | enum | Normal, Tank, Flying |
| maxHp | int | 1 normal/flying, 2 tank |
| damage | int | always 1 |
| sprite / animation | refs |

Enemy range is derived: melee = own tile, ranged = adjacent. Attack timing is
derived from speed: fast attacks on entry, slow attacks at the start of the next
turn if the player is still in range. Movement blocking is derived: ranged
blocks, melee does not. `canBeJumpedOver` is true except for Flying;
`ignoresSand` is true only for Flying.

### MapData
| Field | Type |
|-------|------|
| width / height | int |
| tiles | flat `TileType[]` (row-major) or a Tilemap import |
| playerSpawn, exitPos | coordinate |
| enemySpawns | `EnemySpawn[] { EnemyData data; Vector2Int coord; }` |
| treasureSpawns | `TreasureSpawn[] { Vector2Int coord; int gold; CardData[] cards; }` |

### RunConfig
| Field | Type |
|-------|------|
| startingDeck | CardData[] |
| startingDice | DiceData[] |
| maxHp, handSize, handCap | int |
| torchSeconds | float (the single torch duration source) |
| packChoices, bonusChoices | typed choice tables |

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
Pathfinder      — weighted BFS/Dijkstra; returns PathResult
TurnStateMachine — states (see §7); exposes transitions & events
CombatResolver  — apply damage/heal, handle death, check win/lose
TorchTimer      — real-time countdown; pauses with the menu; raises Burnout
ICardEffect     — Execute(Command) → Result; implementations per effect type
PlayerState     — hp, position
EnemyState      — hp, attackType, speedType, bodyType, position, telegraphTurn (slow)
```

All domain objects are plain C#. `TurnManager` (a `MonoBehaviour` in the
presentation layer) drives them and forwards input commands.

**PathResult** — `{ State: Safe | Damaging | Unreachable, Tiles: coord[], HpCost: int }`.
The pathfinder prefers a safe path within range. If none exists, it returns the
lowest-HP damaging path within range. If neither exists, it returns
`Unreachable`.

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

One implementation per `CardEffectType`. `CardData` names the effect type and
params; a factory instantiates the strategy. This avoids a giant switch and
keeps each effect independently testable.

Effect implementations:
- `WalkEffect` — query `Pathfinder` for a `PathResult` within the rolled value;
  move along the chosen path; apply `HpCost` damage. Damaging tiles and **fast**
  enemy range entries count toward `HpCost`. Slow enemies telegraph instead.
  Holes are not walkable.
- `JumpEffect` — validate a 2-tile leap (8 directions); holes, damaging tiles,
  and non-flying enemies may be skipped; walls and flying enemies block; landing
  must be walkable. Landing in a fast enemy's range deals immediate damage;
  landing in a slow enemy's range telegraphs it. Landing on a ranged enemy tile
  is blocked.
- `DamageEffect` — damage an enemy on the target tile within melee range.
- `HealEffect` — heal self.
- `RemoveObstacleEffect` — remove a damaging obstacle on an adjacent tile;
  walls and holes are unaffected.
- `DrawEffect` — draw N cards; hand cap and spent-pile rules apply.
- `CrossDamageEffect` — damage in a + shape centered on the player.
- `FloodEffect` — convert 8 neighbors to sand: fill holes, cover damaging tiles,
  damage enemies. Walls unaffected; flying enemies ignore sand.

---

## 7. Turn state machine

States:
1. **TurnStart** — resolve slow enemy attacks (melee and ranged) before the
   first die. Fast enemy attacks resolve during movement, not here.
2. **RollDie** — roll the next die; publish `RollEvent(number)`.
3. **ChooseCard** — highlight playable cards. If a playable card exists, the
   player must play one. If none exists, go to **Reroll**.
4. **Reroll** — first reroll of the turn is free; later rerolls cost 1 HP. A
   paid reroll at 1 HP kills the player. After a reroll, return to ChooseCard.
5. **ResolveCard** — execute the effect; check win/lose.
6. **NextDieOrEndTurn** — if dice remain, go to RollDie; otherwise end the turn.
7. **DrawPhase** — draw 1 card per die rolled. Kill draws happen immediately
   during ResolveCard (1 card; 2 for a tank).
8. **TurnEnd** — loop to TurnStart (next turn).

Outside the turn machine:
- **TorchTimer** ticks in real time. On zero it raises `Burnout`; the level
  retries (HP, deck, hand, enemies, treasure, torch).
- **DeckExhausted** also retries the level.
- **HP 0** ends the run.

Transitions are validated by `TurnManager`; illegal drops are rejected without
state change.

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
3. Drop → `TurnManager.PlayCard(card, coord)` → validates → runs effect.
4. Simulation publishes events → views animate (DOTween) and update HUD.

Enemy attack flow:
1. Player enters a fast enemy's range → `EnemyAttacked(timing: Entry)`.
2. Player enters a slow enemy's range → `EnemyTelegraphed(attackTurn)`.
3. At the next `TurnStart`, if the player is still in range,
   `EnemyAttacked(timing: TurnStart)`.
4. If the player leaves range, the slow telegraph is cleared.
5. A slow enemy attack repeats each `TurnStart` while the player stays.

---

## 9. Module responsibilities

| Module | Responsibility |
|--------|----------------|
| Core | bootstrap, run lifecycle, scene loading, global config |
| Turn | state machine, phase orchestration, turn flow, torch timer |
| Dice | dice data, rolling, set management, roll events, rerolls |
| Cards | card data, deck/hand/spent pile, effect strategies |
| Grid | tile grid, terrain rules, occupancy, pathfinding |
| Entities | player/enemy state, health/damage, attack/body types, contact and telegraph |
| Interaction | drag & drop, target selection, range highlighting, path preview |
| UI | hand/dice/card views, HUD, torch view, tooltips, animation triggers |

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
- **Reroll tests** — one free reroll; paid rerolls cost 1 HP; paid reroll at 1 HP
  kills; reroll only when no playable card.
- **Enemy tests** — fast attack on entry; slow attack at next `TurnStart`;
  leaving range cancels; staying in range attacks every turn; tank/flying
  traits; layered occupancy; fast enemy re-entry.
- **Deck growth tests** — treasure adds 1–2 cards; completion merges them into
  the run deck; retry keeps claimed cards and removes opened treasure.
- **Retry tests** — retry HP equals level-entry HP; opened treasure does not
  respawn; no duplicate claimed cards.
- **Torch tests** — real-time tick, pause with menu, burnout retries the level.
- **Editor tests** — validate ScriptableObject data (unique lucky numbers, valid
  params, valid enemy traits).
- **PlayMode smoke tests** — boot scene, roll, play a Walk card, reach exit,
  empty the deck, burn the torch.

---

## 11. Technical risks / notes

- **Real-time torch in a turn-based game** — the timer must pause cleanly with
  the menu, survive scene reloads, and not tick during a level retry. Use
  `Time.unscaledDeltaTime` or a dedicated clock.
- **38×38 pathfinding** — weighted BFS/Dijkstra must run per drag. Cache the
  grid and invalidate on tile change; preview only the reachable region.
- **DOTween has no `.asmdef`** — lives in `Assembly-CSharp-firstpass`; to use it
  from `_Game.Runtime`, add `DOTween.asmdef` (+ a `DOTween.Editor` asmdef) or
  keep tweening in a non-asmdef area.
- **FMOD** — `FMODUnity.asmdef` references Addressables (not in manifest);
  add the reference only when audio code is written and FMOD compiles cleanly.
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
