using System;
using System.Text;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 统一的运行时错误格式化输出。
    /// 把 <see cref="ShizukuExecutionContext"/> 中的 GameObject / 蓝图资源 / 当前节点 / 执行路径
    /// 拼成结构化日志，第二参数挂 GameObject 让 Console 双击能跳到出错的对象。
    /// </summary>
    public static class ShizukuErrorReporter
    {
        public static void LogException(Exception e, ShizukuNodeBase node)
        {
            LogException(e, node, ShizukuExecutionContext.Current);
        }

        public static void LogException(Exception e, ShizukuNodeBase node, ShizukuExecutionContext ctx)
        {
            var sb = new StringBuilder(512);
            sb.AppendLine("[Shizuku] 节点执行异常");

            if (node != null)
            {
                string title = null;
                try { title = node.Title; } catch { /* ignore */ }
                sb.Append("  Node     : ").Append(node.GetType().Name);
                if (!string.IsNullOrEmpty(title)) sb.Append(" \"").Append(title).Append('"');
                sb.Append(" (").Append(node.GUID).AppendLine(")");
            }

            if (ctx != null)
            {
                if (ctx.Owner != null || !string.IsNullOrEmpty(ctx.BehaviorTypeName))
                {
                    sb.Append("  Behavior : ");
                    if (!string.IsNullOrEmpty(ctx.BehaviorTypeName)) sb.Append(ctx.BehaviorTypeName);
                    if (ctx.Owner != null) sb.Append(" on '").Append(ctx.Owner.name).Append('\'');
                    sb.AppendLine();
                }
                if (!string.IsNullOrEmpty(ctx.GraphAssetPath))
                    sb.Append("  Asset    : ").AppendLine(ctx.GraphAssetPath);
                sb.Append("  Path     : ").AppendLine(ctx.FormatPath());
            }

            sb.Append("  Message  : ").AppendLine(e?.Message ?? "<null exception>");
            if (e != null)
                sb.Append("  Stack    :\n").Append(e.StackTrace);

            UnityEngine.Object contextObj = ctx?.Owner != null
                ? (UnityEngine.Object)ctx.Owner
                : ctx?.GraphAsset;
            Debug.LogError(sb.ToString(), contextObj);
        }

        /// <summary>
        /// 输出一条结构化警告/错误（无异常对象时使用），同样会附带 Path / Behavior 信息。
        /// </summary>
        public static void LogError(string message, ShizukuNodeBase node = null, ShizukuExecutionContext ctx = null)
        {
            ctx ??= ShizukuExecutionContext.Current;
            var sb = new StringBuilder(256);
            sb.Append("[Shizuku] ").AppendLine(message);
            if (node != null)
                sb.Append("  Node : ").Append(node.GetType().Name).Append(" (").Append(node.GUID).AppendLine(")");
            if (ctx != null)
            {
                if (ctx.Owner != null) sb.Append("  Owner: ").AppendLine(ctx.Owner.name);
                if (!string.IsNullOrEmpty(ctx.GraphAssetPath)) sb.Append("  Asset: ").AppendLine(ctx.GraphAssetPath);
                if (ctx.ExecutionPath.Count > 0) sb.Append("  Path : ").AppendLine(ctx.FormatPath());
            }
            UnityEngine.Object contextObj = ctx?.Owner != null
                ? (UnityEngine.Object)ctx.Owner
                : ctx?.GraphAsset;
            Debug.LogError(sb.ToString(), contextObj);
        }
    }
}

