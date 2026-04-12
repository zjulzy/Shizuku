using System;
using UnityEngine;

/// <summary>
/// 乘法节点 - Int 版本
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public class MultiplyNode_Int : ShizukuValueNode
    {
        public override string Title => "Multiply (Int)";

        [SerializeReference]
        private IntParameterEdgePort _a = new() { IsOut = false, Name = "A" };

        [SerializeReference]
        private IntParameterEdgePort _b = new() { IsOut = false, Name = "B" };

        [SerializeReference]
        private IntParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _a.Value * _b.Value;
        }
    }

    /// <summary>
    /// 乘法节点 - Float 版本
    /// </summary>
    [Serializable]
    public class MultiplyNode_Float : ShizukuValueNode
    {
        public override string Title => "Multiply (Float)";

        [SerializeReference]
        private FloatParameterEdgePort _a = new() { IsOut = false, Name = "A" };

        [SerializeReference]
        private FloatParameterEdgePort _b = new() { IsOut = false, Name = "B" };

        [SerializeReference]
        private FloatParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _a.Value * _b.Value;
        }
    }

    /// <summary>
    /// 乘法节点 - Vector2 版本（标量乘法）
    /// </summary>
    [Serializable]
    public class MultiplyNode_Vector2 : ShizukuValueNode
    {
        public override string Title => "Multiply (Vector2)";

        [SerializeReference]
        private Vector2ParameterEdgePort _vector = new() { IsOut = false, Name = "Vector" };

        [SerializeReference]
        private FloatParameterEdgePort _scalar = new() { IsOut = false, Name = "Scalar" };

        [SerializeReference]
        private Vector2ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _vector.Value * _scalar.Value;
        }
    }

    /// <summary>
    /// 乘法节点 - Vector3 版本（标量乘法）
    /// </summary>
    [Serializable]
    [NodeMenuItem("数学/乘法/三维向量", NodeCategory.Math, Description = "三维向量标量乘法")]
    public class MultiplyNode_Vector3 : ShizukuValueNode
    {
        public override string Title => "Multiply (Vector3)";

        [SerializeReference]
        private Vector3ParameterEdgePort _vector = new() { IsOut = false, Name = "Vector" };

        [SerializeReference]
        private FloatParameterEdgePort _scalar = new() { IsOut = false, Name = "Scalar" };

        [SerializeReference]
        private Vector3ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = _vector.Value * _scalar.Value;
        }
    }


}
