using System;
using System.Collections.Generic;
using TinyTanks.Tanks;
using UnityEditor;
using UnityEngine;

namespace Runtime.Editor.Windows
{
    public class NumberKruncher : EditorWindow
    {
        [MenuItem("Tools/Number Kruncher")]
        public static void OpenWindow() => CreateWindow<NumberKruncher>();

        public List<TankController> tanks = new List<TankController>();
        
        private void OnEnable()
        {
            var data = EditorPrefs.GetString("NumberKruncher.data");
            var tokens = data.Split(',');
            
            tanks.Clear();
            foreach (var token in tokens)
            {
                if (string.IsNullOrEmpty(token))
                    continue;
                else if (token == "<null>")
                    tanks.Add(null);
                else
                    tanks.Add(AssetDatabase.LoadAssetAtPath<TankController>(AssetDatabase.GUIDToAssetPath(token)));
            }
        }

        private void OnDisable()
        {
            var data = "";
            foreach (var tank in tanks)
            {
                if (tank != null)
                    data += $"{AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(tank))},";
                else
                    data += "<null>,";
            }
            EditorPrefs.SetString("NumberKruncher.data", data);
        }

        private void OnGUI()
        {
            using var serializeObject = new SerializedObject(this);

            var propertyIterator = serializeObject.GetIterator();
            propertyIterator.NextVisible(true);
            while (propertyIterator.NextVisible(false))
            {
                EditorGUILayout.PropertyField(propertyIterator);
            }
            
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

            var cellSize = new Vector2(40f, 20f);
            var centerSize = cellSize * tanks.Count;
            var centerArea = new Rect(rect.center - centerSize / 2f, centerSize);

            var xLabelArea = new Rect(centerArea.x, rect.y, centerArea.width, centerArea.y - rect.y);
            var yLabelArea = new Rect(rect.x, centerArea.y, centerArea.x - rect.x, centerArea.height);
            
            GUI.DrawTexture(xLabelArea, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(1f, 0f, 0f, 0.2f), Vector4.zero, Vector4.zero);
            GUI.DrawTexture(yLabelArea, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0f, 1f, 0f, 0.2f), Vector4.zero, Vector4.zero);
            GUI.DrawTexture(centerArea, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 1f, 0.2f), Vector4.zero, Vector4.zero);

            for (var x = 0; x < tanks.Count; x++)
            {
                var tank = tanks[x];
                GUI.Label(new Rect(yLabelArea.x, yLabelArea.y + cellSize.y * x, yLabelArea.width, cellSize.y), tank != null ? tank.name : "</>");

                var matrix = GUI.matrix;
                GUI.matrix *= Matrix4x4.TRS(new Vector2(xLabelArea.x + cellSize.x * x, xLabelArea.y), Quaternion.Euler(0f, 0f, 90f), Vector3.one);
                GUI.DrawTexture(new Rect(0f, 0f, xLabelArea.height, cellSize.x), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, Color.HSVToRGB((float)x / tanks.Count, 1f, 1f), Vector4.zero, Vector4.zero);
                GUI.Label(new Rect(0f, 0f, xLabelArea.height, cellSize.x), tank != null ? tank.name : "</>");
                GUI.matrix = matrix;
                break;
            }
            
            for (var x = 0; x < tanks.Count; x++)
            for (var y = 0; y < tanks.Count; y++)
            {
                var attacker = tanks[x];
                var defender = tanks[y];
                
                
            }

            serializeObject.ApplyModifiedProperties();
            Repaint();
        }
    }
}