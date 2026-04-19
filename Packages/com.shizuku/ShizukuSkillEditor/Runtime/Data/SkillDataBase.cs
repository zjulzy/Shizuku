using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 技能 Clip 基类。每个 Clip 表示时间轴上一段时间内的行为。
    /// </summary>
    [Serializable]
    public abstract class SkillClip
    {
        public float StartTime;
        public float Duration;
        public float EndTime => StartTime + Duration;
    }

    /// <summary>
    /// 技能轨道基类。持有多态 Clip 列表。
    /// </summary>
    [Serializable]
    public abstract class SkillTrack
    {
        public string TrackName;
        public bool Enabled = true;

        [SerializeReference]
        public List<SkillClip> Clips = new();
    }

    /// <summary>
    /// 技能配置资产。序列化整个技能的轨道和 Clip 数据。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillConfig", menuName = "Shizuku/Skill Config")]
    public class ShizukuSkillConfig : ScriptableObject
    {
        public string SkillName;
        public float Duration = 1f;

        [SerializeReference]
        public List<SkillTrack> Tracks = new();
    }
}

