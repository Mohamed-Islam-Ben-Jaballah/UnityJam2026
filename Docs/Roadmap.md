# Roadmap & Build Order

> Milestone-based plan. Each milestone ends with a playable/verifiable state.

---

## M0 — Foundation (grid + dice + gating + torch)
- [ ] Create grid map, tile rendering, camera setup, path preview skeleton.
- [ ] Tile types: floor, exit, treasure, hole, sand, damaging, wall.
- [ ] Dice model (d4/d6/d8) + roll one at a time.
- [ ] Card-type → lucky-number gating (Normal always playable).
- [ ] Minimal hand UI, dice UI, and torch UI.
- [ ] Real-time torch timer with menu pause and level retry.
- **Verify:** roll a die, see which card types highlight; let the torch burn
  out and confirm the level retries.

## M1 — Movement & card economy
- [ ] Consumable deck / hand / spent pile. Draw 5 at map start; hand cap 8;
  overflow discards the oldest.
- [ ] Treasure adds 1–2 random cards. Completing the level merges them into the
  run deck; a retry restores the level-start deck.
- [ ] Walk (up to die value) with auto-path: green safe, orange/red damaging or
  melee contact, grey unreachable.
- [ ] Jump 1 (2-tile leap, 8 directions). Holes, damaging tiles, and non-flying
  enemies may be skipped; walls and flying enemies block.
- [ ] Draw 1 per die rolled at end of turn; kill draws immediate (1 card, 2 for
  a tank).
- [ ] Mandatory play: play a card, or reroll when none is playable. First reroll
  free, then 1 HP; paid reroll at 1 HP kills.
- **Verify:** navigate a map with holes, damaging tiles, walls, and a melee
  enemy; confirm a safe path is preferred and a melee tile shows contact
  damage; empty the deck and confirm the level retries; reroll at 1 HP and
  confirm death.

## M2 — Combat & enemies
- [ ] Player HP + enemy HP.
- [ ] Damage / Heal / RemoveObstacle effects (melee ranges).
- [ ] Attack types: melee (own tile) and ranged (adjacent).
- [ ] Speed types: fast (attack on entry) and slow (attack at TurnStart if the
  player stays).
- [ ] Layered occupancy: terrain, enemy, and player can share a tile.
- [ ] Normal / tank / flying bodies. Tank has 2 HP and draws 2 on kill; flying
  ignores sand and cannot be jumped over.
- **Verify:** fast melee and fast ranged attack on entry; slow melee and slow
  ranged telegraph and attack at the next turn start if the player stays; leaving
  range cancels; confirm a tank draws 2 and a flying enemy blocks a jump.

## M3 — Area & terrain
- [ ] Seraphil cross damage (centered on the player).
- [ ] Zulfar sand flood: fill holes, cover damaging tiles, damage enemies.
  Walls are unaffected; flying enemies ignore sand.
- [ ] Obstacle taxonomy: holes, damaging, walls.
- **Verify:** use terrain/area cards to reshape the battlefield, fill a hole to
  cross, and cover a damaging tile with sand.

## M4 — Level flow & win/lose
- [ ] Exit detection; treasure opens once, adds 1–2 random cards, and gives
  gold/score.
- [ ] Deck exhaustion → level retry.
- [ ] Torch burnout → level retry.
- [ ] Retry restores HP to the level-entry value, not full. Claimed treasure is
  kept; opened treasure stays gone.
- [ ] Player HP 0 → run ends.
- [ ] Fast/slow enemy attack timing pass.
- [ ] Remove the old hole-death and hole-restart rules.
- **Verify:** complete a full map; empty the deck and confirm the level retries;
  burn the torch and confirm the retry HP equals entry HP; confirm opened
  treasure does not respawn; lose a run at HP 0. There is no "fall into a hole"
  test.

## M5 — Roguelike meta & polish
- [ ] Between-map pack choice: 1 of 3 packs, 1–3 cards each.
- [ ] Claimed treasure is kept across retries and merges into the run deck on
  completion. Opened treasure does not reappear.
- [ ] Between-map bonus choice: 1 of 3 bonuses (+1 max HP and full heal / remove
  1–3 cards / effect TBD).
- [ ] Reroll each option set once.
- [ ] Removal comes only from the bonus; no free removal.
- [ ] Dice add on some levels, not all (schedule TBD).
- [ ] Gold sink (TBD).
- [ ] Audio (FMOD), VFX, animation polish, seed-based RNG.
- **Verify:** full run across multiple maps with pack and bonus choices.

---

## Design decisions (resolved)
- Cards are consumable; played cards never return this level. Claimed treasure
  enters the level deck immediately and is kept across retries; opened treasure
  stays gone. The per-level pack grows the run deck. Empty deck retries the
  level.
- Draw: 1 per die rolled at end of turn; kills draw immediately (1 card, 2 for a
  tank). Hand cap 8; overflow discards the oldest.
- Reroll: only when no card is playable. First reroll free, later rerolls 1 HP.
  A paid reroll at 1 HP kills.
- Walk: up to the die value; player picks the end tile; auto-path prefers safe
  routes, then damaging or fast-enemy routes (HP cost), then unreachable.
- Terrain: three obstacle types — holes (jumpable), damaging (walkable at HP
  cost), walls (impassable). The player never falls into holes.
- Enemies: stationary. Attack type: melee (own tile) or ranged (adjacent). Speed
  type: fast (attacks on entry) or slow (attacks at the start of the next turn
  if the player stays; leaving cancels). Body types: normal, tank, flying.
- Torch: real-time timer; burnout retries the level at the level-entry HP; it is
  a time penalty, not a run loss. Pauses in the menu only.
- Between maps: 1 of 3 packs + 1 of 3 bonuses; reroll each set once.
- No level-up. Progression is deck-building, bonuses, and occasional extra dice.
- Permadeath: HP 0 ends the run. Torch burnout and deck exhaustion are retries.

---

## Team workflow notes
- Empty folders are preserved via `.gitkeep`; Unity generates `.meta` on open —
  commit them.
- Art/audio binaries are Git LFS-tracked (already configured).
- Scripts are split into `_Game.Runtime` and `_Game.Editor` assemblies.
- Add `DOTween.asmdef` if tweening is needed from `_Game.Runtime`.

---

## Scope note
Map sizes are 8×8 for tutorial maps, 12×12 for beginner maps, and up to 38×38
for endgame maps. Treasure cards and the per-level pack are the deck-growth
sources, so treasure density must scale with map size. The endgame size is large
for the early action economy and the 3–6 minute torch. The first playable build
should prove the 8×8 loop before investing in large maps.
