# Branch Merge Review — MazenUI and SoundDesign

**Date:** 2026-11-06
**Current branch:** `main` at `dab707a` ("Machine State and Logic Intial Setup -Not Tested yet-")
**Merge base:** `2a7f449` (`origin/main`)
**Reviewed branches:** `origin/MazenUI`, `origin/SoundDesign`

The requested `/MawenUI` branch does not exist. The remote branch is
`origin/MazenUI`; this report reviews that branch. If you meant a different
branch, stop and confirm the name.

---

## Execution status (completed)

The merge sequence in this report was executed on `main`.

- `60a5897` — Merge SoundDesign.
- `868fa38` — Merge MazenUI.
- Backup branch: `pre-merge-backup`.
- LFS objects fetched; `Master.bank`, `LiberationSans.ttf`, and `Card_01.png`
  verified as real files.
- `_Game.Runtime.asmdef` now references `UnityEngine.UI`, `Unity.TextMeshPro`,
  and `FMODUnity`.
- `Docs/Architecture.md` auto-merged and kept the new assembly references.
- Unity compiled successfully (`Tundra build success`, no `error CS` in the
  editor log). The MCP connection dropped during the domain reload.
- Scenes present: `Assets/Scenes/Cards_Dice_UI.unity`,
  `Assets/Scenes/SoundDesign.unity`.
- Remaining: runtime scene verification and the UI ↔ `GameSession` bridge.

---

## 1. Branch map

```
2a7f449 origin/main
├── dab707a main                 (current: docs, core code, balancing assets)
├── 2196646 origin/MazenUI       (cards UI)
└── 68d37cb origin/SoundDesign   (audio bank)
    ├── bff4fbb Set up FMOD for version control and link Studio project
    └── 7ff2a69 Fixed Import errors for FMOD plugin
```

Both feature branches are based on `origin/main`, not on the current `main`.
They do not contain the docs, core state machine, or balancing assets added on
`main`.

---

## 2. What each branch adds

### `origin/MazenUI` — 129 files, about 64,800 insertions

- `Assets/Scenes/Cards_Dice_UI.unity` — card hand sandbox scene.
- `Assets/_Game/Prefabs/UI/CardView.prefab` — card prefab.
- `Assets/_Game/Scripts/Cards/` — `CardInfo`, `CardSelection`,
  `CardSelectionAudio`, `CardSelectionService`, `PlaceholderCardSource`.
- `Assets/_Game/Scripts/UI/` — `CardView`, `HandView`, `HandLayout`,
  `CardRevealView`.
- `Assets/TextMesh Pro/` — full TMP essential resources.
- `Assets/UI/Placeholders/` — card, dice, and background images.
- `Assets/_Game/Scripts/_Game.Runtime.asmdef` — changes references to
  `UnityEngine.UI`, `Unity.TextMeshPro`, `FMODUnity`.
- `Docs/Architecture.md` — updates the assemblies line to match.

### `origin/SoundDesign` — 99 files, about 1,400 insertions, 1,100 deletions

- `FMOD/Test/` — FMOD Studio project (`Test.fspro`), metadata, `Master.bank`,
  `Master.strings.bank`, and a test music file.
- `Assets/Scenes/SoundDesign.unity` — audio test scene.
- `Assets/Plugins/FMOD/Resources/FMODStudioSettings.asset` — links the Studio
  project (`FMOD/Test/Test.fspro`) and the bank path (`FMOD/Test/Build`),
  with `Master` as the master bank.
- `.gitattributes` — adds `*.bank` to LFS and FMOD bundle/plist text rules.
- `.gitignore` — ignores the FMOD cache, `StreamingAssets` banks, and Studio
  per-user state.
- Deletes `Assets/Plugins/FMOD/Cache/Editor/FMODStudioCache.asset` and its meta.
- Updates FMOD platform `.meta` files.

---

## 3. Structural verdict

### MazenUI: not structurally broken

The UI code is a clean, self-contained card selection and hand view:

- `CardInfo` is a view model (id + artwork), separate from gameplay data.
- `CardSelection` is plain C# and event-driven.
- `CardSelectionService` is the scene access point.
- `CardView`, `HandView`, `HandLayout`, `CardRevealView` are view-only.
- The scene wiring is correct: `HandView` has `service`, `cardPrefab`,
  `cardContainer`, and `reveal` assigned; `CardSelectionAudio` has `service`
  assigned; `PlaceholderCardSource` has its three cards and art assigned;
  `CardView.prefab` has `visual` and `artwork` assigned.

The gap is integration, not structure. The UI uses `CardInfo` and
`CardSelection`; the gameplay uses `CardInstance` and `Hand`. Do not rewrite the
UI. Add a bridge.

### SoundDesign: not structurally broken

