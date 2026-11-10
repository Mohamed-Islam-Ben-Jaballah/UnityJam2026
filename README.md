# DICE-DRIVE
### Mini Game Design Document (GDD)
**Genre:** Narrative Fantasy / Dice & Card Strategy  
**Engine:** Unity | **Perspective:** 2D Isometric  
**Platform:** PC | **Mode:** Single-player

---

## 1. Game Concept

A lone swordsman journeys through a mysterious kingdom to reunite with his beloved.

His journey is governed by a six-sided die and supernatural character cards representing three philosophies of love: **Union, Desire, and Fortune**.

The player must survive fixed enemy encounters, manage randomized abilities, and reach one of three narrative endings.

**Core Loop:** Roll → Choose Card → Move → Resolve Encounter → Repeat.

## 2. Dice & Movement

- One **D6** per turn.
- **One optional free reroll** per turn.
- Dice result determines maximum movement distance and available faction abilities.
- Movement follows a single predefined path, like Snakes & Ladders.
- Basic Walk moves up to the rolled number of tiles.
- Enemies interrupt movement. Unused movement points are lost.
- Only one card can be played per turn.

**Example:** Roll 6 → Enemy at tile 3 → Play Damage 1 → Kill enemy → Stop at tile 3.

## 3. Cards & Factions

| Dice | Faction | Character | Ability |
|---|---|---|---|
| Any | Basic | Protagonist | Walk × Dice |
| 2 | Elf — Union | Squimi | Damage 1 |
| 2 | Elf — Union | Heilin | Heal 1 |
| 5 | Devil — Desire | Sara'akh | Damage 1 |
| 5 | Devil — Desire | Ha'yen | Withdraw 2 cards |
| 6 | China — Fortune | Maoyi | Jump 1 |
| 6 | China — Fortune | Sunhai | Damage 1 |
| 6 | China — Fortune | Qiahni | Heal 1 |

Dice results 1, 3, and 4 have no special faction activation. Basic remains playable on every result.

## 4. Hand & Focus Economy

**Hand size: 5 cards**
- 2 renewable Basic cards.
- 3 randomized Ability cards.
- Played abilities are consumed and replaced.

**Focus:**
- Basic action = +1 Focus.
- Maximum = 2 Focus.
- Spend 2 Focus to activate any Ability card regardless of dice result.
- Focus resets between levels.

**Between levels:** Keep 1 Ability card from the current hand; replace the remaining 2 randomly.

## 5. Enemies & Health

**Starting HP: 6 (proposed).**

There are two encounter types:

**Visible Enemies**
- Occupy fixed path tiles.
- Basic Walk → Enter enemy tile, take 1 damage, stop.
- Damage card → Kill enemy, enter tile, stop.
- Jump → Skip enemy and avoid damage.

**Hidden Devils**
- Invisible beneath fixed path tiles.
- Trigger when crossed.
- Deal 1 damage and stop movement.
- Disappear after activation.

There are **no obstacles**, free-roaming enemies, or complex enemy AI.

At 0 HP, the run ends.

## 6. Level Design

All five areas use **18 fixed path tiles**, excluding the entrance tile.

| Area | Theme | Visible enemy tiles | Hidden devil tiles |
|---|---|---|---|
| A | Beginning | 3, 9, 14 | 6 |
| B | Union | 2, 7, 14 | 5, 11 |
| C | Desire | 2, 7, 10, 15 | 5, 12 |
| D | Fortune | 2, 6, 11, 15 | 4, 8, 13 |
| E | Finale | 2, 6, 9, 13, 16 | 4, 11 |

- Tile 0 = Start.
- Tile 18 = Exit.
- All enemy positions remain fixed.
- Dice results and deck draws vary each playthrough.
- Tile distributions are provisional until playtested.

## 7. Narrative & Endings

The hero searches for a lost love while uncovering forgotten memories and confronting three interpretations of love.

| Route | Progression | Ending |
|---|---|---|
| Union | A → B → C → E | Love through trust and mutual choice |
| Desire | A → C → E | Love through possession and sacrifice |
| Fortune | A → D → E | Love through acceptance of fate |

The first branch determines the ending.

**Story delivery:** Opening text cinematic, short narration during levels, environmental storytelling, and three distinct illustrated/narrated endings.

## 8. UI & Visual Design

- Subtly illuminated fixed-path tiles.
- Clear distinction between visible enemies and ordinary tiles.
- Hidden devils initially concealed.
- Highlight movement destination and reachable encounters.
- Display D6 result, reroll, five-card hand, HP, and Focus.
- Dark-fantasy pixel-art environments with unique thematic areas.

## 9. Balance Targets

- No progression softlocks caused by dice or cards.
- Meaningful Ability usage instead of repetitive Basic movement.
- Manageable health loss across a full route.
- Different risk levels across narrative paths.
- Short, replayable journeys.

**Testing:** Monte Carlo simulations with randomized dice and card draws, plus human playtests.

## 10. Decisions to Finalize

- What exactly does Ha'yen's **Withdraw 2 cards** do?
- Does Jump end movement immediately after landing?
- Can Heal or Withdraw be combined with movement?
- How can players avoid unavoidable death from unlucky enemy encounters?
- What are the final deck copy counts and death/retry rules?

---

**DICE-DRIVE — Roll. Choose. Advance. Defy Fate.**
