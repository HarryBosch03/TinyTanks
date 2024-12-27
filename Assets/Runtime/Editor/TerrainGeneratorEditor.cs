using TinyTanks.TerrainGeneration;
using UnityEditor;
using UnityEngine;

namespace Runtime.Editor
{
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var target = this.target as TerrainGenerator;

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Generate"))
                {
                    target.Generate();
                }
            }
        }
    }
}