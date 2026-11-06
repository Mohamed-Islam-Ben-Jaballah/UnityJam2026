# Dice-Drive — Revised Core Loop

* **Author:** Project team (EGM Brainstorm)
* **Date:** 2026-11-06
* **Status:** Concepts brief after six-lens Goldfish review
* **Target:** `Docs/Game-Design.md`, `Docs/Architecture.md`, `Docs/Roadmap.md`

---

## Overview

Dice-Drive is a turn-based grid roguelike deck-builder. The player rolls dice one
at a time, and each roll unlocks the card types that can be played. Cards are
consumable, movement is auto-pathed across a small tactical grid, and a
real-time torch pressures the player to reach the exit. Between maps the player
chooses a small card pack and one bonus.

The target player is a roguelike deck-builder player who enjoys turn-by-turn
puzzles and risk management. The core value is the decision "given this roll,
this hand, this board, and this timer, what is the best line?" The design is
intended for a jam-scale build, with a vertical slice first and the meta layers
added only after the core decision is proven.

Six Goldfish lenses reviewed the revised documents: design critic, codebase
details, zoom-out/YAGNI, comprehension/UX, implementation readiness, and
structural/maintainability. The findings below are deduplicated and grouped by
the design decision they attack, not by reviewer.

---

## Analysis

### A. The action economy is the primary blocker

The deck is now a consumable action budget. A 14-card starting deck with a
5-card opening hand gives roughly nine draws for the level. Every play consumes
a card. A level therefore yields at most about 14 plays no matter how many dice
the player owns. Adding a die does not add total plays; it compresses the same
draws into fewer turns. The claim in `Game-Design.md` §3.4 that a new die is a
new card play is true per turn and false per level. **The reward for progression
does not increase the player's capacity to finish a map.**

The map sizes make this concrete. An 8×8 map needs a 7-tile corner-to-corner
walk; a 12×12 map needs 11; a 38×38 map needs 37. The movement budget is about
eight Walk cards at a d4 average of 2.5, or roughly 20 tiles before walls and
detours. The 12×12 map already consumes most of the realistic budget; the 38×38
map is unreachable even if every play were a maximum Walk. The map-size curve
and the action economy are in different orders of magnitude.

The same currency creates three more problems:

- **Draw is a cost, not a reward.** Kill draws and Qiahni's Draw 2 consume the
  finite deck, so combat and card draw accelerate the level restart. The combat
  reward is a punishment.
- **Removal reduces capacity.** The "remove 1–3 cards" bonus lowers the number
  of plays available for the level. Packs increase capacity, so the dominant
  late-game choice is to take the biggest pack, which reintroduces deck dilution.
- **Hand overflow silently shrinks the budget.** The cap of 8 with "oldest card
  discarded" destroys cards without player choice and can delete the only
  movement card.

**Direction to resolve:** choose one model and make the docs consistent.

1. **Cycling deck:** shuffle the spent pile back; the deck stops being an action
   budget. The torch becomes the only pressure.
2. **Deck as budget:** keep consumable cards, remove the per-die draw, size maps
   from a published movement budget, and treat removal as a deliberate
   capacity-for-consistency trade.
3. **Separate action resource:** dice grant actions; the deck is drawn from a
   recycling pile. The two economies no longer share a number.

### B. The failure model erases the stakes

Deck exhaustion and torch burnout both restart the level. Burnout resets HP,
deck, hand, enemies, treasure, and the torch. Because HP also resets, a player
at low HP can intentionally burn the torch to heal, and a player who cannot
finish the level can retry forever. The design says the only run loss is HP 0,
but the two restart paths make HP 0 avoidable. The stated loss condition is not
reachable through the systems that are supposed to threaten the player. The
reroll rule is the main death vector, not enemies or terrain.

The restart is also not defined as a transaction. The reset list omits player
position, spent pile, dice state, turn index, RNG stream, taunt flags, sand and
grid mutations, gold, and in-flight animations. A restart can race a card
resolution, re-enter itself, or leave the view and the simulation out of sync.
Terminal outcomes (HP 0, exit reached, deck empty, torch empty) can co-occur,
and no priority is defined.

**Direction to resolve:** define a terminal priority and a single restart
routine. Make at least one restart path carry a permanent cost (HP loss, card
loss, or run end on repeated burnout). Decide whether the RNG seed resets; a
reset seed can soft-lock an unwinnable opening, and a non-reset seed lets the
player farm a better hand. Publish a restart manifest and a test that asserts
full state equality after restart.

