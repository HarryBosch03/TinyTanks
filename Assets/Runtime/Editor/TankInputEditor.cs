using TinyTanks.Tanks;
using UnityEditor;
using UnityEngine;

namespace Runtime.Editor
{
    [CustomEditor(typeof(TankInput))]
    public class TankInputEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var target = this.target as TankInput;
            if (!Application.isPlaying)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Button("Enter playmode to request Ownership");
                }   
            }
            else if (target.enabled && target.IsOwner)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Button("Already Owner");
                }
            }
            else
            {
                if (GUILayout.Button("Request Ownership"))
                {
                    target.TakeOver();
                }
            }
        }
    }
}