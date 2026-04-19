using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [NodeMenuItem("打印", NodeCategory.Basic, Description = "输出日志消息")]
    public class ShizukuLogNode : ShizukuRunnableNode
    {
        public override string Title => "Log Node";

        [SerializeReference]
        private StringParameterEdgePort Message = new() { IsOut = false, Name = "message" };

        [SerializeField]
        private ChainPort _nextPort = new() {Name = "next" };

        protected override void OnExecute()
        {
            Debug.Log($"帧号:{Time.frameCount} 执行节点 {GUID} 日志:{Message.Value}");
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _nextPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }
    }
}
