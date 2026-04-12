using System;
using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public abstract class PropertyGetNode<TPort> : ShizukuValueNode where TPort : ParameterEdgePort, new()
    {
        [SerializeField]
        public string PropertyName = "property";

        [SerializeReference]
        public TPort Output = new TPort { IsOut = true, Name = "Value" };

        protected abstract string TypeName { get; }

        public override string Title => $"Get {PropertyName} ({TypeName})";
        public override Color TitleBarColor => new Color(0.2f, 0.6f, 1f, 1f);

        public override void GetOutputValues()
        {
            if (RootGraph != null && (RootGraph as ShizukuBluePrint).TryGetProperty(PropertyName, out var value))
            {
                try
                {
                    SetOutputValue(value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"GetPropertyNode<{TypeName}>: Failed to convert '{PropertyName}': {e.Message}");
                    SetOutputValueDefault();
                }
            }
            else
            {
                SetOutputValueDefault();
            }
        }

        protected abstract void SetOutputValue(object value);
        protected abstract void SetOutputValueDefault();
    }

    [Serializable]
    public abstract class PropertySetNode<TPort> : ShizukuRunnableNode where TPort : ParameterEdgePort, new()
    {
        [SerializeField]
        public string PropertyName = "property";

        [SerializeReference]
        public TPort Input = new TPort { IsOut = false, Name = "Value" };

        [SerializeField]
        private ChainPort _nextPort = new ChainPort { Name = "Next" };

        protected abstract string TypeName { get; }

        public override string Title => $"Set {PropertyName} ({TypeName})";
        public override Color TitleBarColor => new Color(1f, 0.5f, 0.2f, 1f);

        protected override void OnExecute()
        {
            if (RootGraph != null)
            {
                var value = GetInputValue();
                (RootGraph as ShizukuBluePrint).TrySetProperty(PropertyName, value);
            }
        }

        protected abstract object GetInputValue();

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _nextPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }
    }

    // ============================================================
    // Float 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_Float : PropertyGetNode<FloatParameterEdgePort>
    {
        protected override string TypeName => "Float";

        protected override void SetOutputValue(object value)
        {
            Output.Value = Convert.ToSingle(value);
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = 0f;
        }
    }

    [Serializable]
    public class SetPropertyNode_Float : PropertySetNode<FloatParameterEdgePort>
    {
        protected override string TypeName => "Float";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // Int 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_Int : PropertyGetNode<IntParameterEdgePort>
    {
        protected override string TypeName => "Int";

        protected override void SetOutputValue(object value)
        {
            Output.Value = Convert.ToInt32(value);
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = 0;
        }
    }

    [Serializable]
    public class SetPropertyNode_Int : PropertySetNode<IntParameterEdgePort>
    {
        protected override string TypeName => "Int";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // Bool 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_Bool : PropertyGetNode<BoolParameterEdgePort>
    {
        protected override string TypeName => "Bool";

        protected override void SetOutputValue(object value)
        {
            Output.Value = Convert.ToBoolean(value);
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = false;
        }
    }

    [Serializable]
    public class SetPropertyNode_Bool : PropertySetNode<BoolParameterEdgePort>
    {
        protected override string TypeName => "Bool";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // String 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_String : PropertyGetNode<StringParameterEdgePort>
    {
        protected override string TypeName => "String";

        protected override void SetOutputValue(object value)
        {
            Output.Value = value?.ToString() ?? "";
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = "";
        }
    }

    [Serializable]
    public class SetPropertyNode_String : PropertySetNode<StringParameterEdgePort>
    {
        protected override string TypeName => "String";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // Vector2 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_Vector2 : PropertyGetNode<Vector2ParameterEdgePort>
    {
        protected override string TypeName => "Vector2";

        protected override void SetOutputValue(object value)
        {
            Output.Value = value is Vector2 v ? v : Vector2.zero;
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = Vector2.zero;
        }
    }

    [Serializable]
    public class SetPropertyNode_Vector2 : PropertySetNode<Vector2ParameterEdgePort>
    {
        protected override string TypeName => "Vector2";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // Vector3 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_Vector3 : PropertyGetNode<Vector3ParameterEdgePort>
    {
        protected override string TypeName => "Vector3";

        protected override void SetOutputValue(object value)
        {
            Output.Value = value is Vector3 v ? v : Vector3.zero;
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = Vector3.zero;
        }
    }

    [Serializable]
    public class SetPropertyNode_Vector3 : PropertySetNode<Vector3ParameterEdgePort>
    {
        protected override string TypeName => "Vector3";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // GameObject 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_GameObject : PropertyGetNode<GameObjectParameterEdgePort>
    {
        protected override string TypeName => "GameObject";

        protected override void SetOutputValue(object value)
        {
            Output.Value = value as GameObject;
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = null;
        }
    }

    [Serializable]
    public class SetPropertyNode_GameObject : PropertySetNode<GameObjectParameterEdgePort>
    {
        protected override string TypeName => "GameObject";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // Transform 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_Transform : PropertyGetNode<TransformParameterEdgePort>
    {
        protected override string TypeName => "Transform";

        protected override void SetOutputValue(object value)
        {
            Output.Value = value as Transform;
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = null;
        }
    }

    [Serializable]
    public class SetPropertyNode_Transform : PropertySetNode<TransformParameterEdgePort>
    {
        protected override string TypeName => "Transform";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // Color 类型节点
    // ============================================================

    [Serializable]
    public class GetPropertyNode_Color : PropertyGetNode<ColorParameterEdgePort>
    {
        protected override string TypeName => "Color";

        protected override void SetOutputValue(object value)
        {
            Output.Value = value is Color c ? c : Color.white;
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = Color.white;
        }
    }

    [Serializable]
    public class SetPropertyNode_Color : PropertySetNode<ColorParameterEdgePort>
    {
        protected override string TypeName => "Color";

        protected override object GetInputValue()
        {
            return Input.Value;
        }
    }

    // ============================================================
    // 通用版本（Object 类型）
    // ============================================================

    /// <summary>
    /// 获取属性节点（通用版本）
    /// 用于从 BlueprintBehavior 中读取任意类型属性值
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// 1. 在蓝图中读取 Behavior 的字段或属性
    /// 2. 返回 object 类型，适合后续需要类型转换的场景
    /// 
    /// 提示：
    /// - 如果知道具体类型，推荐使用强类型版本（GetPropertyNode_Float 等）
    /// - 强类型版本提供更好的类型安全和编辑器支持
    /// </remarks>
    [Serializable]
    [NodeMenuItem("蓝图/获取属性/通用", NodeCategory.Blueprint, Description = "获取 Object 类型属性")]
    public class GetPropertyNode : PropertyGetNode<ObjectParameterEdgePort>
    {
        protected override string TypeName => "Object";

        protected override void SetOutputValue(object value)
        {
            Output.Value = value;
        }

        protected override void SetOutputValueDefault()
        {
            Output.Value = null;
        }

        // 重写 Title 以简化显示
        public override string Title => $"Get {PropertyName}";
    }

    /// <summary>
    /// 设置属性节点（通用版本）
    /// 用于向 BlueprintBehavior 中写入任意类型属性值
    /// </summary>
    /// <remarks>
    /// 使用场景：
    /// 1. 在蓝图中修改 Behavior 的字段或属性
    /// 2. 接收 object 类型，适合动态类型场景
    /// 
    /// 提示：
    /// - 如果知道具体类型，推荐使用强类型版本（SetPropertyNode_Float 等）
    /// - 强类型版本提供更好的类型安全和编辑器支持
    /// </remarks>
    [Serializable]
    [NodeMenuItem("蓝图/设置属性/通用", NodeCategory.Blueprint, Description = "设置 Object 类型属性")]
    public class SetPropertyNode : PropertySetNode<ObjectParameterEdgePort>
    {
        protected override string TypeName => "Object";

        protected override object GetInputValue()
        {
            return Input.Value;
        }

        // 重写 Title 以简化显示
        public override string Title => $"Set {PropertyName}";
    }

}
