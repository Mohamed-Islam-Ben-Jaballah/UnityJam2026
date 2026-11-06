# Game Design Document — Dice-Drive (working title)

> Turn-based grid roguelike deck-builder. The player plays consumable cards whose
> types are gated by dice rolls, races a real-time torch, and fights or avoids
> stationary contact and ranged enemies to reach the exit.

---

## 1. Elevator Pitch

The player holds a hand of cards spanning several "types" (Normal, Elf, Devil,
China, Angel, Desert). Each turn they roll their dice **one at a time**; the
number rolled unlocks which card types may be played. They drag a playable card
onto a tile of the map grid to move, jump, attack, heal, or reshape terrain.
The goal is to cross each map to the exit while a real-time torch burns down.
Between maps the player chooses a small card pack and one bonus, growing the
deck and the dice set.

---

## 2. Core Loop

```
[ Map start ]
   draw 5 cards
   start the torch (real time)
   └─ Turn
       ├─ TurnStart: resolve slow enemy attacks (before the first die)
       ├─ for each die in the dice set:
       │    roll the die
       │    if a playable card exists:
       │       the player must play one card
       │    else:
       │       the player must reroll
       │       first reroll of the turn is free; later rerolls cost 1 HP
       │       a paid reroll at 1 HP kills the player
       ├─ End of turn:
       │    draw 1 card per die rolled
       │    kills draw immediately (1 card; 2 for a tank)
       │    hand cap is 8; drawing over the cap discards the oldest card
       └─ reach Exit → map complete

   Treasure tiles add 1–2 random cards to the current level deck.
   Each completed level grants a card pack (see §7).
[ Between maps ]
   choose 1 of 3 packs (1–3 cards)
   choose 1 of 3 bonuses (+1 max HP and full heal / remove 1–3 cards / effect TBD)
   each option set can be rerolled once
[ Next map ]
```

**Failure states**

- **Deck empty** → retry the current level. This is a level reset, not a run loss.
- **Torch empty** → retry the current level. This is a level reset, not a run loss.
- **Player HP 0** → the run ends.

**Card lifecycle:** cards are consumable. A played card never returns this
level. The discard pile is a spent pile. The level deck is finite and resets on
a level retry. Treasure and the per-level pack are the intended sources of new
cards.

---

## 3. Dice System

### 3.1 Dice types

| Die | Sides | Notes |
|-----|-------|-------|
| d4  | 1–4   | Starting die |
| d6  | 1–6   | Adds faces 5 and 6 |
| d8  | 1–8   | Adds faces 7 and 8 |

- Start: `1 × d4`.
- The player gains **+1 die on some levels**, not all. The exact schedule is TBD.
- The dice set is rolled **one die at a time**; each die resolves one card play.

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

- **Normal** cards are playable on any roll.
- Every special type has a single "lucky number".
- Rolls of 2 / 4 / 7 enable only Normal cards ("normal numbers").
- A special card is playable only when its lucky number is rolled.

### 3.3 Reroll and mandatory play

- If a card in hand is playable for the rolled number, the player **must play
  one**. They choose which.
- If no card in hand is playable, the player **must reroll**.
- The first reroll of the turn is free. Each later reroll costs 1 HP.
- A paid reroll at 1 HP kills the player.
- Rerolling is not allowed while a playable card exists.

### 3.4 Dice progression

- The player adds dice between maps. The design intent is that dice add
  **actions**, not just numbers, so a new die is a new card play per turn.
- Whether d6 and d8 are added, and on which levels, is TBD.

---

## 4. Card System

### 4.1 Play rules

- Cards are played by **drag-and-drop onto a tile**, or confirmed for Self cards.
- Only cards whose type is currently unlocked may be played.
- Playing a card consumes it for the level. It goes to the spent pile and does
  not return.
- A card's effect resolves immediately after a valid drop.
- The hand cap is **8**. When a card is drawn over the cap, the oldest card in
  hand is discarded (spent).
- If the deck is empty and a draw is required, the level retries.

### 4.2 Card list

| Type | Card | Effect | Range | Target |
|------|------|--------|-------|--------|
| Normal | Walk | Move up to `diceValue` movement points (diagonal = 2, orthogonal = 1) | Move | tile within rolled movement |
| Elf (3) | Leapol | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| Elf (3) | Squimi | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| Elf (3) | Heilin | Heal 1 HP | Self | — |
| Devil (6) | Qa'faz | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| Devil (6) | Sara'akh | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| Devil (6) | Ha'yen | Remove 1 damaging obstacle | Melee (1) | adjacent damaging tile |
| China (8) | Maoyi | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| China (8) | Sunhai | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| China (8) | Qiahni | Draw 2 cards | Self | — |
| Angel (1) | Seraphil | 1 damage in a cross (+) | Self (cross) | centered on player |
| Desert (5) | Zulfar | Flood 8 neighbors with sand | Self (8-neighbor) | centered on player |

