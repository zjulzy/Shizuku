using System;
using UnityEngine;

// ============================================================
// Get Variable 节点（零装箱版）
// ============================================================

[Serializable]
[NodeMenuItem("变量/获取/Int", NodeCategory.Variable, Description = "获取整数变量")]
public class GetVariableNode_Int : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public IntParameterEdgePort Output = new IntParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableInt(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = 0;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/Float", NodeCategory.Variable, Description = "获取浮点数变量")]
public class GetVariableNode_Float : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public FloatParameterEdgePort Output = new FloatParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableFloat(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = 0f;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/Bool", NodeCategory.Variable, Description = "获取布尔变量")]
public class GetVariableNode_Bool : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public BoolParameterEdgePort Output = new BoolParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableBool(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = false;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/String", NodeCategory.Variable, Description = "获取字符串变量")]
public class GetVariableNode_String : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public StringParameterEdgePort Output = new StringParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableString(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = "";
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/Vector2", NodeCategory.Variable, Description = "获取Vector2变量")]
public class GetVariableNode_Vector2 : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public Vector2ParameterEdgePort Output = new Vector2ParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableVector2(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = Vector2.zero;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/Vector3", NodeCategory.Variable, Description = "获取Vector3变量")]
public class GetVariableNode_Vector3 : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public Vector3ParameterEdgePort Output = new Vector3ParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableVector3(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = Vector3.zero;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/GameObject", NodeCategory.Variable, Description = "获取GameObject变量")]
public class GetVariableNode_GameObject : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public GameObjectParameterEdgePort Output = new GameObjectParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableGameObject(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = null;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/Transform", NodeCategory.Variable, Description = "获取Transform变量")]
public class GetVariableNode_Transform : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public TransformParameterEdgePort Output = new TransformParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableTransform(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = null;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/获取/Color", NodeCategory.Variable, Description = "获取Color变量")]
public class GetVariableNode_Color : ShizukuValueNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public ColorParameterEdgePort Output = new ColorParameterEdgePort { IsOut = true, Name = "Value" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);
    
    public override void GetOutputValues()
    {
        if (_parentGraph.TryGetVariableColor(VariableGUID, out var value))
        {
            Output.Value = value;
        }
        else
        {
            Output.Value = Color.white;
        }
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Get {variable.Name}" : "Get <未设置>";
    }
}