The FMOD setup is coherent:

- The Studio project is outside `Assets/`, which is correct.
- `FMODStudioSettings.asset` points to `FMOD/Test/Test.fspro` and
  `FMOD/Test/Build`, with `Master` as the master bank.
- `.gitignore` correctly removes the FMOD cache and per-user Studio state.
- `.gitattributes` correctly adds `*.bank` to LFS.
- `Assets/Scenes/SoundDesign.unity.meta` is a valid `DefaultImporter` meta, not
  a folder meta.

The Git status shows the scene `.meta` as a rename from
`Assets/Plugins/FMOD/Cache/Editor.meta`. That is a similarity-detection
artifact. Treat it as a delete of the old cache folder meta and an add of the
new scene meta. Do not let the merge tool carry `folderAsset: yes` into the
scene meta.

---

## 4. Critical prerequisite: LFS objects are missing locally

The binary files on both branches are Git LFS pointers, and the LFS objects are
not present in this clone.

Verified examples:

- `FMOD/Test/Build/Desktop/Master.bank` — pointer,
  `oid sha256:10190feca6eb264fc63f20156ceca9b8b709e49ff48fe98a99d0681399538310`
- `Assets/UI/Placeholders/Card_01.png` — pointer,
  `oid sha256:a65f7e6d58aa7b27d9b874b263a4cd51ed2fc4a78d15bf9c56e86bd8f90a4c1a`

Both objects are absent from `.git/lfs/objects`. Without fetching them, the
merge will produce pointer text files instead of banks, fonts, and images. The
TMP font (`LiberationSans.ttf`), the UI placeholders, and the FMOD banks will
all fail to load.

Before merging:

```powershell
git lfs install
git lfs fetch origin SoundDesign
git lfs fetch origin MazenUI
```

If the remote requires authentication, authenticate first. After merging, run
`git lfs status` and open one bank and one PNG to confirm they are binary, not
pointer text.

---

## 5. Merge conflicts

### SoundDesign: no conflicts

`git merge-tree` found no conflicting paths between `main` and
`origin/SoundDesign`. The merge applies cleanly.

### MazenUI: one conflict

The only conflict is `Docs/Architecture.md`, at the assemblies line:

```text
Assemblies: `Scripts/_Game.Runtime.asmdef` (references Unity.InputSystem,
Unity.ugui) ...
```

Resolution: keep the current architecture document and change the reference list
to the branch's list:

```text
Assemblies: `Scripts/_Game.Runtime.asmdef` (references Unity.InputSystem,
UnityEngine.UI, Unity.TextMeshPro, FMODUnity) ...
```

All other paths are additive and conflict-free:

- `Assets/_Game/Scripts/Cards/` — new file names; no collision with
  `CardData`, `CardInstance`, `Deck`, `Hand`, `SpentPile`.
- `Assets/_Game/Scripts/UI/` — new folder; no collision.
- `Assets/TextMesh Pro/`, `Assets/UI/`, `Assets/_Game/Prefabs/UI/`,
  `Assets/Scenes/Cards_Dice_UI.unity` — additive.
- `Assets/_Game/Scripts/_Game.Runtime.asmdef` — `main` did not change it, so the
  branch version applies without a conflict. It replaces `Unity.ugui` with
  `UnityEngine.UI` and adds `Unity.TextMeshPro` and `FMODUnity`.

---

## 6. Recommended merge order

Merge `origin/SoundDesign` first, then `origin/MazenUI`. The UI branch adds a
`FMODUnity` reference to `_Game.Runtime` and a `CardSelectionAudio` component.
The audio branch establishes the FMOD project and bank settings that make that
reference meaningful. Merging audio first gives a cleaner intermediate state.

### Commands

```powershell
git switch main
git status
git branch pre-merge-backup

git lfs install
git lfs fetch origin SoundDesign
git lfs fetch origin MazenUI

git merge --no-ff origin/SoundDesign -m "Merge SoundDesign"
# Verify the FMOD settings and bank path. Treat the scene meta as add, and the
# cache Editor meta as delete. Do not keep folderAsset: yes on the scene meta.

git merge --no-ff origin/MazenUI -m "Merge MazenUI"
# Resolve Docs/Architecture.md: keep current content, update the assemblies line.
```

Do not squash or rebase these branches. A merge with `--no-ff` preserves their
work and makes the integration history visible.

---

## 7. Post-merge verification

1. **LFS:** run `git lfs status`; confirm the bank, font, and placeholder images
   are not pointer files.
2. **FMOD:** open `Assets/Scenes/SoundDesign.unity`; confirm the `Master` bank
   loads and no FMOD event is missing. Check `FMODStudioSettings.asset` still
   points to `FMOD/Test/Test.fspro` and `FMOD/Test/Build`.
