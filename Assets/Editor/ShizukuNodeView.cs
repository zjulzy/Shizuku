using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using Unity.Mathematics;
using UnityEditor.UIElements;

public class ShizukuNodeView : Node
{
    private ShizukuNodeBase _node;
    private ShizukuGraphBase _graphAsset;

    public ControlFlowPortContainer ControlFlowContainer = null;
    
    
    // 静态缓存样式表，只加载一次
    private static StyleSheet s_StyleSheet;
    
    static ShizukuNodeView()
    {
        // 静态构造函数，只会执行一次
        s_StyleSheet = Resources.Load<StyleSheet>("ShizukuNodeView");
    }
    
    public ShizukuNodeBase RuntimeNode => _node;
    
    public ShizukuNodeView(ShizukuNodeBase node, ShizukuGraphBase graphAsset = null)
    {
        _node = node;
        _graphAsset = graphAsset;
        title = node.Title;
        
        // 应用已加载的样式表（只是引用，不会重复加载）
        if (s_StyleSheet != null && !styleSheets.Contains(s_StyleSheet))
        {
            styleSheets.Add(s_StyleSheet);
        }

        // 设置标题栏背景颜色
        schedule.Execute(() =>
        {
            var titleElement = this.Q("title");
            if (titleElement != null)
            {
                titleElement.style.backgroundColor = node.TitleBarColor;
            }
        }).ExecuteLater(0);
        
        if (node is BlueprintEventNode eventNode)
        {
            ValidateEventNode(eventNode);
        }
    }
    
    private void ValidateEventNode(BlueprintEventNode eventNode)
    {
        if (!eventNode.IsValid())
        {
            var warningLabel = new Label($"⚠ {eventNode.GetValidationMessage()}")
            {
                style =
                {
                    color = new Color(1f, 0.8f, 0f, 1f),
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 5,
                    marginBottom = 5,
                    marginLeft = 5,
                    marginRight = 5,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            extensionContainer.Add(warningLabel);
            
            var refreshButton = new Button(() => ShowRefreshMenu(eventNode))
            {
                text = "更新事件节点"
            };
            refreshButton.style.marginTop = 2;
            refreshButton.style.marginBottom = 5;
            extensionContainer.Add(refreshButton);
        }
    }
    
    private void ShowRefreshMenu(BlueprintEventNode eventNode)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("重新生成参数"), false, () =>
        {
            if (RegenerateEventParameters(eventNode))
            {
                EditorUtility.DisplayDialog("成功", "事件节点已更新", "确定");
                RefreshView();
            }
            else
            {
                EditorUtility.DisplayDialog("失败", $"无法找到方法 '{eventNode.EventName}'", "确定");
            }
        });
        menu.ShowAsContext();
    }
    
