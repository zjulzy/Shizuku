using System.Collections.Generic;

namespace Shizuku.Tag
{
    public static class TagExtension
    {
        /// <summary>
        /// 根据已有 tag 集合和名称，为新 tag 生成一个不重复的 uint 值。
        /// 同层级下自增分配 1-255。
        /// 例如已有 "Element"=0x01000000，新增 "Element.Fire" 会在 0x01_XX_0000 中找到下一个可用的 XX。
        /// </summary>
        public static uint GenerateTag(string tagName, uint parentValue, IEnumerable<uint> existingTags)
        {
            int parentDepth = parentValue == 0 ? 0 : parentValue.GetDepth();
            int targetLayer = parentDepth; // 0-based，要写入的字节位置
            if (targetLayer >= 4) return 0; // 已经 4 层满了

            int shift = 24 - targetLayer * 8;

            // 收集同一父级下已使用的字节值
            uint parentMask = parentDepth == 0 ? 0u : GetLayerMask(parentDepth);
            var used = new HashSet<byte>();
            foreach (uint t in existingTags)
            {
                // 前缀匹配父级
                if ((t & parentMask) == (parentValue & parentMask))
                {
                    byte layerByte = (byte)((t >> shift) & 0xFF);
                    if (layerByte != 0)
                        used.Add(layerByte);
                }
            }

            // 分配下一个可用值 1-255
            for (byte v = 1; v != 0; v++) // byte 溢出后变 0 自动停
            {
                if (!used.Contains(v))
                    return parentValue | ((uint)v << shift);
            }

            return 0; // 255 个都用完了
        }

        // ...existing code...
        /// <summary>
        /// 计算 tag 的有效深度（最深的非零层）。
        /// 0x01_00_00_00 → 1, 0x01_02_00_00 → 2,
        /// 0x01_02_03_00 → 3, 0x01_02_03_04 → 4
        /// </summary>
        public static int GetDepth(this uint tag)
        {
            if ((tag & 0x000000FFu) != 0) return 4;
            if ((tag & 0x0000FF00u) != 0) return 3;
            if ((tag & 0x00FF0000u) != 0) return 2;
            return 1;
        }

        /// <summary>
        /// 根据层数返回高位掩码。
        /// depth=1 → 0xFF000000, depth=2 → 0xFFFF0000,
        /// depth=3 → 0xFFFFFF00, depth=4 → 0xFFFFFFFF
        /// </summary>
        public static uint GetLayerMask(int depth)
        {
            return depth switch
            {
                1 => 0xFF000000u,
                2 => 0xFFFF0000u,
                3 => 0xFFFFFF00u,
                _ => 0xFFFFFFFFu
            };
        }

        /// <summary>
        /// 判断 tagA 是否是 tagB 的前缀（即 tagB 属于 tagA 的子层级）。
        /// 例如 0x01_02_00_00.IsPrefixOf(0x01_02_03_04) → true
        ///      0x01_02_00_00.IsPrefixOf(0x01_03_00_00) → false
        /// </summary>
        public static bool IsPrefixOf(this uint tagA, uint tagB)
        {
            uint mask = GetLayerMask(tagA.GetDepth());
            return (tagA & mask) == (tagB & mask);
        }
    }
}