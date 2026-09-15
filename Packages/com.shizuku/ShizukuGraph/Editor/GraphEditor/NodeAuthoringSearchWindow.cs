using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Shizuku.Graph.Editor
{
    internal sealed class NodeAuthoringSearchWindow : EditorWindow
    {
        internal sealed class Item
        {
            public Type Type;
            public ShizukuMethod Method;
            public string Path, Description, Keywords;
        }

        private ShizukuGraphView _view;
        private Vector2 _position, _scroll;
        private Port _source;
        private List<Item> _items;
        private string _query = "";
        private bool _compatibleOnly = true;
        private int _selected;
        private const string RecentKey = "Shizuku.Authoring.RecentNodes";

        internal static bool Matches(Item item, string query) =>
            (query ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).All(word =>
                (item.Path + " " + item.Description + " " + item.Keywords).IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0);

        internal static List<Item> Catalog()
        {
            return TypeCache.GetTypesDerivedFrom<ShizukuNodeBase>()
                .Where(t => !t.IsAbstract && !t.ContainsGenericParameters && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (type: t, menu: t.GetCustomAttribute<NodeMenuItemAttribute>()))
                .Where(x => x.menu != null && NodeMenuItemAttribute.TryValidateMenuPath(x.menu.MenuPath, out _))
                .Select(x => new Item { Type = x.type, Path = x.menu.MenuPath, Description = x.menu.Description, Keywords = x.menu.Keywords })
                .OrderBy(x => x.Path).ToList();
        }

        internal static void Open(ShizukuGraphView view, Vector2 graphPosition, Vector2 screenPosition, ShizukuGraphBase graph, Port source = null)
        {
            var window = CreateInstance<NodeAuthoringSearchWindow>();
            window._view = view; window._position = graphPosition; window._source = source;
            window._items = Catalog();
            if (graph) window._items.AddRange(graph.Methods.Select(m => new Item { Method = m, Path = "调用函数/" + m.Name, Description = "调用图中定义的函数" }));
            window.ShowAsDropDown(new Rect(screenPosition, Vector2.zero), new Vector2(510, 460));
        }

        private void OnGUI()
        {
            GUI.SetNextControlName("search");
            var query = EditorGUILayout.TextField("搜索名称 / 用途 / 别名", _query);
            if (query != _query) { _query = query; _selected = 0; }
            if (_source != null) _compatibleOnly = EditorGUILayout.ToggleLeft("只显示适合当前端口的节点", _compatibleOnly);
            else EditorGUILayout.LabelField("支持中文用途关键词；空搜索优先显示最近使用", EditorStyles.miniLabel);
            var recent = EditorPrefs.GetString(RecentKey, "").Split('|');
            var items = _items.Where(i => Matches(i, _query) && (!_compatibleOnly || _source == null || Compatible(i)))
                .OrderBy(i => string.IsNullOrEmpty(_query) && i.Type != null && Array.IndexOf(recent, i.Type.FullName) >= 0 ? Array.IndexOf(recent, i.Type.FullName) : 100)
                .ThenBy(i => i.Path).ToList();
            _selected = Mathf.Clamp(_selected, 0, Math.Max(0, items.Count - 1));
            var evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                if (evt.keyCode == KeyCode.DownArrow) { _selected = Math.Min(_selected + 1, items.Count - 1); evt.Use(); Repaint(); }
                else if (evt.keyCode == KeyCode.UpArrow) { _selected = Math.Max(0, _selected - 1); evt.Use(); Repaint(); }
                else if (evt.keyCode == KeyCode.Return && items.Count > 0) { Choose(items[_selected]); evt.Use(); return; }
                else if (evt.keyCode == KeyCode.Escape) { Close(); evt.Use(); return; }
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                GUI.backgroundColor = i == _selected ? new Color(.5f, .75f, 1f) : Color.white;
                if (GUILayout.Button(new GUIContent(item.Path, item.Description), GUILayout.Height(25))) { Choose(item); break; }
                GUI.backgroundColor = Color.white;
                if (!string.IsNullOrEmpty(item.Description)) EditorGUILayout.LabelField(item.Description, EditorStyles.wordWrappedMiniLabel);
            }
            GUI.backgroundColor = Color.white;
            if (items.Count == 0) EditorGUILayout.HelpBox("没有匹配节点。可缩短搜索词，或取消端口筛选。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl())) EditorGUI.FocusTextInControl("search");
        }

        private bool Compatible(Item item)
        {
            try
            {
                ShizukuNodeBase node = item.Type != null ? (ShizukuNodeBase)Activator.CreateInstance(item.Type) : new InvokeMethodNode();
                if (node is InvokeMethodNode invoke && item.Method != null) invoke.SyncPortsFromMethod(item.Method);
                if (_source is ControlFlowPort)
                    return _source.direction == Direction.Output ? node.SupportControlInput : node.SupportControlOutput;
                var ports = NodeAuthoringUtility.Fields(node.GetType()).Where(f => typeof(ParameterEdgePort).IsAssignableFrom(f.FieldType))
                    .Select(f => f.GetValue(node) as ParameterEdgePort).Where(p => p != null);
                if (node is InvokeMethodNode methodNode)
                    ports = ports.Concat(methodNode.DynamicInputPorts.Concat(methodNode.DynamicOutputPorts).Select(p => p.Port));
                if (node is IDynamicParameterPortProvider dynamicNode)
                    ports = ports.Concat(dynamicNode.DynamicParameterPorts.Select(p => p.Port));
                return ports.Any(p => p.IsOut == (_source.direction == Direction.Input) &&
                    CompatibleTypes(_source.direction == Direction.Output ? ValueType(_source.portType) : ValueType(p.GetType()),
                        _source.direction == Direction.Output ? ValueType(p.GetType()) : ValueType(_source.portType)));
            }
            catch { return false; }
        }

        internal static Type ValueType(Type type)
        {
            for (; type != null; type = type.BaseType)
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ParameterEdgePort<>)) return type.GetGenericArguments()[0];
            return null;
        }
        internal static bool CompatibleTypes(Type output, Type input) => output != null && input != null &&
            (output == input || ConverterNodeRegistry.CanConvert(output, input));

        private void Choose(Item item)
        {
            if (_view == null) { Close(); return; }
            var before = new HashSet<string>(_view.nodes.OfType<ShizukuNodeView>().Select(n => n.RuntimeNode.GUID));
            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            if (item.Type != null)
            {
                _view.CreateNodeFromType(item.Type, _position);
                var recent = new[] { item.Type.FullName }.Concat(EditorPrefs.GetString(RecentKey, "").Split('|')).Where(s => !string.IsNullOrEmpty(s)).Distinct().Take(8);
                EditorPrefs.SetString(RecentKey, string.Join("|", recent));
            }
            else _view.CreateInvokeMethodNode(item.Method, _position);
            var created = _view.nodes.OfType<ShizukuNodeView>().FirstOrDefault(n => !before.Contains(n.RuntimeNode.GUID));
            if (created != null && _source != null && _source.panel != null)
            {
                var compatible = _view.GetCompatiblePorts(_source, null).Where(p => p.node == created).ToList();
                if (compatible.Count == 1) _view.ConnectAuthoringPorts(_source, compatible[0]);
                else if (compatible.Count > 1)
                {
                    var menu = new GenericMenu();
                    var view = _view; var source = _source;
                    foreach (var target in compatible) { var captured = target; menu.AddItem(new GUIContent("连接到/" + target.portName), false, () => view.ConnectAuthoringPorts(source, captured)); }
                    menu.ShowAsContext();
                }
                else _view.ShowAuthoringFeedback("节点已创建；配置资源后可出现动态端口，请手动连接。");
            }
            Undo.CollapseUndoOperations(group);
            Close();
        }
    }
}
