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
    
    [NonSerialized]
    public readonly List<ParameterEdgePort> SelfInputPorts = new List<ParameterEdgePort>();
    
    [NonSerialized]
    public readonly List<ShizukuNodeBase> DependentNodes = new List<ShizukuNodeBase>();

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
        
        SelfInputPorts.Clear();
        foreach (var field in fields)
        {
            if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(this) as ParameterEdgePort;
                if (port != null)
                {
                    if (!port.IsOut)
                        SelfInputPorts.Add(port);
                }
            }
        }
    }
    
    protected void GetInputValues()
    {
        foreach (var node in DependentNodes)
        {
            node.GetOutputValues();
        }

        foreach (var port in SelfInputPorts)
        {
            port.GetSourceValue();
        }
    }
    
    public virtual void GetOutputValues(){}
}