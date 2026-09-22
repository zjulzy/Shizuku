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
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Shizuku.Tests.EditMode
{
    public sealed class NodeAuthoringTests
    {
        [Serializable]
        private class BaseProbe : ShizukuNodeBase
        {
            [SerializeField, Tooltip("非负等待时间"), Min(0), NodeField("等待", Unit = "秒", Summary = true)]
            private float duration = 2;
        }
        [Serializable]
        private sealed class Probe : BaseProbe
        {
            [SerializeField, UnityEngine.Range(2, 178), NodeField("视野", Summary = true)] public float angle = 50;
            [SerializeField] private Vector3 offset = Vector3.one;
            [SerializeField] private List<int> items = new() { 1, 2 };
            [NonSerialized] public int transient;
            [SerializeReference, NodeField("目标", Required = true)] public GameObjectParameterEdgePort target = new() { Name = "target" };
        }

        [Serializable]
        private sealed class SceneObjectPortProbe : ShizukuNodeBase
        {
            [SerializeReference] public GameObjectParameterEdgePort gameObject = new() { Name = "gameObject" };
            [SerializeReference] public TransformParameterEdgePort transform = new() { Name = "transform" };
        }

        [Serializable]
        private sealed class PortLayoutProbe : ShizukuNodeBase
        {
            [SerializeReference] public BoolParameterEdgePort failed = new() { Name = "failed" };
            [SerializeReference] public StringParameterEdgePort message = new() { Name = "message" };
        }

        [Serializable]
        private sealed class DynamicSyncProbe : ShizukuNodeBase, IDynamicParameterPortProvider
        {
            [NonSerialized] public int SynchronizeCount;
            public IEnumerable<DynamicParameterPortDescriptor> DynamicParameterPorts =>
                Array.Empty<DynamicParameterPortDescriptor>();

            public bool SynchronizeDynamicParameterPorts(INodeContext context)
            {
                SynchronizeCount++;
                return SynchronizeCount == 1;
            }
        }

        private ShizukuGraphBase _graph;
        private const string Folder = "Assets/__NodeAuthoringTests";
        [SetUp]
        public void Setup()
        {
            AssetDatabase.CreateFolder("Assets", "__NodeAuthoringTests");
            _graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            AssetDatabase.CreateAsset(_graph, Folder + "/Graph.asset");
        }
        [TearDown]
        public void Cleanup()
        {
            Undo.ClearUndo(_graph);
            AssetDatabase.DeleteAsset(Folder);
        }

        [Test]
        public void FieldsIncludeInheritedPrivateAndComplexValuesButNotGraphInternals()
        {
            var fields = NodeAuthoringUtility.Fields(typeof(Probe)).Select(f => f.Name).ToArray();
            CollectionAssert.Contains(fields, "duration");
            CollectionAssert.Contains(fields, "offset");
            CollectionAssert.Contains(fields, "items");
            CollectionAssert.DoesNotContain(fields, "GUID");
            CollectionAssert.DoesNotContain(fields, "PositionAndSize");
            CollectionAssert.DoesNotContain(fields, "SelfInputPorts");
            CollectionAssert.DoesNotContain(fields, "transient");
        }

        [TestCase(typeof(InvokeMethodNode), "TargetMethodGUID")]
        [TestCase(typeof(InvokeMethodNode), "DynamicInputPorts")]
        [TestCase(typeof(BlueprintEventNode), "ReturnNodeGUID")]
        [TestCase(typeof(BlueprintEventNode), "EventParameters")]
        public void GeneratedStructureIsNotEditable(Type type, string field)
        {
            Assert.That(NodeAuthoringUtility.Fields(type).Select(f => f.Name), Does.Not.Contain(field));
        }

        [Test]
        public void ReplacingSingleInputConnectionIsOneUndoOperation()
        {
            for (var i = 0; i < 3; i++) _graph.AddNode(new AddNode_Float());
            var view = new ShizukuGraphView(); view.LoadFromAsset(_graph);
            var nodes = view.nodes.OfType<ShizukuNodeView>().ToArray();
            var first = nodes[0].outputContainer.Children().OfType<Port>().First(p => p is not ControlFlowPort);
            var second = nodes[1].outputContainer.Children().OfType<Port>().First(p => p is not ControlFlowPort);
            var target = nodes[2].inputContainer.Children().OfType<Port>().First(p => p is not ControlFlowPort);
            view.ConnectAuthoringPorts(first, target);
            Undo.IncrementCurrentGroup();
            view.ConnectAuthoringPorts(second, target);
            Undo.FlushUndoRecordObjects();
            Assert.That(_graph.Edges.Single().OutputNodeGuid, Is.EqualTo(nodes[1].RuntimeNode.GUID));
            Undo.PerformUndo();
            Assert.That(_graph.Edges.Single().OutputNodeGuid, Is.EqualTo(nodes[0].RuntimeNode.GUID));
            Undo.PerformRedo();
            Assert.That(_graph.Edges.Single().OutputNodeGuid, Is.EqualTo(nodes[1].RuntimeNode.GUID));
        }

        [Test]
        public void DescriptionUnitsSummaryAndRequiredValidationUseMetadata()
        {
            var node = new Probe(); _graph.AddNode(node);
            var duration = NodeAuthoringUtility.Fields(typeof(Probe)).Single(f => f.Name == "duration");
            Assert.That(NodeAuthoringUtility.Label(duration), Is.EqualTo("等待 (秒)"));
            Assert.That(NodeAuthoringUtility.Tooltip(duration), Is.EqualTo("非负等待时间"));
            Assert.That(NodeAuthoringUtility.Summary(node), Does.Contain("等待 (秒)：2"));
            Assert.That(NodeAuthoringUtility.Validate(node, _graph.Edges).Count(), Is.EqualTo(1));
            node.angle = 999;
            Assert.That(NodeAuthoringUtility.Validate(node, _graph.Edges).Count(), Is.EqualTo(2));
            _graph.Edges.Add(new ParameterEdge("source", "output", node.GUID, "target"));
            Assert.That(NodeAuthoringUtility.Validate(node, _graph.Edges).Count(), Is.EqualTo(1));
        }

        [Test]
        public void FriendlyPortCaptionNeverChangesSerializedConnectionKey()
        {
            var node = new Probe(); _graph.AddNode(node);
            var view = new ShizukuGraphView(); view.LoadFromAsset(_graph);
            var port = view.nodes.OfType<ShizukuNodeView>().Single().inputContainer.Children().OfType<Port>().Single();
            Assert.That(port.portName, Is.EqualTo("target"));
            Assert.That(port.Q<Label>("authoring-port-label").text, Does.Contain("目标"));
        }

        [TestCase("duration")]
        [TestCase("offset")]
        [TestCase("items")]
        public void SerializableFieldsUseNativePropertyEditors(string name)
        {
            var node = new Probe(); _graph.AddNode(node);
            var field = NodeAuthoringUtility.Fields(typeof(Probe)).Single(f => f.Name == name);
            var editor = NodeAuthoringUtility.CreateField(_graph, node, field, null);
            Assert.That(editor, Is.TypeOf<PropertyField>());
            Assert.That(editor.tooltip, Is.EqualTo(NodeAuthoringUtility.Tooltip(field)));
        }

        [Test]
        public void SearchMatchesDescriptionAndAliasesWithoutChangingMenuPath()
        {
            var item = new NodeAuthoringSearchWindow.Item { Path = "角色/Set Tag", Keywords = "锁输入 禁止移动", Description = "控制玩家输入" };
            Assert.That(NodeAuthoringSearchWindow.Matches(item, "禁止移动"), Is.True);
            Assert.That(NodeAuthoringSearchWindow.Matches(item, "玩家 输入"), Is.True);
            Assert.That(NodeAuthoringSearchWindow.Matches(item, "set tag"), Is.True);
            Assert.That(NodeAuthoringSearchWindow.Matches(item, "加载地图"), Is.False);
            Assert.That(item.Path, Is.EqualTo("角色/Set Tag"));
        }

        [Test]
        public void CreateNodeUndoRedoRestoresGraph()
        {
            var view = new ShizukuGraphView(); view.LoadFromAsset(_graph);
            Undo.IncrementCurrentGroup();
            view.CreateNodeFromType(typeof(AddNode_Float), new Vector2(10, 20));
            Undo.FlushUndoRecordObjects();
            Assert.That(_graph.Nodes.Count, Is.EqualTo(1));
            var guid = _graph.Nodes[0].GUID;
            Undo.PerformUndo();
            Assert.That(_graph.Nodes, Is.Empty);
            Undo.PerformRedo();
            Assert.That(_graph.Nodes.Single().GUID, Is.EqualTo(guid));
        }

        [Test]
        public void PortDefaultValueUndoRedoDoesNotEditRuntimeValue()
        {
            var node = new AddNode_Float(); _graph.AddNode(node); _graph.Init();
            var port = (FloatParameterEdgePort)node.SelfInputPorts[0];
            var editor = NodeAuthoringUtility.CreateDefaultValue(_graph, port, null);
            Assert.That(editor, Is.Not.Null);
            var serialized = (SerializedObject)editor.userData;
            var property = serialized.FindProperty(editor.bindingPath);
            Undo.IncrementCurrentGroup();
            property.floatValue = 17;
            serialized.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            Assert.That(port.DefaultValue, Is.EqualTo(17));
            Assert.That(port.Value, Is.EqualTo(0));
            Undo.PerformUndo(); _graph.Init();
            Assert.That(((FloatParameterEdgePort)_graph.Nodes[0].SelfInputPorts[0]).DefaultValue, Is.EqualTo(0));
            Undo.PerformRedo(); _graph.Init();
            Assert.That(((FloatParameterEdgePort)_graph.Nodes[0].SelfInputPorts[0]).DefaultValue, Is.EqualTo(17));
        }

        [Test]
        public void SceneObjectPortsDoNotExposeDefaultValueEditors()
        {
            var node = new SceneObjectPortProbe();
            _graph.AddNode(node);

            Assert.That(NodeAuthoringUtility.CreateDefaultValue(_graph, node.gameObject, null), Is.Null);
            Assert.That(NodeAuthoringUtility.CreateDefaultValue(_graph, node.transform, null), Is.Null);
        }

        [Test]
        public void GraphLoadSynchronizesDynamicPortsOnlyOnceAndPreservesDirtySignal()
        {
            var node = new DynamicSyncProbe();
            _graph.AddNode(node);
            EditorUtility.ClearDirty(_graph);

            var view = new ShizukuGraphView();
            view.LoadFromAsset(_graph);

            Assert.That(node.SynchronizeCount, Is.EqualTo(1));
            Assert.That(EditorUtility.IsDirty(_graph), Is.True,
                "动态端口同步产生序列化变更时仍必须标记图资产为 Dirty");
        }

        [Test]
        public void GraphLoadSharesSerializedPropertyLookupAcrossAllPortEditors()
        {
            _graph.AddNode(new AddNode_Float());
            _graph.AddNode(new AddNode_Float());

            var view = new ShizukuGraphView();
            view.LoadFromAsset(_graph);

            var serializedObjects = view.nodes.OfType<ShizukuNodeView>()
                .SelectMany(node => node.inputContainer.Query<PropertyField>(className: "port-default-value").ToList())
                .Select(field => field.userData)
                .ToArray();

            Assert.That(serializedObjects, Is.Not.Empty);
            Assert.That(serializedObjects.All(item => ReferenceEquals(item, serializedObjects[0])), Is.True,
                "一次图视图构建中的端口编辑器应复用同一个 SerializedObject 和属性索引");
        }

        [UnityTest]
        public IEnumerator InputPortDefaultEditorsStayNextToTheirNames()
        {
            _graph.AddNode(new PortLayoutProbe());
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                var view = new ShizukuGraphView { style = { flexGrow = 1 } };
                window.ShowUtility();
                window.rootVisualElement.Add(view);
                view.LoadFromAsset(_graph);
                var nodeView = view.nodes.OfType<ShizukuNodeView>().Single();
                nodeView.style.width = 300;
                for (var i = 0; i < 3; i++) yield return null;

                foreach (var port in nodeView.inputContainer.Children().OfType<Port>())
                {
                    var editor = port.Q<PropertyField>(className: "port-default-value");
                    Assert.That(editor, Is.Not.Null);
                    var portLabel = port.Q<Label>("type") ?? port.Q<Label>("connector-text");
                    Assert.That(portLabel, Is.Not.Null);
                    var gap = editor.worldBound.xMin - portLabel.worldBound.xMax;
                    Assert.That(gap, Is.LessThanOrEqualTo(12f),
                        $"port={port.portName}, classes={string.Join(",", port.GetClasses())}, " +
                        $"portRect={port.worldBound}, labelRect={portLabel.worldBound}, editorRect={editor.worldBound}, gap={gap}");
                    Assert.That(editor.worldBound.xMin - port.worldBound.xMin, Is.LessThanOrEqualTo(100f),
                        $"The default editor for '{port.portName}' was pushed to the far edge of the port: " +
                        $"portRect={port.worldBound}, labelRect={portLabel.worldBound}, editorRect={editor.worldBound}");
                }
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void ConnectionUndoRedoRestoresBothEdgesAndNodes()
        {
            _graph.AddNode(new AddNode_Float()); _graph.AddNode(new AddNode_Float());
            var view = new ShizukuGraphView(); view.LoadFromAsset(_graph);
            var nodes = view.nodes.OfType<ShizukuNodeView>().ToArray();
            var output = nodes[0].outputContainer.Children().OfType<Port>().First(p => p is not ControlFlowPort);
            var input = nodes[1].inputContainer.Children().OfType<Port>().First(p => p is not ControlFlowPort);
            Undo.IncrementCurrentGroup();
            view.ConnectAuthoringPorts(output, input);
            Undo.FlushUndoRecordObjects();
            Assert.That(_graph.Edges.Count, Is.EqualTo(1));
            Undo.PerformUndo(); Assert.That(_graph.Edges, Is.Empty);
            Undo.PerformRedo(); Assert.That(_graph.Edges.Count, Is.EqualTo(1));
            Assert.That(_graph.Nodes.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator NativeTimelineFieldUndoRestoresDynamicPortsAndEdgesAtomically()
        {
            var timeline = ScriptableObject.CreateInstance<UnityEngine.Timeline.TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, Folder + "/Timeline.playable");
            timeline.CreateTrack<UnityEngine.Timeline.AnimationTrack>(null, "Actor");
            var source = new FindGameObjectNode();
            var node = new PlayTimelineNode { Timeline = timeline };
            _graph.AddNode(source); _graph.AddNode(node); _graph.Init();
            _graph.AddParameterEdge(source, "result", node, node.BindingPorts.Single().Port.Name);
            var originalPort = node.BindingPorts.Single().Port.Name;
            var editor = NodeAuthoringUtility.CreateField(_graph, node, typeof(PlayTimelineNode).GetField("Timeline"),
                () => ((INodeSerializedFieldChangeHandler)node).OnSerializedFieldChanged("Timeline", _graph));
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            try
            {
                window.ShowUtility(); window.rootVisualElement.Add(editor);
                for (var i = 0; i < 10; i++) yield return null;
                var picker = editor.Q<ObjectField>();
                Assert.That(picker, Is.Not.Null);
                Undo.IncrementCurrentGroup();
                picker.value = null;
                for (var i = 0; i < 3; i++) yield return null;
                Undo.FlushUndoRecordObjects();
                Assert.That(node.Timeline, Is.Null);
                Assert.That(node.BindingPorts, Is.Empty);
                Assert.That(_graph.Edges, Is.Empty);
                Undo.PerformUndo();
                var restored = _graph.Nodes.OfType<PlayTimelineNode>().Single();
                Assert.That(restored.Timeline, Is.SameAs(timeline));
                Assert.That(restored.BindingPorts.Single().Port.Name, Is.EqualTo(originalPort));
                Assert.That(_graph.Edges.Count, Is.EqualTo(1));
                Undo.PerformRedo();
                Assert.That(_graph.Nodes.OfType<PlayTimelineNode>().Single().Timeline, Is.Null);
                Assert.That(_graph.Edges, Is.Empty);
            }
            finally { window.Close(); }
        }
    }
}
