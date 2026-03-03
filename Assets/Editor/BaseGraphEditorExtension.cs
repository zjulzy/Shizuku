using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

public class BaseGraphEditorExtension : IGraphEditorExtension
{
    private ShizukuGraphWindow _window;
    private ShizukuGraphView _graphView;
    private VisualElement _rootElement;
    private ShizukuGraphBase _currentGraph;
    
    private VisualElement _rightPanel;
    private ScrollView _variablesPanel;
    private VisualElement _horizontalResizer;
    
    public bool CanHandle(ShizukuGraphBase graph)
    {
        // 只要是 ShizukuGraphBase 的图都可以处理
        return graph != null;
    }

    public void OnEnable(ShizukuGraphWindow window, ShizukuGraphView graphView, VisualElement rootElement)
    {
        _window = window;
        _graphView = graphView;
        _rootElement = rootElement;
        
        BuildUI();
    }

    public void OnDisable()
    {
        if (_rightPanel != null && _rightPanel.parent != null)
        {
            _rightPanel.RemoveFromHierarchy();
        }
        
        _rightPanel = null;
        _variablesPanel = null;
        _currentGraph = null;
    }

    public void OnGraphLoaded(ShizukuGraphBase graph)
    {
        _currentGraph = graph;
        RefreshVariablesPanel();
    }
    
