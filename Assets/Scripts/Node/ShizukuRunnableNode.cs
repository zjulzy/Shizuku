using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public abstract class ShizukuRunnableNode : ShizukuNormalNode
{
    

    public sealed override bool SupportControlInput => true;
    public sealed override bool SupportControlOutput => true;

    [NonSerialized]
    public readonly List<ShizukuNodeBase> DependentNodes = new List<ShizukuNodeBase>();
    
    [NonSerialized]
    public readonly List<ParameterEdgePort> SelfInputPorts = new List<ParameterEdgePort>();

    public override void Init(ShizukuGraphBase parentGraph)
    {
        base.Init(parentGraph);

        var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

    public void Execute()
    {
        GetInputValues();
        OnExecute();

        if (OnSelectNextNode(out var guid))
        {
            if (_parentGraph.Guid2NodeMap.TryGetValue(guid, out var nextNode))
            {
                if (nextNode is ShizukuRunnableNode runnable)
                {
                    runnable.Execute();
                }
                else
                {
                    Debug.LogError($"Next node is not a runnable node: {guid}");
                }
            }
            else
            {
                Debug.LogError($"Next node not found: {guid}");
            }
        }
    }

    private void GetInputValues()
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

    protected abstract void OnExecute();
    protected abstract bool OnSelectNextNode(out string nextNodeGUID);
}