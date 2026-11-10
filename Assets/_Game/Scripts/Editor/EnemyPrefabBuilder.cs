using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiceDrive.Characters;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace DiceDrive.EditorTools
{
    /// <summary>
    /// Builds the four "Monsters Creatures Fantasy" enemies the same way the Warrior is set up:
    ///
    ///   Enemy (SortingGroup on "Characters", CharacterActor, CharacterSorting)
    ///   └─ Visual (SpriteRenderer, Animator, CharacterAnimator)
    ///
    /// Each enemy gets its own folder of clips named after <see cref="CharacterAnim"/> (CharacterAnimator plays
    /// states by name and CharacterActor reads clip lengths by name) plus an AnimatorController laid out like
    /// Warrior.controller. Missing animations in the pack (Jump/Fall, Skeleton Run, Flying eye Idle/Run) are
    /// mapped onto the closest available frames so every state exists.
    ///
    /// Safe to re-run: existing clips, controllers and prefabs are updated in place, keeping their GUIDs.
    /// </summary>
    public static class EnemyPrefabBuilder
    {
        private const string SpriteRoot = "Assets/Monsters Creatures Fantasy/Sprites";
        private const string ArtRoot = "Assets/_Game/Art/Characters";
        private const string PrefabRoot = "Assets/_Game/Prefabs/Characters/Enemies";
        private const string WarriorPrefab = "Assets/_Game/Prefabs/Characters/Warrior.prefab";

        private const float FrameRate = 12f;           // Same as the Warrior clips.
        private const float VisualScale = 2.5f;        // Same as the Warrior: identical on-screen pixel size.
        private const float PixelsPerUnit = 100f;
        private const int FrameHeight = 150;
        private const int FootRow = 49;                // Lowest opaque pixel row of every ground pose in the pack.

        private sealed class ClipSpec
        {
            public string Sheet;
            public int[] Frames;                       // null = all frames of the sheet
            public bool Loop;
            public int ImpactFrame = -1;               // Attack clips: frame the blow lands (OnAttackImpact).

            public ClipSpec(string sheet, bool loop, int[] frames = null, int impactFrame = -1)
            {
                Sheet = sheet;
                Loop = loop;
                Frames = frames;
                ImpactFrame = impactFrame;
            }
        }

        private sealed class EnemySpec
        {
            public string Name;
            public string SourceFolder;
            public readonly Dictionary<string, ClipSpec> Clips = new Dictionary<string, ClipSpec>();
            /// <summary>Extra non-enum states that return to Idle when finished (e.g. Skeleton Shield).</summary>
            public readonly List<string> ExtraStates = new List<string>();
        }

        private static readonly int[] FirstHalf = { 0, 1, 2, 3 };

        private static IEnumerable<EnemySpec> Specs()
        {
            // Impact frames were measured from the sprite sheets: the first frame where the weapon / body
            // reaches clearly past the idle silhouette.
            var goblin = new EnemySpec { Name = "Goblin", SourceFolder = "Goblin" };
            goblin.Clips["Idle"] = new ClipSpec("Idle", true);
            goblin.Clips["Run"] = new ClipSpec("Run", true);
            goblin.Clips["Jump"] = new ClipSpec("Run", false, FirstHalf);
            goblin.Clips["Fall"] = new ClipSpec("Run", true, new[] { 4 });          // feet off the ground
            goblin.Clips["Attack1"] = new ClipSpec("Attack1", false, null, 6);
            goblin.Clips["Attack2"] = new ClipSpec("Attack2", false, null, 6);
            goblin.Clips["Hit"] = new ClipSpec("Take Hit", false);
            goblin.Clips["Death"] = new ClipSpec("Death", false);
            yield return goblin;

            var mushroom = new EnemySpec { Name = "Mushroom", SourceFolder = "Mushroom" };
            mushroom.Clips["Idle"] = new ClipSpec("Idle", true);
            mushroom.Clips["Run"] = new ClipSpec("Run", true);
            mushroom.Clips["Jump"] = new ClipSpec("Run", false, FirstHalf);
            mushroom.Clips["Fall"] = new ClipSpec("Run", true, new[] { 3 });
            mushroom.Clips["Attack1"] = new ClipSpec("Attack1", false, null, 6);
            mushroom.Clips["Attack2"] = new ClipSpec("Attack2", false, null, 6);
            mushroom.Clips["Hit"] = new ClipSpec("Take Hit", false);
            mushroom.Clips["Death"] = new ClipSpec("Death", false);
            yield return mushroom;

            var skeleton = new EnemySpec { Name = "Skeleton", SourceFolder = "Skeleton" };
            skeleton.Clips["Idle"] = new ClipSpec("Idle", true);
            skeleton.Clips["Run"] = new ClipSpec("Walk", true);                     // pack has Walk, no Run
            skeleton.Clips["Jump"] = new ClipSpec("Walk", false);
            skeleton.Clips["Fall"] = new ClipSpec("Walk", true, new[] { 0 });
            skeleton.Clips["Attack1"] = new ClipSpec("Attack1", false, null, 6);
            skeleton.Clips["Attack2"] = new ClipSpec("Attack2", false, null, 6);
            skeleton.Clips["Hit"] = new ClipSpec("Take Hit", false);
            skeleton.Clips["Death"] = new ClipSpec("Death", false);
            skeleton.Clips["Shield"] = new ClipSpec("Shield", false);               // extra: block pose
            skeleton.ExtraStates.Add("Shield");
            yield return skeleton;

            var eye = new EnemySpec { Name = "FlyingEye", SourceFolder = "Flying eye" };
            eye.Clips["Idle"] = new ClipSpec("Flight", true);                       // always airborne
            eye.Clips["Run"] = new ClipSpec("Flight", true);
            eye.Clips["Jump"] = new ClipSpec("Flight", false);
            eye.Clips["Fall"] = new ClipSpec("Flight", true);
            eye.Clips["Attack1"] = new ClipSpec("Attack1", false, null, 6);         // bite lunge
            eye.Clips["Attack2"] = new ClipSpec("Attack2", false, null, 5);
            eye.Clips["Hit"] = new ClipSpec("Take Hit", false);
            eye.Clips["Death"] = new ClipSpec("Death", false);                      // drops to the floor
            yield return eye;
        }

        [MenuItem("Tools/DiceDrive/Build Enemy Prefabs")]
        public static void BuildAll()
        {
            var warrior = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPrefab);
            var warriorRenderer = warrior != null ? warrior.GetComponentInChildren<SpriteRenderer>(true) : null;
            var material = warriorRenderer != null ? warriorRenderer.sharedMaterial : null;
            var warriorActor = warrior != null ? warrior.GetComponent<CharacterActor>() : null;

            EnsureFolder(PrefabRoot);

            var built = new List<string>();
            foreach (var spec in Specs())
            {
                var path = BuildEnemy(spec, material, warriorActor);
                if (path != null)
                {
                    built.Add(path);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyPrefabBuilder] Built:\n" + string.Join("\n", built));
        }

        private static string BuildEnemy(EnemySpec spec, Material material, CharacterActor warriorActor)
        {
            var artFolder = $"{ArtRoot}/{spec.Name}";
            EnsureFolder(artFolder);

            var clips = new Dictionary<string, AnimationClip>();
            foreach (var pair in spec.Clips)
            {
                var sprites = LoadFrames($"{SpriteRoot}/{spec.SourceFolder}/{pair.Value.Sheet}.png");
                if (sprites.Count == 0)
                {
                    Debug.LogError($"[EnemyPrefabBuilder] {spec.Name}: no sprites in sheet '{pair.Value.Sheet}'.");
                    return null;
                }

                var frames = pair.Value.Frames == null
                    ? sprites
                    : pair.Value.Frames.Where(i => i >= 0 && i < sprites.Count).Select(i => sprites[i]).ToList();
                clips[pair.Key] = WriteClip($"{artFolder}/{pair.Key}.anim", pair.Key, frames, pair.Value);
            }

            var controller = WriteController($"{artFolder}/{spec.Name}.controller", clips, spec.ExtraStates);
            var idleSprite = LoadFrames($"{SpriteRoot}/{spec.SourceFolder}/{spec.Clips["Idle"].Sheet}.png")[0];
            return WritePrefab(spec, controller, idleSprite, material, warriorActor);
        }

        // ------------------------------------------------------------------ clips

        private static List<Sprite> LoadFrames(string sheetPath)
        {
            return AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.rect.y > 0 ? -s.rect.y : 0) // single-row sheets: rows top-to-bottom
                .ThenBy(s => s.rect.x)
                .ToList();
        }

        private static AnimationClip WriteClip(string path, string clipName, List<Sprite> frames, ClipSpec spec)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            var isNew = clip == null;
            if (isNew)
            {
                clip = new AnimationClip();
            }
            else
            {
                clip.ClearCurves();
            }

            clip.name = clipName;
            clip.frameRate = FrameRate;

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            // One key per frame, like the Warrior clips: Unity gives the last sprite key a full frame,
            // so the clip length is frames / fps.
            var keys = new ObjectReferenceKeyframe[frames.Count];
            for (var i = 0; i < frames.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe { time = i / FrameRate, value = frames[i] };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = spec.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var events = new List<AnimationEvent>();
            if (spec.ImpactFrame >= 0)
            {
                var frame = Mathf.Min(spec.ImpactFrame, frames.Count - 1);
                events.Add(new AnimationEvent { time = frame / FrameRate, functionName = "OnAttackImpact" });
            }

            AnimationUtility.SetAnimationEvents(clip, events.ToArray());

            if (isNew)
            {
                AssetDatabase.CreateAsset(clip, path);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }

            return clip;
        }

        // ------------------------------------------------------------------ controller

        private static AnimatorController WriteController(
            string path, Dictionary<string, AnimationClip> clips, List<string> extraStates)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(path);

            var machine = controller.layers[0].stateMachine;
            foreach (var child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            foreach (var t in machine.anyStateTransitions.ToArray())
            {
                machine.RemoveAnyStateTransition(t);
            }

            var states = new Dictionary<string, AnimatorState>();
            var order = System.Enum.GetNames(typeof(CharacterAnim)).Concat(extraStates).ToList();
            for (var i = 0; i < order.Count; i++)
            {
                var name = order[i];
                if (!clips.TryGetValue(name, out var clip))
                {
                    continue;
                }

                var state = machine.AddState(name, new Vector3(300f + (i % 4) * 220f, 60f * (i / 4) * 2f + 60f));
                state.motion = clip;
                state.writeDefaultValues = true;
                states[name] = state;
            }

            machine.defaultState = states["Idle"];

            // Same transitions as Warrior.controller: one-shots return to Idle, Jump flows into Fall.
            AddExit(states, "Jump", "Fall");
            AddExit(states, "Attack1", "Idle");
            AddExit(states, "Attack2", "Idle");
            AddExit(states, "Hit", "Idle");
            foreach (var extra in extraStates)
            {
                AddExit(states, extra, "Idle");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddExit(Dictionary<string, AnimatorState> states, string from, string to)
        {
            if (!states.TryGetValue(from, out var a) || !states.TryGetValue(to, out var b))
            {
                return;
            }

            var t = a.AddTransition(b);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.duration = 0f;
            t.hasFixedDuration = true;
        }

        // ------------------------------------------------------------------ prefab

        private static string WritePrefab(
            EnemySpec spec, AnimatorController controller, Sprite idleSprite, Material material,
            CharacterActor warriorActor)
        {
            var path = $"{PrefabRoot}/{spec.Name}.prefab";

            var root = new GameObject(spec.Name);
            try
            {
                var group = root.AddComponent<SortingGroup>();
                var layerId = SortingLayer.NameToID(CharacterSorting.DefaultLayerName);
                if (SortingLayer.IsValid(layerId))
                {
                    group.sortingLayerID = layerId;
                }

                var visual = new GameObject("Visual");
                visual.transform.SetParent(root.transform, false);
                // Feet (pixel row FootRow of a centre-pivoted frame) land exactly on the root, like the Warrior.
                var footOffset = (FrameHeight * 0.5f - FootRow) / PixelsPerUnit * VisualScale;
                visual.transform.localPosition = new Vector3(0f, footOffset, 0f);
                visual.transform.localScale = Vector3.one * VisualScale;

                var renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = idleSprite;
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                // Art faces right; enemies start facing left, toward the player entering from the left.
                renderer.flipX = true;

                var animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;

                var characterAnimator = visual.AddComponent<CharacterAnimator>();
                var animSo = new SerializedObject(characterAnimator);
                animSo.FindProperty("artFacesRight").boolValue = true;
                animSo.ApplyModifiedPropertiesWithoutUndo();

                var actor = root.AddComponent<CharacterActor>();
                if (warriorActor != null)
                {
                    EditorUtility.CopySerialized(warriorActor, actor);  // same movement/combat tuning
                }

                var actorSo = new SerializedObject(actor);
                actorSo.FindProperty("visual").objectReferenceValue = characterAnimator;
                actorSo.ApplyModifiedPropertiesWithoutUndo();

                root.AddComponent<CharacterSorting>();

                PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
                return success ? path : null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
