using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    internal static class NodeAuthoringUtility
    {
        internal static IEnumerable<FieldInfo> Fields(Type type)
        {
            for (; type != null && type != typeof(object); type = type.BaseType)
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (!field.IsStatic && !field.IsInitOnly && !field.IsNotSerialized &&
                        (field.IsPublic || field.IsDefined(typeof(SerializeField)) || field.IsDefined(typeof(SerializeReference))) &&
                        !field.IsDefined(typeof(HideInInspector)) && field.DeclaringType != typeof(ShizukuNodeBase))
                        yield return field;
        }

        internal static string Label(FieldInfo field)
        {
            var metadata = field.GetCustomAttribute<NodeFieldAttribute>();
            var label = metadata?.Label ?? ObjectNames.NicifyVariableName(field.Name);
            if (!string.IsNullOrEmpty(metadata?.Unit)) label += " (" + metadata.Unit + ")";
            if (metadata?.Required == true) label += " *";
            return label;
        }

        internal static string Tooltip(FieldInfo field) => field.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? string.Empty;

        internal static PropertyField CreateField(ShizukuGraphBase graph, ShizukuNodeBase node, FieldInfo field, Action changed)
        {
            if (!graph) return null;
            return CreateField(new GraphSerializedPropertyIndex(graph), graph, node, field, changed);
        }

        internal static PropertyField CreateField(GraphSerializedPropertyIndex index, ShizukuGraphBase graph,
            ShizukuNodeBase node, FieldInfo field, Action changed)
        {
            if (index == null || !graph) return null;
            var serialized = index.SerializedGraph;
            var property = index.FindNodeField(node, field);
            if (property == null) return null;
            return Bind(serialized, property, Label(field), Tooltip(field), graph, changed);
        }

        internal static PropertyField CreateDefaultValue(ShizukuGraphBase graph, ParameterEdgePort port, Action changed)
        {
            if (!graph) return null;
            return CreateDefaultValue(new GraphSerializedPropertyIndex(graph), graph, port, changed);
        }

        internal static PropertyField CreateDefaultValue(GraphSerializedPropertyIndex index, ShizukuGraphBase graph,
            ParameterEdgePort port, Action changed)
        {
            if (index == null || !graph) return null;
            // Scene-object ports must be supplied through an edge. Showing an ObjectField here suggests
            // that a Hierarchy object can be persisted safely in the graph asset, which is not true.
            if (port is GameObjectParameterEdgePort || port is TransformParameterEdgePort)
                return null;

            var serialized = index.SerializedGraph;
            var value = index.FindRelative(port, "DefaultValue");
            return value == null ? null : Bind(serialized, value, "", "未连线时使用此默认值", graph, changed);
        }

        private static PropertyField Bind(SerializedObject serialized, SerializedProperty property, string label,
            string tooltip, ShizukuGraphBase graph, Action changed)
        {
            var control = new PropertyField(property, label) { tooltip = tooltip, userData = serialized };
            control.style.marginBottom = 4;
            control.BindProperty(property);
            // Native property binding records the original value. Capture callback side effects in the same Undo group.
            control.RegisterValueChangeCallback(_ =>
            {
                if (!graph) return;
                serialized.ApplyModifiedProperties();
                UnityGraphEditExecutor.RecordSideEffects(graph, "修改节点配置");
                changed?.Invoke();
                EditorUtility.SetDirty(graph);
            });
            // Graph assets must never serialize live scene references.
            control.RegisterCallback<GeometryChangedEvent>(_ =>
                control.Query<ObjectField>().ForEach(f => f.allowSceneObjects = false));
            return control;
        }

        internal static string Summary(ShizukuNodeBase node)
        {
            var fields = Fields(node.GetType()).Where(f => f.GetCustomAttribute<NodeFieldAttribute>()?.Summary == true);
            return string.Join("\n", fields.Select(f => Label(f) + "：" + Describe(f.GetValue(node))));
        }

        private static string Describe(object value)
        {
            if (value == null || value is UnityEngine.Object obj && !obj) return "未设置";
            if (value is UnityEngine.Object asset) return asset.name;
            if (AssetReferenceFieldEditorUtility.IsAssetReferenceType(value.GetType()))
            {
                var guid = value.GetType().GetProperty("AssetGUID")?.GetValue(value) as string;
                var path = AssetDatabase.GUIDToAssetPath(guid ?? "");
                return string.IsNullOrEmpty(path) ? "未设置或资源不存在" : System.IO.Path.GetFileNameWithoutExtension(path);
            }
            if (value is string text) return string.IsNullOrEmpty(text) ? "（空）" : text;
            if (value is bool flag) return flag ? "是" : "否";
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static IEnumerable<string> Validate(ShizukuNodeBase node, IEnumerable<ParameterEdge> edges)
        {
            foreach (var field in Fields(node.GetType()))
            {
                var value = field.GetValue(node);
                var metadata = field.GetCustomAttribute<NodeFieldAttribute>();
                if (metadata?.Required == true)
                {
                    if (value is ParameterEdgePort port)
                    {
                        if (port.IsOut || edges.Any(e => e.InputNodeGuid == node.GUID && e.InputPortName == port.Name)) continue;
                        value = port.GetType().GetField("DefaultValue")?.GetValue(port);
                    }
                    var missing = value == null || value is UnityEngine.Object obj && !obj || value is string str && string.IsNullOrWhiteSpace(str);
                    if (value != null && AssetReferenceFieldEditorUtility.IsAssetReferenceType(value.GetType()))
                    {
                        var guid = value.GetType().GetProperty("AssetGUID")?.GetValue(value) as string;
                        missing = string.IsNullOrEmpty(guid) || string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid));
                    }
                    if (missing) yield return Label(field) + " 未配置，请填写或连接来源。";
                }
                if (value is float || value is int)
                {
                    var number = Convert.ToDouble(value);
                    var range = field.GetCustomAttribute<RangeAttribute>();
                    var min = field.GetCustomAttribute<MinAttribute>();
                    if (double.IsNaN(number) || double.IsInfinity(number)) yield return Label(field) + " 必须是有限数值。";
                    else if (range != null && (number < range.min || number > range.max)) yield return Label(field) + $" 应在 {range.min}–{range.max} 之间。";
                    else if (min != null && number < min.min) yield return Label(field) + $" 不能小于 {min.min}。";
                }
            }
        }
    }
}
