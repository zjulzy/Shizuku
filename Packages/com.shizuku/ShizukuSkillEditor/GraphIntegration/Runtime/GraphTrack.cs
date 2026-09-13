using System;
using UnityEngine;
using Shizuku.SkillEditor;
using Shizuku.Graph;

namespace Shizuku.SkillEditor.GraphIntegration
{
    // ============================================================
    // GraphTrack — 蓝图逻辑轨道
    // ============================================================
    [Serializable]
    [TrackRunner(typeof(SimpleTrackRunner))]
    public class GraphTrack : SkillTrack { }

    // ============================================================
    // GraphClipData — 时间轴上一段技能蓝图执行
    // ============================================================
    [Serializable]
    [ClipForTrack(typeof(GraphTrack), "蓝图逻辑")]
    public class GraphClipData : SkillClip
    {
        /// <summary>引用的技能蓝图资产。运行时会被克隆，避免修改源资产。</summary>
        public SkillGraph GraphAsset;

        /// <summary>false 时 Root 只在 Enter 触发一次；已经启动的 Latent 仍会逐帧推进。</summary>
        public bool TickEveryFrame = true;
    }

    // ============================================================
    // GraphClipHandler — 进入时实例化图，逐帧 Update，退出时销毁
    // ============================================================
    public class GraphClipHandler : ClipHandler<GraphClipData>
    {
        private ShizukuGraphRuntime<SkillGraph> _runtime;

        protected override void OnEnterTyped(GraphClipData clip, SkillContext ctx)
        {
            // Handler 被异常重复 Enter 时先释放旧实例，避免运行时克隆泄漏。
            _runtime?.Dispose();
            _runtime = null;
            if (clip.GraphAsset == null) return;

            _runtime = ShizukuGraphRuntime<SkillGraph>.Create(
                clip.GraphAsset,
                ctx?.Player != null ? ctx.Player.gameObject : ctx?.Caster,
                graph => graph.SkillContext = ctx);

            // 触发一次根节点（Enter 时执行链）
            _runtime.ExecuteRootOnce();
        }

        protected override void OnUpdateTyped(GraphClipData clip, float localTime, float dt, SkillContext ctx)
        {
            // TickEveryFrame 只控制 Root 是否重跑；Enter 中启动的 Latent 必须持续推进。
            _runtime?.Tick(clip.TickEveryFrame);
        }

        protected override void OnExitTyped(GraphClipData clip, SkillContext ctx)
        {
            _runtime?.Dispose();
            _runtime = null;
        }
    }

    // ============================================================
    // Bootstrap — 运行时自动注册 GraphClipHandler 工厂
    // ============================================================
    internal static class GraphIntegrationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            ClipHandlerRegistry.Register<GraphClipData>(() => new GraphClipHandler());
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterInEditor()
        {
            ClipHandlerRegistry.Register<GraphClipData>(() => new GraphClipHandler());
        }
#endif
    }
}
