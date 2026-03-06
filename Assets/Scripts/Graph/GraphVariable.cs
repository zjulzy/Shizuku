using System;
using UnityEngine;

/// <summary>
/// 图变量定义
/// 使用 GUID 引用，支持重命名而不破坏节点连接
/// </summary>
[Serializable]
public partial class GraphVariable
{
    [SerializeField]
    public string GUID;
    
    [SerializeField]
    public string Name;
    
    [SerializeField]
    public VariableType Type;
    
    // 每种类型一个字段，避免装箱
    // TODO : 后续可以优化成 union 结构，减少内存占用
    [SerializeField]
    public int IntValue;
    
    [SerializeField]
    public float FloatValue;
    
    [SerializeField]
    public bool BoolValue;
    
    [SerializeField]
    public string StringValue;
    
    [SerializeField]
    public Vector2 Vector2Value;
    
    [SerializeField]
    public Vector3 Vector3Value;
    
    [SerializeField]
    public GameObject GameObjectValue;
    
    [SerializeField]
    public Transform TransformValue;
    
    [SerializeField]
    public Color ColorValue;
    
    // TODO: 可以设置一个编辑器only的变量，存放所有依赖这个变量的节点
    
    public GraphVariable()
    {
        GUID = System.Guid.NewGuid().ToString();
        Name = "NewVariable";
        Type = VariableType.Float;
        FloatValue = 0f;
    }
    
    public GraphVariable(string name, VariableType type)
    {
        GUID = System.Guid.NewGuid().ToString();
        Name = name;
        Type = type;
        SetDefaultValue(type);
    }
    
    private void SetDefaultValue(VariableType type)
    {
        switch (type)
        {
            case VariableType.Int:
                IntValue = 0;
                break;
            case VariableType.Float:
                FloatValue = 0f;
                break;
            case VariableType.Bool:
                BoolValue = false;
                break;
            case VariableType.String:
                StringValue = "";
                break;
            case VariableType.Vector2:
                Vector2Value = Vector2.zero;
                break;
            case VariableType.Vector3:
                Vector3Value = Vector3.zero;
                break;
            case VariableType.GameObject:
                GameObjectValue = null;
                break;
            case VariableType.Transform:
                TransformValue = null;
                break;
            case VariableType.Color:
                ColorValue = Color.white;
                break;
            default:
                // 自定义类型由生成代码处理
                SetDefaultValueCustomType(type);
                break;
        }
    }
    
    /// <summary>
    /// 设置自定义类型的默认值（由生成代码实现）
    /// </summary>
    partial void SetDefaultValueCustomType(VariableType type);
}
