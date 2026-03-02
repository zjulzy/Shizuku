using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ShizukuGraphWindow : EditorWindow
{
    private ShizukuGraphView _graphView;
    private IGraphEditorExtension _currentExtension;
    
    // 使用 SerializeField 保存当前图的引用，在 Play 模式切换时不会丢失
    [SerializeField]
    private ShizukuGraphBase _currentGraph;
    
    private VisualElement _contentContainer;
    private List<IGraphEditorExtension> _availableExtensions = new List<IGraphEditorExtension>();
    
    [MenuItem("Shizuku/ShizukuGraphWindow")]
    public static void OpenWindow()
    {
        ShizukuGraphWindow window = GetWindow<ShizukuGraphWindow>();
        window.titleContent = new GUIContent("Shizuku Graph");
    }

    private void OnEnable()
    {
        RegisterExtensions();
        BuildUI();
        
        // 如果之前有打开的图（例如从 Play 模式返回时），重新加载它
        if (_currentGraph != null)
        {
            _graphView.LoadFromAsset(_currentGraph);
            LoadExtension(_currentGraph);
        }
    }
    
    private void RegisterExtensions()
    {
        _availableExtensions.Clear();
        _availableExtensions.Add(new BlueprintEditorExtension());
        _availableExtensions.Add(new BaseGraphEditorExtension());
    }
    
    private void BuildUI()
    {
        rootVisualElement.Clear();
        
        // 工具栏
        Toolbar toolbar = new Toolbar();
        Button saveButton = new Button(() => { _graphView?.SaveToAsset(); });
        saveButton.text = "保存";
        
        Button refreshButton = new Button(() => { RefreshExtension(); });
        refreshButton.text = "刷新";
        
        toolbar.Add(saveButton);
        toolbar.Add(refreshButton);
        rootVisualElement.Add(toolbar);
        
        // 内容容器
        _contentContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 1
            }
        };
        rootVisualElement.Add(_contentContainer);
        
        // 图视图
        _graphView = new ShizukuGraphView();
        _graphView.style.flexGrow = 1;
        _contentContainer.Add(_graphView);
    }
    
    private void OnDisable()
    {
        UnloadExtension();
        rootVisualElement.Clear();
        _graphView = null;
        // 不清空 _currentGraph，让它被 Unity 序列化保存，以便在 Play 模式切换后恢复
    }
    
    private void LoadExtension(ShizukuGraphBase graph)
    {
        UnloadExtension();
        
        foreach (var extension in _availableExtensions)
        {
            if (extension.CanHandle(graph))
            {
                _currentExtension = extension;
                _currentExtension.OnEnable(this, _graphView, _contentContainer);
                _currentExtension.OnGraphLoaded(graph);
                
                _graphView.OnGraphChanged = () => _currentExtension?.OnGraphLoaded(graph);
                break;
            }
        }
    }
    
    private void UnloadExtension()
    {
        if (_currentExtension != null)
        {
            _currentExtension.OnDisable();
            _currentExtension = null;
        }
    }
    
    private void RefreshExtension()
    {
        if (_currentGraph != null && _currentExtension != null)
        {
            _currentExtension.OnGraphLoaded(_currentGraph);
        }
    }
    
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        ShizukuGraphBase graphAsset = EditorUtility.InstanceIDToObject(instanceID) as ShizukuGraphBase;
        if (graphAsset != null)
        {
            ShizukuGraphWindow window = GetWindow<ShizukuGraphWindow>();
            window.titleContent = new GUIContent($"Shizuku Graph - {graphAsset.name}");
            
            window._currentGraph = graphAsset;
            window._graphView.LoadFromAsset(graphAsset);
            window.LoadExtension(graphAsset);
            window.Show();
            return true;
        }
        return false;
    }
}
