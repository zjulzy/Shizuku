using System.Collections.Generic;

namespace Shizuku.Tag
{
    /// <summary>
    /// 存储一个个体上所有的 Tag。
    /// 每个 Tag 是一个 uint，按 8-8-8-8 分成四层：
    ///   [Layer1 : 8][Layer2 : 8][Layer3 : 8][Layer4 : 8]
    /// 例如 0x01_02_03_00 表示三级标签（第四层为 0）。
    /// </summary>
    public class TagContainer
    {
        private readonly HashSet<uint> _tags = new HashSet<uint>();

        /// <summary> 当前持有的 Tag 数量 </summary>
        public int Count => _tags.Count;

        /// <summary> 添加一个 Tag，返回是否新增成功 </summary>
        public bool Add(uint tag) => _tags.Add(tag);

        /// <summary> 移除一个 Tag，返回是否存在并移除 </summary>
        public bool Remove(uint tag) => _tags.Remove(tag);

        /// <summary> 精确匹配：是否持有该 Tag </summary>
        public bool HasExact(uint tag) => _tags.Contains(tag);

        /// <summary>
        /// 层级匹配：是否持有任意一个与 tag 前缀相同的 Tag。
        /// 自动根据 tag 自身最深的非零层确定比较深度。
        /// 例如 0x01_02_00_00 → 比较前 2 层（高 16 位）。
        /// </summary>
        public bool HasAncestor(uint tag)
        {
            int depth = tag.GetDepth();
            uint mask = TagExtension.GetLayerMask(depth);
            uint prefix = tag & mask;
            foreach (uint t in _tags)
            {
                if ((t & mask) == prefix)
                    return true;
            }
            return false;
        }

        /// <summary> 清空所有 Tag </summary>
        public void Clear() => _tags.Clear();

        /// <summary> 获取所有 Tag 的只读迭代 </summary>
        public HashSet<uint>.Enumerator GetEnumerator() => _tags.GetEnumerator();
    }
}