using System.Reflection;
using UnityEngine;

/// <summary>
/// 蓝图基类的辅助扩展方法
/// 提供便捷的节点绑定功能
/// </summary>
public static class BlueprintExtensions
{
    /// <summary>
    /// 自动绑定所有属性和事件节点到Behavior
    /// 在ShizukuBluePrint的InitializeBehavior中调用
    /// </summary>
    public static void BindAllNodes<T>(this ShizukuBluePrint<T> blueprint, T behavior) 
        where T : BlueprintBehavior
    {
        if (blueprint == null || behavior == null) return;
        
        foreach (var node in blueprint.Nodes)
        {
            BindNode(node, behavior);
        }
    }
    
    /// <summary>
    /// 绑定单个节点到Behavior
    /// </summary>
    private static void BindNode(ShizukuNodeBase node, BlueprintBehavior behavior)
    {
        // 尝试查找并调用BindToBehavior方法
        var nodeType = node.GetType();
        var bindMethod = nodeType.GetMethod("BindToBehavior", 
            BindingFlags.Public | BindingFlags.Instance);
        
        if (bindMethod != null)
        {
            try
            {
                bindMethod.Invoke(node, new object[] { behavior });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to bind node {node.Title} to behavior: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 注册单个属性的访问器（手动版本）
    /// 用于性能敏感场景或需要自定义逻辑的情况
    /// </summary>
    public static void RegisterProperty<T, TValue>(
        this T behavior, 
        string propertyName,
        System.Func<TValue> getter,
        System.Action<TValue> setter = null) 
        where T : BlueprintBehavior
    {
        // 注册Getter
        behavior.RegisterPropertyGetter(propertyName, () => getter());
        
        // 注册Setter（如果提供）
        if (setter != null)
        {
            behavior.RegisterPropertySetter(propertyName, (value) => 
            {
                if (value is TValue typedValue)
                {
                    setter(typedValue);
                }
            });
        }
    }
    
    /// <summary>
    /// 注册只读属性
    /// </summary>
    public static void RegisterReadOnlyProperty<T, TValue>(
        this T behavior,
        string propertyName,
        System.Func<TValue> getter)
        where T : BlueprintBehavior
    {
        behavior.RegisterPropertyGetter(propertyName, () => getter());
    }
}

