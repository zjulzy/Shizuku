
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 蓝图行为基类
/// </summary>
/// <remarks>
/// 使用流程：
/// 1. 定义行为类：public class EnemyBehavior : BlueprintBehavior { }
/// 2. 右键菜单"Generate Blueprint" → 自动生成 EnemyBlueprint : ShizukuBluePrint EnemyBehavior；
/// 3. 在Inspector中将蓝图赋值给 _blueprint 字段
/// 
/// 关键设计：
/// - Blueprint字段类型为ShizukuGraphBase（基类），BlueprintBehavior不需要知道蓝图的具体类型
/// - 蓝图会在Init时主动查找并绑定到持有它的Behavior实例
/// - 生成的蓝图类使用泛型ShizukuBluePrint&lt;T&gt;，保持类型安全
/// - 支持事件系统：允许蓝图"重写"Behavior中的虚拟方法
/// - 支持字段访问：允许蓝图读写Behavior中的public/protected字段
/// </remarks>
public abstract class BlueprintBehavior<T> : MonoBehaviour where T : BlueprintBehavior<T>
{
    [SerializeField]
    private ShizukuBluePrint<T> _blueprint;
    
    public ShizukuBluePrint<T> Blueprint => _blueprint;

    #region 蓝图事件系统
    
    private Dictionary<string, Action<object[]>> _blueprintEvents = new Dictionary<string, Action<object[]>>();
    
    private Dictionary<string, Func<object>> _propertyGetters = new Dictionary<string, Func<object>>();

    private Dictionary<string, Action<object>> _propertySetters = new Dictionary<string, Action<object>>();
    
    public void RegisterBlueprintEvent(string eventName, Action<object[]> handler)
    {
        _blueprintEvents[eventName] = handler;
    }
    
    public void UnregisterBlueprintEvent(string eventName)
    {
        _blueprintEvents.Remove(eventName);
    }
    
    /// <summary>
    /// 执行蓝图事件
    /// 在Behavior的方法中调用，如果蓝图实现了该事件，则执行蓝图逻辑
    /// </summary>
    protected bool ExecuteBlueprintEvent(string eventName, params object[] args)
    {
        if (_blueprintEvents != null && _blueprintEvents.TryGetValue(eventName, out var handler))
        {
            handler?.Invoke(args);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 检查蓝图是否实现了某个事件
    /// </summary>
    protected bool IsBlueprintEventImplemented(string eventName)
    {
        return _blueprintEvents != null && _blueprintEvents.ContainsKey(eventName);
    }
    
    /// <param name="methodName">方法名（使用 nameof(方法名)）</param>
    /// <param name="args">方法参数</param>
    /// <returns>蓝图是否实现了该方法</returns>
    protected bool TryExecuteBlueprintOverride(string methodName, params object[] args)
    {
        if(!IsBlueprintEventImplemented(methodName))
        {
            return false;
        }
        ExecuteBlueprintEvent(methodName, args);
        return true;
    }
    
    /// <summary>
    /// 注册属性获取器（由蓝图调用）
    /// </summary>
    public void RegisterPropertyGetter(string propertyName, Func<object> getter)
    {
        _propertyGetters[propertyName] = getter;
    }
    
    /// <summary>
    /// 注册属性设置器（由蓝图调用）
    /// </summary>
    public void RegisterPropertySetter(string propertyName, Action<object> setter)
    {
        _propertySetters[propertyName] = setter;
    }
    
    /// <summary>
    /// 获取属性值（供蓝图节点调用）
    /// </summary>
    public bool TryGetBlueprintProperty(string propertyName, out object value)
    {
        value = null;
        if (_propertyGetters != null && _propertyGetters.TryGetValue(propertyName, out var getter))
        {
            value = getter?.Invoke();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 设置属性值（供蓝图节点调用）
    /// </summary>
    public bool TrySetBlueprintProperty(string propertyName, object value)
    {
        if (_propertySetters != null && _propertySetters.TryGetValue(propertyName, out var setter))
        {
            setter?.Invoke(value);
            return true;
        }
        return false;
    }
    
    #endregion

    protected virtual void Start()
    {
        // 初始化蓝图（蓝图会主动绑定到this）
        if (_blueprint != null)
        {
            // 强制转换：this 在运行时实际上是 T 类型（如 EnemyBehavior）
            _blueprint.InitializeBehavior((T)this);
        }
    }

    private void Update()
    {
        // 每帧更新蓝图图表（执行 Root Node）
        // 设计理念：Root Node 可以包含每帧执行的逻辑
        // 事件驱动的逻辑使用 BlueprintEventNode
        if (_blueprint != null)
        {
            _blueprint.Update();
        }
    }

    protected virtual void OnDestroy()
    {
        // 清理所有事件和属性访问器，防止内存泄漏
        _blueprintEvents?.Clear();
        _propertyGetters?.Clear();
        _propertySetters?.Clear();
    }
}