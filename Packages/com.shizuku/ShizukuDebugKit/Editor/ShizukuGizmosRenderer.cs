#if SHIZUKU_DEBUGKIT
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shizuku.DebugKit.Editor
{
    /// <summary>
    /// �?Scene 视图中渲�?<see cref="ShizukuGizmos"/> 提交的绘制请求�?
    /// 自动随编辑器启动，无需手动挂载�?
    /// </summary>
    [InitializeOnLoad]
    public static class ShizukuGizmosRenderer
    {
        static ShizukuGizmosRenderer()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ShizukuGizmos.Clear();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            IReadOnlyList<ShizukuGizmos.DrawRequest> requests = ShizukuGizmos.GetRequests();
            if (requests.Count == 0) return;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            for (int i = 0; i < requests.Count; i++)
            {
                var req = requests[i];
                DrawRequest(ref req);
            }

            ShizukuGizmos.Flush();
            sceneView.Repaint();
        }

        private static void DrawRequest(ref ShizukuGizmos.DrawRequest req)
        {
            switch (req.Shape)
            {
                case ShizukuGizmos.ShapeType.Line:
                    Handles.color = req.Color;
                    Handles.DrawLine(req.Position, req.Size);
                    break;

                case ShizukuGizmos.ShapeType.Ray:
                    Handles.color = req.Color;
                    Handles.DrawLine(req.Position, req.Position + req.Size);
                    break;

                case ShizukuGizmos.ShapeType.WireSphere:
                    Handles.color = req.Color;
                    Handles.RadiusHandle(Quaternion.identity, req.Position, req.Radius, false);
                    break;

                case ShizukuGizmos.ShapeType.Sphere:
                    Handles.color = req.Color;
                    Handles.SphereHandleCap(0, req.Position, Quaternion.identity,
                        req.Radius * 2f, EventType.Repaint);
                    break;

                case ShizukuGizmos.ShapeType.WireCube:
                    Handles.color = req.Color;
                    Handles.DrawWireCube(req.Position, req.Size);
                    break;

                case ShizukuGizmos.ShapeType.Cube:
                    Handles.color = req.Color;
                    Handles.CubeHandleCap(0, req.Position, Quaternion.identity,
                        req.Size.x, EventType.Repaint);
                    break;

                case ShizukuGizmos.ShapeType.Label:
                    GUIStyle style = new GUIStyle(GUI.skin.label)
                    {
                        normal = { textColor = req.Color },
                        fontSize = 12,
                    };
                    Handles.Label(req.Position, req.Text, style);
                    break;
            }
        }
    }
}
#endif

