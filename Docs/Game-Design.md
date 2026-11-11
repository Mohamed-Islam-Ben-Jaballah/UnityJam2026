# Game Design Document — Dice-Drive (working title)

> Turn-based grid roguelike deck-builder. The player plays consumable cards whose
> types are gated by dice rolls, races a real-time torch, and fights or avoids
> stationary contact and ranged enemies to reach the exit. Maps are either open
> grids or painted one-way paths.

---

## 1. Elevator Pitch

The player holds a hand of cards spanning several "types" (Normal, Elvish,
Devilish, Sinitic, and special Loneliness cards). Each turn they roll their dice **one at a time**; the
number rolled unlocks which card types may be played. On a grid map they drag a
playable card onto a tile to move, jump, attack, heal, or reshape terrain. On a
linear map they click the card and the action resolves along the painted path.
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
       │       one free reroll per map; paid 1 HP rerolls start on map 2
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

**Map types**

- **Grid map** — the original open grid. The player chooses destinations and the
  game auto-paths across the grid.
- **Linear map** — a painted, one-way path on an N×M canvas. The player only
  moves forward along the path and can choose a branch at marked junctions.

Both map types use the same turn, deck, dice, torch, enemy, reward, and failure
rules unless §5.0 says otherwise.

**Failure states**

- **Deck empty** → reset the current level. This is a level reset, not a run loss.
- **Torch empty** → reset the current level. This is a level reset, not a run loss.
- **Player HP 0** → reset the current level to its entry state. This is a level reset,
  not a run loss. The run continues.

**Card lifecycle:** cards are consumable. A played card never returns this
level. The discard pile is a spent pile. The level deck is finite and resets on
a level retry. Treasure and the per-level pack are the intended sources of new
cards.

---

## 3. Dice System

### 3.1 Dice types

| Die | Sides | Notes |
|-----|-------|-------|
| d6  | 1–6   | The only die type |

- Start: `1 × d6`.
- The player gains **+1 d6 on some maps**, not all. The exact schedule is TBD.
- The dice set is rolled **one die at a time**; each die resolves one card play.

### 3.2 Number → card type mapping

| Roll | Unlocks |
|------|---------|
| 1    | Loneliness special effects (not a family) |
| 2    | Elvish (Union) |
| 3    | Normal only |
| 4    | Normal only |
| 5    | Devilish (Desire) |
| 6    | Sinitic (Fortune and wealth) |

- **Normal** cards are playable on any roll.
- Every special type has a single "lucky number".
- Rolls of 3 and 4 enable only Normal cards ("normal numbers").
- Roll 1 is a normal number with Loneliness special effects. Those effects are
  still being defined.
- A special card is playable only when its lucky number is rolled.

### 3.3 Reroll and mandatory play

- If a card in hand is playable for the rolled number, the player **must play
  one**. They choose which.
- If no card in hand is playable, the player **must reroll**.
- The player has **1 free reroll per map**. It resets at map entry and at a
  level reset.
- After the free reroll is spent, paid rerolls cost 1 HP. Paid rerolls unlock
  starting on map 2. A paid reroll at 1 HP kills the player.
- On map 1, after the free reroll, an unplayable die is skipped (lost).
- Rerolling is not allowed while a playable card exists.

### 3.4 Dice progression

- The player adds d6 dice between maps. Each die adds one card play per turn.
- The exact schedule for extra dice is TBD.

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
| Elvish (2) | Leapol | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| Elvish (2) | Squimi | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| Elvish (2) | Heilin | Heal 1 HP | Self | — |
| Elvish (2) | MelowCart | Spread tree roots (flood 8 neighbors) | Self (8-neighbor) | centered on player |
| Devilish (5) | Qa'faz | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| Devilish (5) | Sara'akh | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| Devilish (5) | Ha'yen | Remove 1 damaging obstacle | Melee (1) | adjacent damaging tile |
| Sinitic (6) | Maoyi | Jump 1 | Jump (2, skip 1) | line, 2 tiles |
| Sinitic (6) | Sunhai | Deal 1 damage | Melee (1) | adjacent tile w/ enemy |
| Sinitic (6) | Qiahni | Draw 2 cards | Self | — |
| Loneliness (1) | Cale Azrael | Fire inverted cross: 1 damage in a + | Self (cross) | centered on player |

