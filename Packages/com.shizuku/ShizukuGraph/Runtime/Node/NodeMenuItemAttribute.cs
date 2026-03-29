using System;

/// <summary>
/// 标记节点在创建菜单中的显示信息
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NodeMenuItemAttribute : Attribute
    {
        /// <summary>
        /// 节点在菜单中的路径，使用 "/" 分隔，如 "数学/加法"
        /// </summary>
        public string MenuPath { get; set; }

        /// <summary>
        /// 节点分类
        /// </summary>
        public NodeCategory Category { get; set; }

        /// <summary>
        /// 节点描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order { get; set; }

        public NodeMenuItemAttribute(string menuPath, NodeCategory category = NodeCategory.Basic)
        {
            MenuPath = menuPath;
            Category = category;
            Order = 0;
        }
    }


}
