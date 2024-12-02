using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

[EditorToolbarElement(Id, typeof(SceneView))]
public class CustomToolbarOverlay : EditorToolbarToggle
{
    public const string Id = "Custom/Toggle Scope";

    public CustomToolbarOverlay()
    {
        text = "Toggle Scope";
        tooltip = "Toggles Scope Render Feature";
    }

    protected override void ToggleValue()
    {
        base.ToggleValue();
        var scopeRenderFeature = AssetDatabase.LoadAssetAtPath<FullScreenPassRendererFeature>(AssetDatabase.GUIDToAssetPath("f288ae1f4751b564a96ac7587541f7a2"));
        scopeRenderFeature.SetActive(value);
    }

    [MenuItem("Assets/Copy GUID")]
    public static void CopyGuid()
    {
        var target = Selection.activeObject;
        var guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(target));
        EditorGUIUtility.systemCopyBuffer = guid.ToString();
        Debug.Log($"GUID Copied to keyboard");
    }
}

//[Icon("")]
[Overlay(typeof(SceneView), "Custom Tools")]
public class CustomEditorToolbar : ToolbarOverlay
{
    private CustomEditorToolbar() : base(CustomToolbarOverlay.Id)
    {
        
    }
}
