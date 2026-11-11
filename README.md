# Dice-Drive

> A roguelike deck-builder where every roll of the dice decides which cards you
> can play. Roll. Pick. Drag. Survive.

---

## Welcome

Welcome travelers and heroes of all the facets of the earth! Are you ready to
roll your dice and throw your cards? Let fate spin you on a journey across
plates — only if you are brave enough.

<!-- SCREENSHOT: title / hero image -->

---

## The Game in One Minute

You hold a hand of consumable cards. Each turn you roll your dice **one at a
time**, and the number you roll unlocks which card types you may play. Play one
card per die. Move across the grid or follow a painted path, jump holes, fight or
avoid enemies, and reach the exit. A real-time torch burns down while you decide.

If the torch burns out, your deck empties, or your HP reaches 0, you **reset the
level** to the state you entered it with. The run continues.

---

## Map Types

Dice-Drive has two map kinds. Both use the same cards, dice, enemies, torch, and
failure rules. Maps are **4 to 64 tiles per side**.

- **Grid maps** are open tactical boards. You drag Walk to a destination and the
  game auto-paths. Holes are barriers that need a Jump.
- **Linear maps** are painted one-way paths on the same N×M canvas. You only move
  forward along the path. Walk spends the full roll, holes are lethal, and Jump
  crosses two path steps. At a marked branch, pick one of the arrow buttons.
  Merges are automatic. Interior exits pause the walk and ask Continue or Enter;
  the final exit takes itself.

On a linear map you click a card instead of dragging it. Walk and Jump resolve
forward automatically, Damage hits the first enemy ahead within range, and Heal
and Draw are self. Complex cards wait for the next grid map.

---

## How to Play

### Step 1 — Roll the dice

You start with one d6 and add more d6s on some maps. Roll them **one at a time**.
Each die gives one card play. If no card in hand can be played, you must reroll:
you have one free reroll per map; after that, paid rerolls cost 1 HP starting on
map 2, and a paid reroll at 1 HP kills. On the first map, if no card is playable
after the free reroll, the die is skipped.

<!-- SCREENSHOT: the dice rolling / dice UI -->

### Step 2 — Pick a card

Normal cards (Walk) are always playable. Every other type needs its **lucky
number**. Playable cards glow; locked cards show the number they need. Cards are
consumed when played, and the deck is finite. Treasure opens once and grants
cards from its `TreasureData` asset, plus gold and heal; the claimed cards are
kept if you retry, but the opened tile is gone. Taking an exit opens a three-card
reward screen.

<!-- SCREENSHOT: hand of cards with playable cards highlighted -->

### Step 3 — Drag it to an area of use

Walk up to the rolled die value of movement. Diagonal steps cost 2; orthogonal steps cost 1. The path preview shows the route:

- **Green** — safe.
- **Orange/red** — crosses damaging tiles or melee enemies; you take HP damage.
- **Grey** — unreachable.

On a **grid map**, holes are barriers and need a **Jump**. On a **linear map**,
walking into a hole kills you and resets the level; use Jump to cross it. Enemies
have a speed: **fast** enemies attack when you enter their range; **slow** enemies
attack at the start of the next turn if you stay in range. Melee range is the
enemy's tile; ranged range is adjacent, including diagonals.

<!-- SCREENSHOT: dragging a card, valid tiles highlighted -->

### Step 4 — Watch the action

The card resolves instantly. Ranged enemies telegraph: if you enter their range,
they attack at the start of the next turn unless you leave range or kill them.
Then it is the next die's turn.

<!-- SCREENSHOT: a card action in progress -->

---

## Lucky Numbers & the Classes

Normal cards are **always** playable. Every other class answers to a single
**lucky number** — roll it, and that class awakens.

| Roll | Awakens |
|------|---------|
| 1    | Loneliness special effects |
| 2    | Elvish (Union) |
| 3    | Normal only |
| 4    | Normal only |
| 5    | Devilish (Desire) |
| 6    | Sinitic (Fortune and wealth) |

- **Normal** — Walk; move up to the rolled value (diagonal 2, orthogonal 1).
- **Elvish (2)** — Leapol (jump), Squimi (damage), Heilin (heal), MelowCart (tree-root flood).
- **Devilish (5)** — Qa'faz (jump), Sara'akh (damage), Ha'yen (remove obstacle).
- **Sinitic (6)** — Maoyi (jump), Sunhai (damage), Qiahni (draw 2 cards).
- **Loneliness (1)** — Cale Azrael (fire inverted cross); not a family.

> Type decides **when** a card can be played, not what it does. Extra d6s add
> card plays per turn.

---

## Your Goal

Reach the **exit** of each dungeon before the torch burns out.

- **Torch burnout**, an **empty deck**, and **HP 0** reset the level. The reset
  restores HP to the value you entered the level with, restocks the deck and hand,
  resets the free reroll and torch, and respawns enemies and unopened treasure. It is
  not a run loss.
- **Treasure** opens once and grants cards from its `TreasureData` asset, plus
  gold and heal. Claimed cards are kept on a retry; the opened tile is gone.
  Gold's use is TBD.
- **Enemies** hold their ground. **Fast** enemies attack when you enter their
  range. **Slow** enemies attack at the start of the next turn if you stay in
  range. Melee range is the enemy's tile; ranged range is adjacent, including diagonals. Tank enemies
  have 2 HP and draw 2 on death; flying enemies ignore sand and cannot be jumped
  over.
- **Terrain:** on grid maps, holes need a Jump or sand, damaging tiles cost HP,
  and walls block. Linear maps have no walls or sand, and a hole is lethal to
  Walk.
- **Between maps** use an exit. On a grid map, stand on a numbered exit and use
  **Take Exit**. On a linear map, an interior exit asks Continue or Enter; the
  final exit takes itself. Choose one of three three-card reward options, reroll
  each option once, then confirm. The destination map starts in the same scene.
  HP, max HP, gold, owned cards, and the level number carry; the deck restocks,
  the hand resets, the torch resets, and the old map is discarded. The bonus and
  meta stage is deferred.

Get stronger. Go deeper. Reach the exit.

<!-- SCREENSHOT: the exit / victory moment -->

---

## Screenshots

> Drop your images below, replacing each comment with an image tag such as
> `![Description](path/to/image.png)`.

<!-- SCREENSHOT: title / hero image -->

<!-- SCREENSHOT: the dice rolling / dice UI -->

<!-- SCREENSHOT: hand of cards with playable cards highlighted -->

<!-- SCREENSHOT: dragging a card, valid tiles highlighted -->

<!-- SCREENSHOT: a card action in progress -->

<!-- SCREENSHOT: a special class card in action -->

<!-- SCREENSHOT: the between-map deck screen -->

<!-- SCREENSHOT: the exit / victory moment -->
