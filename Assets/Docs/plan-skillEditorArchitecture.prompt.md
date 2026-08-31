# Plan: 技能编辑器架构设计

Shizuku 技能编辑器是一个时间轴驱动的技能编排系统，用自己的时间轴引擎替代 Unity Timeline 的复杂 Playable 体系，只在动画轨道内部使用 PlayableGraph 控制 Animator。图轨道功能通过独立桥接包 `com.shizuku.skilleditor-graph` 可选安装。

---

## 核心架构

```
ShizukuSkillConfig (ScriptableObject)     ← 序列化资产
├── 基础属性（名称、时长、冷却等）
└── List<SkillTrack> tracks               ← [SerializeReference] 多态
    ├── AnimationTrack    → 专用 AnimationTrackRunner
    ├── LogicTrack        → SimpleTrackRunner
    ├── EffectTrack       → SimpleTrackRunner（VFX + SFX Handler）
    └── GraphTrack*       → 专用 GraphTrackRunner    ← *桥接包提供

每条 Track 持有 List<SkillClip>（[SerializeReference] 多态）
同一条轨道上可以放不同类型的 Clip
```

---

## UPM 包结构与模块依赖

```
com.shizuku.skilleditor                    ← 核心包（不依赖图插件）
├── Runtime/
│   ├── Data/        SkillClip, SkillTrack, ShizukuSkillConfig
│   │                AnimationClipData, VfxClipData, SfxClipData, LogicClipData
│   │                AnimationTrack, LogicTrack, EffectTrack
│   ├── Runner/      ITrackRunner, SimpleTrackRunner, AnimationTrackRunner
│   │                IClipHandler, ClipHandler<T>, ClipHandlerRegistry
│   │                VfxClipHandler, SfxClipHandler, LogicClipHandler
│   ├── Context/     SkillContext, SkillPlayer
│   └── Attributes/  TrackRunnerAttribute, ClipForTrackAttribute
├── Editor/
│   └── ShizukuSkillEditorWindow（时间轴编辑器）
└── package.json
    dependencies: { "com.shizuku.core": "0.1.0" }

com.shizuku.skilleditor-graph              ← 桥接包（可选，同时依赖技能+图）
├── Runtime/
│   ├── SkillGraph : ShizukuGraphBase
│   ├── Nodes/       SkillNode, GetCasterNode, GetTargetNode...
│   ├── Data/        GraphClipData, GraphTrack
│   └── Runner/      GraphTrackRunner
├── Editor/
│   ├── SkillGraphWindow : ShizukuGraphWindow
│   └── SkillGraphView : ShizukuGraphView
└── package.json
    dependencies: {
        "com.shizuku.skilleditor": "0.1.0",
        "com.shizuku.graph": "0.1.0"
    }
```

**依赖关系图**：

```
com.shizuku.core
    ↑
com.shizuku.graph          com.shizuku.skilleditor
    ↑                           ↑
    └──── com.shizuku.skilleditor-graph ────┘
          （可选桥接包）
```

- 不装图插件 → 没有 `skilleditor-graph` → 技能编辑器正常工作，只是没有图轨道
- 装了图插件 → 可选装 `skilleditor-graph` → 自动获得 GraphTrack + 技能图编辑器

---

## 数据模型层

### Step 1: 基础数据结构（核心包）

```csharp
// ---- TrackRunner 绑定 Attribute ----
[AttributeUsage(AttributeTargets.Class)]
public class TrackRunnerAttribute : Attribute
{
    public Type RunnerType { get; }
    public TrackRunnerAttribute(Type runnerType) => RunnerType = runnerType;
}

// ---- ClipData 绑定到 Track 的 Attribute ----
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ClipForTrackAttribute : Attribute
{
    public Type TrackType { get; }
    public string DisplayName { get; }
    public ClipForTrackAttribute(Type trackType, string displayName = null)
    {
        TrackType = trackType;
        DisplayName = displayName;
    }
}

// ---- Clip 基类 ----
[Serializable]
public abstract class SkillClip
{
    public float StartTime;
    public float Duration;
    public float EndTime => StartTime + Duration;
}

// ---- Track 基类 ----
[Serializable]
public abstract class SkillTrack
{
    public string TrackName;
    public bool Enabled = true;
    
    [SerializeReference]
    public List<SkillClip> Clips = new();
}

// ---- 配置外壳 ----
public class ShizukuSkillConfig : ScriptableObject
{
    public string SkillName;
    public float Duration;
    [SerializeReference]
    public List<SkillTrack> Tracks = new();
}
```

