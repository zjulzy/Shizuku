using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时变量存储容器（零装箱）
/// 将所有分类型字典集中管理，支持一键克隆
/// 自定义类型通过 partial 扩展
/// </summary>
public partial class RuntimeVariableStore
{
    // 内置类型字典
    public Dictionary<string, int> Ints = new Dictionary<string, int>();
    public Dictionary<string, float> Floats = new Dictionary<string, float>();
    public Dictionary<string, bool> Bools = new Dictionary<string, bool>();
    public Dictionary<string, string> Strings = new Dictionary<string, string>();
    public Dictionary<string, Vector2> Vector2s = new Dictionary<string, Vector2>();
    public Dictionary<string, Vector3> Vector3s = new Dictionary<string, Vector3>();
    public Dictionary<string, GameObject> GameObjects = new Dictionary<string, GameObject>();
    public Dictionary<string, Transform> Transforms = new Dictionary<string, Transform>();
    public Dictionary<string, Color> Colors = new Dictionary<string, Color>();
    
    /// <summary>
    /// 初始化自定义类型字典（由生成代码实现）
    /// </summary>
    partial void InitCustomTypeDictionaries();
    
    /// <summary>
    /// 克隆自定义类型字典（由生成代码实现）
    /// </summary>
    partial void CloneCustomTypeDictionaries(RuntimeVariableStore target);
    
    /// <summary>
    /// 初始化所有字典（清空后创建新实例）
    /// </summary>
    public void Init()
    {
        Ints = new Dictionary<string, int>();
        Floats = new Dictionary<string, float>();
        Bools = new Dictionary<string, bool>();
        Strings = new Dictionary<string, string>();
        Vector2s = new Dictionary<string, Vector2>();
        Vector3s = new Dictionary<string, Vector3>();
        GameObjects = new Dictionary<string, GameObject>();
        Transforms = new Dictionary<string, Transform>();
        Colors = new Dictionary<string, Color>();
        InitCustomTypeDictionaries();
    }
    
    /// <summary>
    /// 从 GraphVariable 列表加载初始值
    /// </summary>
    public void LoadFromVariables(List<GraphVariable> variables)
    {
        foreach (var variable in variables)
        {
            switch (variable.Type)
            {
                case VariableType.Int:
                    Ints[variable.GUID] = variable.IntValue;
                    break;
                case VariableType.Float:
                    Floats[variable.GUID] = variable.FloatValue;
                    break;
                case VariableType.Bool:
                    Bools[variable.GUID] = variable.BoolValue;
                    break;
                case VariableType.String:
                    Strings[variable.GUID] = variable.StringValue;
                    break;
                case VariableType.Vector2:
                    Vector2s[variable.GUID] = variable.Vector2Value;
                    break;
                case VariableType.Vector3:
                    Vector3s[variable.GUID] = variable.Vector3Value;
                    break;
                case VariableType.GameObject:
                    GameObjects[variable.GUID] = variable.GameObjectValue;
                    break;
                case VariableType.Transform:
                    Transforms[variable.GUID] = variable.TransformValue;
                    break;
                case VariableType.Color:
                    Colors[variable.GUID] = variable.ColorValue;
                    break;
                default:
                    LoadCustomTypeVariable(variable);
                    break;
            }
        }
    }
    
    /// <summary>
    /// 加载单个自定义类型变量（由生成代码实现）
    /// </summary>
    partial void LoadCustomTypeVariable(GraphVariable variable);
    
    /// <summary>
    /// 深拷贝整个容器（值类型字典天然深拷贝）
    /// </summary>
    public RuntimeVariableStore Clone()
    {
        var clone = new RuntimeVariableStore
        {
            Ints = new Dictionary<string, int>(Ints),
            Floats = new Dictionary<string, float>(Floats),
            Bools = new Dictionary<string, bool>(Bools),
            Strings = new Dictionary<string, string>(Strings),
            Vector2s = new Dictionary<string, Vector2>(Vector2s),
            Vector3s = new Dictionary<string, Vector3>(Vector3s),
            GameObjects = new Dictionary<string, GameObject>(GameObjects),
            Transforms = new Dictionary<string, Transform>(Transforms),
            Colors = new Dictionary<string, Color>(Colors),
        };
        CloneCustomTypeDictionaries(clone);
        return clone;
    }
}
