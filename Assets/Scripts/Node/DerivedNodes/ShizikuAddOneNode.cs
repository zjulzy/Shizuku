using UnityEngine;


public class ShizikuAddOneNode : ShizukuNodeBase
{
    public override string Title => "Add One Node";

    [SerializeReference]
    private IntParameterEdgePort Parameter = new() { IsOut = false, Name = "parameter" };

    [SerializeReference] 
    private IntParameterEdgePort ParameterResult = new() { IsOut = true, Name = "result" };

    protected override void OnExecute()
    {
        ParameterResult.Value = Parameter.Value + 1; // 示例逻辑：参数加1
        Debug.Log($"帧号:{Time.frameCount} 执行节点 {GUID}  参数:{Parameter.Value}");
    }
}