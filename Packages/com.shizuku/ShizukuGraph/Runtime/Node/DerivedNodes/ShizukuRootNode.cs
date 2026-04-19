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

        public override void Init(INodeContext context)
        {
            base.Init(context);
            ChainPorts.Clear();
            var type = GetType();
            while (type != null && type != typeof(object))
            {
                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (typeof(ChainPort).IsAssignableFrom(field.FieldType))
                    {
                        var port = field.GetValue(this) as ChainPort;
                        if (port != null)
                        {
                            ChainPorts[port.Name] = port;
                        }
                    }
                }
                type = type.BaseType;
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
