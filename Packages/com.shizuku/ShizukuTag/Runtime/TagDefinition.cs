using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Shizuku.Tag
{
    /// <summary>
    /// 单个 Tag 定义，包含名称和序列化存储的 uint 值。
    /// 值在添加时由 TagCollection 自动分配，保证唯一。
    /// </summary>
    [Serializable, InlineProperty, HideReferenceObjectPicker]
    public struct TagDefinition
    {
        [HorizontalGroup("Row"), LabelWidth(40)]
        [Tooltip("Tag 名称，用 . 分隔层级，例如 Element.Fire.Burn")]
        [ReadOnly]
        public string Name;

        [HorizontalGroup("Row", Width = 100), LabelWidth(40)]
        [Tooltip("自动分配的 uint 值")]
        [DisplayAsString]
        public uint Value;

        /// <summary> 显示用的 hex 字符串 </summary>
        [HorizontalGroup("Row", Width = 90), HideLabel, ShowInInspector, ReadOnly]
        public string Hex => $"0x{Value:X8}";
    }

    /// <summary>
    /// Tag 集合配置，ScriptableObject 资产。
    /// 在编辑器中集中管理项目中所有可用的 Tag 定义。
    /// </summary>
    [CreateAssetMenu(fileName = "TagCollection", menuName = "Shizuku/Tag/Tag Collection")]
    public class TagCollection : ScriptableObject
    {
        [SerializeField, ListDrawerSettings(ShowFoldout = true, DraggableItems = false,
             HideAddButton = true, HideRemoveButton = true)]
        [Searchable]
        private List<TagDefinition> _tags = new List<TagDefinition>();

        /// <summary> 所有 Tag 定义（只读） </summary>
        public IReadOnlyList<TagDefinition> Tags => _tags;

        // ────────── 编辑器操作区 ──────────

        [TitleGroup("添加 Tag")]
        [InfoBox("输入完整 Tag 名称（用 . 分隔层级），例如 Element 或 Element.Fire.Burn。\n父级 Tag 必须已存在。")]
        [ShowInInspector, HideLabel, LabelWidth(80)]
        [PropertyOrder(100)]
        private string _newTagName = "";

        [TitleGroup("添加 Tag")]
        [Button("添加", ButtonSizes.Medium), PropertyOrder(101)]
        [EnableIf("@!string.IsNullOrWhiteSpace(_newTagName)")]
        private void AddNewTag()
        {
            string tagName = _newTagName.Trim();
            if (string.IsNullOrEmpty(tagName)) return;

            if (GetTagByName(tagName) != 0)
            {
                Debug.LogWarning($"[TagCollection] Tag \"{tagName}\" 已存在。");
                return;
            }

            // 从根节点逐层检查，自动补齐缺失的父级
            string[] parts = tagName.Split('.');
            uint parentValue = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                string ancestorPath = string.Join(".", parts, 0, i + 1);
                uint existing = GetTagByName(ancestorPath);
                if (existing != 0)
                {
                    parentValue = existing;
                    continue;
                }

                uint result = AddTag(ancestorPath, parentValue);
                if (result == 0)
                {
                    Debug.LogError($"[TagCollection] 添加 \"{ancestorPath}\" 失败，同层级可能已满（最多 255 个）。");
                    return;
                }

                Debug.Log($"[TagCollection] 已添加 Tag \"{ancestorPath}\" = 0x{result:X8}");
                parentValue = result;
            }

            _newTagName = "";
        }

        [TitleGroup("删除 Tag")]
        [ShowInInspector, HideLabel, ValueDropdown("GetTagNames"), PropertyOrder(200)]
        private string _removeTagName = "";

        [TitleGroup("删除 Tag")]
        [Button("删除", ButtonSizes.Medium), PropertyOrder(201)]
        [EnableIf("@!string.IsNullOrWhiteSpace(_removeTagName)")]
        private void RemoveSelectedTag()
        {
            if (string.IsNullOrWhiteSpace(_removeTagName)) return;

            if (RemoveTag(_removeTagName))
                Debug.Log($"[TagCollection] 已删除 Tag \"{_removeTagName}\"");
            else
                Debug.LogWarning($"[TagCollection] 未找到 Tag \"{_removeTagName}\"");

            _removeTagName = "";
        }

        /// <summary> 为 Odin ValueDropdown 提供已有 Tag 名称列表 </summary>
        private IEnumerable<string> GetTagNames()
        {
            return _tags.Select(t => t.Name);
        }

        // ────────── 运行时 API ──────────

        /// <summary>
        /// 添加一个新 Tag，自动在 parentValue 下分配唯一 uint 值。
        /// parentValue 为 0 表示顶层。返回分配的值，0 表示失败。
        /// </summary>
        public uint AddTag(string tagName, uint parentValue = 0)
        {
            uint value = TagExtension.GenerateTag(tagName, parentValue, _tags.Select(t => t.Value));
            if (value == 0) return 0;

            _tags.Add(new TagDefinition { Name = tagName, Value = value });
            return value;
        }

        /// <summary> 移除指定名称的 Tag </summary>
        public bool RemoveTag(string tagName)
        {
            return _tags.RemoveAll(t => t.Name == tagName) > 0;
        }

        /// <summary> 根据名称查找 Tag 值，找不到返回 0 </summary>
        public uint GetTagByName(string tagName)
        {
            foreach (var def in _tags)
            {
                if (def.Name == tagName)
                    return def.Value;
            }
            return 0;
        }

        /// <summary> 根据 uint 值查找名称，找不到返回 null </summary>
        public string GetNameByTag(uint tag)
        {
            foreach (var def in _tags)
            {
                if (def.Value == tag)
                    return def.Name;
            }
            return null;
        }

        /// <summary> 是否包含指定 Tag 值 </summary>
        public bool Contains(uint tag)
        {
            foreach (var def in _tags)
            {
                if (def.Value == tag)
                    return true;
            }
            return false;
        }
    }
}
