using System;
using System.Linq;
using UnityEditor;
using UnityEditor.VFX;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.VFX;

namespace DiceDrive.EditorTools.Vfx
{
    /// <summary>
    /// Generates Ambient.vfx: one data-driven VFX Graph whose look is entirely set through exposed
    /// properties, so every board/candle effect (embers, hellfire, haze, mist, soul motes, candle halo)
    /// is just a VisualEffect component with different values. No physics, no collisions.
    ///
    /// VFX Graph has no public authoring API, so this assembly is named after one of the assemblies the
    /// VFX editor grants internal access to (see the asmdef). It is editor-only and never ships.
    /// Re-run from Tools > DiceDrive > Build Ambient VFX Graph after changing the layout below.
    /// </summary>
    public static class AmbientVfxGraphBuilder
    {
        public const string AssetPath = "Assets/_Game/Art/Board/VFX/Ambient.vfx";

        [MenuItem("Tools/DiceDrive/Build Ambient VFX Graph")]
        public static void Build()
        {
            // Overwrites the file in place (no delete), so the asset keeps its GUID and prefab references survive.
            VisualEffectAssetEditorUtility.CreateNewAsset(AssetPath);
            var resource = VisualEffectResource.GetResourceAtPath(AssetPath);
            var graph = resource.GetGraph() ?? resource.CreateGraph();

            // ---- Contexts -------------------------------------------------------------------
            var spawner = Add<VFXBasicSpawner>(graph);
            var init = Add<VFXBasicInitialize>(graph);
            var update = Add<VFXBasicUpdate>(graph);
            var output = Add<VFXPlanarPrimitiveOutput>(graph);

            spawner.LinkTo(init);
            init.LinkTo(update);
            update.LinkTo(output);

            var data = init.GetData();
            data.SetSettingValue("capacity", 512u);
            data.SetSettingValue("boundsMode", BoundsSettingMode.Manual);
            In(init, "bounds").value = new AABox { center = new Vector3(0f, 1.5f, 0f), size = new Vector3(12f, 8f, 2f) };

            output.SetSettingValue("blendMode", VFXAbstractRenderedOutput.BlendMode.Additive);

            // ---- Spawn: constant rate + optional periodic burst ------------------------------
            var rate = AddBlock<VFXSpawnerConstantRate>(spawner);
            Expose(graph, "SpawnRate", 5f, In(rate, "Rate"));

            var burst = AddBlock<VFXSpawnerBurst>(spawner);
            burst.SetSettingValue("repeat", VFXSpawnerBurst.RepeatMode.Periodic);
            Expose(graph, "BurstCount", 0f, In(burst, "Count"));
            Expose(graph, "BurstPeriod", 2f, In(burst, "Delay"));

            // ---- Initialize: random position / lifetime / size / velocity / rotation ---------
            RandomAttribute(graph, init, "position", "PositionMin", new Vector3(-0.5f, -0.5f, 0f), "PositionMax", new Vector3(0.5f, 0.5f, 0f));
            RandomAttribute(graph, init, "lifetime", "LifetimeMin", 1f, "LifetimeMax", 2f);
            RandomAttribute(graph, init, "size", "SizeMin", 0.1f, "SizeMax", 0.2f);
            RandomAttribute(graph, init, "velocity", "VelocityMin", new Vector3(-0.1f, 0.3f, 0f), "VelocityMax", new Vector3(0.1f, 0.8f, 0f));

            var angle = AddBlock<SetAttribute>(init);
            angle.SetSettingValue("attribute", "angle");
            angle.SetSettingValue("channels", VariadicChannelOptions.Z);
            angle.SetSettingValue("Random", RandomMode.Uniform);
            In(angle, "A").value = 0f;
            Expose(graph, "RotationRange", 0f, In(angle, "B"));

            // Slow spin (integrated by the Update context) so vapor wisps curl as they rise.
            var spin = AddBlock<SetAttribute>(init);
            spin.SetSettingValue("attribute", "angularVelocity");
            spin.SetSettingValue("channels", VariadicChannelOptions.Z);
            spin.SetSettingValue("Random", RandomMode.Uniform);
            Expose(graph, "SpinMin", 0f, In(spin, "A"));
            Expose(graph, "SpinMax", 0f, In(spin, "B"));

            // ---- Update: turbulence for the drifting / flickering motion ---------------------
            var turbulence = AddBlock<Turbulence>(update);
            turbulence.SetSettingValue("Mode", UnityEditor.VFX.Block.ForceMode.Absolute); // adds a force; Relative would cancel the authored velocity
            Expose(graph, "TurbulenceIntensity", 0.5f, In(turbulence, "Intensity"));
            Expose(graph, "TurbulenceFrequency", 1f, In(turbulence, "frequency"));

            // ---- Output: color/alpha + size over life, tint, texture -------------------------
            var colorOverLife = AddBlock<AttributeFromCurve>(output);
            colorOverLife.SetSettingValue("attribute", "color");
            colorOverLife.SetSettingValue("SampleMode", AttributeFromCurve.CurveSampleMode.OverLife);
            colorOverLife.SetSettingValue("ColorMode", AttributeFromCurve.ColorApplicationMode.ColorAndAlpha);
            var defaultGradient = new Gradient();
            defaultGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            Expose(graph, "ColorOverLife", defaultGradient, colorOverLife.inputSlots[0]);

            var sizeOverLife = AddBlock<AttributeFromCurve>(output);
            sizeOverLife.SetSettingValue("attribute", "size");
            sizeOverLife.SetSettingValue("SampleMode", AttributeFromCurve.CurveSampleMode.OverLife);
            sizeOverLife.SetSettingValue("Composition", AttributeCompositionMode.Multiply);
            Expose(graph, "SizeOverLife", AnimationCurve.Linear(0f, 1f, 1f, 1f), sizeOverLife.inputSlots[0]);

            var tint = AddBlock<SetAttribute>(output);
            tint.SetSettingValue("attribute", "color");
            tint.SetSettingValue("Composition", AttributeCompositionMode.Multiply);
            Expose(graph, "Tint", Vector3.one, tint.inputSlots[0]);

            var alpha = AddBlock<SetAttribute>(output);
            alpha.SetSettingValue("attribute", "alpha");
            alpha.SetSettingValue("Composition", AttributeCompositionMode.Multiply);
            Expose(graph, "Alpha", 1f, alpha.inputSlots[0]);

            Expose(graph, "MainTexture", (Texture2D)null, In(output, "mainTexture"));

            // Stamp the current graph version (otherwise every import logs an "out of date" upgrade warning).
            graph.SanitizeGraph();
            resource.WriteAssetWithSubAssets();
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Built {AssetPath}");
        }