### 4.3 Effect definitions

- **Walk** — move up to `diceValue` movement points. The player picks the end
  tile and the game auto-paths. A diagonal step costs 2; an orthogonal step
  costs 1.
  - A **safe path** (green) avoids holes, walls, damaging tiles, and fast enemy
    ranges. Slow enemy ranges are shown as a telegraph overlay, not an HP cost.
  - If no safe path exists, an **orange/red path** crosses damaging tiles or fast
    enemy ranges. The player takes 1 HP per such tile entered.
  - A **grey path** is unreachable and cannot be selected.
  - Walk cannot cross holes. A hole requires a **Jump** card.
- **Jump 1** — move to a tile exactly 2 tiles away in one of the 8 directions,
  skipping the tile(s) in between. Holes, damaging tiles, and non-flying enemies
  may be skipped. Walls and flying enemies block the jump. The landing tile must
  be walkable and free of blocking occupants. Landing in a fast enemy's range
  deals immediate damage; landing in a slow enemy's range telegraphs it.
- **Deal 1 damage** — reduce an enemy's HP by 1 on the targeted tile. Melee
  range is adjacent. The player cannot normally target their own tile; see §10
  for the melee-enemy exception.
- **Heal 1** — restore 1 HP up to max. Heilin has no extra cost beyond the card.
- **Remove 1 damaging obstacle** — destroy a damaging obstacle on the targeted
  adjacent tile. Walls and holes are not affected.
- **Draw 2 cards** — draw 2 cards from the deck to the hand. The hand cap and
  spent-pile rules apply.
- **Seraphil (cross)** — deal 1 damage to all enemies in a cross (+): the
  player's tile and its 4 orthogonal neighbors.
- **Zulfar (sand flood)** — the 8 surrounding tiles are converted to sand: fill
  holes, cover damaging tiles (removing their damage), and deal 1 damage to
  enemies in those tiles. Walls are unaffected. Flying enemies ignore sand.

### 4.4 Range and targeting model

Every card has a range defined relative to the player's current position:

- **Self** — Heal, Draw, Seraphil, Zulfar.
- **Melee** — target an adjacent tile (8-way; a diagonal counts as 1 range).
- **Move** — Walk: any valid end tile within `diceValue`.
- **Jump** — fixed 2-tile leap (skip 1) in any of the 8 directions.

The `Ranged` card range type is removed. Only enemies have ranged attacks.

### 4.5 Hand, deck, and spent pile

- Hand size at map start: **5**.
- Hand cap: **8**.
- Played cards and overflow cards are **spent**. They do not return this level.
- The level deck is finite. When it empties, the level is retried.
- The deck can grow during a level from treasure (1–2 random cards) and between
  levels from the pack. Claimed treasure cards enter the level deck immediately
  and are kept across retries. They merge into the run deck when the level is
  completed. An opened treasure tile disappears and does not reappear on a
  retry.
- Draw: 1 card per die rolled at end of turn. Kill draws are immediate.

---

## 5. Grid and Terrain

### 5.1 Tile types

| Tile | Walkable | Jumpable | Notes |
|------|----------|----------|-------|
| Floor | yes | yes | default |
| Exit | yes | yes | win on enter |
| Treasure | yes | yes | opens once; adds 1–2 random cards; gold/score on enter |
| Hole | no | yes | filled by sand; no falling |
| Sand | yes | yes | created by Zulfar |
| Damaging | yes, at 1 HP per tile | yes | covered by sand |
| Wall | no | no | cannot be destroyed |

There are only **three obstacle types**: holes, damaging obstacles, and walls.

### 5.2 Obstacle taxonomy

1. **Hole** — not walkable; can be jumped over; filled by sand.
2. **Damaging (thorns/spikes)** — walkable at 1 HP per tile; can be jumped over;
   covered by sand.
3. **Wall/pillar** — not walkable; not jumpable; not destroyable.

### 5.3 Path preview

When a card is dragged, valid end tiles are highlighted:

- **Green** — a safe path exists.
- **Orange/red** — the only path crosses damaging tiles or fast enemy ranges; the
  preview shows the HP cost.
- **Telegraph** — slow enemy ranges are marked separately; entering them starts a
  slow attack.
- **Grey** — unreachable; cannot be selected.

---

## 6. Player, Enemies, Win/Lose

### 6.1 Player

- Max HP: **6**.
- Movement via Walk/Jump cards only; no free movement.
- Hand size at map start: **5**; hand cap: **8**.
- Healing: Heilin (+1), and the +1 max HP bonus restores to full.