### C. Melee enemies are mechanically broken

A melee enemy's range is its own tile. Leaving range cancels the attack. Melee
cards target adjacent tiles. The player is allowed to step onto the melee
enemy's tile. The result is a loop where the player can step on, step off, and
never be hit, while the enemy on the player's tile cannot be targeted by a melee
card. The only card that can hit it is Seraphil's cross, which includes the
player's tile. The design notes this risk in `Game-Design.md` §10.1 and ships it
anyway.

The occupancy model makes this worse. `Tile.occupant` is a single reference, so
it cannot represent the player and a melee enemy on the same coordinate. The
"melee cards may target the player's own tile" rule is a stated assumption, not
a rule, and the architecture lists the same point as unresolved. Walk, Jump,
targeting, and path preview all depend on an occupancy answer that does not
exist.

**Direction to resolve:** pick one enemy model and write it as a rule.

- **Blocking melee:** the enemy blocks its tile; the player taunts it from an
  adjacent tile; melee cards hit adjacent tiles. This removes co-occupancy.
- **Contact hazard:** stepping onto the tile deals damage immediately; no taunt
  state. This keeps co-occupancy but drops the start-of-turn phase.
- **Lock-on:** entering range locks the attack; leaving does not cancel. This
  makes the threat real but removes kiting.

Whichever is chosen, use layered occupancy (terrain / enemy / player) and define
`IsOccupied`, `BlocksWalk`, and `BlocksJump` as one shared set of predicates.

### D. Movement, pathfinding, and preview are underspecified

The docs do not define whether Walk uses 4-way or 8-way movement, whether
adjacency for melee cards is orthogonal or includes diagonals, or whether
"within `diceValue`" is path length, Chebyshev distance, or Manhattan distance.
Jump says "2 tiles away in one of the 8 directions" but does not define the
intermediate cells for a diagonal leap, which cells block it, or whether landing
on a damaging tile costs HP. Safe versus damaging precedence is also undefined:
a safe path may be longer than the die value while a shorter damaging path fits.

The preview can disagree with the executed path. `PathResult` is described per
destination, which invites one search per candidate tile. Equal-cost tie-breaks
are unspecified, so the preview, the movement, and the tests can disagree. The
38×38 performance note targets the wrong cost: Walk range is bounded by the die
value, so the search is at most a 17×17 neighborhood. The real costs are
per-tile `TileView` objects and rebuilding the preview on every drag event.

**Direction to resolve:** publish a movement table and a path cost tuple.

- Choose one metric and state it in `Game-Design.md` §4.4 and
  `Architecture.md` §5.
- Define the comparator: fits within N first, then lower HP cost, then shorter
  path, then a deterministic coordinate tie-break.
- Run one bounded search from the player per drag and derive every tile's color
  from it.
- Draw the full route and the taunt exposure, not only the destination color.

### E. The dice gimmick is inert and the card pool is thin

Normal is always playable, and Walk is Normal. The opening hand has eight Walks
in a 14-card deck, so the chance of having no Walk in the opening five is about
0.3%. The reroll and HP tension does not exist in the phase where players form
their impression. Because Walk is always available, the die number rarely
changes the plan; the specials are optional. Mandatory play then forces a Walk
even when the player would rather pass, and if no Walk is in hand, the reroll
rule can force a paid reroll that kills at 1 HP.

The card pool is roughly six unique effects across twelve cards: three identical
jumps and three identical damage cards. Type changes only when a card can be
played, not what it does. Desert (5), Devil (6), and China (8) are dead content
until a d6/d8 schedule exists, and that schedule is TBD. A pack can hand the
player a card whose lucky number is unreachable, which is a strictly negative
reward. Tank HP and the flying/sand interaction are also unresolved.

**Direction to resolve:** decide whether the roll gates movement or only
specials. If it gates only specials, accept that the dice are a special-card
filter and stop presenting them as the core of every turn. If it gates movement,
remove Normal's blanket exemption and provide a basic move or a pass. Cut the
clone cards or give each type a rider. Do not offer unreachable types until the
matching die exists.

### F. The real-time torch conflicts with turn-based play

