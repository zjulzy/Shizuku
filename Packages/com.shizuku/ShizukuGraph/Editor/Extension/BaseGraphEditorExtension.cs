using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public class BaseGraphEditorExtension : IGraphEditorExtension
    {
        private ShizukuGraphWindow _window;
        private ShizukuGraphView _graphView;
        private VisualElement _rootElement;
        private ShizukuGraphBase _currentGraph;

        private VisualElement _rightPanel;
        private ScrollView _nodeInspectorPanel;
        private ScrollView _variablesPanel;
        private ScrollView _functionsPanel;
        private VisualElement _horizontalResizer;
        private ShizukuNodeBase _selectedNode;

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

            // 监听节点选择事件
            if (_graphView != null)
            {
                _graphView.OnNodeSelected += OnNodeSelected;
            }
        }

        public void OnDisable()
        {
            // 取消监听节点选择事件
            if (_graphView != null)
            {
                _graphView.OnNodeSelected -= OnNodeSelected;
            }

            if (_rightPanel != null && _rightPanel.parent != null)
            {
                _rightPanel.RemoveFromHierarchy();
            }

            _rightPanel = null;
            _nodeInspectorPanel = null;
            _variablesPanel = null;
            _functionsPanel = null;
            _currentGraph = null;
            _selectedNode = null;
        }

        public void OnGraphLoaded(ShizukuGraphBase graph)
        {
            _currentGraph = graph;
            RefreshVariablesPanel();
            RefreshFunctionsPanel();
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

            // ===== 节点检查器面板 =====
            var inspectorHeader = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f),
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10
                }
            };

            var inspectorLabel = new Label("节点检查器")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            inspectorHeader.Add(inspectorLabel);
            _rightPanel.Add(inspectorHeader);

            // 节点检查器滚动视图
            _nodeInspectorPanel = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minHeight = 100,
                    maxHeight = 400,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.2f, 0.2f, 0.2f)
                }
            };
            _rightPanel.Add(_nodeInspectorPanel);

            // 初始显示提示
            RefreshNodeInspector();

            // ===== 变量列表面板 =====
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

            // ===== 函数列表面板 =====
            var functionsHeaderContainer = new VisualElement
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

            var functionsHeaderLabel = new Label("函数列表")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };

            var addFunctionButton = new Button(() => OnAddFunctionClicked())
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

            functionsHeaderContainer.Add(functionsHeaderLabel);
            functionsHeaderContainer.Add(addFunctionButton);
            _rightPanel.Add(functionsHeaderContainer);

            _functionsPanel = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            _rightPanel.Add(_functionsPanel);

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

        /// <summary>
        /// 节点选择事件处理
        /// </summary>
        private void OnNodeSelected(ShizukuNodeBase node)
        {
            _selectedNode = node;
            RefreshNodeInspector();
        }

        /// <summary>
        /// 刷新节点检查器面板
        /// </summary>
        private void RefreshNodeInspector()
        {
            _nodeInspectorPanel.Clear();

            if (_selectedNode == null)
            {
                var emptyLabel = new Label("未选择节点")
                {
                    style =
                    {
                        paddingTop = 20,
                        paddingLeft = 10,
                        color = new Color(0.6f, 0.6f, 0.6f),
                        unityTextAlign = TextAnchor.UpperCenter
                    }
                };
                _nodeInspectorPanel.Add(emptyLabel);
                return;
            }

            // 显示节点信息
            var container = new VisualElement
            {
                style =
                {
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10
                }
            };

            // 节点标题
            var titleLabel = new Label(_selectedNode.Title)
            {
                style =
                {
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10,
                    color = new Color(0.9f, 0.9f, 0.9f)
                }
            };
            container.Add(titleLabel);

            // 节点类型
            var typeLabel = new Label($"类型: {_selectedNode.GetType().Name}")
            {
                style =
                {
                    fontSize = 11,
                    marginBottom = 5,
                    color = new Color(0.7f, 0.7f, 0.7f)
                }
            };
            container.Add(typeLabel);

            // GUID
            var guidLabel = new Label($"GUID: {_selectedNode.GUID}")
            {
                style =
                {
                    fontSize = 10,
                    marginBottom = 10,
                    color = new Color(0.6f, 0.6f, 0.6f)
                }
            };
            container.Add(guidLabel);

            // 分隔线
            var separator = new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                    marginTop = 5,
                    marginBottom = 10
                }
            };
            container.Add(separator);

            // 使用反射显示所有序列化字段
            var nodeType = _selectedNode.GetType();
            var fields = nodeType.GetFields(System.Reflection.BindingFlags.Public | 
                                           System.Reflection.BindingFlags.NonPublic | 
                                           System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                // 只显示有 SerializeField 或 SerializeReference 特性的字段，或者是 public 字段
                bool isSerializable = field.IsPublic || 
                                     field.GetCustomAttributes(typeof(SerializeField), true).Length > 0 ||
                                     field.GetCustomAttributes(typeof(SerializeReference), true).Length > 0;

                if (!isSerializable)
                    continue;

                // 跳过端口字段（这些已经在节点上显示了）
                if (typeof(ParameterEdgePort).IsAssignableFrom(field.FieldType) || 
                    typeof(ChainPort).IsAssignableFrom(field.FieldType))
                    continue;

                var fieldElement = CreateFieldEditor(field, _selectedNode);
                if (fieldElement != null)
                {
                    container.Add(fieldElement);
                }
            }

            _nodeInspectorPanel.Add(container);
        }

        /// <summary>
        /// 为字段创建编辑器
        /// </summary>
        private VisualElement CreateFieldEditor(System.Reflection.FieldInfo field, ShizukuNodeBase node)
        {
            var fieldType = field.FieldType;
            var fieldName = field.Name;
            var fieldValue = field.GetValue(node);

            // 特殊处理：如果是变量节点的 VariableGUID 字段，有一说一这里有点硬
            if (fieldName == "VariableGUID" && fieldType == typeof(string))
            {
                return CreateVariableGUIDSelector(field, node);
            }

            // 处理常见类型
            if (fieldType == typeof(string))
            {
                var textField = new TextField(ObjectNames.NicifyVariableName(fieldName))
                {
                    value = fieldValue as string ?? "",
                    style = { marginBottom = 5 }
                };
                textField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(node, evt.newValue);
                    if (_currentGraph != null) EditorUtility.SetDirty(_currentGraph);
                });
                return textField;
            }
            else if (fieldType == typeof(int))
            {
                var intField = new IntegerField(ObjectNames.NicifyVariableName(fieldName))
                {
                    value = (int)fieldValue,
                    style = { marginBottom = 5 }
                };
                intField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(node, evt.newValue);
                    if (_currentGraph != null) EditorUtility.SetDirty(_currentGraph);
                });
                return intField;
            }
            else if (fieldType == typeof(float))
            {
                var floatField = new FloatField(ObjectNames.NicifyVariableName(fieldName))
                {
                    value = (float)fieldValue,
                    style = { marginBottom = 5 }
                };
                floatField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(node, evt.newValue);
                    if (_currentGraph != null) EditorUtility.SetDirty(_currentGraph);
                });
                return floatField;
            }
            else if (fieldType == typeof(bool))
            {
                var boolField = new Toggle(ObjectNames.NicifyVariableName(fieldName))
                {
                    value = (bool)fieldValue,
                    style = { marginBottom = 5 }
                };
                boolField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(node, evt.newValue);
                    if (_currentGraph != null) EditorUtility.SetDirty(_currentGraph);
                });
                return boolField;
            }
            else if (fieldType.IsEnum)
            {
                var enumField = new EnumField(ObjectNames.NicifyVariableName(fieldName), (System.Enum)fieldValue)
                {
                    style = { marginBottom = 5 }
                };
                enumField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(node, evt.newValue);
                    if (_currentGraph != null) EditorUtility.SetDirty(_currentGraph);
                });
                return enumField;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                var objectField = new ObjectField(ObjectNames.NicifyVariableName(fieldName))
                {
                    objectType = fieldType,
                    value = fieldValue as UnityEngine.Object,
                    style = { marginBottom = 5 }
                };
                objectField.RegisterValueChangedCallback(evt =>
                {
                    field.SetValue(node, evt.newValue);
                    if (_currentGraph != null) EditorUtility.SetDirty(_currentGraph);
                });
                return objectField;
            }

            // 其他类型显示只读标签
            var label = new Label($"{ObjectNames.NicifyVariableName(fieldName)}: {fieldValue?.ToString() ?? "null"}")
            {
                style =
                {
                    fontSize = 11,
                    marginBottom = 5,
                    color = new Color(0.7f, 0.7f, 0.7f)
                }
            };
            return label;
        }

        /// <summary>
        /// 为变量节点创建变量选择器
        /// </summary>
        private VisualElement CreateVariableGUIDSelector(System.Reflection.FieldInfo field, ShizukuNodeBase node)
        {
            var container = new VisualElement
            {
                style = { marginBottom = 10 }
            };

            string currentGuid = field.GetValue(node) as string;

            // 获取节点的目标变量类型（如果实现了 IVariableNode）
            VariableType? targetType = null;
            if (node is IVariableNode variableNode)
            {
                targetType = variableNode.TargetVariableType;
            }

            // 构建变量选项列表（按类型过滤）
            var choices = new List<string> { "<未选择>" };
            var guidMap = new Dictionary<string, string>(); // 显示名 -> GUID

            if (_currentGraph != null)
            {
                foreach (var variable in _currentGraph.Variables)
                {
                    // 如果节点指定了目标类型，则只显示匹配类型的变量
                    if (targetType.HasValue && variable.Type != targetType.Value)
                        continue;

                    string displayName = $"{variable.Name} ({variable.Type})";
                    choices.Add(displayName);
                    guidMap[displayName] = variable.GUID;
                }
            }

            // 找到当前选中项的索引
            int currentIndex = 0;
            if (!string.IsNullOrEmpty(currentGuid) && _currentGraph != null)
            {
                var currentVar = _currentGraph.GetVariableByGUID(currentGuid);
                if (currentVar != null)
                {
                    string currentDisplay = $"{currentVar.Name} ({currentVar.Type})";
                    currentIndex = choices.IndexOf(currentDisplay);
                    if (currentIndex < 0) currentIndex = 0;
                }
            }

            // 创建下拉菜单
            var popupField = new PopupField<string>("目标变量", choices, currentIndex, 
                formatSelectedValueCallback: choice => choice,
                formatListItemCallback: choice => choice);
            popupField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "<未选择>")
                {
                    field.SetValue(node, "");
                }
                else if (guidMap.TryGetValue(evt.newValue, out string selectedGuid))
                {
                    field.SetValue(node, selectedGuid);
                }

                // 刷新节点标题
                if (_graphView != null)
                {
                    _graphView.RefreshNodeTitle(node);
                }

                // 标记为脏
                if (_currentGraph != null)
                {
                    EditorUtility.SetDirty(_currentGraph);
                }
            });

            container.Add(popupField);

            return container;
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

        #region 函数列表

        private void OnAddFunctionClicked()
        {
            if (_currentGraph == null) return;

            var baseName = "NewFunction";
            var name = baseName;
            int index = 1;
            while (_currentGraph.GetMethodByName(name) != null)
            {
                name = $"{baseName}_{index++}";
            }

            var method = new ShizukuMethod(name);

            // 自动创建入口节点
            var entryNode = new MethodEntryNode
            {
                MethodGUID = method.GUID,
                PositionAndSize = new Unity.Mathematics.float4(100, 200, 200, 100)
            };
            method.AddNode(entryNode);
            method.EntryNodeGUID = entryNode.GUID;

            _currentGraph.AddMethod(method);
            EditorUtility.SetDirty(_currentGraph);

            RefreshFunctionsPanel();
            Debug.Log($"已创建函数: {name}");
        }

        private void RefreshFunctionsPanel()
        {
            if (_functionsPanel == null) return;
            _functionsPanel.Clear();

            if (_currentGraph == null) return;

            var methods = _currentGraph.Methods;
            if (methods.Count == 0)
            {
                _functionsPanel.Add(new Label("暂无函数")
                {
                    style =
                    {
                        paddingTop = 10,
                        paddingLeft = 10,
                        color = new Color(0.6f, 0.6f, 0.6f),
                        unityTextAlign = TextAnchor.UpperCenter
                    }
                });
                return;
            }

            foreach (var method in methods)
            {
                var methodCard = CreateMethodCard(method);
                _functionsPanel.Add(methodCard);
            }
        }

        /// <summary>
        /// 创建函数卡片（包含标题行 + 可展开的参数编辑区域）
        /// </summary>
        private VisualElement CreateMethodCard(ShizukuMethod method)
        {
            var card = new VisualElement
            {
                style =
                {
                    marginTop = 4,
                    marginBottom = 4,
                    marginLeft = 5,
                    marginRight = 5,
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            // ===== 标题行 =====
            var headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingLeft = 8,
                    paddingRight = 5,
                    paddingTop = 6,
                    paddingBottom = 6,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center
                }
            };

            var isEditing = _graphView != null && _graphView.CurrentMethod == method;

            // 展开/折叠箭头
            var foldoutArrow = new Label("▶")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.6f, 0.6f, 0.6f),
                    width = 14,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginRight = 4
                }
            };

            var nameLabel = new Label($"ƒ  {method.Name}")
            {
                style =
                {
                    flexGrow = 1,
                    color = isEditing ? new Color(0.4f, 0.8f, 1f) : new Color(0.85f, 0.85f, 0.85f),
                    unityFontStyleAndWeight = isEditing ? FontStyle.Bold : FontStyle.Normal,
                    fontSize = 12
                }
            };

            var paramCount = method.InputParameters.Count;
            var returnCount = method.OutputParameters.Count;
            var hintLabel = new Label($"({paramCount}→{returnCount})")
            {
                style =
                {
                    color = new Color(0.5f, 0.5f, 0.5f),
                    fontSize = 10,
                    marginRight = 5
                }
            };

            var deleteBtn = new Button(() => OnDeleteMethod(method))
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

            headerRow.Add(foldoutArrow);
            headerRow.Add(nameLabel);
            headerRow.Add(hintLabel);
            headerRow.Add(deleteBtn);
            card.Add(headerRow);

            // ===== 参数编辑区域（默认折叠） =====
            var detailPanel = new VisualElement
            {
                style =
                {
                    display = DisplayStyle.None,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingBottom = 8
                }
            };

            // 函数名编辑
            var nameRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 6
                }
            };
            nameRow.Add(new Label("名称") { style = { width = 50, color = new Color(0.7f, 0.7f, 0.7f), fontSize = 11 } });
            var nameField = new TextField { value = method.Name, style = { flexGrow = 1 } };
            nameField.RegisterValueChangedCallback(evt =>
            {
                if (!string.IsNullOrWhiteSpace(evt.newValue) && _currentGraph.RenameMethod(method.GUID, evt.newValue))
                {
                    nameLabel.text = $"ƒ  {evt.newValue}";
                    EditorUtility.SetDirty(_currentGraph);
                    SyncAllInvokeMethodNodes(method);
                }
            });
            nameRow.Add(nameField);
            detailPanel.Add(nameRow);

            // --- 输入参数区域 ---
            detailPanel.Add(CreateParameterSection("输入参数", method.InputParameters, method));

            // --- 输出参数区域 ---
            detailPanel.Add(CreateParameterSection("输出参数（返回值）", method.OutputParameters, method));

            card.Add(detailPanel);

            // ===== 交互 =====
            // 双击进入函数编辑
            var capturedMethod = method;
            nameLabel.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    OnEnterMethod(capturedMethod);
                    evt.StopPropagation();
                }
            });

            // 单击标题行展开/折叠
            bool expanded = false;
            foldoutArrow.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    expanded = !expanded;
                    detailPanel.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                    foldoutArrow.text = expanded ? "▼" : "▶";
                    evt.StopPropagation();
                }
            });

            // 悬停效果
            headerRow.RegisterCallback<MouseEnterEvent>(evt =>
            {
                headerRow.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            });
            headerRow.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                headerRow.style.backgroundColor = new Color(0, 0, 0, 0);
            });

            return card;
        }

        /// <summary>
        /// 创建参数列表编辑区域（输入参数或输出参数）
        /// </summary>
        private VisualElement CreateParameterSection(string sectionTitle, List<MethodParameter> parameters, ShizukuMethod method)
        {
            var section = new VisualElement { style = { marginTop = 6, marginBottom = 2 } };

            var sectionHeader = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };
            sectionHeader.Add(new Label(sectionTitle)
            {
                style =
                {
                    fontSize = 11,
                    color = new Color(0.6f, 0.8f, 0.6f),
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });

            var addBtn = new Button(() =>
            {
                parameters.Add(new MethodParameter($"param{parameters.Count}", VariableType.Float));
                EditorUtility.SetDirty(_currentGraph);
                OnMethodParametersChanged(method);
                RefreshFunctionsPanel();
            })
            {
                text = "+",
                style =
                {
                    width = 22,
                    height = 20,
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleCenter
                }
            };
            sectionHeader.Add(addBtn);
            section.Add(sectionHeader);

            for (int i = 0; i < parameters.Count; i++)
            {
                section.Add(CreateParameterRow(parameters, i, method));
            }

            if (parameters.Count == 0)
            {
                section.Add(new Label("  （无）")
                {
                    style = { color = new Color(0.5f, 0.5f, 0.5f), fontSize = 10, marginLeft = 4 }
                });
            }

            return section;
        }

        /// <summary>
        /// 创建单个参数编辑行（名称 + 类型下拉 + 删除按钮）
        /// </summary>
        private VisualElement CreateParameterRow(List<MethodParameter> parameters, int index, ShizukuMethod method)
        {
            var param = parameters[index];
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 3,
                    marginLeft = 8
                }
            };

            var nameField = new TextField
            {
                value = param.Name,
                style = { flexGrow = 1, marginRight = 4 }
            };
            nameField.RegisterValueChangedCallback(evt =>
            {
                if (!string.IsNullOrWhiteSpace(evt.newValue))
                {
                    param.Name = evt.newValue;
                    EditorUtility.SetDirty(_currentGraph);
                    OnMethodParametersChanged(method);
                }
            });

            var typeField = new EnumField(param.Type)
            {
                style = { width = 85, marginRight = 4 }
            };
            typeField.RegisterValueChangedCallback(evt =>
            {
                param.Type = (VariableType)evt.newValue;
                EditorUtility.SetDirty(_currentGraph);
                OnMethodParametersChanged(method);
            });

            var deleteBtn = new Button(() =>
            {
                parameters.RemoveAt(index);
                EditorUtility.SetDirty(_currentGraph);
                OnMethodParametersChanged(method);
                RefreshFunctionsPanel();
            })
            {
                text = "×",
                style =
                {
                    width = 20,
                    height = 20,
                    fontSize = 12,
                    backgroundColor = new Color(0.6f, 0.25f, 0.25f),
                    color = Color.white,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            row.Add(nameField);
            row.Add(typeField);
            row.Add(deleteBtn);
            return row;
        }

        /// <summary>
        /// 当函数参数定义改变后，同步所有相关节点的端口
        /// </summary>
        private void OnMethodParametersChanged(ShizukuMethod method)
        {
            // 同步入口节点
            if (!string.IsNullOrEmpty(method.EntryNodeGUID))
            {
                var entryNode = method.GetNodeByGUID(method.EntryNodeGUID) as MethodEntryNode;
                entryNode?.SyncPortsFromMethod(method);
            }

            // 同步返回节点
            if (!string.IsNullOrEmpty(method.ReturnNodeGUID))
            {
                var returnNode = method.GetNodeByGUID(method.ReturnNodeGUID) as MethodReturnNode;
                returnNode?.SyncPortsFromMethod(method);
            }

            // 同步所有调用该函数的 InvokeMethodNode
            SyncAllInvokeMethodNodes(method);

            EditorUtility.SetDirty(_currentGraph);

            // 刷新图视图
            if (_graphView != null)
            {
                _graphView.RefreshCurrentView();
            }
        }

        /// <summary>
        /// 同步所有引用指定函数的 InvokeMethodNode
        /// </summary>
        private void SyncAllInvokeMethodNodes(ShizukuMethod method)
        {
            if (_currentGraph == null) return;

            // 主图
            foreach (var node in _currentGraph.Nodes)
            {
                if (node is InvokeMethodNode invokeNode && invokeNode.TargetMethodGUID == method.GUID)
                {
                    invokeNode.SyncPortsFromMethod(method);
                }
            }

            // 所有函数子图
            foreach (var m in _currentGraph.Methods)
            {
                foreach (var node in m.Nodes)
                {
                    if (node is InvokeMethodNode invokeNode && invokeNode.TargetMethodGUID == method.GUID)
                    {
                        invokeNode.SyncPortsFromMethod(method);
                    }
                }
            }
        }

        private void OnEnterMethod(ShizukuMethod method)
        {
            if (_window != null)
            {
                _window.EnterMethodGraph(method);
                RefreshFunctionsPanel();
            }
        }

        private void OnDeleteMethod(ShizukuMethod method)
        {
            if (_currentGraph == null) return;

            if (_graphView != null && _graphView.CurrentMethod == method)
            {
                _graphView.ReturnToMainGraph();
            }

            if (EditorUtility.DisplayDialog("删除函数",
                $"确定要删除函数 \"{method.Name}\" 吗？\n函数内的所有节点和边都将被删除。",
                "删除", "取消"))
            {
                _currentGraph.RemoveMethod(method.GUID);
                EditorUtility.SetDirty(_currentGraph);
                RefreshFunctionsPanel();
                Debug.Log($"已删除函数: {method.Name}");
            }
        }

        #endregion

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

}
