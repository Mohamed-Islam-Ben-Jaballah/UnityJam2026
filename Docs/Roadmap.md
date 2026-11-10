# Roadmap & Build Order

> Milestone-based plan. Each milestone ends with a playable/verifiable state.

---

## M0 — Foundation (grid + dice + gating + torch)
- [ ] Create grid map, tile rendering, camera setup, path preview skeleton.
- [ ] Tile types: floor, exit, treasure, hole, sand, damaging, wall.
- [ ] Dice model (d6 only; start with 1, add more) + roll one at a time.
- [ ] Card-type → lucky-number gating (Normal always; Elvish 2, Devilish 5, Sinitic 6, Loneliness 1).
- [ ] Minimal hand UI, dice UI, and torch UI.
- [ ] Real-time torch timer with menu pause and level retry.
- **Verify:** roll a die, see which card types highlight; let the torch burn
  out and confirm the level retries.

## M1 — Movement & card economy
- [ ] Consumable deck / hand / spent pile. Draw 5 at map start; hand cap 8;
  overflow discards the oldest.
- [ ] Treasure adds 1–2 random cards. Completing the level merges them into the
  run deck; a retry restores the level-start deck.
- [ ] Walk (up to die value of movement; diagonal = 2, orthogonal = 1) with auto-path: green safe, orange/red damaging or
  melee contact, grey unreachable.
- [ ] Jump 1 (2-tile leap, 8 directions). Holes, damaging tiles, and non-flying
  enemies may be skipped; walls and flying enemies block.
- [ ] Draw 1 per die rolled at end of turn; kill draws immediate (1 card, 2 for
  a tank).
- [ ] Mandatory play: play a card, or reroll when none is playable. 1 free
  reroll per map; paid 1 HP from map 2; paid reroll at 1 HP kills.
- **Verify:** navigate a map with holes, damaging tiles, walls, and a melee
  enemy; confirm a safe path is preferred and a melee tile shows contact
  damage; empty the deck and confirm the level retries; reroll at 1 HP and
  confirm death.

## M2 — Combat & enemies
- [ ] Player HP + enemy HP.
- [ ] Damage / Heal / RemoveObstacle effects (melee ranges).
- [ ] Attack types: melee (own tile) and ranged (adjacent, 8-way; diagonal = 1 range).
- [ ] Speed types: fast (attack on entry) and slow (attack at TurnStart if the
  player stays).
- [ ] Layered occupancy: terrain, enemy, and player can share a tile.
- [ ] Normal / tank / flying bodies. Tank has 2 HP and draws 2 on kill; flying
  ignores sand and cannot be jumped over.
- **Verify:** fast melee and fast ranged attack on entry; slow melee and slow
  ranged telegraph and attack at the next turn start if the player stays; leaving
  range cancels; confirm a tank draws 2 and a flying enemy blocks a jump.

## M3 — Area & terrain
- [ ] Cale Azrael fire inverted cross (centered on the player).
- [ ] MelowCart tree-root flood: fill holes, cover damaging tiles, damage enemies.
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
- [ ] Add d6s on some maps, not all (schedule TBD).
- [ ] Gold sink (TBD).
- [ ] Audio (FMOD), VFX, animation polish, seed-based RNG.
- **Verify:** full run across multiple maps with pack and bonus choices.

---

## Implementation status (post-merge)

| Area | Status | Notes |
|------|--------|-------|
| Core simulation | Implemented | Grid, pathfinder, dice, deck/hand/spent, entities, torch, `GameSession` loop, retry, win/lose. |
| Balancing assets | Implemented | 6 card types, 12 cards, 6 enemies under `Assets/_Game/Data/`. |
| Card UI | Merged, not wired | `Cards_Dice_UI.unity`, `CardSelection`, `HandView`, `CardView`, `HandLayout`, `CardRevealView`, TMP. |
| Audio | Merged, not wired | `FMOD/Test` Studio project, `Master` bank, `SoundDesign.unity`, `CardSelectionAudio`. |
| Integration | Pending | `HandPresenter` bridge from `GameSession.PlayerHand` to `CardSelection`; FMOD card-select event; runtime scene checks. |
| Tests | Pending | No `_Game.Tests` assembly yet. |
| Meta | Pending | Pack/bonus, run length, gold sink, dice schedule. |

Merge commits: `60a5897` (SoundDesign), `868fa38` (MazenUI).

## Next steps after the merge

1. Add a `HandPresenter` bridge from `GameSession` to `CardSelection`.
2. Assign a card-select FMOD event and verify `SoundDesign.unity`.
3. Open `Cards_Dice_UI.unity` and verify deal, hover, select, and reveal.
4. Wire the gameplay scene to the UI and add the `_Game.Tests` assembly.
5. Implement the between-map meta.

---

## Design decisions (resolved)
- Cards are consumable; played cards never return this level. Claimed treasure
  enters the level deck immediately and is kept across retries; opened treasure
  stays gone. The per-level pack grows the run deck. Empty deck retries the
  level.
- Draw: 1 per die rolled at end of turn; kills draw immediately (1 card, 2 for a
  tank). Hand cap 8; overflow discards the oldest.
- Reroll: only when no card is playable. 1 free reroll per map; paid 1 HP from
  map 2; reset at map entry and retry. On map 1, an unplayable die is skipped.
- Walk: up to the die value of movement (diagonal = 2, orthogonal = 1); player picks the end tile; auto-path prefers safe
  routes, then damaging or fast-enemy routes (HP cost), then unreachable.
- Terrain: three obstacle types — holes (jumpable), damaging (walkable at HP
  cost), walls (impassable). The player never falls into holes.
- Enemies: stationary. Attack type: melee (own tile) or ranged (adjacent, 8-way). Speed
  type: fast (attacks on entry) or slow (attacks at the start of the next turn
  if the player stays; leaving cancels). Body types: normal, tank, flying.
- Torch: real-time timer; burnout retries the level at the level-entry HP; it is
  a time penalty, not a run loss. Pauses in the menu only.
- Between maps: 1 of 3 packs + 1 of 3 bonuses; reroll each set once.
- No level-up. Progression is deck-building, bonuses, and occasional extra dice.
- Families: Elvish (2, Union), Devilish (5, Desire), Sinitic (6, Fortune and
  wealth). Rolls 1, 3, and 4 are normal; 1 has Loneliness special effects, not a
  family. Only d6s exist; start with one and add more.
- Permadeath: HP 0 ends the run. Torch burnout and deck exhaustion are retries.

---

## Team workflow notes
- Empty folders are preserved via `.gitkeep`; Unity generates `.meta` on open —
  commit them.
- Art/audio binaries are Git LFS-tracked (already configured).
- Scripts are split into `_Game.Runtime` and `_Game.Editor` assemblies.
  `_Game.Runtime` references Unity.InputSystem, UnityEngine.UI,
  Unity.TextMeshPro, and FMODUnity.
- LFS covers art, fonts, and FMOD banks; run `git lfs fetch` after cloning.
- FMOD Studio project: `FMOD/Test`; built bank:
  `FMOD/Test/Build/Desktop/Master.bank`.
- TMP essential resources: `Assets/TextMesh Pro`.
- Add `DOTween.asmdef` if tweening is needed from `_Game.Runtime`.

---

## Scope note
Map sizes are 8×8 for tutorial maps, 12×12 for beginner maps, and up to 38×38
for endgame maps. Treasure cards and the per-level pack are the deck-growth
sources, so treasure density must scale with map size. The endgame size is large
for the early action economy and the 3–6 minute torch. The first playable build
should prove the 8×8 loop before investing in large maps.
