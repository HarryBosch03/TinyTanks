using TinyTanks.Rendering;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[EditorToolbarElement(Id, typeof(SceneView))]
public class ScopeToolbarToggle : EditorToolbarToggle
{
    public const string Id = "Custom/Toggle Scope";

    public ScopeToolbarToggle()
    {
        text = "Toggle Scope";
        tooltip = "Toggles Scope Render Feature";
        value = GetFeature().isActive;
    }

    protected override void ToggleValue()
    {
        base.ToggleValue();
        GetFeature().SetActive(value);
    }
    
    public ScriptableRendererFeature GetFeature()
    {
        var allFeatures = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath("f288ae1f4751b564a96ac7587541f7a2"));
        foreach (var feature in allFeatures)
        {
            if (feature is ScriptableRendererFeature fullScreenPassRendererFeature && fullScreenPassRendererFeature.name == "ScopeRenderFeature")
            {
                return fullScreenPassRendererFeature;
            }
        }
        return null;
    }
}

[EditorToolbarElement(Id, typeof(SceneView))]
public class CdmToolbarToggle : EditorToolbarToggle
{
    public const string Id = "Custom/Toggle Cdm";

    public CdmToolbarToggle()
    {
        text = "Toggle Cdm";
        tooltip = "Toggles Cdm Render Feature";
        value = GetFeature().renderInScene;
    }

    protected override void ToggleValue()
    {
        base.ToggleValue();
        GetFeature().renderInScene = value;
    }

    public CdmRendererFeature GetFeature()
    {
        var allFeatures = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath("f288ae1f4751b564a96ac7587541f7a2"));
        foreach (var feature in allFeatures)
        {
            if (feature is CdmRendererFeature cdmRendererFeature && cdmRendererFeature.name == "CdmRendererFeature")
            {
                return cdmRendererFeature;
            }
        }
        return null;
    }
}

//[Icon("")]
[Overlay(typeof(SceneView), "Custom Tools")]
public class CustomEditorToolbar : ToolbarOverlay
{
    private CustomEditorToolbar() : base(ScopeToolbarToggle.Id, CdmToolbarToggle.Id) { }
}