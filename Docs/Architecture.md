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

---

## 2. Layered overview

```
┌─────────────────────────────────────────────────────────────┐
│ Presentation (MonoBehaviours)                                │
│  CardView, DiceView, TileView, DragDrop, RangeHighlight, HUD │
│  DOTween animations, UGUI panels                             │
└──────────────▲───────────────────────────────┬───────────────┘
               │ events (subscribe)            │ commands (dispatch)
┌──────────────┴───────────────────────────────▼───────────────┐
│ Domain / Simulation (pure C#)                                 │
│  Dice, DiceSet, Deck, Hand, Discard, Grid, TurnStateMachine,  │
│  CombatResolver, CardEffect strategies                        │
└──────────────▲───────────────────────────────┬───────────────┘
               │ reads                         │ reads
┌──────────────┴───────────────────────────────▼───────────────┐
│ Data (ScriptableObjects)                                      │
│  CardData, CardTypeData, DiceData, EnemyData, MapData, RunConfig│
└───────────────────────────────────────────────────────────────┘

Coordination: GameManager (bootstrap) + TurnManager (flow) + EventBus.
```

---

## 3. Folder structure

```
Assets/_Game/
├── Scripts/
│   ├── Core/           GameManager, RunManager, EventBus, SceneLoader
│   ├── Turn/           TurnManager, TurnStateMachine, phases
│   ├── Dice/           Dice, DiceSet, DiceRoller, roll events
│   ├── Cards/          Card, CardEffect (base + subclasses), Deck/Hand/Discard
│   ├── Grid/           GridMap, Tile, TileType, pathfinding (BFS)
│   ├── Entities/       Player, Enemy, Health/Damage, EnemyArchetype
│   ├── Interaction/    DragDrop, TargetSelector, RangeHighlighter
│   ├── UI/             HandView, DiceView, CardView, HUD, tooltips
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
| rangeType | enum | Self, Melee, Ranged, Move, Jump |
| rangeValue | int | distance (melee=1, ranged=2, move=rolled dice) |
| params | struct | walkMultiplier, jumpDistance, damage, heal, drawCount |
| art / description | sprite / string | UI |

### DiceData
| Field | Type |
|-------|------|
| sides | int (4/6/8) |

### EnemyData
| Field | Type |
|-------|------|
| archetype | enum (Sentry, Archer, Brute, Anchor) |
| maxHp | int (1–2) |
| range | int (1–2) |
| blocksJump | bool |
| damage | int (always 1) |
| sprite / animation | refs |

### MapData
| Field | Type |
|-------|------|
| width / height | int |
| tiles | serialized grid (or a TileType[,] + spawn points) |
| playerSpawn, exitPos, treasures, enemies | coordinates |

### RunConfig
| Field | Type |
|-------|------|
| startingDeck | CardData[] |
| startingDice | DiceData[] |
| maxHp, handSize | int |

---

## 5. Domain classes

```
Dice          — sides, Roll() → int (also drives roll animation via events)
DiceSet       — ordered list of Dice; NextDie(), consumed per roll
Deck / Hand / Discard — draw/shuffle/discard; emits DrawEvent/DiscardEvent
Grid          — Tile[,]; occupancy; IsWalkable(coord); pathfinding (BFS)
Tile          — TileType, occupant ref, obstacle flags, sand flag
TurnStateMachine — states (see §7); exposes transitions & events
CombatResolver — apply damage/heal, handle death, check win/lose
ICardEffect  — Execute(Command) → Result; implementations per effect type
PlayerState  — hp, position
EnemyState   — hp, archetype, position, telegraphTimer (warn turn 1 → attack turn 2)
```

All domain objects are plain C#; they are driven by `TurnManager` (a
`MonoBehaviour` in the presentation layer, or a thin C# orchestrator).

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
- `WalkEffect` — BFS range = diceValue; move player.
- `JumpEffect` — validate 2-tile leap (8 directions), skipped tile(s) free of
  uncrossable obstacles/block-jump enemies, valid landing; move player.
- `DamageEffect` — damage enemy on target tile within melee/ranged range.
- `HealEffect` — heal self.
- `RemoveObstacleEffect` — clear damaging/destroyable obstacle (not indestructible).
- `DrawEffect` — draw N cards.
- `CrossDamageEffect` — damage in + shape centered on the player.
- `FloodEffect` — convert 8 neighbors to sand (fill holes, clear obstacles,
  damage enemies), centered on the player.

---

## 7. Turn state machine

States:
1. **TurnStart** — begin player turn.
2. **RollDie** — roll the next die; publish `RollEvent(number)`.
3. **ChooseCard** — highlight playable cards; await drag/drop.
4. **ResolveCard** — execute effect; check win/lose.
5. **NextDieOrEnemy** — if dice remain → RollDie, else → EnemyPhase.
6. **EnemyPhase** — stationary enemies telegraph (turn 1) then attack (turn 2)
   per conditions.
7. **DrawPhase** — draw per the draw condition (TBD).
8. **TurnEnd** — loop to TurnStart (next turn).

Transitions are validated by `TurnManager`; illegal drops are rejected without
state change.

---

## 8. Event flow

Events (C# `Action`/`event` or SO events) carry the simulation into the view:
- `RollEvent(int number, unlocked types)`
- `HandChanged`, `CardPlayed`, `CardDrawn`
- `GridChanged(coord, tile)` / `TileConverted`
- `PlayerMoved`, `HealthChanged`, `EnemyDied`, `EnemyTelegraph`
- `Win`, `Lose`, `LevelRestart`

View flow for a card play:
1. Player starts drag → `TargetSelector` asks `Grid` for valid tiles.
2. Valid tiles highlighted via `RangeHighlighter`.
3. Drop → `TurnManager.PlayCard(card, coord)` → validates → runs effect.
4. Simulation publishes events → views animate (DOTween) and update HUD.

---

## 9. Module responsibilities

| Module | Responsibility |
|--------|----------------|
| Core | bootstrap, run lifecycle, scene loading, global config |
| Turn | state machine, phase orchestration, turn flow |
| Dice | dice data, rolling, set management, roll events |
| Cards | card data, deck/hand/discard, effect strategies |
| Grid | tile grid, terrain rules, occupancy, BFS pathfinding |
| Entities | player/enemy state, health/damage, archetype logic |
| Interaction | drag & drop, target selection, range highlighting |
| UI | hand/dice/card views, HUD, tooltips, animation triggers |

---

## 10. Testing strategy

- **Domain unit tests** — pure C# (Grid, Deck, DiceSet, effects, state machine).
  Use a test assembly (`_Game.Tests`) with NUnit/Unity Test Framework.
- **Editor tests** — validate ScriptableObject data (unique lucky numbers, valid
  params).
- **PlayMode smoke tests** — boot scene, roll, play a Walk card, reach exit.

---

## 11. Technical risks / notes

- **DOTween has no `.asmdef`** — lives in `Assembly-CSharp-firstpass`; to use it
  from `_Game.Runtime`, add `DOTween.asmdef` (+ a `DOTween.Editor` asmdef) or
  keep tweening in a non-asmdef area.
- **FMOD** — `FMODUnity.asmdef` references Addressables (not in manifest);
  add the reference only when audio code is written and FMOD compiles cleanly.
- **Determinism** — keep RNG (dice/deck shuffle) behind an interface so runs can
  be seeded for testing.
