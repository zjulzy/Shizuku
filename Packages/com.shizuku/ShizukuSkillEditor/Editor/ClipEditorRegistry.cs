using System;
using System.Collections.Generic;

namespace Shizuku.SkillEditor.Editor
{
    /// <summary>
    /// Clip 编辑器扩展注册表。
    /// 让外部模块（如 GraphIntegration）可以为自己定义的 Clip 类型注册：
    /// 1. 双击跳转处理器（DoubleClickHandler）
    /// 2. Inspector 自定义 IMGUI 绘制器（InspectorDrawer）
    /// 这样核心 SkillEditor.Editor 不需要直接引用桥接包的类型。
    /// </summary>
    public static class ClipEditorRegistry
    {
        private static readonly Dictionary<Type, Action<SkillClip>> _doubleClickHandlers = new();
        private static readonly Dictionary<Type, Action<SkillClip>> _inspectorDrawers = new();

        public static void RegisterDoubleClick<TClip>(Action<TClip> handler) where TClip : SkillClip
            => _doubleClickHandlers[typeof(TClip)] = c => handler((TClip)c);

        public static void RegisterInspector<TClip>(Action<TClip> drawer) where TClip : SkillClip
            => _inspectorDrawers[typeof(TClip)] = c => drawer((TClip)c);

        /// <summary>派发双击事件。返回 true 表示已被处理。</summary>
        public static bool TryHandleDoubleClick(SkillClip clip)
        {
            if (clip == null) return false;
            if (_doubleClickHandlers.TryGetValue(clip.GetType(), out var h))
            {
                h(clip);
                return true;
            }
            return false;
        }

        /// <summary>绘制扩展 Inspector。返回 true 表示已绘制。</summary>
        public static bool TryDrawInspector(SkillClip clip)
        {
            if (clip == null) return false;
            if (_inspectorDrawers.TryGetValue(clip.GetType(), out var d))
            {
                d(clip);
                return true;
            }
            return false;
        }
    }
}

