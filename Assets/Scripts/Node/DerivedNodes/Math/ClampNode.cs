using System;
using UnityEngine;

// ============================================================
// Clamp 节点（限制范围）
// ============================================================

[Serializable]
[NodeMenuItem("数学/限制范围/Int", NodeCategory.Math, Description = "限制整数在指定范围内")]
public class ClampNode_Int : ShizukuValueNode
{
    [SerializeReference]
    public IntParameterEdgePort InputValue = new IntParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeReference]
    public IntParameterEdgePort InputMin = new IntParameterEdgePort { IsOut = false, Name = "Min", DefaultValue = 0 };
    
    [SerializeReference]
    public IntParameterEdgePort InputMax = new IntParameterEdgePort { IsOut = false, Name = "Max", DefaultValue = 100 };
    
    [SerializeReference]
    public IntParameterEdgePort Output = new IntParameterEdgePort { IsOut = true, Name = "Result" };
    
    public override string Title => "Clamp (Int)";
    public override Color TitleBarColor => new Color(0.6f, 0.7f, 0.9f, 1f);
    
    public override void GetOutputValues()
    {
        Output.Value = Mathf.Clamp(InputValue.Value, InputMin.Value, InputMax.Value);
    }
}

[Serializable]
[NodeMenuItem("数学/限制范围/Float", NodeCategory.Math, Description = "限制浮点数在指定范围内")]
public class ClampNode_Float : ShizukuValueNode
{
    [SerializeReference]
    public FloatParameterEdgePort InputValue = new FloatParameterEdgePort { IsOut = false, Name = "Value" };
    
    [SerializeReference]
    public FloatParameterEdgePort InputMin = new FloatParameterEdgePort { IsOut = false, Name = "Min", DefaultValue = 0f };
    
    [SerializeReference]
    public FloatParameterEdgePort InputMax = new FloatParameterEdgePort { IsOut = false, Name = "Max", DefaultValue = 1f };
    
    [SerializeReference]
    public FloatParameterEdgePort Output = new FloatParameterEdgePort { IsOut = true, Name = "Result" };
    
    public override string Title => "Clamp (Float)";
    public override Color TitleBarColor => new Color(0.6f, 0.7f, 0.9f, 1f);
    
    public override void GetOutputValues()
    {
        Output.Value = Mathf.Clamp(InputValue.Value, InputMin.Value, InputMax.Value);
    }
}

