#if SHIZUKU_DEBUGKIT
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.DebugKit
{
    /// <summary>
    /// 运行时 Gizmo 绘制系统。
    /// 在游戏逻辑中调用 ShizukuGizmos.DrawXxx 提交绘制请求，
    /// 由 Editor 端的 ShizukuGizmosRenderer 在 OnDrawGizmos 中统一渲染。
    /// </summary>
    public static class ShizukuGizmos
    {
        // ── 绘制请求数据 ────────────────────────────────────

        public enum ShapeType : byte
        {
            Line,
            WireSphere,
            Sphere,
            WireCube,
            Cube,
            Ray,
            Label,
        }

        public readonly struct DrawRequest
        {
            public readonly ShapeType Shape;
            public readonly Vector3 Position;
            public readonly Vector3 Size;       // 或 Direction (Ray) / End (Line)
            public readonly float Radius;
            public readonly Color Color;
            public readonly string Text;         // 仅 Label 使用
            public readonly float ExpireTime;    // Time.unscaledTime 过期时刻

            public DrawRequest(ShapeType shape, Vector3 position, Vector3 size,
                               float radius, Color color, string text, float duration)
            {
                Shape = shape;
                Position = position;
                Size = size;
                Radius = radius;
                Color = color;
                Text = text;
                ExpireTime = duration <= 0f ? 0f : Time.unscaledTime + duration;
            }

            public bool IsExpired => ExpireTime > 0f && Time.unscaledTime > ExpireTime;
        }

        // ── 请求缓冲 ────────────────────────────────────────

        private static readonly List<DrawRequest> Requests = new List<DrawRequest>(128);

        /// <summary>获取当前帧的全部绘制请求（由 Editor 渲染器调用）。</summary>
        public static IReadOnlyList<DrawRequest> GetRequests() => Requests;

        /// <summary>清除已过期的请求。</summary>
        public static void Flush()
        {
            for (int i = Requests.Count - 1; i >= 0; i--)
            {
                if (Requests[i].IsExpired || Requests[i].ExpireTime == 0f)
                    Requests.RemoveAt(i);
            }
        }

        public static void Clear() => Requests.Clear();

        // ── 全局开关 ────────────────────────────────────────

        public static bool Enabled = true;

        // ── 绘制 API ────────────────────────────────────────

        /// <summary>画线段。</summary>
        public static void DrawLine(Vector3 from, Vector3 to, Color color, float duration = 0f)
        {
            if (!Enabled) return;
            Requests.Add(new DrawRequest(ShapeType.Line, from, to, 0f, color, null, duration));
        }

        /// <summary>画射线。</summary>
        public static void DrawRay(Vector3 origin, Vector3 direction, Color color, float duration = 0f)
        {
            if (!Enabled) return;
            Requests.Add(new DrawRequest(ShapeType.Ray, origin, direction, 0f, color, null, duration));
        }

        /// <summary>画线框球。</summary>
        public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = 0f)
        {
            if (!Enabled) return;
            Requests.Add(new DrawRequest(ShapeType.WireSphere, center, Vector3.zero, radius, color, null, duration));
        }

        /// <summary>画实心球。</summary>
        public static void DrawSphere(Vector3 center, float radius, Color color, float duration = 0f)
        {
            if (!Enabled) return;
            Requests.Add(new DrawRequest(ShapeType.Sphere, center, Vector3.zero, radius, color, null, duration));
        }

        /// <summary>画线框立方体。</summary>
        public static void DrawWireCube(Vector3 center, Vector3 size, Color color, float duration = 0f)
        {
            if (!Enabled) return;
            Requests.Add(new DrawRequest(ShapeType.WireCube, center, size, 0f, color, null, duration));
        }

        /// <summary>画实心立方体。</summary>
        public static void DrawCube(Vector3 center, Vector3 size, Color color, float duration = 0f)
        {
            if (!Enabled) return;
            Requests.Add(new DrawRequest(ShapeType.Cube, center, size, 0f, color, null, duration));
        }

        /// <summary>在世界坐标绘制文字标签。</summary>
        public static void DrawLabel(Vector3 position, string text, Color color, float duration = 0f)
        {
            if (!Enabled) return;
            Requests.Add(new DrawRequest(ShapeType.Label, position, Vector3.zero, 0f, color, text, duration));
        }
    }
}
#endif

