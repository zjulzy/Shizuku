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
    private ScrollView _propertiesPanel;
    private ScrollView _eventsPanel;

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
                backgroundColor = new Color(0.22f, 0.22f, 0.22f)
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
        _leftPanel.Add(propertiesHeader);

        _propertiesPanel = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1,
                maxHeight = 300
            }
        };
        _leftPanel.Add(_propertiesPanel);

        var separator = new VisualElement
        {
            style =
            {
                height = 1,
                backgroundColor = new Color(0.15f, 0.15f, 0.15f),
                marginTop = 5,
                marginBottom = 5
            }
        };
        _leftPanel.Add(separator);

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
        _leftPanel.Add(eventsHeader);

        _eventsPanel = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1
            }
        };
        _leftPanel.Add(_eventsPanel);

        _rootElement.Insert(0, _leftPanel);
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
}
