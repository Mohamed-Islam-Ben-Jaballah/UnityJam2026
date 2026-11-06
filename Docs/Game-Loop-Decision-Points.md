# Dice-Drive — Decision Points for Clarification

**Purpose:** Turn the audit findings into answerable design decisions. Each item states the issue, the reason it matters, some directions worth considering, and the open question to answer. These are not recommendations; they are the branches that change the game most.
**How to use:** Answer by number in chat, or fill the `Your answer` line. Partial answers are fine. I will fold the answers back into the audit and then run the Goldfish review.

---

## D1 — How card supply scales with dice (from C1)

**Issue:** One play per die, but only one draw per turn. At two dice the hand drains unless enemies die.

**Directions worth considering:**
- Draw one card per die rolled at the end of the turn, so actions and cards move together.
- Refill the hand to a fixed size at the start of each turn instead of drawing a fixed number.
- Keep one draw per turn, but make extra dice grant movement or utility instead of extra card plays.
- Draw one per turn plus a small refill when the hand falls below a threshold.

**Question:** How many cards should a player expect to play per turn at one, two, and three dice, and where should those cards come from?

`Your answer:`

---

## D2 — Are packs mandatory, optional, or a choice? (from C2)

**Issue:** The example pack adds about 15 cards while free removal takes at most 3, so the deck bloats. If packs are optional, skipping them is the safe play.

**Directions worth considering:**
- Offer a choice of three small packs (3–5 cards) instead of one fixed large pack.
- Keep free removal at 1–3 and add a deck-size cap.
- Make the pack optional but give a different reward (gold or a die upgrade) when declined.
- Make removal cost gold and keep packs free, so growth and trimming trade off.

**Question:** Is the pack a reward the player must take, or a choice among options, and should the deck be allowed to grow without limit?

`Your answer:`

---

## D3 — What makes the player hurry? (from C3)

**Issue:** There is no turn limit, no chase, and no escalating hazard, so waiting is free.

**Directions worth considering:**
- A visible turn limit per map, with a hazard or loss when it runs out.
- Enemy reinforcements every few turns.
- A resource that decays each turn (for example, a torch or corruption meter).
- Enemies that pursue slowly, so standing still is dangerous.
- Accept stalling as a valid low-risk, low-score path.

**Question:** What should make the player hurry, and what happens when that pressure is ignored?

`Your answer:`

---

## D4 — Are holes lethal hazards or just barriers? (from C4)

**Issue:** Holes are non-walkable, and no rule lets the player enter one. The restart rule and Roadmap M4 cannot happen.

**Directions worth considering:**
- Let Walk step onto a hole, trigger the fall, and restart the level.
- Remove the hole-restart rule and treat holes only as barriers that sand can fill.
- Make holes deal damage and return the player to the map start without a full restart.
- Keep holes as barriers, but make falling possible only through a push or a failed jump.

**Question:** Should holes be a real hazard, and what should the penalty be when the player falls?

`Your answer:`

---

## D5 — Should combat be mandatory? (from C5)

**Issue:** Walk is always playable, enemies are avoidable, treasure is optional, and the exit is the only win condition. Walking past everything works.

**Directions worth considering:**
- Lock the exit until a key or a number of kills is reached.
- Put enemies in chokepoints that cannot be walked around.
- Make treasure required to progress (keys, map pieces, or currency for the next map).
- Add a boss or guard at the exit that must be defeated.
- Accept the pacifist line as a valid low-score strategy and reward speed.

**Question:** Is skipping combat an allowed strategy, and what should force or reward engagement?

`Your answer:`

---

## D6 — Should the roll gate movement, or only specials? (from H1)

**Issue:** Normal is always playable and Walk is Normal, so the roll rarely gates the core action.

**Directions worth considering:**
- Keep Normal always playable and accept that the roll gates only specials.
- Remove the Normal exemption and give the player a free one-tile move each turn as a fallback.
- Make Walk type-gated, with a weaker basic move available on any roll.
- Keep Normal always playable but make Normal cards weaker on non-Normal rolls.

**Question:** Should the roll decide whether the player can move, or only which special options are open?

`Your answer:`

---

## D7 — Should die upgrades always help? (from H2)