On **Linear maps** the target column changes: Walk and Jump resolve forward along
the path, Damage auto-targets the first enemy ahead within range, and Heal/Draw
are self. RemoveObstacle, CrossDamage, and Flood are not shuffled into a Linear
level deck. See §5.0.

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
- **Walk (Linear)** — no destination choice. The player clicks Walk and the
  player moves the full rolled distance forward along the path. Branch and
  interior-exit prompts pause the movement. Entering a hole kills the player and
  resets the level. Enemy, damaging, and treasure effects resolve on each tile.
- **Jump 1** — move to a tile exactly 2 tiles away in one of the 8 directions,
  skipping the tile(s) in between. Holes, damaging tiles, and non-flying enemies
  may be skipped. Walls and flying enemies block the jump. The landing tile must
  be walkable and free of blocking occupants. Landing in a fast enemy's range
  deals immediate damage; landing in a slow enemy's range telegraphs it.
- **Jump (Linear)** — move exactly two path steps forward, following turns. Jump
  is unavailable when the first step is a branch tile. It may cross holes,
  damaging tiles, enemies, treasures, and interior exits. A landing on a branch
  or exit uses that prompt.
- **Deal 1 damage** — reduce an enemy's HP by 1 on the targeted tile. Melee
  range is adjacent. The player cannot normally target their own tile; see §10
  for the melee-enemy exception.
- **Deal 1 damage (Linear)** — no target choice. Hit the first enemy ahead within
  the card's range value, counted in path steps. Range 0 means the current tile.
  Holes do not block the check. A branch ends the search unless the player is on
  that branch with a direction chosen.
- **Heal 1** — restore 1 HP up to max. Heilin has no extra cost beyond the card.
- **Remove 1 damaging obstacle** — destroy a damaging obstacle on the targeted
  adjacent tile. Walls and holes are not affected.
- **Draw 2 cards** — draw 2 cards from the deck to the hand. The hand cap and
  spent-pile rules apply.
- **Cale Azrael (fire inverted cross)** — deal 1 damage to all enemies in a
  cross (+): the player's tile and its 4 orthogonal neighbors. The visual is a
  fire inverted cross.
- **MelowCart (tree roots)** — the 8 surrounding tiles are converted to roots
  (mechanically sand): fill holes, cover damaging tiles (removing their damage),
  and deal 1 damage to enemies in those tiles. Walls are unaffected. Flying
  enemies ignore the roots.

### 4.4 Range and targeting model

Every card has a range defined relative to the player's current position:

- **Self** — Heal, Draw, Cale Azrael, MelowCart.
- **Melee** — target an adjacent tile (8-way; a diagonal counts as 1 range).
- **Move** — Walk: any valid end tile within `diceValue`.
- **Jump** — fixed 2-tile leap (skip 1) in any of the 8 directions.

The `Ranged` card range type is removed. Only enemies have ranged attacks.

On **Linear maps**, Move and Jump become forward-only actions and Melee damage
uses path distance. The player never selects a destination or an enemy.

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

### 5.0 Map kinds (Grid and Linear)

Every `MapData` asset has a `mapKind` of **Grid** or **Linear**.

**Shared**

- Width and height are each **4 to 64** cells. Non-square maps are allowed.
- Terrain, enemies, treasures, exits, and reward pools use the same data assets
  in both modes.
- Enemies keep their melee/ranged, fast/slow, and body-type rules.

**Grid maps**

- The whole area is traversable terrain. Walls, sand, holes, and damaging tiles
  follow §5.1–§5.2.
- Walk uses 8-way auto-pathing with diagonal steps costing 2 and orthogonal
  steps costing 1. The player chooses the destination tile.
- Holes are barriers. A Jump crosses them; a Walk cannot enter them.
- Standing on an exit shows the persistent Take Exit button.

**Linear maps**

- The map is an N×M canvas, but only painted **path tiles** are traversable.
  The designer paints a path and leaves at least one empty cell between path
  sections that are not meant to touch.
