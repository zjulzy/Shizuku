using System;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 函数端口定义（动态端口）
    /// 类似 EventParameter，但基于 VariableType 而非反射 System.Type。
    /// 用于 MethodEntryNode 和 MethodReturnNode 的可变参数端口。
    /// </summary>
    [Serializable]
    public class MethodPort
    {
        [SerializeField] public string Name;
        [SerializeField] public VariableType Type;

        [SerializeReference]
        public ParameterEdgePort Port;

        public MethodPort() { }

        public MethodPort(string name, VariableType type, bool isOut)
        {
            Name = name;
            Type = type;
            Port = CreatePortForVariableType(name, type, isOut);
        }

        /// <summary>
        /// 根据 VariableType 创建对应的具体 ParameterEdgePort 实例
        /// </summary>
        public static ParameterEdgePort CreatePortForVariableType(string name, VariableType type, bool isOut)
        {
            ParameterEdgePort port;
            switch (type)
            {
                case VariableType.Int:
                    port = new IntParameterEdgePort();
                    break;
                case VariableType.Float:
                    port = new FloatParameterEdgePort();
                    break;
                case VariableType.Bool:
                    port = new BoolParameterEdgePort();
                    break;
                case VariableType.String:
                    port = new StringParameterEdgePort();
                    break;
                case VariableType.Vector2:
                    port = new Vector2ParameterEdgePort();
                    break;
                case VariableType.Vector3:
                    port = new Vector3ParameterEdgePort();
                    break;
                case VariableType.GameObject:
                    port = new GameObjectParameterEdgePort();
                    break;
                case VariableType.Transform:
                    port = new TransformParameterEdgePort();
                    break;
                case VariableType.Color:
                    port = new ColorParameterEdgePort();
                    break;
                default:
                    port = new ObjectParameterEdgePort();
                    break;
            }

            port.Name = name;
            port.IsOut = isOut;
            return port;
        }
    }
}

