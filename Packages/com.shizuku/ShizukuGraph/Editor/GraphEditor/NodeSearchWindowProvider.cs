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
    public class NodeSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private ShizukuGraphView _graphView;
        private Vector2 _nodePosition;
        private ShizukuGraphBase _graphAsset;

        /// <summary>
        /// 初始化提供者
        /// </summary>
        public void Initialize(ShizukuGraphView graphView, Vector2 nodePosition)
        {
            _graphView = graphView;
            _nodePosition = nodePosition;
        }

        /// <summary>
        /// 初始化提供者（带图资产引用，用于显示函数调用项）
        /// </summary>
        public void Initialize(ShizukuGraphView graphView, Vector2 nodePosition, ShizukuGraphBase graphAsset)
        {
            _graphView = graphView;
            _nodePosition = nodePosition;
            _graphAsset = graphAsset;
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

            var menuRoot = BuildMenuTree(ScanAllNodeTypes());
            AppendMenuEntries(tree, menuRoot, 1);

            // 添加"调用函数"分组
            AddMethodCallEntries(tree);

            return tree;
        }

        private static MenuGroup BuildMenuTree(IEnumerable<NodeInfo> nodes)
        {
            var root = new MenuGroup(string.Empty);

            foreach (var node in nodes)
            {
                var parts = node.MenuPath
                    .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Trim())
                    .Where(part => part.Length > 0)
                    .ToArray();

                if (parts.Length == 0)
                    continue;

                node.MenuPath = string.Join("/", parts);
                node.DisplayName = parts[parts.Length - 1];

                var group = root;
                group.Order = Math.Min(group.Order, node.Order);
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    group = group.GetOrAddGroup(parts[i]);
                    group.Order = Math.Min(group.Order, node.Order);
                }

                group.Nodes.Add(node);
            }

            return root;
        }

        private static void AppendMenuEntries(List<SearchTreeEntry> tree, MenuGroup group, int level)
        {
            foreach (var childGroup in group.Groups.Values
                         .OrderBy(item => item.Order)
                         .ThenBy(item => item.Name, StringComparer.CurrentCulture))
            {
                tree.Add(new SearchTreeGroupEntry(new GUIContent(childGroup.Name), level));
                AppendMenuEntries(tree, childGroup, level + 1);
            }

            foreach (var node in group.Nodes
                         .OrderBy(item => item.Order)
                         .ThenBy(item => item.DisplayName, StringComparer.CurrentCulture))
            {
                tree.Add(new SearchTreeEntry(new GUIContent(node.DisplayName, node.Description))
                {
                    level = level,
                    userData = node
                });
            }
        }

        /// <summary>
        /// 将图中定义的函数添加为搜索条目
        /// </summary>
        private void AddMethodCallEntries(List<SearchTreeEntry> tree)
        {
            if (_graphAsset == null || _graphAsset.Methods.Count == 0)
                return;

            tree.Add(new SearchTreeGroupEntry(new GUIContent("调用函数"), 1));

            foreach (var method in _graphAsset.Methods)
            {
                var content = new GUIContent($"📞 {method.Name}", $"调用函数 {method.Name}");
                tree.Add(new SearchTreeEntry(content)
                {
                    level = 2,
                    userData = method
                });
            }
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

            if (entry.userData is ShizukuMethod method)
            {
                _graphView.CreateInvokeMethodNode(method, _nodePosition);
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

                        // NodeMenuItem 是进入创建菜单的唯一入口；内部结构节点默认不暴露。
                        var attr = type.GetCustomAttribute<NodeMenuItemAttribute>();
                        if (attr == null || string.IsNullOrWhiteSpace(attr.MenuPath))
                            continue;

                        if (!NodeMenuItemAttribute.TryValidateMenuPath(attr.MenuPath, out var pathError))
                        {
                            Debug.LogWarning($"节点 {type.FullName} 的菜单路径无效：{pathError}");
                            continue;
                        }

                        nodeInfos.Add(new NodeInfo
                        {
                            NodeType = type,
                            MenuPath = attr.MenuPath,
                            Description = attr.Description ?? string.Empty,
                            Order = attr.Order
                        });
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
        /// 节点信息类
        /// </summary>
        private class NodeInfo
        {
            public Type NodeType;
            public string MenuPath;
            public string DisplayName;
            public string Description;
            public int Order;
        }

        private sealed class MenuGroup
        {
            public readonly string Name;
            public readonly Dictionary<string, MenuGroup> Groups = new(StringComparer.Ordinal);
            public readonly List<NodeInfo> Nodes = new();
            public int Order = int.MaxValue;

            public MenuGroup(string name)
            {
                Name = name;
            }

            public MenuGroup GetOrAddGroup(string name)
            {
                if (Groups.TryGetValue(name, out var group))
                    return group;

                group = new MenuGroup(name);
                Groups.Add(name, group);
                return group;
            }
        }
    }


}
