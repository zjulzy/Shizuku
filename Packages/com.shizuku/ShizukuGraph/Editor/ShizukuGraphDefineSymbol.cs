using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace Shizuku.Graph.Editor
{
    /// <summary>
    /// 当 ShizukuGraph 插件存在（即本程序集被编译）时，自动向当前 NamedBuildTarget
    /// 的 ScriptingDefineSymbols 注入 <c>SHIZUKU_GRAPH</c>，
    /// 使依赖图插件的桥接程序集（如 ShizukuSkillEditor.GraphIntegration.*）
    /// 通过 defineConstraints 自动启用。
    ///
    /// 移除图插件后，由于本脚本不再被编译，宏会变为"残留"——
    /// 这不会引起编译错误（依赖它的程序集也会一同被禁用），
    /// 用户可在 Project Settings → Player → Scripting Define Symbols 中手动清理。
    /// </summary>
    [InitializeOnLoad]
    internal static class ShizukuGraphDefineSymbol
    {
        private const string Define = "SHIZUKU_GRAPH";

        static ShizukuGraphDefineSymbol()
        {
            // 仅处理当前激活的构建目标即可——切换平台时 Unity 会再次触发 InitializeOnLoad。
            var named = NamedBuildTarget.FromBuildTargetGroup(
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

            PlayerSettings.GetScriptingDefineSymbols(named, out var defines);
            var set = new HashSet<string>(defines ?? System.Array.Empty<string>());
            if (set.Add(Define))
            {
                var arr = new string[set.Count];
                set.CopyTo(arr);
                PlayerSettings.SetScriptingDefineSymbols(named, arr);
            }
        }
    }
}

