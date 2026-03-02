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

    public virtual object GetSelfValue()
    {
        return null;
    }
    
}

// 输入输出值的接口
[Serializable]
public class ParameterEdgePort<T>: ParameterEdgePort
{
    public T Value = default;
    
    public T DefaultValue = default;
    
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
            // TODO: 这里装箱了，后续需要优化
            var differentValue = DifferentTypeConnectedPort.GetSelfValue();
            if (differentValue != null)
            {
                Value = (T)Convert.ChangeType(differentValue, typeof(T));
            }
        }
        else
        {
            Value = DefaultValue;
        }
    }

    public override object GetSelfValue()
    {
        return Value;
    }
}

[Serializable]
public class IntParameterEdgePort : ParameterEdgePort<int>
{
}

[Serializable]
public class StringParameterEdgePort : ParameterEdgePort<string>
{
}

[Serializable]
public class FloatParameterEdgePort : ParameterEdgePort<float>
{
}

[Serializable]
public class BoolParameterEdgePort : ParameterEdgePort<bool>
{
}

[Serializable]
public class ObjectParameterEdgePort : ParameterEdgePort<object>
{
}

[Serializable]
public class Vector2ParameterEdgePort : ParameterEdgePort<Vector2>
{
}

[Serializable]
public class Vector3ParameterEdgePort : ParameterEdgePort<Vector3>
{
}

[Serializable]
public class GameObjectParameterEdgePort : ParameterEdgePort<GameObject>
{
}

[Serializable]
public class TransformParameterEdgePort : ParameterEdgePort<Transform>
{
}

[Serializable]
public class ColorParameterEdgePort : ParameterEdgePort<Color>
{
}

// 指定执行顺序的接口
[Serializable]
public class ChainPort
{
    public string NextNodeGuid = null;
    public string Name;
}

