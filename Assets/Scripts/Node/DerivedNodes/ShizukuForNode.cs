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