**Issue:** Adding a d6 adds faces 5 and 6, which are dead if the deck lacks Desert and Devil cards. A reward can make the player worse.

**Directions worth considering:**
- Let the player choose which face to add when upgrading.
- Allow one reroll per turn.
- Add a wild face that counts as any number.
- Show which cards each face can actually play before the player commits.
- Make extra dice add actions rather than new numbers.

**Question:** Should a die upgrade always be an improvement, and how much control should the player have over its faces?

`Your answer:`

---

## D8 — Should card types change how a card plays? (from H3)

**Issue:** Leapol, Qa'faz, and Maoyi are the same jump. Squimi, Sara'akh, and Sunhai are the same damage. Only the unlock number differs.

**Directions worth considering:**
- Give each type a rider (Elf draws, Devil costs self HP, China changes terrain, Angel heals, Desert spreads sand).
- Cut the clones and make fewer, more distinct cards.
- Keep the clones but add a bonus for playing two cards of the same type in one turn.
- Keep them as is and treat type as a cost, not a playstyle.

**Question:** Is a card type supposed to change how the card plays, or only when it can be played?

`Your answer:`

---

## D9 — Does a telegraph lock the hit? (from H4)

**Issue:** The docs say an enemy warns on turn 1 and attacks on turn 2, but not what happens if the player leaves range. The answer decides whether kiting works.

**Directions worth considering:**
- Lock the hit: once warned, the attack lands even if the player leaves range. This creates real tension.
- Reset the warning when the player leaves range. This makes enemies easy to dodge.
- Make the telegraph follow the player's last known position.
- Cancel the attack if the player leaves range, but keep the enemy warned for the next time.

**Question:** When an enemy warns, should leaving its range cancel the attack or not?

`Your answer:`

---

## D10 — Should enemies stay stationary? (from H5)

**Issue:** All enemies are stationary and differ only by HP, range, and anti-jump. They are obstacles, not threats.

**Directions worth considering:**
- Keep them stationary and design maps around their zones.
- Add simple behaviors: patrol, guard a tile, or overwatch a line.
- Reduce to two archetypes and invest the time in map layouts.
- Add one mobile enemy type to create chase pressure.

**Question:** Should enemies remain stationary, or should some of them move or patrol?

`Your answer:`

---

## D11 — What can the player do when the roll is bad? (from H6)

**Issue:** The die order is fixed and there is no reroll or wild face. A hand of Devil cards with Normal rolls leaves the player stuck.

**Directions worth considering:**
- Let the player choose the order of dice.
- Allow one reroll per turn.
- Let the player spend HP or gold to unlock a type for one play.
- Add one wild face to each die.
- Let the player discard a card to change the roll.

**Question:** What options should the player have when the roll does not match the hand?

`Your answer:`

---

## D12 — Core rules that are currently undefined (from M1 and M2)

**Issue:** It is not stated whether playing a card is mandatory, what the hand limit is, whether a played card returns or stays in the discard, when the deck reshuffles, or whether Walk can cross damaging tiles.

**Directions worth considering:**
- Playing is optional, so the player can pass and keep cards.
- Hand limit is 5, and the player discards down at the end of the turn.
- A played card goes to the discard and returns only after a reshuffle.
- Walk can cross damaging tiles at 1 HP per tile, or cannot cross them at all.
- Define tile predicates explicitly: walkable, pass-through, jumpable, and blocks-jump.

**Question:** Should play be mandatory, what is the hand limit, and can Walk cross damaging tiles?

`Your answer:`

---

## D13 — Meta triggers and run length (from M6)

**Issue:** The docs do not define what a level-up is, whether dice rewards are a choice, how many maps a run contains, or how big a map is.

**Directions worth considering:**
- A level-up happens when the player reaches the exit.
- One die reward per map: add, upgrade, or drop.
- A run is three to five maps.
- Map size is 7×7 to 9×9 with two to four enemies.
- Gold carries over between maps; HP does not fully reset.

**Question:** What triggers upgrades, how long is a run, and how big are the maps?

`Your answer:`

---

## D14 — Should area cards cost something? (from M3 and M4)

**Issue:** Seraphil and Zulfar are free, self-centered area effects with no energy cost or downside. Meanwhile Devil (6) and China (8) are rare, so those cards are often unplayable.

