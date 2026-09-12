using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public abstract class ShizukuNodeBase
    {
        [SerializeField]
        public string GUID = System.Guid.NewGuid().ToString();

        [SerializeField]
        public float4 PositionAndSize;

        public virtual string Title => GetDefaultTitle(GetType());
        public virtual Color TitleBarColor => Color.gray;

        public virtual bool SupportControlInput => true;
        public virtual bool SupportControlOutput => true;

        [NonSerialized]
        protected INodeContext _context;

        /// <summary>
        /// 便捷访问根图（变量、函数等全局资源）
        /// </summary>
        protected ShizukuGraphBase RootGraph => _context?.RootGraph;

        /// <summary>
        /// 当前运行时图实例的宿主。需要创建持续性运行时对象的节点应将对象挂在该宿主下，
        /// 并在 <see cref="DisposeRuntime"/> 中主动释放。
        /// </summary>
        protected GameObject RuntimeOwner => _context?.RuntimeOwner;

        [NonSerialized]
        public readonly List<ParameterEdgePort> SelfOutputPorts = new List<ParameterEdgePort>();

        [NonSerialized]
        public readonly List<ParameterEdgePort> SelfInputPorts = new List<ParameterEdgePort>();

        [NonSerialized]
        public readonly List<ShizukuNodeBase> DependentNodes = new List<ShizukuNodeBase>();

        // ---- 反射字段缓存（按 Type 共享，避免每次 Init 重复反射） ----
        private static readonly Dictionary<Type, FieldInfo[]> s_paramPortFieldCache = new();
        private static readonly Dictionary<Type, string> s_defaultTitleCache = new();

        private static string GetDefaultTitle(Type type)
        {
            if (s_defaultTitleCache.TryGetValue(type, out var cached)) return cached;

            var menuItem = type.GetCustomAttribute<NodeMenuItemAttribute>();
            var title = menuItem?.DisplayName;
            if (string.IsNullOrWhiteSpace(title))
                title = type.Name;

            s_defaultTitleCache[type] = title;
            return title;
        }

        private static FieldInfo[] GetCachedParamPortFields(Type type)
        {
            if (s_paramPortFieldCache.TryGetValue(type, out var cached)) return cached;
            var list = new List<FieldInfo>();
            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(ParameterEdgePort).IsAssignableFrom(f.FieldType))
                    list.Add(f);
            }
            cached = list.ToArray();
            s_paramPortFieldCache[type] = cached;
            return cached;
        }

        public virtual void Init(INodeContext context)
        {
            _context = context;

            DependentNodes.Clear();
            SelfOutputPorts.Clear();
            SelfInputPorts.Clear();

            if (this is IDynamicParameterPortProvider dynamicPortProvider)
                dynamicPortProvider.SynchronizeDynamicParameterPorts(context);

            var fields = GetCachedParamPortFields(GetType());

            for (int i = 0; i < fields.Length; i++)
            {
                var port = fields[i].GetValue(this) as ParameterEdgePort;
                RegisterParameterPort(port);
            }

            if (this is IDynamicParameterPortProvider provider)
            {
                var descriptors = provider.DynamicParameterPorts;
                if (descriptors != null)
                {
                    foreach (var descriptor in descriptors)
                        RegisterParameterPort(descriptor.Port);
                }
            }
        }

        private void RegisterParameterPort(ParameterEdgePort port)
        {
            if (port == null)
                return;

            port.SameTypeConnectedPort = null;
            port.DifferentTypeConnectedPort = null;

            var ports = port.IsOut ? SelfOutputPorts : SelfInputPorts;
            if (!ports.Contains(port))
                ports.Add(port);
        }

        /// <summary>
        /// 释放节点在本次运行时图实例中创建的状态或 Unity 对象。
        /// 实现必须可重复调用；派生类清理完成后应调用 base。
        /// </summary>
        public virtual void DisposeRuntime()
        {
            foreach (var port in SelfInputPorts)
            {
                port.SameTypeConnectedPort = null;
                port.DifferentTypeConnectedPort = null;
            }

            foreach (var port in SelfOutputPorts)
            {
                port.SameTypeConnectedPort = null;
                port.DifferentTypeConnectedPort = null;
            }

            DependentNodes.Clear();
            SelfInputPorts.Clear();
            SelfOutputPorts.Clear();
            _context = null;
        }

        protected void GetInputValues()
        {
            foreach (var node in DependentNodes)
            {
                node.GetOutputValues();
            }

            foreach (var port in SelfInputPorts)
            {
                port.GetSourceValue();
            }
        }

        public virtual void GetOutputValues(){}
    }
}
