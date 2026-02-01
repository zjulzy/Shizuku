using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BlueprintEventNode : ShizukuRootNode
{
    [SerializeField]
    public string EventName = "OnEvent";
    
    [SerializeField]
    public List<EventParameter> EventParameters = new List<EventParameter>();
    
    public override string Title => $"Event: {EventName}";
    public override Color TitleBarColor => new Color(1f, 0.3f, 0.3f, 1f);
    
    public void TriggerEvent(params object[] args)
    {
        for (int i = 0; i < EventParameters.Count && i < args.Length; i++)
        {
            EventParameters[i].SetValue(args[i]);
        }
        
        StartExcute();
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
        
        if (OutputPort != null)
        {
            SetPortValue(OutputPort, value);
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
                Debug.LogWarning($"Failed to set value for event parameter {Name}");
            }
        }
    }
}