The torch pauses in the menu only and keeps running on alt-tab. Opening the
pause menu gives unlimited planning time at zero cost, which defeats the
pressure. Focus loss, minimize, Steam overlay, and laptop sleep can produce
unfair wipes. A flat 3–6 minute target for 8×8, 12×12, and 38×38 maps is
under-scaled for large maps and over-scaled for small ones. The torch can fire
mid-drag, mid-`ResolveCard`, or mid-restart, and no preemption rule exists.

**Direction to resolve:** choose one of three:

- **In-game clock:** the torch ticks per turn or per action. It is deterministic,
  testable, and needs no pause or focus handling.
- **Real-time with a pause matrix:** pause on focus loss, pause during drag or
  menus, and scale the duration to map tier.
- **Remove the torch:** use the finite deck as the only pressure.

If real time stays, inject a clock interface, add debug controls to set or freeze
the torch, and log the effective duration at map start.

### G. The meta is underspecified and over-scoped

The between-map flow is "choose 1 of 3 packs, then 1 of 3 bonuses." Run length is
TBD. The third bonus is "effect TBD." The gold sink is TBD. The dice schedule is
TBD. The removal source, order, and count are undefined. There is no deck floor,
so a deck below the opening hand can trigger an immediate `DeckExhausted` loop
with no player action. Pack contents and rarity distributions are not specified.
The free reroll of both option sets makes the choice a UI step rather than a
tension.

**Direction to resolve:** fix the run length and the content budget before M5.
Ship three concrete bonuses, or reduce to the two that are defined. Define a
minimum deck size. Choose a gold sink or cut treasure. Define the dice schedule
and constrain packs to reachable numbers.

### H. The data model cannot be implemented as written

Several schema problems block implementation:

- `TileType[,]` is not Inspector-serializable in Unity. Use a flat array with
  width and height, or a serializable row struct, or a Tilemap import.
- `MapData.enemies` stores coordinates but no `EnemyData` reference. Use
  `EnemySpawn[] { EnemyData data; Vector2Int coord; }`.
- `Tile.occupant` is a single reference. Use layered occupancy.
- Sand is both a `TileType` and a flag. Choose an overlay (`baseTileType` plus
  `hasSand`) or a tile type, not both.
- `torchSeconds` exists in both `MapData` and `RunConfig`. Pick one source.
- `CardData.rangeValue` cannot represent a rolled-dice range, and `params` is an
  untyped struct. Use typed per-effect payloads.
- There is no `CardInstance` identity, so duplicate Walks cannot be ordered,
  removed, or logged.
- There is no `RunState` for the run-persistent deck, dice, HP, gold, and
  modifiers. `Deck` is level-scoped, and restart has no source of truth.

### I. Observability, testing, and docs are not ready

There are no stable IDs for runs, maps, turns, enemies, or card instances. There
is no command/event log, no state dump, no seed policy, and no test assembly.
The torch cannot be tested without waiting real minutes. Maps have no validator
for spawn on a wall, exit reachability, or enemy placement. The Roadmap's
verification lines omit staying-in-range repeat attacks, ranged taunt, taunt
cancellation, simultaneous failures, and restart determinism.

The README still says holes are instant death and that gold buys healing,
removals, and rerolls. The design now says holes are barriers and the gold sink
is TBD. `Architecture.md` lists classes such as `GameManager`, `TurnManager`,
and `HandView` as the current layout, but the script folders contain only
`.gitkeep`. A new contributor will triage from a false map.

### J. Scope, terminology, and UI load

The Roadmap is six milestones plus meta, audio, VFX, and a test pyramid. The
design's own scope note concedes this is large for a jam. The codebase has no
gameplay code yet, so the first build should prove the loop, not the meta.

Terminology collides: "Normal" is a card type, a set of dice numbers, and an
enemy body type. "Melee" is a card range and an enemy attack type with a
different range. "Jump 1" means a two-tile leap. "Taunt" means the enemy marks
the player. "Map," "level," and "run" are used interchangeably.

