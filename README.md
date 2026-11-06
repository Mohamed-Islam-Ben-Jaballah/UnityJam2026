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
card per die. Move across the grid, jump holes, fight or avoid enemies, and
reach the exit. A real-time torch burns down while you decide.

If the torch burns out or your deck empties, you **retry the level**. If your HP
reaches 0, the **run ends**.

---

## How to Play

### Step 1 — Roll the dice

You start with one die and add more on some levels. Roll them **one at a time**.
Each die gives one card play. If no card in hand can be played, you must reroll:
the first reroll of the turn is free, later rerolls cost 1 HP, and a paid reroll
at 1 HP kills.

<!-- SCREENSHOT: the dice rolling / dice UI -->

### Step 2 — Pick a card

Normal cards (Walk) are always playable. Every other type needs its **lucky
number**. Playable cards glow; locked cards show the number they need. Cards are
consumed when played, and the deck is finite. Treasure opens once and adds 1–2
cards to the current level deck; the claimed cards are kept if you retry, but the
opened tile is gone. Completing a level grants a pack.

<!-- SCREENSHOT: hand of cards with playable cards highlighted -->

### Step 3 — Drag it to an area of use

Walk up to the rolled die value. The path preview shows the route:

- **Green** — safe.
- **Orange/red** — crosses damaging tiles or melee enemies; you take HP damage.
- **Grey** — unreachable.

Holes need a **Jump**. Enemies have a speed: **fast** enemies attack when you
enter their range; **slow** enemies attack at the start of the next turn if you
stay in range. Melee range is the enemy's tile; ranged range is adjacent.

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
| 1    | Angel |
| 2    | Normal only |
| 3    | Elf |
| 4    | Normal only |
| 5    | Desert |
| 6    | Devil |
| 7    | Normal only |
| 8    | China |

- **Normal** — your faithful Walk; move up to the rolled value.
- **Elf (3)** — Leapol (jump), Squimi (damage), Heilin (heal).
- **Devil (6)** — Qa'faz (jump), Sara'akh (damage), Ha'yen (remove obstacle).
- **China (8)** — Maoyi (jump), Sunhai (damage), Qiahni (draw 2 cards).
- **Angel (1)** — Seraphil (deal damage in a cross around you).
- **Desert (5)** — Zulfar (flood the 8 tiles around you with sand).

> Type decides **when** a card can be played, not what it does. Add dice on some
> levels to reach higher lucky numbers.

---

## Your Goal

Reach the **exit** of each dungeon before the torch burns out.

- **Torch burnout** and an **empty deck** retry the level. A retry restores HP
  to the value you entered the level with, not full. **HP 0** ends the run.
- **Treasure** opens once and gives 1–2 random cards and gold. Claimed cards are
  kept on a retry; the opened tile is gone. Gold's use is TBD.
- **Enemies** hold their ground. **Fast** enemies attack when you enter their
  range. **Slow** enemies attack at the start of the next turn if you stay in
  range. Melee range is the enemy's tile; ranged range is adjacent. Tank enemies
  have 2 HP and draw 2 on death; flying enemies ignore sand and cannot be jumped
  over.
- **Terrain:** holes need a Jump or sand; damaging tiles cost HP; walls block.
- **Between maps** choose 1 of 3 packs (1–3 cards), then 1 of 3 bonuses (+1 max
  HP and full heal / remove 1–3 cards / effect TBD). Each set can be rerolled
  once. Removal comes from the bonus.

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
