using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    public abstract class ShizukuNormalNode : ShizukuNodeBase
    {
        // 用于editor下，向后的控制port
        [NonSerialized]
        public Dictionary<string, ChainPort> ChainPorts = new Dictionary<string, ChainPort>();

        // ---- 反射字段缓存（按 Type 共享） ----
        private static readonly Dictionary<Type, FieldInfo[]> s_chainPortFieldCache = new();

        private static FieldInfo[] GetCachedChainPortFields(Type nodeType)
        {
            if (s_chainPortFieldCache.TryGetValue(nodeType, out var cached)) return cached;
            var list = new List<FieldInfo>();
            var t = nodeType;
            while (t != null && t != typeof(object))
            {
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (typeof(ChainPort).IsAssignableFrom(f.FieldType))
                        list.Add(f);
                }
                t = t.BaseType;
            }
            cached = list.ToArray();
            s_chainPortFieldCache[nodeType] = cached;
            return cached;
        }

        public override void Init(INodeContext context)
        {
            base.Init(context);
            ChainPorts.Clear();
            var fields = GetCachedChainPortFields(GetType());
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].GetValue(this) is ChainPort port)
                    ChainPorts[port.Name] = port;
            }
        }
    }

    public class ShizukuRootNode : ShizukuNormalNode
    {
        public override string Title => "Root Node";
        public sealed override bool SupportControlInput => false;

        public sealed override bool SupportControlOutput => true;
        public override Color TitleBarColor => new Color(0.8f, 0.2f, 0.2f, 1f);

        [SerializeField]
        protected ChainPort _nextPort = new() { Name = "next" };

        public ExecuteResult StartExcute()
        {
            var nextNodeGUID = _nextPort.NextNodeGuid;
            if (_context.Guid2NodeMap.TryGetValue(nextNodeGUID, out var nextNode))
            {
                if (nextNode is ShizukuRunnableNode runnable)
                {
                    return runnable.Execute();
                }
            }
            return ExecuteResult.Continue;
        }
    }
}
