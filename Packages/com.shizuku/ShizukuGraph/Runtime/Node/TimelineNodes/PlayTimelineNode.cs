using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Shizuku.Graph
{
    /// <summary>
    /// Timeline 输出轨道对应的动态 GameObject 输入端口。
    /// TrackAsset 引用是端口的稳定身份，轨道改名时可据此迁移已有边。
    /// </summary>
    [Serializable]
    public sealed class TimelineBindingPort
    {
        [SerializeField]
        private TrackAsset _track;

        [SerializeField]
        private string _trackName;

        [SerializeField]
        private string _targetTypeName;

        [SerializeReference]
        private GameObjectParameterEdgePort _port;

        public TrackAsset Track => _track;
        public string TrackName => _trackName;
        public string TargetTypeName => _targetTypeName;
        public GameObjectParameterEdgePort Port => _port;

        public TimelineBindingPort()
        {
        }

        public TimelineBindingPort(TrackAsset track, string trackName, Type targetType, string portName)
        {
            _track = track;
            _trackName = trackName;
            _targetTypeName = targetType?.AssemblyQualifiedName;
            _port = new GameObjectParameterEdgePort
            {
                IsOut = false,
                Name = portName
            };
        }

        internal bool UpdateDescriptor(string trackName, Type targetType, string portName)
        {
            var changed = false;
            var targetTypeName = targetType?.AssemblyQualifiedName;

            if (_trackName != trackName)
            {
                _trackName = trackName;
                changed = true;
            }

            if (_targetTypeName != targetTypeName)
            {
                _targetTypeName = targetTypeName;
                changed = true;
            }

            if (_port == null)
            {
                _port = new GameObjectParameterEdgePort { IsOut = false, Name = portName };
                return true;
            }

            if (_port.Name != portName)
            {
                _port.Name = portName;
                changed = true;
            }

            _port.IsOut = false;
            return changed;
        }

        internal Type ResolveTargetType()
        {
            return string.IsNullOrEmpty(_targetTypeName) ? null : Type.GetType(_targetTypeName);
        }
    }

    /// <summary>
    /// 播放一个 Unity Timeline。节点拥有并维护内部 PlayableDirector，
    /// 不依赖场景中预先放置的 Director。
    /// </summary>
    [Serializable]
    [NodeMenuItem("时间轴/Play Timeline", Description = "播放 Timeline，并根据输出轨道动态生成绑定端口")]
    public sealed class PlayTimelineNode : ShizukuRunnableNode
    {
        [SerializeField]
        public TimelineAsset Timeline;

        [SerializeField]
        public DirectorWrapMode WrapMode = DirectorWrapMode.None;

        [SerializeField, HideInInspector]
        private List<TimelineBindingPort> _bindingPorts = new();

        [SerializeField]
        private ChainPort _nextPort = new() { Name = "next" };

        [NonSerialized]
        private GameObject _directorObject;

        [NonSerialized]
        private PlayableDirector _director;

        [NonSerialized]
        private List<UnityEngine.Object> _boundTracks = new();

        public IReadOnlyList<TimelineBindingPort> BindingPorts => _bindingPorts;
        public bool IsPlaying => _director != null && _director.state == PlayState.Playing;

        public override Color TitleBarColor => new(0.55f, 0.35f, 0.75f, 1f);

        public override void Init(INodeContext context)
        {
            // Timeline 可能在图上次保存后被修改，运行前也要保证端口描述与资产一致。
            SyncBindingPorts(context);

            base.Init(context);
            foreach (var bindingPort in _bindingPorts)
            {
                var port = bindingPort?.Port;
                if (port == null)
                    continue;

                port.SameTypeConnectedPort = null;
                port.DifferentTypeConnectedPort = null;
                SelfInputPorts.Add(port);
            }
        }

        /// <summary>
        /// 根据 Timeline 输出轨道同步动态输入端口。
        /// 保留同一 TrackAsset 的端口和边，删除已移除轨道的边。
        /// </summary>
        public bool SyncBindingPorts(INodeContext context)
        {
            _bindingPorts ??= new List<TimelineBindingPort>();

            var oldPorts = _bindingPorts.Where(item => item != null).ToList();
            var oldByTrack = new Dictionary<TrackAsset, TimelineBindingPort>();
            foreach (var oldPort in oldPorts)
            {
                if (oldPort.Track != null && !oldByTrack.ContainsKey(oldPort.Track))
                    oldByTrack.Add(oldPort.Track, oldPort);
            }

            var descriptors = BuildBindingDescriptors();
            var newPorts = new List<TimelineBindingPort>(descriptors.Count);
            var retainedPorts = new HashSet<TimelineBindingPort>();
            var renamedPorts = new Dictionary<string, string>(StringComparer.Ordinal);
            var changed = false;

            foreach (var descriptor in descriptors)
            {
                if (oldByTrack.TryGetValue(descriptor.Track, out var existing))
                {
                    retainedPorts.Add(existing);
                    var oldName = existing.Port?.Name;
                    if (existing.UpdateDescriptor(descriptor.TrackName, descriptor.TargetType, descriptor.PortName))
                        changed = true;

                    if (!string.IsNullOrEmpty(oldName) && oldName != descriptor.PortName)
                        renamedPorts[oldName] = descriptor.PortName;

                    newPorts.Add(existing);
                }
                else
                {
                    newPorts.Add(new TimelineBindingPort(
                        descriptor.Track,
                        descriptor.TrackName,
                        descriptor.TargetType,
                        descriptor.PortName));
                    changed = true;
                }
            }

            var removedPortNames = new HashSet<string>(
                oldPorts.Where(port => !retainedPorts.Contains(port))
                    .Select(port => port.Port?.Name)
                    .Where(name => !string.IsNullOrEmpty(name)),
                StringComparer.Ordinal);

            if (removedPortNames.Count > 0)
                changed = true;

            if (oldPorts.Count != newPorts.Count ||
                oldPorts.Where((port, index) => index < newPorts.Count && !ReferenceEquals(port, newPorts[index])).Any())
            {
                changed = true;
            }

            _bindingPorts = newPorts;

            var edges = ResolveEdges(context);
            if (edges != null)
            {
                for (var index = edges.Count - 1; index >= 0; index--)
                {
                    var edge = edges[index];
                    if (edge == null || edge.InputNodeGuid != GUID)
                        continue;

                    var originalPortName = edge.InputPortName;
                    if (removedPortNames.Contains(originalPortName))
                    {
                        edges.RemoveAt(index);
                        context?.Guid2EdgeMap.Remove(edge.GUID);
                        changed = true;
                    }
                    else if (renamedPorts.TryGetValue(originalPortName, out var newPortName))
                    {
                        edge.InputPortName = newPortName;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        protected override void OnExecute()
        {
            // 非抢占：同一节点上一次仍在播放时，本次静默跳过。
            if (IsPlaying)
                return;

            if (Timeline == null)
            {
                ShizukuErrorReporter.LogError("Play Timeline 未指定 Timeline Asset", this);
                return;
            }

            if (!TryResolveBindings(out var bindings))
                return;

            EnsureDirector();

            _boundTracks ??= new List<UnityEngine.Object>();

            // 只有非播放状态才会走到这里；清掉上一轮状态后从头播放。
            _director.Stop();
            foreach (var track in _boundTracks)
            {
                if (track != null)
                    _director.ClearGenericBinding(track);
            }
            _boundTracks.Clear();

            _director.playableAsset = Timeline;
            _director.playOnAwake = false;
            _director.timeUpdateMode = DirectorUpdateMode.GameTime;
            _director.extrapolationMode = WrapMode;

            foreach (var binding in bindings)
            {
                _director.SetGenericBinding(binding.Track, binding.Target);
                _boundTracks.Add(binding.Track);
            }

            _director.time = 0d;
            _director.Play();
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _nextPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }

        public override void DisposeRuntime()
        {
            try
            {
                if (_director != null)
                    _director.Stop();
            }
            finally
            {
                if (_directorObject != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(_directorObject);
                    else
                        UnityEngine.Object.DestroyImmediate(_directorObject);
                }

                _director = null;
                _directorObject = null;
                _boundTracks?.Clear();
                base.DisposeRuntime();
            }
        }

        private List<BindingDescriptor> BuildBindingDescriptors()
        {
            var descriptors = new List<BindingDescriptor>();
            if (Timeline == null)
                return descriptors;

            var usedNames = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var output in Timeline.outputs)
            {
                if (output.sourceObject is not TrackAsset track || output.outputTargetType == null)
                    continue;

                var trackName = !string.IsNullOrWhiteSpace(output.streamName)
                    ? output.streamName
                    : track.name;
                if (string.IsNullOrWhiteSpace(trackName))
                    trackName = track.GetType().Name;

                var basePortName = $"{trackName} [{output.outputTargetType.Name}]";
                usedNames.TryGetValue(basePortName, out var duplicateIndex);
                duplicateIndex++;
                usedNames[basePortName] = duplicateIndex;

                var portName = duplicateIndex == 1
                    ? basePortName
                    : $"{basePortName} #{duplicateIndex}";

                descriptors.Add(new BindingDescriptor(track, trackName, output.outputTargetType, portName));
            }

            return descriptors;
        }

        private bool TryResolveBindings(out List<ResolvedBinding> bindings)
        {
            bindings = new List<ResolvedBinding>(_bindingPorts.Count);

            foreach (var bindingPort in _bindingPorts)
            {
                if (bindingPort?.Track == null || bindingPort.Port == null)
                {
                    ShizukuErrorReporter.LogError("Play Timeline 存在无效的轨道绑定端口", this);
                    return false;
                }

                var source = bindingPort.Port.Value;
                if (source == null)
                {
                    ShizukuErrorReporter.LogError(
                        $"Play Timeline 的轨道 '{bindingPort.TrackName}' 未绑定 GameObject",
                        this);
                    return false;
                }

                var targetType = bindingPort.ResolveTargetType();
                if (targetType == null)
                {
                    ShizukuErrorReporter.LogError(
                        $"Play Timeline 无法解析轨道 '{bindingPort.TrackName}' 的绑定类型 '{bindingPort.TargetTypeName}'",
                        this);
                    return false;
                }

                UnityEngine.Object target;
                if (targetType.IsInstanceOfType(source))
                {
                    target = source;
                }
                else if (typeof(Component).IsAssignableFrom(targetType))
                {
                    target = source.GetComponent(targetType);
                    if (target == null)
                    {
                        ShizukuErrorReporter.LogError(
                            $"Play Timeline 的轨道 '{bindingPort.TrackName}' 需要组件 {targetType.Name}，" +
                            $"但 GameObject '{source.name}' 上不存在该组件",
                            this);
                        return false;
                    }
                }
                else
                {
                    ShizukuErrorReporter.LogError(
                        $"Play Timeline 的轨道 '{bindingPort.TrackName}' 需要 {targetType.Name}，" +
                        "当前 GameObject 绑定方式不支持该类型",
                        this);
                    return false;
                }

                bindings.Add(new ResolvedBinding(bindingPort.Track, target));
            }

            return true;
        }

        private void EnsureDirector()
        {
            if (_director != null)
                return;

            _directorObject = new GameObject($"__ShizukuTimeline_{GUID}");
            _directorObject.hideFlags = HideFlags.HideInHierarchy |
                                        HideFlags.DontSaveInEditor |
                                        HideFlags.DontSaveInBuild;

            if (RuntimeOwner != null)
                _directorObject.transform.SetParent(RuntimeOwner.transform, false);

            _director = _directorObject.AddComponent<PlayableDirector>();
            _director.playOnAwake = false;
            _director.timeUpdateMode = DirectorUpdateMode.GameTime;
        }

        private static List<ParameterEdge> ResolveEdges(INodeContext context)
        {
            return context switch
            {
                ShizukuMethod method => method.Edges,
                ShizukuGraphBase graph => graph.Edges,
                _ => null
            };
        }

        private readonly struct BindingDescriptor
        {
            public readonly TrackAsset Track;
            public readonly string TrackName;
            public readonly Type TargetType;
            public readonly string PortName;

            public BindingDescriptor(TrackAsset track, string trackName, Type targetType, string portName)
            {
                Track = track;
                TrackName = trackName;
                TargetType = targetType;
                PortName = portName;
            }
        }

        private readonly struct ResolvedBinding
        {
            public readonly TrackAsset Track;
            public readonly UnityEngine.Object Target;

            public ResolvedBinding(TrackAsset track, UnityEngine.Object target)
            {
                Track = track;
                Target = target;
            }
        }
    }
}
