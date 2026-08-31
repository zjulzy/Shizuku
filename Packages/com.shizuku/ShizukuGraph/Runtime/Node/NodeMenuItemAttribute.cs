using System;

/// <summary>
/// 标记节点在创建菜单中的显示信息
/// </summary>
namespace Shizuku.Graph
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NodeMenuItemAttribute : Attribute
    {
        /// <summary>
        /// 节点在菜单中的路径，使用 "/" 分隔，如 "数学/Add (Float)"
        /// </summary>
        public string MenuPath { get; }

        /// <summary>
        /// 节点在菜单和静态节点标题中使用的显示名称。
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MenuPath))
                    return string.Empty;

                var normalizedPath = MenuPath.Trim().Trim('/');
                var separatorIndex = normalizedPath.LastIndexOf('/');
                return (separatorIndex >= 0
                    ? normalizedPath.Substring(separatorIndex + 1)
                    : normalizedPath).Trim();
            }
        }

        /// <summary>
        /// 节点描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order { get; set; }

        public NodeMenuItemAttribute(string menuPath)
        {
            MenuPath = menuPath;
            Order = 0;
        }

        /// <summary>
        /// 验证菜单路径是否为“中文分组/英文节点名”。
        /// </summary>
        public static bool TryValidateMenuPath(string menuPath, out string error)
        {
            if (string.IsNullOrWhiteSpace(menuPath))
            {
                error = "菜单路径不能为空";
                return false;
            }

            var parts = menuPath.Split('/');
            if (parts.Length < 2)
            {
                error = "菜单路径至少需要一个中文分组和一个英文节点名";
                return false;
            }

            for (var i = 0; i < parts.Length - 1; i++)
            {
                var group = parts[i].Trim();
                if (group.Length == 0 || !ContainsChinese(group) || ContainsAsciiLetter(group))
                {
                    error = $"分组“{parts[i]}”必须使用中文且不能包含英文字母";
                    return false;
                }
            }

            var displayName = parts[parts.Length - 1].Trim();
            if (displayName.Length == 0 || !ContainsAsciiLetter(displayName) || ContainsChinese(displayName))
            {
                error = $"节点名“{parts[parts.Length - 1]}”必须使用英文且不能包含中文";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool ContainsAsciiLetter(string text)
        {
            foreach (var character in text)
            {
                if ((character >= 'A' && character <= 'Z') ||
                    (character >= 'a' && character <= 'z'))
                    return true;
            }

            return false;
        }

        private static bool ContainsChinese(string text)
        {
            foreach (var character in text)
            {
                if (character >= '\u3400' && character <= '\u9fff')
                    return true;
            }

            return false;
        }
    }


}
