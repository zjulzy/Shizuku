using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public interface IGraphEditorExtension
    {
        bool CanHandle(ShizukuGraphBase graph);
        void OnEnable(ShizukuGraphWindow window, ShizukuGraphView graphView, VisualElement rootElement);
        void OnDisable();
        void OnGraphLoaded(ShizukuGraphBase graph);
    }

}
