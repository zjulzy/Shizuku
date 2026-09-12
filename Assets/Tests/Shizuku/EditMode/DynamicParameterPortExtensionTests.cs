using System;
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
using UnityEngine.UIElements;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class DynamicParameterPortExtensionTests
    {
        [Serializable]
        private sealed class ExternalDynamicPortNode : ShizukuNodeBase, IDynamicParameterPortProvider
        {
            [SerializeReference]
            private readonly List<ParameterEdgePort> _ports = new();

            [NonSerialized]
            public int SynchronizeCount;

            public IEnumerable<DynamicParameterPortDescriptor> DynamicParameterPorts =>
                _ports.Select(port => new DynamicParameterPortDescriptor(
                    port,
                    $"External {port.Name}"));

            public void AddPort(ParameterEdgePort port)
            {
                _ports.Add(port);
            }

            public bool SynchronizeDynamicParameterPorts(INodeContext context)
            {
                SynchronizeCount++;
                return false;
            }
        }

        [Serializable]
        private sealed class ExternalFieldChangeNode : ShizukuNodeBase, INodeSerializedFieldChangeHandler
        {
            [SerializeField]
            public int Value;

            public string LastFieldName { get; private set; }
            public INodeContext LastContext { get; private set; }

            public bool OnSerializedFieldChanged(string fieldName, INodeContext context)
            {
                LastFieldName = fieldName;
                LastContext = context;
                return false;
            }
        }

        [Test]
        public void SerializedFieldChangeNotification_NotifiesExternalHandlerWithoutAssetReferenceSpecialCase()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var node = new ExternalFieldChangeNode();
                graph.AddNode(node);
                var extension = new BaseGraphEditorExtension();
                SetPrivateField(extension, "_currentGraph", graph);

                var field = typeof(ExternalFieldChangeNode).GetField(nameof(ExternalFieldChangeNode.Value));
                var createFieldEditor = typeof(BaseGraphEditorExtension).GetMethod(
                    "CreateFieldEditor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(createFieldEditor, Is.Not.Null);

                var editor = createFieldEditor.Invoke(extension, new object[] { field, node }) as IntegerField;
                Assert.That(editor, Is.Not.Null);
                field.SetValue(node, 42);
                InvokeNodeFieldChange(extension, field, node);

                Assert.That(node.Value, Is.EqualTo(42));
                Assert.That(node.LastFieldName, Is.EqualTo(nameof(ExternalFieldChangeNode.Value)));
                Assert.That(node.LastContext, Is.SameAs(graph));
                Assert.That(EditorUtility.IsDirty(graph), Is.True);

                extension.OnDisable();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void RuntimeInit_RegistersExternalDynamicInputAndOutputPorts()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var input = new GameObjectParameterEdgePort { Name = "Target", IsOut = false };
                var output = new BoolParameterEdgePort { Name = "Succeeded", IsOut = true };
                var node = new ExternalDynamicPortNode();
                node.AddPort(input);
                node.AddPort(output);
                graph.AddNode(node);

                graph.Init();

                Assert.That(node.SynchronizeCount, Is.EqualTo(1));
                Assert.That(node.SelfInputPorts, Is.EquivalentTo(new[] { input }));
                Assert.That(node.SelfOutputPorts, Is.EquivalentTo(new[] { output }));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void GraphView_LoadAndRefreshSynchronizeAndRenderExternalDynamicPorts()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var node = new ExternalDynamicPortNode();
                node.AddPort(new GameObjectParameterEdgePort { Name = "Target", IsOut = false });
                node.AddPort(new BoolParameterEdgePort { Name = "Succeeded", IsOut = true });
                graph.AddNode(node);

                var graphView = new ShizukuGraphView();
                graphView.LoadFromAsset(graph);

                AssertVisualPorts(graphView, node, "Target", "Succeeded");
                var countAfterLoad = node.SynchronizeCount;
                Assert.That(countAfterLoad, Is.GreaterThan(0));

                graphView.RefreshCurrentView();

                Assert.That(node.SynchronizeCount, Is.GreaterThan(countAfterLoad));
                AssertVisualPorts(graphView, node, "Target", "Succeeded");
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void GraphView_SynchronizesAndRendersExternalDynamicPortsInMethodGraph()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var method = new ShizukuMethod("ExternalDynamicPorts");
                var node = new ExternalDynamicPortNode();
                node.AddPort(new IntParameterEdgePort { Name = "Amount", IsOut = false });
                node.AddPort(new StringParameterEdgePort { Name = "Result", IsOut = true });
                method.AddNode(node);
                graph.Methods.Add(method);

                var graphView = new ShizukuGraphView();
                graphView.LoadFromAsset(graph);
                Assert.That(node.SynchronizeCount, Is.GreaterThan(0),
                    "载入根图时也必须同步尚未打开的函数子图");

                var countBeforeEnteringMethod = node.SynchronizeCount;
                graphView.EnterMethodGraph(method);

                Assert.That(node.SynchronizeCount, Is.GreaterThan(countBeforeEnteringMethod));
                AssertVisualPorts(graphView, node, "Amount", "Result");
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
            }
        }

        private static void AssertVisualPorts(
            ShizukuGraphView graphView,
            ShizukuNodeBase node,
            string expectedInput,
            string expectedOutput)
        {
            var nodeView = graphView.nodes.OfType<ShizukuNodeView>()
                .Single(view => ReferenceEquals(view.RuntimeNode, node));
            var input = nodeView.inputContainer.Children().OfType<Port>().Single();
            var output = nodeView.outputContainer.Children().OfType<Port>().Single();

            Assert.That(input.portName, Is.EqualTo(expectedInput));
            Assert.That(input.tooltip, Is.EqualTo($"External {expectedInput}"));
            Assert.That(output.portName, Is.EqualTo(expectedOutput));
            Assert.That(output.tooltip, Is.EqualTo($"External {expectedOutput}"));
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
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
    }
}
