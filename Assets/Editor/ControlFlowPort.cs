using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 自定义控制流端口 - 菱形样式
/// </summary>
public class ControlFlowPort : Port
{
    private static StyleSheet s_StyleSheet;
    
    static ControlFlowPort()
    {
        s_StyleSheet = Resources.Load<StyleSheet>("ControlFlowPort");
        if (s_StyleSheet == null)
        {
            Debug.LogError("⚠️ ControlFlowPort样式表未找到");
        }
    }
    
    /// <summary>
    /// 创建控制流端口
    /// </summary>
    public static ControlFlowPort Create(Orientation orientation, Direction direction, Capacity capacity, string portName)
    {
        var port = new ControlFlowPort(orientation, direction, capacity, null, portName);
        return port;
    }
    
    private ControlFlowPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, System.Type type, string name)
        : base(portOrientation, portDirection, portCapacity, type)
    {
        // 设置端口名称
        portName = name;
        
        // 加载样式表（用于端口标签等非connector样式）
        if (s_StyleSheet != null && !styleSheets.Contains(s_StyleSheet))
        {
            styleSheets.Add(s_StyleSheet);
        }
        
        AddToClassList("control-flow-port");
        
        // 延迟设置connector样式（必须在C#中设置，USS无法覆盖Unity内联样式）
        schedule.Execute(SetupConnectorStyle).ExecuteLater(0);
    }
    
    /// <summary>
    /// 设置connector的菱形样式
    /// 注意：必须在C#中设置，USS无法覆盖Unity的内联样式
    /// </summary>
    private void SetupConnectorStyle()
    {
        var connector = this.Q("connector");
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
        else
        {
            Debug.LogError($"⚠️ 控制流端口 '{portName}' 的connector元素未找到");
        }
    }
}

