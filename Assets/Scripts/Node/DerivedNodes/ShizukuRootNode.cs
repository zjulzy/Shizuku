

using UnityEngine;

public class ShizukuRootNode : ShizukuNodeBase
{
    public override string Title => "Root Node";
    public override bool SupportControlInput => false;
    
    public override Color TitleBarColor => new Color(0.8f, 0.2f, 0.2f, 1f);
   
    [SerializeField]
    private ChainPort _nextPort = new() {Name = "next" };

    protected override void OnExecute()
    {
    }
    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = _nextPort.NextNodeGuid;
        return !string.IsNullOrEmpty(nextNodeGUID);
    }
}