- A path tile may hold at most one special overlay or marker: a hole, a damaging
  tile, an enemy, a treasure, an exit, or a branch marker. Overlays do not stack.
  Walls and sand are not allowed on linear maps.
- Exactly one player start is placed at a path endpoint. Every other path dead
  end must be a terminal exit.
- **No backward movement.** Forward means the direction with the next path steps.
- **Branches:** a path tile that splits has a branch marker. The game shows two
  or three arrow buttons derived from the path graph. Choosing an arrow sets the
  direction for the next movement. The choice is locked until the player moves.
- **Merges:** a merge tile is not marked. The builder infers it from the path
  graph and gives it one outgoing direction. A merge does not prompt.
- **Walk:** the player does not choose a destination. On click, Walk spends the
  full rolled distance moving forward along the path, step by step.
  - Entering a **hole** kills the player and resets the level.
  - Entering a **damaging** tile costs HP normally.
  - Entering an **enemy** tile uses the normal enemy entry rules. Passing through
    a slow enemy does not make it attack; a fast enemy attacks on entry.
  - Reaching an **interior exit** pauses the walk and offers Continue or Enter.
    Enter starts the normal exit transition. Continue forfeits that exit and
    spends the remaining steps. If no steps remain, the player stays on the tile.
  - Reaching a **terminal exit** takes it automatically.
  - Reaching a **branch** pauses the walk until an arrow is chosen, then the
    remaining steps continue along that branch.
- **Jump:** Jump moves exactly two path steps, following turns. It is unavailable
  when the first step is a branch tile. It may jump over holes, damaging tiles,
  enemies, treasures, and interior exits. It lands normally, including branch or
  exit prompts. Merges are allowed.
- **Damage:** the player does not choose a target. Damage hits the first enemy
  ahead within the card's range value, counted in path steps. Range 0 means the
  player's current tile. Holes do not block the range. A branch ends the search
  unless the player is already on that branch with a direction chosen.
- **Heal and Draw:** self cards. Click to activate. They do not need a target.
- **Card pool:** only Walk, Jump, Damage, Heal, and Draw are shuffled into a
  linear level deck. Complex cards (RemoveObstacle, CrossDamage, Flood) stay in
  the run deck and are shuffled again on the next Grid map. Linear treasure and
  reward pools are restricted to the allowed cards.
- **Camera:** a linear map frames the painted path bounds, not the whole canvas.
- **Torch:** keeps running during branch and exit prompts. If it burns out, the
  resumable movement is cancelled and the level resets.

### 5.1 Tile types

| Tile | Walkable | Jumpable | Notes |
|------|----------|----------|-------|
| Floor | yes | yes | default |
| Exit | yes | yes | win on enter |
| Treasure | yes | yes | opens once; adds 1–2 random cards; gold/score on enter |
| Hole | no | yes | Grid: filled by sand, no falling. Linear: entering with Walk kills and resets. |
| Sand | yes | yes | created by MelowCart (tree roots); Grid only |
| Damaging | yes, at 1 HP per tile | yes | covered by sand |
| Wall | no | no | cannot be destroyed; Grid only |

There are only **three obstacle types**: holes, damaging obstacles, and walls.
Linear maps use holes and damaging tiles, but not walls or sand.

### 5.2 Obstacle taxonomy

1. **Hole** — Grid: not walkable, can be jumped over, filled by sand. Linear:
   Walk into it kills and resets the level; Jump crosses it.
2. **Damaging (thorns/spikes)** — walkable at 1 HP per tile; can be jumped over;
   covered by sand.
3. **Wall/pillar** — not walkable; not jumpable; not destroyable. Grid only.

### 5.3 Path preview

When a card is dragged, valid end tiles are highlighted:

- **Green** — a safe path exists.
- **Orange/red** — the only path crosses damaging tiles or fast enemy ranges; the
  preview shows the HP cost.
- **Telegraph** — slow enemy ranges are marked separately; entering them starts a
  slow attack.
- **Grey** — unreachable; cannot be selected.