**Directions worth considering:**
- Add a cost to area cards: self-damage, a discard, or once per map.
- Let special cards work on two numbers instead of one.
- Add a wild face so rare types appear more often.
- Keep area cards strong but rare; make their effect decisive when it lands.
- Reduce the area or effect and remove the cost.

**Question:** Should area cards have a cost, and should rare card types be easier to play?

`Your answer:`

---

## Items not covered here

The audit also lists lower-severity items: onboarding pace, invented card names, lack of combos, missing map specs, the kill-draw description conflict between §2 and §9, the unused `Ranged` type, Archer line of sight, hole-restart state, and the difficulty of reaching a run loss. These can be answered after D1–D14, or folded into the same pass if you want.

---

## Round 1 answers (as given)

| # | Answer |
|---|--------|
| D1 | Draw 1 per die rolled plus 1 per kill, cap 8; at 8 the oldest card in hand burns. |
| D2 | Choose 1 of 3 small packs, 1–3 cards. |
| D3 | A torch that dims over time; when it burns out, return to the starting point. Theme can change, but the time constraint stays. |
| D4 | Holes are barriers; only a jump crosses them. The player can click a hole to suicide; auto-pathing avoids holes. |
| D5 | Some levels are enemy-blocked; some can be completed pacifist. |
| D6 | Normal is always playable; Normal "adds/multiplies the dice value." |
| D7 | Start with one die; add one die per level. Extra dice allow more card plays per turn. |
| D8 | Type is only a playability test: a card is playable only on its lucky number. |
| D9 | B — the warning resets when the player leaves range. |
| D10 | A — enemies stay stationary. Two logical types (melee and ranged), many visual variants. |
| D11 | Spend 1 life for an extra reroll. |
| D12 | Playing is mandatory. A bad roll with no Normal card in hand costs 1 HP. |
| D13 | No level-up. Per level, pick one bonus: +1 max HP and full heal, remove more cards, or an effect to be determined. |
| D14 | Rarity is the cost. No cost on the card itself; you need the lucky number and the card. Keep specials as is. |

---

## Round 2 follow-ups created by those answers

These are not repeats. They are the rules that the Round 1 answers leave open, and the conflicts the answers introduce.

### F1 — Burn timing and permanence

**Conflicts with:** D1 and the existing draw rules.

- Does the oldest card burn immediately when drawn, or at end of turn?
- Does a burned card go to the discard (and can return) or is it gone for the rest of the map/run?
- When several cards are drawn at once, does each one burn one old card?
- Is the kill draw immediate or end-of-turn? `Game-Design.md` §2 and §9 describe it differently.

### F2 — Torch drain and burnout state

**Conflicts with:** D3 and D4, which both return the player to the start.

- What drains the torch: one tick per turn, per die rolled, per card played, or per tile moved?
- On burnout, does the whole level reset (enemies, treasure, torch), or does the player return to start with the torch refilled while the map keeps its changes?
- If the torch resets and the deck cannot solve the level, can the player loop forever? If the torch does not reset, is the run stuck at the start with a dead torch?
- What is the fail-forward rule when the torch burns out?

### F3 — Clicking a hole

**Conflicts with:** D4 and the earlier hole-restart rule.

- Does clicking a hole restart the level, cost 1 HP and return to start, or end the run?
- Does the torch refill after a hole restart?
- Is there a confirmation prompt, or can a misclick end the attempt?
- If clicking a hole restarts the level, is it a free reset button that can be used to reroll a bad start?

### F4 — What "Normal adds/multiplies the dice value" means

**Conflicts with:** D6, `Game-Design.md` §4.3 (Walk moves 1 × dice value), and the identity risk in H1.

- Does every Normal card use the die value, and are Normal cards the only scaling cards?
- Do some Normal cards add the die value and others multiply it?
- If Normal is always playable and also scales with the die, specials become optional decoration. Is that intended?

### F5 — Die progression and the d6/d8 question

**Conflicts with:** D7, `Game-Design.md` §3.1–3.3, and D8.

- Which die is added at each level, and does the player choose it?
- If every added die is a d4, how are Desert (5), Devil (6), and China (8) ever rolled?
- If d6/d8 are added, does the dead-face dilution from H2 return when the deck lacks those types?

