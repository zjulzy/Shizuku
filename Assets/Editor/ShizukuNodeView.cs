using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Reflection;
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
            // Unity的Node元素的标题栏
            var titleElement = this.Q("title");
            if (titleElement != null)
            {
                titleElement.style.backgroundColor = node.TitleBarColor;
            }
        }).ExecuteLater(0);
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

        // 基于反射自动为所有参数和结果生成端口
        
        foreach (var field in fields)
        {
            // 检查字段是否是 ParameterEdgePort 类型
            if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(_node) as ParameterEdgePort;
                if (port != null)
                {
                    // 根据 IsOut 属性判断是输入端口还是输出端口
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

                        // 为输入端口添加默认值输入框
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
