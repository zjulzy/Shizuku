using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 调用函数节点
    /// 放置在主图（或其他函数子图）中，用于调用某个 ShizukuMethod。
    /// 输入端口对应函数的 InputParameters（调用者传入的参数），
    /// 输出端口对应函数的 OutputParameters（函数的返回值）。
    /// 运行时：将输入值注入 EntryNode → 执行函数子图 → 从 ReturnNode 收集返回值。
    /// </summary>
    [Serializable]
    public class InvokeMethodNode : ShizukuRunnableNode
    {
        /// <summary>
        /// 目标函数的 GUID
        /// </summary>
        [SerializeField] public string TargetMethodGUID;

        /// <summary>
        /// 缓存的函数名称（用于显示 Title，避免每帧查找）
        /// </summary>
        [SerializeField] public string TargetMethodName;

        /// <summary>
        /// 动态输入端口列表（对应函数的输入参数）
        /// 调用者通过这些端口传入参数值
        /// </summary>
        [SerializeField]
        public List<MethodPort> DynamicInputPorts = new List<MethodPort>();

        /// <summary>
        /// 动态输出端口列表（对应函数的输出参数/返回值）
        /// 函数执行后的返回值从这些端口流出
        /// </summary>
        [SerializeField]
        public List<MethodPort> DynamicOutputPorts = new List<MethodPort>();

        [SerializeField]
        private ChainPort _nextPort = new() { Name = "next" };

        public override string Title => $"📞 {TargetMethodName ?? "调用函数"}";
        public override Color TitleBarColor => new Color(0.3f, 0.4f, 0.7f, 1f);

        public override void Init(INodeContext context)
        {
            base.Init(context);

            // 将动态输入端口注册到 SelfInputPorts
            foreach (var methodPort in DynamicInputPorts)
            {
                if (methodPort.Port != null && !SelfInputPorts.Contains(methodPort.Port))
                {
                    SelfInputPorts.Add(methodPort.Port);
                }
            }

            // 将动态输出端口注册到 SelfOutputPorts
            foreach (var methodPort in DynamicOutputPorts)
            {
                if (methodPort.Port != null && !SelfOutputPorts.Contains(methodPort.Port))
                {
                    SelfOutputPorts.Add(methodPort.Port);
                }
            }
        }

        /// <summary>
        /// 根据函数定义同步端口列表
        /// 保留名称和类型都匹配的已有端口，移除多余的，添加新增的。
        /// </summary>
        public void SyncPortsFromMethod(ShizukuMethod method)
        {
            TargetMethodName = method.Name;

            // 同步输入端口（对应函数的 InputParameters）
            var newInputPorts = new List<MethodPort>();
            foreach (var param in method.InputParameters)
            {
                var existing = DynamicInputPorts.Find(p => p.Name == param.Name && p.Type == param.Type);
                if (existing != null)
                {
                    newInputPorts.Add(existing);
                }
                else
                {
                    newInputPorts.Add(new MethodPort(param.Name, param.Type, isOut: false));
                }
            }
            DynamicInputPorts = newInputPorts;

            // 同步输出端口（对应函数的 OutputParameters）
            var newOutputPorts = new List<MethodPort>();
            foreach (var param in method.OutputParameters)
            {
                var existing = DynamicOutputPorts.Find(p => p.Name == param.Name && p.Type == param.Type);
                if (existing != null)
                {
                    newOutputPorts.Add(existing);
                }
                else
                {
                    newOutputPorts.Add(new MethodPort(param.Name, param.Type, isOut: true));
                }
            }
            DynamicOutputPorts = newOutputPorts;
        }

        protected override void OnExecute()
        {
            if (RootGraph == null) return;

            var method = RootGraph.GetMethodByGUID(TargetMethodGUID);
            if (method == null)
            {
                ShizukuErrorReporter.LogError($"InvokeMethodNode 找不到目标函数: {TargetMethodGUID}", this);
                return;
            }

            // 1. 找到函数的入口节点
            var entryNode = method.GetNodeByGUID(method.EntryNodeGUID) as MethodEntryNode;
            if (entryNode == null)
            {
                ShizukuErrorReporter.LogError($"InvokeMethodNode 函数 {method.Name} 没有入口节点", this);
                return;
            }

            // 2. 将调用者的输入值注入到入口节点的输出端口
            for (int i = 0; i < DynamicInputPorts.Count && i < entryNode.OutputPorts.Count; i++)
            {
                var callerPort = DynamicInputPorts[i].Port;
                var entryPort = entryNode.OutputPorts[i].Port;
                if (callerPort != null && entryPort != null)
                {
                    // 将调用者端口的值复制到入口节点对应端口
                    entryPort.SetSelfValue(callerPort.GetSelfValue());
                }
            }

            // 3. 从入口节点开始执行函数子图（压入 Method 帧便于错误定位）
            var ctx = ShizukuExecutionContext.Current;
            ctx?.PushMethodFrame(method.Name);
            try
            {
                entryNode.StartExcute();
            }
            finally
            {
                ctx?.PopFrame();
            }

            // 4. 从返回节点收集返回值
            if (!string.IsNullOrEmpty(method.ReturnNodeGUID))
            {
                var returnNode = method.GetNodeByGUID(method.ReturnNodeGUID) as MethodReturnNode;
                if (returnNode != null)
                {
                    var returnValues = returnNode.CollectReturnValues();

                    // 将返回值设置到调用者的输出端口
                    foreach (var outputMethodPort in DynamicOutputPorts)
                    {
                        if (outputMethodPort.Port != null && returnValues.TryGetValue(outputMethodPort.Name, out var value))
                        {
                            outputMethodPort.Port.SetSelfValue(value);
                        }
                    }
                }
            }
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _nextPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }
    }
}