### F6 — The cost and cap of a reroll

**Conflicts with:** D11 and D12, which already use HP as a cost.

- Is "1 life" the same as 1 HP, or a separate resource?
- Is the reroll once per turn, or unlimited while HP remains?
- With healing and a 6 HP pool, an uncapped reroll lets the player buy the face they want and the lucky-number gate stops mattering.

### F7 — Mandatory play versus a bad card

**Conflicts with:** D12 and D11.

- If the only playable card is bad for the player (Walk into danger, Heal at full HP), must they play it?
- Can they always choose to lose 1 HP instead of playing?
- Does the 1 HP penalty apply only when no card is playable, or also when the player refuses to play?

### F8 — Walk over damaging tiles and card destination

**Conflicts with:** `Game-Design.md` §5.1 (damaging is "pass-through") and §4.3 (Walk needs a clear walkable path).

- Can Walk cross a damaging tile? If yes, is it 1 HP per tile or 1 HP per crossing?
- Does a played card go to the discard and reshuffle when the deck empties, return to hand next turn, or exhaust for the map?
- Does a burned card use the same destination as a played card?

### F9 — Between-map flow, run length, and map size

**Conflicts with:** D2, D13, and the missing specs.

- Each map, does the player get a pack and a bonus, or one of the two?
- Is the free removal of 1–3 cards still there, or is removal only the bonus?
- How many maps is a run: 3, 5, 7+, or endless?
- What are the map size and enemy counts: 7×7 with 2–3, 9×9 with 3–4, 11×11 with 4–6, or varied?

### F10 — Enemy-blocked levels and the two-type rule

**Conflicts with:** D5, D9, D10, and the four archetypes in `Game-Design.md` §6.2.

- What makes a level "enemy-blocked": an enemy in a one-tile corridor, an exit locked until a kill, or an enemy guarding the only bridge?
- Are Brute (2 HP) and Anchor (blocks jump) kept as stat/trait variants of the two logical types, or cut?
- If telegraphs reset (D9) and enemies are stationary (D10), what stops the player from kiting the blocker and walking past it?

### F11 — Area cards without a cost

**Conflicts with:** D14 and M3.

- Rarity is the only cost. Is that enough when a player finally gets Seraphil or Zulfar, given that both are free to play and Zulfar also reshapes terrain?
- Should either card be once per map, or is the rarity gate the whole plan?

---

## Round 2 answers (as given)

| # | Answer |
|---|--------|
| F3b | The torch resets on death. |
| F9b | Healing costs 1 card. Cards are used, not infinite. Using a card removes it and you draw another next turn. |
| F5 | Normal is walking. You cannot finish by walking alone. You are forced to walk the die amount of steps, so you can be forced into damaging obstacles, enemy range, or a hole. |
| F6 | +1 die on some levels, not all. |
| F11b | Two enemy attack types: melee attacks only if you step on its tile; ranged attacks if you are in an adjacent tile. The enemy body can be tank or flying. |
| F4 | Clicking a hole is a free level reset. The UI handles misclicks. |
| F1 | A — burn immediately on draw; burned cards are gone for the map. |
| F3a/b | Time burns the torch (game time, not turns). Burnout causes a full level reset. |

---

## Round 3 follow-ups

These are the rules still missing, plus three contradictions created by the Round 2 answers.

### Conflict 1 — "Cards are used, not infinite" against the deck-builder loop

If a played card is gone for the map, a 14-card deck runs out. If it goes to the discard and reshuffles, it is not gone. The answer changes whether the game is a deck-builder or a consumable-card puzzle.

### Conflict 2 — Forced walking against holes as barriers

If auto-pathing avoids holes, Walk cannot force the player into one. If Walk must take exactly N steps, the design needs a rule for a blocked path and for a route that crosses a hole.

### Conflict 3 — Free hole reset against torch pressure

A free full reset removes the penalty for falling and for a bad start. If burnout also resets the torch, the only cost of both is real time. "You cannot finish by walking" is not a rule yet; only enemy-blocked maps can stop it.

### G1 — Card lifecycle

