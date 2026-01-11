public class ShizukuForNode: ShizukuNodeBase
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