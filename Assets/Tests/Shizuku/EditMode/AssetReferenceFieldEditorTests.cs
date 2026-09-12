using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Timeline;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class AssetReferenceFieldEditorTests
    {
        private const string TempFolder = "Assets/__ShizukuAssetReferenceTests";
        private const string TimelinePath = TempFolder + "/Timeline.playable";
        private const string GraphPath = TempFolder + "/Graph.asset";
        private const string GraphCopyPath = TempFolder + "/GraphCopy.asset";

        [Serializable]
        private sealed class TimelineAssetReference : AssetReferenceT<TimelineAsset>
        {
            public TimelineAssetReference(string guid)
                : base(guid)
            {
            }
        }

        [Serializable]
        private sealed class AssetReferenceProbeNode : ShizukuNodeBase,
            IDynamicParameterPortProvider,
            INodeSerializedFieldChangeHandler
        {
            [SerializeField]
            private TimelineAssetReference _timeline = new(string.Empty);

            [SerializeReference]
            private List<ParameterEdgePort> _dynamicPorts = new();

            public TimelineAssetReference Timeline => _timeline;
            public int FieldChangeCount { get; private set; }
            public string LastChangedFieldName { get; private set; }
            public INodeContext LastChangedContext { get; private set; }

            public IEnumerable<DynamicParameterPortDescriptor> DynamicParameterPorts =>
                _dynamicPorts.Select(port => new DynamicParameterPortDescriptor(
                    port,
                    "Timeline binding from AssetReference"));

            public bool SynchronizeDynamicParameterPorts(INodeContext context)
            {
                var shouldHavePort = !string.IsNullOrEmpty(_timeline?.AssetGUID);
                if (shouldHavePort == (_dynamicPorts.Count == 1))
                    return false;

                _dynamicPorts = shouldHavePort
                    ? new List<ParameterEdgePort>
                    {
                        new GameObjectParameterEdgePort
                        {
                            Name = "Timeline Binding",
                            IsOut = false
                        }
                    }
                    : new List<ParameterEdgePort>();
                return true;
            }

            public bool OnSerializedFieldChanged(string fieldName, INodeContext context)
            {
                FieldChangeCount++;
                LastChangedFieldName = fieldName;
                LastChangedContext = context;
                return fieldName == nameof(_timeline) &&
                       SynchronizeDynamicParameterPorts(context);
            }
        }

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);

            AssetDatabase.CreateFolder("Assets", "__ShizukuAssetReferenceTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void AssetReferenceTypeDetection_IsGenericAcrossAssetReferenceSubclasses()
        {
            Assert.That(
                AssetReferenceFieldEditorUtility.IsAssetReferenceType(typeof(AssetReferenceGameObject)),
                Is.True);
            Assert.That(
                AssetReferenceFieldEditorUtility.IsAssetReferenceType(typeof(TimelineAssetReference)),
                Is.True);
            Assert.That(
                AssetReferenceFieldEditorUtility.IsAssetReferenceType(typeof(TimelineAsset)),
                Is.False);
        }

        [UnityTest]
        public IEnumerator AssetReferenceField_UsesPropertyFieldRefreshesDynamicPortsAndPersistsGuid()
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, TimelinePath);

            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var node = new AssetReferenceProbeNode();
            graph.AddNode(node);
            AssetDatabase.CreateAsset(graph, GraphPath);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            Assert.That(EditorUtility.IsDirty(graph), Is.False);

            var field = typeof(AssetReferenceProbeNode).GetField(
                "_timeline",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Assert.That(
                AssetReferenceFieldEditorUtility.IsAssetReferenceType(field.FieldType),
                Is.True);

            var extension = new BaseGraphEditorExtension();
            var graphView = new ShizukuGraphView();
            graphView.LoadFromAsset(graph);
            SetPrivateField(extension, "_currentGraph", graph);
            SetPrivateField(extension, "_graphView", graphView);
            var fieldEditor = InvokeCreateFieldEditor(extension, field, node);
            Assert.That(fieldEditor, Is.TypeOf<PropertyField>(),
                "AssetReference 应交给 SerializedProperty/PropertyField，而不是只读 Label");

            var propertyField = (PropertyField)fieldEditor;
            var serializedGraph = propertyField.userData as SerializedObject;
            Assert.That(serializedGraph, Is.Not.Null);

            var serializedField = AssetReferenceFieldEditorUtility.FindNodeFieldProperty(
                serializedGraph,
                node,
                field);
            Assert.That(serializedField, Is.Not.Null);

            var guidProperty = serializedField.FindPropertyRelative("m_AssetGUID");
            Assert.That(guidProperty, Is.Not.Null);
            var expectedGuid = AssetDatabase.AssetPathToGUID(TimelinePath);
            guidProperty.stringValue = expectedGuid;
            Assert.That(serializedGraph.ApplyModifiedProperties(), Is.True);
            InvokeNodeFieldChange(extension, field, node);

            // Graph Editor 延迟到当前 SerializedProperty 事件结束后再安全地重建视图。
            yield return null;

            Assert.That(EditorUtility.IsDirty(graph), Is.True,
                "AssetReference 变化后必须将图资产标记为 dirty");
            Assert.That(node.Timeline.AssetGUID, Is.EqualTo(expectedGuid));
            Assert.That(node.FieldChangeCount, Is.EqualTo(1));
            Assert.That(node.LastChangedFieldName, Is.EqualTo("_timeline"));
            Assert.That(node.LastChangedContext, Is.SameAs(graph));

            var nodeView = graphView.nodes.OfType<ShizukuNodeView>()
                .Single(view => ReferenceEquals(view.RuntimeNode, node));
            var dynamicPort = nodeView.inputContainer.Children().OfType<Port>().Single();
            Assert.That(dynamicPort.portName, Is.EqualTo("Timeline Binding"));
            Assert.That(dynamicPort.tooltip, Is.EqualTo("Timeline binding from AssetReference"));

            AssetDatabase.SaveAssets();
            Assert.That(AssetDatabase.CopyAsset(GraphPath, GraphCopyPath), Is.True);
            AssetDatabase.ImportAsset(GraphCopyPath, ImportAssetOptions.ForceSynchronousImport);

            var loadedGraph = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(GraphCopyPath);
            var loadedNode = (AssetReferenceProbeNode)loadedGraph.Nodes[0];
            Assert.That(loadedNode.Timeline, Is.Not.Null);
            Assert.That(loadedNode.Timeline.AssetGUID, Is.EqualTo(expectedGuid));
            Assert.That(loadedNode.Timeline.editorAsset, Is.SameAs(timeline));
            Assert.That(loadedNode.DynamicParameterPorts.Single().Port.Name,
                Is.EqualTo("Timeline Binding"));

            extension.OnDisable();
        }

        private static VisualElement InvokeCreateFieldEditor(
            BaseGraphEditorExtension extension,
            FieldInfo field,
            ShizukuNodeBase node)
        {
            var method = typeof(BaseGraphEditorExtension).GetMethod(
                "CreateFieldEditor",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(extension, new object[] { field, node }) as VisualElement;
        }

        private static void InvokeNodeFieldChange(
            BaseGraphEditorExtension extension,
            FieldInfo field,
            ShizukuNodeBase node)
        {
            var method = typeof(BaseGraphEditorExtension).GetMethod(
                "NotifyNodeSerializedFieldChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(extension, new object[] { field, node });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