This preview is for Grid maps. On Linear maps there is no destination drag:
cards play on click, branch arrows choose the next path, and the exit prompt
handles interior exits. Walk still advances step by step and applies each tile's
effect.

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
- **HP 0** — reset the current level to its entry state: entry HP, restocked deck and
  hand, free reroll, torch, enemies, and unopened treasure. This is a level reset, not a
  run loss. The run continues.
- **Deck empty** — reset the current level. This is intended, not a run loss.
- **Torch empty** — reset the current level. This is intended, not a run loss.
- **Holes** — Grid: jumpable barriers; no fall and no restart. Linear: entering a
  hole with Walk kills the player and resets the level.

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

Treasure opens once and grants cards from its `TreasureData` asset, plus gold and
heal. The gold sink is TBD. The core between-map flow is the reward screen in
§6.6; the older pack and bonus text in §7 is superseded for the current build.

### 6.6 Map exits, rewards, and transition

- Exits are object markers on Floor cells. They are numbered in creation order
  and each has a destination map. A destination is a `MapData` asset or a prefab
  with a `MapView` that exposes a `MapData`.
- **Grid:** standing on an exit shows a persistent **Take Exit** button. The
  torch keeps running while the button is visible. Clicking it opens the reward
  screen.
- **Linear:** reaching an interior exit with Walk or Jump pauses movement and
  offers **Continue** or **Enter**. Enter opens the reward screen. Continue
  forfeits that exit permanently and resumes the remaining steps. If no steps
  remain, Continue leaves the player on the tile with the exit forfeited.
  Reaching a terminal exit at a path dead end takes it automatically.
- The torch keeps running during the Linear exit prompt.
- Taking an exit heals **1 HP**, capped at max.
- The reward screen shows **three distinct `Card3Tuple` options** from the map's
  reward pool. Each option is three cards. Each option can be rerolled **once**
  for a tuple that has not been shown. Choosing an option grants all three cards
  to the run deck.
- A loading overlay runs for at least one second, then the destination starts in
  the same scene.
- Current HP, max HP, gold, owned cards, claimed treasure, and the level number
  carry. The torch resets. The deck restocks and shuffles. The hand resets to 5.
  The free reroll resets. The exited map's state is discarded and cannot be
  returned to; reusing the same map asset starts it fresh.
- HP 0, deck exhaustion, and torch burnout reset the current level to its entry
  state. The run continues.
- The reward pool should contain at least six tuples. A smaller pool logs a soft
  alert. The meta upgrade stage is deferred.

---

## 7. Between Maps

> **Current build:** the pack and bonus flow below is superseded by the
> `Card3Tuple` reward screen in §6.6. The bonus and meta stage are deferred.

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
- Map size: **4 to 64 per side**, with **8×8** tutorial and **12×12** beginner
  presets. Maps are Grid or Linear; see §5.0.

### 7.1 Starting deck (14 cards)

| Card | Type | Lucky # | Count |
|------|------|---------|-------|
| Walk | Normal | always | 8 |
| Leapol | Elvish | 2 | 2 |
| Squimi | Elvish | 2 | 2 |
| Heilin | Elvish | 2 | 2 |

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
| Linear Walk | full rolled distance along the path |
| Linear Jump | 2 path steps; first step cannot be a branch |
| Damaging tile | 1 HP per tile entered |
| Heal | +1 HP |
| Draw | 1 per die rolled at end of turn; 1 per kill immediately (2 for a tank) |
| Treasure | 1–2 random cards, plus gold/score |
| Reroll | 1 free per map; paid 1 HP from map 2 |
| Torch | tuned for 3–6 minute levels |
| Map size | 4 to 64 per side; 8×8 tutorial, 12×12 beginner |
| Map kinds | Grid and Linear; same 4–64 canvas |

---

## 9. Resolved design decisions

1. **Cards are consumable.** Played cards never return this level. The deck is
   finite; treasure and the per-level pack are the growth sources. An empty deck
   retries the level. A retry restores HP and the deck to their level-start
   values; opened treasure stays opened.
2. **Draw** — 1 card per die rolled at end of turn; kills draw immediately
   (1 card, or 2 for a tank). Hand cap 8; overflow discards the oldest card.
3. **Reroll** — only when no card is playable. One free reroll per map, reset at
   map entry and level reset. After it, paid rerolls cost 1 HP and unlock on map
   2. A paid reroll at 1 HP kills the player. On map 1, an unplayable die is
   skipped.
