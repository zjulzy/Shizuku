using System;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 标记在 SkillClip 派生类上，声明该 Clip 可以放在哪种 Track 上。
    /// 编辑器通过反射扫描此 Attribute 生成右键菜单。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ClipForTrackAttribute : Attribute
    {
        public Type TrackType { get; }
        public string DisplayName { get; }

        public ClipForTrackAttribute(Type trackType, string displayName = null)
        {
            TrackType = trackType;
            DisplayName = displayName;
        }
    }
}