### Step 2: 具体 Clip 类型（核心包）

```csharp
[Serializable]
[ClipForTrack(typeof(AnimationTrack), "动画片段")]
public class AnimationClipData : SkillClip
{
    public AnimationClip Clip;
    public float BlendIn;
    public float BlendOut;
}

[Serializable]
[ClipForTrack(typeof(EffectTrack), "特效")]
public class VfxClipData : SkillClip
{
    public GameObject Prefab;
    public string AttachBone;
    public Vector3 Offset;
}

[Serializable]
[ClipForTrack(typeof(EffectTrack), "音效")]
public class SfxClipData : SkillClip
{
    public AudioClip Clip;
    public float Volume = 1f;
}

[Serializable]
[ClipForTrack(typeof(LogicTrack), "逻辑事件")]
public class LogicClipData : SkillClip
{
    public string EventName;
}
```

### Step 3: 具体 Track 类型（核心包）

```csharp
[Serializable]
[TrackRunner(typeof(AnimationTrackRunner))]
public class AnimationTrack : SkillTrack { }

[Serializable]
[TrackRunner(typeof(SimpleTrackRunner))]
public class LogicTrack : SkillTrack { }

[Serializable]
[TrackRunner(typeof(SimpleTrackRunner))]
public class EffectTrack : SkillTrack { }
```

### Step 4: 图轨道数据（桥接包）

```csharp
// ---- com.shizuku.skilleditor-graph/Runtime ----

[Serializable]
[ClipForTrack(typeof(GraphTrack), "蓝图逻辑")]
public class GraphClipData : SkillClip
{
    public SkillGraph GraphAsset;
}

[Serializable]
[TrackRunner(typeof(GraphTrackRunner))]
public class GraphTrack : SkillTrack { }
```

---

## 运行时引擎层

### Step 5: SkillContext（核心包）

```csharp
public class SkillContext
{
    public GameObject Caster;
    public GameObject Target;
    public Vector3 CastPosition;
    public Animator CasterAnimator;
}
```

### Step 6: ITrackRunner + SimpleTrackRunner（核心包）

```csharp
public interface ITrackRunner
{
    void Init(SkillTrack track);
    void OnSkillStart(SkillContext ctx);
    void OnTick(float currentTime, float deltaTime);
    void OnSkillEnd();
    void OnSkillInterrupt();
}

public interface IClipHandler
{
    void OnEnter(SkillClip clip, SkillContext ctx);
    void OnUpdate(SkillClip clip, float clipLocalTime, float deltaTime, SkillContext ctx);
    void OnExit(SkillClip clip, SkillContext ctx);
}

public abstract class ClipHandler<TClip> : IClipHandler where TClip : SkillClip
{
    public void OnEnter(SkillClip clip, SkillContext ctx) => OnEnterTyped((TClip)clip, ctx);
    public void OnUpdate(SkillClip clip, float localTime, float dt, SkillContext ctx) => OnUpdateTyped((TClip)clip, localTime, dt, ctx);
    public void OnExit(SkillClip clip, SkillContext ctx) => OnExitTyped((TClip)clip, ctx);
    
    protected abstract void OnEnterTyped(TClip clip, SkillContext ctx);
    protected virtual void OnUpdateTyped(TClip clip, float localTime, float dt, SkillContext ctx) { }
    protected abstract void OnExitTyped(TClip clip, SkillContext ctx);
}

public static class ClipHandlerRegistry
{
    private static readonly Dictionary<Type, Func<IClipHandler>> _factories = new();
    
    static ClipHandlerRegistry()
    {
        Register<VfxClipData>(() => new VfxClipHandler());
        Register<SfxClipData>(() => new SfxClipHandler());
        Register<LogicClipData>(() => new LogicClipHandler());
    }
    
    public static void Register<TClip>(Func<IClipHandler> factory) where TClip : SkillClip
        => _factories[typeof(TClip)] = factory;
    
    public static IClipHandler CreateHandler(Type clipType)
        => _factories.TryGetValue(clipType, out var factory) ? factory() : null;
}

public class SimpleTrackRunner : ITrackRunner
{
    private SkillTrack _track;
    private SkillContext _ctx;
    private readonly Dictionary<SkillClip, IClipHandler> _clipHandlers = new();
    private readonly HashSet<SkillClip> _activeClips = new();
    
    public void Init(SkillTrack track)
    {
        _track = track;
        foreach (var clip in _track.Clips)
        {
            var handler = ClipHandlerRegistry.CreateHandler(clip.GetType());
            if (handler != null) _clipHandlers[clip] = handler;
        }
    }
    
    public void OnSkillStart(SkillContext ctx) => _ctx = ctx;
    
    public void OnTick(float currentTime, float deltaTime)
    {
        foreach (var clip in _track.Clips)
        {
            if (!_clipHandlers.TryGetValue(clip, out var handler)) continue;
            bool shouldBeActive = currentTime >= clip.StartTime && currentTime < clip.EndTime;
            bool isActive = _activeClips.Contains(clip);
            
            if (shouldBeActive && !isActive)  { _activeClips.Add(clip); handler.OnEnter(clip, _ctx); }
            if (shouldBeActive && isActive)   { handler.OnUpdate(clip, currentTime - clip.StartTime, deltaTime, _ctx); }
            if (!shouldBeActive && isActive)  { _activeClips.Remove(clip); handler.OnExit(clip, _ctx); }
        }
    }
    
    public void OnSkillEnd()
    {
        foreach (var clip in _activeClips)
            if (_clipHandlers.TryGetValue(clip, out var h)) h.OnExit(clip, _ctx);
        _activeClips.Clear();
    }
    
    public void OnSkillInterrupt() => OnSkillEnd();
}
```

