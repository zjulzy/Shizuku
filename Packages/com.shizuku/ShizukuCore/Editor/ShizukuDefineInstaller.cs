using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;

namespace Shizuku.Core.Editor
{
    /// <summary>
    /// 管理 Shizuku 的脚本宏。
    /// 自动模块宏根据已加载程序集同步；用户模块宏通过 Project Settings 显式切换。
    /// </summary>
    [InitializeOnLoad]
    static class ShizukuDefineInstaller
    {
        internal const string TagDefine = "SHIZUKU_TAG";

        /// <summary>
        /// 模块映射表：宏定义 → 检测用的程序集名（assembly name，不带 .dll）。
        /// 新增模块时在这里加一项即可。
        /// </summary>
        static readonly (string Define, string AssemblyName)[] AutomaticModuleMap = new[]
        {
            ("SHIZUKU_GRAPH",        "ShizukuGraph.Runtime"),
            ("SHIZUKU_SKILL_EDITOR", "ShizukuSkillEditor.Runtime"),
            ("SHIZUKU_DEBUGKIT",     "ShizukuDebugKit.Runtime"),
        };

        static ShizukuDefineInstaller()
        {
            SynchronizeAutomaticModuleDefines();
        }

        internal static NamedBuildTarget CurrentBuildTarget
        {
            get
            {
                var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                if (group == BuildTargetGroup.Unknown)
                    group = BuildTargetGroup.Standalone;

                return NamedBuildTarget.FromBuildTargetGroup(group);
            }
        }

        internal static bool IsDefineEnabled(string define)
        {
            PlayerSettings.GetScriptingDefineSymbols(CurrentBuildTarget, out var defines);
            return Array.IndexOf(defines ?? Array.Empty<string>(), define) >= 0;
        }

        internal static bool SetDefineEnabled(string define, bool enabled)
        {
            var target = CurrentBuildTarget;
            PlayerSettings.GetScriptingDefineSymbols(target, out var currentDefines);
            var defines = new List<string>(currentDefines ?? Array.Empty<string>());
            var changed = enabled
                ? AddIfMissing(defines, define)
                : defines.RemoveAll(item => string.Equals(item, define, StringComparison.Ordinal)) > 0;

            if (!changed)
                return false;

            PlayerSettings.SetScriptingDefineSymbols(target, defines.ToArray());
            Debug.Log($"[Shizuku] {(enabled ? "启用" : "禁用")}模块 define：{define} ({target.TargetName})");
            return true;
        }

        internal static bool IsOdinInspectorAvailable()
        {
            const string assemblyName = "Sirenix.OdinInspector.Attributes";
            if (AppDomain.CurrentDomain.GetAssemblies()
                .Any(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal)))
            {
                return true;
            }

            return CompilationPipeline
                .GetPrecompiledAssemblyPaths(CompilationPipeline.PrecompiledAssemblySources.All)
                .Any(path => string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    assemblyName,
                    StringComparison.OrdinalIgnoreCase));
        }

        static void SynchronizeAutomaticModuleDefines()
        {
            // 收集当前已加载的程序集名（O(n) 一次）
            var loadedAssemblies = new HashSet<string>(
                AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name));

            var buildTarget = CurrentBuildTarget;
            PlayerSettings.GetScriptingDefineSymbols(buildTarget, out var currentDefines);
            var defineList = new List<string>(currentDefines ?? Array.Empty<string>());

            bool changed = false;
            var added = new List<string>();
            var removed = new List<string>();

            foreach (var (define, asmName) in AutomaticModuleMap)
            {
                bool present = loadedAssemblies.Contains(asmName);
                bool defined = defineList.Contains(define);

                if (present && !defined)
                {
                    defineList.Add(define);
                    added.Add(define);
                    changed = true;
                }
                else if (!present && defined)
                {
                    defineList.Remove(define);
                    removed.Add(define);
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, defineList.ToArray());
                if (added.Count > 0)
                    Debug.Log($"[Shizuku] 自动启用模块 define：{string.Join(", ", added)}");
                if (removed.Count > 0)
                    Debug.Log($"[Shizuku] 自动移除模块 define（程序集已不存在）：{string.Join(", ", removed)}");
            }
        }

        static bool AddIfMissing(List<string> defines, string define)
        {
            if (defines.Contains(define))
                return false;

            defines.Add(define);
            return true;
        }
    }
}
