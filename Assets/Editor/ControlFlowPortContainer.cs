using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 控制流端口容器 - 用于放置Previous和Next端口
/// </summary>
public class ControlFlowPortContainer : VisualElement
{
    private static StyleSheet s_StyleSheet;
    
    static ControlFlowPortContainer()
    {
        s_StyleSheet = Resources.Load<StyleSheet>("ControlFlowPortContainer");
        if (s_StyleSheet != null)
        {
            Debug.Log("✅ ControlFlowPortContainer样式表加载成功");
        }
        else
        {
            Debug.LogWarning("⚠️ ControlFlowPortContainer样式表未找到");
        }
    }
    
    private VisualElement leftContainer;
    private VisualElement rightContainer;
    
    public ControlFlowPortContainer()
    {
        // 加载样式表
        if (s_StyleSheet != null && !styleSheets.Contains(s_StyleSheet))
        {
            styleSheets.Add(s_StyleSheet);
        }
        
        // 设置容器样式
        name = "control-flow-container";
        AddToClassList("control-flow-container");
        
        // 创建左侧容器（用于Previous端口）
        leftContainer = new VisualElement();
        leftContainer.name = "left-container";
        leftContainer.AddToClassList("port-side-container");
        Add(leftContainer);
        
        // 创建右侧容器（用于Next端口）
        rightContainer = new VisualElement();
        rightContainer.name = "right-container";
        rightContainer.AddToClassList("port-side-container");
        rightContainer.AddToClassList("port-right-container");
        Add(rightContainer);
    }
    
    /// <summary>
    /// 添加Previous端口到左侧容器
    /// </summary>
    public void AddPreviousPort(Port port)
    {
        leftContainer.Add(port);
    }
    
    /// <summary>
    /// 添加Next端口到右侧容器
    /// </summary>
    public void AddNextPort(Port port)
    {
        rightContainer.Add(port);
    }
}

