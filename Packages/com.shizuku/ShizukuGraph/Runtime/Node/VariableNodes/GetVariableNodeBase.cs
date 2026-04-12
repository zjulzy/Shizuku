using System;
using UnityEngine;

// ============================================================
// Get Variable 泛型中间层
// TPort: 具体的 ParameterEdgePort<TValue> 子类
// TValue: 变量值类型
// ============================================================

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public abstract class GetVariableNodeBase<TPort, TValue> : ShizukuValueNode, IVariableNode
        where TPort : ParameterEdgePort<TValue>, new()
    {
        [SerializeField]
        public string VariableGUID;

        [SerializeReference]
        public TPort Output = new TPort { IsOut = true, Name = "Value" };

        public override string Title => GetDisplayName();
        public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);

        /// <summary>
        /// 子类实现：返回此节点对应的 VariableType，用于变量选择器过滤
        /// </summary>
        public abstract VariableType TargetVariableType { get; }

        protected override void OnComputeOutputValues()
        {
            if (TryGetVariable(VariableGUID, out var value))
            {
                Output.Value = value;
            }
            else
            {
                Output.Value = GetDefaultValue();
            }
        }

        /// <summary>
        /// 子类实现：调用 RootGraph 上对应类型的 TryGetVariable 方法
        /// </summary>
        protected abstract bool TryGetVariable(string guid, out TValue value);

        /// <summary>
        /// 子类实现：返回该类型的默认值
        /// </summary>
        protected abstract TValue GetDefaultValue();

        private string GetDisplayName()
        {
            var variable = RootGraph?.GetVariableByGUID(VariableGUID);
            return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
        }
    }


}
