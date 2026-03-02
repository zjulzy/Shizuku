using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseGraphEditorExtension : IGraphEditorExtension
{
    private ShizukuGraphWindow _window;
    private ShizukuGraphView _graphView;
    private VisualElement _rootElement;
    private ShizukuGraphBase _currentGraph;
    
    private VisualElement _rightPanel;
    private ScrollView _variablesPanel;
    
    public bool CanHandle(ShizukuGraphBase graph)
    {
        // 只要是 ShizukuGraphBase 的图都可以处理
        return graph != null;
    }

    public void OnEnable(ShizukuGraphWindow window, ShizukuGraphView graphView, VisualElement rootElement)
    {
        _window = window;
        _graphView = graphView;
        _rootElement = rootElement;
        
        BuildUI();
    }

    public void OnDisable()
    {
        if (_rightPanel != null && _rightPanel.parent != null)
        {
            _rightPanel.RemoveFromHierarchy();
        }
        
        _rightPanel = null;
        _variablesPanel = null;
        _currentGraph = null;
    }

    public void OnGraphLoaded(ShizukuGraphBase graph)
    {
        _currentGraph = graph;
        RefreshVariablesPanel();
    }
    
    private void BuildUI()
    {
        _rightPanel = new VisualElement
        {
            style =
            {
                width = 300,
                borderLeftWidth = 1,
                borderLeftColor = new Color(0.2f, 0.2f, 0.2f),
                backgroundColor = new Color(0.22f, 0.22f, 0.22f),
                flexDirection = FlexDirection.Column
            }
        };
        
        // 变量列表标题栏（带添加按钮）
        var variablesHeader = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                paddingTop = 8,
                paddingBottom = 8,
                paddingLeft = 10,
                paddingRight = 10,
                justifyContent = Justify.SpaceBetween
            }
        };
        
        var headerLabel = new Label("变量列表")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };
        
        var addButton = new Button(() => AddNewVariable())
        {
            text = "+",
            style =
            {
                width = 24,
                height = 24,
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 16
            }
        };
        
        variablesHeader.Add(headerLabel);
        variablesHeader.Add(addButton);
        _rightPanel.Add(variablesHeader);
        
        // 变量列表滚动视图
        _variablesPanel = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1
            }
        };
        _rightPanel.Add(_variablesPanel);
        
        // 添加到根容器
        _rootElement.Add(_rightPanel);
    }
    
    private void RefreshVariablesPanel()
    {
        _variablesPanel.Clear();
        
        if (_currentGraph == null)
        {
            var emptyLabel = new Label("未加载图数据")
            {
                style =
                {
                    paddingTop = 20,
                    paddingLeft = 10,
                    color = new Color(0.6f, 0.6f, 0.6f),
                    unityTextAlign = TextAnchor.UpperCenter
                }
            };
            _variablesPanel.Add(emptyLabel);
            return;
        }
        
        // TODO: 这里将来会从 _currentGraph 中读取变量列表
        // 目前显示占位内容
        var placeholderLabel = new Label("变量功能开发中...")
        {
            style =
            {
                paddingTop = 20,
                paddingLeft = 10,
                color = new Color(0.8f, 0.8f, 0.5f),
                unityTextAlign = TextAnchor.UpperCenter
            }
        };
        _variablesPanel.Add(placeholderLabel);
    }
    
    private void AddNewVariable()
    {
        if (_currentGraph == null)
        {
            Debug.LogWarning("没有加载的图，无法添加变量");
            return;
        }
        
        // TODO: 弹出变量创建对话框
        Debug.Log("添加新变量（功能开发中）");
    }
}
