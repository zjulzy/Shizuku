using UnityEngine;

public class ShizikuAddOneNode : ShizukuRunnableNode
{
    public override string Title => "Add One Node";

    [SerializeReference]
    private IntParameterEdgePort _parameter = new() { IsOut = false, Name = "parameter" };

    [SerializeReference] 
    private IntParameterEdgePort _parameterResult = new() { IsOut = true, Name = "result" };
    
    [SerializeField]
    private ChainPort _nextPort = new() {Name = "next" };

    protected override void OnExecute()
    {
        _parameterResult.Value = _parameter.Value + 1;
        Debug.Log($"帧号:{Time.frameCount} 执行节点 {GUID}  参数:{_parameter.Value}");
    }

    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
}