The UI spec is missing the pieces the rules depend on: a deck counter and
turns-remaining estimate, the dice queue and active die, reroll state ("free /
next −1 HP / lethal"), hand order for overflow, taunt markers and countdown,
route preview with taunt exposure, locked-card affordance, and color-blind-safe
path coding. Irreversible plays have no confirmation.

---

## Top blockers (deduplicated)

| ID | Blocker | Lenses |
|----|---------|--------|
| B1 | Deck growth from treasure (1–2 cards) and the per-level pack is the intended action-budget refill. Residual: treasure density vs map size; 38×38 remains a tuning risk. | Design, Zoom-out, Comprehension, Implementation |
| B2 | Retries are intended. Torch burnout and deck exhaustion reset the level; HP 0 is the run loss. Residual: a player can deliberately retry to heal. | Design, Codebase, Implementation, Structural |
| B3 | Melee enemies now deal contact damage on entry. No telegraph. Residual: co-occupancy and self-tile targeting remain. | Design, Comprehension, Implementation, Structural |
| B4 | No terminal priority or safe points; the torch can preempt a turn; restart races animations. | Design, Codebase, Implementation, Structural |
| B5 | Movement topology, path cost, and preview/execution equivalence are undefined. | Design, Codebase, Comprehension, Implementation |
| B6 | Mandatory play plus "no playable card" can force a lethal reroll; no pass or skip exists. | Design, Comprehension, Implementation |
| B7 | The data model is not serializable or complete (`TileType[,]`, occupant, spawns, sand, params). | Codebase, Implementation, Structural |
| B8 | Kill draws and Draw 2 accelerate deck exhaustion; the combat reward is a punishment. | Design, Comprehension, Codebase |
| B9 | Real-time torch pauses in the menu and runs on alt-tab; flat duration ignores map size. | Design, Comprehension, Implementation |
| B10 | Meta is TBD and over-scoped: run length, third bonus, gold, dice schedule, deck floor. | Design, Zoom-out, Codebase, Implementation |
| B11 | No IDs, event log, state dump, seed policy, test assembly, map validator, or torch test clock. | Codebase, Implementation, Structural |
| B12 | Scope, terminology, and UI affordances are not at jam-build quality. | Zoom-out, Comprehension, Structural |

---

## Goals vs. Non-Goals

### In-scope goals (first playable)

- Prove the roll → play → move decision on one small map.
- Three obstacle types: holes (jump), damaging (HP cost), walls (blocked).
- Auto-path preview with green / orange-red / grey states.
- A clear action budget: either a cycling deck or a consumable deck with maps
  sized to the movement budget.
- One enemy model with defined range, taunt timing, and occupancy.
- One failure pressure with a real cost and a defined restart transaction.
- A run loss that the bestiary or the economy can actually produce.

### Out-of-scope non-goals (for the first build)

- 38×38 endgame maps.
- Six card types and clone cards.
- Full pack/bonus/gold meta and a long run.
- A real-time torch if it cannot be tuned and tested.
- VFX, audio, and polish before the loop is proven.
- Save/load and deterministic replay beyond a debug state dump.

---

## Key Decisions & Staging for Next Steps

### Decisions to resolve before code

1. **Action budget:** cycling deck, consumable deck with sized maps, or a
   separate action resource.
2. **Failure model:** what a restart resets, what is permanent, terminal
   priority, and the seed policy.
3. **Melee enemy model:** blocking, contact hazard, or lock-on; layered
   occupancy and self-tile targeting.
4. **Movement:** 4-way or 8-way; distance metric; path comparator; preview and
   execution equivalence.
5. **Normal:** keep the blanket "always playable" rule, or gate movement and add
   a fallback move.
6. **Torch:** real time with a pause matrix, in-game ticks, or no torch.
7. **Scope:** the vertical slice, the map size, and the content budget.

### Staging

1. **Paper pass:** resolve decisions 1–3 and publish the movement table. Update
   the three design docs so they agree.
2. **Vertical slice (8×8):** one d4, Walk, one special, one enemy, one failure
   pressure. No meta, no gold, no 38×38.
3. **Combat pass:** taunt, tank, flying, path preview, hand cap, reroll. Add the
   headless test harness and state dump.
4. **Pressure pass:** torch or deck exhaustion, restart transaction, terminal
   priority. Verify no soft-lock and no free heal.
5. **Meta pass:** pack + bonus, run length, deck floor, gold sink or cut.
6. **Polish:** audio, VFX, accessibility, color-blind path coding.

### What survives the review

The core verb (roll, play, move) is clear. The three-obstacle simplification
removes the hole-death contradiction. The green/orange/grey preview is a strong
basis for the movement UI. The pack-plus-bonus idea is viable once the run
length and the deck budget are fixed. The tank/flying traits are legible if
their interactions are defined.

---

## Round 2 resolution update

The designer answered the three top blockers. The docs were updated to match.

### Deck growth

Treasure tiles now add 1–2 random cards, and each completed level grants a pack
choice. The deck is no longer a fixed 14-card budget; it is a run resource that
grows from treasure and packs. The working model is:

- `RunDeck` is the persistent collection.
- Each level builds a `LevelDeck` from `RunDeck` at level start.
- Treasure cards join `RunDeck` when the level is completed.
- A retry rebuilds `LevelDeck` from the level-start snapshot, so treasure from a
  failed attempt is not kept.

**Residual risk:** treasure density, map size, and deck size must be tuned
together. A 38×38 map needs enough treasure to fund the movement; an 8×8 map
does not. The movement-budget formula is still not published.

### Retries are intended

Torch burnout and deck exhaustion are deliberate level retries, not run losses.
A retry resets HP, deck, hand, enemies, treasure, and the torch. HP 0 remains the
only run loss.

**Residual risk:** a player at low HP can deliberately let the torch burn to
reset HP. If the retry is meant to be a time penalty, this is acceptable; if the
run is meant to carry attrition, the retry should preserve HP or add a retry
counter. The docs now state the intent; the tuning still needs a decision.

### Melee contact damage

Melee enemies now deal 1 contact damage when the player enters their tile. There
is no melee telegraph. Melee enemies do not block movement and can share the
player's tile. Ranged enemies keep the telegraph: entering an adjacent tile
marks the enemy, it attacks at the start of the next turn, and leaving range
cancels.

**Residual risk:** co-occupancy and self-tile targeting. A melee enemy on the
player's tile cannot be targeted by a melee card; the current assumption is that
the player must step off first or use a self-centered effect. This needs a rule
and a UI affordance. The single-occupant `Tile` model was replaced with layered
occupancy in `Architecture.md`.

### Still open after Round 2

1. Treasure density and the movement budget per map tier.
2. Whether a retry should preserve HP or add a retry cost.
3. Melee self-tile targeting and co-occupancy rendering.
4. Real-time torch pause and alt-tab behavior.
5. 38×38 map viability.
6. Run length.
7. Gold sink.
8. Dice-add schedule and reachable card types.
9. Tank HP value.
10. Flying/sand interaction.

### Recommended next step

Re-run the Goldfish review on the changed sections (deck growth, retries, melee
contact) before freezing the design. The changes resolve the top three blockers
but introduce new tuning questions around treasure density and retry economics.

---

## Round 3 — second Goldfish pass

Three lenses (design, comprehension, implementation) reviewed the changed rules.
The changes resolve the original blockers but create three new blockers and a
set of tuning gaps.

### New blockers

| ID | Blocker | Why it matters |
|----|---------|----------------|
| B13 | Treasure timing is contradictory | The docs say treasure adds cards to the run deck during the level, and also that treasure cards join the run deck only on completion. If completion-only, treasure cannot refill the current level's action budget. If immediate, the retry snapshot and completion merge are undefined. |
| B14 | Retries make HP 0 unenforceable | Both retry triggers reset HP, so a player at 1 HP can burn the torch to heal. The only run loss is HP 0, but the retry systems prevent it. The +1 max HP/full heal bonus is dominated. |
| B15 | Contact melee is an avoidable tax | The pathfinder prefers safe paths and avoids melee tiles; melee enemies do not block; contact is 1 HP on entry with no follow-up. A co-located melee enemy is untargetable by melee cards, so removing it costs a step-off plus an attack, or Seraphil. |
| B16 | Jump landing contradicts occupancy | `Game-Design.md` §4.3 says the landing tile must be "walkable and unoccupied"; §6.2 and `Architecture.md` say landing on a melee enemy tile is allowed and deals contact damage. |

### New major gaps

1. **Effective action budget.** The draw pile is `RunDeck − opening hand`, so a
   14-card deck with a 5-card hand gives about 9 plays, not 14. Adding dice
   compresses those plays into fewer turns; it does not add capacity. Maps and
   treasure are still sized as if the full deck is available.
2. **Treasure density vs map size.** Constant per-tile density overfunds large
   maps by area; path-proportional placement can underfund off-path treasure.
   The 38×38 tier needs a published traversal, treasure count, and torch budget.
3. **Treasure detours cost torch time.** Treasure is the budget refill, but it is
   off the direct path. A flat 3–6 minute torch makes large maps worse: the
   refill costs the same clock it is racing.
4. **Random growth dilutes Walk share.** Treasure and packs add unweighted cards,
   so the movement share can fall as the deck grows. Publish a Walk floor or a
   non-Walk movement option.
5. **No deck floor.** Removal can shrink the run deck below the 5-card opening
   hand, causing an immediate retry loop with no player action.
6. **Terminal priority is still undefined.** HP 0, exit, deck exhaustion, and
   torch burnout can co-occur. A deck-exhaustion retry could erase an HP-0 run
   loss, and a torch tick could erase an exit win.
7. **Data model gaps.** There is no `CardInstance`, `LevelSnapshot`, treasure card
   pool, pack schema, or gold field in `RunState`. The reset manifest omits
   position, spent pile, dice index, reroll flag, sand overlays, telegraphs,
   gold, RNG, and animations.
8. **Retry determinism is undefined.** A retry may restore or re-roll the deck
   order, treasure, and enemies. Restoring lets the player memorize a losing
   hand; re-rolling lets them farm a better opening.

### Recommended resolutions

- **Treasure lifecycle:** add a third collection, `LevelGainedCards`. Treasure
  enters the current level deck immediately, merges into `RunDeck` on completion,
  and is discarded on retry. This makes treasure the in-level refill and keeps
  the retry snapshot clean.
- **Retry cost:** either preserve HP on retry, or add a retry counter with an
  escalating cost (max HP, gold, or card burn). If the game is a time attack,
  say so and remove the permadeath framing.
- **Melee threat:** make contact repeat at `TurnStart` while co-occupied, or make
  melee enemies block and attack adjacent. If contact stays entry-only, add a map
  validator requiring at least one unavoidable melee zone per combat map.
- **Jump landing:** replace "unoccupied" with "no blocking occupant." Melee
  enemies do not block landing; ranged enemies do.
- **Deck floor:** set the run deck minimum to the opening hand plus one movement
  card, and enforce it in the removal bonus.
- **Terminal priority:** define one order, for example
  `Lose > Win > LevelRestart > Burnout`, and resolve once after each command.
- **Budget formula:** publish `plays ≈ RunDeckSize − openingHand` and size maps
  against it, not against deck size.

---

## Round 4 — resolutions

The designer resolved the second-pass blockers. The docs were updated to match.

### Treasure lifecycle

Treasure now opens **once**. On open, its 1–2 random cards enter the current
level deck immediately. The claimed cards are kept across retries; the opened
treasure tile disappears and does not reappear. On completion, the claimed cards
merge into the run deck. This makes treasure the in-level action-budget refill
without allowing the same treasure to be farmed on a retry.

**Residual risk:** the claimed cards are kept after a failed attempt, so a retry
starts with the same extra cards but one fewer treasure tile. Treasure density
and the map movement budget still need a published formula.

### Retry HP

A retry restores HP to the value the player had when the level started, not to
full. This removes the free-heal loop: a player at low HP cannot burn the torch
to heal. The retry still resets the torch, enemies, and unopened treasure.

**Residual risk:** a player who enters the level at 1 HP retries at 1 HP, so a
single mistake can end the run. This is a deliberate difficulty choice; tune the
entry HP and healing sources together.

### Enemy speed

Enemies now have an attack type (melee/ranged), a **speed type** (fast/slow), and
a body type (normal/tank/flying).

- **Fast** enemies attack immediately when the player enters their range.
- **Slow** enemies attack at the start of the next turn, before the first die,
  if the player is still in range. Leaving range cancels. If the player stays,
  the attack repeats every turn.
- Melee range is the enemy's tile. Ranged range is adjacent.
- Fast enemies attack on entry only; leaving and re-entering triggers them again.

**Residual risk:** fast melee is still an avoidable entry tax because melee does
not block; a co-located melee enemy is still untargetable by melee cards. The
speed matrix reduces the problem but does not remove it. Tune the speed mix and
clump density, or add a self-tile targeting rule.

### Still open after Round 4

1. Treasure density and the movement budget per map tier.
2. The action-budget formula (`RunDeckSize − openingHand`) and map sizing.
3. Self-tile targeting for a melee enemy on the player's tile.
4. Deck floor to prevent an immediate retry loop after removal.
5. Terminal priority for HP 0, exit, deck exhaustion, and torch burnout.
6. `CardInstance`, `LevelSnapshot`, treasure pool, pack schema, and gold state.
7. Fast enemy re-entry and slow enemy repeat tuning.
8. Whether fast enemies should also attack at `TurnStart` while the player stays.
9. Run length, dice-add schedule, tank HP, and flying/sand interaction.
