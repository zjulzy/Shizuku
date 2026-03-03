using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 变量创建对话框
/// </summary>
public class VariableCreationDialog : EditorWindow
{
    private ShizukuGraphBase _targetGraph;
    private Action _onVariableCreated;
    
    private TextField _nameField;
    private EnumField _typeField;
    
    private string _variableName = "NewVariable";
    private VariableType _variableType = VariableType.Float;
    
    public void Initialize(ShizukuGraphBase targetGraph, Action onVariableCreated)
    {
        _targetGraph = targetGraph;
        _onVariableCreated = onVariableCreated;
    }
    
    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;
        root.style.paddingLeft = 15;
        root.style.paddingRight = 15;
        
        // 标题
        var title = new Label("创建新变量")
        {
            style =
            {
                fontSize = 16,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 15,
                unityTextAlign = TextAnchor.MiddleCenter
            }
        };
        root.Add(title);
        
        // 名称输入
        _nameField = new TextField("变量名称")
        {
            value = _variableName,
            style =
            {
                marginBottom = 10
            }
        };
        _nameField.RegisterValueChangedCallback(evt =>
        {
            _variableName = evt.newValue;
        });
        root.Add(_nameField);
        
        // 类型选择
        _typeField = new EnumField("变量类型", _variableType)
        {
            style =
            {
                marginBottom = 20
            }
        };
        _typeField.RegisterValueChangedCallback(evt =>
        {
            _variableType = (VariableType)evt.newValue;
        });
        root.Add(_typeField);
        
        // 按钮容器
        var buttonContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.FlexEnd
            }
        };
        
        // 取消按钮
        var cancelButton = new Button(() => Close())
        {
            text = "取消",
            style =
            {
                width = 80,
                height = 30,
                marginRight = 10
            }
        };
        buttonContainer.Add(cancelButton);
        
        // 创建按钮
        var createButton = new Button(OnCreateClicked)
        {
            text = "创建",
            style =
            {
                width = 80,
                height = 30,
                backgroundColor = new Color(0.3f, 0.6f, 0.9f)
            }
        };
        buttonContainer.Add(createButton);
        
        root.Add(buttonContainer);
        
        // 聚焦到名称输入框
        _nameField.Focus();
        _nameField.SelectAll();
    }
    
    private void OnCreateClicked()
    {
        // 验证名称
        if (string.IsNullOrWhiteSpace(_variableName))
        {
            EditorUtility.DisplayDialog("错误", "变量名称不能为空", "确定");
            return;
        }
        
        // 检查是否重名
        if (_targetGraph.GetVariableByName(_variableName) != null)
        {
            EditorUtility.DisplayDialog("错误", $"变量名称 '{_variableName}' 已存在", "确定");
            return;
        }
        
        // 创建变量
        var newVariable = new GraphVariable(_variableName, _variableType);
        _targetGraph.AddVariable(newVariable);
        
        // 先关闭窗口
        Close();
        
        // 延迟两帧执行回调，确保窗口完全关闭且渲染完成
        DelayedCall(2, () =>
        {
            _onVariableCreated?.Invoke();
        });
    }
    
    /// <summary>
    /// 延迟指定帧数后执行回调
    /// </summary>
    private void DelayedCall(int frameCount, System.Action callback)
    {
        int remainingFrames = frameCount;
        EditorApplication.CallbackFunction updateAction = null;
        
        updateAction = () =>
        {
            remainingFrames--;
            if (remainingFrames <= 0)
            {
                EditorApplication.update -= updateAction;
                callback?.Invoke();
            }
        };
        
        EditorApplication.update += updateAction;
    }
}

