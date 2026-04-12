using System;
using UnityEngine;

// ============================================================
// Get Variable 节点（零装箱版 - 基于泛型中间层）
// ============================================================

namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    [NodeMenuItem("变量/获取/Int", NodeCategory.Variable, Description = "获取整数变量")]
    public class GetVariableNode_Int : GetVariableNodeBase<IntParameterEdgePort, int>
    {
        public override VariableType TargetVariableType => VariableType.Int;
        protected override bool TryGetVariable(string guid, out int value) => RootGraph.TryGetVariableInt(guid, out value);
        protected override int GetDefaultValue() => 0;
    }

    [Serializable]
    [NodeMenuItem("变量/获取/Float", NodeCategory.Variable, Description = "获取浮点数变量")]
    public class GetVariableNode_Float : GetVariableNodeBase<FloatParameterEdgePort, float>
    {
        public override VariableType TargetVariableType => VariableType.Float;
        protected override bool TryGetVariable(string guid, out float value) => RootGraph.TryGetVariableFloat(guid, out value);
        protected override float GetDefaultValue() => 0f;
    }

    [Serializable]
    [NodeMenuItem("变量/获取/Bool", NodeCategory.Variable, Description = "获取布尔变量")]
    public class GetVariableNode_Bool : GetVariableNodeBase<BoolParameterEdgePort, bool>
    {
        public override VariableType TargetVariableType => VariableType.Bool;
        protected override bool TryGetVariable(string guid, out bool value) => RootGraph.TryGetVariableBool(guid, out value);
        protected override bool GetDefaultValue() => false;
    }

    [Serializable]
    [NodeMenuItem("变量/获取/String", NodeCategory.Variable, Description = "获取字符串变量")]
    public class GetVariableNode_String : GetVariableNodeBase<StringParameterEdgePort, string>
    {
        public override VariableType TargetVariableType => VariableType.String;
        protected override bool TryGetVariable(string guid, out string value) => RootGraph.TryGetVariableString(guid, out value);
        protected override string GetDefaultValue() => "";
    }

    [Serializable]
    [NodeMenuItem("变量/获取/Vector2", NodeCategory.Variable, Description = "获取Vector2变量")]
    public class GetVariableNode_Vector2 : GetVariableNodeBase<Vector2ParameterEdgePort, Vector2>
    {
        public override VariableType TargetVariableType => VariableType.Vector2;
        protected override bool TryGetVariable(string guid, out Vector2 value) => RootGraph.TryGetVariableVector2(guid, out value);
        protected override Vector2 GetDefaultValue() => Vector2.zero;
    }

    [Serializable]
    [NodeMenuItem("变量/获取/Vector3", NodeCategory.Variable, Description = "获取Vector3变量")]
    public class GetVariableNode_Vector3 : GetVariableNodeBase<Vector3ParameterEdgePort, Vector3>
    {
        public override VariableType TargetVariableType => VariableType.Vector3;
        protected override bool TryGetVariable(string guid, out Vector3 value) => RootGraph.TryGetVariableVector3(guid, out value);
        protected override Vector3 GetDefaultValue() => Vector3.zero;
    }

    [Serializable]
    [NodeMenuItem("变量/获取/GameObject", NodeCategory.Variable, Description = "获取GameObject变量")]
    public class GetVariableNode_GameObject : GetVariableNodeBase<GameObjectParameterEdgePort, GameObject>
    {
        public override VariableType TargetVariableType => VariableType.GameObject;
        protected override bool TryGetVariable(string guid, out GameObject value) => RootGraph.TryGetVariableGameObject(guid, out value);
        protected override GameObject GetDefaultValue() => null;
    }

    [Serializable]
    [NodeMenuItem("变量/获取/Transform", NodeCategory.Variable, Description = "获取Transform变量")]
    public class GetVariableNode_Transform : GetVariableNodeBase<TransformParameterEdgePort, Transform>
    {
        public override VariableType TargetVariableType => VariableType.Transform;
        protected override bool TryGetVariable(string guid, out Transform value) => RootGraph.TryGetVariableTransform(guid, out value);
        protected override Transform GetDefaultValue() => null;
    }

    [Serializable]
    [NodeMenuItem("变量/获取/Color", NodeCategory.Variable, Description = "获取Color变量")]
    public class GetVariableNode_Color : GetVariableNodeBase<ColorParameterEdgePort, Color>
    {
        public override VariableType TargetVariableType => VariableType.Color;
        protected override bool TryGetVariable(string guid, out Color value) => RootGraph.TryGetVariableColor(guid, out value);
        protected override Color GetDefaultValue() => Color.white;
    }

}