### Step 7: ClipHandler 实现（核心包）

```csharp
public class VfxClipHandler : ClipHandler<VfxClipData>
{
    private GameObject _instance;
    protected override void OnEnterTyped(VfxClipData clip, SkillContext ctx)
    {
        _instance = Object.Instantiate(clip.Prefab);
        // TODO: AttachBone + Offset
    }
    protected override void OnExitTyped(VfxClipData clip, SkillContext ctx)
    {
        if (_instance != null) Object.Destroy(_instance);
        _instance = null;
    }
}

public class SfxClipHandler : ClipHandler<SfxClipData>
{
    protected override void OnEnterTyped(SfxClipData clip, SkillContext ctx)
        => AudioSource.PlayClipAtPoint(clip.Clip, ctx.Caster.transform.position, clip.Volume);
    protected override void OnExitTyped(SfxClipData clip, SkillContext ctx) { }
}

public class LogicClipHandler : ClipHandler<LogicClipData>
{
    protected override void OnEnterTyped(LogicClipData clip, SkillContext ctx) { /* 触发事件 */ }
    protected override void OnExitTyped(LogicClipData clip, SkillContext ctx) { }
}
```

### Step 8: SkillPlayer（核心包）

```csharp
public class SkillPlayer
{
    private List<ITrackRunner> _runners;
    private float _currentTime, _duration;
    private bool _isPlaying;
    private static readonly Dictionary<Type, Type> _runnerTypeCache = new();
    
    public bool IsPlaying => _isPlaying;
    
    public void Play(ShizukuSkillConfig config, SkillContext ctx)
    {
        _duration = config.Duration; _currentTime = 0f; _isPlaying = true;
        _runners = new List<ITrackRunner>();
        foreach (var track in config.Tracks)
        {
            if (!track.Enabled) continue;
            var runner = CreateRunnerForTrack(track);
            runner.Init(track); runner.OnSkillStart(ctx);
            _runners.Add(runner);
        }
    }
    
    private static ITrackRunner CreateRunnerForTrack(SkillTrack track)
    {
        var trackType = track.GetType();
        if (!_runnerTypeCache.TryGetValue(trackType, out var runnerType))
        {
            var attr = trackType.GetCustomAttribute<TrackRunnerAttribute>();
            runnerType = attr?.RunnerType 
                ?? throw new Exception($"Track {trackType.Name} 没有 [TrackRunner]");
            _runnerTypeCache[trackType] = runnerType;
        }
        return (ITrackRunner)Activator.CreateInstance(runnerType);
    }
    
    public void Tick(float deltaTime)
    {
        if (!_isPlaying) return;
        _currentTime += deltaTime;
        foreach (var r in _runners) r.OnTick(_currentTime, deltaTime);
        if (_currentTime >= _duration) Stop();
    }
    
    public void Stop()    { foreach (var r in _runners) r.OnSkillEnd(); _isPlaying = false; }
    public void Interrupt(){ foreach (var r in _runners) r.OnSkillInterrupt(); _isPlaying = false; }
}
```

### Step 9: AnimationTrackRunner（核心包，Playable）

