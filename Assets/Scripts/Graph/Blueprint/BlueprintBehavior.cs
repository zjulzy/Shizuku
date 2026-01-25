
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
/// - 支持属性访问：允许蓝图读写Behavior中的protected字段
/// </remarks>
public abstract class BlueprintBehavior<T> : MonoBehaviour where T : BlueprintBehavior<T>
{
    /// <summary>
    /// 蓝图引用（使用基类类型，避免编译时依赖）
    /// 赋值的时候需要检测泛型类型
    /// </summary>
    // TODO：严格限制蓝图类型
    [SerializeField]
    private ShizukuBluePrint<T> _blueprint;
    
    /// <summary>
    /// 获取蓝图实例
    /// </summary>
    public ShizukuGraphBase Blueprint => _blueprint;

    protected virtual Type GraphType => typeof(ShizukuBluePrint<>);

    #region 蓝图事件系统
    
    /// <summary>
    /// 蓝图事件处理器字典
    /// </summary>
    private Dictionary<string, Action<object[]>> _blueprintEvents = new Dictionary<string, Action<object[]>>();
    
    /// <summary>
    /// 蓝图属性获取器字典
    /// </summary>
    private Dictionary<string, Func<object>> _propertyGetters = new Dictionary<string, Func<object>>();
    
    /// <summary>
    /// 蓝图属性设置器字典
    /// </summary>
    private Dictionary<string, Action<object>> _propertySetters = new Dictionary<string, Action<object>>();
    
    /// <summary>
    /// 可重写方法缓存字典（避免重复反射）
    /// key: 方法名, value: 对应的蓝图事件名（null 表示该方法不可重写）
    /// </summary>
    private Dictionary<string, string> _overridableMethodCache = new Dictionary<string, string>();
    
    /// <summary>
    /// 注册蓝图事件
    /// </summary>
    public void RegisterBlueprintEvent(string eventName, Action<object[]> handler)
    {
        _blueprintEvents[eventName] = handler;
    }
    
    /// <summary>
    /// 注销蓝图事件
    /// </summary>
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
    
    /// <summary>
    /// 尝试执行标记了 [BlueprintOverridable] 的方法的蓝图重写版本
    /// 如果蓝图实现了对应事件，返回 true；否则返回 false，继续执行 C# 逻辑
    /// 使用缓存机制避免重复反射，第一次调用会有反射开销，后续调用性能接近字典查询
    /// </summary>
    /// <param name="methodName">方法名（使用 nameof(方法名)）</param>
    /// <param name="args">方法参数</param>
    /// <returns>蓝图是否实现了该方法</returns>
    protected bool TryExecuteBlueprintOverride(string methodName, params object[] args)
    {
        // 先查缓存
        if (!_overridableMethodCache.TryGetValue(methodName, out var eventName))
        {
            // 缓存未命中，执行反射检查（只在第一次调用时发生）
            var method = GetType().GetMethod(methodName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method == null)
            {
                Debug.LogWarning($"Method '{methodName}' not found on {GetType().Name}");
                _overridableMethodCache[methodName] = null; // 缓存失败结果
                return false;
            }
            
            // 检查是否有 [BlueprintOverridable] 标记
            var attr = method.GetCustomAttribute<BlueprintOverridableAttribute>();
            if (attr == null)
            {
                Debug.LogWarning($"Method '{methodName}' is not marked with [BlueprintOverridable]");
                _overridableMethodCache[methodName] = null; // 缓存失败结果
                return false;
            }
            
            // 获取事件名并缓存
            eventName = string.IsNullOrEmpty(attr.EventName) ? methodName : attr.EventName;
            _overridableMethodCache[methodName] = eventName;
        }
        
        // 如果缓存的结果是 null，说明之前检查失败过
        if (eventName == null)
            return false;
        
        // 尝试执行蓝图事件
        return ExecuteBlueprintEvent(eventName, args);
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
    public object GetBlueprintProperty(string propertyName)
    {
        if (_propertyGetters != null && _propertyGetters.TryGetValue(propertyName, out var getter))
        {
            return getter?.Invoke();
        }
        return null;
    }
    
    /// <summary>
    /// 设置属性值（供蓝图节点调用）
    /// </summary>
    public void SetBlueprintProperty(string propertyName, object value)
    {
        if (_propertySetters != null && _propertySetters.TryGetValue(propertyName, out var setter))
        {
            setter?.Invoke(value);
        }
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
        _overridableMethodCache?.Clear();
    }
}