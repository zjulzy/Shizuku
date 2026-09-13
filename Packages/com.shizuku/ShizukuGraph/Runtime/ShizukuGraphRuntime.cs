using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shizuku.Graph
{
    /// <summary>
    /// 一个运行时图克隆的唯一所有者。
    /// 统一管理克隆、初始化、执行上下文、逐帧推进和销毁，避免各运行入口重复维护生命周期。
    /// </summary>
    public sealed class ShizukuGraphRuntime<TGraph> : IDisposable
        where TGraph : ShizukuGraphBase
    {
        private TGraph _instance;
        private readonly string _executionContextName;

        private ShizukuGraphRuntime(
            TGraph sourceAsset,
            TGraph instance,
            GameObject owner,
            string executionContextName)
        {
            SourceAsset = sourceAsset;
            _instance = instance;
            Owner = owner;
            _executionContextName = executionContextName;
        }

        /// <summary>用于创建该运行时克隆的源资产。句柄不会修改或销毁它。</summary>
        public TGraph SourceAsset { get; }

        /// <summary>当前运行时克隆；句柄释放后为 null。</summary>
        public TGraph Instance => _instance;

        /// <summary>运行时图的宿主对象，可为空。</summary>
        public GameObject Owner { get; }

        public bool IsDisposed => _instance == null;

        /// <summary>
        /// 创建并初始化运行时克隆。
        /// configureBeforeInitialize 可用于写入初始化所需上下文；initializeRuntime 为空时调用 Init(owner)；
        /// afterInitialize 用于应用依赖已初始化变量/节点的数据。
        /// 自定义初始化委托适用于需要保留虚方法扩展点的图类型，例如 Blueprint 的 InitializeBehavior。
        /// 任一阶段抛出异常时都会释放并销毁已创建的克隆。
        /// </summary>
        public static ShizukuGraphRuntime<TGraph> Create(
            TGraph sourceAsset,
            GameObject owner = null,
            Action<TGraph> configureBeforeInitialize = null,
            Action<TGraph> initializeRuntime = null,
            Action<TGraph> afterInitialize = null,
            string executionContextName = null)
        {
            if (sourceAsset == null)
                throw new ArgumentNullException(nameof(sourceAsset));

            var instance = Object.Instantiate(sourceAsset);
            instance.name = $"{sourceAsset.name}_Runtime_{instance.GetInstanceID()}";
            var runtime = new ShizukuGraphRuntime<TGraph>(
                sourceAsset,
                instance,
                owner,
                executionContextName);

            try
            {
                configureBeforeInitialize?.Invoke(instance);
                if (initializeRuntime != null)
                    initializeRuntime(instance);
                else
                    instance.Init(owner);

                afterInitialize?.Invoke(instance);

                return runtime;
            }
            catch
            {
                runtime.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 推进一帧。即使 executeRoot 为 false，已经启动的 Latent 节点也会继续推进。
        /// </summary>
        public void Tick(bool executeRoot = true)
        {
            var instance = GetRequiredInstance();
            using (ShizukuExecutionContext.Begin(instance, Owner, _executionContextName))
            {
                instance.AdvanceRuntime(executeRoot);
            }
        }

        /// <summary>在不推进 Latent 的情况下尝试执行一次 Root。</summary>
        public ExecuteResult ExecuteRootOnce()
        {
            var instance = GetRequiredInstance();
            using (ShizukuExecutionContext.Begin(instance, Owner, _executionContextName))
            {
                return instance.ExecuteRootOnce();
            }
        }

        /// <summary>在该运行时图的结构化执行上下文中调用自定义逻辑。</summary>
        public void Invoke(Action<TGraph> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var instance = GetRequiredInstance();
            using (ShizukuExecutionContext.Begin(instance, Owner, _executionContextName))
            {
                action(instance);
            }
        }

        /// <summary>在该运行时图的结构化执行上下文中调用自定义逻辑并返回结果。</summary>
        public TResult Invoke<TResult>(Func<TGraph, TResult> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var instance = GetRequiredInstance();
            using (ShizukuExecutionContext.Begin(instance, Owner, _executionContextName))
            {
                return action(instance);
            }
        }

        public void Dispose()
        {
            var instance = _instance;
            if (instance == null)
                return;

            // 先断开句柄，保证重入 Dispose 时仍然幂等。
            _instance = null;
            try
            {
                instance.DisposeRuntime();
            }
            finally
            {
                if (Application.isPlaying)
                    Object.Destroy(instance);
                else
                    Object.DestroyImmediate(instance);
            }
        }

        private TGraph GetRequiredInstance()
        {
            if (_instance == null)
                throw new ObjectDisposedException(GetType().Name);

            return _instance;
        }
    }
}
