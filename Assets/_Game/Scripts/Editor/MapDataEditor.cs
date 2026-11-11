using DiceDrive.Grid;
using UnityEditor;
using UnityEngine;

namespace DiceDrive.EditorTools
{
    [CustomEditor(typeof(MapData))]
    public sealed class MapDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var map = (MapData)target;
            EditorGUILayout.HelpBox(
                "Use the Map Builder window for terrain, markers, exits, and rewards.",
                MessageType.Info);

            if (GUILayout.Button("Open Map Builder"))
            {
                MapBuilderWindow.Open(map);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
