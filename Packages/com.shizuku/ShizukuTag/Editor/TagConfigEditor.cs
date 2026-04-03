#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shizuku.Tag.Editor
{
    [CustomEditor(typeof(TagConfig))]
    public class TagConfigEditor : UnityEditor.Editor
    {
        private sealed class TagTreeNode
        {
            public string Name;
            public string FullPath;
            public uint Value;
            public List<TagTreeNode> Children = new List<TagTreeNode>();
            public bool IsLeaf => Children.Count == 0;
        }

        private static readonly Dictionary<string, bool> Foldouts = new Dictionary<string, bool>(StringComparer.Ordinal);
        private static readonly Dictionary<int, string> NewTagInputs = new Dictionary<int, string>();
        private static readonly Dictionary<int, int> RemoveSelections = new Dictionary<int, int>();

        // 阻挡 / 互斥编辑器状态
        private static int _selectedRuleTagIndex;
        private static int _blockAddIndex;
        private static int _cancelAddIndex;
        private static bool _ruleFoldout = true;

        public override void OnInspectorGUI()
        {
            var collection = (TagConfig)target;
            int instanceId = collection.GetInstanceID();

            DrawTagTreeSection(collection);
            EditorGUILayout.Space(8f);
            DrawAddSection(collection, instanceId);
            EditorGUILayout.Space(8f);
            DrawRemoveSection(collection, instanceId);
            EditorGUILayout.Space(12f);
            DrawRulesSection(collection);
        }

        private static void DrawTagTreeSection(TagConfig collection)
        {
            EditorGUILayout.LabelField("Tag 列表", EditorStyles.boldLabel);

            var roots = BuildTree(collection.Tags);
            if (roots.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有 Tag。", MessageType.Info);
                return;
            }

            foreach (var root in roots)
                DrawNodeRow(root, 0);
        }

        private static void DrawAddSection(TagConfig collection, int instanceId)
        {
            EditorGUILayout.LabelField("添加 Tag", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("输入完整 Tag 名称（例如 Element.Fire.Burn），会自动补齐缺失父级。", MessageType.None);

            NewTagInputs.TryGetValue(instanceId, out string input);
            input = EditorGUILayout.TextField("名称", input ?? string.Empty);
            NewTagInputs[instanceId] = input;

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(input)))
            {
                if (!GUILayout.Button("添加"))
                    return;

                string tagName = input.Trim();
                if (collection.GetTagByName(tagName) != 0)
                {
                    Debug.LogWarning($"[TagConfig] Tag \"{tagName}\" 已存在。");
                    return;
                }

                string[] parts = tagName.Split('.');
                uint parentValue = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    string ancestorPath = string.Join(".", parts, 0, i + 1);
                    uint existing = collection.GetTagByName(ancestorPath);
                    if (existing != 0)
                    {
                        parentValue = existing;
                        continue;
                    }

                    uint result = collection.AddTag(ancestorPath, parentValue);
                    if (result == 0)
                    {
                        Debug.LogError($"[TagConfig] 添加 \"{ancestorPath}\" 失败，同层级可能已满（最多 255 个）。");
                        return;
                    }

                    parentValue = result;
                }

                NewTagInputs[instanceId] = string.Empty;
                MarkDirty(collection);
            }
        }

        private static void DrawRemoveSection(TagConfig collection, int instanceId)
        {
            EditorGUILayout.LabelField("删除 Tag", EditorStyles.boldLabel);

            var names = collection.Tags
                .Select(t => t.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            if (names.Length == 0)
            {
                EditorGUILayout.HelpBox("当前没有可删除的 Tag。", MessageType.Info);
                return;
            }

            if (!RemoveSelections.TryGetValue(instanceId, out int index))
                index = 0;
            index = Mathf.Clamp(index, 0, names.Length - 1);

            index = EditorGUILayout.Popup("目标", index, names);
            RemoveSelections[instanceId] = index;

            if (!GUILayout.Button("删除"))
                return;

            string selected = names[index];
            if (collection.RemoveTag(selected))
            {
                Debug.Log($"[TagConfig] 已删除 Tag \"{selected}\"");
                MarkDirty(collection);
            }
            else
            {
                Debug.LogWarning($"[TagConfig] 未找到 Tag \"{selected}\"");
            }
        }

        private static void DrawNodeRow(TagTreeNode node, int depth)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            float indent = depth * 12f;
            float nameX = rect.x + indent;
            float nameWidth = Mathf.Max(0, rect.width - indent - 200f);

            if (!node.IsLeaf)
            {
                Rect foldoutRect = new Rect(nameX, rect.y, nameWidth, rect.height);
                bool expanded = GetFoldout(node.FullPath, true);
                bool newExpanded = EditorGUI.Foldout(foldoutRect, expanded, node.FullPath, true);
                SetFoldout(node.FullPath, newExpanded);
            }
            else
            {
                Rect nameRect = new Rect(nameX + 12f, rect.y, nameWidth - 12f, rect.height);
                EditorGUI.LabelField(nameRect, node.FullPath);
            }

            Rect valueRect = new Rect(rect.xMax - 190f, rect.y, 85f, rect.height);
            Rect hexRect = new Rect(rect.xMax - 100f, rect.y, 100f, rect.height);

            if (node.Value != 0)
            {
                EditorGUI.LabelField(valueRect, node.Value.ToString());
                EditorGUI.LabelField(hexRect, $"0x{node.Value:X8}");
            }

            if (!node.IsLeaf || GetFoldout(node.FullPath, true))
            {
                if (!node.IsLeaf && !GetFoldout(node.FullPath, true))
                    return;

                foreach (var child in node.Children)
                    DrawNodeRow(child, depth + 1);
            }
        }

        private static bool GetFoldout(string key, bool defaultValue)
        {
            if (!Foldouts.TryGetValue(key, out bool state))
            {
                Foldouts[key] = defaultValue;
                return defaultValue;
            }

            return state;
        }

        private static void SetFoldout(string key, bool value)
        {
            Foldouts[key] = value;
        }

        private static List<TagTreeNode> BuildTree(IReadOnlyList<TagDefinition> tags)
        {
            var roots = new List<TagTreeNode>();
            var nodes = new Dictionary<string, TagTreeNode>(StringComparer.Ordinal);

            foreach (var def in tags.OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                string[] parts = def.Name.Split('.');
                string path = string.Empty;
                TagTreeNode parent = null;

                for (int i = 0; i < parts.Length; i++)
                {
                    path = i == 0 ? parts[i] : $"{path}.{parts[i]}";
                    if (!nodes.TryGetValue(path, out TagTreeNode current))
                    {
                        current = new TagTreeNode
                        {
                            Name = parts[i],
                            FullPath = path,
                            Value = 0,
                        };
                        nodes[path] = current;

                        if (parent == null)
                            roots.Add(current);
                        else
                            parent.Children.Add(current);
                    }

                    if (i == parts.Length - 1)
                        current.Value = def.Value;

                    parent = current;
                }
            }

            return roots;
        }

        private static void MarkDirty(UnityEngine.Object obj)
        {
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
        }

        // ────────── 阻挡 / 互斥 规则 UI ──────────

        private static void DrawRulesSection(TagConfig collection)
        {
            _ruleFoldout = EditorGUILayout.Foldout(_ruleFoldout, "阻挡 / 互斥 规则", true, EditorStyles.foldoutHeader);
            if (!_ruleFoldout) return;

            string[] tagNames = collection.Tags
                .Select(t => t.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();

            if (tagNames.Length == 0)
            {
                EditorGUILayout.HelpBox("没有可用的 Tag。", MessageType.Info);
                return;
            }

            // 选择要编辑规则的 Tag
            _selectedRuleTagIndex = Mathf.Clamp(_selectedRuleTagIndex, 0, tagNames.Length - 1);
            _selectedRuleTagIndex = EditorGUILayout.Popup("选择 Tag", _selectedRuleTagIndex, tagNames);
            string selectedTag = tagNames[_selectedRuleTagIndex];

            // 查找当前 tag 对应的 block / cancel 规则
            List<string> blockTargets = GetTargets(collection.BlockRules, selectedTag);
            List<string> cancelTargets = GetTargets(collection.CancelRules, selectedTag);

            EditorGUILayout.Space(4f);

            // ── 阻挡集 ──
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("阻挡集（拥有以下 Tag 时，该 Tag 无法添加）", EditorStyles.boldLabel);
            DrawTagSet(collection, selectedTag, blockTargets, ref _blockAddIndex, tagNames,
                (targets) => { collection.SetBlockRule(selectedTag, targets); MarkDirty(collection); },
                () => { collection.RemoveBlockRule(selectedTag); MarkDirty(collection); });
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4f);

            // ── 互斥集 ──
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("互斥集（该 Tag 添加后，以下 Tag 被移除）", EditorStyles.boldLabel);
            DrawTagSet(collection, selectedTag, cancelTargets, ref _cancelAddIndex, tagNames,
                (targets) => { collection.SetCancelRule(selectedTag, targets); MarkDirty(collection); },
                () => { collection.RemoveCancelRule(selectedTag); MarkDirty(collection); });
            EditorGUILayout.EndVertical();
        }

        private static List<string> GetTargets(IReadOnlyList<TagRelationEntry> rules, string sourceTag)
        {
            foreach (var rule in rules)
            {
                if (rule.SourceTag == sourceTag)
                    return rule.TargetTags;
            }
            return null;
        }

        private static void DrawTagSet(
            TagConfig collection, string sourceTag,
            List<string> targets, ref int addIndex, string[] tagNames,
            Action<List<string>> setTargets, Action removeRule)
        {
            if (targets == null || targets.Count == 0)
            {
                EditorGUILayout.LabelField("  （空）", EditorStyles.miniLabel);
            }
            else
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(12f);
                    EditorGUILayout.LabelField("• " + targets[i]);
                    if (GUILayout.Button("-", GUILayout.Width(22f)))
                    {
                        var newTargets = new List<string>(targets);
                        newTargets.RemoveAt(i);
                        if (newTargets.Count > 0)
                            setTargets(newTargets);
                        else
                            removeRule();
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            // 添加新 target
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12f);
            addIndex = Mathf.Clamp(addIndex, 0, tagNames.Length - 1);
            addIndex = EditorGUILayout.Popup(addIndex, tagNames);
            if (GUILayout.Button("+", GUILayout.Width(22f)))
            {
                string target = tagNames[addIndex];
                if (target != sourceTag && (targets == null || !targets.Contains(target)))
                {
                    var newTargets = targets != null ? new List<string>(targets) { target } : new List<string> { target };
                    setTargets(newTargets);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