```csharp
// 专用 Runner，内部使用 PlayableGraph
// - 技能开始：PlayableGraph.Create() + AnimationPlayableOutput + AnimationMixerPlayable
// - Clip 进入：AnimationClipPlayable.Create() 接入 mixer
// - Clip 重叠：计算 crossfade 权重
// - 技能结束：PlayableGraph.Destroy()
// - 不需要 PlayableAsset / PlayableBehaviour / PlayableDirector
```

### Step 10: SkillGraph + 技能节点（桥接包）

```csharp
// ---- com.shizuku.skilleditor-graph/Runtime ----

public class SkillGraph : ShizukuGraphBase
{
    [NonSerialized] public SkillContext SkillContext;
}

public abstract class SkillNode : ShizukuRunnableNode
{
    protected SkillContext SkillCtx => (_context as SkillGraph)?.SkillContext;
}

[NodeMenuItem("技能/Get Caster")]
public class GetCasterNode : SkillNode
{
    [SerializeField] private GameObjectParameterEdgePort _output = new() { IsOut = true, Name = "Caster" };
    protected override void OnExecute() => _output.Value = SkillCtx?.Caster;
}

[NodeMenuItem("技能/Get Target")]
public class GetTargetNode : SkillNode
{
    [SerializeField] private GameObjectParameterEdgePort _output = new() { IsOut = true, Name = "Target" };
    protected override void OnExecute() => _output.Value = SkillCtx?.Target;
}
```

### Step 11: GraphTrackRunner（桥接包）

```csharp
// ---- com.shizuku.skilleditor-graph/Runtime ----

public class GraphTrackRunner : ITrackRunner
{
    private SkillTrack _track;
    private SkillContext _ctx;
    private readonly Dictionary<SkillClip, SkillGraph> _activeGraphs = new();
    private readonly HashSet<SkillClip> _activeClips = new();
    
    public void Init(SkillTrack track) => _track = track;
    public void OnSkillStart(SkillContext ctx) => _ctx = ctx;
    
    public void OnTick(float currentTime, float deltaTime)
    {
        foreach (var clip in _track.Clips)
        {
            if (clip is not GraphClipData graphClip) continue;
            bool shouldBeActive = currentTime >= clip.StartTime && currentTime < clip.EndTime;
            bool isActive = _activeClips.Contains(clip);
            
            if (shouldBeActive && !isActive)
            {
                _activeClips.Add(clip);
                var graph = Object.Instantiate(graphClip.GraphAsset) as SkillGraph;
                graph.SkillContext = _ctx;
                graph.Init();
                _activeGraphs[clip] = graph;
            }
            if (shouldBeActive && isActive && _activeGraphs.TryGetValue(clip, out var g))
                g.Update();
            if (!shouldBeActive && isActive)
            {
                _activeClips.Remove(clip);
                if (_activeGraphs.TryGetValue(clip, out var gEnd))
                { Object.Destroy(gEnd); _activeGraphs.Remove(clip); }
            }
        }
    }
    
    public void OnSkillEnd()
    {
        foreach (var g in _activeGraphs.Values) Object.Destroy(g);
        _activeGraphs.Clear(); _activeClips.Clear();
    }
    public void OnSkillInterrupt() => OnSkillEnd();
}

// 桥接包自动注册 Handler
static class SkillEditorGraphBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register()
    {
        ClipHandlerRegistry.Register<GraphClipData>(() => new GraphClipHandler());
    }
}
```

### Step 12: 技能图编辑器（桥接包 Editor）

```csharp
// ---- com.shizuku.skilleditor-graph/Editor ----

public class SkillGraphWindow : ShizukuGraphWindow
{
    protected override ShizukuGraphView CreateGraphView() => new SkillGraphView();
    public static void Open(SkillGraph graph) { ... }
}

public class SkillGraphView : ShizukuGraphView
{
    // 覆写 SearchWindow：只显示通用节点 + 技能专用节点
    // 双击 GraphClipData 时从时间轴编辑器跳转到这里
}
```

---

## 编辑器 UI 层

### Step 13: 时间轴编辑器（核心包 Editor）

在已有的 `ShizukuSkillEditorWindow` 基础上实现：

