# Dice-Drive — Game-Loop Audit

**Date:** 2026-11-06
**Scope:** Paper design audit of `Docs/Game-Design.md`, `Docs/Architecture.md`, and `Docs/Roadmap.md`.
**Code status:** `Assets/_Game/Scripts/*` contains only `.gitkeep`; there is no gameplay code yet. Every finding below is a design-level risk, not a confirmed runtime bug. It can still be tested on paper or in a quick prototype.

**Method:** Trace one turn and one run step by step, then stress-test the loop against six lenses: contrarian/YAGNI, cost and economy, UX, spec consistency, testability, and long-term maintenance.

---

## Verdict

The loop has a clear verb: roll a die, play one card, move or fight. It does not yet have a working economy or a reason to engage with most of its systems.

Three problems dominate:

1. **Actions, cards, and dice do not scale together.** Extra dice add plays, but card draw stays at one per turn, so hands drain and dice go unused.
2. **The safest winning line avoids the game.** `Walk` is `Normal` and therefore always playable. Enemies are stationary, treasure is optional, and the only win condition is reaching the exit. Walking past everything is reliable; fighting is not.
3. **Several rules contradict each other or are undefined**, so the design cannot be implemented or verified as written.

The dice-type gimmick is the identity of the game, but in the current rules `Normal` is always playable and `Walk` is `Normal`. The roll therefore rarely gates the action that matters (movement). It mostly gates optional combat and utility cards, which the player can often skip.

---

## Resolution status after clarification

The findings below are the original audit. This table records what the design decisions changed.

| ID | Original finding | Resolution | Residual risk |
|----|------------------|------------|---------------|
| C1 | Actions outpace cards | Draw 1 per die; treasure (1–2 cards) and the per-level pack grow the deck | Treasure density vs map size |
| C2 | Forced deck bloat | 1 of 3 packs, 1–3 cards; removal via bonus | Pack size and rarity tuning |
| C3 | Stalling is free | Real-time torch; burnout is an intended level retry | Alt-tab; real-time pressure |
| C4 | Hole death unreachable | Holes are barriers; no falling | Removed |
| C5 | Pacifist Walk dominates | Enemy-blocked maps; damaging paths; torch | Map design must force it |
| H1 | Gimmick is toothless | Accepted: Normal always; specials gated | Identity risk |
| H2 | Die upgrade can downgrade | +1 die on some levels; d6/d8 schedule TBD | Dead faces if the deck lacks types |
| H3 | Clone cards | Accepted: type is only playability | Variety risk |
| H4 | Combat is easy to kite | Melee contact damage; ranged telegraph cancels on leaving | Self-tile targeting; contact tuning |
| H5 | Enemies nearly identical | 2 attack types + 3 body types | Simple but legible |
| H6 | Low agency | One free reroll, then 1 HP | Death spiral at 1 HP |
| M1 | Draw and hand rules undefined | Defined in `Game-Design.md` §4.5 | Overflow discard tuning |
| M2 | Damaging tiles vs. Walk | Walk crosses damaging at HP cost; holes need Jump | HP cost tuning |
| M3 | Area cards have no cost | Accepted: rarity is the cost | Balance risk |
| M4 | Specials have poor expected value | Accepted: lucky-number gate | Dead-card risk |
| M5 | `Ranged` unused; LOS undefined | Ranged card type removed; enemy ranged is adjacent | None |
| M6 | Meta triggers undefined | Pack + bonus; run length TBD | Scope |
| M7 | Hole restart state undefined | No hole death; torch and deck retries are full | None |
| M8 | Death is hard to reach | Accepted: retries are intended; only HP 0 ends the run | Stakes may be too low |

**Still open after the clarification**

1. How a melee card hits an enemy on the player's own tile.
2. Treasure density and the movement budget per map tier.
3. The exact flying/sand interaction.
4. The tank HP value.
5. The gold sink.
6. Run length and torch duration.
7. Whether the damaging-tile cost is per tile or per path.
8. Whether the player may reroll while holding a playable card.
9. Hand-overflow discard timing.
10. Whether retries should preserve HP or add a retry cost.
11. Whether the only run loss, HP 0, is reachable often enough.

