using System;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 标记在 SkillTrack 派生类上，声明该轨道是否允许 Clip 重叠。
    /// 未标记此 Attribute 的轨道默认不允许重叠。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AllowClipOverlapAttribute : Attribute
    {
        /// <summary>
        /// 同一时刻最多允许重叠的 Clip 数量（例如 2 表示允许两个片段重叠，但不允许三个同时重叠）。
        /// </summary>
        public int MaxOverlap { get; }

        public AllowClipOverlapAttribute(int maxOverlap = 2)
        {
            MaxOverlap = maxOverlap;
        }
    }
}

