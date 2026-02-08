using UnityEngine.UIElements;

public class BaseGraphEditorExtension : IGraphEditorExtension
{
    public bool CanHandle(ShizukuGraphBase graph)
    {
        return graph != null && graph.GetType() == typeof(ShizukuGraphBase);
    }

    public void OnEnable(ShizukuGraphWindow window, ShizukuGraphView graphView, VisualElement rootElement)
    {
    }

    public void OnDisable()
    {
    }

    public void OnGraphLoaded(ShizukuGraphBase graph)
    {
    }
}
