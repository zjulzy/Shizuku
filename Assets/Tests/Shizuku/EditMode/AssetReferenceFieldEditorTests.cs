using System;
using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Timeline;
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
        private sealed class AssetReferenceProbeNode : ShizukuNodeBase
        {
            [SerializeField]
            private TimelineAssetReference _timeline = new(string.Empty);

            public TimelineAssetReference Timeline => _timeline;
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

        [Test]
        public void AssetReferenceField_UsesPropertyFieldAndPersistsGuidAfterReload()
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
            SetPrivateField(extension, "_currentGraph", graph);
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

            using (var changeEvent = SerializedPropertyChangeEvent.GetPooled(serializedField))
            {
                propertyField.SendEvent(changeEvent);
            }

            Assert.That(EditorUtility.IsDirty(graph), Is.True,
                "AssetReference 变化后必须将图资产标记为 dirty");
            Assert.That(node.Timeline.AssetGUID, Is.EqualTo(expectedGuid));

            AssetDatabase.SaveAssets();
            Assert.That(AssetDatabase.CopyAsset(GraphPath, GraphCopyPath), Is.True);
            AssetDatabase.ImportAsset(GraphCopyPath, ImportAssetOptions.ForceSynchronousImport);

            var loadedGraph = AssetDatabase.LoadAssetAtPath<ShizukuGraphBase>(GraphCopyPath);
            var loadedNode = (AssetReferenceProbeNode)loadedGraph.Nodes[0];
            Assert.That(loadedNode.Timeline, Is.Not.Null);
            Assert.That(loadedNode.Timeline.AssetGUID, Is.EqualTo(expectedGuid));
            Assert.That(loadedNode.Timeline.editorAsset, Is.SameAs(timeline));
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
