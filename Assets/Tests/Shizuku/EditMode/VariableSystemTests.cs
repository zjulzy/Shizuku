using System.Reflection;
using NUnit.Framework;
using Shizuku.Graph;
using Shizuku.Graph.Editor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Shizuku.Tests.EditMode
{
    [Category("Tier2")]
    public sealed class VariableSystemTests
    {
        [Test]
        public void VariableNames_AreTrimmedAndUniqueIgnoringCase()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var score = new GraphVariable("Score", VariableType.Int);
                var health = new GraphVariable("Health", VariableType.Int);

                Assert.That(graph.AddVariable(score), Is.True);
                Assert.That(graph.AddVariable(health), Is.True);
                Assert.That(
                    graph.AddVariable(new GraphVariable(" score ", VariableType.Float)),
                    Is.False);

                Assert.That(graph.RenameVariable(health.GUID, " SCORE "), Is.False);
                Assert.That(health.Name, Is.EqualTo("Health"));
                Assert.That(graph.RenameVariable(health.GUID, " Mana "), Is.True);
                Assert.That(health.Name, Is.EqualTo("Mana"));
                Assert.That(graph.GetVariableByName("mana"), Is.SameAs(health));
            }
            finally
            {
                Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void RemoveVariable_CascadesAcrossMainAndMethodGraphs()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var targetVariable = new GraphVariable("Target", VariableType.Int);
                var otherVariable = new GraphVariable("Other", VariableType.Int);
                graph.AddVariable(targetVariable);
                graph.AddVariable(otherVariable);

                var root = new ShizukuRootNode();
                var mainGet = new GetVariableNode_Int { VariableGUID = targetVariable.GUID };
                var mainSet = new SetVariableNode_Int { VariableGUID = targetVariable.GUID };
                var mainAdd = new AddNode_Int();
                graph.AddNode(root);
                graph.AddNode(mainGet);
                graph.AddNode(mainSet);
                graph.AddNode(mainAdd);
                graph.RootNodeGUID = root.GUID;
                graph.AddParameterEdge(mainGet, "Value", mainAdd, "A");

                var method = new ShizukuMethod("UsesVariable");
                var methodGet = new GetVariableNode_Int { VariableGUID = targetVariable.GUID };
                var methodSet = new SetVariableNode_Int { VariableGUID = targetVariable.GUID };
                var methodSource = new SetVariableNode_Int { VariableGUID = otherVariable.GUID };
                var methodAdd = new AddNode_Int();
                method.AddNode(methodGet);
                method.AddNode(methodSet);
                method.AddNode(methodSource);
                method.AddNode(methodAdd);
                method.AddParameterEdge(methodGet, "Value", methodAdd, "A");
                graph.AddMethod(method);

                graph.Init();
                root.ChainPorts["next"].NextNodeGuid = mainSet.GUID;
                methodSource.ChainPorts["Next"].NextNodeGuid = methodSet.GUID;

                Assert.That(graph.CountVariableReferences(targetVariable.GUID), Is.EqualTo(4));
                Assert.That(graph.RemoveVariable(targetVariable.GUID), Is.EqualTo(4));

                Assert.That(graph.GetVariableByGUID(targetVariable.GUID), Is.Null);
                Assert.That(graph.Nodes.Find(node => node.GUID == mainGet.GUID), Is.Null);
                Assert.That(graph.Nodes.Find(node => node.GUID == mainSet.GUID), Is.Null);
                Assert.That(graph.Nodes.Find(node => node.GUID == root.GUID), Is.SameAs(root));
                Assert.That(graph.Nodes.Find(node => node.GUID == mainAdd.GUID), Is.SameAs(mainAdd));
                Assert.That(graph.Edges, Is.Empty);
                Assert.That(root.ChainPorts["next"].NextNodeGuid, Is.Null);
                Assert.That(graph.Guid2NodeMap.ContainsKey(mainGet.GUID), Is.False);
                Assert.That(graph.Guid2NodeMap.ContainsKey(mainSet.GUID), Is.False);

                Assert.That(method.GetNodeByGUID(methodGet.GUID), Is.Null);
                Assert.That(method.GetNodeByGUID(methodSet.GUID), Is.Null);
                Assert.That(method.GetNodeByGUID(methodSource.GUID), Is.SameAs(methodSource));
                Assert.That(method.GetNodeByGUID(methodAdd.GUID), Is.SameAs(methodAdd));
                Assert.That(method.Edges, Is.Empty);
                Assert.That(methodSource.ChainPorts["Next"].NextNodeGuid, Is.Null);
                Assert.That(method.Guid2NodeMap.ContainsKey(methodGet.GUID), Is.False);
                Assert.That(graph.VariableStore.Ints.ContainsKey(targetVariable.GUID), Is.False);
            }
            finally
            {
                graph.DisposeRuntime();
                Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void SetVariable_DoesNotCreateUnknownOrWrongTypeRuntimeKeys()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var value = new GraphVariable("Value", VariableType.Float);
                graph.AddVariable(value);
                graph.Init();

                LogAssert.Expect(
                    LogType.Error,
                    $"[ShizukuGraph] 无法设置 Int 变量：GUID 'missing-guid' 不存在、类型不匹配或图尚未初始化。");
                graph.SetVariableInt("missing-guid", 1);
                Assert.That(graph.VariableStore.Ints.ContainsKey("missing-guid"), Is.False);

                LogAssert.Expect(
                    LogType.Error,
                    $"[ShizukuGraph] 无法设置 Int 变量：GUID '{value.GUID}' 不存在、类型不匹配或图尚未初始化。");
                graph.SetVariableInt(value.GUID, 2);
                Assert.That(graph.VariableStore.Ints.ContainsKey(value.GUID), Is.False);
                Assert.That(graph.VariableStore.Floats.ContainsKey(value.GUID), Is.True);
            }
            finally
            {
                graph.DisposeRuntime();
                Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void GraphRunner_AppliesPerInstanceSceneVariableBindingsBeforeExecution()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            var host = new GameObject("GraphRunnerVariableBindingHost");
            var gameObjectTarget = new GameObject("GraphRunnerGameObjectTarget");
            var transformTarget = new GameObject("GraphRunnerTransformTarget");

            try
            {
                var gameObjectVariable = new GraphVariable("Actor", VariableType.GameObject);
                var transformVariable = new GraphVariable("Anchor", VariableType.Transform);
                var intVariable = new GraphVariable("Count", VariableType.Int);
                graph.AddVariable(gameObjectVariable);
                graph.AddVariable(transformVariable);
                graph.AddVariable(intVariable);

                var runner = host.AddComponent<GraphRunner>();
                runner.GraphAsset = graph;
                Assert.That(
                    runner.SetSceneVariableBinding(gameObjectVariable.GUID, gameObjectTarget),
                    Is.True);
                Assert.That(
                    runner.SetSceneVariableBinding(transformVariable.GUID, transformTarget.transform),
                    Is.True);
                Assert.That(
                    runner.SetSceneVariableBinding(intVariable.GUID, gameObjectTarget),
                    Is.False);

                InvokePrivate(runner, "Start");
                var runtimeGraph = GetPrivateField<ShizukuGraphBase>(runner, "_runtimeGraph");

                Assert.That(runtimeGraph, Is.Not.Null);
                Assert.That(
                    runtimeGraph.TryGetVariableGameObject(gameObjectVariable.GUID, out var runtimeGameObject),
                    Is.True);
                Assert.That(runtimeGameObject, Is.SameAs(gameObjectTarget));
                Assert.That(
                    runtimeGraph.TryGetVariableTransform(transformVariable.GUID, out var runtimeTransform),
                    Is.True);
                Assert.That(runtimeTransform, Is.SameAs(transformTarget.transform));
                Assert.That(gameObjectVariable.GameObjectValue, Is.Null,
                    "GraphRunner 的实例绑定不能回写源图资产默认值");

                InvokePrivate(runner, "OnDestroy");
                runner.GraphAsset = null;
                Assert.That(runner.RemoveInvalidSceneVariableBindings(), Is.EqualTo(2));
                Assert.That(runner.SceneVariableBindings, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(gameObjectTarget);
                Object.DestroyImmediate(transformTarget);
                Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void VariableEditor_MakesTypeReadOnlyAndRejectsSceneObjectDefaults()
        {
            var graph = ScriptableObject.CreateInstance<ShizukuGraphBase>();
            try
            {
                var variable = new GraphVariable("Actor", VariableType.GameObject);
                graph.AddVariable(variable);
                var extension = new BaseGraphEditorExtension();
                SetPrivateField(extension, "_currentGraph", graph);

                var createValueEditor = typeof(BaseGraphEditorExtension).GetMethod(
                    "CreateValueEditor",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(createValueEditor, Is.Not.Null);
                var objectField = createValueEditor.Invoke(extension, new object[] { variable }) as UnityEditor.UIElements.ObjectField;
                Assert.That(objectField, Is.Not.Null);
                Assert.That(objectField.allowSceneObjects, Is.False);

                var createVariableItem = typeof(BaseGraphEditorExtension).GetMethod(
                    "CreateVariableItem",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(createVariableItem, Is.Not.Null);
                var item = createVariableItem.Invoke(extension, new object[] { variable }) as VisualElement;
                Assert.That(item, Is.Not.Null);
                Assert.That(item.Query<UnityEngine.UIElements.EnumField>().ToList(), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(graph);
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }
    }
}
