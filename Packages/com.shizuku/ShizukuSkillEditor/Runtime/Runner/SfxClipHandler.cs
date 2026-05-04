using UnityEngine;
namespace Shizuku.SkillEditor
{
    public class SfxClipHandler : ClipHandler<SfxClipData>
    {
        protected override void OnEnterTyped(SfxClipData clip, SkillContext ctx)
        {
            if (clip.Clip == null || ctx.Caster == null) return;
            AudioSource.PlayClipAtPoint(clip.Clip, ctx.Caster.transform.position, clip.Volume);
        }
        protected override void OnExitTyped(SfxClipData clip, SkillContext ctx) { }
    }
}
