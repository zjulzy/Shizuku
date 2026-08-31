using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    /// <summary>
    /// 统一的 Shizuku 代码生成器管理窗口
    /// 集成 ShizukuClass 和 ShizukuFunction 的管理和生成
    /// </summary>
    public class UnifiedShizukuGeneratorTab
    {
        private const string FUNCTION_NODE_PATH = "Assets/Scripts/Node/DerivedNodes/Generated";
        private const string VARIABLE_NODE_OUTPUT_PATH = "Assets/Scripts/Node/VariableNodes/Generated";
        private const string PORT_TYPE_OUTPUT_PATH = "Assets/Scripts/Node/Generated";

        private List<ShizukuClassEntry> _classEntries = new List<ShizukuClassEntry>();
        private ScrollView _scrollView;
        private VisualElement _contentContainer;
        private Label _statusLabel;

        public void BuildUI(VisualElement parent)
        {
            parent.Clear();

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

            var titleLabel = new Label("ShizukuClass & Function Generator")
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

            var refreshButton = new Button(ScanAll)
            {
                text = "🔄 Refresh",
                style = { marginRight = 5 }
            };

            var generateAllButton = new Button(GenerateAllPending)
            {
                text = "Generate All Pending",
                style =
                {
                    marginRight = 5,
                    backgroundColor = new Color(0.2f, 0.6f, 0.2f, 0.8f)
                }
            };

            var generateVariableTypesButton = new Button(GenerateVariableTypes)
            {
                text = "Generate Variable Types",
                style =
                {
                    backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.8f)
                }
            };

            buttonContainer.Add(refreshButton);
            buttonContainer.Add(generateAllButton);
            buttonContainer.Add(generateVariableTypesButton);
            titleBar.Add(titleLabel);
            titleBar.Add(buttonContainer);
            parent.Add(titleBar);


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
            parent.Add(statusContainer);

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
            parent.Add(_scrollView);

            // 初始扫描
            ScanAll();
        }

        /// <summary>
        /// 扫描所有 ShizukuClass 和 ShizukuFunction
        /// </summary>
        private void ScanAll()
        {
            _classEntries.Clear();
            _contentContainer.Clear();

            // 确保注册中心已初始化
            ShizukuTypeRegistry.Initialize();

            var allClassInfos = ShizukuTypeRegistry.GetAllShizukuClassInfos().ToList();

            foreach (var classInfo in allClassInfos)
            {
                var entry = new ShizukuClassEntry
                {
                    ClassInfo = classInfo,
                    Functions = new List<FunctionEntry>()
                };

                // 获取该类的所有 ShizukuFunction
                var functions = ShizukuTypeRegistry.GetFunctionsForType(classInfo.Type);
                foreach (var funcInfo in functions)
                {
                    var unsupportedMessage = GetUnsupportedTypesMessage(funcInfo);
                    var funcEntry = new FunctionEntry
                    {
                        FunctionInfo = funcInfo,
                        NodeClassName = funcInfo.GetNodeClassName(),
                        IsGenerated = CheckIfFunctionNodeGenerated(funcInfo),
                        HasUnsupportedTypes = !string.IsNullOrEmpty(unsupportedMessage),
                        UnsupportedTypesMessage = unsupportedMessage
                    };
                    entry.Functions.Add(funcEntry);
                }

                // 检查是否支持生成变量类型（非静态类且 ShowInVariableMenu 为 true）
                entry.SupportsVariableType = classInfo.ShowInVariableMenu && !classInfo.Type.IsAbstract && !classInfo.Type.IsStatic();

                _classEntries.Add(entry);
            }

            // 按 Category 和 DisplayName 排序
            _classEntries = _classEntries
                .OrderBy(e => e.ClassInfo.Category)
                .ThenBy(e => e.ClassInfo.DisplayName)
                .ToList();

            UpdateUI();

            var totalClasses = _classEntries.Count;
            var totalFunctions = _classEntries.Sum(e => e.Functions.Count);
            var generatedFunctions = _classEntries.Sum(e => e.Functions.Count(f => f.IsGenerated));
            _statusLabel.text = $"Found {totalClasses} class(es), {totalFunctions} function(s) ({generatedFunctions} generated, {totalFunctions - generatedFunctions} pending)";
        }

        /// <summary>
        /// 检查函数节点是否已生成
        /// </summary>
        private bool CheckIfFunctionNodeGenerated(ShizukuFunctionInfo funcInfo)
        {
            var nodeClassName = funcInfo.GetNodeClassName();
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

            if (_classEntries.Count == 0)
            {
                var noClassLabel = new Label("No ShizukuClass found.\nAdd [ShizukuClass] attribute to your classes.")
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
                _contentContainer.Add(noClassLabel);
                return;
            }

            // 按类别分组
            var grouped = _classEntries.GroupBy(e => e.ClassInfo.Category);

            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                // 类别标题
                var categoryHeader = new VisualElement
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

                var categoryLabel = new Label($"📁 {group.Key}")
                {
                    style =
                    {
                        fontSize = 14,
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                };

                categoryHeader.Add(categoryLabel);
                _contentContainer.Add(categoryHeader);

                // 类列表
                foreach (var entry in group.OrderBy(e => e.ClassInfo.DisplayName))
                {
                    var classContainer = CreateClassItem(entry);
                    _contentContainer.Add(classContainer);
                }
            }
        }

        /// <summary>
        /// 创建 ShizukuClass 项的 UI
        /// </summary>
        private VisualElement CreateClassItem(ShizukuClassEntry entry)
        {
            var container = new VisualElement
            {
                style =
                {
                    marginBottom = 5,
                    marginLeft = 10,
                    backgroundColor = new Color(0.22f, 0.22f, 0.22f, 0.8f),
                    paddingTop = 5,
                    paddingBottom = 5,
                    paddingLeft = 5,
                    paddingRight = 5
                }
            };

            // 类头部
            var classHeader = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    paddingTop = 5,
                    paddingBottom = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    backgroundColor = new Color(0.28f, 0.28f, 0.32f, 1f)
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

            var classNameLabel = new Label($"📦 {entry.ClassInfo.DisplayName}")
            {
                style =
                {
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.8f, 0.9f, 1f, 1f)
                }
            };

            var classTypeLabel = new Label($"Type: {entry.ClassInfo.Type.Name} | Functions: {entry.Functions.Count}")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.7f, 0.7f, 0.7f, 1f)
                }
            };

            if (!string.IsNullOrEmpty(entry.ClassInfo.Description))
            {
                var descLabel = new Label(entry.ClassInfo.Description)
                {
                    style =
                    {
                        fontSize = 10,
                        color = new Color(0.6f, 0.8f, 0.6f, 1f)
                    }
                };
                infoContainer.Add(classNameLabel);
                infoContainer.Add(classTypeLabel);
                infoContainer.Add(descLabel);
            }
            else
            {
                infoContainer.Add(classNameLabel);
                infoContainer.Add(classTypeLabel);
            }

            // 右侧按钮
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            // 如果支持生成变量类型，显示生成按钮
            if (entry.SupportsVariableType)
            {
                var varTypeButton = new Button(() => GenerateSingleVariableType(entry))
                {
                    text = "Generate Variable Type",
                    style =
                    {
                        width = 160,
                        backgroundColor = new Color(0.2f, 0.5f, 0.7f, 0.8f)
                    }
                };
                buttonContainer.Add(varTypeButton);
            }

            classHeader.Add(infoContainer);
            classHeader.Add(buttonContainer);
            container.Add(classHeader);

            // 函数列表
            if (entry.Functions.Count > 0)
            {
                var functionsContainer = new VisualElement
                {
                    style =
                    {
                        paddingLeft = 15,
                        paddingTop = 5
                    }
                };

                foreach (var funcEntry in entry.Functions.OrderBy(f => f.FunctionInfo.DisplayName))
                {
                    var funcItem = CreateFunctionItem(funcEntry);
                    functionsContainer.Add(funcItem);
                }

                container.Add(functionsContainer);
            }

            return container;
        }

        /// <summary>
        /// 创建函数项的 UI
        /// </summary>
        private VisualElement CreateFunctionItem(FunctionEntry funcEntry)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    paddingTop = 5,
                    paddingBottom = 5,
                    paddingLeft = 10,
                    paddingRight = 10,
                    backgroundColor = funcEntry.HasUnsupportedTypes
                        ? new Color(0.4f, 0.2f, 0.2f, 0.3f)
                        : funcEntry.IsGenerated
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

            var nameLabel = new Label($"⚡ {funcEntry.FunctionInfo.DisplayName}")
            {
                style =
                {
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            var signatureLabel = new Label(GetFunctionSignature(funcEntry.FunctionInfo))
            {
                style =
                {
                    fontSize = 9,
                    color = new Color(0.7f, 0.7f, 0.7f, 1f)
                }
            };

            infoContainer.Add(nameLabel);
            infoContainer.Add(signatureLabel);

            // 如果有不支持的类型，显示错误信息
            if (funcEntry.HasUnsupportedTypes)
            {
                var errorLabel = new Label($"⚠ Unsupported types: {funcEntry.UnsupportedTypesMessage}")
                {
                    style =
                    {
                        fontSize = 9,
                        color = new Color(1f, 0.5f, 0.3f, 1f),
                        whiteSpace = WhiteSpace.Normal
                    }
                };
                infoContainer.Add(errorLabel);
            }

            // 右侧按钮
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            // 状态标签
            Label statusLabel;
            if (funcEntry.HasUnsupportedTypes)
            {
                statusLabel = new Label("⚠ Cannot Generate")
                {
                    style =
                    {
                        marginRight = 10,
                        fontSize = 10,
                        color = new Color(1f, 0.5f, 0.3f, 1f)
                    }
                };
            }
            else
            {
                statusLabel = new Label(funcEntry.IsGenerated ? "✓ Generated" : "⚠ Pending")
                {
                    style =
                    {
                        marginRight = 10,
                        fontSize = 10,
                        color = funcEntry.IsGenerated ? Color.green : Color.yellow
                    }
                };
            }

            var generateButton = new Button(() => GenerateSingleFunction(funcEntry))
            {
                text = funcEntry.IsGenerated ? "Regenerate" : "Generate",
                style =
                {
                    width = 90,
                    backgroundColor = funcEntry.IsGenerated
                        ? new Color(0.3f, 0.3f, 0.6f, 0.8f)
                        : new Color(0.2f, 0.6f, 0.2f, 0.8f)
                }
            };

            // 如果有不支持的类型，禁用生成按钮
            if (funcEntry.HasUnsupportedTypes)
            {
                generateButton.SetEnabled(false);
                generateButton.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }

            if (funcEntry.IsGenerated)
            {
                var deleteButton = new Button(() => DeleteGeneratedFunction(funcEntry))
                {
                    text = "Delete",
                    style =
                    {
                        width = 70,
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
        /// 生成所有待生成的节点
        /// </summary>
        private void GenerateAllPending()
        {
            var pendingFunctions = _classEntries
                .SelectMany(e => e.Functions)
                .Where(f => !f.IsGenerated && !f.HasUnsupportedTypes)
                .ToList();

            var unsupportedCount = _classEntries
                .SelectMany(e => e.Functions)
                .Count(f => !f.IsGenerated && f.HasUnsupportedTypes);

            if (pendingFunctions.Count == 0)
            {
                if (unsupportedCount > 0)
                {
                    EditorUtility.DisplayDialog("Info", 
                        $"All pending function nodes have unsupported types ({unsupportedCount} function(s)).\nPlease check the error messages.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Info", "All function nodes are already generated!", "OK");
                }
                return;
            }

            var message = $"Generate {pendingFunctions.Count} function node(s)?";
            if (unsupportedCount > 0)
            {
                message += $"\n({unsupportedCount} function(s) with unsupported types will be skipped)";
            }

            if (!EditorUtility.DisplayDialog("Confirm", message, "Generate", "Cancel"))
            {
                return;
            }

            int successCount = 0;
            foreach (var funcEntry in pendingFunctions)
            {
                if (GenerateFunctionNodeClass(funcEntry))
                {
                    successCount++;
                }
            }

            AssetDatabase.Refresh();
            ScanAll();

            var resultMessage = $"Successfully generated {successCount}/{pendingFunctions.Count} node class(es)!";
            if (unsupportedCount > 0)
            {
                resultMessage += $"\n({unsupportedCount} function(s) with unsupported types were skipped)";
            }

            EditorUtility.DisplayDialog("Complete", resultMessage, "OK");
        }

        /// <summary>
        /// 生成单个函数节点
        /// </summary>
        private void GenerateSingleFunction(FunctionEntry funcEntry)
        {
            if (GenerateFunctionNodeClass(funcEntry))
            {
                AssetDatabase.Refresh();

                // 延迟扫描以等待编译完成
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.delayCall += ScanAll;
                };

                EditorUtility.DisplayDialog("Success",
                    $"Successfully generated node class: {funcEntry.NodeClassName}", "OK");
            }
        }

        /// <summary>
        /// 删除已生成的函数节点
        /// </summary>
        private void DeleteGeneratedFunction(FunctionEntry funcEntry)
        {
            if (!EditorUtility.DisplayDialog("Confirm Delete",
                $"Delete generated node class: {funcEntry.NodeClassName}?", "Delete", "Cancel"))
            {
                return;
            }

            var filePath = FindGeneratedFilePath(funcEntry.NodeClassName);
            if (string.IsNullOrEmpty(filePath))
            {
                EditorUtility.DisplayDialog("Error", "Cannot find generated file!", "OK");
                return;
            }

            AssetDatabase.DeleteAsset(filePath);
            AssetDatabase.Refresh();

            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += ScanAll;
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
        /// 生成函数节点类代码
        /// </summary>
        private bool GenerateFunctionNodeClass(FunctionEntry funcEntry)
        {
            try
            {
                var code = GenerateFunctionCode(funcEntry.FunctionInfo);
                var fileName = $"{funcEntry.NodeClassName}.cs";
                var path = FUNCTION_NODE_PATH;

                // 确保目录存在
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var filePath = Path.Combine(path, fileName);
                File.WriteAllText(filePath, code);

                Debug.Log($"[UnifiedShizukuGenerator] Generated: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedShizukuGenerator] Failed to generate {funcEntry.NodeClassName}: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate node class:\n{ex.Message}", "OK");
                return false;
            }
        }

        /// <summary>
        /// 生成函数节点代码
        /// </summary>
        private string GenerateFunctionCode(ShizukuFunctionInfo funcInfo)
        {
            var sb = new StringBuilder();

            // 文件头注释
            sb.AppendLine("// Auto-generated by UnifiedShizukuGenerator");
            sb.AppendLine($"// Source: {funcInfo.DeclaringType.FullName}.{funcInfo.Method.Name}");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Using 语句
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Shizuku.Core;");
            sb.AppendLine("using Shizuku.Graph;");
            if (!string.IsNullOrEmpty(funcInfo.DeclaringType.Namespace))
            {
                sb.AppendLine($"using {funcInfo.DeclaringType.Namespace};");
            }
            sb.AppendLine();

            // NodeMenuItem 特性
            var menuPath = funcInfo.GetMenuPath();
            EnsureValidGeneratedMenuPath(menuPath);
            sb.AppendLine($"[NodeMenuItem(\"{menuPath}\", Description = \"{funcInfo.Description}\")]");

            // 类定义
            var baseClass = "ShizukuRunnableNode";
            sb.AppendLine($"public class {funcInfo.GetNodeClassName()} : {baseClass}");
            sb.AppendLine("{");

            // TitleBarColor - 函数节点使用紫色
            sb.AppendLine("    public override Color TitleBarColor => new Color(0.6f, 0.4f, 0.8f, 1f);");
            sb.AppendLine();

            // 如果是非静态方法，添加 self 端口
            if (!funcInfo.IsStatic)
            {
                var selfPortType = GetPortTypeName(funcInfo.DeclaringType);
                sb.AppendLine($"    [SerializeReference]");
                sb.AppendLine($"    private {selfPortType} _self = new() {{ IsOut = false, Name = \"self\" }};");
                sb.AppendLine();
            }

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
                    // 实例方法调用 - 从 self 端口获取实例
                    sb.AppendLine($"        var instance = _self.Value as {funcInfo.DeclaringType.Name};");
                    sb.AppendLine($"        if (instance == null)");
                    sb.AppendLine($"        {{");
                    sb.AppendLine($"            Debug.LogError(\"[{funcInfo.GetNodeClassName()}] Self instance is null!\");");
                    sb.AppendLine($"            return;");
                    sb.AppendLine($"        }}");
                    sb.AppendLine();

                    var paramNames = funcInfo.Parameters.Select(p => $"_{p.Name}.Value").ToArray();
                    var call = $"instance.{funcInfo.Method.Name}({string.Join(", ", paramNames)})";

                    if (funcInfo.ReturnType != typeof(void))
                    {
                        sb.AppendLine($"        _result.Value = {call};");
                    }
                    else
                    {
                        sb.AppendLine($"        {call};");
                    }
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
                sb.AppendLine("    protected override void OnComputeOutputValues()");
                sb.AppendLine("    {");

                if (funcInfo.IsStatic)
                {
                    var paramNames = funcInfo.Parameters.Select(p => $"_{p.Name}.Value").ToArray();
                    var call = $"{funcInfo.DeclaringType.Name}.{funcInfo.Method.Name}({string.Join(", ", paramNames)})";
                    sb.AppendLine($"        _result.Value = {call};");
                }
                else
                {
                    // 实例方法调用 - 从 self 端口获取实例
                    sb.AppendLine($"        var instance = _self.Value;");
                    sb.AppendLine($"        if (instance != null)");
                    sb.AppendLine($"        {{");
                    var paramNames = funcInfo.Parameters.Select(p => $"_{p.Name}.Value").ToArray();
                    var call = $"instance.{funcInfo.Method.Name}({string.Join(", ", paramNames)})";
                    sb.AppendLine($"            _result.Value = {call};");
                    sb.AppendLine($"        }}");
                    sb.AppendLine($"        else");
                    sb.AppendLine($"        {{");
                    sb.AppendLine($"            Debug.LogWarning(\"[{funcInfo.GetNodeClassName()}] Self instance is null, using default value.\");");
                    sb.AppendLine($"        }}");
                }

                sb.AppendLine("    }");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// 检查类型是否支持节点生成
        /// </summary>
        private bool IsTypeSupportedForNode(Type type)
        {
            if (type == typeof(void)) return true;
            if (type == typeof(int)) return true;
            if (type == typeof(float)) return true;
            if (type == typeof(bool)) return true;
            if (type == typeof(string)) return true;
            if (type == typeof(Vector2)) return true;
            if (type == typeof(Vector3)) return true;
            if (type == typeof(GameObject)) return true;
            if (type == typeof(Transform)) return true;
            if (type == typeof(Color)) return true;

            // 检查是否是 ShizukuClass 类型
            if (ShizukuTypeRegistry.IsShizukuClass(type))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取不支持的类型名称
        /// </summary>
        private string GetUnsupportedTypesMessage(ShizukuFunctionInfo funcInfo)
        {
            var unsupportedTypes = new List<string>();

            // 检查返回值类型
            if (!IsTypeSupportedForNode(funcInfo.ReturnType))
            {
                unsupportedTypes.Add($"Return: {GetTypeName(funcInfo.ReturnType)}");
            }

            // 检查参数类型
            foreach (var param in funcInfo.Parameters)
            {
                if (!IsTypeSupportedForNode(param.ParameterType))
                {
                    unsupportedTypes.Add($"Param '{param.Name}': {GetTypeName(param.ParameterType)}");
                }
            }

            if (unsupportedTypes.Count > 0)
            {
                return string.Join(", ", unsupportedTypes);
            }

            return null;
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

            // 检查是否是 ShizukuClass 类型，使用生成的端口类型
            if (ShizukuTypeRegistry.IsShizukuClass(type))
            {
                return $"{type.Name}ParameterEdgePort";
            }

            // 默认使用 ObjectParameterEdgePort
            return "ObjectParameterEdgePort";
        }

        /// <summary>
        /// 生成单个类的变量类型
        /// </summary>
        private void GenerateSingleVariableType(ShizukuClassEntry entry)
        {
            if (!entry.SupportsVariableType)
            {
                EditorUtility.DisplayDialog("Error", 
                    "This class does not support variable type generation!", "OK");
                return;
            }

            // 直接调用生成所有变量类型（包含此类）
            GenerateVariableTypes();
        }

        /// <summary>
        /// 生成所有变量类型代码
        /// </summary>
        private void GenerateVariableTypes()
        {
            var customTypes = _classEntries
                .Where(e => e.SupportsVariableType)
                .Select(e => e.ClassInfo)
                .ToList();

            if (customTypes.Count == 0)
            {
                EditorUtility.DisplayDialog("Info", "No custom types available for variable generation!", "OK");
                return;
            }

            try
            {
                // 生成自定义类型的 ParameterEdgePort
                GenerateCustomParameterEdgePorts(customTypes);

                // 生成自定义类型的 Set/Get 变量节点
                GenerateCustomVariableNodes(customTypes);

                AssetDatabase.Refresh();

                _statusLabel.text = $"✅ Generated ports and Set/Get nodes for {customTypes.Count} class(es)";
                EditorUtility.DisplayDialog("Success",
                    $"Successfully generated for {customTypes.Count} class(es):\n" +
                    $"• ParameterEdgePort types\n" +
                    $"• Set/Get variable nodes", "OK");
            }
            catch (Exception ex)
            {
                _statusLabel.text = $"❌ Error: {ex.Message}";
                EditorUtility.DisplayDialog("Error", $"Failed to generate variable types:\n{ex.Message}", "OK");
                Debug.LogError($"[UnifiedShizukuGenerator] Error: {ex}");
            }
        }


        /// <summary>
        /// 为所有自定义类型生成 ParameterEdgePort 子类
        /// </summary>
        private void GenerateCustomParameterEdgePorts(List<ShizukuClassInfo> customTypes)
        {
            // 确保输出目录存在
            if (!Directory.Exists(PORT_TYPE_OUTPUT_PATH))
            {
                Directory.CreateDirectory(PORT_TYPE_OUTPUT_PATH);
            }

            var sb = new StringBuilder();

            // 文件头
            sb.AppendLine("// Auto-generated by UnifiedShizukuGenerator");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// DO NOT MODIFY THIS FILE MANUALLY");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Shizuku.Core;");
            sb.AppendLine("using Shizuku.Graph;");
            sb.AppendLine();
            sb.AppendLine("// ============================================================");
            sb.AppendLine("// Custom ShizukuClass ParameterEdgePort types (auto-generated)");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();

            foreach (var typeInfo in customTypes)
            {
                var typeName = typeInfo.Type.FullName ?? typeInfo.Type.Name;
                var portClassName = $"{typeInfo.Type.Name}ParameterEdgePort";

                sb.AppendLine($"[Serializable]");
                sb.AppendLine($"public class {portClassName} : ParameterEdgePort<{typeName}>");
                sb.AppendLine($"{{");
                sb.AppendLine($"}}");
                sb.AppendLine();
            }

            var filePath = Path.Combine(PORT_TYPE_OUTPUT_PATH, "CustomParameterEdgePorts.Generated.cs");
            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"[UnifiedShizukuGenerator] Generated: {filePath}");
        }

        /// <summary>
        /// 为所有自定义类型生成 Set/Get 变量节点
        /// </summary>
        private void GenerateCustomVariableNodes(List<ShizukuClassInfo> customTypes)
        {
            // 确保输出目录存在
            if (!Directory.Exists(VARIABLE_NODE_OUTPUT_PATH))
            {
                Directory.CreateDirectory(VARIABLE_NODE_OUTPUT_PATH);
            }

            // 生成 Get 节点文件
            GenerateCustomGetVariableNodes(customTypes);

            // 生成 Set 节点文件
            GenerateCustomSetVariableNodes(customTypes);
        }

        /// <summary>
        /// 生成自定义类型的 Get 变量节点
        /// </summary>
        private void GenerateCustomGetVariableNodes(List<ShizukuClassInfo> customTypes)
        {
            var sb = new StringBuilder();

            // 文件头
            sb.AppendLine("// Auto-generated by UnifiedShizukuGenerator");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// DO NOT MODIFY THIS FILE MANUALLY");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Shizuku.Core;");
            sb.AppendLine("using Shizuku.Graph;");
            sb.AppendLine();
            sb.AppendLine("// ============================================================");
            sb.AppendLine("// Get Variable 节点 - 自定义 ShizukuClass 类型（自动生成）");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();

            foreach (var typeInfo in customTypes)
            {
                var simpleTypeName = typeInfo.Type.Name;
                var fullTypeName = typeInfo.Type.FullName ?? simpleTypeName;
                var portClassName = $"{simpleTypeName}ParameterEdgePort";
                var menuPath = $"变量/Get {typeInfo.DisplayName}";
                EnsureValidGeneratedMenuPath(menuPath);

                sb.AppendLine($"[Serializable]");
                sb.AppendLine($"[NodeMenuItem(\"{menuPath}\", Description = \"获取{typeInfo.DisplayName}变量\")]");
                sb.AppendLine($"public class GetVariableNode_Custom_{simpleTypeName} : ShizukuValueNode, IVariableNode");
                sb.AppendLine($"{{");
                sb.AppendLine($"    [SerializeField] public string VariableGUID;");
                sb.AppendLine($"    [SerializeReference] public {portClassName} Output = new {portClassName} {{ IsOut = true, Name = \"Value\" }};");
                sb.AppendLine();
                sb.AppendLine($"    public override string Title => GetDisplayName();");
                sb.AppendLine($"    public override Color TitleBarColor => new Color(0.8f, 0.4f, 0.8f, 1f);");
                sb.AppendLine($"    public VariableType TargetVariableType => VariableType.Custom;");
                sb.AppendLine();
                sb.AppendLine($"    protected override void OnComputeOutputValues()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        var dict = RootGraph.VariableStore.GetOrCreateCustomDict<{fullTypeName}>();");
                sb.AppendLine($"        Output.Value = dict.TryGetValue(VariableGUID, out var v) ? v : default;");
                sb.AppendLine($"    }}");
                sb.AppendLine();
                sb.AppendLine($"    private string GetDisplayName()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        var variable = RootGraph?.GetVariableByGUID(VariableGUID);");
                sb.AppendLine($"        return variable != null ? $\"Get {{variable.Name}}\" : \"Get <未设置>\";");
                sb.AppendLine($"    }}");
                sb.AppendLine($"}}");
                sb.AppendLine();
            }

            var filePath = Path.Combine(VARIABLE_NODE_OUTPUT_PATH, "GetVariableNodes.Custom.Generated.cs");
            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"[UnifiedShizukuGenerator] Generated: {filePath}");
        }

        /// <summary>
        /// 生成自定义类型的 Set 变量节点
        /// </summary>
        private void GenerateCustomSetVariableNodes(List<ShizukuClassInfo> customTypes)
        {
            var sb = new StringBuilder();

            // 文件头
            sb.AppendLine("// Auto-generated by UnifiedShizukuGenerator");
            sb.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("// DO NOT MODIFY THIS FILE MANUALLY");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using Shizuku.Core;");
            sb.AppendLine("using Shizuku.Graph;");
            sb.AppendLine();
            sb.AppendLine("// ============================================================");
            sb.AppendLine("// Set Variable 节点 - 自定义 ShizukuClass 类型（自动生成）");
            sb.AppendLine("// ============================================================");
            sb.AppendLine();

            foreach (var typeInfo in customTypes)
            {
                var simpleTypeName = typeInfo.Type.Name;
                var fullTypeName = typeInfo.Type.FullName ?? simpleTypeName;
                var portClassName = $"{simpleTypeName}ParameterEdgePort";
                var menuPath = $"变量/Set {typeInfo.DisplayName}";
                EnsureValidGeneratedMenuPath(menuPath);

                sb.AppendLine($"[Serializable]");
                sb.AppendLine($"[NodeMenuItem(\"{menuPath}\", Description = \"设置{typeInfo.DisplayName}变量\")]");
                sb.AppendLine($"public class SetVariableNode_Custom_{simpleTypeName} : ShizukuRunnableNode, IVariableNode");
                sb.AppendLine($"{{");
                sb.AppendLine($"    [SerializeField] public string VariableGUID;");
                sb.AppendLine($"    [SerializeReference] public {portClassName} Input = new {portClassName} {{ IsOut = false, Name = \"Value\" }};");
                sb.AppendLine($"    [SerializeField] private ChainPort _nextPort = new ChainPort {{ Name = \"Next\" }};");
                sb.AppendLine();
                sb.AppendLine($"    public override string Title => GetDisplayName();");
                sb.AppendLine($"    public override Color TitleBarColor => new Color(0.9f, 0.5f, 0.8f, 1f);");
                sb.AppendLine($"    public VariableType TargetVariableType => VariableType.Custom;");
                sb.AppendLine();
                sb.AppendLine($"    protected override void OnExecute()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        RootGraph.SetCustomVariable(VariableGUID, Input.Value);");
                sb.AppendLine($"    }}");
                sb.AppendLine();
                sb.AppendLine($"    protected override bool OnSelectNextNode(out string nextNodeGUID)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        nextNodeGUID = _nextPort.NextNodeGuid;");
                sb.AppendLine($"        return !string.IsNullOrEmpty(nextNodeGUID);");
                sb.AppendLine($"    }}");
                sb.AppendLine();
                sb.AppendLine($"    private string GetDisplayName()");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        var variable = RootGraph?.GetVariableByGUID(VariableGUID);");
                sb.AppendLine($"        return variable != null ? $\"Set {{variable.Name}}\" : \"Set <未设置>\";");
                sb.AppendLine($"    }}");
                sb.AppendLine($"}}");
                sb.AppendLine();
            }

            var filePath = Path.Combine(VARIABLE_NODE_OUTPUT_PATH, "SetVariableNodes.Custom.Generated.cs");
            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"[UnifiedShizukuGenerator] Generated: {filePath}");
        }

        private static void EnsureValidGeneratedMenuPath(string menuPath)
        {
            if (!NodeMenuItemAttribute.TryValidateMenuPath(menuPath, out var error))
                throw new InvalidOperationException($"无法生成节点菜单路径“{menuPath}”：{error}");
        }

        /// <summary>
        /// ShizukuClass 条目
        /// </summary>
        private class ShizukuClassEntry
        {
            public ShizukuClassInfo ClassInfo;
            public List<FunctionEntry> Functions;
            public bool SupportsVariableType;
        }

        /// <summary>
        /// 函数条目
        /// </summary>
        private class FunctionEntry
        {
            public ShizukuFunctionInfo FunctionInfo;
            public string NodeClassName;
            public bool IsGenerated;
            public bool HasUnsupportedTypes;
            public string UnsupportedTypesMessage;
        }
    }

    /// <summary>
    /// Type 扩展方法
    /// </summary>
    public static class TypeExtensions
    {
        public static bool IsStatic(this Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }
    }
}