    private bool RegenerateEventParameters(BlueprintEventNode eventNode)
    {
        var behaviorType = GetBehaviorTypeFromGraph(_graphAsset);
        if (behaviorType == null) return false;
        
        var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<BlueprintOverridableAttribute>();
            if (attr != null)
            {
                var methodEventName = attr.EventName ?? method.Name;
                if (methodEventName == eventNode.EventName)
                {
                    eventNode.EventParameters.Clear();
                    foreach (var param in method.GetParameters())
                    {
                        var eventParam = new EventParameter
                        {
                            Name = param.Name,
                            TypeName = param.ParameterType.Name,
                            OutputPort = CreatePortForType(param.Name, param.ParameterType)
                        };
                        eventNode.EventParameters.Add(eventParam);
                    }
                    
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private ParameterEdgePort CreatePortForType(string name, Type type)
    {
        if (type == typeof(float))
            return new FloatParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(int))
            return new IntParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(bool))
            return new BoolParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(string))
            return new StringParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(Vector2))
            return new Vector2ParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(Vector3))
            return new Vector3ParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(GameObject))
            return new GameObjectParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(Transform))
            return new TransformParameterEdgePort { IsOut = true, Name = name };
        else if (type == typeof(Color))
            return new ColorParameterEdgePort { IsOut = true, Name = name };
        else
            return new ObjectParameterEdgePort { IsOut = true, Name = name };
    }
    
    private Type GetBehaviorTypeFromGraph(ShizukuGraphBase graph)
    {
        if (graph == null) return null;
        
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
    
    private void RefreshView()
    {
        var container = outputContainer;
        container.Clear();
        InitPort();
    }

    public sealed override string title
    {
        get => base.title;
        set => base.title = value;
    }

    public void SetGraphAsset(ShizukuGraphBase graphAsset)
    {
        _graphAsset = graphAsset;
    }

    public void InitPort()
    {
        if (_node == null)
        {
            Debug.LogError("还没初始化节点就想着初始化端口？");
            return;
        }
        var nodeType = _node.GetType();
        var fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        #region chain端口

        // 创建控制流端口容器（放在节点顶部）
        if (_node.SupportControlInput || _node.SupportControlOutput)
        {
            ControlFlowContainer = new ControlFlowPortContainer();
            // 第一位是标题栏，第二位放控制流端口容器，最后是参数端口容器
            mainContainer.Insert(1, ControlFlowContainer);
        }

        // 添加控制流输入端口（Previous）
        if (_node.SupportControlInput)
        {
            var previousPort = ControlFlowPort.Create(this, Orientation.Horizontal, Direction.Input, Port.Capacity.Single, "Previous");
            ControlFlowContainer?.AddPreviousPort(previousPort);
        }

        // 添加控制流输出端口（Next）
        if (_node.SupportControlOutput)
        {
            foreach (var field in fields)
            {
                // 检查字段是否是 ChainPort 类型
                if (typeof(ChainPort).IsAssignableFrom(field.FieldType))
                {
                    var chainPort = field.GetValue(_node) as ChainPort;
                    if (chainPort != null)
                    {
                        var nextPort = ControlFlowPort.Create(this, Orientation.Horizontal, Direction.Output, Port.Capacity.Single, chainPort.Name);
                        ControlFlowContainer?.AddNextPort(nextPort);
                    }
                }
            }
        }

        #endregion

        #region 参数端口

        if (_node is BlueprintEventNode eventNode)
        {
            foreach (var param in eventNode.EventParameters)
            {
                if (param.OutputPort != null)
                {
                    var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, param.OutputPort.GetType());
                    outputPort.portName = param.Name;
                    outputPort.AddToClassList("parameter-port");
                    
                    // 添加数据类型 tooltip
                    SetPortTooltip(outputPort, param.OutputPort.GetType());
                    
                    outputContainer.Add(outputPort);
                }
            }
        }
        
        foreach (var field in fields)
        {
            if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(_node) as ParameterEdgePort;
                if (port != null)
                {
                    if (port.IsOut)
                    {
                        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, field.FieldType);
                        outputPort.portName = port.Name;
                        outputPort.AddToClassList("parameter-port");
                        
                        // 添加数据类型 tooltip
                        SetPortTooltip(outputPort, field.FieldType);
                        
                        outputContainer.Add(outputPort);
                    }
                    else
                    {
                        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, field.FieldType);
                        inputPort.portName = port.Name;
                        inputPort.AddToClassList("parameter-port");
                        
                        // 添加数据类型 tooltip
                        SetPortTooltip(inputPort, field.FieldType);

                        var inputField = CreateInputFieldForPort(port);
                        if (inputField != null)
                        {
                            // 将输入字段存储在 userData 中，方便后续隐藏/显示
                            inputPort.userData = inputField;
                            
                            // 检查图数据中是否已有连接到该端口的边
                            bool hasConnection = _graphAsset != null && _graphAsset.Edges.Any(e => 
                                e.InputNodeGuid == _node.GUID && e.InputPortName == port.Name);
                            
                            // 根据连接状态决定是否显示输入字段
                            UpdateInputFieldVisibility(inputPort, hasConnection);
                            
                            inputPort.contentContainer.Add(inputField);
                        }

                        inputContainer.Add(inputPort);
                    }
                }
            }
        }

        #endregion


        RefreshExpandedState();
        RefreshPorts();
    }


    private VisualElement CreateInputFieldForPort(ParameterEdgePort port)
    {
        // 根据端口类型创建对应的输入控件
        switch (port)
        {
            case IntParameterEdgePort intPort:
            {
                var intField = new IntegerField()
                {
                    value = intPort.DefaultValue
                };
                intField.style.minWidth = 30;
                intField.RegisterValueChangedCallback(evt =>
                {
                    intPort.DefaultValue = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return intField;
            }
            
            case FloatParameterEdgePort floatPort:
            {
                var floatField = new FloatField()
                {
                    value = floatPort.DefaultValue
                };
                floatField.style.minWidth = 30;
                floatField.RegisterValueChangedCallback(evt =>
                {
                    floatPort.DefaultValue = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return floatField;
            }
            
            case BoolParameterEdgePort boolPort:
            {
                var boolField = new Toggle()
                {
                    value = boolPort.DefaultValue
                };
                boolField.style.minWidth = 10;
                boolField.RegisterValueChangedCallback(evt =>
                {
                    boolPort.DefaultValue = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return boolField;
            }
            
            case StringParameterEdgePort stringPort:
            {
                var stringField = new TextField()
                {
                    value = stringPort.DefaultValue
                };
                stringField.style.minWidth = 30;
                stringField.RegisterValueChangedCallback(evt =>
                {
                    stringPort.DefaultValue = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return stringField;
            }
            
            case Vector2ParameterEdgePort vector2Port:
            {
                var vector2Field = new Vector2Field()
                {
                    value = vector2Port.DefaultValue
                };
                vector2Field.style.minWidth = 80;
                vector2Field.RegisterValueChangedCallback(evt =>
                {
                    vector2Port.DefaultValue = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return vector2Field;
            }
            
            case Vector3ParameterEdgePort vector3Port:
            {
                var vector3Field = new Vector3Field()
                {
                    value = vector3Port.DefaultValue
                };
                vector3Field.style.minWidth = 100;
                vector3Field.RegisterValueChangedCallback(evt =>
                {
                    vector3Port.DefaultValue = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return vector3Field;
            }
            
            case GameObjectParameterEdgePort gameObjectPort:
            {
                var objectField = new ObjectField()
                {
                    objectType = typeof(GameObject),
                    value = gameObjectPort.DefaultValue
                };
                objectField.style.minWidth = 80;
                objectField.RegisterValueChangedCallback(evt =>
                {
                    gameObjectPort.DefaultValue = evt.newValue as GameObject;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return objectField;
            }
            
            case TransformParameterEdgePort transformPort:
            {
                var objectField = new ObjectField()
                {
                    objectType = typeof(Transform),
                    value = transformPort.DefaultValue
                };
                objectField.style.minWidth = 80;
                objectField.RegisterValueChangedCallback(evt =>
                {
                    transformPort.DefaultValue = evt.newValue as Transform;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return objectField;
            }
            
            case ColorParameterEdgePort colorPort:
            {
                var colorField = new ColorField()
                {
                    value = colorPort.DefaultValue
                };
                colorField.style.minWidth = 50;
                colorField.RegisterValueChangedCallback(evt =>
                {
                    colorPort.DefaultValue = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return colorField;
            }
            
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 更新输入字段的可见性（根据端口是否有连接）
    /// </summary>
    private void UpdateInputFieldVisibility(Port inputPort, bool isConnected)
    {
        if (inputPort == null || inputPort.userData == null)
            return;
        
        var inputField = inputPort.userData as VisualElement;
        if (inputField == null)
            return;
        
        // 如果端口已连接，隐藏输入字段；否则显示
        inputField.style.display = isConnected ? DisplayStyle.None : DisplayStyle.Flex;
    }
    
    /// <summary>
    /// 当端口连接状态改变时调用此方法
    /// </summary>
    /// <param name="port">输入端口</param>
    /// <param name="isConnected">是否已连接（true=已连接，false=未连接）</param>
    public void OnPortConnectionChanged(Port port, bool isConnected)
    {
        if (port != null && port.direction == Direction.Input)
        {
            UpdateInputFieldVisibility(port, isConnected);
        }
    }
    
    /// <summary>
    /// AI-generated
    /// 为端口设置数据类型 tooltip
    /// </summary>
    private void SetPortTooltip(Port port, System.Type portType)
    {
        if (port == null || portType == null)
            return;
        
        // 获取实际的值类型
        string typeName = GetPortValueTypeName(portType);
        string tooltipText = $"类型: {typeName}";
        
        // 1. 设置端口本身的 tooltip
        port.tooltip = tooltipText;
        
        // 2. 扩展端口的可交互区域
        // 让整个端口行（包括标签文字）都能触发 tooltip
        port.style.paddingLeft = 0;
        port.style.paddingRight = 0;
        port.style.paddingTop = 2;
        port.style.paddingBottom = 2;
        port.style.marginLeft = 0;
        port.style.marginRight = 0;
        
        // 确保端口可以响应鼠标事件
        port.pickingMode = PickingMode.Position;
        
        // 3. 为端口的连接器（圆点）设置 tooltip
        var connector = port.Q("connector");
        if (connector != null)
        {
            connector.tooltip = tooltipText;
            connector.pickingMode = PickingMode.Position;
        }
        
        // 4. 为端口的标签设置 tooltip（这是端口名称文字）
        var label = port.Q<Label>();
        if (label != null)
        {
            label.tooltip = tooltipText;
            label.pickingMode = PickingMode.Position;
            // 扩展标签的可点击区域
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
        }
        
        // 5. 为端口的内容容器设置 tooltip（包含输入控件的区域）
        if (port.contentContainer != null)
        {
            port.contentContainer.tooltip = tooltipText;
            port.contentContainer.pickingMode = PickingMode.Position;
        }
        
        // 6. 为 connector-text 设置 tooltip（如果存在）
        var connectorText = port.Q("connectorText");
        if (connectorText != null)
        {
            connectorText.tooltip = tooltipText;
            connectorText.pickingMode = PickingMode.Position;
        }
        
        // 7. 递归为所有子元素设置 tooltip，扩大触发范围
        SetTooltipRecursive(port, tooltipText);
    }
    
    /// <summary>
    /// 递归为元素及其所有子元素设置 tooltip
    /// </summary>
    private void SetTooltipRecursive(VisualElement element, string tooltip)
    {
        if (element == null)
            return;
        
        element.tooltip = tooltip;
        element.pickingMode = PickingMode.Position;
        
        // 为所有子元素也设置 tooltip
        foreach (var child in element.Children())
        {
            SetTooltipRecursive(child, tooltip);
        }
    }
    
    /// <summary>
    /// 获取端口值类型的友好名称
    /// </summary>
    private string GetPortValueTypeName(System.Type portType)
    {
        // 如果自身是泛型类型（如 ParameterEdgePort<float>），提取泛型参数
        if (portType.IsGenericType)
        {
            var genericArgs = portType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return GetFriendlyTypeName(genericArgs[0]);
            }
        }
        
        // 如果自身不是泛型，但基类是泛型（如 FloatParameterEdgePort : ParameterEdgePort<float>）
        var baseType = portType.BaseType;
        if (baseType != null && baseType.IsGenericType)
        {
            var genericArgs = baseType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return GetFriendlyTypeName(genericArgs[0]);
            }
        }
        
        // 否则返回类型名称
        return portType.Name;
    }
    
    /// <summary>
    /// 获取类型的友好名称
    /// </summary>
    private string GetFriendlyTypeName(System.Type type)
    {
        // 处理常见的 C# 类型别名
        if (type == typeof(int))
            return "Int";
        if (type == typeof(float))
            return "Float";
        if (type == typeof(bool))
            return "Bool";
        if (type == typeof(string))
            return "String";
        if (type == typeof(Vector2))
            return "Vector2";
        if (type == typeof(Vector3))
            return "Vector3";
        if (type == typeof(GameObject))
            return "GameObject";
        if (type == typeof(Transform))
            return "Transform";
        if (type == typeof(Color))
            return "Color";
        
        // 默认返回类型名称
        return type.Name;
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        _node.PositionAndSize = new float4(newPos.x, newPos.y, newPos.width, newPos.height);
    }
}
