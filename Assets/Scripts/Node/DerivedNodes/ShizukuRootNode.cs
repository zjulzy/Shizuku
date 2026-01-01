

public class ShizukuRootNode : ShizukuNodeBase
{
    public override string Title => "Root Node";
    public override bool SupportControlInput => false;
    public override bool SupportControlOutput => true;

    protected override void OnExecute()
    {
    }
}