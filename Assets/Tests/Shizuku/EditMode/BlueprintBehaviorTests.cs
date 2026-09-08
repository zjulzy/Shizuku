using System;
using NUnit.Framework;
using Shizuku.Graph;
using UnityEngine;

namespace Shizuku.Tests.EditMode
{
    public class BlueprintBehaviorTestHost : BlueprintBehavior<BlueprintBehaviorTestHost>
    {
        public int PublicValue = 7;
        protected string ProtectedValue = "initial";

        public bool FallbackCalled;
        public string ProtectedValueForTest => ProtectedValue;

        [BlueprintOverridable]
        public virtual void HandleEvent(int amount, string message, GameObject sender)
        {
            if (TryExecuteBlueprintOverride(nameof(HandleEvent), amount, message, sender))
                return;

            FallbackCalled = true;
        }

        [BlueprintOverridable]
        public virtual void HandleNullable(string value)
        {
            if (TryExecuteBlueprintOverride(nameof(HandleNullable), value))
                return;

            FallbackCalled = true;
        }

        [BlueprintOverridable]
        public virtual int Evaluate(int value)
        {
            if (TryExecuteBlueprintOverride<int>(nameof(Evaluate), out var result, value))
                return result;

            FallbackCalled = true;
            return -1;
        }
    }

    [Serializable]
    public sealed class BlueprintArgumentCaptureNode : ShizukuRunnableNode
    {
        [SerializeReference]
        public IntParameterEdgePort Amount = new()
        {
            IsOut = false,
            Name = "amount"
        };

        [SerializeReference]
        public StringParameterEdgePort Message = new()
        {
            IsOut = false,
            Name = "message"
        };

        [SerializeReference]
        public GameObjectParameterEdgePort Sender = new()
        {
            IsOut = false,
            Name = "sender"
        };

        public int CapturedAmount;
        public string CapturedMessage;
        public GameObject CapturedSender;
        public int ExecuteCount;

        protected override void OnExecute()
        {
            CapturedAmount = Amount.Value;
            CapturedMessage = Message.Value;
            CapturedSender = Sender.Value;
            ExecuteCount++;
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = null;
            return false;
        }
    }

    [Serializable]
    public sealed class BlueprintNullableCaptureNode : ShizukuRunnableNode
    {
        [SerializeReference]
        public StringParameterEdgePort Value = new()
        {
            IsOut = false,
            Name = "value"
        };

        public string CapturedValue;

        protected override void OnExecute()
        {
            CapturedValue = Value.Value;
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = null;
            return false;
        }
    }

