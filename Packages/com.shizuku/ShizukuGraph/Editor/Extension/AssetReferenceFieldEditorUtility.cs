using System;
using System.Reflection;
using UnityEditor;

namespace Shizuku.Graph.Editor
{
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
