using System;
using UnityEngine;

// ============================================================
// Set Variable 节点（零装箱版 - 基于泛型中间层）
// ============================================================

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    [NodeMenuItem("变量/设置/Int", NodeCategory.Variable, Description = "设置整数变量")]
    public class SetVariableNode_Int : SetVariableNodeBase<IntParameterEdgePort, int>
    {
        public override VariableType TargetVariableType => VariableType.Int;
        protected override void SetVariable(string guid, int value) => _parentGraph.SetVariableInt(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/Float", NodeCategory.Variable, Description = "设置浮点数变量")]
    public class SetVariableNode_Float : SetVariableNodeBase<FloatParameterEdgePort, float>
    {
        public override VariableType TargetVariableType => VariableType.Float;
        protected override void SetVariable(string guid, float value) => _parentGraph.SetVariableFloat(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/Bool", NodeCategory.Variable, Description = "设置布尔变量")]
    public class SetVariableNode_Bool : SetVariableNodeBase<BoolParameterEdgePort, bool>
    {
        public override VariableType TargetVariableType => VariableType.Bool;
        protected override void SetVariable(string guid, bool value) => _parentGraph.SetVariableBool(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/String", NodeCategory.Variable, Description = "设置字符串变量")]
    public class SetVariableNode_String : SetVariableNodeBase<StringParameterEdgePort, string>
    {
        public override VariableType TargetVariableType => VariableType.String;
        protected override void SetVariable(string guid, string value) => _parentGraph.SetVariableString(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/Vector2", NodeCategory.Variable, Description = "设置Vector2变量")]
    public class SetVariableNode_Vector2 : SetVariableNodeBase<Vector2ParameterEdgePort, Vector2>
    {
        public override VariableType TargetVariableType => VariableType.Vector2;
        protected override void SetVariable(string guid, Vector2 value) => _parentGraph.SetVariableVector2(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/Vector3", NodeCategory.Variable, Description = "设置Vector3变量")]
    public class SetVariableNode_Vector3 : SetVariableNodeBase<Vector3ParameterEdgePort, Vector3>
    {
        public override VariableType TargetVariableType => VariableType.Vector3;
        protected override void SetVariable(string guid, Vector3 value) => _parentGraph.SetVariableVector3(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/GameObject", NodeCategory.Variable, Description = "设置GameObject变量")]
    public class SetVariableNode_GameObject : SetVariableNodeBase<GameObjectParameterEdgePort, GameObject>
    {
        public override VariableType TargetVariableType => VariableType.GameObject;
        protected override void SetVariable(string guid, GameObject value) => _parentGraph.SetVariableGameObject(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/Transform", NodeCategory.Variable, Description = "设置Transform变量")]
    public class SetVariableNode_Transform : SetVariableNodeBase<TransformParameterEdgePort, Transform>
    {
        public override VariableType TargetVariableType => VariableType.Transform;
        protected override void SetVariable(string guid, Transform value) => _parentGraph.SetVariableTransform(guid, value);
    }

    [Serializable]
    [NodeMenuItem("变量/设置/Color", NodeCategory.Variable, Description = "设置Color变量")]
    public class SetVariableNode_Color : SetVariableNodeBase<ColorParameterEdgePort, Color>
    {
        public override VariableType TargetVariableType => VariableType.Color;
        protected override void SetVariable(string guid, Color value) => _parentGraph.SetVariableColor(guid, value);
    }

}
