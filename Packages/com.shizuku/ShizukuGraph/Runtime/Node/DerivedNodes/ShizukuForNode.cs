using System;
using Shizuku.Core;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// For 循环节点。从 Start 到 End（不含）步进 Step，每次执行 Body 子链。
    /// 输出当前索引 Index 供 Body 内节点读取。
    /// </summary>
    [Serializable]
    [NodeMenuItem("逻辑/For 循环", NodeCategory.Logic, Description = "按索引循环执行")]
    public class ShizukuForNode : ShizukuRunnableNode
    {
        public override string Title => "For";
        public override Color TitleBarColor => new Color(0.2f, 0.6f, 0.4f, 1f);

        [SerializeReference]
        private IntParameterEdgePort _start = new() { IsOut = false, Name = "start", Value = 0 };

        [SerializeReference]
        private IntParameterEdgePort _end = new() { IsOut = false, Name = "end", Value = 10 };

        [SerializeReference]
        private IntParameterEdgePort _step = new() { IsOut = false, Name = "step", Value = 1 };

        [SerializeReference]
        private IntParameterEdgePort _index = new() { IsOut = true, Name = "index" };

        [SerializeField]
        private ChainPort _bodyPort = new() { Name = "body" };

        [SerializeField]
        private ChainPort _completedPort = new() { Name = "completed" };

        private const int MaxIterations = 100000;

        protected override void OnExecute()
        {
            int start = _start.Value;
            int end = _end.Value;
            int step = _step.Value;

            if (step == 0)
            {
                Debug.LogWarning("[ForNode] Step 不能为 0，跳过循环");
                return;
            }

            int count = 0;
            for (int i = start; step > 0 ? i < end : i > end; i += step)
            {
                if (++count > MaxIterations)
                {
                    Debug.LogError("[ForNode] 超过最大迭代次数，强制退出");
                    break;
                }
                _index.Value = i;
                var result = ExecuteSubChain(_bodyPort);
                if (result == ExecuteResult.Halted)
                    break;
            }
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _completedPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }
    }
}
