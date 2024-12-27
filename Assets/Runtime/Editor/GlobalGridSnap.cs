using System.Numerics;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using Vector3 = UnityEngine.Vector3;

//[Icon("")]
[Overlay(typeof(SceneView), "Global Grid Snap")]
public class GlobalGridSnap : ToolbarOverlay
{
    public bool enabled;
    public int gridSize = 1;

    public override void OnCreated()
    {
        EditorApplication.update += OnUpdate;
    }

    public override void OnWillBeDestroyed()
    {
        EditorApplication.update -= OnUpdate;
    }

    private void OnUpdate()
    {
        if (!enabled || gridSize < 1) return;
        
        foreach (var gameObject in Selection.gameObjects)
        {
            var transform = gameObject.transform;
            
            transform.localScale = new Vector3()
            {
                x = Mathf.Round(transform.localScale.x / gridSize) * gridSize,
                y = transform.localScale.y,
                z = Mathf.Round(transform.localScale.z / gridSize) * gridSize
            };

            var position = transform.position - transform.localScale * 0.5f;
            position = new Vector3()
            {
                x = Mathf.Round(position.x / gridSize) * gridSize,
                y = position.y,
                z = Mathf.Round(position.z / gridSize) * gridSize
            };
            transform.position = position + transform.localScale * 0.5f;
        }
    }

    public override VisualElement CreatePanelContent()
    {
        var root = base.CreatePanelContent();
        root.style.flexDirection = FlexDirection.Column;
        
        var enabledElement = new Toggle("Enabled");
        enabledElement.RegisterValueChangedCallback(value => enabled = value.newValue);
        root.Add(enabledElement);

        var gridSizeElement = new IntegerField("Grid Size");
        gridSizeElement.RegisterValueChangedCallback(value => gridSize = value.newValue);
        root.Add(gridSizeElement);
        
        return root;
    }
}
