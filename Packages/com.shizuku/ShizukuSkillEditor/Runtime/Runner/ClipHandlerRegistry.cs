using System;
using System.Collections.Generic;

namespace Shizuku.SkillEditor
{
    /// <summary>
    /// ClipHandler 工厂注册表。按 Clip 类型注册工厂函数，运行时创建对应 Handler。
    /// </summary>
    public static class ClipHandlerRegistry
    {
        private static readonly Dictionary<Type, Func<IClipHandler>> _factories = new();

        static ClipHandlerRegistry()
        {
            Register<LogicClipData>(() => new LogicClipHandler());
        }

        public static void Register<TClip>(Func<IClipHandler> factory) where TClip : SkillClip
            => _factories[typeof(TClip)] = factory;

        public static IClipHandler CreateHandler(Type clipType)
            => _factories.TryGetValue(clipType, out var factory) ? factory() : null;
    }
}

