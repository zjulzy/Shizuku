using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
/// - 字段访问：支持蓝图读写Behavior的public/protected字段
/// </remarks>
public abstract class ShizukuBluePrint : ShizukuGraphBase
{
    public abstract bool TryGetProperty(string propertyName, out object value);
    public abstract bool TrySetProperty(string propertyName, object value);
}

public abstract class ShizukuBluePrint<T> : ShizukuBluePrint where T : BlueprintBehavior<T>
{
    private T _behavior;

    private readonly Dictionary<string, BlueprintEventNode> _eventNodes = new Dictionary<string, BlueprintEventNode>();

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

        // 注册字段访问器
        RegisterFieldAccessors(behavior);
    }

    #region 注册各种东西

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
    /// 注册字段访问器
    /// 通过反射为Behavior的public/protected字段生成访问器
    /// 使用静态缓存优化性能（首次反射，后续复用）
    /// </summary>
    protected virtual void RegisterFieldAccessors(T behavior)
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
    /// 构建字段访问器缓存（只在首次调用时执行）
    /// 基于 Expression Tree，编译后比反射快约 50 倍
    /// 只注册用户在 Behavior 子类中显式声明的 public/protected 字段，
    /// 通过 DeclaredOnly 过滤掉 MonoBehaviour / Component / BlueprintBehavior 等基类的成员。
    /// </summary>
    private static void BuildAccessorCache()
    {
        _cachedGetters = new Dictionary<string, Func<T, object>>();
        _cachedSetters = new Dictionary<string, Action<T, object>>();

        var behaviorType = typeof(T);
        // 只收集声明在 T 自身的成员（DeclaredOnly 排除所有继承成员）
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        foreach (var field in behaviorType.GetFields(flags))
        {
            // 只暴露 public 和 protected 字段
            if (!field.IsPublic && !field.IsFamily) continue;
            // 跳过编译器生成的 backing field 和 Unity 内部字段
            if (field.Name.StartsWith("<") || field.Name.StartsWith("m_")) continue;

            var fieldName = field.Name;

            // === Expression Tree 生成 Getter ===
            var paramGet = Expression.Parameter(typeof(T), "b");
            var fieldAccess = Expression.Field(paramGet, field);
            var convertGet = Expression.Convert(fieldAccess, typeof(object));
            var lambdaGet = Expression.Lambda<Func<T, object>>(convertGet, paramGet);
            _cachedGetters[fieldName] = lambdaGet.Compile();

            // === Expression Tree 生成 Setter ===
            if (!field.IsInitOnly)
            {
                var paramSet = Expression.Parameter(typeof(T), "b");
                var paramValue = Expression.Parameter(typeof(object), "value");
                var convertValue = Expression.Convert(paramValue, field.FieldType);
                var assignExpr = Expression.Assign(
                    Expression.Field(paramSet, field),
                    convertValue
                );
                var lambdaSet = Expression.Lambda<Action<T, object>>(
                    assignExpr, paramSet, paramValue
                );
                _cachedSetters[fieldName] = lambdaSet.Compile();
            }
        }
    }

    #endregion

    #region Debug 快照支持

    /// <summary>
    /// 重写快照：额外捕获 Behavior 上所有 public/protected 字段的当前值
    /// </summary>
    public override DebugSnapshot CaptureSnapshot(string pausedAtNodeGuid)
    {
        var snapshot = base.CaptureSnapshot(pausedAtNodeGuid);
        
        if (_behavior != null && _cachedGetters != null)
        {
            var props = new Dictionary<string, object>();
            foreach (var kvp in _cachedGetters)
            {
                try
                {
                    props[kvp.Key] = kvp.Value(_behavior);
                }
                catch
                {
                    props[kvp.Key] = "<error>";
                }
            }
            snapshot.BehaviorFields = props;
        }
        
        return snapshot;
    }

    /// <summary>
    /// 重写还原：额外把快照中的 Behavior 字段写回。
    /// 只涉及 public/protected 字段，无属性 setter 的副作用风险。
    /// </summary>
    protected override void RestoreVariablesFromSnapshot()
    {
        base.RestoreVariablesFromSnapshot();
        
        var snapshot = ShizukuDebugger.CurrentSnapshot;
        if (snapshot?.BehaviorFields == null || _behavior == null || _cachedSetters == null)
            return;
        
        foreach (var kvp in snapshot.BehaviorFields)
        {
            if (_cachedSetters.TryGetValue(kvp.Key, out var setter))
            {
                try
                {
                    setter(_behavior, kvp.Value);
                }
                catch
                {
                    // readonly 字段或类型不匹配，静默跳过
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// 获取属性值（由蓝图节点调用）
    /// </summary>
    public override bool TryGetProperty(string propertyName, out object value)
    {
        value = null;
        if (_behavior == null)
            return false;

        return _behavior.TryGetBlueprintProperty(propertyName, out value);
    }

    /// <summary>
    /// 设置属性值（由蓝图节点调用）
    /// </summary>
    public override bool TrySetProperty(string propertyName, object value)
    {
        if (_behavior == null)
            return false;

        return _behavior.TrySetBlueprintProperty(propertyName, value);
    }
}

