using System;
using UnityEngine;

// ============================================================
// Set Variable 泛型中间层
// TPort: 具体的 ParameterEdgePort<TValue> 子类
// TValue: 变量值类型
// ============================================================

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public abstract class SetVariableNodeBase<TPort, TValue> : ShizukuRunnableNode, IVariableNode
        where TPort : ParameterEdgePort<TValue>, new()
    {
        [SerializeField]
        public string VariableGUID;

        [SerializeReference]
        public TPort Input = new TPort { IsOut = false, Name = "Value" };

        [SerializeField]
        private ChainPort _nextPort = new ChainPort { Name = "Next" };

        public override string Title => GetDisplayName();
        public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);

        /// <summary>
        /// 子类实现：返回此节点对应的 VariableType，用于变量选择器过滤
        /// </summary>
        public abstract VariableType TargetVariableType { get; }

        protected override void OnExecute()
        {
            SetVariable(VariableGUID, Input.Value);
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _nextPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }

        /// <summary>
        /// 子类实现：调用 _parentGraph 上对应类型的 SetVariable 方法
        /// </summary>
        protected abstract void SetVariable(string guid, TValue value);

        private string GetDisplayName()
        {
            var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
            return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
        }
    }


}
