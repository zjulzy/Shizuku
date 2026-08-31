using System;
using UnityEngine;

/// <summary>
/// Vector 类型转换节点集合
/// </summary>

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    [NodeMenuItem("类型转换/Vector3 To Vector2", Description = "三维向量转二维向量（丢弃Z）")]
    public class Vector3ToVector2Converter : TypeConverterNode
    {
        [SerializeReference]
        private Vector3ParameterEdgePort _input = new Vector3ParameterEdgePort { IsOut = false, Name = "Vector3" };

        [SerializeReference]
        private Vector2ParameterEdgePort _output = new Vector2ParameterEdgePort { IsOut = true, Name = "Vector2" };


        public override Type InputType => typeof(Vector3);
        public override Type OutputType => typeof(Vector2);

        protected override void ConvertValue()
        {
            _output.Value = new Vector2(_input.Value.x, _input.Value.y);
        }
    }

    [Serializable]
    [NodeMenuItem("类型转换/Vector2 To Vector3", Description = "二维向量转三维向量（Z=0）")]
    public class Vector2ToVector3Converter : TypeConverterNode
    {
        [SerializeReference]
        private Vector2ParameterEdgePort _input = new Vector2ParameterEdgePort { IsOut = false, Name = "Vector2" };

        [SerializeReference]
        private Vector3ParameterEdgePort _output = new Vector3ParameterEdgePort { IsOut = true, Name = "Vector3" };


        public override Type InputType => typeof(Vector2);
        public override Type OutputType => typeof(Vector3);

        protected override void ConvertValue()
        {
            _output.Value = new Vector3(_input.Value.x, _input.Value.y, 0f);
        }
    }


}
