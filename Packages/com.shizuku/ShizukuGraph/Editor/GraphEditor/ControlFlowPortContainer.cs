using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 控制流端口容器 - 用于放置Previous和Next端口
/// </summary>
namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public class ControlFlowPortContainer : VisualElement
    {
        private static StyleSheet s_StyleSheet;

        static ControlFlowPortContainer()
        {
            s_StyleSheet = Resources.Load<StyleSheet>("ControlFlowPortContainer");
            if (s_StyleSheet != null)
            {
                Debug.Log("✅ ControlFlowPortContainer样式表加载成功");
            }
            else
            {
                Debug.LogWarning("⚠️ ControlFlowPortContainer样式表未找到");
            }
        }

        public VisualElement LeftContainer;
        public VisualElement RightContainer;

        public ControlFlowPortContainer()
        {
            // 加载样式表
            if (s_StyleSheet != null && !styleSheets.Contains(s_StyleSheet))
            {
                styleSheets.Add(s_StyleSheet);
            }

            // 设置容器样式
            name = "control-flow-container";
            AddToClassList("control-flow-container");

            // 创建左侧容器（用于Previous端口）
            LeftContainer = new VisualElement();
            LeftContainer.name = "left-container";
            LeftContainer.AddToClassList("port-side-container");
            Add(LeftContainer);

            // 创建右侧容器（用于Next端口）
            RightContainer = new VisualElement();
            RightContainer.name = "right-container";
            RightContainer.AddToClassList("port-side-container");
            RightContainer.AddToClassList("port-right-container");
            Add(RightContainer);
        }

        /// <summary>
        /// 添加Previous端口到左侧容器
        /// </summary>
        public void AddPreviousPort(Port port)
        {
            LeftContainer.Add(port);
        }

        /// <summary>
        /// 添加Next端口到右侧容器
        /// </summary>
        public void AddNextPort(Port port)
        {
            RightContainer.Add(port);
        }
    }


}