    private void BuildUI()
    {
        _rightPanel = new VisualElement
        {
            style =
            {
                width = 300,
                borderLeftWidth = 1,
                borderLeftColor = new Color(0.2f, 0.2f, 0.2f),
                backgroundColor = new Color(0.22f, 0.22f, 0.22f),
                flexDirection = FlexDirection.Column
            }
        };
        
        // 变量列表标题栏（带添加按钮）
        var variablesHeader = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                paddingTop = 8,
                paddingBottom = 8,
                paddingLeft = 10,
                paddingRight = 10,
                justifyContent = Justify.SpaceBetween
            }
        };
        
        var headerLabel = new Label("变量列表")
        {
            style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 14,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };
        
        var addButton = new Button(() => AddNewVariable())
        {
            text = "+",
            style =
            {
                width = 24,
                height = 24,
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 16
            }
        };
        
        variablesHeader.Add(headerLabel);
        variablesHeader.Add(addButton);
        _rightPanel.Add(variablesHeader);
        
        // 变量列表滚动视图
        _variablesPanel = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1
            }
        };
        _rightPanel.Add(_variablesPanel);
        
        // 水平拖拽条（调整右侧面板宽度）
        _horizontalResizer = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0,
                top = 0,
                bottom = 0,
                width = 8,
                cursor = new Cursor() { texture = null, hotspot = Vector2.zero }
            }
        };
        
        // 添加悬停效果
        _horizontalResizer.RegisterCallback<MouseEnterEvent>(evt =>
        {
            _horizontalResizer.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
        });
        _horizontalResizer.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            if (!_isResizingHorizontal)
            {
                _horizontalResizer.style.backgroundColor = Color.clear;
            }
        });
        
        // 添加拖动功能
        _horizontalResizer.RegisterCallback<MouseDownEvent>(OnHorizontalResizerMouseDown);
        
        _rightPanel.Add(_horizontalResizer);
        
        // 添加到根容器
        _rootElement.Add(_rightPanel);
    }
    
    private void RefreshVariablesPanel()
    {
        _variablesPanel.Clear();
        
        if (_currentGraph == null)
        {
            var emptyLabel = new Label("未加载图数据")
            {
                style =
                {
                    paddingTop = 20,
                    paddingLeft = 10,
                    color = new Color(0.6f, 0.6f, 0.6f),
                    unityTextAlign = TextAnchor.UpperCenter
                }
            };
            _variablesPanel.Add(emptyLabel);
            return;
        }
        
        // 显示所有变量
        if (_currentGraph.Variables.Count == 0)
        {
            var emptyLabel = new Label("暂无变量")
            {
                style =
                {
                    paddingTop = 20,
                    paddingLeft = 10,
                    color = new Color(0.6f, 0.6f, 0.6f),
                    unityTextAlign = TextAnchor.UpperCenter
                }
            };
            _variablesPanel.Add(emptyLabel);
        }
        else
        {
            foreach (var variable in _currentGraph.Variables)
            {
                var variableItem = CreateVariableItem(variable);
                _variablesPanel.Add(variableItem);
            }
        }
    }
    
    private VisualElement CreateVariableItem(GraphVariable variable)
    {
        var container = new VisualElement
        {
            style =
            {
                marginTop = 5,
                marginBottom = 5,
                marginLeft = 5,
                marginRight = 5,
                paddingTop = 8,
                paddingBottom = 8,
                paddingLeft = 8,
                paddingRight = 8,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                borderTopLeftRadius = 4,
                borderTopRightRadius = 4,
                borderBottomLeftRadius = 4,
                borderBottomRightRadius = 4
            }
        };
        
        // 标题行（名称 + 类型 + 删除按钮）
        var headerRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.SpaceBetween,
                alignItems = Align.Center,
                marginBottom = 8
            }
        };
        
        // 名称输入框
        var nameField = new TextField
        {
            value = variable.Name,
            style =
            {
                flexGrow = 1,
                marginRight = 5
            }
        };
        nameField.RegisterValueChangedCallback(evt =>
        {
            if (!string.IsNullOrWhiteSpace(evt.newValue))
            {
                variable.Name = evt.newValue;
                EditorUtility.SetDirty(_currentGraph);
            }
        });
        headerRow.Add(nameField);
        
        // 类型下拉框
        var typeField = new EnumField(variable.Type)
        {
            style =
            {
                width = 100,
                marginRight = 5
            }
        };
        typeField.RegisterValueChangedCallback(evt =>
        {
            variable.Type = (VariableType)evt.newValue;
            EditorUtility.SetDirty(_currentGraph);
            RefreshVariablesPanel(); // 重新刷新以显示对应类型的值编辑器
        });
        headerRow.Add(typeField);
        
        // 删除按钮
        var deleteButton = new Button(() => OnDeleteVariable(variable))
        {
            text = "×",
            style =
            {
                width = 24,
                height = 24,
                backgroundColor = new Color(0.8f, 0.3f, 0.3f),
                color = Color.white,
                unityTextAlign = TextAnchor.MiddleCenter,
                fontSize = 16,
                unityFontStyleAndWeight = FontStyle.Bold
            }
        };
        headerRow.Add(deleteButton);
        
        container.Add(headerRow);
        
        // 值编辑器
        var valueEditor = CreateValueEditor(variable);
        if (valueEditor != null)
        {
            container.Add(valueEditor);
        }
        
        return container;
    }
    
    private VisualElement CreateValueEditor(GraphVariable variable)
    {
        VisualElement editor = null;
        
        switch (variable.Type)
        {
            case VariableType.Int:
                var intField = new IntegerField("默认值")
                {
                    value = variable.IntValue
                };
                intField.RegisterValueChangedCallback(evt =>
                {
                    variable.IntValue = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = intField;
                break;
                
            case VariableType.Float:
                var floatField = new FloatField("默认值")
                {
                    value = variable.FloatValue
                };
                floatField.RegisterValueChangedCallback(evt =>
                {
                    variable.FloatValue = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = floatField;
                break;
                
            case VariableType.Bool:
                var boolField = new Toggle("默认值")
                {
                    value = variable.BoolValue
                };
                boolField.RegisterValueChangedCallback(evt =>
                {
                    variable.BoolValue = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = boolField;
                break;
                
            case VariableType.String:
                var stringField = new TextField("默认值")
                {
                    value = variable.StringValue,
                    multiline = true
                };
                stringField.RegisterValueChangedCallback(evt =>
                {
                    variable.StringValue = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = stringField;
                break;
                
            case VariableType.Vector2:
                var vector2Field = new Vector2Field("默认值")
                {
                    value = variable.Vector2Value
                };
                vector2Field.RegisterValueChangedCallback(evt =>
                {
                    variable.Vector2Value = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = vector2Field;
                break;
                
            case VariableType.Vector3:
                var vector3Field = new Vector3Field("默认值")
                {
                    value = variable.Vector3Value
                };
                vector3Field.RegisterValueChangedCallback(evt =>
                {
                    variable.Vector3Value = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = vector3Field;
                break;
                
            case VariableType.GameObject:
                var gameObjectField = new ObjectField("默认值")
                {
                    objectType = typeof(GameObject),
                    value = variable.GameObjectValue
                };
                gameObjectField.RegisterValueChangedCallback(evt =>
                {
                    variable.GameObjectValue = evt.newValue as GameObject;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = gameObjectField;
                break;
                
            case VariableType.Transform:
                var transformField = new ObjectField("默认值")
                {
                    objectType = typeof(Transform),
                    value = variable.TransformValue
                };
                transformField.RegisterValueChangedCallback(evt =>
                {
                    variable.TransformValue = evt.newValue as Transform;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = transformField;
                break;
                
            case VariableType.Color:
                var colorField = new ColorField("默认值")
                {
                    value = variable.ColorValue
                };
                colorField.RegisterValueChangedCallback(evt =>
                {
                    variable.ColorValue = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                });
                editor = colorField;
                break;
        }
        
        return editor;
    }
    
    private void OnDeleteVariable(GraphVariable variable)
    {
        if (EditorUtility.DisplayDialog("删除变量", 
            $"确定要删除变量 '{variable.Name}' 吗？\n\n" +
            "注意：所有引用该变量的节点将失效。", 
            "删除", "取消"))
        {
            _currentGraph.RemoveVariable(variable.GUID);
            EditorUtility.SetDirty(_currentGraph);
            RefreshVariablesPanel();
        }
    }
    
    private void AddNewVariable()
    {
        if (_currentGraph == null)
        {
            Debug.LogWarning("没有加载的图，无法添加变量");
            return;
        }
        
        // 创建新变量对话框
        var window = EditorWindow.GetWindow<VariableCreationDialog>(true, "添加新变量", true);
        window.minSize = new Vector2(400, 200);
        window.maxSize = new Vector2(400, 200);
        window.Initialize(_currentGraph, () =>
        {
            EditorUtility.SetDirty(_currentGraph);
            RefreshVariablesPanel();
        });
        window.ShowModal();
    }
    
    private bool _isResizingHorizontal = false;
    private float _startMouseX;
    private float _startPanelWidth;
    
    private void OnHorizontalResizerMouseDown(MouseDownEvent evt)
    {
        if (evt.button == 0) // 左键
        {
            _isResizingHorizontal = true;
            _startMouseX = evt.mousePosition.x;
            _startPanelWidth = _rightPanel.resolvedStyle.width;
            
            _horizontalResizer.CaptureMouse();
            _horizontalResizer.RegisterCallback<MouseMoveEvent>(OnHorizontalResizerMouseMove);
            _horizontalResizer.RegisterCallback<MouseUpEvent>(OnHorizontalResizerMouseUp);
            
            evt.StopPropagation();
        }
    }
    
    private void OnHorizontalResizerMouseMove(MouseMoveEvent evt)
    {
        if (_isResizingHorizontal)
        {
            float deltaX = evt.mousePosition.x - _startMouseX;
            // 注意：右侧面板是从右向左拖动，所以 deltaX 需要取反
            float newWidth = Mathf.Clamp(_startPanelWidth - deltaX, 150, 600);
            
            _rightPanel.style.width = newWidth;
            
            evt.StopPropagation();
        }
    }
    
    private void OnHorizontalResizerMouseUp(MouseUpEvent evt)
    {
        if (_isResizingHorizontal)
        {
            _isResizingHorizontal = false;
            _horizontalResizer.ReleaseMouse();
            _horizontalResizer.UnregisterCallback<MouseMoveEvent>(OnHorizontalResizerMouseMove);
            _horizontalResizer.UnregisterCallback<MouseUpEvent>(OnHorizontalResizerMouseUp);
            
            // 恢复分隔条颜色
            _horizontalResizer.style.backgroundColor = Color.clear;
            
            evt.StopPropagation();
        }
    }
}
