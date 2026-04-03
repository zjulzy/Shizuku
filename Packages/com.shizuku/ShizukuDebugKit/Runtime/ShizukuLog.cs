#if SHIZUKU_DEBUGKIT
using System;

namespace Shizuku.DebugKit
{
    /// <summary>
    /// Shizuku 统一日志工具�?
    /// 支持分类标签、日志等级过滤，以及富文本着色�?
    /// Release 构建中带 [Conditional] 的方法会被自动剥离�?
    /// </summary>
    public static class ShizukuLog
    {
        // ── 日志等级 ──────────────────────────────────────────
        [Flags]
        public enum Level
        {
            None    = 0,
            Trace   = 1 << 0,
            Info    = 1 << 1,
            Warning = 1 << 2,
            Error   = 1 << 3,
            All     = Trace | Info | Warning | Error,
        }

        /// <summary>当前允许输出的等级掩码，可在运行时修改�?/summary>
        public static Level EnabledLevels = Level.All;

        /// <summary>是否为日志添加富文本颜色�?/summary>
        public static bool UseRichText = true;

        // ── 便捷 API ─────────────────────────────────────────

        [System.Diagnostics.Conditional("SHIZUKU_DEBUGKIT")]
        public static void Trace(string tag, string message, UnityEngine.Object context = null)
            => Log(Level.Trace, tag, message, context);

        [System.Diagnostics.Conditional("SHIZUKU_DEBUGKIT")]
        public static void Info(string tag, string message, UnityEngine.Object context = null)
            => Log(Level.Info, tag, message, context);

        [System.Diagnostics.Conditional("SHIZUKU_DEBUGKIT")]
        public static void Warn(string tag, string message, UnityEngine.Object context = null)
            => Log(Level.Warning, tag, message, context);

        [System.Diagnostics.Conditional("SHIZUKU_DEBUGKIT")]
        public static void Error(string tag, string message, UnityEngine.Object context = null)
            => Log(Level.Error, tag, message, context);

        // ── 核心 ─────────────────────────────────────────────

        public static void Log(Level level, string tag, string message, UnityEngine.Object context = null)
        {
            if ((EnabledLevels & level) == 0) return;

            string formatted = FormatMessage(level, tag, message);

            switch (level)
            {
                case Level.Trace:
                case Level.Info:
                    UnityEngine.Debug.Log(formatted, context);
                    break;
                case Level.Warning:
                    UnityEngine.Debug.LogWarning(formatted, context);
                    break;
                case Level.Error:
                    UnityEngine.Debug.LogError(formatted, context);
                    break;
            }
        }

        private static string FormatMessage(Level level, string tag, string message)
        {
            string prefix = $"[{tag}]";
            if (!UseRichText) return $"{prefix} {message}";

            string color = level switch
            {
                Level.Trace   => "#888888",
                Level.Info    => "#58D68D",
                Level.Warning => "#F4D03F",
                Level.Error   => "#EC7063",
                _             => "#FFFFFF",
            };

            return $"<color={color}>{prefix}</color> {message}";
        }
    }
}
#endif