- **G1a. Played cards:** A. Go to discard; reshuffle when the deck empties (standard deck-builder) / B. Gone for the rest of the map; return next map / C. Gone for the rest of the run / D. Other
- **G1b. Burned cards (F1-A):** A. Gone for the map, return next map / B. Gone for the run / C. Go to discard / D. Other

### G2 — Walk model

- **G2a. Who chooses the path?** A. Player clicks a destination; the game auto-paths / B. Player steps tile by tile / C. Player picks a direction and slides N tiles / D. Other
- **G2b. Distance:** A. Exactly N / B. Up to N / C. Exactly N if possible, otherwise stop at the last legal tile / D. Other
- **G2c. Damaging tiles:** A. Walk crosses them at 1 HP per tile / B. 1 HP per crossing / C. Walk cannot cross them / D. Other
- **G2d. If the only N-step route crosses a hole:** A. That destination cannot be selected / B. The player falls / C. The player stops before the hole / D. Other

### G3 — Torch clock and reset

- **G3a. Clock type:** A. Real seconds / B. In-game ticks per action (die, card, or tile) / C. Per turn / D. Other
- **G3b. Burnout reset includes:** A. Everything (HP, deck, hand, enemies, treasure, torch) / B. Position, enemies, treasure, and torch, but HP and deck stay / C. Position and torch only / D. Other
- **G3c. Burnout result:** A. Free retry / B. Costs 1 HP / C. Ends the run / D. Other

### G4 — Hole reset

- **G4a. Effect:** A. Same as torch burnout (full reset) / B. Free reset, but the torch does not refill / C. Costs 1 HP / D. Other
- **G4b. Can it be used to reroll a bad start?** A. Yes / B. No, the level is fixed / C. Other

### G5 — Reroll cost

- A. 1 HP per reroll, once per turn / B. 1 HP per reroll, unlimited while HP remains / C. A separate life resource, once per map / D. Other

### G6 — Mandatory play

- A. Must play a playable card, even if bad / B. May pass by losing 1 HP / C. May pass freely; the 1 HP penalty only applies when no card is playable / D. Other

### G7 — Die progression

- **G7a. Which levels grant a die?** A. Fixed levels grant +1 die; the rest grant cards or bonuses / B. The player chooses +1 die or another bonus at each level / C. Dice are d4 only; 5, 6, and 8 come from special cards or events / D. Other
- **G7b. Are d6/d8 ever added?** A. Yes / B. No / C. Only through a specific reward / D. Other

### G8 — Enemy contact and body types

- **G8a. Can the player enter a melee enemy's tile?** A. Yes, and takes damage on entry / B. No, it blocks / C. Yes, but only to attack it / D. Other
- **G8b. What do tank and flying change?** A. Tank has more HP; flying ignores holes and terrain / B. Tank has more HP; flying can be jumped over / C. Tank blocks; flying ignores ground effects / D. Other
- **G8c. Does the telegraph still apply?** A. Yes, warn then hit / B. No, contact and ranged hits are immediate / C. Other

### G9 — Between maps and run shape

- **G9a. Per map:** A. Pack + bonus / B. Pack or bonus / C. Bonus only / D. Other
- **G9b. Free removal:** A. 1–3 every map / B. Only through the bonus / C. None / D. Other
- **G9c. Run length:** A. 3 maps / B. 5 / C. 7+ / D. Endless
- **G9d. Map size:** A. 7×7, 2–3 enemies / B. 9×9, 3–4 / C. 11×11, 4–6 / D. Varies

### G10 — Kill draw timing

- A. Immediate on kill, plus the end-of-turn draw / B. End of turn only, +1 per kill / C. Immediate only / D. Other

### G11 — What makes an enemy-blocked level real?

- **G11a. Block method:** A. A one-tile corridor with a stationary enemy / B. Exit locked until a kill / C. An enemy guards the only bridge / D. Other
- **G11b. Open question:** If the enemy can be kited, reset, or walked around, what physically stops the player from reaching the exit?

---

## Round 3 answers (as given)

