using System;
using UnityEngine;

/// <summary>
/// 减法节点 - Int 版本
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    [NodeMenuItem("减法/Int", NodeCategory.Math, Description = "整数减法")]
    public class SubtractNode_Int : ShizukuValueNode
    {
        public override string Title => "Subtract (Int)";

        [SerializeReference]
        private IntParameterEdgePort _a = new() { IsOut = false, Name = "A" };

        [SerializeReference]
        private IntParameterEdgePort _b = new() { IsOut = false, Name = "B" };

        [SerializeReference]
        private IntParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _a.Value - _b.Value;
        }
    }

    /// <summary>
    /// 减法节点 - Float 版本
    /// </summary>
    [Serializable]
    [NodeMenuItem("减法/Float", NodeCategory.Math, Description = "浮点数减法")]
    public class SubtractNode_Float : ShizukuValueNode
    {
        public override string Title => "Subtract (Float)";

        [SerializeReference]
        private FloatParameterEdgePort _a = new() { IsOut = false, Name = "A" };

        [SerializeReference]
        private FloatParameterEdgePort _b = new() { IsOut = false, Name = "B" };

        [SerializeReference]
        private FloatParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _a.Value - _b.Value;
        }
    }

    /// <summary>
    /// 减法节点 - Vector2 版本
    /// </summary>
    [Serializable]
    [NodeMenuItem("减法/Vector2", NodeCategory.Math, Description = "二维向量减法")]
    public class SubtractNode_Vector2 : ShizukuValueNode
    {
        public override string Title => "Subtract (Vector2)";

        [SerializeReference]
        private Vector2ParameterEdgePort _a = new() { IsOut = false, Name = "A" };

        [SerializeReference]
        private Vector2ParameterEdgePort _b = new() { IsOut = false, Name = "B" };

        [SerializeReference]
        private Vector2ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _a.Value - _b.Value;
        }
    }

    /// <summary>
    /// 减法节点 - Vector3 版本
    /// </summary>
    [Serializable]
    [NodeMenuItem("减法/Vector3", NodeCategory.Math, Description = "三维向量减法")]
    public class SubtractNode_Vector3 : ShizukuValueNode
    {
        public override string Title => "Subtract (Vector3)";

        [SerializeReference]
        private Vector3ParameterEdgePort _a = new() { IsOut = false, Name = "A" };

        [SerializeReference]
        private Vector3ParameterEdgePort _b = new() { IsOut = false, Name = "B" };

        [SerializeReference]
        private Vector3ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _a.Value - _b.Value;
        }
    }


}