### 6.2 Enemies

All enemies are stationary. They have two independent traits:

**Attack type**

| Type | Range | Slow behavior | Fast behavior |
|------|-------|---------------|---------------|
| Melee | the enemy's own tile | Attacks at the start of the next turn if the player stays on the tile | Attacks immediately when the player enters the tile |
| Ranged | adjacent tile (8-way; diagonal = 1 range) | Attacks at the start of the next turn if the player stays in range | Attacks immediately when the player enters range |

**Speed type**

| Type | Behavior |
|------|----------|
| Slow | Attacks at the start of the next player turn, before the first die, if the player is still in range. Leaving range cancels. If the player stays, it attacks every turn. |
| Fast | Attacks immediately when the player enters range. It does not attack again while the player stays; leaving and re-entering triggers it again. |

**Body type**

| Type | Traits |
|------|--------|
| Normal | 1 HP; can be jumped over |
| Tank | more HP (currently 2); kill draws 2 cards; can be jumped over |
| Flying | 1 HP; ignores sand; **cannot** be jumped over |

- Melee enemies do not block movement and can share the player's tile.
- Ranged enemies occupy and block their tile.
- Entry damage applies every time the player enters the tile or range,
  including a Walk path that passes through it and a Jump that lands in it.
- A melee enemy on the player's tile cannot be targeted by a melee card; the
  player must step off first or use a self-centered effect.

All enemies deal **1 damage**.

### 6.3 Win and lose

- **Win** — reach the Exit tile.
- **Lose (run)** — player HP reaches 0.
- **Deck empty** — retry the current level. This is intended, not a run loss.
- **Torch empty** — retry the current level. This is intended, not a run loss.
- **Holes** — no longer cause a fall or a restart. They are jumpable barriers.

### 6.4 Torch

- The torch is a **real-time** timer.
- Pausing the game pauses the torch. Alt-tabbing does not; the torch keeps
  running outside the pause menu.
- Burnout causes a **full level retry**: the torch, enemies, and unopened
  treasure reset. HP and the deck reset to the values recorded when the level
  started, not to full. This is a deliberate time penalty, not a run loss. A
  player can choose to let the torch burn to retry the level.
- Opened treasure stays opened and does not reappear on a retry.
- Target level length is **3–6 minutes** when played correctly. The exact torch
  duration is a tuning value.

### 6.5 Treasure and gold

Treasure gives 1–2 random cards and gold. The gold sink is TBD. The core
between-map flow is the pack and bonus choice; gold may later buy extra rerolls
or removals.

---

## 7. Between Maps

- **Pack:** every completed level grants a pack choice: choose 1 of 3 packs.
  Each pack contains 1–3 cards.
- **Bonus:** choose 1 of 3 bonuses:
  - +1 max HP and full heal.
  - Remove 1–3 cards.
  - An effect to be determined later.
- Each option set (packs and bonuses) can be rerolled **once**, for free.
- Claimed treasure cards enter the level deck immediately, are kept across
  retries, and join the run deck on completion. An opened treasure tile does not
  reappear on a retry.
- Free removal is removed. Removal comes only from the bonus choice.
- Dice: +1 die on some levels, not all. The schedule is TBD.
- Run length: TBD.
- Map size: tutorial maps average **8×8**, beginner maps **12×12**, and endgame
  maps up to **38×38**.

### 7.1 Starting deck (14 cards)

| Card | Type | Lucky # | Count |
|------|------|---------|-------|
| Walk | Normal | always | 8 |
| Leapol | Elf | 3 | 2 |
| Squimi | Elf | 3 | 2 |
| Heilin | Elf | 3 | 2 |

---

## 8. Initial tuning values

| Setting | Value |
|---------|-------|
| Player max HP | 6 |
| Hand size at map start | 5 |
| Hand cap | 8 |
| Enemy damage | 1 (all) |
| Enemy HP | 1 normal, 2 tank |
| Jump distance | 2 (skip 1) |
| Walk | up to the rolled die value of movement; diagonal = 2, orthogonal = 1 |
| Damaging tile | 1 HP per tile entered |
| Heal | +1 HP |
| Draw | 1 per die rolled at end of turn; 1 per kill immediately (2 for a tank) |
| Treasure | 1–2 random cards, plus gold/score |
| Reroll | first free, then 1 HP each |
| Torch | tuned for 3–6 minute levels |
| Map size | 8×8 tutorial, 12×12 beginner, up to 38×38 endgame |

---

## 9. Resolved design decisions

1. **Cards are consumable.** Played cards never return this level. The deck is
   finite; treasure and the per-level pack are the growth sources. An empty deck
   retries the level. A retry restores HP and the deck to their level-start
   values; opened treasure stays opened.