        private static T Add<T>(VFXGraph graph) where T : VFXModel
        {
            var model = ScriptableObject.CreateInstance<T>();
            graph.AddChild(model);
            return model;
        }

        private static T AddBlock<T>(VFXContext context) where T : VFXBlock
        {
            var block = ScriptableObject.CreateInstance<T>();
            context.AddChild(block);
            return block;
        }

        private static void RandomAttribute<TValue>(VFXGraph graph, VFXContext context, string attribute,
            string minName, TValue min, string maxName, TValue max)
        {
            var block = AddBlock<SetAttribute>(context);
            block.SetSettingValue("attribute", attribute);
            // PerComponent: independent random per axis. Uniform would use one random for x/y/z, putting every
            // particle on the diagonal between min and max.
            block.SetSettingValue("Random", typeof(TValue) == typeof(Vector3) ? RandomMode.PerComponent : RandomMode.Uniform);
            Expose(graph, minName, min, In(block, "A"));
            Expose(graph, maxName, max, In(block, "B"));
        }

        private static void Expose<TValue>(VFXGraph graph, string name, TValue defaultValue, VFXSlot target)
        {
            var parameter = ScriptableObject.CreateInstance<VFXParameter>();
            parameter.Init(typeof(TValue));
            parameter.SetSettingValue("m_Exposed", true);
            parameter.SetSettingValue("m_ExposedName", name);
            graph.AddChild(parameter);
            parameter.outputSlots[0].value = defaultValue;

            if (!parameter.outputSlots[0].Link(target))
            {
                throw new InvalidOperationException($"Could not link exposed '{name}' ({typeof(TValue).Name}) to slot '{target.property.name}' ({target.property.type.Name}).");
            }
        }

        private static VFXSlot In(VFXContext model, string name) => Find(model.inputSlots, name, model);

        private static VFXSlot In(VFXBlock model, string name) => Find(model.inputSlots, name, model);

        private static VFXSlot Find(System.Collections.Generic.IEnumerable<VFXSlot> slots, string name, VFXModel owner)
        {
            var slot = slots.FirstOrDefault(s => string.Equals(s.property.name, name, StringComparison.OrdinalIgnoreCase));
            if (slot == null)
            {
                throw new InvalidOperationException(
                    $"{owner.GetType().Name} has no input slot '{name}'. Slots: {string.Join(", ", slots.Select(s => s.property.name))}");
            }

            return slot;
        }
    }
}