---

## Findings by severity (original audit)

### Critical

#### C1. Actions scale with dice; cards do not

**What the docs say:** The dice set is rolled one die at a time and each die allows one card play (`Game-Design.md` §2, §3.3). Draw is one card at the end of the turn, plus a kill bonus (§8, §9). Starting hand is five.

**Why it matters:** At one die, the player plays one card and draws one, so the hand is stable. At two dice, the player wants to play two cards but still draws one, so the hand loses one card per turn unless enemies die. The second die becomes a dead die once the hand is empty. The more dice the player earns, the worse the mismatch becomes.

**Risk:** The reward for progressing (more dice) can reduce the number of real actions. Players will feel punished for upgrading. The fix is to scale supply with demand: draw per die rolled, refill the hand to a fixed size each turn, or let extra dice pay for movement instead of cards.

#### C2. The between-map economy forces deck bloat

**What the docs say:** Between maps the player opens a card pack (example: 3 Elf + 2 Angel + 10 Normal) and may remove 1–3 cards for free (`Game-Design.md` §2, §7).

**Why it matters:** The example pack adds about 15 cards while free removal takes out at most 3, for a net gain of about 12 cards per map. Draw stays at one per turn. A larger deck with the same draw rate means the player sees each card less often. The deck loses identity and the game becomes luck-driven.

**Second problem:** If packs are optional, the best move is to decline every pack and keep removing cards. That is a deck that gets smaller and more consistent with no cost, so progression stops.

**Risk:** Either packs are mandatory and decks become random mush, or packs are optional and the meta is degenerate. Neither outcome supports deck-building decisions. Options: offer a choice of three small packs, cap deck size, scale draw with deck or dice, or make removals cost something and packs add less.

#### C3. Stalling is free, so waiting is optimal

**What the docs say:** Enemies never move and only attack when the player is in range at the enemy phase (`Game-Design.md` §6.2). There is no turn limit, no chase, and no escalating hazard.

**Why it matters:** The player can skip turns, walk out of range after a telegraph, or stand still to draw more cards. Because there is no cost to time, the safe move is to wait until the hand and roll are favorable. Dead rolls and bad hands cost real-world minutes, not resources.

**Risk:** The game rewards patience over decisions. The player optimizes toward the least interactive line. Options: add a clock (turn limit, rising hazard, reinforcements, a resource that decays), make enemies pursue or zone the map, or make the exit time-sensitive.

#### C4. Hole death is unreachable, and the Roadmap test cannot be performed

**What the docs say:** Holes are not walkable (`Game-Design.md` §5.1). `Walk` requires a clear path of walkable tiles, and `Jump` requires a walkable landing tile (§4.3). Falling into a hole restarts the level (§6.3). Roadmap M4 asks the team to verify "fall into a hole."

**Why it matters:** Given the movement rules, the player can never enter a hole. There is no rule for stepping into one, no knockback, and no forced movement. The death-and-restart rule is dead text, and the M4 verification step cannot pass.

**Risk:** A whole penalty system and a test milestone are built on an impossible event. Decide: either holes are enterable hazards (walking onto one triggers the fall), or holes are pure barriers and the restart rule is removed. Update M4 either way.

#### C5. The pacifist Walk strategy bypasses the content

**What the docs say:** `Walk` is a `Normal` card, and Normal cards are playable on any roll (`Game-Design.md` §3.2, §4.2). The starting deck is 8 Walk, 2 Leapol, 2 Squimi, 2 Heilin (§7.1). Enemies are stationary and avoidable. Treasure is gold, and gold buys optional between-map extras (§7). The only win condition is reaching the exit (§6.3).

**Why it matters:** The reliable line is to keep and play Walks, path around enemies, skip optional treasure, and walk to the exit. Combat, healing, obstacle removal, and most special card types are not required. The player can finish maps without engaging the systems the game is built around.

