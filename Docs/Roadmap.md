# Roadmap & Build Order

> Milestone-based plan. Each milestone ends with a playable/verifiable state.

---

## M0 — Foundation (grid + dice + gating)
- [ ] Create grid map, tile rendering, camera setup.
- [ ] Tile types + walkability rules.
- [ ] Dice model (d4/d6/d8) + roll one-at-a-time.
- [ ] Card-type → lucky-number gating (Normal always playable).
- [ ] Minimal hand UI + dice UI.
- **Verify:** roll a die, see which card types highlight.

## M1 — Movement & drag-drop
- [ ] Deck/hand/discard (draw 5 at map start).
- [ ] Walk card (1 × dice) + drag-drop onto tile.
- [ ] BFS range highlight + valid-tile preview.
- [ ] Jump 1 (2-tile leap, 8 directions, skip rules).
- **Verify:** navigate a small map with holes/obstacles via Walk + Jump.

## M2 — Combat & healing
- [ ] Player HP + enemy HP.
- [ ] Damage / Heal / RemoveObstacle effects (melee/ranged ranges).
- [ ] Stationary enemies (Sentry, Archer, Brute, Anchor) + attack conditions.
- [ ] Enemy telegraph (warn turn 1, attack turn 2).
- [ ] Enemy phase between player turns.
- **Verify:** kill enemies, take damage, heal, remove obstacles.

## M3 — Area & terrain effects
- [ ] Seraphil cross damage (centered on player).
- [ ] Zulfar sand flood (fill holes, clear destroyable obstacles, AoE damage).
- [ ] Obstacle taxonomy (damaging / destroyable / indestructible).
- **Verify:** use terrain/area cards to reshape the battlefield.

## M4 — Level flow & win/lose
- [ ] Exit detection, gold pickup, hole → level restart.
- [ ] Win/lose states + run lifecycle.
- [ ] Enemy AI pass (ranged line-of-sight, telegraph timing).
- **Verify:** complete a full map, lose a run, fall into a hole.

## M5 — Roguelike meta & polish
- [ ] Between-map card pack + remove 1–3 cards + gold shop (heal/removals/reroll).
- [ ] Dice add/upgrade/drop progression.
- [ ] End-of-turn draw (+1 per kill).
- [ ] Audio (FMOD), VFX, animation polish, seed-based RNG.
- **Verify:** full run across multiple maps with deck/dice growth.

---

## Design decisions (resolved)
- Draw: 1 card at end of turn (+1 per enemy kill).
- Hole: level restart only (not a run loss).
- Treasure: gold — heal/removals/reroll between maps; also score.
- Starting deck: 14 cards (see Game-Design §7.1).
- Permadeath: HP 0 ends the run.

---

## Team workflow notes
- Empty folders are preserved via `.gitkeep`; Unity generates `.meta` on open —
  commit them.
- Art/audio binaries are Git LFS-tracked (already configured).
- Scripts are split into `_Game.Runtime` and `_Game.Editor` assemblies.
- Add `DOTween.asmdef` if tweening is needed from `_Game.Runtime`.
