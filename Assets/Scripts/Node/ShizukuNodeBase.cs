using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEditorInternal;
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
    
    // 标定是否支持控制链输入输出
    public virtual bool SupportControlInput => true;
    public virtual bool SupportControlOutput => true;
    
    [NonSerialized]
    private ShizukuGraphBase _parentGraph;
    
    [NonSerialized]
    public readonly List<ShizukuNodeBase> DependentNodes = new List<ShizukuNodeBase>();
    
    [NonSerialized]
    public readonly List<ParameterEdgePort> SelfInputPorts = new List<ParameterEdgePort>();
    
    [NonSerialized]
    public readonly List<ParameterEdgePort> SelfOutputPorts = new List<ParameterEdgePort>();
    
    [NonSerialized]
    public Dictionary<string, ChainPort> ChainPorts = new Dictionary<string, ChainPort>();
    
    private int _executedFrame = -1;
    

    // 在运行时初始化调用
    public void Init(ShizukuGraphBase parentGraph)
    {
        _parentGraph = parentGraph;
        // 通过反射获取自身的输入输出端口
        SelfInputPorts.Clear();
        SelfOutputPorts.Clear();
        var fields = this.GetType().GetFields();
        foreach (var field in fields)
        {
            if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(this) as ParameterEdgePort;
                if (port != null)
                {
                    if (!port.IsOut)
                    {
                        SelfInputPorts.Add(port);
                    }
                    else
                    {
                        SelfOutputPorts.Add(port);
                    }
                }
            }
        }
        // 通过反射初始化_chainPorts
        foreach (var field in fields)
        {
            if (typeof(ChainPort).IsAssignableFrom(field.FieldType))
            {
                var port = field.GetValue(this) as ChainPort;
                if (port != null)
                {
                    ChainPorts[port.Name] = port;
                }
            }
        }
    }
    
    public void Execute()
    {
        GetInputValues();
        OnExecute();

        _executedFrame = Time.frameCount;
        // 执行下一个节点
        if (OnSelectNextNode(out var guid))
        {
            if (_parentGraph.Guid2NodeMap.TryGetValue(guid, out var nextNode))
            {
                nextNode.Execute();
            }
        }
    }

    private void GetInputValues()
    {
        foreach (var node in DependentNodes)
        {
            node.GetOutputValues();
        }
        SelfInputPorts.ForEach(port =>
        {
            port.GetSourceValue();
        });
        
    }
    
    public virtual void GetOutputValues()
    {
        
    }

    #region 子类实现的生命周期

    protected abstract void OnExecute();
    protected abstract bool OnSelectNextNode(out string nextNodeGUID);

    #endregion
}