| # | Answer |
|---|--------|
| Deck | If the deck ends on a map, the player dies. |
| Movement simplification | Three obstacle types: holes (jumpable), damaging obstacles, and pillars/walls (impassable). Holes are jumpable obstacles; the player never falls into them. |
| Walk | The player chooses the end tile; the game auto-paths. A safe path is preferred. If no safe path exists, the path is orange/red and the player takes damage along the way. A grey path cannot be taken. Walk is 1 to N steps. |
| Cards | Burned and played cards are the same thing. Use "discarded" for both. A discarded card cannot be played until the next turn. |
| Torch | The torch runs in real seconds. Burnout restarts the level from the start. A correctly played level takes 3–6 minutes. |
| Holes | No more hole resets. The player cannot fall into a hole. Holes are obstacles that require a jump or sand. |
| Reroll | Reroll costs 1 HP. Rerolling at 1 HP kills the player. |
| Mandatory play | Play a card, or reroll if no card can be played. If no card can be played, a reroll must happen. |
| Progression | Define at a later stage. |
| Melee | Entering a melee enemy's range taunts it. It attacks at the start of the next player turn, before the first die is rolled. The player must kill it. |
| Between maps | Pick 1 of 3 packs, then 1 of 3 bonuses. Each option set can be rerolled once. |
| Kill draw | Immediate: draw 1 on a kill, 2 on a tank kill. |
| Enemy block | Depends on level design; handled by the level designer. |
| Healing | Heilin is a lucky card. Rolling its number lets the player play it; it heals 1 with no extra cost. |

---

## Round 4 follow-ups

These are the remaining blockers. After these, the concept can be anchored and sent to the Goldfish review.

### H1 — The deck model

"Played cards are discarded and cannot be played until next turn" and "if the deck ends on a map you die" describe two different games. Pick one.

- **H1a. Card lifecycle:** A. Finite deck; played cards go to a discard that never returns; an empty deck kills the player (consumable cards) / B. Finite deck; the discard shuffles back when the deck is empty; empty deck and discard kills the player (standard cycling) / C. Played cards return to the deck next turn; the deck is finite for the map; empty kills the player / D. Other
- **H1b. Deck death:** A. Run loss / B. Level restart / C. Lose 1 HP and reshuffle / D. Other

### H2 — Melee range and the taunt

- **H2a. Melee range is:** A. The enemy's own tile / B. Orthogonally adjacent tiles / C. Adjacent including diagonals / D. Other
- **H2b. If the player leaves range after taunting:** A. The attack still lands / B. The attack is cancelled / C. The taunt persists until the player re-enters / D. Other
- **H2c. If the player stays in range after the first hit:** A. The enemy attacks every turn at the start / B. It attacks once, then must be re-taunted / C. Other
- **H2d. Ranged range:** A. Adjacent / B. Up to 2 tiles / C. Other

### H3 — Tank and flying bodies

- A. Tank has 2 HP only; flying can be jumped over / B. Tank has 2 HP and blocks; flying ignores holes and terrain / C. Tank has more HP; flying is immune to sand and ground effects / D. Other

### H4 — When can the player reroll?

- A. Only when no card is playable (forced, 1 HP) / B. Anytime, 1 HP / C. One free reroll per turn, then 1 HP / D. Other

### H5 — Torch timer details

- **H5a. Pause behavior:** A. The timer pauses in menus, pause, and alt-tab / B. It keeps running / C. It pauses only in the pause menu / D. Other
- **H5b. On burnout, does HP reset?** A. Yes, full reset / B. No, HP carries over / C. HP resets to 3 / D. Other
- **H5c. On burnout, does the deck reset?** A. Yes / B. No / C. Other

### H6 — Run shape and between-map

- **H6a. Run length:** A. 3 maps / B. 5 / C. 7+ / D. Later
- **H6b. Map size:** A. 7×7, 2–3 enemies / B. 9×9, 3–4 / C. 11×11, 4–6 / D. Later
- **H6c. Free removal:** A. 1–3 every map / B. Only through the bonus / C. None / D. Other

### H7 — Walk confirmation

- A. Walk is up to N steps. The player picks the end tile. The game auto-paths. A safe path is preferred; a damaging path is orange/red and deals damage; a grey path cannot be selected. Holes are barriers for Walk and require a Jump. Correct.
- B. Different: write the correction.

### H8 — Tank kill draw

- A. 2 cards total / B. 2 extra cards (3 total) / C. Other

