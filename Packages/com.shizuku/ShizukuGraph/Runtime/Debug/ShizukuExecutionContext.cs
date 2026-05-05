using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 执行链中的一帧（节点 / 函数调用）
    /// </summary>
    public struct ShizukuExecutionFrame
    {
        public string NodeGUID;
        public string NodeType;
        public string DisplayName;
        public string MethodName; // 非空时表示这是一帧函数调用上下文（InvokeMethodNode 进入子图）

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(MethodName))
                return $"Method:{MethodName}";
            return string.IsNullOrEmpty(DisplayName) ? NodeType : $"{DisplayName}({NodeType})";
        }
    }

    /// <summary>
    /// 结构化执行上下文：在节点执行链路上携带 GameObject / 蓝图资产 / 当前节点 / 执行路径等信息，
    /// 出错时由 <see cref="ShizukuErrorReporter"/> 汇总输出，便于定位"哪个对象的哪个蓝图走到哪一步出错"。
    /// 通过 [ThreadStatic] 暴露 <see cref="Current"/>，正常路径零开销，异常路径再读取。
    /// </summary>
    public class ShizukuExecutionContext
    {
        // 每帧一个 Behavior 一份；同一 Behavior 多帧间复用，避免 GC
        [ThreadStatic] private static ShizukuExecutionContext _current;
        public static ShizukuExecutionContext Current => _current;

        /// <summary>
        /// 全局开关：关闭时所有 Begin / Push / Pop 都变成 no-op，
        /// 节点执行链不会再维护栈，正常路径开销降到 ~1ns（一次 bool 判断）。
        /// 出错日志仍然会打，只是少了 Owner / Asset / Path 这些上下文字段。
        /// 默认：编辑器/开发版开启，Release 关闭。可在游戏启动早期重新赋值。
        /// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool Enabled = true;
#else
        public static bool Enabled = false;
#endif

        public GameObject Owner;            // 持有蓝图的 GameObject（可空，例如 SkillGraph 场景）
        public string BehaviorTypeName;     // BlueprintBehavior 派生类型名（可空）
        public ShizukuGraphBase GraphAsset; // 蓝图/图资产（运行时为克隆实例，原始路径在 Editor 下取）
        public string GraphAssetPath;       // 仅在编辑器环境下有值

        public string CurrentNodeGUID;
        public string CurrentNodeType;

        // 用 List 模拟栈，便于格式化时按顺序遍历
        public readonly List<ShizukuExecutionFrame> ExecutionPath = new List<ShizukuExecutionFrame>(32);

        /// <summary>
        /// 开始一次根执行（由 BlueprintBehavior / SkillPlayer 等驱动方调用），返回 IDisposable 自动清理。
        /// </summary>
        public static Scope Begin(ShizukuGraphBase graph, GameObject owner = null, string behaviorTypeName = null)
        {
            if (!Enabled) return default; // 关闭时返回空 Scope，Dispose 也是 no-op

            var ctx = _current;
            if (ctx == null)
            {
                ctx = new ShizukuExecutionContext();
                _current = ctx;
            }

            ctx.Owner = owner;
            ctx.BehaviorTypeName = behaviorTypeName;
            ctx.GraphAsset = graph;
            ctx.GraphAssetPath = ResolveAssetPath(graph);
            ctx.CurrentNodeGUID = null;
            ctx.CurrentNodeType = null;
            ctx.ExecutionPath.Clear();
            return new Scope(ctx);
        }

        public void PushNodeFrame(ShizukuNodeBase node)
        {
            if (!Enabled) return;
            CurrentNodeGUID = node.GUID;
            CurrentNodeType = node.GetType().Name;
            ExecutionPath.Add(new ShizukuExecutionFrame
            {
                NodeGUID = node.GUID,
                NodeType = CurrentNodeType,
                DisplayName = SafeGetTitle(node)
            });
        }

        public void PushMethodFrame(string methodName)
        {
            if (!Enabled) return;
            ExecutionPath.Add(new ShizukuExecutionFrame
            {
                MethodName = string.IsNullOrEmpty(methodName) ? "<anonymous>" : methodName
            });
        }

        public void PopFrame()
        {
            if (!Enabled) return;
            int last = ExecutionPath.Count - 1;
            if (last < 0) return;
            ExecutionPath.RemoveAt(last);

            // 恢复 Current 节点为新的栈顶节点（跳过 Method 帧）
            for (int i = ExecutionPath.Count - 1; i >= 0; i--)
            {
                var f = ExecutionPath[i];
                if (string.IsNullOrEmpty(f.MethodName))
                {
                    CurrentNodeGUID = f.NodeGUID;
                    CurrentNodeType = f.NodeType;
                    return;
                }
            }
            CurrentNodeGUID = null;
            CurrentNodeType = null;
        }

        public string FormatPath(int maxFrames = 16)
        {
            if (ExecutionPath.Count == 0) return "<empty>";
            var sb = new StringBuilder();
            int start = ExecutionPath.Count > maxFrames ? ExecutionPath.Count - maxFrames : 0;
            if (start > 0) sb.Append("… → ");
            for (int i = start; i < ExecutionPath.Count; i++)
            {
                if (i > start) sb.Append(" → ");
                sb.Append(ExecutionPath[i].ToString());
            }
            return sb.ToString();
        }

        private static string SafeGetTitle(ShizukuNodeBase node)
        {
            try { return node.Title; }
            catch { return null; }
        }

        private static string ResolveAssetPath(ShizukuGraphBase graph)
        {
#if UNITY_EDITOR
            if (graph == null) return null;
            // 运行时 Behavior 持有的是 Instantiate 出来的克隆，没有资产路径，
            // 故名字里通常包含 "_<InstanceID>"，这里只能尽力而为。
            var path = UnityEditor.AssetDatabase.GetAssetPath(graph);
            return string.IsNullOrEmpty(path) ? graph.name : path;
#else
            return graph != null ? graph.name : null;
#endif
        }

        /// <summary>
        /// using 范式自动结束执行作用域。
        /// </summary>
        public readonly struct Scope : IDisposable
        {
            private readonly ShizukuExecutionContext _ctx;
            public Scope(ShizukuExecutionContext ctx) { _ctx = ctx; }
            public void Dispose()
            {
                if (_ctx == null) return;
                _ctx.ExecutionPath.Clear();
                _ctx.Owner = null;
                _ctx.BehaviorTypeName = null;
                _ctx.GraphAsset = null;
                _ctx.GraphAssetPath = null;
                _ctx.CurrentNodeGUID = null;
                _ctx.CurrentNodeType = null;
            }
        }
    }
}

