# Game Design Document — Dice-Drive (working title)

> Roguelike deck-builder where dice rolls unlock card types. Navigate a grid of
> holes, obstacles, enemies and treasure to reach the exit each level.

---

## 1. Elevator Pitch

The player holds a hand of cards spanning several "types" (Normal, Elf, Devil,
China, Angel, Desert). Each turn they roll their dice **one at a time**; the
number rolled unlocks which card types may be played. They drag a playable card
onto a tile of the map grid to move, jump, attack, heal, or reshape terrain.
The goal is to cross each map to the exit, collecting treasure and surviving
stationary enemies, then spend rewards between maps to grow/trim the deck and
upgrade their dice set.

---

## 2. Core Loop

```
[ Map start ]
   draw 5 cards
   └─ Turn
       ├─ for each die in the dice set:
       │    roll die → number unlocks card types
       │    player drags one playable card onto a tile → resolve effect
       ├─ Enemy phase (stationary enemies attack if conditions met)
       └─ draw 1 card at end of turn (+1 per enemy killed)
   └─ reach Exit → map complete
[ Between maps ]
   open a card pack → view full deck → remove 1–3 cards
   upgrade/add/drop dice
[ Next map ]
```

Death of the player (HP 0) ends the run. Falling into a hole returns the player
to the start of the current map (see §6.3).

---

## 3. Dice System

### 3.1 Dice types
| Die | Sides | Notes |
|-----|-------|-------|
| d4  | 1–4   | Starting die |
| d6  | 1–6   | Earned early; unlocks Devil (6) |
| d8  | 1–8   | Unlocks China (8) |

### 3.2 Number → card type mapping
| Roll | Unlocks |
|------|---------|
| 1    | Angel |
| 2    | Normal only |
| 3    | Elf |
| 4    | Normal only |
| 5    | Desert |
| 6    | Devil |
| 7    | Normal only |
| 8    | China |

- **Normal** cards are playable on *any* roll.
- Every special type has a single "lucky number".
- Rolls of 2 / 4 / 7 only enable Normal cards (called "normal numbers").

> Design note: d4 can only reach Angel/Elf/Desert + Normal; Devil (6) and China
> (8) become reachable only after acquiring a d6/d8. Dice progression therefore
> gates card variety.

### 3.3 Dice progression
- Start: `1 × d4`.
- Earn a `d6`.
- Each level-up lets the player **add / upgrade / drop** dice as rewards
  (e.g. d4→d6→d8, or hold multiple dice).
- The dice set is rolled **one die at a time**; each roll resolves one card play.

---

## 4. Card System

### 4.1 Play rules
- Cards are played by **drag-and-drop onto a tile** (see §4.3).
- Only cards whose type is currently unlocked may be played.
- Playing a card consumes it for the turn; it goes to the discard pile.
- A card's effect is resolved immediately after a valid drop.

### 4.2 Card list
| Type | Card | Effect | Range | Target |
|------|------|--------|-------|--------|
| Normal | Walk | Move `1 × diceValue` tiles | Move (= roll) | tile within rolled range |
| Elf (3) | Leapol | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| Elf (3) | Squimi | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| Elf (3) | Heilin | Heal 1 HP | Self | — |
| Devil (6) | Qa'faz | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| Devil (6) | Sara'akh | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| Devil (6) | Ha'yen | Remove 1 obstacle | Melee (1) | adjacent damaging/destroyable |
| China (8) | Maoyi | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| China (8) | Sunhai | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| China (8) | Qiahni | Draw 2 cards | Self | — |
| Angel (1) | Seraphil | 1 damage in a cross (+) | Self (cross) | centered on player |
| Desert (5) | Zulfar | Flood 8 neighbors with sand | Self (8-neighbor) | centered on player |

### 4.3 Effect definitions
- **Walk** — move up to `diceValue` tiles along a clear path of walkable tiles.
- **Jump 1** — move to a tile exactly 2 tiles away in one of the 8 directions
  (orthogonal or diagonal), skipping the tile(s) in between. The skipped
  tile(s) must be free of uncrossable (indestructible) obstacles and block-jump
  enemies; holes, damaging and destroyable obstacles may be skipped. Landing
  tile must be walkable and unoccupied.
- **Deal 1 damage** — reduce an enemy's HP by 1 on the targeted tile. Range is
  melee (adjacent) or ranged (2), per the card's data.
- **Heal 1** — restore 1 HP up to max.
- **Remove 1 obstacle** — destroy a **destroyable** or **damaging (passable)**
  obstacle on the targeted adjacent tile. Indestructible obstacles are not
  affected.
- **Draw 2 cards** — draw 2 cards from deck to hand (no tile target).
- **Seraphil (cross)** — deal 1 damage to all enemies in a cross (+): the
  player's tile and its 4 orthogonal neighbors. Originates from the player.
