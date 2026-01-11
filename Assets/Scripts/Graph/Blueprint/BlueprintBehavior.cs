
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 蓝图行为基类
/// </summary>
/// <remarks>
/// 使用流程：
/// 1. 定义行为类：public class EnemyBehavior : BlueprintBehavior { }
/// 2. 右键菜单"Generate Blueprint" → 自动生成 EnemyBlueprint : ShizukuBluePrint&lt;EnemyBehavior&gt;
/// 3. 在Inspector中将蓝图赋值给 _blueprint 字段
/// 
/// 关键设计：
/// - Blueprint字段类型为ShizukuGraphBase（基类），BlueprintBehavior不需要知道蓝图的具体类型
/// - 蓝图会在Init时主动查找并绑定到持有它的Behavior实例
/// - 生成的蓝图类使用泛型ShizukuBluePrint&lt;T&gt;，保持类型安全
/// - 支持事件系统：允许蓝图"重写"Behavior中的虚拟方法
/// - 支持属性访问：允许蓝图读写Behavior中的protected字段
/// </remarks>
public abstract class BlueprintBehavior : MonoBehaviour
{
    /// <summary>
    /// 蓝图引用（使用基类类型，避免编译时依赖）
    /// 赋值的时候需要检测泛型类型
    /// </summary>
    [SerializeField]
    private ShizukuGraphBase _blueprint;
    
    /// <summary>
    /// 获取蓝图实例
    /// </summary>
    public ShizukuGraphBase Blueprint => _blueprint;

    protected virtual Type GraphType => typeof(ShizukuBluePrint<>);

    #region 蓝图事件系统
    
    /// <summary>
    /// 蓝图事件处理器字典
    /// key: 事件名称, value: 事件处理器
    /// </summary>
    private Dictionary<string, Action<object[]>> _blueprintEvents = new Dictionary<string, Action<object[]>>();
    
    /// <summary>
    /// 蓝图属性获取器字典
    /// key: 属性名称, value: 获取器函数
    /// </summary>
    private Dictionary<string, Func<object>> _propertyGetters = new Dictionary<string, Func<object>>();
    
    /// <summary>
    /// 蓝图属性设置器字典
    /// key: 属性名称, value: 设置器函数
    /// </summary>
    private Dictionary<string, Action<object>> _propertySetters = new Dictionary<string, Action<object>>();
    
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
    protected void ExecuteBlueprintEvent(string eventName, params object[] args)
    {
        if (_blueprintEvents != null && _blueprintEvents.TryGetValue(eventName, out var handler))
        {
            handler?.Invoke(args);
        }
    }
    
    /// <summary>
    /// 检查蓝图是否实现了某个事件
    /// 可用于判断是执行蓝图逻辑还是默认C#逻辑
    /// </summary>
    protected bool IsBlueprintEventImplemented(string eventName)
    {
        return _blueprintEvents != null && _blueprintEvents.ContainsKey(eventName);
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
            _blueprint.Init();
        }
    }

    private void Update()
    {
        // 每帧更新蓝图图表
        if (_blueprint != null)
        {
            _blueprint.Update();
        }
    }
}