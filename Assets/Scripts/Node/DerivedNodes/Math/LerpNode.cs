using System;
using UnityEngine;

// ============================================================
// Lerp 节点（线性插值）
// ============================================================

[Serializable]
[NodeMenuItem("数学/线性插值/Float", NodeCategory.Math, Description = "浮点数线性插值")]
public class LerpNode_Float : ShizukuValueNode
{
    [SerializeReference]
    public FloatParameterEdgePort InputA = new FloatParameterEdgePort { IsOut = false, Name = "A", DefaultValue = 0f };
    
    [SerializeReference]
    public FloatParameterEdgePort InputB = new FloatParameterEdgePort { IsOut = false, Name = "B", DefaultValue = 1f };
    
    [SerializeReference]
    public FloatParameterEdgePort InputT = new FloatParameterEdgePort { IsOut = false, Name = "T", DefaultValue = 0.5f };
    
    [SerializeReference]
    public FloatParameterEdgePort Output = new FloatParameterEdgePort { IsOut = true, Name = "Result" };
    
    public override string Title => "Lerp (Float)";
    public override Color TitleBarColor => new Color(0.8f, 0.6f, 0.9f, 1f);
    
    public override void GetOutputValues()
    {
        Output.Value = Mathf.Lerp(InputA.Value, InputB.Value, InputT.Value);
    }
}

[Serializable]
[NodeMenuItem("数学/线性插值/Vector2", NodeCategory.Math, Description = "Vector2 线性插值")]
public class LerpNode_Vector2 : ShizukuValueNode
{
    [SerializeReference]
    public Vector2ParameterEdgePort InputA = new Vector2ParameterEdgePort { IsOut = false, Name = "A" };
    
    [SerializeReference]
    public Vector2ParameterEdgePort InputB = new Vector2ParameterEdgePort { IsOut = false, Name = "B" };
    
    [SerializeReference]
    public FloatParameterEdgePort InputT = new FloatParameterEdgePort { IsOut = false, Name = "T", DefaultValue = 0.5f };
    
    [SerializeReference]
    public Vector2ParameterEdgePort Output = new Vector2ParameterEdgePort { IsOut = true, Name = "Result" };
    
    public override string Title => "Lerp (Vector2)";
    public override Color TitleBarColor => new Color(0.8f, 0.6f, 0.9f, 1f);
    
    public override void GetOutputValues()
    {
        Output.Value = Vector2.Lerp(InputA.Value, InputB.Value, InputT.Value);
    }
}

[Serializable]
[NodeMenuItem("数学/线性插值/Vector3", NodeCategory.Math, Description = "Vector3 线性插值")]
public class LerpNode_Vector3 : ShizukuValueNode
{
    [SerializeReference]
    public Vector3ParameterEdgePort InputA = new Vector3ParameterEdgePort { IsOut = false, Name = "A" };
    
    [SerializeReference]
    public Vector3ParameterEdgePort InputB = new Vector3ParameterEdgePort { IsOut = false, Name = "B" };
    
    [SerializeReference]
    public FloatParameterEdgePort InputT = new FloatParameterEdgePort { IsOut = false, Name = "T", DefaultValue = 0.5f };
    
    [SerializeReference]
    public Vector3ParameterEdgePort Output = new Vector3ParameterEdgePort { IsOut = true, Name = "Result" };
    
    public override string Title => "Lerp (Vector3)";
    public override Color TitleBarColor => new Color(0.8f, 0.6f, 0.9f, 1f);
    
    public override void GetOutputValues()
    {
        Output.Value = Vector3.Lerp(InputA.Value, InputB.Value, InputT.Value);
    }
}

[Serializable]
[NodeMenuItem("数学/线性插值/Color", NodeCategory.Math, Description = "颜色线性插值")]
public class LerpNode_Color : ShizukuValueNode
{
    [SerializeReference]
    public ColorParameterEdgePort InputA = new ColorParameterEdgePort { IsOut = false, Name = "A", DefaultValue = Color.black };
    
    [SerializeReference]
    public ColorParameterEdgePort InputB = new ColorParameterEdgePort { IsOut = false, Name = "B", DefaultValue = Color.white };
    
    [SerializeReference]
    public FloatParameterEdgePort InputT = new FloatParameterEdgePort { IsOut = false, Name = "T", DefaultValue = 0.5f };
    
    [SerializeReference]
    public ColorParameterEdgePort Output = new ColorParameterEdgePort { IsOut = true, Name = "Result" };
    
    public override string Title => "Lerp (Color)";
    public override Color TitleBarColor => new Color(0.8f, 0.6f, 0.9f, 1f);
    
    public override void GetOutputValues()
    {
        Output.Value = Color.Lerp(InputA.Value, InputB.Value, InputT.Value);
    }
}

