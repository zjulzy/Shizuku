using UnityEngine;

public class ShizukuIfNode : ShizukuRunnableNode
{
    public override string Title => "If";
    public override Color TitleBarColor => Color.cyan;
    
    [SerializeReference]
    private BoolParameterEdgePort _condition = new() { IsOut = false, Name = "condition" };
    
    [SerializeField]
    private ChainPort _truePort = new() {Name = "true" };
    
    [SerializeField]
    private ChainPort _falsePort = new() {Name = "false" };

    protected override void OnExecute()
    {
    }

    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _condition.Value ? _truePort.NextNodeGuid : _falsePort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
}