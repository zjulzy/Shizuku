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

        public virtual string Title => "No Title";
        public virtual Color TitleBarColor => Color.gray;

        public virtual bool SupportControlInput => true;
        public virtual bool SupportControlOutput => true;

        [NonSerialized]
        protected INodeContext _context;

        /// <summary>
        /// 便捷访问根图（变量、函数等全局资源）
        /// </summary>
        protected ShizukuGraphBase RootGraph => _context?.RootGraph;

        [NonSerialized]
        public readonly List<ParameterEdgePort> SelfOutputPorts = new List<ParameterEdgePort>();

        [NonSerialized]
        public readonly List<ParameterEdgePort> SelfInputPorts = new List<ParameterEdgePort>();

        [NonSerialized]
        public readonly List<ShizukuNodeBase> DependentNodes = new List<ShizukuNodeBase>();

        // ---- 反射字段缓存（按 Type 共享，避免每次 Init 重复反射） ----
        private static readonly Dictionary<Type, FieldInfo[]> s_paramPortFieldCache = new();

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
            var fields = GetCachedParamPortFields(GetType());

            SelfOutputPorts.Clear();
            SelfInputPorts.Clear();

            for (int i = 0; i < fields.Length; i++)
            {
                var port = fields[i].GetValue(this) as ParameterEdgePort;
                if (port == null) continue;
                if (port.IsOut) SelfOutputPorts.Add(port);
                else SelfInputPorts.Add(port);
            }
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
