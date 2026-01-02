using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 自定义的可调整大小的分组，提供更好的视觉反馈
/// </summary>
public class CustomGroup : Group
{
    // 静态缓存样式表，只加载一次
    private static StyleSheet s_StyleSheet;
    
    // 关联的数据对象，用于序列化
    public GroupData Data { get; private set; }
    
    static CustomGroup()
    {
        // 静态构造函数，只会执行一次
        s_StyleSheet = Resources.Load<StyleSheet>("ShizukuGraphView");
    }
    
    public CustomGroup() : this(new GroupData())
    {
    }
    
    public CustomGroup(GroupData data)
    {
        Data = data;
        
        // 添加自定义样式类
        AddToClassList("custom-group");
        
        // 确保具有可调整大小的能力
        capabilities |= Capabilities.Resizable;
        
        // 设置自动更新几何形状
        autoUpdateGeometry = true;
        
        // 应用已加载的样式表（只是引用，不会重复加载）
        if (s_StyleSheet != null && !styleSheets.Contains(s_StyleSheet))
        {
            styleSheets.Add(s_StyleSheet);
        }
        
        // 设置标题
        title = data.Title;
        
    }
    
    // 更新数据对象的位置和大小
    public void UpdateData()
    {
        if (Data != null)
        {
            var pos = GetPosition();
            Data.PositionAndSize = new Unity.Mathematics.float4(pos.x, pos.y, pos.width, pos.height);
            Data.Title = title;
        }
    }
}

