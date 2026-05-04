using UnityEngine;
using Shizuku.Graph;
namespace Shizuku.SkillEditor.GraphIntegration
{
    /// <summary>
    /// 技能蓝图：在 ShizukuGraphBase 之上注入 SkillContext，
    /// 供 SkillNode 派生节点访问施法者 / 目标 / 位置等信息。
    /// </summary>
    [CreateAssetMenu(fileName = "SkillGraph", menuName = "Shizuku/Skill Graph", order = 2)]
    public class SkillGraph : ShizukuGraphBase
    {
        /// <summary>当前激活的技能上下文（运行时由 GraphTrackRunner 注入）。</summary>
        [System.NonSerialized]
        public SkillContext SkillContext;
    }
}
