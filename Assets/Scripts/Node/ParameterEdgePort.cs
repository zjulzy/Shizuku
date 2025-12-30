using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


[Serializable]
public class ParameterEdgePort{
    [SerializeField]
    public string Name;
    [SerializeField]
    public string InputEdgeGUID;

    [SerializeField] 
    public bool IsOut;
    
    [NonSerialized]
    public ParameterEdgePort SameTypeConnectedPort;
    
    [NonSerialized]
    public ParameterEdgePort DifferentTypeConnectedPort;
    
    public virtual void GetSourceValue()
    {
        
    }
    
}

// 输入输出值的接口
[Serializable]
public class ParameterEdgePort<T>: ParameterEdgePort
{
    public T Value = default;
    
    public Type GetValueType => typeof(T);
    
    public override void GetSourceValue()
    {
        if (SameTypeConnectedPort != null)
        {
            var port = SameTypeConnectedPort as ParameterEdgePort<T>;
            Value = port.Value;
        }
        else if (DifferentTypeConnectedPort != null)
        {
            // 类型转换逻辑
            var differentValue = DifferentTypeConnectedPort as ParameterEdgePort<object>;
            if (differentValue != null)
            {
                Value = (T)Convert.ChangeType(differentValue.Value, typeof(T));
            }
        }
    }
}

[Serializable]
public class IntParameterEdgePort : ParameterEdgePort<int>
{
}

// 指定执行顺序的接口
public class ChainPort
{
}