- **轨道区域**（左侧轨道头 + 右侧时间线）
- **时间刻度尺** + **播放头**（Playhead）
- **Clip 色块**可视化（不同类型不同颜色，拖动移动、拖边缘调时长）
- **右键菜单**（通过反射扫描 `[ClipForTrack]` 自动生成可选 Clip 列表）
- **检查器面板**（选中 Clip 后显示属性）
- 新建/打开/保存 → `ShizukuSkillConfig` 资产
- ▶⏸⏹ → 编辑器预览
- **双击 GraphClipData** → 打开 `SkillGraphWindow`（桥接包提供，不装则无此功能）

---

## 扩展新轨道 / 新 Clip 的流程

**添加新 Clip 类型到已有轨道：**
```
1. 定义 ClipData（[Serializable] + [ClipForTrack] 继承 SkillClip）
2. 定义 ClipHandler（继承 ClipHandler<TClipData>）
3. 在 ClipHandlerRegistry 注册工厂
```

**添加全新轨道类型：**
```
1. 定义 Track（[Serializable] + [TrackRunner] 继承 SkillTrack）
2. 简单轨道：指定 SimpleTrackRunner
3. 复杂轨道：写专用 TrackRunner
```

---

## 开发顺序

### Phase 1: 核心数据 + 最小可运行引擎
1. **Attributes** — `TrackRunnerAttribute`, `ClipForTrackAttribute`
2. **基础数据类** — `SkillClip`, `SkillTrack`, `ShizukuSkillConfig`
3. **SkillContext**
4. **接口** — `ITrackRunner`, `IClipHandler`, `ClipHandler<T>`
5. **ClipHandlerRegistry**
6. **LogicClipData + LogicClipHandler + LogicTrack** — 最简单的轨道先跑通
7. **SimpleTrackRunner**
8. **SkillPlayer** — 到此可以纯代码创建 Config + SkillPlayer.Play() 验证

### Phase 2: 时间轴编辑器
9. **ShizukuSkillEditorWindow** — 时间轴 UI 渲染
10. **Clip 拖拽编辑** — 移动、调整时长
11. **右键菜单** — 反射扫描 `[ClipForTrack]` 生成 Clip 添加列表
12. **检查器面板** — 选中 Clip 显示属性
13. **工具栏** — 新建/打开/保存/预览

### Phase 3: 更多轨道
14. **VfxClipData + VfxClipHandler** — 特效轨道
15. **SfxClipData + SfxClipHandler** — 音效轨道
16. **EffectTrack** — 合并 VFX + SFX

### Phase 4: 动画轨道（最复杂）
17. **AnimationClipData + AnimationTrack**
18. **AnimationTrackRunner** — PlayableGraph + Mixer + Crossfade

### Phase 5: 图轨道桥接包（可选）
19. **SkillGraph : ShizukuGraphBase**
20. **SkillNode 基类 + GetCasterNode / GetTargetNode**
21. **GraphClipData + GraphTrack + GraphTrackRunner**
22. **SkillEditorGraphBootstrap** — 自动注册
23. **SkillGraphWindow + SkillGraphView** — 图编辑器
24. **时间轴双击跳转** — GraphClipData → SkillGraphWindow

---

## 设计决策

| 决策 | 选择 | 原因 |
|------|------|------|
| 序列化 | ScriptableObject + [SerializeReference] | 必须引用 Unity 资产 |
| Track 持有 Clip | `List<SkillClip>`（非泛型，多态） | 同一轨道支持多种 Clip 类型 |
| Track↔Runner 绑定 | `[TrackRunner(typeof(...))]` Attribute | 声明式，SkillPlayer 统一创建 |
| Clip↔Handler 绑定 | `ClipHandlerRegistry` 工厂注册 | 集中管理，桥接包可通过 Bootstrap 追加注册 |
| Clip↔Handler 实例 | 一对一（每个 Clip 独立 Handler） | 状态隔离 |
| Runner 创建缓存 | `static Dictionary<Type, Type>` | 同类型只反射一次 |
| 时间轴驱动 | 自己写 SkillPlayer.Tick | 不依赖 PlayableDirector |
| Playable 范围 | 仅动画轨道内部 | 动画混合绕不开，其他不需要 |
| 图轨道依赖 | 独立桥接 UPM 包 `skilleditor-graph` | 核心包不依赖图插件，可选安装 |
| 技能图 | SkillGraph : ShizukuGraphBase | 持有 SkillContext，类型安全 |
| 技能图编辑器 | SkillGraphWindow : ShizukuGraphWindow | 继承复用，放桥接包 Editor |
| 模块依赖 | 核心包 → Core；桥接包 → 核心包 + Graph | 单向依赖，图模块不感知技能模块 |
