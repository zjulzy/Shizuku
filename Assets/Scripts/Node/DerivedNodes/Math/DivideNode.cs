using System;
using UnityEngine;

/// <summary>
/// 除法节点 - Int 版本
/// </summary>
[Serializable]
public class DivideNode_Int : ShizukuValueNode
{
    public override string Title => "Divide (Int)";

    [SerializeReference]
    private IntParameterEdgePort _a = new() { IsOut = false, Name = "A" };

    [SerializeReference]
    private IntParameterEdgePort _b = new() { IsOut = false, Name = "B" };

    [SerializeReference]
    private IntParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        if (_b.Value == 0)
        {
            Debug.LogWarning($"[DivideNode_Int] 除数为 0，返回 0");
            _result.Value = 0;
        }
        else
        {
            _result.Value = _a.Value / _b.Value;
        }
    }
}

/// <summary>
/// 除法节点 - Float 版本
/// </summary>
[Serializable]
public class DivideNode_Float : ShizukuValueNode
{
    public override string Title => "Divide (Float)";

    [SerializeReference]
    private FloatParameterEdgePort _a = new() { IsOut = false, Name = "A" };

    [SerializeReference]
    private FloatParameterEdgePort _b = new() { IsOut = false, Name = "B" };

    [SerializeReference]
    private FloatParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        if (Mathf.Approximately(_b.Value, 0f))
        {
            Debug.LogWarning($"[DivideNode_Float] 除数接近 0，返回 0");
            _result.Value = 0f;
        }
        else
        {
            _result.Value = _a.Value / _b.Value;
        }
    }
}

/// <summary>
/// 除法节点 - Vector2 版本（标量除法）
/// </summary>
[Serializable]
public class DivideNode_Vector2 : ShizukuValueNode
{
    public override string Title => "Divide (Vector2)";

    [SerializeReference]
    private Vector2ParameterEdgePort _vector = new() { IsOut = false, Name = "Vector" };

    [SerializeReference]
    private FloatParameterEdgePort _scalar = new() { IsOut = false, Name = "Scalar" };

    [SerializeReference]
    private Vector2ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        if (Mathf.Approximately(_scalar.Value, 0f))
        {
            Debug.LogWarning($"[DivideNode_Vector2] 除数接近 0，返回 Vector2.zero");
            _result.Value = Vector2.zero;
        }
        else
        {
            _result.Value = _vector.Value / _scalar.Value;
        }
    }
}

/// <summary>
/// 除法节点 - Vector3 版本（标量除法）
/// </summary>
[Serializable]
public class DivideNode_Vector3 : ShizukuValueNode
{
    public override string Title => "Divide (Vector3)";

    [SerializeReference]
    private Vector3ParameterEdgePort _vector = new() { IsOut = false, Name = "Vector" };

    [SerializeReference]
    private FloatParameterEdgePort _scalar = new() { IsOut = false, Name = "Scalar" };

    [SerializeReference]
    private Vector3ParameterEdgePort _result = new() { IsOut = true, Name = "Result" };

    public override void GetOutputValues()
    {
        GetInputValues();
        if (Mathf.Approximately(_scalar.Value, 0f))
        {
            Debug.LogWarning($"[DivideNode_Vector3] 除数接近 0，返回 Vector3.zero");
            _result.Value = Vector3.zero;
        }
        else
        {
            _result.Value = _vector.Value / _scalar.Value;
        }
    }
}