**Risk:** The optimal-to-win strategy is the least interesting one. The design needs a reason combat cannot be skipped: chokepoints guarded by enemies, an exit that requires a key or a number of kills, treasure that is required for progression, or a cost for avoiding fights. If map design is expected to force engagement, that requirement must be written down and tested, not assumed.

---

### High

#### H1. The dice gimmick is toothless because Normal is always playable and Walk is Normal

**What the docs say:** "Normal cards are playable on any roll" (`Game-Design.md` §3.2). Walk is Normal and is the main movement card (§4.2).

**Why it matters:** Every roll is effectively "Normal plus maybe one special type." Since movement is the core action and Walk is Normal, the roll rarely decides whether the player can do the important thing. The gimmick only gates specials, which are optional in most situations.

**Risk:** The game's identity is decorative. To give the gimmick teeth, either movement must also be type-gated (with a fallback basic move), or special cards must be necessary often enough that the roll changes the plan.

#### H2. A die "upgrade" can be a downgrade

**What the docs say:** d4 rolls 1–4; d6 adds 5 and 6; d8 adds 7 and 8 (`Game-Design.md` §3.1, §3.2). Roll 5 unlocks Desert, 6 unlocks Devil, 7 is Normal, 8 unlocks China.

**Why it matters:** If the deck lacks Desert and Devil cards, adding a d6 replaces a die where 3 of 4 faces are useful (Angel, Normal, Elf) with a die where only 4 of 6 are useful. The added faces are dead unless the deck also grows those types. The same hand-starvation problem from C1 makes extra dice less useful.

**Risk:** The UI and the Roadmap present dice upgrades as rewards. A player can take the reward and get worse. Options: let the player choose faces, reroll, or add a wild face; show how many cards each face can actually play; or make new faces unlock something the player already owns.

#### H3. Card variety is cosmetic

**What the docs say:** Leapol, Qa'faz, and Maoyi are all "Jump 1" (`Game-Design.md` §4.2). Squimi, Sara'akh, and Sunhai are all "Deal 1 damage." The only difference is card type and lucky number.

**Why it matters:** The deck contains roughly six unique effects spread across fourteen cards, with three names per effect. Deck-building choices are mostly about which number you can roll, not which effect you want. Players must reread invented names to find the card they already know.

**Risk:** The deck feels like filler. Give each type a distinct rider (for example, Elf jump also draws, Devil damage costs self HP, China damage also changes terrain), or cut the clones and make fewer, more distinct cards.

#### H4. Combat is low-risk and easy to kite

**What the docs say:** Enemies telegraph on turn 1 and attack on turn 2 (`Game-Design.md` §6.2). The check happens in the enemy phase, after the player has acted. Enemies never move.

**Why it matters:** If the telegraph resets when the player leaves range, the player can step in, get the warning, step out, and never take damage. A fast Walk can cross a danger zone and end outside it. Only the Brute (2 HP) survives long enough to matter, and only if the player stays adjacent. One-HP enemies die before they can attack if the player commits.

**Risk:** The bestiary is a set of avoidable obstacles, not a threat. If the telegraph instead locks the attack even when the player leaves, combat becomes an unavoidable tax. That may be the intended tension, but the rule is not written. Define whether telegraphs persist, reset, or follow.

#### H5. Enemies are nearly identical and create no puzzles

**What the docs say:** Sentry (1 HP, range 1), Archer (1 HP, range 2), Brute (2 HP, range 1), Anchor (1 HP, range 1, blocks jump) (`Game-Design.md` §6.2).

**Why it matters:** With stationary enemies, one damage, and a telegraph, the four archetypes differ only by HP, range, and one niche anti-jump trait. There is no positioning play, no pursuit, no combination, and no escalation. Anchor only matters if the player needs to jump, and Walk can usually go around.

**Risk:** The enemy phase becomes a formality. Either give each archetype a behavior that changes the player's path (Archer covers lines, Anchor zones jumps, Brute blocks a corridor, Sentry guards a treasure), or reduce to two archetypes and invest the time in map design.

#### H6. The die, not the player, chooses the options

