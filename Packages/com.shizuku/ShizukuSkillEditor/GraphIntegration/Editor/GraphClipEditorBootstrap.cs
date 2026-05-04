using UnityEditor;
using UnityEngine;
using Shizuku.SkillEditor.Editor;
using Shizuku.SkillEditor.GraphIntegration;

namespace Shizuku.SkillEditor.GraphIntegration.Editor
{
    /// <summary>
    /// 桥接 SkillEditor 时间轴与 ShizukuGraph 编辑器：
    /// 1. 给 GraphClipData 提供 Inspector 字段（GraphAsset / TickEveryFrame + 打开按钮）
    /// 2. 双击 GraphClip → 自动打开对应的 SkillGraph 资产
    /// 仅当 SHIZUKU_GRAPH 宏存在（即图插件已安装）时才会被编译。
    /// </summary>
    [InitializeOnLoad]
    internal static class GraphClipEditorBootstrap
    {
        static GraphClipEditorBootstrap()
        {
            // ---- Inspector 自定义绘制 ----
            ClipEditorRegistry.RegisterInspector<GraphClipData>(clip =>
            {
                clip.GraphAsset = (SkillGraph)EditorGUILayout.ObjectField(
                    "技能蓝图", clip.GraphAsset, typeof(SkillGraph), false);
                clip.TickEveryFrame = EditorGUILayout.Toggle("每帧执行", clip.TickEveryFrame);

                using (new EditorGUI.DisabledScope(clip.GraphAsset == null))
                {
                    if (GUILayout.Button("打开蓝图编辑器"))
                        OpenSkillGraph(clip.GraphAsset);
                }
            });

            // ---- 双击跳转 ----
            ClipEditorRegistry.RegisterDoubleClick<GraphClipData>(clip =>
            {
                if (clip.GraphAsset != null)
                    OpenSkillGraph(clip.GraphAsset);
                else
                    Debug.LogWarning("[GraphClip] GraphAsset 未设置，无法打开蓝图编辑器。");
            });
        }

        private static void OpenSkillGraph(SkillGraph graph)
        {
            // 直接走 Unity 的资产打开流程，会触发 ShizukuGraphWindow.OnOpenAsset
            AssetDatabase.OpenAsset(graph);
        }
    }
}

