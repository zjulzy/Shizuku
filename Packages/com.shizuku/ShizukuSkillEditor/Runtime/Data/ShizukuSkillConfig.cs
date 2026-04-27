using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// 技能配置资产。序列化整个技能的轨道和 Clip 数据。
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillConfig", menuName = "Shizuku/Skill Config")]
    public class ShizukuSkillConfig : ScriptableObject
    {
        public string SkillName;
        public float Duration = 5f;

        [SerializeReference] public List<SkillTrack> Tracks = new();
    }
}