2. **Draw** — 1 card per die rolled at end of turn; kills draw immediately
   (1 card, or 2 for a tank). Hand cap 8; overflow discards the oldest card.
3. **Reroll** — only when no card is playable. First reroll free, later rerolls
   1 HP. A paid reroll at 1 HP kills the player.
4. **Mandatory play** — if a playable card exists, the player must play one.
5. **Movement** — Walk moves up to the die value of movement (diagonal = 2,
   orthogonal = 1). The player picks the end tile;
   the game auto-paths. Safe paths are preferred; damaging paths cost HP;
   unreachable paths are grey. Holes require a Jump card.
6. **Terrain** — three obstacle types: holes (jumpable), damaging (walkable at
   HP cost), walls (impassable). The player never falls into holes.
7. **Enemies** — stationary. Attack type: melee (own tile) or ranged (adjacent,
   8-way; diagonal = 1 range).
   Speed type: fast (attacks on entry) or slow (attacks at the start of the next
   turn if the player stays; leaving cancels). Body type: normal, tank, flying.
8. **Body types** — normal (1 HP, jumpable), tank (2 HP, kill draws 2), flying
   (ignores sand, cannot be jumped over). Other enemies can be jumped over.
9. **Torch** — real-time timer. Burnout retries the level to the level-entry HP,
   not full. It is a time penalty, not a run loss. The torch pauses in the menu
   only.
10. **Between maps** — choose 1 of 3 packs (1–3 cards), then 1 of 3 bonuses.
    Each option set can be rerolled once. Removal comes only from the bonus.
11. **No hole death.** Holes are barriers, not hazards.
12. **No level-up.** The meta is deck-building plus the per-level bonus.
13. **Treasure** — opens once and adds 1–2 random cards. Claimed cards enter the
    level deck immediately, are kept across retries, and merge into the run deck
    on completion. Opened treasure does not reappear on a retry.
14. **Enemy speed** — fast enemies attack on entry; slow enemies attack at the
    start of the next turn if the player stays in range.

---

## 10. Open questions and risks

1. **Fast melee may be too weak or too strong.** Fast melee attacks only on
   entry, so a non-blocking enemy can be stepped on and left. Slow melee forces
   a stay-or-leave choice. Tune damage, speed mix, and clump density together.
2. **How melee cards hit an enemy on the player's tile.** Melee cards target
   adjacent tiles. If a melee enemy shares the player's tile, the targeting rule
   is undefined. Current assumption: the player must step off first or use a
   self-centered effect.
3. **Treasure and pack growth vs. deck size.** Treasure cards and the per-level
   pack refill the deck, but the refill rate, treasure density, and map size must
   be tuned together. Claimed treasure survives a retry; opened treasure does
   not reappear.
4. **Two retry timers.** The torch and the finite deck both trigger level
   retries. Retries are intended, but two independent retry triggers make tuning
   and failure attribution harder. Retry HP is the level-entry HP, not full.
5. **Real time vs. turn-based play.** The torch punishes thinking and keeps
   running on alt-tab. It may frustrate players who read cards or plan.
6. **Map size.** A 38×38 endgame map with limited early actions may be slow
   against a 3–6 minute torch.
7. **Gold has no sink.** Treasure gives gold, but the current meta does not
   spend it.
8. **Run length is undefined.** The number of maps per run is TBD.
9. **Card variety is cosmetic.** Leapol/Qa'faz/Maoyi are the same jump;
   Squimi/Sara'akh/Sunhai are the same damage. Type changes only when a card can
   be played.
10. **Flying and sand.** The exact interaction (immune to damage, immune to
    conversion, or both) needs a rule.
11. **Tank HP.** "More HP" is currently set to 2; the final value is TBD.
12. **Fast enemy repeat behavior.** Fast enemies attack on entry. Whether they
    also attack at `TurnStart` while the player stays is a tuning decision; the
    current rule is entry only.
13. **Opened treasure and retries.** Claimed cards are kept, but the tile is
    gone. Verify that a player cannot re-open the same treasure and that the
    claimed cards are not duplicated on a retry.

---

## 11. Presentation and integration

- The card hand UI is `Assets/Scenes/Cards_Dice_UI.unity`, using
  `CardSelection`, `HandView`, `CardView`, `HandLayout`, and `CardRevealView`.
  It is a standalone selection demo; it does not yet drive `GameSession`.
- The audio test scene is `Assets/Scenes/SoundDesign.unity`, using the FMOD
  Studio project `FMOD/Test` and the `Master` bank. `CardSelectionAudio` plays a
  card-select event when one is assigned; it is silent while unassigned.
- The gameplay rules remain in `GameSession`. The planned bridge maps
  `CardInstance` to `CardInfo` and routes selection into `PlayCard`.
