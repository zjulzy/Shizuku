using UnityEngine;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 技能运行时上下文，由外部传入，所有 TrackRunner/ClipHandler 共享。
    /// </summary>
    public class SkillContext
    {
        public GameObject Caster;
        public GameObject Target;
        public Vector3 CastPosition;
        public Animator CasterAnimator;
    }
}