3. **UI:** open `Assets/Scenes/Cards_Dice_UI.unity`; confirm cards deal in,
   hover, click, and reveal. Confirm `HandView.service`, `cardPrefab`,
   `cardContainer`, and `reveal` are assigned.
4. **TMP:** confirm the TMP essentials imported without duplicate
   `TMP Settings.asset` files. If Unity already has a TMP copy, keep one.
5. **Compile:** open Unity and confirm `_Game.Runtime` builds with
   `UnityEngine.UI`, `Unity.TextMeshPro`, and `FMODUnity`. The FMOD assembly
   lists `Unity.Timeline`, `Unity.Addressables`, and `Unity.ResourceManager` in
   its asmdef even though the project manifest has no Addressables or Timeline
   package. The current project compiles because those code paths are guarded by
   version defines. If the merged build reports missing references, either add
   the packages or remove the optional references from `FMODUnity.asmdef`.
6. **Regression:** confirm the existing `GameSession`, grid, deck, and balancing
   assets still compile and load. The branches do not touch them.

---

## 8. Integration plan: preserve the UI, add a bridge

The UI branch is a selection-and-hand prototype. The gameplay state machine is
`GameSession`. Do not replace one with the other. Add one adapter.

Suggested bridge:

```text
HandPresenter (new MonoBehaviour)
  - Holds a reference to GameSession and CardSelectionService.
  - Subscribes to GameSession.PhaseChanged, CardDrawn, CardPlayed, CardDiscarded.
  - On each change, maps GameSession.PlayerHand to CardInfo:
        id      = CardInstance.Id.ToString()
        artwork = CardInstance.Data.art
  - Calls CardSelectionService.Selection.SetOffer(mappedCards).
  - When GameSession.Phase is AwaitingCard, offer only GameSession.GetPlayableCards().
  - On CardSelection.CardSelected:
        store the selected CardInstance id.
        enable a targeting step that asks GameSession.GetWalkPaths / CanPlay.
        on tile drop, call GameSession.PlayCard(instance, target).
```

Do not change `CardInfo`, `CardSelection`, `HandView`, or `HandLayout` for this.
The bridge converts between their view model and the gameplay model. The
branch's `PlaceholderCardSource` stays for the UI sandbox scene; disable it in
the gameplay scene and let the bridge feed the hand.

The branch's `CardSelection` is click-to-select. The gameplay needs a tile
target. Add a small `CardTargetingController` that reacts to
`CardSelection.CardSelected`, highlights valid tiles from the `GameSession`
API, and calls `PlayCard` on drop. This is new glue, not a rewrite.

`CardSelectionAudio` is already wired and stays silent while its
`cardSelectedSound` is unassigned. Once the SoundDesign bank has a card-select
event, assign it in the Inspector.

---

## 9. Structural changes only if needed

These are the only cases where a change is justified. None of them require
rewriting the branches' work.

1. **If the core runtime must not depend on audio:** move
   `CardSelectionAudio.cs` into a separate `_Game.Audio` assembly that references
   `FMODUnity`, and keep `_Game.Runtime` free of FMOD. This is a small
   decoupling, not a redesign. It preserves the UI.
2. **If TMP resources duplicate an existing copy:** keep one
   `TMP Settings.asset` and one `Assets/TextMesh Pro` tree. Delete the duplicate
   folder, not the UI scripts.
3. **If `FMODUnity.asmdef` fails to resolve `Unity.Addressables` or
   `Unity.Timeline`:** add the missing packages or remove those optional
   references. The FMOD code is guarded by version defines, so removing the
   references is safe for this project.
4. **If the scene meta arrives as a rename with `folderAsset: yes`:** replace it
   with the branch's `DefaultImporter` meta. The scene must not be treated as a
   folder.

---

## 10. Risks

- **Missing LFS objects** is the top risk. The UI images and FMOD banks will be
  pointer text without `git lfs fetch`.
- **The UI is not yet wired to gameplay.** It is a standalone selection demo.
  The bridge is required before it can play cards from `GameSession`.
- **The FMOD bank is a build artifact.** If the Studio project changes, rebuild
  the banks and commit the new LFS objects.
- **The `_Game.Runtime` to `FMODUnity` reference couples core code to audio.**
  It is acceptable for the jam, but a separate audio assembly is the cleaner
  long-term structure.
- **The branch name mismatch.** Confirm `origin/MazenUI` is the intended branch.

---

## 11. Bottom line

Preserve both branches. Merge SoundDesign first, then MazenUI. Fetch LFS before
either merge. Resolve only one real conflict (`Docs/Architecture.md`). Keep the
MazenUI UI code as-is and add a bridge to `GameSession`. The SoundDesign setup
is structurally sound; verify the bank loads and the FMOD optional references
after the merge.
