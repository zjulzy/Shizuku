using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// TODO: 基于模板的蓝图系统
// 初步构想是蓝图模板绑定BlueprintBehavior类型，就可以在具体的蓝图类中重写BlueprintBehavior中的虚方法
// 在新建一个BlueprintBehavior类型后，通过自动生成代码生成对应的蓝图类

/// <summary>
/// 蓝图基类（泛型版本，用于代码生成和类型安全）
/// T: 对应的BlueprintBehavior类型
/// </summary>
/// <remarks>
/// 使用流程：
/// 1. 先定义 EnemyBehavior : BlueprintBehavior（不会有编译错误）
/// 2. 右键菜单"Generate Blueprint" → 自动生成 EnemyBlueprint : ShizukuBluePrint&lt;EnemyBehavior&gt;
/// 3. 生成器通过反射获取 EnemyBehavior 的成员，生成强类型的初始化代码
/// 
/// 新增功能：
/// - 事件系统：支持蓝图重写Behavior的虚拟方法
/// - 属性访问：支持蓝图读写Behavior的protected字段
/// </remarks>
public abstract class ShizukuBluePrint<T> : ShizukuGraphBase where T : BlueprintBehavior
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
    private Dictionary<string, BlueprintEventNode> _eventNodes = new Dictionary<string, BlueprintEventNode>();
    
    /// <summary>
    /// 重写Init方法，在初始化时主动绑定到Behavior
    /// </summary>
    public override void Init()
    {
        // 先调用基类Init初始化节点和边
        base.Init();
        
        // 查找场景中持有此蓝图的Behavior
        FindAndBindBehavior();
    }
    
    /// <summary>
    /// 查找并绑定到持有此蓝图的Behavior实例
    /// </summary>
    private void FindAndBindBehavior()
    {
        // 在场景中查找所有T类型的Behavior
        var behaviors = UnityEngine.Object.FindObjectsOfType<T>();
        
        foreach (var behavior in behaviors)
        {
            // 检查这个Behavior是否持有当前蓝图
            if (behavior.Blueprint == this)
            {
                _behavior = behavior;
                InitializeBehavior(behavior);
                break;
            }
        }
    }
    
    /// <summary>
    /// 初始化Behavior（由BlueprintBehavior的Start调用）
    /// 子类可以重写此方法来自定义初始化逻辑
    /// </summary>
    public virtual void InitializeBehavior(T behavior)
    {
        _behavior = behavior;
        
        // 注册蓝图事件
        RegisterBlueprintEvents(behavior);
        
        // 注册属性访问器
        RegisterPropertyAccessors(behavior);
        
        // 使用扩展方法绑定所有节点
        this.BindAllNodes(behavior);
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
                
                // 将事件节点绑定到Behavior
                eventNode.BindToBehavior(behavior);
                
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
    /// 子类可以重写此方法来添加自定义访问器
    /// </summary>
    protected virtual void RegisterPropertyAccessors(T behavior)
    {
        var behaviorType = typeof(T);
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        // 获取所有字段
        var fields = behaviorType.GetFields(flags);
        
        foreach (var field in fields)
        {
            // 跳过Unity的私有字段
            if (field.Name.StartsWith("m_")) continue;
            
            var fieldName = field.Name;
            
            // 注册Getter
            behavior.RegisterPropertyGetter(fieldName, () => field.GetValue(behavior));
            
            // 注册Setter
            behavior.RegisterPropertySetter(fieldName, (value) => field.SetValue(behavior, value));
        }
        
        // 获取所有属性
        var properties = behaviorType.GetProperties(flags);
        
        foreach (var property in properties)
        {
            var propertyName = property.Name;
            
            // 注册Getter
            if (property.CanRead)
            {
                behavior.RegisterPropertyGetter(propertyName, () => property.GetValue(behavior));
            }
            
            // 注册Setter
            if (property.CanWrite)
            {
                behavior.RegisterPropertySetter(propertyName, (value) => property.SetValue(behavior, value));
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

