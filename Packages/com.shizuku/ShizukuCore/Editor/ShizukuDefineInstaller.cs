using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Shizuku.Core.Editor
{
    /// <summary>
    /// 包导入时自动添加 Shizuku 模块宏定义
    /// 确保已安装的模块默认启用，用户可手动移除以禁用
    /// </summary>
    [InitializeOnLoad]
    static class ShizukuDefineInstaller
    {
        /// <summary>
        /// 当前包中存在的模块及其对应的宏定义
        /// 新增模块时在这里加一行即可
        /// </summary>
        static readonly string[] RequiredDefines = new[]
        {
            "SHIZUKU_GRAPH",
            "SHIZUKU_TAG",
            "SHIZUKU_SKILL_EDITOR",
        };

        static ShizukuDefineInstaller()
        {
            var buildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (buildTarget == BuildTargetGroup.Unknown)
                buildTarget = BuildTargetGroup.Standalone;

            var currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTarget);
            var defineList = new List<string>(currentDefines.Split(';').Where(d => !string.IsNullOrWhiteSpace(d)));

            bool changed = false;
            foreach (var define in RequiredDefines)
            {
                if (!defineList.Contains(define))
                {
                    defineList.Add(define);
                    changed = true;
                }
            }

            if (changed)
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTarget, string.Join(";", defineList));
                UnityEngine.Debug.Log($"[Shizuku] Auto-enabled defines: {string.Join(", ", RequiredDefines)}");
            }
        }
    }


}
