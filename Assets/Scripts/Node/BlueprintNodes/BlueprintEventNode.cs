using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

[Serializable]
public class BlueprintEventNode : ShizukuRootNode
{
    [SerializeField]
    public string EventName = "OnEvent";
    
    [SerializeField]
    public List<EventParameter> EventParameters = new List<EventParameter>();
    
    public override string Title => $"Event: {EventName}";
    public override Color TitleBarColor => IsValid() ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.8f, 0.4f, 0f, 1f);
    
    public override void Init(ShizukuGraphBase parentGraph)
    {
        base.Init(parentGraph);
        
        foreach (var param in EventParameters)
        {
            if (param.OutputPort != null && !SelfOutputPorts.Contains(param.OutputPort))
            {
                SelfOutputPorts.Add(param.OutputPort);
            }
        }
    }
    
    public override void GetOutputValues()
    {
        base.GetOutputValues();
        
        foreach (var param in EventParameters)
        {
            if (param.OutputPort != null && param.Value != null)
            {
                SetPortValue(param.OutputPort, param.Value);
            }
        }
    }
    
    private void SetPortValue(ParameterEdgePort port, object value)
    {
        var portType = port.GetType();
        var valueField = portType.GetField("Value");
        
        if (valueField != null)
        {
            try
            {
                var convertedValue = Convert.ChangeType(value, valueField.FieldType);
                valueField.SetValue(port, convertedValue);
            }
            catch
            {
                Debug.LogWarning($"Failed to set value for event parameter in GetOutputValues");
            }
        }
    }
    
    public void TriggerEvent(params object[] args)
    {
        if (EventParameters.Count != args.Length)
        {
            Debug.LogWarning($"Event '{EventName}' parameter count mismatch. Expected {EventParameters.Count}, got {args.Length}");
        }
        
        for (int i = 0; i < EventParameters.Count && i < args.Length; i++)
        {
            EventParameters[i].SetValue(args[i]);
        }
        
        StartExcute();
    }
    
    public bool IsValid()
    {
        if (_parentGraph == null) return true;
        
        var methodInfo = FindMatchingMethod();
        if (methodInfo == null)
        {
            return false;
        }
        
        var parameters = methodInfo.GetParameters();
        if (parameters.Length != EventParameters.Count)
        {
            return false;
        }
        
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.Name != EventParameters[i].TypeName)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public string GetValidationMessage()
    {
        if (_parentGraph == null) return "图未初始化";
        
        var methodInfo = FindMatchingMethod();
        if (methodInfo == null)
        {
            return $"未找到事件方法 '{EventName}'，可能已被删除或重命名";
        }
        
        var parameters = methodInfo.GetParameters();
        if (parameters.Length != EventParameters.Count)
        {
            return $"参数数量不匹配：期望 {parameters.Length} 个，当前 {EventParameters.Count} 个";
        }
        
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.Name != EventParameters[i].TypeName)
            {
                return $"参数 '{EventParameters[i].Name}' 类型不匹配：期望 {parameters[i].ParameterType.Name}，当前 {EventParameters[i].TypeName}";
            }
        }
        
        return "有效";
    }
    
    private MethodInfo FindMatchingMethod()
    {
        var behaviorType = GetBehaviorType();
        if (behaviorType == null) return null;
        
        var methods = behaviorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<BlueprintOverridableAttribute>();
            if (attr != null)
            {
                var methodEventName = attr.EventName ?? method.Name;
                if (methodEventName == EventName)
                {
                    return method;
                }
            }
        }
        
        return null;
    }
    
    private Type GetBehaviorType()
    {
        if (_parentGraph == null) return null;
        
        var graphType = _parentGraph.GetType();
        while (graphType != null && graphType != typeof(object))
        {
            if (graphType.IsGenericType)
            {
                var genericDef = graphType.GetGenericTypeDefinition();
                if (genericDef.Name.StartsWith("ShizukuBluePrint"))
                {
                    var genericArgs = graphType.GetGenericArguments();
                    if (genericArgs.Length > 0)
                    {
                        return genericArgs[0];
                    }
                }
            }
            graphType = graphType.BaseType;
        }
        return null;
    }
}

[Serializable]
public class EventParameter
{
    [SerializeField]
    public string Name;
    
    [SerializeField]
    public string TypeName;
    
    [NonSerialized]
    public object Value;
    
    [SerializeReference]
    public ParameterEdgePort OutputPort;
    
    public void SetValue(object value)
    {
        Value = value;
    }
}

