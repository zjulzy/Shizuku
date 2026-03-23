using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ShizukuGraphWindow : EditorWindow
{
    private ShizukuGraphView _graphView;
    private IGraphEditorExtension _currentExtension;
    
    // 使用 SerializeField 保存当前图的引用，在 Play 模式切换时不会丢失
    [SerializeField]
    private ShizukuGraphBase _currentGraph;
    
    private VisualElement _contentContainer;
    private List<IGraphEditorExtension> _availableExtensions = new List<IGraphEditorExtension>();
    
    // ---- Debug UI ----
    private VisualElement _debugToolbar;
    private Button _debugToggleButton;
    private Button _continueButton;
    private Button _stepButton;
    private Button _stopButton;
    private Label _debugStatusLabel;
    private VisualElement _debugInfoPanel;
    private ScrollView _debugInfoScrollView;
    private bool _isDebugInfoVisible;
    
    [MenuItem("Shizuku/ShizukuGraphWindow")]
    public static void OpenWindow()
    {
        ShizukuGraphWindow window = GetWindow<ShizukuGraphWindow>();
        window.titleContent = new GUIContent("Shizuku Graph");
    }

    private void OnEnable()
    {
        RegisterExtensions();
        BuildUI();
        
        // 如果之前有打开的图（例如从 Play 模式返回时），重新加载它
        if (_currentGraph != null)
        {
            _graphView.LoadFromAsset(_currentGraph);
            LoadExtension(_currentGraph);
        }
        
        // 注册编辑器更新回调，用于刷新调试可视化
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    
    private void RegisterExtensions()
    {
        _availableExtensions.Clear();
        _availableExtensions.Add(new BlueprintEditorExtension());
        _availableExtensions.Add(new BaseGraphEditorExtension());
    }
    
    private void BuildUI()
    {
        rootVisualElement.Clear();
        
        // 主工具栏
        Toolbar toolbar = new Toolbar();
        Button saveButton = new Button(() => { _graphView?.SaveToAsset(); });
        saveButton.text = "保存";
        
        Button refreshButton = new Button(() => { RefreshExtension(); });
        refreshButton.text = "刷新";
        
        toolbar.Add(saveButton);
        toolbar.Add(refreshButton);
        rootVisualElement.Add(toolbar);
        
        // 调试工具栏
        BuildDebugToolbar();
        
        // 内容容器
        _contentContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 1
            }
        };
        rootVisualElement.Add(_contentContainer);
        
        // 图视图
        _graphView = new ShizukuGraphView();
        _graphView.style.flexGrow = 1;
        _contentContainer.Add(_graphView);
        
        // 调试信息面板（右侧，默认隐藏）
        BuildDebugInfoPanel();
        
        // 初始化调试 UI 状态
        RefreshDebugUIState();
    }
    
    #region Debug 工具栏
    
    private void BuildDebugToolbar()
    {
        _debugToolbar = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 2,
                paddingBottom = 2,
                backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f),
                borderBottomWidth = 1,
                borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f),
                height = 28
            }
        };
        
        // Debug 开关按钮
        _debugToggleButton = CreateDebugButton("🔧 Debug", "开启/关闭调试模式", () => OnDebugToggle());
        _debugToggleButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        _debugToolbar.Add(_debugToggleButton);
        
        AddDebugSeparator();
        
        // 继续按钮
        _continueButton = CreateDebugButton("▶ 继续", "继续执行直到下一个断点 (F5)", () => OnContinue());
        _continueButton.style.backgroundColor = new Color(0.2f, 0.45f, 0.2f, 1f);
        _debugToolbar.Add(_continueButton);
        
        // 单步按钮
        _stepButton = CreateDebugButton("⏭ 单步", "执行一个节点后暂停 (F10)", () => OnStep());
        _stepButton.style.backgroundColor = new Color(0.2f, 0.35f, 0.55f, 1f);
        _debugToolbar.Add(_stepButton);
        
        // 停止按钮
        _stopButton = CreateDebugButton("⏹ 停止", "停止调试", () => OnStopDebug());
        _stopButton.style.backgroundColor = new Color(0.55f, 0.2f, 0.2f, 1f);
        _debugToolbar.Add(_stopButton);
        
        AddDebugSeparator();
        
        // 调试信息面板切换按钮
        var infoToggle = CreateDebugButton("📋 变量", "显示/隐藏调试信息面板", () => ToggleDebugInfoPanel());
        _debugToolbar.Add(infoToggle);
        
        // 状态标签
        _debugStatusLabel = new Label("")
        {
            style =
            {
                color = new Color(0.7f, 0.7f, 0.7f, 1f),
                fontSize = 11,
                marginLeft = 10,
                flexGrow = 1,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };
        _debugToolbar.Add(_debugStatusLabel);
        
        rootVisualElement.Add(_debugToolbar);
    }
    
    private Button CreateDebugButton(string text, string tooltip, System.Action onClick)
    {
        var btn = new Button(onClick)
        {
            text = text,
            tooltip = tooltip,
            style =
            {
                height = 22,
                paddingLeft = 8,
                paddingRight = 8,
                marginLeft = 2,
                marginRight = 2,
                borderTopLeftRadius = 3,
                borderTopRightRadius = 3,
                borderBottomLeftRadius = 3,
                borderBottomRightRadius = 3,
                color = Color.white,
                fontSize = 11
            }
        };
        return btn;
    }
    
    private void AddDebugSeparator()
    {
        var sep = new VisualElement
        {
            style =
            {
                width = 1,
                height = 18,
                backgroundColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                marginLeft = 6,
                marginRight = 6
            }
        };
        _debugToolbar.Add(sep);
    }
    
    #endregion
    
    #region Debug 信息面板（快照变量查看）
    
    private void BuildDebugInfoPanel()
    {
        _debugInfoPanel = new VisualElement
        {
            style =
            {
                width = 260,
                borderLeftWidth = 1,
                borderLeftColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                display = DisplayStyle.None // 默认隐藏
            }
        };
        
        // 标题
        var header = new Label("🔍 调试信息")
        {
            style =
            {
                fontSize = 13,
                unityFontStyleAndWeight = FontStyle.Bold,
                paddingTop = 8,
                paddingBottom = 6,
                paddingLeft = 10,
                color = new Color(0.8f, 0.9f, 1f, 1f),
                backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f)
            }
        };
        _debugInfoPanel.Add(header);
        
        _debugInfoScrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1,
                paddingLeft = 8,
                paddingRight = 8,
                paddingTop = 6
            }
        };
        _debugInfoPanel.Add(_debugInfoScrollView);
        
        _contentContainer.Add(_debugInfoPanel);
    }
    
    private void ToggleDebugInfoPanel()
    {
        _isDebugInfoVisible = !_isDebugInfoVisible;
        _debugInfoPanel.style.display = _isDebugInfoVisible ? DisplayStyle.Flex : DisplayStyle.None;
        
        if (_isDebugInfoVisible)
        {
            RefreshDebugInfoPanel();
        }
    }
    
    /// <summary>
    /// 刷新调试信息面板内容（显示快照中的变量值）
    /// </summary>
    private void RefreshDebugInfoPanel()
    {
        if (_debugInfoScrollView == null) return;
        _debugInfoScrollView.Clear();
        
        var snapshot = ShizukuDebugger.CurrentSnapshot;
        
        if (!ShizukuDebugger.Enabled)
        {
            AddInfoLabel("调试模式未开启", new Color(0.6f, 0.6f, 0.6f));
            return;
        }
        
        if (!ShizukuDebugger.IsPaused || snapshot == null)
        {
            AddInfoLabel("运行中...", new Color(0.5f, 0.8f, 0.5f));
            AddInfoLabel($"已执行节点: {ShizukuDebugger.ExecutedNodesLastFrame.Count}", new Color(0.7f, 0.7f, 0.7f));
            return;
        }
        
        // ---- 暂停状态：显示快照信息 ----
        AddInfoLabel($"⏸ 暂停于帧 #{snapshot.FrameCount}", new Color(1f, 0.85f, 0.3f));
        
        // 暂停节点名称
        if (snapshot.GraphClone != null && !string.IsNullOrEmpty(snapshot.PausedAtNodeGuid))
        {
            var pausedNode = snapshot.GraphClone.Nodes.FirstOrDefault(n => n.GUID == snapshot.PausedAtNodeGuid);
            if (pausedNode != null)
            {
                AddInfoLabel($"节点: {pausedNode.Title}", Color.white);
            }
        }
        
        AddInfoSeparator();
        AddInfoLabel("📦 变量值 (快照)", new Color(0.7f, 0.85f, 1f));
        
        // 显示快照中的变量
        if (snapshot.GraphClone != null && snapshot.GraphClone.VariableStore != null)
        {
            var store = snapshot.GraphClone.VariableStore;
            var variables = snapshot.GraphClone.Variables;
            bool hasAny = false;
            
            foreach (var variable in variables)
            {
                string valueStr = GetVariableValueString(store, variable);
                AddVariableRow(variable.Name, variable.Type.ToString(), valueStr);
                hasAny = true;
            }
            
            if (!hasAny)
            {
                AddInfoLabel("(无变量)", new Color(0.5f, 0.5f, 0.5f));
            }
        }
        else
        {
            AddInfoLabel("(快照无数据)", new Color(0.5f, 0.5f, 0.5f));
        }
    }
    
    private string GetVariableValueString(RuntimeVariableStore store, GraphVariable variable)
    {
        switch (variable.Type)
        {
            case VariableType.Int:
                return store.Ints.TryGetValue(variable.GUID, out var intVal) ? intVal.ToString() : "?";
            case VariableType.Float:
                return store.Floats.TryGetValue(variable.GUID, out var floatVal) ? floatVal.ToString("F3") : "?";
            case VariableType.Bool:
                return store.Bools.TryGetValue(variable.GUID, out var boolVal) ? boolVal.ToString() : "?";
            case VariableType.String:
                return store.Strings.TryGetValue(variable.GUID, out var strVal) ? $"\"{strVal}\"" : "?";
            case VariableType.Vector2:
                return store.Vector2s.TryGetValue(variable.GUID, out var v2Val) ? v2Val.ToString() : "?";
            case VariableType.Vector3:
                return store.Vector3s.TryGetValue(variable.GUID, out var v3Val) ? v3Val.ToString() : "?";
            case VariableType.GameObject:
                return store.GameObjects.TryGetValue(variable.GUID, out var goVal) ? (goVal != null ? goVal.name : "null") : "?";
            case VariableType.Transform:
                return store.Transforms.TryGetValue(variable.GUID, out var trVal) ? (trVal != null ? trVal.name : "null") : "?";
            case VariableType.Color:
                return store.Colors.TryGetValue(variable.GUID, out var colVal) ? colVal.ToString() : "?";
            default:
                return "?";
        }
    }
    
    private void AddInfoLabel(string text, Color color)
    {
        var label = new Label(text)
        {
            style =
            {
                color = color,
                fontSize = 11,
                marginBottom = 3,
                whiteSpace = WhiteSpace.Normal
            }
        };
        _debugInfoScrollView.Add(label);
    }
    
    private void AddInfoSeparator()
    {
        var sep = new VisualElement
        {
            style =
            {
                height = 1,
                backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f),
                marginTop = 4,
                marginBottom = 4
            }
        };
        _debugInfoScrollView.Add(sep);
    }
    
    private void AddVariableRow(string name, string type, string value)
    {
        var row = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                marginBottom = 2,
                paddingLeft = 4,
                paddingTop = 2,
                paddingBottom = 2,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f),
                borderTopLeftRadius = 2,
                borderTopRightRadius = 2,
                borderBottomLeftRadius = 2,
                borderBottomRightRadius = 2
            }
        };
        
        var nameLabel = new Label(name)
        {
            style =
            {
                color = new Color(0.9f, 0.8f, 0.6f, 1f),
                fontSize = 11,
                width = 80,
                overflow = Overflow.Hidden
            }
        };
        
        var typeLabel = new Label($"[{type}]")
        {
            style =
            {
                color = new Color(0.5f, 0.7f, 0.5f, 1f),
                fontSize = 10,
                width = 55,
                overflow = Overflow.Hidden
            }
        };
        
        var valueLabel = new Label(value)
        {
            style =
            {
                color = Color.white,
                fontSize = 11,
                flexGrow = 1,
                overflow = Overflow.Hidden
            }
        };
        
        row.Add(nameLabel);
        row.Add(typeLabel);
        row.Add(valueLabel);
        _debugInfoScrollView.Add(row);
    }
    
    #endregion
    
    #region Debug 按钮回调
    
    private void OnDebugToggle()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[ShizukuDebugger] 调试模式只能在 Play 模式下使用");
            return;
        }
        
        ShizukuDebugger.Enabled = !ShizukuDebugger.Enabled;
        
        if (!ShizukuDebugger.Enabled)
        {
            ShizukuDebugger.Stop();
            _graphView?.ClearAllDebugVisuals();
        }
        
        RefreshDebugUIState();
        Debug.Log($"[ShizukuDebugger] 调试模式: {(ShizukuDebugger.Enabled ? "开启" : "关闭")}");
    }
    
    private void OnContinue()
    {
        if (_currentGraph == null || !ShizukuDebugger.Enabled || !ShizukuDebugger.IsPaused) return;
        
        _currentGraph.ContinueExecute();
        RefreshDebugUIState();
    }
    
    private void OnStep()
    {
        if (_currentGraph == null || !ShizukuDebugger.Enabled) return;
        
        if (!ShizukuDebugger.IsPaused && string.IsNullOrEmpty(_currentGraph.PendingResumeNodeGuid))
        {
            // 还没暂停，不能单步
            return;
        }
        
        _currentGraph.StepExecute();
        RefreshDebugUIState();
        
        // 单步后如果暂停了，聚焦到暂停节点
        if (ShizukuDebugger.IsPaused && ShizukuDebugger.CurrentSnapshot != null)
        {
            _graphView?.FocusOnNode(ShizukuDebugger.CurrentSnapshot.PausedAtNodeGuid);
        }
    }
    
    private void OnStopDebug()
    {
        ShizukuDebugger.Stop();
        _graphView?.ClearAllDebugVisuals();
        RefreshDebugUIState();
    }
    
    #endregion
    
    #region Debug 状态刷新
    
    private void RefreshDebugUIState()
    {
        bool isPlaying = EditorApplication.isPlaying;
        bool isEnabled = ShizukuDebugger.Enabled;
        bool isPaused = ShizukuDebugger.IsPaused;
        
        // Debug 开关按钮
        _debugToggleButton.style.backgroundColor = isEnabled 
            ? new Color(0.2f, 0.5f, 0.2f, 1f) 
            : new Color(0.3f, 0.3f, 0.3f, 1f);
        _debugToggleButton.text = isEnabled ? "🔧 Debug ✓" : "🔧 Debug";
        _debugToggleButton.SetEnabled(isPlaying);
        
        // 继续/单步/停止按钮
        _continueButton.SetEnabled(isEnabled && isPaused);
        _stepButton.SetEnabled(isEnabled && isPaused);
        _stopButton.SetEnabled(isEnabled);
        
        // 状态文字
        if (!isPlaying)
        {
            _debugStatusLabel.text = "⏹ 未运行";
            _debugStatusLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
        }
        else if (!isEnabled)
        {
            _debugStatusLabel.text = "▶ 运行中 (调试未开启)";
            _debugStatusLabel.style.color = new Color(0.5f, 0.7f, 0.5f);
        }
        else if (isPaused)
        {
            var snapshot = ShizukuDebugger.CurrentSnapshot;
            string nodeInfo = "";
            if (snapshot != null && _currentGraph != null)
            {
                var node = _currentGraph.Nodes.FirstOrDefault(n => n.GUID == snapshot.PausedAtNodeGuid);
                if (node != null) nodeInfo = $" @ {node.Title}";
            }
            _debugStatusLabel.text = $"⏸ 断点暂停{nodeInfo} (帧 #{snapshot?.FrameCount})";
            _debugStatusLabel.style.color = new Color(1f, 0.85f, 0.3f);
        }
        else
        {
            _debugStatusLabel.text = "▶ 调试运行中";
            _debugStatusLabel.style.color = new Color(0.5f, 0.9f, 0.5f);
        }
        
        // 刷新调试信息面板
        if (_isDebugInfoVisible)
        {
            RefreshDebugInfoPanel();
        }
    }
    
    /// <summary>
    /// 编辑器每帧更新，用于刷新调试可视化
    /// </summary>
    private void OnEditorUpdate()
    {
        if (_graphView == null) return;
        
        if (ShizukuDebugger.Enabled && EditorApplication.isPlaying)
        {
            _graphView.RefreshDebugVisuals();
            RefreshDebugUIState();
            Repaint();
        }
    }
    
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // 退出 Play 模式时停止调试
            ShizukuDebugger.Stop();
            _graphView?.ClearAllDebugVisuals();
            RefreshDebugUIState();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            RefreshDebugUIState();
        }
    }
    
    #endregion
    
    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        
        UnloadExtension();
        rootVisualElement.Clear();
        _graphView = null;
        // 不清空 _currentGraph，让它被 Unity 序列化保存，以便在 Play 模式切换后恢复
    }
    
    private void LoadExtension(ShizukuGraphBase graph)
    {
        UnloadExtension();
        
        foreach (var extension in _availableExtensions)
        {
            if (extension.CanHandle(graph))
            {
                _currentExtension = extension;
                _currentExtension.OnEnable(this, _graphView, _contentContainer);
                _currentExtension.OnGraphLoaded(graph);
                
                _graphView.OnGraphChanged += () => _currentExtension?.OnGraphLoaded(graph);
            }
        }
    }
    
    private void UnloadExtension()
    {
        if (_currentExtension != null)
        {
            _currentExtension.OnDisable();
            _currentExtension = null;
        }
    }
    
    private void RefreshExtension()
    {
        if (_currentGraph != null && _currentExtension != null)
        {
            _currentExtension.OnGraphLoaded(_currentGraph);
        }
    }
    
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        ShizukuGraphBase graphAsset = EditorUtility.InstanceIDToObject(instanceID) as ShizukuGraphBase;
        if (graphAsset != null)
        {
            ShizukuGraphWindow window = GetWindow<ShizukuGraphWindow>();
            window.titleContent = new GUIContent($"Shizuku Graph - {graphAsset.name}");
            
            window._currentGraph = graphAsset;
            window._graphView.LoadFromAsset(graphAsset);
            window.LoadExtension(graphAsset);
            window.Show();
            return true;
        }
        return false;
    }
}