- **Zulfar (sand flood)** — the 8 surrounding tiles (Moore neighborhood) are
  converted to **sand**: fill any holes, destroy any destroyable obstacles, and
  deal 1 damage to any enemy in those tiles. Originates from the player.

### 4.4 Range & targeting model
Every card has a **range** defined relative to the player's current position:

- **Self** — Heal, Draw, Seraphil, Zulfar originate from the player (no tile
  target).
- **Melee** — target an adjacent tile (distance 1): damage, remove obstacle.
- **Ranged** — target a tile up to distance 2: some attacks.
- **Move** — Walk: any walkable tile within `diceValue` tiles.
- **Jump** — fixed 2-tile leap (skip 1) in any of the 8 directions.

Drag the card over the grid; valid tiles (computed from the player's position)
are highlighted. Drop resolves the effect. Self-range cards require no tile;
drop anywhere or click to confirm.

---

## 5. Grid & Terrain

### 5.1 Tile types
| Tile | Walkable | Notes |
|------|----------|-------|
| Floor | yes | default |
| Exit | yes | win on enter |
| Treasure | yes | pick up on enter |
| Hole | no | lethal (see §6.3); jumpable; filled by sand |
| Sand | yes | created by Zulfar |
| Obstacle — Damaging (thorns/spikes) | pass-through | costs 1 HP to cross; jumpable |
| Obstacle — Destroyable | no | blocks; 1 damage or Ha'yen destroys it; jumpable |
| Obstacle — Indestructible | no | blocks movement **and** jump; cannot be removed |

### 5.2 Obstacle taxonomy
1. **Damaging (passable)** — walking through deals 1 damage (spikes/thorns);
   removed by Ha'yen.
2. **Destroyable (blocking)** — cannot walk through; destroyed by 1 attack or
   Ha'yen; can be jumped over.
3. **Indestructible / uncrossable (blocking)** — cannot cross, cannot land on,
   cannot be destroyed, cannot be jumped over; Ha'yen has no effect.

---

## 6. Player, Enemies, Win/Lose

### 6.1 Player
- Max HP: **6**.
- Movement via Walk/Jump cards only; no free movement.
- Hand size at map start: **5**.

### 6.2 Enemies (all stationary, all deal 1 damage)
Enemies never move and never roll dice. They attack only during the Enemy
phase, when their condition is satisfied.

| Archetype | HP | Range | Traits |
|-----------|----|-------|--------|
| Sentry | 1 | adjacent (1) | — |
| Archer | 1 | 2 tiles | ranged |
| Brute | 2 | adjacent (1) | 2 HP |
| Anchor | 1 | adjacent (1) | blocks jump (cannot be jumped over) |

- **Telegraph mechanic** — when an enemy's attack condition is met, it shows a
  **warning telegraph** on turn 1, then attacks on turn 2.
- **Melee condition** — trigger when the player is orthogonally adjacent.
- **Ranged condition** — trigger when the player is within 2 tiles (line of
  sight).
- Jump may not cross an **Anchor** enemy's tile.

### 6.3 Win / Lose
- **Win** — reach the Exit tile.
- **Lose (run)** — player HP reaches 0: permadeath, the run ends.
- **Hole** — falling into a hole is instant death: the player returns to the
  start of the current map (level restart only, not a run loss).

---

## 7. Between Maps (Deck-building meta)

- Open a **card pack** (e.g. 3 Elf + 2 Angel + 10 Normal) and add its cards.
- View the full deck and **remove 1–3 cards** (free).
- **Spend gold** (from treasure) to heal HP, remove additional cards, or reroll
  the offered pack.
- Upgrade/add/drop dice from level-up rewards.

### 7.1 Starting deck (14 cards)
| Card | Type | Lucky # | Count |
|------|------|---------|-------|
| Walk | Normal | always | 8 |
| Leapol | Elf | 3 | 2 |
| Squimi | Elf | 3 | 2 |
| Heilin | Elf | 3 | 2 |

---

## 8. Initial tuning values (reference)

| Setting | Value |
|---------|-------|
| Player max HP | 6 |
| Hand size at map start | 5 |
| Enemy damage | 1 (all) |
| Enemy HP | 1–2 |
| Jump distance | 2 (skip 1) |
| Walk | 1 × dice value |
| Heal | +1 HP |
| Draw | 1 at end of turn (+1 per kill) |

---

## 9. Resolved design decisions

1. **Draw condition** — draw 1 card at the end of each turn; killing an enemy
   immediately draws 1 additional card.
2. **Hole consequence** — level restart only (not a run loss).
3. **Treasure purpose** — gold; spent between maps on healing, extra removals,
   or pack rerolls; also tracked as score.
4. **Starting deck** — 14 cards (see §7.1).
5. **Permadeath** — HP 0 ends the run; holes only restart the current level.
