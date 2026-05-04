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

        /// <summary>是否每帧 Update（false 则只在 Enter 时触发一次根节点）。</summary>
        public bool TickEveryFrame = true;
    }

    // ============================================================
    // GraphClipHandler — 进入时实例化图，逐帧 Update，退出时销毁
    // ============================================================
    public class GraphClipHandler : ClipHandler<GraphClipData>
    {
        private SkillGraph _instance;

        protected override void OnEnterTyped(GraphClipData clip, SkillContext ctx)
        {
            if (clip.GraphAsset == null) return;

            // 克隆资产，避免运行时污染原 ScriptableObject
            _instance = UnityEngine.Object.Instantiate(clip.GraphAsset);
            _instance.SkillContext = ctx;
            _instance.Init();

            // 触发一次根节点（Enter 时执行链）
            TryRunRoot(_instance);
        }

        protected override void OnUpdateTyped(GraphClipData clip, float localTime, float dt, SkillContext ctx)
        {
            if (_instance == null || !clip.TickEveryFrame) return;
            TryRunRoot(_instance);
        }

        protected override void OnExitTyped(GraphClipData clip, SkillContext ctx)
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance);
                _instance = null;
            }
        }

        private static void TryRunRoot(SkillGraph graph)
        {
            if (string.IsNullOrEmpty(graph.RootNodeGUID)) return;
            if (!graph.Guid2NodeMap.TryGetValue(graph.RootNodeGUID, out var node)) return;
            if (node is Shizuku.Graph.ShizukuRootNode root)
                root.StartExcute();
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