**What the docs say:** The type unlocked by each roll is fixed by the mapping (`Game-Design.md` §3.2). The dice set is ordered and the next die is chosen for the player (`Architecture.md` §5). There is no reroll, no wild face, and no way to spend a resource to unlock a type.

**Why it matters:** A hand full of Devil cards with repeated Normal rolls leaves the player unable to act. The only reliable play is Normal, which pushes the player away from specials. This is the "game plays you" risk that makes roll-and-play feel passive.

**Risk:** Low agency turns the core loop into waiting. Add a fallback: choose the die order, spend HP or gold to unlock a type, include a wild face, or let the player discard a card to change the roll. Any of these gives the player a decision when the roll is bad.

---

### Medium

#### M1. The draw and hand rules are underspecified

`Game-Design.md` §4.1 says a played card "consumes it for the turn; it goes to the discard pile." Those two statements conflict: a card that goes to the discard is consumed until the deck reshuffles, not just for the turn. The hand limit is not stated. What happens when drawing above the limit is not stated. Whether playing a card is mandatory is not stated. Whether the discard reshuffles automatically is not stated.

These rules decide whether the game has hand management or not. They need one paragraph each before implementation.

#### M2. Damaging obstacles contradict Walk

`Game-Design.md` §5.1 marks damaging obstacles as "pass-through" and says crossing costs 1 HP. §4.3 says Walk needs "a clear path of walkable tiles." `Architecture.md` §5 lists `IsWalkable(coord)` without defining pass-through tiles.

If damaging tiles are not walkable, Walk cannot cross them and the 1 HP cost never applies. If they are walkable, Walk can cross them and should charge HP. Jump landing on a damaging tile is also undefined. Write explicit tile predicates: `Walkable`, `PassThrough`, `Jumpable`, `BlocksJump`.

#### M3. Self-centered area cards have no cost or tradeoff

Seraphil (cross damage) and Zulfar (8-neighbor flood) originate on the player, need no target, and have no energy cost, self-damage, or condition (`Game-Design.md` §4.3). Zulfar also fills holes, destroys destroyable obstacles, and damages enemies.

With no resource beyond the die, both are strictly good when their number appears. Zulfar in particular can reshape the board and clear a group at once. Add a cost (self-damage, discard, a condition) or reduce the area/effect, otherwise these cards will define every encounter that allows them.

#### M4. Special cards have poor expected value

With one action per die and type gating, every special competes with the always-available Walk. Its expected value is the chance of rolling its number multiplied by its effect. On a d6, Devil (6) appears 1/6 of the time. On a d8, China (8) appears 1/8. Late types are rarely playable, and packs that add them mostly add dead weight.

Consider numbers that appear on more faces, wild faces, or cards that work on two numbers. Otherwise, the later card types will feel like traps.

#### M5. `Ranged` is defined but unused, and Archer line of sight is undefined

`Architecture.md` §4 gives `CardData` a `Ranged` range type, and `Game-Design.md` §4.4 says ranged attacks target up to distance 2. No card in §4.2 is ranged; every damage card is melee. Archer range 2 also depends on line of sight, which is not defined (corners, holes, obstacles, other enemies).

This is dead spec plus a likely source of edge-case bugs. Either add a ranged card or remove the range type. Define line of sight with a diagram and a corner rule.

#### M6. The meta triggers are undefined

The docs do not define what a "level-up" is, whether dice rewards are a choice, whether packs can be declined, how many maps a run contains, or what the map size and enemy counts are (`Game-Design.md` §7; `Roadmap.md` M5). Without these, the economy cannot be tuned or tested.

#### M7. The hole-restart state is undefined

If holes become reachable, the design must say what resets: player HP, deck, hand, discard, enemies, treasure, dice, and turn count (`Game-Design.md` §6.3). Without this, a restart can be exploited (farm treasure) or can soft-lock (HP too low to continue).

#### M8. Death is hard to reach

Player HP is 6, all enemies deal 1, telegraphs give a turn to react, and Heilin plus gold healing restore HP (`Game-Design.md` §6.1, §6.2, §7). Combined with kiting and free stalling, a run loss may be rare. That weakens the roguelike stakes. If death is meant to be a real risk, damage or pressure must rise.

