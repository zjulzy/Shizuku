using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using Unity.Mathematics;

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
                        outputContainer.Add(outputPort);
                    }
                    else
                    {
                        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, field.FieldType);
                        inputPort.portName = port.Name;
                        inputPort.AddToClassList("parameter-port");

                        var inputField = CreateInputFieldForPort(port);
                        if (inputField != null)
                        {
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
                    value = intPort.Value
                };
                intField.style.minWidth = 30;
                intField.RegisterValueChangedCallback(evt =>
                {
                    intPort.Value = evt.newValue;
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
                    value = floatPort.Value
                };
                floatField.style.minWidth = 30;
                floatField.RegisterValueChangedCallback(evt =>
                {
                    floatPort.Value = evt.newValue;
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
                    value = boolPort.Value
                };
                boolField.style.minWidth = 10;
                boolField.RegisterValueChangedCallback(evt =>
                {
                    boolPort.Value = evt.newValue;
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
                    value = stringPort.Value
                };
                stringField.style.minWidth = 30;
                stringField.RegisterValueChangedCallback(evt =>
                {
                    stringPort.Value = evt.newValue;
                    if (_graphAsset != null)
                    {
                        EditorUtility.SetDirty(_graphAsset);
                    }
                });
                return stringField;
            }
            
            default:
                return null;
        }
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        _node.PositionAndSize = new float4(newPos.x, newPos.y, newPos.width, newPos.height);
    }
}
