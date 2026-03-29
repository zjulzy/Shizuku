using System;
using UnityEngine;

/// <summary>
/// 图变量定义
/// 使用 GUID 引用，支持重命名而不破坏节点连接
/// 自定义类型通过 CustomTypeName + CustomValue 统一存储
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public class GraphVariable
    {
        [SerializeField] public string GUID;
        [SerializeField] public string Name;
        [SerializeField] public VariableType Type;

        /// <summary>
        /// 自定义类型的完整类型名（仅 Type == Custom 时有效）
        /// </summary>
        [SerializeField] public string CustomTypeName;

        // 每种内置类型一个字段，避免装箱
        [SerializeField] public int IntValue;
        [SerializeField] public float FloatValue;
        [SerializeField] public bool BoolValue;
        [SerializeField] public string StringValue;
        [SerializeField] public Vector2 Vector2Value;
        [SerializeField] public Vector3 Vector3Value;
        [SerializeField] public GameObject GameObjectValue;
        [SerializeField] public Transform TransformValue;
        [SerializeField] public Color ColorValue;

        /// <summary>
        /// 自定义类型的值（仅 Type == Custom 时使用）
        /// 要求自定义类型为 [Serializable] class
        /// </summary>
        [SerializeReference] public object CustomValue;

        public GraphVariable()
        {
            GUID = Guid.NewGuid().ToString();
            Name = "NewVariable";
            Type = VariableType.Float;
            FloatValue = 0f;
        }

        public GraphVariable(string name, VariableType type)
        {
            GUID = Guid.NewGuid().ToString();
            Name = name;
            Type = type;
            SetDefaultValue(type);
        }

        private void SetDefaultValue(VariableType type)
        {
            switch (type)
            {
                case VariableType.Int:       IntValue = 0; break;
                case VariableType.Float:     FloatValue = 0f; break;
                case VariableType.Bool:      BoolValue = false; break;
                case VariableType.String:    StringValue = ""; break;
                case VariableType.Vector2:   Vector2Value = Vector2.zero; break;
                case VariableType.Vector3:   Vector3Value = Vector3.zero; break;
                case VariableType.GameObject: GameObjectValue = null; break;
                case VariableType.Transform: TransformValue = null; break;
                case VariableType.Color:     ColorValue = Color.white; break;
                case VariableType.Custom:    CustomValue = null; break;
            }
        }
    }

}
