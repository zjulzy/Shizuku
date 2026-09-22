using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace Shizuku.Graph.Editor
{
    /// <summary>
    /// 单次遍历图资产的 SerializeReference 树，并复用定位结果。
    /// 图视图构建期间所有端口共享同一个实例，避免每个端口重新扫描整张图。
    /// </summary>
    internal sealed class GraphSerializedPropertyIndex
    {
        private readonly Dictionary<object, string> _paths =
            new(ReferenceIdentityComparer.Instance);
        private readonly Dictionary<string, string> _nodePaths =
            new(StringComparer.Ordinal);

        internal GraphSerializedPropertyIndex(ShizukuGraphBase graph)
        {
            SerializedGraph = new SerializedObject(graph);
            SerializedGraph.UpdateIfRequiredOrScript();

            var iterator = SerializedGraph.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ManagedReference)
                    continue;

                var value = iterator.managedReferenceValue;
                if (value == null)
                    continue;

                if (!_paths.ContainsKey(value))
                    _paths.Add(value, iterator.propertyPath);

                if (value is ShizukuNodeBase node &&
                    !string.IsNullOrEmpty(node.GUID) &&
                    !_nodePaths.ContainsKey(node.GUID))
                {
                    _nodePaths.Add(node.GUID, iterator.propertyPath);
                }
            }
        }

        internal SerializedObject SerializedGraph { get; }

        internal SerializedProperty FindRelative(object owner, string relativeName)
        {
            if (owner == null || string.IsNullOrEmpty(relativeName) ||
                !_paths.TryGetValue(owner, out var path))
            {
                return null;
            }

            return SerializedGraph.FindProperty(path)?.FindPropertyRelative(relativeName)?.Copy();
        }

        internal SerializedProperty FindNodeField(ShizukuNodeBase node, FieldInfo field)
        {
            if (node == null || field == null)
                return null;

            if (!_paths.TryGetValue(node, out var path) &&
                (string.IsNullOrEmpty(node.GUID) || !_nodePaths.TryGetValue(node.GUID, out path)))
            {
                return null;
            }

            return SerializedGraph.FindProperty(path)?.FindPropertyRelative(field.Name)?.Copy();
        }

        private sealed class ReferenceIdentityComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceIdentityComparer Instance = new();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Addressables 可选集成。通过类型身份和 SerializedProperty 工作，
    /// 避免 ShizukuGraph.Editor 对 Unity.Addressables 建立编译期依赖。
    /// </summary>
    internal static class AssetReferenceFieldEditorUtility
    {
        private const string AssetReferenceFullName =
            "UnityEngine.AddressableAssets.AssetReference";

        private const string AddressablesAssemblyName = "Unity.Addressables";

        internal static bool IsAssetReferenceType(Type fieldType)
        {
            for (var current = fieldType; current != null; current = current.BaseType)
            {
                if (current.FullName == AssetReferenceFullName &&
                    current.Assembly.GetName().Name == AddressablesAssemblyName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在图资产的主图或函数子图中定位指定节点字段。
        /// 节点由 SerializeReference 保存，因此需要先找到对应 managed reference，
        /// 再从该属性向下查找实际字段。
        /// </summary>
        internal static SerializedProperty FindNodeFieldProperty(
            SerializedObject serializedGraph,
            ShizukuNodeBase node,
            FieldInfo field)
        {
            if (serializedGraph == null || node == null || field == null)
                return null;

            var iterator = serializedGraph.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ManagedReference)
                    continue;

                if (iterator.managedReferenceValue is not ShizukuNodeBase candidate)
                    continue;

                if (!ReferenceEquals(candidate, node) &&
                    !string.Equals(candidate.GUID, node.GUID, StringComparison.Ordinal))
                {
                    continue;
                }

                return iterator.FindPropertyRelative(field.Name)?.Copy();
            }

            return null;
        }
    }
}
