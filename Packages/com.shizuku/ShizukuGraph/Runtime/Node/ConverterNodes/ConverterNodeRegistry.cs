using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 类型转换节点注册中心
/// 管理所有可用的类型转换节点，提供查询和创建功能
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    public static class ConverterNodeRegistry
    {
        /// <summary>
        /// 存储：(源类型, 目标类型) → 转换节点类型
        /// </summary>
        private static Dictionary<(Type, Type), Type> _converterNodes = new Dictionary<(Type, Type), Type>();

        /// <summary>
        /// 静态构造函数，自动扫描并注册所有转换节点
        /// </summary>
        static ConverterNodeRegistry()
        {
            RegisterAllConverterNodes();
            UnityEngine.Debug.Log($"[ConverterNodeRegistry] 已注册 {_converterNodes.Count} 个类型转换节点");
        }

    #if UNITY_EDITOR
        /// <summary>
        /// 编辑器下在 Domain Reload 后自动重新初始化
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // 确保在编辑器启动和每次 Domain Reload 后都重新注册
            if (_converterNodes.Count == 0)
            {
                RegisterAllConverterNodes();
                UnityEngine.Debug.Log($"[ConverterNodeRegistry] Domain Reload 后重新初始化，已注册 {_converterNodes.Count} 个类型转换节点");
            }
        }
    #endif

        /// <summary>
        /// 通过反射自动注册所有转换节点
        /// </summary>
        private static void RegisterAllConverterNodes()
        {
            var converterNodeType = typeof(TypeConverterNode);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // 跳过 Unity 和系统程序集
                if (assembly.FullName.StartsWith("Unity") || 
                    assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("Mono") ||
                    assembly.FullName.StartsWith("mscorlib"))
                {
                    continue;
                }

                try
                {
                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        // 必须是 TypeConverterNode 的非抽象子类
                        if (type.IsAbstract || !converterNodeType.IsAssignableFrom(type))
                            continue;

                        // 尝试获取转换类型
                        try
                        {
                            var instance = Activator.CreateInstance(type) as TypeConverterNode;
                            if (instance != null)
                            {
                                var inputType = instance.InputType;
                                var outputType = instance.OutputType;

                                if (inputType != null && outputType != null)
                                {
                                    _converterNodes[(inputType, outputType)] = type;
                                    UnityEngine.Debug.Log($"[ConverterNodeRegistry] 注册转换: {inputType.Name} → {outputType.Name} ({type.Name})");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"[ConverterNodeRegistry] 无法实例化转换节点 {type.Name}: {ex.Message}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 某些程序集可能无法加载，跳过
                    continue;
                }
            }
        }

        /// <summary>
        /// 检查是否可以转换
        /// </summary>
        public static bool CanConvert(Type from, Type to)
        {
            return _converterNodes.ContainsKey((from, to));
        }

        /// <summary>
        /// 创建转换节点实例
        /// </summary>
        public static TypeConverterNode CreateConverterNode(Type from, Type to)
        {
            if (_converterNodes.TryGetValue((from, to), out var nodeType))
            {
                try
                {
                    return Activator.CreateInstance(nodeType) as TypeConverterNode;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[ConverterNodeRegistry] 创建转换节点失败: {from.Name} → {to.Name}, 错误: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取所有已注册的转换类型对
        /// </summary>
        public static IEnumerable<(Type from, Type to)> GetAllConversions()
        {
            return _converterNodes.Keys;
        }

        /// <summary>
        /// 获取指定类型的所有可能转换（输入）
        /// </summary>
        public static IEnumerable<Type> GetConvertibleFrom(Type targetType)
        {
            return _converterNodes.Keys
                .Where(pair => pair.Item2 == targetType)
                .Select(pair => pair.Item1);
        }

        /// <summary>
        /// 获取指定类型的所有可能转换（输出）
        /// </summary>
        public static IEnumerable<Type> GetConvertibleTo(Type sourceType)
        {
            return _converterNodes.Keys
                .Where(pair => pair.Item1 == sourceType)
                .Select(pair => pair.Item2);
        }

        /// <summary>
        /// 清空注册中心（用于测试或重新加载）
        /// 注意：清空后需要重新加载程序集才能重新注册
        /// </summary>
        public static void Clear()
        {
            _converterNodes.Clear();
        }
    }


}