---

## Round 4 answers (as given)

| # | Answer |
|---|--------|
| H1a | Consumable cards. Played cards never return. |
| H1b | Deck exhaustion restarts the level. |
| H2a | Melee range is the enemy's own tile. |
| H2b | Leaving range cancels the attack. |
| H2c | If the player stays in range, the enemy attacks every turn. |
| H2d | Ranged range is adjacent. |
| H3 | Tank has more HP and gives 2 cards on kill. Flying ignores sand and cannot be jumped over. Other enemies can be jumped over. |
| H4 | One free reroll, then 1 HP each. |
| H5a | The torch pauses in the menu only. |
| H5b | Burnout fully resets HP. |
| H5c | Burnout resets the deck. |
| H6a | Run length: decide later. |
| H6b | Maps vary: 8×8 tutorial, 12×12 beginner, 38×38 endgame. |
| H6c | Free removal only via the bonus. |
| H7 | Walk model confirmed. Holes cannot be auto-jumped; the player must roll and play a Jump card. |
| H8 | Tank kill draws 2 cards total. |

---

## Final resolved model

1. **Cards are consumable.** Played cards never return this level. The deck is
   finite. An empty deck restarts the level.
2. **Draw:** 1 card per die rolled at end of turn. Kills draw immediately
   (1 card; 2 for a tank). Hand cap 8; overflow discards the oldest card.
3. **Reroll:** only when no card is playable. First reroll free, later rerolls
   1 HP. A paid reroll at 1 HP kills the player.
4. **Mandatory play:** if a playable card exists, the player must play one.
5. **Walk:** up to the die value. The player picks the end tile; the game
   auto-paths. Safe paths are preferred; damaging paths cost HP; unreachable
   paths are grey. Holes require a Jump card.
6. **Terrain:** three obstacle types — holes (jumpable), damaging (walkable at
   HP cost), walls (impassable). The player never falls into holes.
7. **Enemies:** stationary. Melee range is the enemy's tile; ranged range is
   adjacent. Taunted enemies attack at the start of the next turn before the
   first die. Leaving range cancels the attack; staying attacks every turn.
8. **Body types:** normal (1 HP, jumpable), tank (2 HP, kill draws 2), flying
   (ignores sand, cannot be jumped over). Other enemies can be jumped over.
9. **Torch:** real-time timer. Burnout restarts the level. Pauses in the menu
   only.
10. **Between maps:** choose 1 of 3 packs (1–3 cards), then 1 of 3 bonuses.
    Each option set can be rerolled once. Removal comes only from the bonus.
11. **No hole death.** Holes are barriers, not hazards.
12. **No level-up.** Progression is deck-building, bonuses, and occasional extra
    dice.

**Still open:** melee-card targeting on the player's tile, melee enemy movement
blocking, the flying/sand interaction, tank HP, the gold sink, run length,
torch duration, the damaging-tile HP cost, reroll while holding a playable card,
hand-overflow timing, and whether HP 0 is reachable often enough to make the run
feel risky.

---

## Round 3 resolutions

The designer answered the three blockers from the first Goldfish pass.

1. **Deck growth.** Treasure tiles add 1–2 random cards. Each completed level
   grants a pack choice. `RunDeck` is the persistent collection; each level
   builds a `LevelDeck` from it. Treasure cards join `RunDeck` on completion. A
   retry rebuilds `LevelDeck` from the level-start snapshot.
2. **Retries are intended.** Torch burnout and deck exhaustion reset the level;
   they are not run losses. HP 0 remains the run loss. A player can deliberately
   retry to heal; this is a time penalty.
3. **Melee contact damage.** Melee enemies deal 1 contact damage when the player
   enters their tile. No melee telegraph. Melee enemies do not block movement
   and can share the player's tile. Ranged enemies keep the telegraph: enter
   range, leave to cancel, or kill before the next turn. The grid uses layered
   occupancy.

**Still open after Round 3:** treasure density and the movement budget per map
tier, whether a retry preserves HP, melee self-tile targeting and co-occupancy
rendering, real-time torch pause and alt-tab, 38×38 viability, run length, the
gold sink, the dice-add schedule, tank HP, and the flying/sand interaction.