    [Category("Tier2")]
    public sealed class BlueprintBehaviorTests
    {
        [Test]
        public void EventOverride_PropagatesSupportedArgumentsAndSkipsFallback()
        {
            var graph = ScriptableObject.CreateInstance<BlueprintBehaviorTestGraph>();
            var hostObject = new GameObject("BlueprintEventTestHost");
            var sender = new GameObject("BlueprintEventSender");

            try
            {
                var behavior = hostObject.AddComponent<BlueprintBehaviorTestHost>();
                var eventNode = CreateEventNode(
                    nameof(BlueprintBehaviorTestHost.HandleEvent),
                    ("amount", typeof(int), new IntParameterEdgePort { IsOut = true, Name = "amount" }),
                    ("message", typeof(string), new StringParameterEdgePort { IsOut = true, Name = "message" }),
                    ("sender", typeof(GameObject), new GameObjectParameterEdgePort { IsOut = true, Name = "sender" }));
                var capture = new BlueprintArgumentCaptureNode();

                graph.AddNode(eventNode);
                graph.AddNode(capture);
                graph.AddParameterEdge(eventNode, "amount", capture, "amount");
                graph.AddParameterEdge(eventNode, "message", capture, "message");
                graph.AddParameterEdge(eventNode, "sender", capture, "sender");
                graph.InitializeBehavior(behavior);
                eventNode.ChainPorts["next"].NextNodeGuid = capture.GUID;

                behavior.HandleEvent(42, "hello", sender);

                Assert.That(behavior.FallbackCalled, Is.False);
                Assert.That(capture.ExecuteCount, Is.EqualTo(1));
                Assert.That(capture.CapturedAmount, Is.EqualTo(42));
                Assert.That(capture.CapturedMessage, Is.EqualTo("hello"));
                Assert.That(capture.CapturedSender, Is.SameAs(sender));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(sender);
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void ReturnValueOverride_ReturnsValueFromBlueprintReturnNode()
        {
            var graph = ScriptableObject.CreateInstance<BlueprintBehaviorTestGraph>();
            var hostObject = new GameObject("BlueprintReturnTestHost");

            try
            {
                var behavior = hostObject.AddComponent<BlueprintBehaviorTestHost>();
                var eventNode = CreateEventNode(
                    nameof(BlueprintBehaviorTestHost.Evaluate),
                    ("value", typeof(int), new IntParameterEdgePort { IsOut = true, Name = "value" }));
                var returnNode = new BlueprintReturnNode
                {
                    EventName = nameof(BlueprintBehaviorTestHost.Evaluate),
                    ReturnPort = new IntParameterEdgePort { IsOut = false, Name = "result" }
                };
                eventNode.ReturnNodeGUID = returnNode.GUID;

                graph.AddNode(eventNode);
                graph.AddNode(returnNode);
                graph.AddParameterEdge(eventNode, "value", returnNode, "result");
                graph.InitializeBehavior(behavior);
                eventNode.ChainPorts["next"].NextNodeGuid = returnNode.GUID;

                var result = behavior.Evaluate(73);

                Assert.That(behavior.FallbackCalled, Is.False);
                Assert.That(result, Is.EqualTo(73));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void InitializeBehavior_RegistersDeclaredPublicAndProtectedFields()
        {
            var graph = ScriptableObject.CreateInstance<BlueprintBehaviorTestGraph>();
            var hostObject = new GameObject("BlueprintPropertyTestHost");

            try
            {
                var behavior = hostObject.AddComponent<BlueprintBehaviorTestHost>();
                graph.InitializeBehavior(behavior);

                Assert.That(graph.TryGetProperty(nameof(BlueprintBehaviorTestHost.PublicValue), out var publicValue), Is.True);
                Assert.That(publicValue, Is.EqualTo(7));
                Assert.That(graph.TryGetProperty("ProtectedValue", out var protectedValue), Is.True);
                Assert.That(protectedValue, Is.EqualTo("initial"));

                Assert.That(graph.TrySetProperty(nameof(BlueprintBehaviorTestHost.PublicValue), 11), Is.True);
                Assert.That(graph.TrySetProperty("ProtectedValue", "changed"), Is.True);
                Assert.That(behavior.PublicValue, Is.EqualTo(11));
                Assert.That(behavior.ProtectedValueForTest, Is.EqualTo("changed"));
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        [Test]
        public void EventOverride_NullArgumentClearsPreviousValue()
        {
            var graph = ScriptableObject.CreateInstance<BlueprintBehaviorTestGraph>();
            var hostObject = new GameObject("BlueprintNullEventTestHost");

            try
            {
                var behavior = hostObject.AddComponent<BlueprintBehaviorTestHost>();
                var eventNode = CreateEventNode(
                    nameof(BlueprintBehaviorTestHost.HandleNullable),
                    ("value", typeof(string), new StringParameterEdgePort { IsOut = true, Name = "value" }));
                var capture = new BlueprintNullableCaptureNode();

                graph.AddNode(eventNode);
                graph.AddNode(capture);
                graph.AddParameterEdge(eventNode, "value", capture, "value");
                graph.InitializeBehavior(behavior);
                eventNode.ChainPorts["next"].NextNodeGuid = capture.GUID;

                behavior.HandleNullable("first");
                Assert.That(capture.CapturedValue, Is.EqualTo("first"));

                behavior.HandleNullable(null);

                Assert.That(capture.CapturedValue, Is.Null,
                    "事件第二次传入 null 时，参数端口不能残留上一次事件的值");
            }
            finally
            {
                graph.DisposeRuntime();
                UnityEngine.Object.DestroyImmediate(graph);
                UnityEngine.Object.DestroyImmediate(hostObject);
            }
        }

        private static BlueprintEventNode CreateEventNode(
            string eventName,
            params (string name, Type type, ParameterEdgePort port)[] parameters)
        {
            var node = new BlueprintEventNode { EventName = eventName };
            foreach (var parameter in parameters)
            {
                node.EventParameters.Add(new EventParameter
                {
                    Name = parameter.name,
                    TypeName = parameter.type.Name,
                    OutputPort = parameter.port
                });
            }

            return node;
        }
    }
}
