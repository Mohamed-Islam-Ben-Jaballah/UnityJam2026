using System.Collections.Generic;
using DiceDrive.Cards;
using DiceDrive.Core;
using DiceDrive.DiceSystem;
using UnityEditor;
using UnityEngine;

namespace DiceDrive.EditorTools
{
    [CustomEditor(typeof(RunConfig))]
    public sealed class RunConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (RunConfig)target;
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("QA Presets", EditorStyles.boldLabel);

            if (GUILayout.Button("Fill Starting Deck (14 cards)"))
            {
                Undo.RecordObject(config, "Fill Starting Deck");
                config.startingDeck = BuildStarterDeck();
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Fill Starting Dice (d4)"))
            {
                Undo.RecordObject(config, "Fill Starting Dice");
                config.startingDice = new[] { LoadDice(4) };
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Add d6"))
            {
                Undo.RecordObject(config, "Add d6");
                config.startingDice = AppendDice(config.startingDice, LoadDice(6));
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Add d8"))
            {
                Undo.RecordObject(config, "Add d8");
                config.startingDice = AppendDice(config.startingDice, LoadDice(8));
                EditorUtility.SetDirty(config);
            }

            if (GUILayout.Button("Reset Tuning"))
            {
                Undo.RecordObject(config, "Reset Tuning");
                config.maxHp = 6;
                config.handSize = 5;
                config.handCap = 8;
                config.torchSeconds = 240f;
                EditorUtility.SetDirty(config);
            }
        }

        private static CardData[] BuildStarterDeck()
        {
            var cards = new List<CardData>();
            Add(cards, LoadCard("Walk"), 8);
            Add(cards, LoadCard("Leapol"), 2);
            Add(cards, LoadCard("Squimi"), 2);
            Add(cards, LoadCard("Heilin"), 2);
            return cards.ToArray();
        }

        private static void Add(List<CardData> cards, CardData card, int count)
        {
            if (card == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                cards.Add(card);
            }
        }

        private static CardData LoadCard(string name)
        {
            var path = "Assets/_Game/Data/Cards/" + name + ".asset";
            var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card == null)
            {
                Debug.LogWarning("Missing card asset: " + path);
            }

            return card;
        }

        private static DiceData[] AppendDice(DiceData[] dice, DiceData die)
        {
            var list = dice != null ? new List<DiceData>(dice) : new List<DiceData>();
            if (die != null && !list.Contains(die))
            {
                list.Add(die);
            }

            return list.ToArray();
        }

        private static DiceData LoadDice(int sides)
        {
            const string folder = "Assets/_Game/Data/Dice";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Data", "Dice");
            }

            var path = folder + "/d" + sides + ".asset";
            var die = AssetDatabase.LoadAssetAtPath<DiceData>(path);
            if (die == null)
            {
                die = ScriptableObject.CreateInstance<DiceData>();
                die.sides = sides;
                AssetDatabase.CreateAsset(die, path);
            }

            return die;
        }
    }
}
