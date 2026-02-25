using System;
using UnityEngine;

/// <summary>
/// 加法节点 - Int 版本
/// </summary>
[Serializable]
public class AddNode_Int : ShizukuValueNode
{
    public override string Title => "Add (Int)";

    [SerializeReference]
    private IntParameterEdgePort _a = new() { IsOut = false, Name = "A" };

    [SerializeReference]
    private IntParameterEdgePort _b = new() { IsOut = false, Name = "B" };

    [SerializeReference]
    private IntParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        _result.Value = _a.Value + _b.Value;
    }
}

/// <summary>
/// 加法节点 - Float 版本
/// </summary>
[Serializable]
public class AddNode_Float : ShizukuValueNode
{
    public override string Title => "Add (Float)";

    [SerializeReference]
    private FloatParameterEdgePort _a = new() { IsOut = false, Name = "A" };

    [SerializeReference]
    private FloatParameterEdgePort _b = new() { IsOut = false, Name = "B" };

    [SerializeReference]
    private FloatParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        _result.Value = _a.Value + _b.Value;
    }
}

/// <summary>
/// 加法节点 - Vector2 版本
/// </summary>
[Serializable]
public class AddNode_Vector2 : ShizukuValueNode
{
    public override string Title => "Add (Vector2)";

    [SerializeReference]
    private Vector2ParameterEdgePort _a = new() { IsOut = false, Name = "A" };

    [SerializeReference]
    private Vector2ParameterEdgePort _b = new() { IsOut = false, Name = "B" };

    [SerializeReference]
    private Vector2ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        _result.Value = _a.Value + _b.Value;
    }
}

/// <summary>
/// 加法节点 - Vector3 版本
/// </summary>
[Serializable]
[NodeMenuItem("数学/加法/三维向量", NodeCategory.Math, Description = "三维向量加法")]
public class AddNode_Vector3 : ShizukuValueNode
{
    public override string Title => "Add (Vector3)";

    [SerializeReference]
    private Vector3ParameterEdgePort _a = new() { IsOut = false, Name = "A" };

    [SerializeReference]
    private Vector3ParameterEdgePort _b = new() { IsOut = false, Name = "B" };

    [SerializeReference]
    private Vector3ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        _result.Value = _a.Value + _b.Value;
    }
}

