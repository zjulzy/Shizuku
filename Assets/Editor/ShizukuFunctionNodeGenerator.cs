using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ShizukuFunction 节点生成器管理窗口
/// 用于管理和生成带有 [ShizukuFunction] 标记的方法对应的节点类
/// </summary>
public class ShizukuFunctionNodeGenerator : EditorWindow
{
    /// <summary>
    /// 默认生成路径：所有新生成的函数节点类都会放在这个目录下
    /// </summary>
    private const string DEFAULT_GENERATION_PATH = "Assets/Scripts/Node/DerivedNodes/Generated";
    
    private List<FunctionNodeInfo> _functionNodes = new List<FunctionNodeInfo>();
    private ScrollView _scrollView;
    private VisualElement _contentContainer;
    private Label _statusLabel;
    private TextField _generationPathField;

    [MenuItem("Shizuku/Function Node Generator")]
    public static void OpenWindow()
    {
        var window = GetWindow<ShizukuFunctionNodeGenerator>();
        window.titleContent = new GUIContent("Function Node Generator");
        window.minSize = new Vector2(700, 500);
    }

    private void OnEnable()
    {
        BuildUI();
        ScanFunctions();
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

        var titleLabel = new Label("ShizukuFunction Node Manager")
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

        var refreshButton = new Button(ScanFunctions)
        {
            text = "🔄 Refresh",
            style = { marginRight = 5 }
        };

        var generateAllButton = new Button(GenerateAllMissing)
        {
            text = "Generate All Missing",
            style =
            {
                marginRight = 5,
                backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.8f)
            }
        };

        buttonContainer.Add(refreshButton);
        buttonContainer.Add(generateAllButton);
        titleBar.Add(titleLabel);
        titleBar.Add(buttonContainer);
        rootVisualElement.Add(titleBar);

