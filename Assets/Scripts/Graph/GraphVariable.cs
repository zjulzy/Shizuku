using System;
using UnityEngine;

/// <summary>
/// 图变量定义
/// 使用 GUID 引用，支持重命名而不破坏节点连接
/// </summary>
[Serializable]
public class GraphVariable
{
    [SerializeField]
    public string GUID;
    
    [SerializeField]
    public string Name;
    
    [SerializeField]
    public VariableType Type;
    
    // 每种类型一个字段，避免装箱
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
        }
    }
}

/// <summary>
/// 变量类型枚举
/// </summary>
public enum VariableType
{
    Int,
    Float,
    Bool,
    String,
    Vector2,
    Vector3,
    GameObject,
    Transform,
    Color
}

