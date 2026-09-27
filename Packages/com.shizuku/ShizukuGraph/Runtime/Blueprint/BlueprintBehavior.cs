using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace Shizuku.Graph
{
    using Shizuku.Core;
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

        [NonSerialized]
        private ShizukuGraphRuntime<ShizukuBluePrint<T>> _runtimeBlueprint;

        private bool _started;
        private bool _destroyed;

        public ShizukuBluePrint<T> Blueprint => _runtimeBlueprint?.Instance != null
            ? _runtimeBlueprint.Instance
            : _blueprint;

        #region 蓝图事件系统

        /// <summary>
        /// 蓝图事件处理器（统一使用 Func，无返回值时返回 null）
        /// </summary>
        private Dictionary<string, Func<object[], object>> _blueprintEvents = new Dictionary<string, Func<object[], object>>();

        private Dictionary<string, Func<object>> _propertyGetters = new Dictionary<string, Func<object>>();

        private Dictionary<string, Action<object>> _propertySetters = new Dictionary<string, Action<object>>();

        public void RegisterBlueprintEvent(string eventName, Func<object[], object> handler)
        {
            _blueprintEvents[eventName] = handler;
        }

        public void UnregisterBlueprintEvent(string eventName)
        {
            _blueprintEvents.Remove(eventName);
        }

        /// <summary>
        /// 检查蓝图是否实现了某个事件
        /// </summary>
        protected bool IsBlueprintEventImplemented(string eventName)
        {
            return _blueprintEvents != null && _blueprintEvents.ContainsKey(eventName);
        }

        /// <summary>
        /// 尝试执行蓝图覆写（无返回值版本）
        /// </summary>
        /// <param name="methodName">方法名（使用 nameof(方法名)）</param>
        /// <param name="args">方法参数</param>
        /// <returns>蓝图是否实现了该方法</returns>
        protected bool TryExecuteBlueprintOverride(string methodName, params object[] args)
        {
            if (_blueprintEvents != null && _blueprintEvents.TryGetValue(methodName, out var handler))
            {
                InvokeBlueprintHandler(handler, args);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试执行蓝图覆写（带返回值版本）
        /// 用法：if (TryExecuteBlueprintOverride&lt;float&gt;(nameof(CalcDamage), out var result, baseDmg)) return result;
        /// </summary>
        protected bool TryExecuteBlueprintOverride<TReturn>(string methodName, out TReturn result, params object[] args)
        {
            result = default;
            if (_blueprintEvents != null && _blueprintEvents.TryGetValue(methodName, out var handler))
            {
                var rawResult = InvokeBlueprintHandler(handler, args);
                if (rawResult is TReturn typed)
                {
                    result = typed;
                }
                return true;
            }
            return false;
        }

        private object InvokeBlueprintHandler(Func<object[], object> handler, object[] args)
        {
            if (_runtimeBlueprint != null)
                return _runtimeBlueprint.Invoke(_ => handler(args));

            // 保留直接调用 InitializeBehavior 的兼容路径（主要用于自定义启动流程与测试）。
            using (ShizukuExecutionContext.Begin(Blueprint, gameObject, typeof(T).Name))
            {
                return handler(args);
            }
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
            if (_started || _destroyed)
                return;
            _started = true;

            // 初始化蓝图（蓝图会主动绑定到this）
            if (_blueprint != null)
            {
                try
                {
                    _runtimeBlueprint = ShizukuGraphRuntime<ShizukuBluePrint<T>>.Create(
                        _blueprint,
                        gameObject,
                        initializeRuntime: graph => graph.InitializeBehavior((T)this),
                        executionContextName: typeof(T).Name);
                }
                catch
                {
                    _blueprintEvents?.Clear();
                    _propertyGetters?.Clear();
                    _propertySetters?.Clear();
                    throw;
                }
            }
            OnStart();
        }

        /// <summary>初始化和事件绑定完成后触发一次，允许启动 Latent 链。</summary>
        [BlueprintOverridable]
        protected virtual void OnStart()
        {
            TryExecuteBlueprintOverride(nameof(OnStart));
        }

        /// <summary>每帧同步派发；不负责推进 Root 或 Latent。</summary>
        [BlueprintOverridable(AllowLatent = false)]
        protected virtual void OnUpdate(float deltaTime)
        {
            TryExecuteBlueprintOverride(nameof(OnUpdate), deltaTime);
        }

        // 使用独立方法映射蓝图事件，绝不反射调用 Unity OnDestroy 回调。
        [BlueprintOverridable("OnDestroy", AllowLatent = false)]
        protected void DispatchBlueprintDestroyEvent()
        {
            TryExecuteBlueprintOverride("OnDestroy");
        }

        private void Update()
        {
            if (!_started || _destroyed)
                return;
            try
            {
                using (Blueprint?.DisallowLatentExecution("Blueprint OnUpdate 只允许同步执行"))
                    OnUpdate(Time.deltaTime);
            }
            finally
            {
                // 即使同步事件抛异常，仍保留原有每帧一次的 Root / Latent 推进。
                if (!_destroyed)
                    _runtimeBlueprint?.Tick();
            }
        }

        protected virtual void OnDestroy()
        {
            if (_destroyed)
                return;
            _destroyed = true;
            try
            {
                using (Blueprint?.DisallowLatentExecution("Blueprint OnDestroy 只允许同步执行"))
                    DispatchBlueprintDestroyEvent();
            }
            finally
            {
                _blueprintEvents?.Clear();
                _propertyGetters?.Clear();
                _propertySetters?.Clear();
                var runtime = _runtimeBlueprint;
                _runtimeBlueprint = null;
                runtime?.Dispose();
            }
        }
    }
}
