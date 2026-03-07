using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Generator
{
    /// <summary>
    /// Shizuku 生成器统一管理窗口
    /// 包含蓝图生成器和 ShizukuClass/Function 生成器两个页签
    /// </summary>
    public class ShizukuGeneratorWindow : EditorWindow
{
    private enum TabType
    {
        Blueprint,
        ShizukuClassAndFunction
    }

    private TabType _currentTab = TabType.Blueprint;
    
    // 各个生成器的实例
    private BlueprintGeneratorTab _blueprintTab;
    private UnifiedShizukuGeneratorTab _unifiedTab;
    
    private VisualElement _tabButtonContainer;
    private VisualElement _contentContainer;

    [MenuItem("Shizuku/Generator Window")]
    public static void OpenWindow()
    {
        var window = GetWindow<ShizukuGeneratorWindow>();
        window.titleContent = new GUIContent("Shizuku Generator");
        window.minSize = new Vector2(700, 500);
    }

    private void OnEnable()
    {
        // 初始化生成器实例
        _blueprintTab = new BlueprintGeneratorTab();
        _unifiedTab = new UnifiedShizukuGeneratorTab();
        
        BuildUI();
        SwitchTab(_currentTab);
    }

    private void BuildUI()
    {
        rootVisualElement.Clear();

        // 标题栏
        var titleBar = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.SpaceBetween,
                paddingTop = 10,
                paddingBottom = 10,
                paddingLeft = 10,
                paddingRight = 10,
                backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f)
            }
        };

        var titleLabel = new Label("Shizuku Generator")
        {
            style =
            {
                fontSize = 16,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = Color.white
            }
        };

        titleBar.Add(titleLabel);
        rootVisualElement.Add(titleBar);

        // 主容器（左侧 Tab 栏 + 右侧内容区）
        var mainContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 1
            }
        };

        // 左侧 Tab 按钮区域（垂直布局）
        _tabButtonContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f),
                borderRightWidth = 1,
                borderRightColor = new Color(0.1f, 0.1f, 0.1f, 1f),
                minWidth = 200,
                paddingTop = 5,
                paddingBottom = 5
            }
        };

        CreateTabButton("Blueprint Generator", TabType.Blueprint, "🎨");
        CreateTabButton("ShizukuClass & Function", TabType.ShizukuClassAndFunction, "📦");

        mainContainer.Add(_tabButtonContainer);

        // 右侧内容区域
        _contentContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f)
            }
        };

        mainContainer.Add(_contentContainer);
        rootVisualElement.Add(mainContainer);
    }

    private void CreateTabButton(string label, TabType tabType, string icon)
    {
        var button = new Button(() => SwitchTab(tabType))
        {
            text = $"{icon} {label}",
            style =
            {
                paddingTop = 12,
                paddingBottom = 12,
                paddingLeft = 15,
                paddingRight = 15,
                borderTopWidth = 0,
                borderLeftWidth = 0,
                borderRightWidth = 0,
                borderBottomWidth = 0,
                marginTop = 2,
                marginBottom = 2,
                marginLeft = 5,
                marginRight = 5,
                backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f),
                unityTextAlign = TextAnchor.MiddleLeft,
                fontSize = 13
            }
        };

        // 根据当前选中状态设置样式
        if (tabType == _currentTab)
        {
            button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            button.style.borderLeftWidth = 3;
            button.style.borderLeftColor = new Color(0.3f, 0.6f, 0.9f, 1f);
        }

        _tabButtonContainer.Add(button);
    }

    private void SwitchTab(TabType tabType)
    {
        _currentTab = tabType;
        
        // 重建 tab 按钮以更新样式
        _tabButtonContainer.Clear();
        CreateTabButton("Blueprint Generator", TabType.Blueprint, "🎨");
        CreateTabButton("ShizukuClass & Function", TabType.ShizukuClassAndFunction, "📦");
        
        // 切换内容
        _contentContainer.Clear();
        
        switch (tabType)
        {
            case TabType.Blueprint:
                _blueprintTab.BuildUI(_contentContainer);
                break;
            case TabType.ShizukuClassAndFunction:
                _unifiedTab.BuildUI(_contentContainer);
                break;
        }
    }
}
}

