using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 变量类型代码生成器
/// 为所有 ShizukuClass 类型自动生成对应的 VariableType 枚举值和 GraphVariable 字段
/// </summary>
public class VariableTypeGenerator : EditorWindow
{
    private const string ENUM_OUTPUT_PATH = "Assets/Scripts/Graph/Generated/VariableType.Generated.cs";
    private const string VARIABLE_OUTPUT_PATH = "Assets/Scripts/Graph/Generated/GraphVariable.Generated.cs";
    private const string GRAPH_BASE_OUTPUT_PATH = "Assets/Scripts/Graph/Generated/ShizukuGraphBase.Generated.cs";
    
    private List<ShizukuClassInfo> _customTypes = new List<ShizukuClassInfo>();
    private ScrollView _scrollView;
    private Label _statusLabel;

    [MenuItem("Shizuku/Generate Variable Types")]
    public static void OpenWindow()
    {
        var window = GetWindow<VariableTypeGenerator>();
        window.titleContent = new GUIContent("Variable Type Generator");
        window.minSize = new Vector2(600, 400);
    }

    private void OnEnable()
    {
        BuildUI();
        ScanCustomTypes();
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

        var titleLabel = new Label("Variable Type Generator")
        {
            style =
            {
                fontSize = 16,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = Color.white
            }
        };

        var buttonContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row
            }
        };

        var refreshButton = new Button(ScanCustomTypes)
        {
            text = "🔄 Refresh",
            style = { marginRight = 5 }
        };

        var generateButton = new Button(GenerateAll)
        {
            text = "Generate All",
            style =
            {
                backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.8f)
            }
        };

        buttonContainer.Add(refreshButton);
        buttonContainer.Add(generateButton);
        titleBar.Add(titleLabel);
        titleBar.Add(buttonContainer);
        rootVisualElement.Add(titleBar);

        // 状态栏
        var statusContainer = new VisualElement
        {
            style =
            {
                paddingTop = 5,
                paddingBottom = 5,
                paddingLeft = 10,
                paddingRight = 10,
                backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f)
            }
        };

        _statusLabel = new Label("Ready")
        {
            style =
            {
                color = Color.white
            }
        };

        statusContainer.Add(_statusLabel);
        rootVisualElement.Add(statusContainer);

        // 内容区域
        _scrollView = new ScrollView(ScrollViewMode.Vertical)
        {
            style =
            {
                flexGrow = 1
            }
        };

        rootVisualElement.Add(_scrollView);
    }

    private void ScanCustomTypes()
    {
        _customTypes.Clear();
        _scrollView.Clear();

        // 确保注册中心已初始化
        ShizukuTypeRegistry.Initialize();

        var allTypes = ShizukuTypeRegistry.GetAllShizukuClassInfos()
            .Where(c => c.ShowInVariableMenu)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.DisplayName)
            .ToList();

        _customTypes.AddRange(allTypes);

        UpdateUI();

        _statusLabel.text = $"Found {_customTypes.Count} custom type(s)";
    }

    private void UpdateUI()
    {
        _scrollView.Clear();

        if (_customTypes.Count == 0)
        {
            var noTypeLabel = new Label("No ShizukuClass types found.\nAdd [ShizukuClass] attribute to your classes.")
            {
                style =
                {
                    fontSize = 14,
                    color = Color.yellow,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    paddingTop = 50,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            _scrollView.Add(noTypeLabel);
            return;
        }

        // 分组显示
        var grouped = _customTypes.GroupBy(t => t.Category);

        foreach (var group in grouped)
        {
            // 分类标题
            var categoryHeader = new Label($"📦 {group.Key}")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingTop = 10,
                    paddingBottom = 5,
                    paddingLeft = 10,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f)
                }
            };
            _scrollView.Add(categoryHeader);

            // 类型列表
            foreach (var typeInfo in group)
            {
                var typeItem = CreateTypeItem(typeInfo);
                _scrollView.Add(typeItem);
            }
        }
    }

    private VisualElement CreateTypeItem(ShizukuClassInfo typeInfo)
    {
        var container = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.SpaceBetween,
                paddingTop = 5,
                paddingBottom = 5,
                paddingLeft = 20,
                paddingRight = 10,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f),
                marginBottom = 2
            }
        };

        var infoLabel = new Label($"{typeInfo.DisplayName} ({typeInfo.Type.Name})")
        {
            style =
            {
                flexGrow = 1,
                fontSize = 12
            }
        };

        var enumValueLabel = new Label($"Enum: Custom_{typeInfo.Type.Name}")
        {
            style =
            {
                fontSize = 10,
                color = new Color(0.7f, 0.7f, 0.9f, 1f)
            }
        };

        container.Add(infoLabel);
        container.Add(enumValueLabel);

        return container;
    }

    private void GenerateAll()
    {
        if (_customTypes.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No custom types to generate!", "OK");
            return;
        }

        try
        {
            // 确保输出目录存在
            var dir = Path.GetDirectoryName(ENUM_OUTPUT_PATH);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // 生成枚举
            GenerateVariableTypeEnum();

            // 生成 GraphVariable partial
            GenerateGraphVariablePartial();

            // 生成 ShizukuGraphBase partial
            GenerateShizukuGraphBasePartial();

            AssetDatabase.Refresh();

            _statusLabel.text = $"✅ Generated code for {_customTypes.Count} type(s)";
            EditorUtility.DisplayDialog("Success", 
                $"Successfully generated code for {_customTypes.Count} custom type(s)!", "OK");
        }
        catch (Exception ex)
        {
            _statusLabel.text = $"❌ Error: {ex.Message}";
            EditorUtility.DisplayDialog("Error", $"Failed to generate code:\n{ex.Message}", "OK");
            Debug.LogError($"[VariableTypeGenerator] Error: {ex}");
        }
    }

    private void GenerateVariableTypeEnum()
    {
        var sb = new StringBuilder();

        // 文件头
        sb.AppendLine("// Auto-generated by VariableTypeGenerator");
        sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// DO NOT MODIFY THIS FILE MANUALLY");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// 变量类型枚举（自动生成）");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public enum VariableType");
        sb.AppendLine("{");

        // 内置类型
        sb.AppendLine("    // Built-in types");
        sb.AppendLine("    Int,");
        sb.AppendLine("    Float,");
        sb.AppendLine("    Bool,");
        sb.AppendLine("    String,");
        sb.AppendLine("    Vector2,");
        sb.AppendLine("    Vector3,");
        sb.AppendLine("    GameObject,");
        sb.AppendLine("    Transform,");
        sb.AppendLine("    Color,");
        sb.AppendLine();

        // 自定义类型
        if (_customTypes.Count > 0)
        {
            sb.AppendLine("    // Custom ShizukuClass types (auto-generated)");
            foreach (var typeInfo in _customTypes)
            {
                var enumName = $"Custom_{typeInfo.Type.Name}";
                var comment = string.IsNullOrEmpty(typeInfo.Description) 
                    ? typeInfo.DisplayName 
                    : typeInfo.Description;
                sb.AppendLine($"    {enumName},  // {comment}");
            }
        }

        sb.AppendLine("}");

        File.WriteAllText(ENUM_OUTPUT_PATH, sb.ToString());
        Debug.Log($"[VariableTypeGenerator] Generated: {ENUM_OUTPUT_PATH}");
    }

    private void GenerateGraphVariablePartial()
    {
        var sb = new StringBuilder();

        // 文件头
        sb.AppendLine("// Auto-generated by VariableTypeGenerator");
        sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// DO NOT MODIFY THIS FILE MANUALLY");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// GraphVariable 的自动生成扩展部分");
        sb.AppendLine("/// 包含所有自定义 ShizukuClass 类型的字段");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[Serializable]");
        sb.AppendLine("public partial class GraphVariable");
        sb.AppendLine("{");

        if (_customTypes.Count > 0)
        {
            sb.AppendLine("    // Custom type fields (auto-generated)");
            foreach (var typeInfo in _customTypes)
            {
                var fieldName = $"Custom_{typeInfo.Type.Name}Value";
                var typeName = typeInfo.Type.FullName ?? typeInfo.Type.Name;
                sb.AppendLine();
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// {typeInfo.DisplayName} ({typeInfo.Type.Name})");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    [SerializeField]");
                sb.AppendLine($"    public {typeName} {fieldName};");
            }
            sb.AppendLine();

            // 实现 SetDefaultValueCustomType partial 方法
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 设置自定义类型的默认值");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    partial void SetDefaultValueCustomType(VariableType type)");
            sb.AppendLine("    {");
            sb.AppendLine("        switch (type)");
            sb.AppendLine("        {");
            foreach (var typeInfo in _customTypes)
            {
                var enumName = $"VariableType.Custom_{typeInfo.Type.Name}";
                var fieldName = $"Custom_{typeInfo.Type.Name}Value";
                sb.AppendLine($"            case {enumName}:");
                sb.AppendLine($"                {fieldName} = default;");
                sb.AppendLine($"                break;");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine("    // No custom types defined");
        }

        sb.AppendLine("}");

        File.WriteAllText(VARIABLE_OUTPUT_PATH, sb.ToString());
        Debug.Log($"[VariableTypeGenerator] Generated: {VARIABLE_OUTPUT_PATH}");
    }

    private void GenerateShizukuGraphBasePartial()
    {
        var sb = new StringBuilder();

        // 文件头
        sb.AppendLine("// Auto-generated by VariableTypeGenerator");
        sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// DO NOT MODIFY THIS FILE MANUALLY");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// ShizukuGraphBase 的自动生成扩展部分");
        sb.AppendLine("/// 包含所有自定义类型的运行时存储和访问方法");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public partial class ShizukuGraphBase");
        sb.AppendLine("{");

        if (_customTypes.Count > 0)
        {
            // 运行时字典
            sb.AppendLine("    // Custom type runtime storage (auto-generated)");
            foreach (var typeInfo in _customTypes)
            {
                var typeName = typeInfo.Type.FullName ?? typeInfo.Type.Name;
                var fieldName = $"_runtimeCustom_{typeInfo.Type.Name}s";
                sb.AppendLine($"    [NonSerialized]");
                sb.AppendLine($"    private Dictionary<string, {typeName}> {fieldName};");
            }
            sb.AppendLine();

            // 实现 partial 方法：InitCustomTypeVariables
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 初始化自定义类型字典");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    partial void InitCustomTypeVariables()");
            sb.AppendLine("    {");
            foreach (var typeInfo in _customTypes)
            {
                var typeName = typeInfo.Type.FullName ?? typeInfo.Type.Name;
                var fieldName = $"_runtimeCustom_{typeInfo.Type.Name}s";
                sb.AppendLine($"        {fieldName} = new Dictionary<string, {typeName}>();");
            }
            sb.AppendLine("    }");
            sb.AppendLine();

            // 实现 partial 方法：InitCustomTypeVariable
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 初始化单个自定义类型变量");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    partial void InitCustomTypeVariable(GraphVariable variable)");
            sb.AppendLine("    {");
            sb.AppendLine("        switch (variable.Type)");
            sb.AppendLine("        {");
            foreach (var typeInfo in _customTypes)
            {
                var enumName = $"VariableType.Custom_{typeInfo.Type.Name}";
                var fieldName = $"_runtimeCustom_{typeInfo.Type.Name}s";
                var valueFieldName = $"Custom_{typeInfo.Type.Name}Value";
                sb.AppendLine($"            case {enumName}:");
                sb.AppendLine($"                {fieldName}[variable.GUID] = variable.{valueFieldName};");
                sb.AppendLine($"                break;");
            }
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();

            // Get 方法
            sb.AppendLine("    // Custom type Get methods (auto-generated)");
            foreach (var typeInfo in _customTypes)
            {
                var typeName = typeInfo.Type.FullName ?? typeInfo.Type.Name;
                var fieldName = $"_runtimeCustom_{typeInfo.Type.Name}s";
                var methodName = $"TryGetVariable_{typeInfo.Type.Name}";
                
                sb.AppendLine($"    public bool {methodName}(string guid, out {typeName} value)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        if ({fieldName} != null && {fieldName}.TryGetValue(guid, out value))");
                sb.AppendLine($"            return true;");
                sb.AppendLine($"        value = default;");
                sb.AppendLine($"        return false;");
                sb.AppendLine($"    }}");
                sb.AppendLine();
            }

            // Set 方法
            sb.AppendLine("    // Custom type Set methods (auto-generated)");
            foreach (var typeInfo in _customTypes)
            {
                var typeName = typeInfo.Type.FullName ?? typeInfo.Type.Name;
                var fieldName = $"_runtimeCustom_{typeInfo.Type.Name}s";
                var methodName = $"SetVariable_{typeInfo.Type.Name}";
                
                sb.AppendLine($"    public void {methodName}(string guid, {typeName} value)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        if ({fieldName} != null) {fieldName}[guid] = value;");
                sb.AppendLine($"    }}");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("    // No custom types defined");
        }

        sb.AppendLine("}");

        File.WriteAllText(GRAPH_BASE_OUTPUT_PATH, sb.ToString());
        Debug.Log($"[VariableTypeGenerator] Generated: {GRAPH_BASE_OUTPUT_PATH}");
    }
}

