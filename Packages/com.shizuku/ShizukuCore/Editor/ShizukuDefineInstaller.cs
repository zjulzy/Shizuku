using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Core.Editor
{
    /// <summary>
    /// 自动管理 Shizuku 模块宏定义。
    /// 检测各子模块对应的程序集是否实际存在，若存在则添加 define，否则移除。
    /// 用户拷入 / 删除子模块文件夹后，下次编辑器加载会自动更新 define 状态。
    /// </summary>
    [InitializeOnLoad]
    static class ShizukuDefineInstaller
    {
        /// <summary>
        /// 模块映射表：宏定义 → 检测用的程序集名（assembly name，不带 .dll）。
        /// 新增模块时在这里加一项即可。
        /// </summary>
        static readonly (string Define, string AssemblyName)[] ModuleMap = new[]
        {
            ("SHIZUKU_GRAPH",        "ShizukuGraph.Runtime"),
            ("SHIZUKU_TAG",          "ShizukuTag.Runtime"),
            ("SHIZUKU_SKILL_EDITOR", "ShizukuSkillEditor.Runtime"),
            ("SHIZUKU_DEBUGKIT",     "ShizukuDebugKit.Runtime"),
        };

        static ShizukuDefineInstaller()
        {
            // 收集当前已加载的程序集名（O(n) 一次）
            var loadedAssemblies = new HashSet<string>(
                AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name));

            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (buildTarget == BuildTargetGroup.Unknown)
                buildTarget = BuildTargetGroup.Standalone;

            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
            var defineList = new List<string>(
                currentDefines.Split(';').Where(d => !string.IsNullOrWhiteSpace(d)));

            bool changed = false;
            var added = new List<string>();
            var removed = new List<string>();

            foreach (var (define, asmName) in ModuleMap)
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
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, string.Join(";", defineList));
                if (added.Count > 0)
                    Debug.Log($"[Shizuku] 自动启用模块 define：{string.Join(", ", added)}");
                if (removed.Count > 0)
                    Debug.Log($"[Shizuku] 自动移除模块 define（程序集已不存在）：{string.Join(", ", removed)}");
            }
        }
    }
}
