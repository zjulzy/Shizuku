using System;
using UnityEngine;

// ============================================================
// Set Variable 节点（零装箱版）
// ============================================================

[Serializable]
[NodeMenuItem("变量/设置/Int", NodeCategory.Variable, Description = "设置整数变量")]
public class SetVariableNode_Int : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public IntParameterEdgePort Input = new IntParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableInt(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/Float", NodeCategory.Variable, Description = "设置浮点数变量")]
public class SetVariableNode_Float : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public FloatParameterEdgePort Input = new FloatParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableFloat(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/Bool", NodeCategory.Variable, Description = "设置布尔变量")]
public class SetVariableNode_Bool : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public BoolParameterEdgePort Input = new BoolParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableBool(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/String", NodeCategory.Variable, Description = "设置字符串变量")]
public class SetVariableNode_String : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public StringParameterEdgePort Input = new StringParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableString(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/Vector2", NodeCategory.Variable, Description = "设置Vector2变量")]
public class SetVariableNode_Vector2 : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public Vector2ParameterEdgePort Input = new Vector2ParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableVector2(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/Vector3", NodeCategory.Variable, Description = "设置Vector3变量")]
public class SetVariableNode_Vector3 : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public Vector3ParameterEdgePort Input = new Vector3ParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableVector3(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/GameObject", NodeCategory.Variable, Description = "设置GameObject变量")]
public class SetVariableNode_GameObject : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public GameObjectParameterEdgePort Input = new GameObjectParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableGameObject(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/Transform", NodeCategory.Variable, Description = "设置Transform变量")]
public class SetVariableNode_Transform : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public TransformParameterEdgePort Input = new TransformParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableTransform(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

[Serializable]
[NodeMenuItem("变量/设置/Color", NodeCategory.Variable, Description = "设置Color变量")]
public class SetVariableNode_Color : ShizukuRunnableNode
{
    [SerializeField]
    public string VariableGUID;
    
    [SerializeReference]
    public ColorParameterEdgePort Input = new ColorParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeField]
    private ChainPort _nextPort = new ChainPort { Name = "Next" };
    
    public override string Title => GetDisplayName();
    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);
    
    protected override void OnExecute()
    {
        _parentGraph.SetVariableColor(VariableGUID, Input.Value);
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    private string GetDisplayName()
    {
        var variable = _parentGraph?.GetVariableByGUID(VariableGUID);
        return variable != null ? $"Set {variable.Name}" : "Set <未设置>";
    }
}