4. **Mandatory play** — if a playable card exists, the player must play one.
5. **Movement** — Grid: Walk moves up to the die value of movement (diagonal = 2,
   orthogonal = 1). The player picks the end tile; the game auto-paths. Safe paths
   are preferred; damaging paths cost HP; unreachable paths are grey. Holes require
   a Jump card. Linear: Walk spends the full roll forward along the path with no
   destination choice; branch and exit prompts pause the movement.
6. **Terrain** — Grid: three obstacle types, holes (jumpable), damaging (walkable
   at HP cost), walls (impassable); the player never falls into holes. Linear:
   holes and damaging only; walls and sand are disallowed and a Walk into a hole
   kills the player.
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
11. **Hole behavior is map-kind specific.** On Grid maps holes are barriers, not
    hazards. On Linear maps a hole is lethal to Walk and is crossed with Jump.
12. **No separate character level-up.** Map completion is the progression step;
    the per-map bonus is the reward. Free reroll increases through map
    progression are TBD.
13. **Treasure** — opens once and adds 1–2 random cards. Claimed cards enter the
    level deck immediately, are kept across retries, and merge into the run deck
    on completion. Opened treasure does not reappear on a retry.
14. **Enemy speed** — fast enemies attack on entry; slow enemies attack at the
    start of the next turn if the player stays in range.
15. **Families and dice** — Elvish (2, Union), Devilish (5, Desire), Sinitic
    (6, Fortune and wealth). Rolls 1, 3, and 4 are normal; 1 carries Loneliness
    special effects, which are not a family. Only d6s exist; the player starts
    with one and adds more.
16. **Free rerolls** — one free reroll per map, reset at map entry and level
    reset. Paid 1 HP rerolls unlock on map 2. More free rerolls through map
    progression are TBD.
17. **Linear path authoring.** The designer paints path tiles and branch markers.
    Path sections that are not meant to connect must not touch orthogonally.
    Every dead end other than the start must have an exit.
18. **Linear auto-targeting.** Walk and Jump spend the full action forward;
    Damage hits the first enemy ahead within range; Heal and Draw are self.
    Complex cards are set aside until the next Grid map.
19. **Linear merges are inferred.** A merge tile is not marked. The builder
    orients the path from the start and treats an unmarked junction with one
    outgoing edge as a merge. A branch marker is required for a split.

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
6. **Map size.** The 4–64 range includes maps much larger than the current
   movement budget. A full 64-wide traversal is an authoring option, not a
   promise of balance. The level designer must size paths and treasure to the
   deck and torch.
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
14. **Loneliness effects.** Roll 1 is a normal number with special effects that
    are not yet defined. Add them when the number-1 cards are designed.
15. **Free reroll scaling.** The base is 1 per map. How additional free rerolls
    are earned through map progression is TBD.
16. **Family balance.** Elvish (2) has four cards, including MelowCart; Devilish
    (5) and Sinitic (6) have three each. The lucky-number spread needs testing.
17. **Linear path length.** A 1×64 or long snaking path may exceed the current
    movement budget. Length is an authoring cap; tune path length, treasure, and
    torch together.
18. **Branch and merge authoring errors.** Missing branch markers, path sections
    that touch accidentally, disconnected path tiles, and dead ends without exits
    are soft alerts. They can still produce a broken map at runtime.
19. **Linear prompt state.** Branch and exit prompts pause card resolution and
    keep the torch running. Burnout, HP 0, or deck exhaustion must cancel the
    pending movement cleanly.
20. **Grid/Linear card pool skew.** Complex cards disappear from the level deck
    on a Linear map. The run deck keeps them, but a long Linear section can feel
    like a different deck. Tune the allowed card mix.

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
- Grid maps use drag-to-target through `QaTargetingController`. Linear maps use
  click-to-play through `QaCardView.PlayCardAuto`, plus a branch arrow panel and
  an exit Continue/Enter panel in `QaBootstrap`.
- Linear maps render only painted path tiles and frame the path bounds. Grid maps
  keep the full-grid rendering and camera.
