[NodeMenuItem("逻辑/循环", NodeCategory.Logic, Description = "循环执行节点（未实现）")]
public class ShizukuForNode : ShizukuRunnableNode
{
    protected override void OnExecute()
    {
    }

    protected override bool OnSelectNextNode(out string nextNodeGUID)
    {
        nextNodeGUID = null;
        return false;
    }
}