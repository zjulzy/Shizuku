using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// 节点搜索窗口提供者，用于自动发现和创建节点
/// </summary>
namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public class NodeSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private ShizukuGraphView _graphView;
        private Vector2 _nodePosition;

        /// <summary>
        /// 初始化提供者
        /// </summary>
        public void Initialize(ShizukuGraphView graphView, Vector2 nodePosition)
        {
            _graphView = graphView;
            _nodePosition = nodePosition;
        }

        /// <summary>
        /// 创建搜索树
        /// </summary>
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("创建节点"), 0)
            };

            // 扫描所有节点类型
            var nodeTypes = ScanAllNodeTypes();

            // 按分类组织节点
            var categorizedNodes = new Dictionary<NodeCategory, List<NodeInfo>>();
            foreach (var nodeInfo in nodeTypes)
            {
                if (!categorizedNodes.ContainsKey(nodeInfo.Category))
                {
                    categorizedNodes[nodeInfo.Category] = new List<NodeInfo>();
                }
                categorizedNodes[nodeInfo.Category].Add(nodeInfo);
            }

            // 按分类添加到树中
            var addedGroupPaths = new HashSet<string>();

            foreach (var category in Enum.GetValues(typeof(NodeCategory)).Cast<NodeCategory>().OrderBy(c => (int)c))
            {
                if (!categorizedNodes.ContainsKey(category))
                    continue;

                var nodes = categorizedNodes[category].OrderBy(n => n.Order).ThenBy(n => n.MenuPath).ToList();
                if (nodes.Count == 0)
                    continue;

                // 添加分类标题
                tree.Add(new SearchTreeGroupEntry(new GUIContent(GetCategoryDisplayName(category)), 1));

                // 添加节点项
                foreach (var node in nodes)
                {
                    var parts = node.MenuPath.Split('/');

                    // 添加子分组（如果有）
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        var groupPath = $"{category}/{string.Join("/", parts.Take(i + 1))}";
                        var groupName = parts[i];

                        // 检查是否已添加该分组
                        if (!addedGroupPaths.Contains(groupPath))
                        {
                            tree.Add(new SearchTreeGroupEntry(new GUIContent(groupName), 2 + i));
                            addedGroupPaths.Add(groupPath);
                        }
                    }

                    // 添加节点项
                    var nodeName = parts[parts.Length - 1];
                    var level = parts.Length + 1;
                    var content = new GUIContent(nodeName, node.Description);

                    tree.Add(new SearchTreeEntry(content)
                    {
                        level = level,
                        userData = node
                    });
                }
            }

            return tree;
        }

        /// <summary>
        /// 选择条目时的回调
        /// </summary>
        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is NodeInfo nodeInfo)
            {
                _graphView.CreateNodeFromType(nodeInfo.NodeType, _nodePosition);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 扫描所有节点类型
        /// </summary>
        private List<NodeInfo> ScanAllNodeTypes()
        {
            var nodeInfos = new List<NodeInfo>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // 跳过系统程序集
                if (assembly.FullName.StartsWith("Unity") || 
                    assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("mscorlib"))
                    continue;

                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // 检查是否是节点类型
                        if (!type.IsClass || type.IsAbstract || !typeof(ShizukuNodeBase).IsAssignableFrom(type))
                            continue;

                        // 跳过某些特殊节点
                        if (type == typeof(ShizukuRootNode) || 
                            type == typeof(BlueprintEventNode) ||
                            type == typeof(MethodEntryNode) ||
                            type == typeof(MethodReturnNode) ||
                            type.Name.Contains("TypeConverterNode"))
                            continue;

                        // 获取特性
                        var attr = type.GetCustomAttribute<NodeMenuItemAttribute>();
                        if (attr != null)
                        {
                            nodeInfos.Add(new NodeInfo
                            {
                                NodeType = type,
                                MenuPath = attr.MenuPath,
                                Category = attr.Category,
                                Description = attr.Description ?? "",
                                Order = attr.Order
                            });
                        }
                        else
                        {
                            // 如果没有特性，使用默认值
                            var defaultPath = type.Name.Replace("Node", "");
                            nodeInfos.Add(new NodeInfo
                            {
                                NodeType = type,
                                MenuPath = defaultPath,
                                Category = NodeCategory.Basic,
                                Description = $"创建 {defaultPath} 节点",
                                Order = 999
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {ex.Message}");
                }
            }

            return nodeInfos;
        }

        /// <summary>
        /// 获取分类显示名称
        /// </summary>
        private string GetCategoryDisplayName(NodeCategory category)
        {
            return category switch
            {
                NodeCategory.Basic => "基础节点",
                NodeCategory.Blueprint => "蓝图节点",
                NodeCategory.Math => "数学节点",
                NodeCategory.Logic => "逻辑节点",
                NodeCategory.Converter => "类型转换",
                NodeCategory.Event => "事件节点",
                _ => category.ToString()
            };
        }

        /// <summary>
        /// 节点信息类
        /// </summary>
        private class NodeInfo
        {
            public Type NodeType;
            public string MenuPath;
            public NodeCategory Category;
            public string Description;
            public int Order;
        }
    }


}
