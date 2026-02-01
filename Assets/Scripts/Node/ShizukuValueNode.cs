using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public abstract class ShizukuValueNode : ShizukuNodeBase
{
    public override bool SupportControlInput => false;
    public override bool SupportControlOutput => false;
    
    public override void GetOutputValues()
    {
        Debug.LogError("Value node cannot get output values");
    }
}