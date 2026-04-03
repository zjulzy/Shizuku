using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Shizuku.Tag
{
    /// <summary>
    /// 单个 Tag 定义，包含名称和序列化存储的 uint 值。
    /// 值在添加时由 TagConfig 自动分配，保证唯一。
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
    /// Tag 关系条目：一个源 Tag 对应一组目标 Tag（按名称存储）。
    /// </summary>
    [Serializable]
    public struct TagRelationEntry
    {
        public string SourceTag;
        public List<string> TargetTags;
    }

    /// <summary>
    /// Tag 集合配置，ScriptableObject 资产。
    /// 在编辑器中集中管理项目中所有可用的 Tag 定义。
    /// </summary>
    [CreateAssetMenu(fileName = "TagConfig", menuName = "Shizuku/Tag/Tag Config")]
    public class TagConfig : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private List<TagDefinition> _tags = new List<TagDefinition>();

        /// <summary> 所有 Tag 定义（只读） </summary>
        public IReadOnlyList<TagDefinition> Tags => _tags;

        // ────────── 阻挡 & 互斥规则 ──────────

        /// <summary>
        /// 阻挡规则：SourceTag 想添加到实体上时，如果实体已拥有 TargetTags 中任一 Tag，则添加被阻止。
        /// </summary>
        [SerializeField, HideInInspector]
        private List<TagRelationEntry> _blockRules = new List<TagRelationEntry>();

        /// <summary>
        /// 互斥规则：SourceTag 被添加到实体上后，TargetTags 中的 Tag 会被立即移除。
        /// </summary>
        [SerializeField, HideInInspector]
        private List<TagRelationEntry> _cancelRules = new List<TagRelationEntry>();

        public IReadOnlyList<TagRelationEntry> BlockRules => _blockRules;
        public IReadOnlyList<TagRelationEntry> CancelRules => _cancelRules;

        // ────────── 阻挡 & 互斥 编辑 API ──────────

        /// <summary> 设置某个 Tag 的阻挡列表（按名称），覆盖旧值 </summary>
        public void SetBlockRule(string sourceTag, List<string> targets)
        {
            RemoveBlockRule(sourceTag);
            if (targets != null && targets.Count > 0)
                _blockRules.Add(new TagRelationEntry { SourceTag = sourceTag, TargetTags = new List<string>(targets) });
        }

        /// <summary> 移除某个 Tag 的阻挡规则 </summary>
        public void RemoveBlockRule(string sourceTag)
        {
            _blockRules.RemoveAll(e => e.SourceTag == sourceTag);
        }

        /// <summary> 设置某个 Tag 的互斥列表（按名称），覆盖旧值 </summary>
        public void SetCancelRule(string sourceTag, List<string> targets)
        {
            RemoveCancelRule(sourceTag);
            if (targets != null && targets.Count > 0)
                _cancelRules.Add(new TagRelationEntry { SourceTag = sourceTag, TargetTags = new List<string>(targets) });
        }

        /// <summary> 移除某个 Tag 的互斥规则 </summary>
        public void RemoveCancelRule(string sourceTag)
        {
            _cancelRules.RemoveAll(e => e.SourceTag == sourceTag);
        }

        // ────────── 阻挡 & 互斥 运行时查询 ──────────

        /// <summary>
        /// 检查 tag 是否被 container 中已有的标签阻挡。
        /// </summary>
        public bool IsBlocked(uint tag, TagCollection container)
        {
            string name = GetNameByTag(tag);
            if (name == null) return false;

            foreach (var rule in _blockRules)
            {
                if (rule.SourceTag != name) continue;
                foreach (var target in rule.TargetTags)
                {
                    uint targetVal = GetTagByName(target);
                    if (targetVal != 0 && container.HasExact(targetVal))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取 tag 添加后需要被移除的标签集合。
        /// </summary>
        public void GetCancelledTags(uint tag, TagCollection container, List<uint> result)
        {
            result.Clear();
            string name = GetNameByTag(tag);
            if (name == null) return;

            foreach (var rule in _cancelRules)
            {
                if (rule.SourceTag != name) continue;
                foreach (var target in rule.TargetTags)
                {
                    uint targetVal = GetTagByName(target);
                    if (targetVal != 0 && container.HasExact(targetVal))
                        result.Add(targetVal);
                }
            }
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

        /// <summary> 移除指定名称的 Tag（同时清理关联的阻挡/互斥规则） </summary>
        public bool RemoveTag(string tagName)
        {
            bool removed = _tags.RemoveAll(t => t.Name == tagName) > 0;
            if (removed)
            {
                // 移除作为 source 的规则
                _blockRules.RemoveAll(e => e.SourceTag == tagName);
                _cancelRules.RemoveAll(e => e.SourceTag == tagName);
                // 从其他规则的 target 列表中移除
                foreach (var rule in _blockRules)
                    rule.TargetTags.Remove(tagName);
                foreach (var rule in _cancelRules)
                    rule.TargetTags.Remove(tagName);
                // 清理空规则
                _blockRules.RemoveAll(e => e.TargetTags.Count == 0);
                _cancelRules.RemoveAll(e => e.TargetTags.Count == 0);
            }
            return removed;
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
