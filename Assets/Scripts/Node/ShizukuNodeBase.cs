using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public abstract class ShizukuNodeBase
{
    [SerializeField]
    public string GUID = System.Guid.NewGuid().ToString();
    
    [SerializeField]
    public float4 PositionAndSize;

    public virtual string Title => "No Title";
    public virtual Color TitleBarColor => Color.gray;
    
    public virtual bool SupportControlInput => true;
    public virtual bool SupportControlOutput => true;
    
    [NonSerialized]
    protected ShizukuGraphBase _parentGraph;
    
    [NonSerialized]
    public readonly List<ParameterEdgePort> SelfOutputPorts = new List<ParameterEdgePort>();

    public virtual void Init(ShizukuGraphBase parentGraph)
    {
        _parentGraph = parentGraph;
        var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        SelfOutputPorts.Clear();
        
        foreach (var field in fields)
        {
            if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(this) as ParameterEdgePort;
                if (port != null)
                {
                    if (port.IsOut)
                        SelfOutputPorts.Add(port);
                }
            }
        }
    }
    
    public virtual void GetOutputValues(){}
}