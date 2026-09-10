using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public class BlueprintEventNode : ShizukuRootNode
    {
        [SerializeField]
        public string EventName = "OnEvent";

        /// <summary>
        /// 关联的返回节点 GUID（可选，有返回值的事件需要设置）
        /// </summary>
        [SerializeField]
        public string ReturnNodeGUID;

        [SerializeField]
        public List<EventParameter> EventParameters = new List<EventParameter>();

        public override string Title => $"Event: {EventName}";
        public override Color TitleBarColor => IsValid() ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.8f, 0.4f, 0f, 1f);

        public override void Init(INodeContext context)
        {
            base.Init(context);

            foreach (var param in EventParameters)
            {
                if (param.OutputPort != null && !SelfOutputPorts.Contains(param.OutputPort))
                {
                    SelfOutputPorts.Add(param.OutputPort);
                }
            }
        }

        public override void GetOutputValues()
        {
            base.GetOutputValues();

            foreach (var param in EventParameters)
            {
                param.OutputPort?.SetSelfValue(param.Value);
            }
        }

        public void TriggerEvent(params object[] args)
        {
            if (RejectActiveLatentReentry())
                return;

            if (EventParameters.Count != args.Length)
            {
                Debug.LogWarning($"Event '{EventName}' parameter count mismatch. Expected {EventParameters.Count}, got {args.Length}");
            }

            for (int i = 0; i < EventParameters.Count && i < args.Length; i++)
            {
                EventParameters[i].SetValue(args[i]);
            }

            StartEventExecution();
        }

        /// <summary>
        /// 触发事件并收集返回值（统一入口，无返回值时返回 null）
        /// </summary>
        public object TriggerEventWithReturn(params object[] args)
        {
            if (RejectActiveLatentReentry())
                return null;

            if (EventParameters.Count != args.Length)
            {
                Debug.LogWarning($"Event '{EventName}' parameter count mismatch. Expected {EventParameters.Count}, got {args.Length}");
            }

            for (int i = 0; i < EventParameters.Count && i < args.Length; i++)
            {
                EventParameters[i].SetValue(args[i]);
            }

            StartEventExecution();

            // 从返回节点收集返回值
            if (!string.IsNullOrEmpty(ReturnNodeGUID) && _context.Guid2NodeMap.TryGetValue(ReturnNodeGUID, out var node))
            {
                if (node is BlueprintReturnNode returnNode)
                {
                    return returnNode.CollectReturnValue();
                }
            }
            return null;
        }

        public bool IsValid()
        {
            if (RootGraph == null) return true;

            var methodInfo = FindMatchingMethod();
            if (methodInfo == null)
            {
                return false;
            }

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != EventParameters.Count)
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType.Name != EventParameters[i].TypeName)
                {
                    return false;
                }
            }

            if (methodInfo.ReturnType != typeof(void) && TryFindReachableLatentNode(out _))
                return false;

            return true;
        }

        public string GetValidationMessage()
        {
            if (RootGraph == null) return "图未初始化";

            var methodInfo = FindMatchingMethod();
            if (methodInfo == null)
            {
                return $"未找到事件方法 '{EventName}'，可能已被删除或重命名";
            }

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != EventParameters.Count)
            {
                return $"参数数量不匹配：期望 {parameters.Length} 个，当前 {EventParameters.Count} 个";
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType.Name != EventParameters[i].TypeName)
                {
                    return $"参数 '{EventParameters[i].Name}' 类型不匹配：期望 {parameters[i].ParameterType.Name}，当前 {EventParameters[i].TypeName}";
                }
            }

            if (methodInfo.ReturnType != typeof(void) && TryFindReachableLatentNode(out var latentNode))
            {
                return $"带返回值事件不支持 Latent 节点：{latentNode.Title}";
            }

            return "有效";
        }

        private bool RequiresSynchronousReturn()
        {
            var methodInfo = FindMatchingMethod();
            return methodInfo != null
                ? methodInfo.ReturnType != typeof(void)
                : !string.IsNullOrEmpty(ReturnNodeGUID);
        }

        private void StartEventExecution()
        {
            if (!RequiresSynchronousReturn())
            {
                StartExcute();
                return;
            }

            using (RootGraph.DisallowLatentExecution(
                       $"带返回值的 Blueprint Event '{EventName}' 不支持 Latent 节点"))
            {
                StartExcute();
            }
        }

        private bool RejectActiveLatentReentry()
        {
            if (!TryFindReachableLatentNode(out var latentNode, activeOnly: true))
                return false;

            ShizukuErrorReporter.LogError(
                $"Blueprint Event '{EventName}' 触发了正在运行的 Latent 节点，重入已拒绝: " +
                $"{latentNode.Title} ({latentNode.GUID})",
                latentNode);
            return true;
        }

        private bool TryFindReachableLatentNode(
            out ShizukuLatentNode latentNode,
            bool activeOnly = false)
        {
            latentNode = null;
            if (_context == null || string.IsNullOrEmpty(_nextPort?.NextNodeGuid))
                return false;

            var pending = new Stack<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            pending.Push(_nextPort.NextNodeGuid);

            while (pending.Count > 0)
            {
                var nodeGuid = pending.Pop();
                if (string.IsNullOrEmpty(nodeGuid) || !visited.Add(nodeGuid))
                    continue;

                if (!_context.Guid2NodeMap.TryGetValue(nodeGuid, out var node) || node == null)
                    continue;

                if (node is ShizukuLatentNode found)
                {
                    if (!activeOnly || found.IsLatentActive)
                    {
                        latentNode = found;
                        return true;
                    }
                }

                if (node is not ShizukuNormalNode normalNode)
                    continue;

                foreach (var port in normalNode.ChainPorts.Values)
                {
                    if (port != null && !string.IsNullOrEmpty(port.NextNodeGuid))
                        pending.Push(port.NextNodeGuid);
                }
            }

            return false;
        }

        private MethodInfo FindMatchingMethod()
        {
            var behaviorType = GetBehaviorType();
            if (behaviorType == null) return null;

            var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<BlueprintOverridableAttribute>();
                if (attr != null)
                {
                    var methodEventName = attr.EventName ?? method.Name;
                    if (methodEventName == EventName)
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        private Type GetBehaviorType()
        {
            if (RootGraph == null) return null;

            var graphType = RootGraph.GetType();
            while (graphType != null && graphType != typeof(object))
            {
                if (graphType.IsGenericType)
                {
                    var genericDef = graphType.GetGenericTypeDefinition();
                    if (genericDef.Name.StartsWith("ShizukuBluePrint"))
                    {
                        var genericArgs = graphType.GetGenericArguments();
                        if (genericArgs.Length > 0)
                        {
                            return genericArgs[0];
                        }
                    }
                }
                graphType = graphType.BaseType;
            }
            return null;
        }
    }

    [Serializable]
    public class EventParameter
    {
        [SerializeField]
        public string Name;

        [SerializeField]
        public string TypeName;

        [NonSerialized]
        public object Value;

        [SerializeReference]
        public ParameterEdgePort OutputPort;

        public void SetValue(object value)
        {
            Value = value;
        }
    }


}
