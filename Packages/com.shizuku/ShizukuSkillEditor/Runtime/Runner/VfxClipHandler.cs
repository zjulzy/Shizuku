using UnityEngine;
namespace Shizuku.SkillEditor
{
    public class VfxClipHandler : ClipHandler<VfxClipData>
    {
        private GameObject _instance;
        protected override void OnEnterTyped(VfxClipData clip, SkillContext ctx)
        {
            if (clip.Prefab == null) return;
            _instance = Object.Instantiate(clip.Prefab);
            Transform parent = null;
            if (!string.IsNullOrEmpty(clip.AttachBone) && ctx.Caster != null)
                parent = ctx.Caster.transform.Find(clip.AttachBone);
            if (parent != null)
            {
                _instance.transform.SetParent(parent, false);
                _instance.transform.localPosition = clip.Offset;
            }
            else if (ctx.Caster != null)
            {
                _instance.transform.position = ctx.Caster.transform.position + clip.Offset;
            }
        }
        protected override void OnExitTyped(VfxClipData clip, SkillContext ctx)
        {
            if (_instance != null) { Object.Destroy(_instance); _instance = null; }
        }
    }
}
