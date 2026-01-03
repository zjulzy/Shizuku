using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

/// <summary>
/// 自定义控制流端口 - 菱形样式
/// 通过封装InstantiatePort确保连接逻辑正常工作
/// </summary>
public class ControlFlowPort : Port
{
    private static StyleSheet s_StyleSheet;
    
    static ControlFlowPort()
    {
        s_StyleSheet = Resources.Load<StyleSheet>("ControlFlowPort");
        if (s_StyleSheet == null)
        {
            Debug.LogWarning("⚠️ ControlFlowPort样式表未找到");
        }
    }
    
    /// <summary>
    /// 创建控制流端口（封装了InstantiatePort的调用）
    /// 通过反射创建真正的ControlFlowPort实例
    /// </summary>
    public static ControlFlowPort Create(Node node, Orientation orientation, Direction direction, Capacity capacity, string portName)
    {
        // 先用InstantiatePort创建普通端口，获取EdgeConnector
        var tempPort = node.InstantiatePort(orientation, direction, capacity, typeof(Chain));
        
        // 创建ControlFlowPort实例
        var controlFlowPort = new ControlFlowPort(orientation, direction, capacity, typeof(Chain))
        {
            portName = portName
        };

        // 通过反射复制EdgeConnector
        var edgeConnectorField = typeof(Port).GetField("m_EdgeConnector", BindingFlags.NonPublic | BindingFlags.Instance);
        if (edgeConnectorField != null)
        {
            var edgeConnector = edgeConnectorField.GetValue(tempPort);
            if (edgeConnector != null)
            {
                edgeConnectorField.SetValue(controlFlowPort, edgeConnector);
                
                // 添加EdgeConnector作为Manipulator
                var manipulator = edgeConnector as IManipulator;
                if (manipulator != null)
                {
                    controlFlowPort.AddManipulator(manipulator);
                }
            }
        }
        
        // 应用自定义样式
        ApplyControlFlowStyle(controlFlowPort);
        
        return controlFlowPort;
    }
    
    /// <summary>
    /// 为端口应用控制流样式
    /// </summary>
    private static void ApplyControlFlowStyle(Port port)
    {
        // 加载样式表
        if (s_StyleSheet != null && !port.styleSheets.Contains(s_StyleSheet))
        {
            port.styleSheets.Add(s_StyleSheet);
        }
        
        port.AddToClassList("control-flow-port");
        
        // 延迟设置connector样式
        port.schedule.Execute(() =>
        {
            SetupConnectorStyle(port);
        }).ExecuteLater(0);
    }
    
    /// <summary>
    /// 设置connector的菱形样式
    /// 注意：必须在C#中设置，USS无法覆盖Unity的内联样式
    /// </summary>
    private static void SetupConnectorStyle(Port port)
    {
        var connector = port.Q("connector");
        if (connector != null)
        {
            // 形状 - 正方形
            connector.style.borderTopLeftRadius = 0;
            connector.style.borderTopRightRadius = 0;
            connector.style.borderBottomLeftRadius = 0;
            connector.style.borderBottomRightRadius = 0;
            connector.style.width = 14;
            connector.style.height = 14;
            
            // 颜色 - 橙黄色
            connector.style.backgroundColor = new Color(1f, 0.78f, 0.39f, 1f);
            connector.style.borderLeftColor = new Color(1f, 0.59f, 0.2f, 1f);
            connector.style.borderRightColor = new Color(1f, 0.59f, 0.2f, 1f);
            connector.style.borderTopColor = new Color(1f, 0.59f, 0.2f, 1f);
            connector.style.borderBottomColor = new Color(1f, 0.59f, 0.2f, 1f);
            
            // 边框宽度
            connector.style.borderLeftWidth = 2;
            connector.style.borderRightWidth = 2;
            connector.style.borderTopWidth = 2;
            connector.style.borderBottomWidth = 2;
            
            // 旋转45度创建菱形
            connector.style.rotate = new Rotate(new Angle(45, AngleUnit.Degree));
            
            // 处理内部cap元素 - 也变成正方形
            var cap = connector.Q("cap");
            if (cap != null)
            {
                cap.style.borderTopLeftRadius = 0;
                cap.style.borderTopRightRadius = 0;
                cap.style.borderBottomLeftRadius = 0;
                cap.style.borderBottomRightRadius = 0;
            }
        }
    }
    
    // 保留protected构造函数以防需要继承
    protected ControlFlowPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, System.Type type)
        : base(portOrientation, portDirection, portCapacity, type)
    {
    }
}

