

using UnityEngine;

public class ShizukuRootNode : ShizukuNodeBase
{
    public override string Title => "Root Node";
    public override bool SupportControlInput => false;
    public override bool SupportControlOutput => true;
    
    public override Color TitleBarColor => new Color(0.8f, 0.2f, 0.2f, 1f);
   

    protected override void OnExecute()
    {
    }
}