---

### Low and polish

- **L1. Onboarding is the least interesting version of the loop.** Map 1 likely has one d4, one action per turn, and mostly Walk. The first minute may feel slow. Consider starting with two dice or a short scripted opening.
- **L2. Invented names plus cloned effects raise cognitive load.** Players will not build a mental model of Squimi vs Sara'akh vs Sunhai. Use readable names or distinct icons and colors per type.
- **L3. There are no combos.** One card per die with no energy or chain limit means deck-builder satisfaction is low. Consider a small bonus for playing the same type twice in a turn, or a once-per-turn extra play.
- **L4. Map and level design are unspecified.** Map size, enemy counts, treasure placement, number of maps, and difficulty curve are all missing. Pacing cannot be validated without them.
- **L5. The kill draw is the only in-turn card engine.** If combat is avoidable, the engine never starts. Tie draw to exploration or movement as well, or make kills necessary.

---

## Spec inconsistencies to resolve before coding

1. Played card "consumed for the turn" vs. "goes to the discard pile."
2. Whether playing a card is mandatory or optional.
3. Hand limit and what happens when drawing above it.
4. Deck reshuffle timing and whether the discard is included.
5. Whether the kill draw is immediate, end-of-turn, or both. `Game-Design.md` §2 and §9 describe it differently.
6. Tile predicates for damaging, destroyable, and indestructible tiles, including Walk pathing and Jump landing.
7. Whether Walk can cross damaging tiles, and if so, whether the HP cost is per tile or per crossing.
8. Line of sight for the Archer, including corners and blockers.
9. Whether a telegraph persists after the player leaves range.
10. Whether the player chooses die order, and whether dice can be rerolled.
11. What a level-up is and what the dice reward choices are.
12. Whether packs are optional and whether the deck has a minimum size.
13. Hole-restart state, if holes become reachable.
14. Map size, enemy counts, treasure counts, number of maps, and run length.

---

## Counter-arguments to this audit

The audit is deliberately harsh. These are the strongest defenses of the current design, and each one is a reason to clarify rather than rewrite.

- **"Maps will force engagement."** If map layouts put enemies in the only corridors and use indestructible walls, the pacifist line fails. This is possible, but it is not in the docs and must be tested map by map.
- **"Telegraphs are meant to lock the hit."** If a warning guarantees an attack next turn, crossing a danger zone costs HP and combat has tension. The rule is missing, but the intent may already be sound.
- **"Short jam runs do not need deep economy."** If a run is three small maps in ten minutes, one action per turn and a shallow meta may be enough. The cost is that the deck-building and dice systems will feel like decoration.
- **"Normal-always is a safety valve."** It prevents dead turns and lets players always move. That is a real benefit. The cost is that the dice gimmick loses teeth. The question is which one the game wants to be.
- **"Spec gaps are normal at this stage."** True. The point is that several gaps hide opposite games (mandatory vs. optional play, persistent vs. resetting telegraphs). Pick one before coding.

---

## Paper tests to run before writing code

1. **Pacifist test:** Can map 1 be completed with only Walk, no attacks, and no treasure? If yes, combat is optional.
2. **Hand-starvation test:** Simulate five turns with two dice, no kills. Count dice that cannot be used because the hand is empty.
3. **Deck-growth test:** Simulate three maps of pack-add and removal. Count the turns needed to draw a specific card in each map.
4. **Kiting test:** Place an Archer, walk into range, then leave. Does the attack still land? Decide the intended answer.
5. **Hole test:** Try to fall into a hole using only the stated movement rules. Confirm it is impossible, then decide the fix.
6. **Roll-agency test:** Build a hand of only Devil cards and roll Normal numbers for three turns. Count how many turns the player has a real choice.

---

## Scope note

The Roadmap lists six milestones (M0–M5) plus art, audio, meta, and tests. If this is a game-jam project, that is a large surface for the time available. The loop must be demonstrably fun by M1–M2. A focused jam build should prove the roll-play-move decision first and treat the deck-building meta as a stretch goal.
