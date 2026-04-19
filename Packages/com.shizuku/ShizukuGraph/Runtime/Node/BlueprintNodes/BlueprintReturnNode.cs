using System;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;

    /// <summary>
    /// 蓝图事件返回节点
    /// 放置在事件执行链的末尾，用于收集返回值传回给 C# 调用方。
    /// 无返回值的事件不需要此节点。
    /// </summary>
    [Serializable]
    public class BlueprintReturnNode : ShizukuNormalNode
    {
        /// <summary>
        /// 所属事件名（用于关联到对应的 BlueprintEventNode）
        /// </summary>
        [SerializeField]
        public string EventName;

        /// <summary>
        /// 返回值输入端口（根据返回类型设置）
        /// </summary>
        [SerializeReference]
        public ParameterEdgePort ReturnPort;

        public override string Title => $"◀ 事件返回 ({EventName})";
        public override Color TitleBarColor => new Color(0.6f, 0.2f, 0.3f, 1f);

        public sealed override bool SupportControlInput => true;
        public sealed override bool SupportControlOutput => false;

        public override void Init(INodeContext context)
        {
            base.Init(context);

            if (ReturnPort != null && !SelfInputPorts.Contains(ReturnPort))
            {
                SelfInputPorts.Add(ReturnPort);
            }
        }

        /// <summary>
        /// 收集返回值（由 BlueprintEventNode.TriggerEventWithReturn 调用）
        /// </summary>
        public object CollectReturnValue()
        {
            // 拉取上游的值
            GetInputValues();

            if (ReturnPort != null)
            {
                return ReturnPort.GetSelfValue();
            }
            return null;
        }
    }
}

