using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BlueprintEditorExtension : IGraphEditorExtension
{
    private ShizukuGraphWindow _window;
    private ShizukuGraphView _graphView;
    private VisualElement _rootElement;
    private ShizukuGraphBase _currentGraph;
    
    private VisualElement _leftPanel;
    private VisualElement _propertiesContainer;
    private VisualElement _eventsContainer;
    private VisualElement _functionsContainer;
    private ScrollView _propertiesPanel;
    private ScrollView _eventsPanel;
    private ScrollView _functionsPanel;
    private VisualElement _resizer1;
    private VisualElement _resizer2;

    public bool CanHandle(ShizukuGraphBase graph)
    {
        return graph != null && graph.GetType().BaseType != null 
               && graph.GetType().BaseType.IsGenericType 
               && graph.GetType().BaseType.GetGenericTypeDefinition().Name.StartsWith("ShizukuBluePrint");
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
        if (_leftPanel != null && _leftPanel.parent != null)
        {
            _leftPanel.RemoveFromHierarchy();
        }
        
        _leftPanel = null;
        _propertiesPanel = null;
        _eventsPanel = null;
        _functionsPanel = null;
        _currentGraph = null;
    }

    public void OnGraphLoaded(ShizukuGraphBase graph)
    {
        _currentGraph = graph;
        RefreshPanels();
    }

    private void BuildUI()
    {
        _leftPanel = new VisualElement
        {
            style =
            {
                width = 250,
                borderRightWidth = 1,
                borderRightColor = new Color(0.2f, 0.2f, 0.2f),
                backgroundColor = new Color(0.22f, 0.22f, 0.22f),
                flexDirection = FlexDirection.Column
            }
        };

        // 属性列表容器
        _propertiesContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                flexShrink = 1,
                minHeight = 100
            }
        };

        var propertiesHeader = new Label("属性列表")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14,
                paddingTop = 10,
                paddingBottom = 5,
                paddingLeft = 10,
                paddingRight = 10,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f)
            }
        };
        _propertiesContainer.Add(propertiesHeader);

        _propertiesPanel = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1
            }
        };
        _propertiesContainer.Add(_propertiesPanel);

        _leftPanel.Add(_propertiesContainer);

        // 第一个可拖动的分隔条（属性 - 事件）
        _resizer1 = new VisualElement
        {
            style =
            {
                height = 8,
                backgroundColor = new Color(0.15f, 0.15f, 0.15f)
            }
        };
        
        // 添加悬停效果
        _resizer1.RegisterCallback<MouseEnterEvent>(evt =>
        {
            _resizer1.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        });
        _resizer1.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            if (!_isResizing1)
            {
                _resizer1.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            }
        });
        
        // 添加拖动功能
        _resizer1.RegisterCallback<MouseDownEvent>(OnResizer1MouseDown);
        
        _leftPanel.Add(_resizer1);

        // 事件列表容器
        _eventsContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                flexShrink = 1,
                minHeight = 100
            }
        };

        var eventsHeader = new Label("事件列表")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14,
                paddingTop = 5,
                paddingBottom = 5,
                paddingLeft = 10,
                paddingRight = 10,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f)
            }
        };
        _eventsContainer.Add(eventsHeader);

        _eventsPanel = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1
            }
        };
        _eventsContainer.Add(_eventsPanel);

        _leftPanel.Add(_eventsContainer);

        // 第二个可拖动的分隔条（事件 - 函数）
        _resizer2 = new VisualElement
        {
            style =
            {
                height = 8,
                backgroundColor = new Color(0.15f, 0.15f, 0.15f)
            }
        };
        
        // 添加悬停效果
        _resizer2.RegisterCallback<MouseEnterEvent>(evt =>
        {
            _resizer2.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.8f);
        });
        _resizer2.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            if (!_isResizing2)
            {
                _resizer2.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            }
        });
        
        // 添加拖动功能
        _resizer2.RegisterCallback<MouseDownEvent>(OnResizer2MouseDown);
        
        _leftPanel.Add(_resizer2);

        // 函数列表容器
        _functionsContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                flexShrink = 1,
                minHeight = 100
            }
        };

        // 函数列表标题栏（包含加号按钮）
        var functionsHeaderContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                paddingTop = 5,
                paddingBottom = 5,
                paddingLeft = 10,
                paddingRight = 5,
                justifyContent = Justify.SpaceBetween,
                alignItems = Align.Center
            }
        };

        var functionsHeader = new Label("函数列表")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14
            }
        };
        functionsHeaderContainer.Add(functionsHeader);

        var addFunctionButton = new Button(() => OnAddFunctionClicked())
        {
            text = "+",
            style =
            {
                width = 24,
                height = 24,
                backgroundColor = new Color(0.3f, 0.5f, 0.8f),
                color = Color.white,
                unityTextAlign = TextAnchor.MiddleCenter,
                fontSize = 16,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginRight = 5
            }
        };
        functionsHeaderContainer.Add(addFunctionButton);

        _functionsContainer.Add(functionsHeaderContainer);

        _functionsPanel = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1
            }
        };
        _functionsContainer.Add(_functionsPanel);

        _leftPanel.Add(_functionsContainer);

        _rootElement.Insert(0, _leftPanel);
    }
    
    private bool _isResizing1 = false;
    private bool _isResizing2 = false;
    private float _startMouseY;
    private float _startPropertiesHeight;
    private float _startEventsHeight;
    private float _startFunctionsHeight;
    
    private void OnResizer1MouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0) // 左键
        {
            _isResizing1 = true;
            _startMouseY = evt.mousePosition.y;
            _startPropertiesHeight = _propertiesContainer.resolvedStyle.height;
            _startEventsHeight = _eventsContainer.resolvedStyle.height;
            
            _resizer1.CaptureMouse();
            _resizer1.RegisterCallback<MouseMoveEvent>(OnResizer1MouseMove);
            _resizer1.RegisterCallback<MouseUpEvent>(OnResizer1MouseUp);
            
            evt.StopPropagation();
        }
    }
    
    private void OnResizer1MouseMove(MouseMoveEvent evt)
    {
        if (_isResizing1)
        {
            float deltaY = evt.mousePosition.y - _startMouseY;
            
            // 计算新高度，确保最小值
            float newPropertiesHeight = Mathf.Max(100, _startPropertiesHeight + deltaY);
            float newEventsHeight = Mathf.Max(100, _startEventsHeight - deltaY);
            
            // 检查是否达到最小值限制
            if (newPropertiesHeight <= 100 && deltaY < 0)
                return; // 属性区域已达最小值，不能再缩小
            if (newEventsHeight <= 100 && deltaY > 0)
                return; // 事件区域已达最小值，不能再缩小
            
            // 应用新高度
            _propertiesContainer.style.height = newPropertiesHeight;
            _propertiesContainer.style.flexGrow = 0;
            _propertiesContainer.style.flexShrink = 0;
            
            _eventsContainer.style.height = newEventsHeight;
            _eventsContainer.style.flexGrow = 0;
            _eventsContainer.style.flexShrink = 0;
            
            // 确保函数列表保持固定或自动调整
            if (_functionsContainer.style.height.value.value > 0)
            {
                // 如果函数区域已经有固定高度，保持不变
                _functionsContainer.style.flexGrow = 0;
                _functionsContainer.style.flexShrink = 0;
            }
            else
            {
                // 否则让它填充剩余空间
                _functionsContainer.style.flexGrow = 1;
                _functionsContainer.style.flexShrink = 1;
            }
            
            evt.StopPropagation();
        }
    }
    
    private void OnResizer1MouseUp(MouseUpEvent evt)
    {
        if (_isResizing1)
        {
            _isResizing1 = false;
            _resizer1.ReleaseMouse();
            _resizer1.UnregisterCallback<MouseMoveEvent>(OnResizer1MouseMove);
            _resizer1.UnregisterCallback<MouseUpEvent>(OnResizer1MouseUp);
            
            // 恢复分隔条颜色
            _resizer1.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            
            evt.StopPropagation();
        }
    }
    
    private void OnResizer2MouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0) // 左键
        {
            _isResizing2 = true;
            _startMouseY = evt.mousePosition.y;
            _startEventsHeight = _eventsContainer.resolvedStyle.height;
            _startFunctionsHeight = _functionsContainer.resolvedStyle.height;
            
            _resizer2.CaptureMouse();
            _resizer2.RegisterCallback<MouseMoveEvent>(OnResizer2MouseMove);
            _resizer2.RegisterCallback<MouseUpEvent>(OnResizer2MouseUp);
            
            evt.StopPropagation();
        }
    }
    
    private void OnResizer2MouseMove(MouseMoveEvent evt)
    {
        if (_isResizing2)
        {
            float deltaY = evt.mousePosition.y - _startMouseY;
            
            // 计算新高度，确保最小值
            float newEventsHeight = Mathf.Max(100, _startEventsHeight + deltaY);
            float newFunctionsHeight = Mathf.Max(100, _startFunctionsHeight - deltaY);
            
            // 检查是否达到最小值限制
            if (newEventsHeight <= 100 && deltaY < 0)
                return; // 事件区域已达最小值，不能再缩小
            if (newFunctionsHeight <= 100 && deltaY > 0)
                return; // 函数区域已达最小值，不能再缩小
            
            // 应用新高度
            _eventsContainer.style.height = newEventsHeight;
            _eventsContainer.style.flexGrow = 0;
            _eventsContainer.style.flexShrink = 0;
            
            _functionsContainer.style.height = newFunctionsHeight;
            _functionsContainer.style.flexGrow = 0;
            _functionsContainer.style.flexShrink = 0;
            
            // 确保属性列表保持固定
            if (_propertiesContainer.style.height.value.value > 0)
            {
                // 如果属性区域已经有固定高度，保持不变
                _propertiesContainer.style.flexGrow = 0;
                _propertiesContainer.style.flexShrink = 0;
            }
            else
            {
                // 否则让它填充剩余空间
                _propertiesContainer.style.flexGrow = 1;
                _propertiesContainer.style.flexShrink = 1;
            }
            
            evt.StopPropagation();
        }
    }
    
    private void OnResizer2MouseUp(MouseUpEvent evt)
    {
        if (_isResizing2)
        {
            _isResizing2 = false;
            _resizer2.ReleaseMouse();
            _resizer2.UnregisterCallback<MouseMoveEvent>(OnResizer2MouseMove);
            _resizer2.UnregisterCallback<MouseUpEvent>(OnResizer2MouseUp);
            
            // 恢复分隔条颜色
            _resizer2.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            
            evt.StopPropagation();
        }
    }
    
    /// <summary>
    /// 点击函数列表的加号按钮
    /// </summary>
    private void OnAddFunctionClicked()
    {
        // TODO: 实现添加函数的逻辑
        Debug.Log("点击了添加函数按钮");
    }

    private void RefreshPanels()
    {
        RefreshPropertiesPanel();
        RefreshEventsPanel();
    }

    private void RefreshPropertiesPanel()
    {
        _propertiesPanel.Clear();

        if (_currentGraph == null) return;

        var behaviorType = GetBehaviorType(_currentGraph);
        if (behaviorType == null)
        {
            _propertiesPanel.Add(new Label("无法获取 Behavior 类型"));
            return;
        }

        var fields = behaviorType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.GetCustomAttribute<SerializeField>() != null || f.IsPublic)
            .ToArray();

        if (fields.Length == 0)
        {
            _propertiesPanel.Add(new Label("无可序列化属性") { style = { paddingLeft = 10, color = Color.gray } });
            return;
        }

        foreach (var field in fields)
        {
            var fieldLabel = new Label($"• {field.Name} ({field.FieldType.Name})")
            {
                style =
                {
                    paddingLeft = 10,
                    paddingTop = 5,
                    paddingBottom = 5,
                    color = new Color(0.8f, 0.8f, 0.8f)
                }
            };
            _propertiesPanel.Add(fieldLabel);
        }
    }

    private void RefreshEventsPanel()
    {
        _eventsPanel.Clear();

        if (_currentGraph == null) return;

        var behaviorType = GetBehaviorType(_currentGraph);
        if (behaviorType == null)
        {
            _eventsPanel.Add(new Label("无法获取 Behavior 类型"));
            return;
        }

        var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<BlueprintOverridableAttribute>() != null)
            .ToArray();

        if (methods.Length == 0)
        {
            _eventsPanel.Add(new Label("无可覆写方法") { style = { paddingLeft = 10, color = Color.gray } });
            return;
        }

        var existingEvents = GetExistingEventNames();

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<BlueprintOverridableAttribute>();
            var eventName = attr.EventName ?? method.Name;
            var parameters = method.GetParameters();
            var paramStr = parameters.Length > 0 
                ? $"({string.Join(", ", parameters.Select(p => p.ParameterType.Name))})" 
                : "()";

            var eventContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 10,
                    paddingTop = 3,
                    paddingBottom = 3,
                    justifyContent = Justify.SpaceBetween
                }
            };

            var isImplemented = existingEvents.Contains(eventName);
            var eventLabel = new Label($"{(isImplemented ? "✓" : "○")} {eventName}{paramStr}")
            {
                style =
                {
                    flexGrow = 1,
                    color = isImplemented ? new Color(0.5f, 1f, 0.5f) : new Color(0.7f, 0.7f, 0.7f)
                }
            };

            // 如果事件已实现，添加点击跳转功能
            if (isImplemented)
            {
                eventLabel.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0) // 左键点击
                    {
                        FocusOnEventNode(eventName);
                    }
                });
                
                // 添加鼠标悬停效果
                eventLabel.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    eventLabel.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                });
                
                eventLabel.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    eventLabel.style.backgroundColor = new Color(0, 0, 0, 0);
                });
            }

            eventContainer.Add(eventLabel);
            _eventsPanel.Add(eventContainer);
        }
    }

    private System.Type GetBehaviorType(ShizukuGraphBase graph)
    {
        var graphType = graph.GetType();
        while (graphType != null && graphType != typeof(object))
        {
            if (graphType.IsGenericType)
            {
                var genericDef = graphType.GetGenericTypeDefinition();
                if (genericDef.Name.StartsWith("ShizukuBluePrint"))
                {
                    var genericArgs = graphType.GetGenericArguments();
                    if (genericArgs.Length > 0)
                    {
                        return genericArgs[0];
                    }
                }
            }
            graphType = graphType.BaseType;
        }
        return null;
    }

    private System.Collections.Generic.HashSet<string> GetExistingEventNames()
    {
        var existingEvents = new System.Collections.Generic.HashSet<string>();
        if (_currentGraph != null)
        {
            foreach (var node in _currentGraph.Nodes)
            {
                if (node is BlueprintEventNode eventNode)
                {
                    existingEvents.Add(eventNode.EventName);
                }
            }
        }
        return existingEvents;
    }
    
    /// <summary>
    /// 聚焦到指定事件名称的节点
    /// </summary>
    private void FocusOnEventNode(string eventName)
    {
        if (_currentGraph == null || _graphView == null)
            return;
        
        // 查找事件节点
        BlueprintEventNode targetEventNode = null;
        foreach (var node in _currentGraph.Nodes)
        {
            if (node is BlueprintEventNode eventNode && eventNode.EventName == eventName)
            {
                targetEventNode = eventNode;
                break;
            }
        }
        
        if (targetEventNode == null)
        {
            Debug.LogWarning($"未找到事件节点: {eventName}");
            return;
        }
        
        // 使用反射调用 GraphView 的私有方法来聚焦节点
        var focusMethod = _graphView.GetType().GetMethod("FocusOnEventNode", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (focusMethod != null)
        {
            focusMethod.Invoke(_graphView, new object[] { eventName });
        }
        else
        {
            Debug.LogWarning("未找到 FocusOnEventNode 方法");
        }
    }
}
