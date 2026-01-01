using UnityEngine;

public class ShizukuLogNode : ShizukuNodeBase
{
    public override string Title => "Log Node";

    [SerializeReference]
    private StringParameterEdgePort Message = new() { IsOut = false, Name = "message" };

    protected override void OnExecute()
    {
        Debug.Log($"帧号:{Time.frameCount} 执行节点 {GUID} 日志:{Message.Value}");
    }
}