        // 生成路径配置
        var pathContainer = new VisualElement
        {
            style =
            {
                paddingTop = 5,
                paddingBottom = 5,
                paddingLeft = 10,
                paddingRight = 10,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f),
                flexDirection = FlexDirection.Row
            }
        };

        var pathLabel = new Label("Generation Path:")
        {
            style =
            {
                width = 120,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };

        _generationPathField = new TextField
        {
            value = DEFAULT_GENERATION_PATH,
            style =
            {
                flexGrow = 1,
                marginRight = 5
            }
        };

        var browseButton = new Button(() =>
        {
            var path = EditorUtility.OpenFolderPanel("Select Generation Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                _generationPathField.value = path;
            }
        })
        {
            text = "Browse",
            style = { width = 80 }
        };

        pathContainer.Add(pathLabel);
        pathContainer.Add(_generationPathField);
        pathContainer.Add(browseButton);
        rootVisualElement.Add(pathContainer);

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

        _contentContainer = new VisualElement
        {
            style =
            {
                paddingTop = 10,
                paddingBottom = 10,
                paddingLeft = 10,
                paddingRight = 10
            }
        };

        _scrollView.Add(_contentContainer);
        rootVisualElement.Add(_scrollView);
    }

    /// <summary>
    /// 扫描所有 ShizukuFunction
    /// </summary>
    private void ScanFunctions()
    {
        _functionNodes.Clear();
        _contentContainer.Clear();

        // 确保注册中心已初始化
        ShizukuTypeRegistry.Initialize();

        var allFunctions = ShizukuTypeRegistry.GetAllFunctions();
        
        foreach (var funcInfo in allFunctions)
        {
            var nodeInfo = new FunctionNodeInfo
            {
                FunctionInfo = funcInfo,
                NodeClassName = funcInfo.GetNodeClassName(),
                IsGenerated = CheckIfGenerated(funcInfo)
            };
            
            _functionNodes.Add(nodeInfo);
        }

        UpdateUI();
        
        var total = _functionNodes.Count;
        var generated = _functionNodes.Count(n => n.IsGenerated);
        _statusLabel.text = $"Found {total} functions, {generated} already generated, {total - generated} pending";
    }

    /// <summary>
    /// 检查函数节点是否已生成
    /// </summary>
    private bool CheckIfGenerated(ShizukuFunctionInfo funcInfo)
    {
        var nodeClassName = funcInfo.GetNodeClassName();
        
        // 搜索所有程序集中是否存在该类
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            try
            {
                var type = assembly.GetType(nodeClassName);
                if (type != null && typeof(ShizukuNodeBase).IsAssignableFrom(type))
                {
                    return true;
                }
            }
            catch
            {
                // 忽略错误
            }
        }
        
        return false;
    }

    /// <summary>
    /// 更新 UI
    /// </summary>
    private void UpdateUI()
    {
        _contentContainer.Clear();

        // 按类型分组显示
        var grouped = _functionNodes.GroupBy(n => n.FunctionInfo.DeclaringType);

        foreach (var group in grouped.OrderBy(g => g.Key.Name))
        {
            // 类型标题
            var typeHeader = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    paddingTop = 10,
                    paddingBottom = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                    marginBottom = 5
                }
            };

            var typeLabel = new Label($"📦 {group.Key.Name}")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            typeHeader.Add(typeLabel);
            _contentContainer.Add(typeHeader);

            // 函数列表
            foreach (var nodeInfo in group.OrderBy(n => n.FunctionInfo.DisplayName))
            {
                var itemContainer = CreateFunctionItem(nodeInfo);
                _contentContainer.Add(itemContainer);
            }
        }
    }

    /// <summary>
    /// 创建函数项 UI
    /// </summary>
    private VisualElement CreateFunctionItem(FunctionNodeInfo nodeInfo)
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
                backgroundColor = nodeInfo.IsGenerated 
                    ? new Color(0.2f, 0.3f, 0.2f, 0.3f) 
                    : new Color(0.3f, 0.2f, 0.2f, 0.3f),
                marginBottom = 2
            }
        };

        // 左侧信息
        var infoContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1
            }
        };

        var nameLabel = new Label($"{nodeInfo.FunctionInfo.DisplayName}")
        {
            style =
            {
                fontSize = 12,
                unityFontStyleAndWeight = FontStyle.Bold
            }
        };

        var detailLabel = new Label(GetFunctionSignature(nodeInfo.FunctionInfo))
        {
            style =
            {
                fontSize = 10,
                color = new Color(0.7f, 0.7f, 0.7f, 1f)
            }
        };

        var pathLabel = new Label($"Menu: {nodeInfo.FunctionInfo.GetMenuPath()}")
        {
            style =
            {
                fontSize = 10,
                color = new Color(0.6f, 0.6f, 0.8f, 1f)
            }
        };

        infoContainer.Add(nameLabel);
        infoContainer.Add(detailLabel);
        infoContainer.Add(pathLabel);

        // 右侧按钮
        var buttonContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center
            }
        };

        var statusLabel = new Label(nodeInfo.IsGenerated ? "✅ Generated" : "⚠️ Pending")
        {
            style =
            {
                marginRight = 10,
                color = nodeInfo.IsGenerated ? Color.green : Color.yellow
            }
        };

        var generateButton = new Button(() => GenerateSingle(nodeInfo))
        {
            text = nodeInfo.IsGenerated ? "Regenerate" : "Generate",
            style =
            {
                width = 100,
                backgroundColor = nodeInfo.IsGenerated 
                    ? new Color(0.3f, 0.3f, 0.6f, 0.8f)
                    : new Color(0.2f, 0.6f, 0.2f, 0.8f)
            }
        };

        if (nodeInfo.IsGenerated)
        {
            var deleteButton = new Button(() => DeleteGenerated(nodeInfo))
            {
                text = "Delete",
                style =
                {
                    width = 80,
                    marginLeft = 5,
                    backgroundColor = new Color(0.6f, 0.2f, 0.2f, 0.8f)
                }
            };
            buttonContainer.Add(deleteButton);
        }

        buttonContainer.Add(statusLabel);
        buttonContainer.Add(generateButton);

        container.Add(infoContainer);
        container.Add(buttonContainer);

        return container;
    }

    /// <summary>
    /// 获取函数签名字符串
    /// </summary>
    private string GetFunctionSignature(ShizukuFunctionInfo funcInfo)
    {
        var sb = new StringBuilder();
        
        if (funcInfo.IsStatic)
            sb.Append("static ");
        
        sb.Append(GetTypeName(funcInfo.ReturnType));
        sb.Append(" ");
        sb.Append(funcInfo.Method.Name);
        sb.Append("(");
        
        var paramStrs = funcInfo.Parameters.Select(p => $"{GetTypeName(p.ParameterType)} {p.Name}");
        sb.Append(string.Join(", ", paramStrs));
        
        sb.Append(")");
        
        return sb.ToString();
    }

    /// <summary>
    /// 获取类型的友好名称
    /// </summary>
    private string GetTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        return type.Name;
    }

    /// <summary>
    /// 生成所有未生成的节点
    /// </summary>
    private void GenerateAllMissing()
    {
        var pending = _functionNodes.Where(n => !n.IsGenerated).ToList();
        
        if (pending.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "All function nodes are already generated!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Confirm", 
            $"Generate {pending.Count} function node(s)?", "Generate", "Cancel"))
        {
            return;
        }

        int successCount = 0;
        foreach (var nodeInfo in pending)
        {
            if (GenerateNodeClass(nodeInfo))
            {
                successCount++;
            }
        }

        AssetDatabase.Refresh();
        ScanFunctions();
        
        EditorUtility.DisplayDialog("Complete", 
            $"Successfully generated {successCount}/{pending.Count} node class(es)!", "OK");
    }

    /// <summary>
    /// 生成单个节点
    /// </summary>
    private void GenerateSingle(FunctionNodeInfo nodeInfo)
    {
        if (GenerateNodeClass(nodeInfo))
        {
            AssetDatabase.Refresh();
            
            // 延迟扫描以等待编译完成
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += ScanFunctions;
            };
            
            EditorUtility.DisplayDialog("Success", 
                $"Successfully generated node class: {nodeInfo.NodeClassName}", "OK");
        }
    }

    /// <summary>
    /// 删除已生成的节点
    /// </summary>
    private void DeleteGenerated(FunctionNodeInfo nodeInfo)
    {
        if (!EditorUtility.DisplayDialog("Confirm Delete", 
            $"Delete generated node class: {nodeInfo.NodeClassName}?", "Delete", "Cancel"))
        {
            return;
        }

        var filePath = FindGeneratedFilePath(nodeInfo.NodeClassName);
        if (string.IsNullOrEmpty(filePath))
        {
            EditorUtility.DisplayDialog("Error", "Cannot find generated file!", "OK");
            return;
        }

        AssetDatabase.DeleteAsset(filePath);
        AssetDatabase.Refresh();
        
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += ScanFunctions;
        };
    }

    /// <summary>
    /// 查找已生成文件的路径
    /// </summary>
    private string FindGeneratedFilePath(string className)
    {
        var guids = AssetDatabase.FindAssets($"{className} t:Script");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains(className))
            {
                return path;
            }
        }
        return null;
    }

    /// <summary>
    /// 生成节点类代码
    /// </summary>
    private bool GenerateNodeClass(FunctionNodeInfo nodeInfo)
    {
        try
        {
            var code = GenerateCode(nodeInfo.FunctionInfo);
            var fileName = $"{nodeInfo.NodeClassName}.cs";
            var path = _generationPathField.value;
            
            // 确保目录存在
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            var filePath = Path.Combine(path, fileName);
            File.WriteAllText(filePath, code);
            
            Debug.Log($"[ShizukuFunctionNodeGenerator] Generated: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ShizukuFunctionNodeGenerator] Failed to generate {nodeInfo.NodeClassName}: {ex.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to generate node class:\n{ex.Message}", "OK");
            return false;
        }
    }

    /// <summary>
    /// 生成代码
    /// </summary>
    private string GenerateCode(ShizukuFunctionInfo funcInfo)
    {
        var sb = new StringBuilder();
        
        // 文件头注释
        sb.AppendLine("// Auto-generated by ShizukuFunctionNodeGenerator");
        sb.AppendLine($"// Source: {funcInfo.DeclaringType.FullName}.{funcInfo.Method.Name}");
        sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        
        // Using 语句
        sb.AppendLine("using System;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        
        // NodeMenuItem 特性
        var menuPath = funcInfo.GetMenuPath();
        sb.AppendLine($"[NodeMenuItem(\"{menuPath}\", NodeCategory.Function, Description = \"{funcInfo.Description}\")]");
        
        // 类定义
        var baseClass = "ShizukuRunnableNode";
        sb.AppendLine($"public class {funcInfo.GetNodeClassName()} : {baseClass}");
        sb.AppendLine("{");
        
        // Title
        sb.AppendLine($"    public override string Title => \"{funcInfo.DisplayName}\";");
        sb.AppendLine();
        
        // TitleBarColor - 函数节点使用紫色
        sb.AppendLine("    public override Color TitleBarColor => new Color(0.6f, 0.4f, 0.8f, 1f);");
        sb.AppendLine();
        
        // 输入端口（参数）
        if (funcInfo.Parameters.Count > 0)
        {
            foreach (var param in funcInfo.Parameters)
            {
                var portType = GetPortTypeName(param.ParameterType);
                sb.AppendLine($"    [SerializeReference]");
                sb.AppendLine($"    private {portType} _{param.Name} = new() {{ IsOut = false, Name = \"{param.Name}\" }};");
                sb.AppendLine();
            }
        }
        
        // 输出端口（返回值）
        if (funcInfo.ReturnType != typeof(void))
        {
            var returnPortType = GetPortTypeName(funcInfo.ReturnType);
            sb.AppendLine($"    [SerializeReference]");
            sb.AppendLine($"    private {returnPortType} _result = new() {{ IsOut = true, Name = \"result\" }};");
            sb.AppendLine();
        }
        
        // 如果是可执行节点，添加 ChainPort
        if (baseClass == "ShizukuRunnableNode")
        {
            sb.AppendLine("    [SerializeField]");
            sb.AppendLine("    private ChainPort _nextPort = new() { Name = \"next\" };");
            sb.AppendLine();
        }
        
        // 执行方法
        if (baseClass == "ShizukuRunnableNode")
        {
            sb.AppendLine("    protected override void OnExecute()");
            sb.AppendLine("    {");
            
            // 调用目标方法
            if (funcInfo.IsStatic)
            {
                // 静态方法调用
                var paramNames = funcInfo.Parameters.Select(p => $"_{p.Name}.Value").ToArray();
                var call = $"{funcInfo.DeclaringType.Name}.{funcInfo.Method.Name}({string.Join(", ", paramNames)})";
                
                if (funcInfo.ReturnType != typeof(void))
                {
                    sb.AppendLine($"        _result.Value = {call};");
                }
                else
                {
                    sb.AppendLine($"        {call};");
                }
            }
            else
            {
                // 实例方法需要获取实例（TODO: 支持从端口传入）
                sb.AppendLine("        // TODO: Get instance from input port or BlueprintBehavior");
                sb.AppendLine($"        // var instance = ...;");
                sb.AppendLine($"        // instance.{funcInfo.Method.Name}(...);");
            }
            
            sb.AppendLine("    }");
            sb.AppendLine();
            
            sb.AppendLine("    protected override bool OnSelectNextNode(out string nextNodeGUID)");
            sb.AppendLine("    {");
            sb.AppendLine("        nextNodeGUID = _nextPort.NextNodeGuid;");
            sb.AppendLine("        return !string.IsNullOrEmpty(nextNodeGUID);");
            sb.AppendLine("    }");
        }
        else
        {
            // 值节点
            sb.AppendLine("    public override void GetOutputValues()");
            sb.AppendLine("    {");
            
            if (funcInfo.IsStatic)
            {
                var paramNames = funcInfo.Parameters.Select(p => $"_{p.Name}.Value").ToArray();
                var call = $"{funcInfo.DeclaringType.Name}.{funcInfo.Method.Name}({string.Join(", ", paramNames)})";
                sb.AppendLine($"        _result.Value = {call};");
            }
            else
            {
                sb.AppendLine("        // TODO: Get instance from input port or BlueprintBehavior");
                sb.AppendLine($"        // var instance = ...;");
                sb.AppendLine($"        // _result.Value = instance.{funcInfo.Method.Name}(...);");
            }
            
            sb.AppendLine("    }");
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    /// <summary>
    /// 获取端口类型名称
    /// </summary>
    private string GetPortTypeName(Type type)
    {
        if (type == typeof(int)) return "IntParameterEdgePort";
        if (type == typeof(float)) return "FloatParameterEdgePort";
        if (type == typeof(bool)) return "BoolParameterEdgePort";
        if (type == typeof(string)) return "StringParameterEdgePort";
        if (type == typeof(Vector2)) return "Vector2ParameterEdgePort";
        if (type == typeof(Vector3)) return "Vector3ParameterEdgePort";
        if (type == typeof(GameObject)) return "GameObjectParameterEdgePort";
        if (type == typeof(Transform)) return "TransformParameterEdgePort";
        if (type == typeof(Color)) return "ColorParameterEdgePort";
        
        // 默认使用 ObjectParameterEdgePort
        return "ObjectParameterEdgePort";
    }

    /// <summary>
    /// 函数节点信息
    /// </summary>
    private class FunctionNodeInfo
    {
        public ShizukuFunctionInfo FunctionInfo;
        public string NodeClassName;
        public bool IsGenerated;
    }
}

