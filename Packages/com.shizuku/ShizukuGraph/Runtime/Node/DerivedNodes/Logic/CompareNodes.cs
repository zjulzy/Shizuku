using System;
using UnityEngine;

// ============================================================
// 比较节点 - Int 类型比较
// ============================================================

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    [NodeMenuItem("逻辑/Int Equal (==)", Description = "比较两个整数是否相等")]
    public class CompareNode_Int_Equal : ShizukuValueNode
    {
        [SerializeReference]
        public IntParameterEdgePort InputA = new IntParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public IntParameterEdgePort InputB = new IntParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A == B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value == InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Int Not Equal (!=)", Description = "比较两个整数是否不等")]
    public class CompareNode_Int_NotEqual : ShizukuValueNode
    {
        [SerializeReference]
        public IntParameterEdgePort InputA = new IntParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public IntParameterEdgePort InputB = new IntParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A != B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value != InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Int Greater (>)", Description = "比较A是否大于B")]
    public class CompareNode_Int_Greater : ShizukuValueNode
    {
        [SerializeReference]
        public IntParameterEdgePort InputA = new IntParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public IntParameterEdgePort InputB = new IntParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A > B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value > InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Int Greater Or Equal (>=)", Description = "比较A是否大于等于B")]
    public class CompareNode_Int_GreaterOrEqual : ShizukuValueNode
    {
        [SerializeReference]
        public IntParameterEdgePort InputA = new IntParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public IntParameterEdgePort InputB = new IntParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A >= B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value >= InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Int Less (<)", Description = "比较A是否小于B")]
    public class CompareNode_Int_Less : ShizukuValueNode
    {
        [SerializeReference]
        public IntParameterEdgePort InputA = new IntParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public IntParameterEdgePort InputB = new IntParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A < B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value < InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Int Less Or Equal (<=)", Description = "比较A是否小于等于B")]
    public class CompareNode_Int_LessOrEqual : ShizukuValueNode
    {
        [SerializeReference]
        public IntParameterEdgePort InputA = new IntParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public IntParameterEdgePort InputB = new IntParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A <= B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value <= InputB.Value;
        }
    }

    // ============================================================
    // 比较节点 - Float 类型比较
    // ============================================================

    [Serializable]
    [NodeMenuItem("逻辑/Float Equal", Description = "比较两个浮点数是否相等（带容差）")]
    public class CompareNode_Float_Equal : ShizukuValueNode
    {
        [SerializeReference]
        public FloatParameterEdgePort InputA = new FloatParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public FloatParameterEdgePort InputB = new FloatParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public FloatParameterEdgePort Tolerance = new FloatParameterEdgePort { IsOut = false, Name = "容差", DefaultValue = 0.0001f };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A ≈ B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = Mathf.Abs(InputA.Value - InputB.Value) <= Tolerance.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Float Greater (>)", Description = "比较A是否大于B")]
    public class CompareNode_Float_Greater : ShizukuValueNode
    {
        [SerializeReference]
        public FloatParameterEdgePort InputA = new FloatParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public FloatParameterEdgePort InputB = new FloatParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A > B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value > InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Float Greater Or Equal (>=)", Description = "比较A是否大于等于B")]
    public class CompareNode_Float_GreaterOrEqual : ShizukuValueNode
    {
        [SerializeReference]
        public FloatParameterEdgePort InputA = new FloatParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public FloatParameterEdgePort InputB = new FloatParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A >= B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value >= InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Float Less (<)", Description = "比较A是否小于B")]
    public class CompareNode_Float_Less : ShizukuValueNode
    {
        [SerializeReference]
        public FloatParameterEdgePort InputA = new FloatParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public FloatParameterEdgePort InputB = new FloatParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A < B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value < InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/Float Less Or Equal (<=)", Description = "比较A是否小于等于B")]
    public class CompareNode_Float_LessOrEqual : ShizukuValueNode
    {
        [SerializeReference]
        public FloatParameterEdgePort InputA = new FloatParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public FloatParameterEdgePort InputB = new FloatParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A <= B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value <= InputB.Value;
        }
    }

    // ============================================================
    // 比较节点 - String 类型比较
    // ============================================================

    [Serializable]
    [NodeMenuItem("逻辑/String Equal", Description = "比较两个字符串是否相等")]
    public class CompareNode_String_Equal : ShizukuValueNode
    {
        [SerializeReference]
        public StringParameterEdgePort InputA = new StringParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public StringParameterEdgePort InputB = new StringParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A == B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value == InputB.Value;
        }
    }

    [Serializable]
    [NodeMenuItem("逻辑/String Not Equal", Description = "比较两个字符串是否不等")]
    public class CompareNode_String_NotEqual : ShizukuValueNode
    {
        [SerializeReference]
        public StringParameterEdgePort InputA = new StringParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public StringParameterEdgePort InputB = new StringParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A != B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value != InputB.Value;
        }
    }

    // ============================================================
    // 比较节点 - Bool 类型比较
    // ============================================================

    [Serializable]
    [NodeMenuItem("逻辑/Bool Equal", Description = "比较两个布尔值是否相等")]
    public class CompareNode_Bool_Equal : ShizukuValueNode
    {
        [SerializeReference]
        public BoolParameterEdgePort InputA = new BoolParameterEdgePort { IsOut = false, Name = "A" };

        [SerializeReference]
        public BoolParameterEdgePort InputB = new BoolParameterEdgePort { IsOut = false, Name = "B" };

        [SerializeReference]
        public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "A == B" };

        public override Color TitleBarColor => new Color(0.5f, 0.7f, 0.9f, 1f);

        protected override void OnComputeOutputValues()
        {
            Output.Value = InputA.Value == InputB.Value;
        }
    }


}
