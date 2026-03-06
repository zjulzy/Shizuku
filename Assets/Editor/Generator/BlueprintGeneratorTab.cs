using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Generator
{
    /// <summary>
    /// 蓝图生成器 Tab
    /// 用于管理和生成 BlueprintBehavior 对应的 ShizukuBluePrint 子类
    /// </summary>
    public class BlueprintGeneratorTab
{
    /// <summary>
    /// 默认生成路径：所有新生成的 Blueprint 类都会放在这个目录下
    /// </summary>
    private const string DEFAULT_GENERATION_PATH = "Assets/Scripts/Graph/Blueprint/Generated";
    
    private List<BlueprintClassInfo> _blueprintClasses = new List<BlueprintClassInfo>();
    private ScrollView _scrollView;
    private VisualElement _contentContainer;
    private Label _statusLabel;

    public void BuildUI(VisualElement parent)
    {
        parent.Clear();

        // 标题栏和按钮
        var headerContainer = new VisualElement
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

        var titleLabel = new Label("Blueprint Class Manager")
        {
            style =
            {
                fontSize = 14,
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

        var refreshButton = new Button(ScanBlueprintClasses)
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
        headerContainer.Add(titleLabel);
        headerContainer.Add(buttonContainer);
        parent.Add(headerContainer);

        // 状态栏
        var statusContainer = new VisualElement
        {
            style =
            {
                backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                paddingLeft = 10,
                paddingTop = 5,
                paddingBottom = 5
            }
        };

        _statusLabel = new Label("Ready")
        {
            style =
            {
                color = new Color(0.7f, 0.7f, 0.7f),
                marginBottom = 2
            }
        };
        statusContainer.Add(_statusLabel);

        // 添加默认路径提示
        var pathInfoLabel = new Label($"📂 Default generation path: {DEFAULT_GENERATION_PATH}")
        {
            style =
            {
                fontSize = 10,
                color = new Color(0.5f, 0.5f, 0.5f)
            }
        };
        statusContainer.Add(pathInfoLabel);

        parent.Add(statusContainer);

        // 滚动区域
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
        ScanBlueprintClasses();
    }

    /// <summary>
    /// 扫描所有 BlueprintBehavior 子类
    /// </summary>
    private void ScanBlueprintClasses()
    {
        _blueprintClasses.Clear();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            // 跳过 Unity 和系统程序集
            if (assembly.FullName.StartsWith("Unity") || 
                assembly.FullName.StartsWith("System") ||
                assembly.FullName.StartsWith("Mono") ||
                assembly.FullName.StartsWith("mscorlib"))
            {
                continue;
            }

            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (IsBlueprintBehaviorType(type))
                    {
                        var info = new BlueprintClassInfo
                        {
                            BehaviorType = type,
                            GeneratedBlueprintType = FindGeneratedBlueprintType(type),
                            GeneratedScriptPath = FindGeneratedScriptPath(type)
                        };
                        _blueprintClasses.Add(info);
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 某些程序集可能无法加载，跳过
                continue;
            }
        }

        // 按名称排序
        _blueprintClasses = _blueprintClasses.OrderBy(c => c.BehaviorType.Name).ToList();

        UpdateStatusLabel();
        RefreshUI();
    }

    /// <summary>
    /// 判断类型是否是 BlueprintBehavior 子类
    /// </summary>
    private bool IsBlueprintBehaviorType(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
            return false;

        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && 
                baseType.GetGenericTypeDefinition().Name.StartsWith("BlueprintBehavior"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// 查找已生成的蓝图类型
    /// 通过检查类型是否继承自 ShizukuBluePrint&lt;behaviorType&gt;
    /// </summary>
    private Type FindGeneratedBlueprintType(Type behaviorType)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // 跳过 Unity 和系统程序集以提高性能
            if (assembly.FullName.StartsWith("Unity") || 
                assembly.FullName.StartsWith("System") ||
                assembly.FullName.StartsWith("Mono") ||
                assembly.FullName.StartsWith("mscorlib"))
            {
                continue;
            }

            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // 跳过抽象类、接口和泛型定义
                    if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                        continue;

                    // 检查是否继承自 ShizukuBluePrint<behaviorType>
                    if (IsValidBlueprintType(type, behaviorType))
                    {
                        return type;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 某些程序集可能无法加载，跳过
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// 验证蓝图类型是否匹配
    /// </summary>
    private bool IsValidBlueprintType(Type blueprintType, Type behaviorType)
    {
        var baseType = blueprintType.BaseType;
        if (baseType == null || !baseType.IsGenericType)
            return false;

        var genericDef = baseType.GetGenericTypeDefinition();
        if (!genericDef.Name.StartsWith("ShizukuBluePrint"))
            return false;

        var genericArgs = baseType.GetGenericArguments();
        return genericArgs.Length > 0 && genericArgs[0] == behaviorType;
    }

    /// <summary>
    /// 查找已生成的脚本路径
    /// </summary>
    private string FindGeneratedScriptPath(Type behaviorType)
    {
        var expectedFileName = behaviorType.Name + "Blueprint.cs";
        
        var guids = AssetDatabase.FindAssets($"{behaviorType.Name}Blueprint t:Script");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileName(path) == expectedFileName)
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// 刷新 UI
    /// </summary>
    private void RefreshUI()
    {
        _contentContainer.Clear();

        if (_blueprintClasses.Count == 0)
        {
            var emptyLabel = new Label("No BlueprintBehavior classes found")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    paddingTop = 50,
                    fontSize = 14,
                    color = Color.gray
                }
            };
            _contentContainer.Add(emptyLabel);
            return;
        }

        foreach (var classInfo in _blueprintClasses)
        {
            var classItem = CreateClassItem(classInfo);
            _contentContainer.Add(classItem);
        }
    }

    /// <summary>
    /// 创建单个类信息的 UI
    /// </summary>
    private VisualElement CreateClassItem(BlueprintClassInfo classInfo)
    {
        var container = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                paddingTop = 8,
                paddingBottom = 8,
                paddingLeft = 10,
                paddingRight = 10,
                marginBottom = 5,
                backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f),
                borderBottomColor = new Color(0.1f, 0.1f, 0.1f, 1f),
                borderBottomWidth = 1
            }
        };

        // 左侧信息
        var infoContainer = new VisualElement
        {
            style =
            {
                flexGrow = 1,
                flexDirection = FlexDirection.Column
            }
        };

        // 类名
        var classNameContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginBottom = 3
            }
        };

        var className = new Label(classInfo.BehaviorType.Name)
        {
            style =
            {
                fontSize = 13,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = Color.white
            }
        };
        classNameContainer.Add(className);

        // 添加命名空间标签
        if (!string.IsNullOrEmpty(classInfo.BehaviorType.Namespace))
        {
            var namespaceLabel = new Label($"({classInfo.BehaviorType.Namespace})")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.5f, 0.5f, 0.5f),
                    marginLeft = 5
                }
            };
            classNameContainer.Add(namespaceLabel);
        }

        infoContainer.Add(classNameContainer);

        // 状态信息
        var isGenerated = classInfo.GeneratedBlueprintType != null;
        var statusColor = isGenerated ? new Color(0.3f, 0.8f, 0.3f) : new Color(0.8f, 0.5f, 0.2f);
        var statusText = isGenerated ? "✓ Generated" : "○ Not Generated";

        var statusLabel = new Label(statusText)
        {
            style =
            {
                fontSize = 11,
                color = statusColor,
                marginBottom = 2
            }
        };
        infoContainer.Add(statusLabel);

        // 路径信息
        if (!string.IsNullOrEmpty(classInfo.GeneratedScriptPath))
        {
            var pathLabel = new Label($"📁 {classInfo.GeneratedScriptPath}")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.6f, 0.6f, 0.6f),
                    marginBottom = 2
                }
            };
            infoContainer.Add(pathLabel);
        }

        // 显示可重写方法数量
        var overridableCount = CountOverridableMethods(classInfo.BehaviorType);
        if (overridableCount > 0)
        {
            var methodsLabel = new Label($"⚡ {overridableCount} overridable method(s)")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.7f, 0.7f, 0.9f)
                }
            };
            infoContainer.Add(methodsLabel);
        }

        container.Add(infoContainer);

        // 右侧按钮
        var buttonContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Column,
                justifyContent = Justify.Center,
                minWidth = 120
            }
        };

        if (isGenerated)
        {
            // 已生成：显示重新生成和定位按钮
            var regenerateButton = new Button(() => GenerateBlueprint(classInfo, true))
            {
                text = "Regenerate",
                style = { marginBottom = 3 }
            };
            buttonContainer.Add(regenerateButton);

            if (!string.IsNullOrEmpty(classInfo.GeneratedScriptPath))
            {
                var locateButton = new Button(() => PingScriptAsset(classInfo.GeneratedScriptPath))
                {
                    text = "Locate"
                };
                buttonContainer.Add(locateButton);
            }
        }
        else
        {
            // 未生成：显示生成按钮
            var generateButton = new Button(() => GenerateBlueprint(classInfo, false))
            {
                text = "Generate",
                style =
                {
                    backgroundColor = new Color(0.2f, 0.6f, 0.2f, 1f)
                }
            };
            buttonContainer.Add(generateButton);
        }

        container.Add(buttonContainer);

        return container;
    }

    /// <summary>
    /// 批量生成所有缺失的蓝图类
    /// </summary>
    private void GenerateAllMissing()
    {
        var missingClasses = _blueprintClasses
            .Where(c => c.GeneratedBlueprintType == null)
            .ToList();

        if (missingClasses.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "All blueprint classes are already generated.", "OK");
            return;
        }

        var message = $"Generate {missingClasses.Count} blueprint class(es)?\n\n" +
                      string.Join("\n", missingClasses.Select(c => "- " + c.BehaviorType.Name));

        if (EditorUtility.DisplayDialog("Batch Generate", message, "Generate", "Cancel"))
        {
            var successCount = 0;
            var failCount = 0;

            foreach (var classInfo in missingClasses)
            {
                try
                {
                    GenerateBlueprint(classInfo, false);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to generate blueprint for {classInfo.BehaviorType.Name}: {ex.Message}");
                    failCount++;
                }
            }

            AssetDatabase.Refresh();
            EditorApplication.delayCall += () =>
            {
                ScanBlueprintClasses();
                EditorUtility.DisplayDialog(
                    "Batch Generation Complete",
                    $"Success: {successCount}\nFailed: {failCount}",
                    "OK");
            };
        }
    }

    /// <summary>
    /// 生成蓝图类
    /// </summary>
    private void GenerateBlueprint(BlueprintClassInfo classInfo, bool isRegenerate)
    {
        var behaviorType = classInfo.BehaviorType;
        var blueprintClassName = behaviorType.Name + "Blueprint";

        // 确定生成路径
        string savePath;
        if (isRegenerate && !string.IsNullOrEmpty(classInfo.GeneratedScriptPath))
        {
            // 重新生成时使用原路径
            savePath = classInfo.GeneratedScriptPath;
        }
        else
        {
            // 新生成：统一使用默认路径
            savePath = Path.Combine(DEFAULT_GENERATION_PATH, blueprintClassName + ".cs");
        }

        // 生成代码
        var code = GenerateBlueprintCode(behaviorType, blueprintClassName);

        // 确保目录存在
        var directoryPath = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 写入文件
        File.WriteAllText(savePath, code);
        AssetDatabase.Refresh();

        UpdateStatusLabel($"Generated: {blueprintClassName} at {savePath}");

        // 重新扫描
        EditorApplication.delayCall += () =>
        {
            ScanBlueprintClasses();
            EditorUtility.DisplayDialog(
                "Success", 
                $"Blueprint class '{blueprintClassName}' has been generated at:\n{savePath}", 
                "OK");
        };
    }

    /// <summary>
    /// 生成蓝图类代码
    /// </summary>
    private string GenerateBlueprintCode(Type behaviorType, string blueprintClassName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using UnityEngine;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Blueprint class for {behaviorType.Name}");
        sb.AppendLine("/// Auto-generated by Blueprint Generator");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"[CreateAssetMenu(fileName = \"{blueprintClassName}\", menuName = \"Shizuku/Blueprint/{behaviorType.Name} Blueprint\")]");
        sb.AppendLine($"public class {blueprintClassName} : ShizukuBluePrint<{behaviorType.Name}>");
        sb.AppendLine("{");
        sb.AppendLine("    // You can override InitializeBehavior to add custom initialization logic");
        sb.AppendLine("    // public override void InitializeBehavior(" + behaviorType.Name + " behavior)");
        sb.AppendLine("    // {");
        sb.AppendLine("    //     base.InitializeBehavior(behavior);");
        sb.AppendLine("    //     // Custom initialization here");
        sb.AppendLine("    // }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// 在 Project 窗口中定位脚本
    /// </summary>
    private void PingScriptAsset(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
    }

    /// <summary>
    /// 统计类型中的可重写方法数量
    /// </summary>
    private int CountOverridableMethods(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return methods.Count(m => m.GetCustomAttribute<BlueprintOverridableAttribute>() != null);
    }

    /// <summary>
    /// 更新状态栏
    /// </summary>
    private void UpdateStatusLabel(string message = null)
    {
        if (message != null)
        {
            _statusLabel.text = message;
        }
        else
        {
            var totalCount = _blueprintClasses.Count;
            var generatedCount = _blueprintClasses.Count(c => c.GeneratedBlueprintType != null);
            _statusLabel.text = $"Total: {totalCount} | Generated: {generatedCount} | Not Generated: {totalCount - generatedCount}";
        }
    }

    /// <summary>
    /// 蓝图类信息
    /// </summary>
    private class BlueprintClassInfo
    {
        public Type BehaviorType;
        public Type GeneratedBlueprintType;
        public string GeneratedScriptPath;
    }
}
}

