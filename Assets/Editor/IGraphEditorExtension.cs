using UnityEngine.UIElements;

public interface IGraphEditorExtension
{
    bool CanHandle(ShizukuGraphBase graph);
    void OnEnable(ShizukuGraphWindow window, ShizukuGraphView graphView, VisualElement rootElement);
    void OnDisable();
    void OnGraphLoaded(ShizukuGraphBase graph);
}
