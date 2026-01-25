using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 蓝图基类（泛型版本，用于代码生成和类型安全）
/// T: 对应的BlueprintBehavior类型
/// </summary>
/// <remarks>
/// 使用流程：
/// 1. 先定义 EnemyBehavior : BlueprintBehavior（不会有编译错误）
/// 2. 右键菜单"Generate Blueprint" → 自动生成 EnemyBlueprint : ShizukuBluePrint EnemyBehavior；
/// 3. 生成器通过反射获取 EnemyBehavior 的成员，生成强类型的初始化代码
/// 
/// 新增功能：
/// - 事件系统：支持蓝图重写Behavior的虚拟方法
/// - 属性访问：支持蓝图读写Behavior的protected字段
/// </remarks>
public abstract class ShizukuBluePrint<T> : ShizukuGraphBase where T : BlueprintBehavior<T>
{
    /// <summary>
    /// 当前绑定的Behavior实例
    /// </summary>
    [NonSerialized]
    private T _behavior;
    
    /// <summary>
    /// 蓝图事件节点字典
    /// key: 事件名称, value: 对应的事件节点
    /// </summary>
    [NonSerialized]
    private readonly Dictionary<string, BlueprintEventNode> _eventNodes = new Dictionary<string, BlueprintEventNode>();
    
    /// <summary>
    /// 静态属性访问器缓存（所有同类型实例共享）
    /// 避免每次 InitializeBehavior 都反射
    /// </summary>
    private static Dictionary<string, Func<T, object>> _cachedGetters;
    private static Dictionary<string, Action<T, object>> _cachedSetters;
    private static bool _accessorsCached = false;
    
    
    /// <summary>
    /// 初始化Behavior（由BlueprintBehavior的Start调用）
    /// 子类可以重写此方法来自定义初始化逻辑
    /// </summary>
    public virtual void InitializeBehavior(T behavior)
    {
        Init();
        _behavior = behavior;
        
        // 注册蓝图事件
        RegisterBlueprintEvents(behavior);
        
        // 注册属性访问器
        RegisterPropertyAccessors(behavior);
    }
    
    /// <summary>
    /// 注册蓝图事件
    /// 扫描图中的所有EventNode，并注册到Behavior
    /// </summary>
    protected virtual void RegisterBlueprintEvents(T behavior)
    {
        _eventNodes.Clear();
        
        // 扫描所有节点，找到BlueprintEventNode
        foreach (var node in Nodes)
        {
            if (node is BlueprintEventNode eventNode)
            {
                _eventNodes[eventNode.EventName] = eventNode;
                
                // 注册事件处理器
                behavior.RegisterBlueprintEvent(eventNode.EventName, (args) =>
                {
                    eventNode.TriggerEvent(args);
                });
            }
        }
    }
    
    /// <summary>
    /// 注册属性访问器
    /// 通过反射为Behavior的protected字段生成访问器
    /// 使用静态缓存优化性能（首次反射，后续复用）
    /// </summary>
    protected virtual void RegisterPropertyAccessors(T behavior)
    {
        // 首次调用时构建缓存
        if (!_accessorsCached)
        {
            BuildAccessorCache();
            _accessorsCached = true;
        }
        
        // 使用缓存的访问器注册
        foreach (var kvp in _cachedGetters)
        {
            var getter = kvp.Value;
            behavior.RegisterPropertyGetter(kvp.Key, () => getter(behavior));
        }
        
        foreach (var kvp in _cachedSetters)
        {
            var setter = kvp.Value;
            behavior.RegisterPropertySetter(kvp.Key, (value) => setter(behavior, value));
        }
    }
    
    /// <summary>
    /// 构建属性访问器缓存（只在首次调用时执行）
    /// </summary>
    private static void BuildAccessorCache()
    {
        _cachedGetters = new Dictionary<string, Func<T, object>>();
        _cachedSetters = new Dictionary<string, Action<T, object>>();
        
        var behaviorType = typeof(T);
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        // 缓存字段访问器
        var fields = behaviorType.GetFields(flags);
        foreach (var field in fields)
        {
            // 跳过Unity的私有字段
            if (field.Name.StartsWith("m_")) continue;
            
            var fieldName = field.Name;
            var fieldCopy = field; // 避免闭包捕获循环变量
            
            // 缓存 Getter
            _cachedGetters[fieldName] = (b) => fieldCopy.GetValue(b);
            
            // 缓存 Setter
            _cachedSetters[fieldName] = (b, value) => fieldCopy.SetValue(b, value);
        }
        
        // 缓存属性访问器
        var properties = behaviorType.GetProperties(flags);
        foreach (var property in properties)
        {
            var propertyName = property.Name;
            var propertyCopy = property;
            
            // 缓存 Getter
            if (property.CanRead)
            {
                _cachedGetters[propertyName] = (b) => propertyCopy.GetValue(b);
            }
            
            // 缓存 Setter
            if (property.CanWrite)
            {
                _cachedSetters[propertyName] = (b, value) => propertyCopy.SetValue(b, value);
            }
        }
    }
    
    /// <summary>
    /// 获取当前绑定的Behavior实例
    /// </summary>
    public T GetBehavior() => _behavior;
    
    /// <summary>
    /// 触发蓝图事件（供外部调用）
    /// </summary>
    public void TriggerBlueprintEvent(string eventName, params object[] args)
    {
        if (_eventNodes.TryGetValue(eventName, out var eventNode))
        {
            eventNode.TriggerEvent(args);
        }
    }
}

