using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 蓝图事件节点
/// 用于定义可被Behavior触发的事件入口点
/// 类似于UE蓝图中的Event节点
/// </summary>
/// <remarks>
/// 使用场景：
/// 1. 在蓝图中创建此节点，设置事件名称（如"OnTakeDamage"）
/// 2. 在Behavior中调用 ExecuteBlueprintEvent("OnTakeDamage", damage)
/// 3. 蓝图会从此节点开始执行后续节点链
/// 
/// 特点：
/// - 作为蓝图执行的入口点，不支持控制流输入
/// - 支持多个输出参数端口，对应事件的参数
/// - 每个事件节点在图中应该是唯一的
/// </remarks>
[Serializable]
public class BlueprintEventNode : ShizukuNodeBase
{
    /// <summary>
    /// 事件名称（必须与Behavior中调用的名称匹配）
    /// </summary>
    [SerializeField]
    public string EventName = "OnEvent";
    
    /// <summary>
    /// 事件参数定义
    /// 用于在编辑器中配置参数
    /// </summary>
    [SerializeField]
    public List<EventParameter> EventParameters = new List<EventParameter>();
    
    /// <summary>
    /// 绑定的Behavior实例
    /// </summary>
    [NonSerialized]
    private BlueprintBehavior _boundBehavior;
    
    /// <summary>
    /// 下一个节点的ChainPort
    /// </summary>
    [SerializeField]
    private ChainPort _nextPort = new() { Name = "Execute" };
    
    public override string Title => $"Event: {EventName}";
    public override Color TitleBarColor => new Color(1f, 0.3f, 0.3f, 1f); // 红色，表示事件
    
    // 事件节点不支持控制流输入（它是入口点）
    public override bool SupportControlInput => false;
    public override bool SupportControlOutput => true;
    
    /// <summary>
    /// 绑定到Behavior实例
    /// </summary>
    public void BindToBehavior(BlueprintBehavior behavior)
    {
        _boundBehavior = behavior;
    }
    
    /// <summary>
    /// 触发事件执行
    /// 由蓝图系统调用，传入事件参数
    /// </summary>
    public void TriggerEvent(params object[] args)
    {
        // 将参数值赋给输出端口
        for (int i = 0; i < EventParameters.Count && i < args.Length; i++)
        {
            EventParameters[i].SetValue(args[i]);
        }
        
        // 开始执行节点链
        Execute();
    }
    
    protected override void OnExecute()
    {
        // 事件节点本身不执行任何逻辑
        // 只是作为入口点，将参数传递给后续节点
    }
    
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
    
    public override void GetOutputValues()
    {
        // 输出当前事件参数值
        base.GetOutputValues();
    }
}

/// <summary>
/// 事件参数定义
/// 用于在蓝图中定义事件的输出参数
/// </summary>
[Serializable]
public class EventParameter
{
    /// <summary>
    /// 参数名称
    /// </summary>
    [SerializeField]
    public string Name;
    
    /// <summary>
    /// 参数类型（用于编辑器显示）
    /// </summary>
    [SerializeField]
    public string TypeName;
    
    /// <summary>
    /// 参数值（运行时设置）
    /// </summary>
    [NonSerialized]
    public object Value;
    
    /// <summary>
    /// 对应的输出端口（根据类型动态创建）
    /// 这里使用object类型的端口作为示例
    /// 实际使用时应该根据TypeName创建对应类型的端口
    /// </summary>
    [SerializeReference]
    public ParameterEdgePort OutputPort;
    
    /// <summary>
    /// 设置参数值（由事件触发时调用）
    /// </summary>
    public void SetValue(object value)
    {
        Value = value;
        
        // 根据端口类型设置值
        if (OutputPort != null)
        {
            SetPortValue(OutputPort, value);
        }
    }
    
    /// <summary>
    /// 通过反射设置端口值
    /// </summary>
    private void SetPortValue(ParameterEdgePort port, object value)
    {
        var portType = port.GetType();
        var valueField = portType.GetField("Value");
        
        if (valueField != null)
        {
            try
            {
                // 尝试类型转换
                var convertedValue = Convert.ChangeType(value, valueField.FieldType);
                valueField.SetValue(port, convertedValue);
            }
            catch
            {
                Debug.LogWarning($"Failed to set value for event parameter {Name}");
            }
        }
    }
}

