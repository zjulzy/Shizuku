using System;
using UnityEngine;
using Shizuku.Core;
using Shizuku.Graph;
namespace Shizuku.SkillEditor.GraphIntegration
{
    /// <summary>
    /// 技能节点基类。从 SkillGraph 上下文中读取 SkillContext。
    /// </summary>
    [Serializable]
    public abstract class SkillValueNode : ShizukuValueNode
    {
        protected SkillContext SkillCtx => (RootGraph as SkillGraph)?.SkillContext;
    }
    // ============================================================
    // GetCaster — 获取施法者 GameObject
    // ============================================================
    [Serializable]
    [NodeMenuItem("技能/获取施法者", NodeCategory.Variable, Description = "从技能上下文中获取施法者 GameObject")]
    public class GetCasterNode : SkillValueNode
    {
        public override string Title => "Caster";
        public override Color TitleBarColor => new Color(0.7f, 0.4f, 0.6f, 1f);
        [SerializeReference]
        private GameObjectParameterEdgePort _output = new() { IsOut = true, Name = "caster" };
        protected override void OnComputeOutputValues()
        {
            _output.Value = SkillCtx?.Caster;
        }
    }
    // ============================================================
    // GetTarget — 获取目标 GameObject
    // ============================================================
    [Serializable]
    [NodeMenuItem("技能/获取目标", NodeCategory.Variable, Description = "从技能上下文中获取目标 GameObject")]
    public class GetTargetNode : SkillValueNode
    {
        public override string Title => "Target";
        public override Color TitleBarColor => new Color(0.7f, 0.4f, 0.6f, 1f);
        [SerializeReference]
        private GameObjectParameterEdgePort _output = new() { IsOut = true, Name = "target" };
        protected override void OnComputeOutputValues()
        {
            _output.Value = SkillCtx?.Target;
        }
    }
    // ============================================================
    // GetCastPosition — 获取施法位置
    // ============================================================
    [Serializable]
    [NodeMenuItem("技能/获取施法位置", NodeCategory.Variable, Description = "从技能上下文中获取施法位置")]
    public class GetCastPositionNode : SkillValueNode
    {
        public override string Title => "Cast Position";
        public override Color TitleBarColor => new Color(0.7f, 0.4f, 0.6f, 1f);
        [SerializeReference]
        private Vector3ParameterEdgePort _output = new() { IsOut = true, Name = "position" };
        protected override void OnComputeOutputValues()
        {
            _output.Value = SkillCtx != null ? SkillCtx.CastPosition : Vector3.zero;
        }
    }
}
