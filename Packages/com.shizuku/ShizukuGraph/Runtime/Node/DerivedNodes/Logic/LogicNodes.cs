using System;
using UnityEngine;

// ============================================================
// 逻辑节点 - And (逻辑与)
// ============================================================

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    [NodeMenuItem("与", NodeCategory.Logic, Description = "逻辑与运算，所有输入为真时返回真")]
    public class AndNode : ShizukuValueNode
    {
        [SerializeReference]
        public BoolParameterEdgePort InputA = new BoolParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public BoolParameterEdgePort InputB = new BoolParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "Result" };

        public override string Title => "And (&&)";
        public override Color TitleBarColor => new Color(0.4f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value && InputB.Value;
        }
    }

    // ============================================================
    // 逻辑节点 - Or (逻辑或)
    // ============================================================

    [Serializable]
    [NodeMenuItem("或", NodeCategory.Logic, Description = "逻辑或运算，任意输入为真时返回真")]
    public class OrNode : ShizukuValueNode
    {
        [SerializeReference]
        public BoolParameterEdgePort InputA = new BoolParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public BoolParameterEdgePort InputB = new BoolParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "Result" };

        public override string Title => "Or (||)";
        public override Color TitleBarColor => new Color(0.4f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value || InputB.Value;
        }
    }

    // ============================================================
    // 逻辑节点 - Not (逻辑非)
    // ============================================================

    [Serializable]
    [NodeMenuItem("非", NodeCategory.Logic, Description = "逻辑非运算，反转布尔值")]
    public class NotNode : ShizukuValueNode
    {
        [SerializeReference]
        public BoolParameterEdgePort Input = new BoolParameterEdgePort { IsOut = false, Name = "Value" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "Result" };

        public override string Title => "Not (!)";
        public override Color TitleBarColor => new Color(0.4f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = !Input.Value;
        }
    }

    // ============================================================
    // 逻辑节点 - Xor (逻辑异或)
    // ============================================================

    [Serializable]
    [NodeMenuItem("异或", NodeCategory.Logic, Description = "逻辑异或运算，两个输入不同时返回真")]
    public class XorNode : ShizukuValueNode
    {
        [SerializeReference]
        public BoolParameterEdgePort InputA = new BoolParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public BoolParameterEdgePort InputB = new BoolParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "Result" };

        public override string Title => "Xor (^)";
        public override Color TitleBarColor => new Color(0.4f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value ^ InputB.Value;
        }
    }


}
