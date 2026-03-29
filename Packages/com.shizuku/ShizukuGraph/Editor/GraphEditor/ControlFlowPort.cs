using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

/// <summary>
/// 自定义控制流端口 - 菱形样式
/// 通过封装InstantiatePort确保连接逻辑正常工作
/// </summary>
namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public class ControlFlowPort : Port
    {
        private static StyleSheet s_StyleSheet;

        static ControlFlowPort()
        {
            s_StyleSheet = Resources.Load<StyleSheet>("ControlFlowPort");
            if (s_StyleSheet == null)
            {
                Debug.LogWarning("⚠️ ControlFlowPort样式表未找到");
            }
        }

        /// <summary>
        /// 创建控制流端口（封装了InstantiatePort的调用）
        /// 通过反射创建真正的ControlFlowPort实例
        /// </summary>
        public static ControlFlowPort Create(Node node, Orientation orientation, Direction direction, Capacity capacity, string portName)
        {
            // 先用InstantiatePort创建普通端口，获取EdgeConnector
            var tempPort = node.InstantiatePort(orientation, direction, capacity, typeof(Chain));

            // 创建ControlFlowPort实例
            var controlFlowPort = new ControlFlowPort(orientation, direction, capacity, typeof(Chain))
            {
                portName = portName
            };

            // 通过反射复制EdgeConnector
            var edgeConnectorField = typeof(Port).GetField("m_EdgeConnector", BindingFlags.NonPublic | BindingFlags.Instance);
            if (edgeConnectorField != null)
            {
                var edgeConnector = edgeConnectorField.GetValue(tempPort);
                if (edgeConnector != null)
                {
                    edgeConnectorField.SetValue(controlFlowPort, edgeConnector);

                    // 添加EdgeConnector作为Manipulator
                    var manipulator = edgeConnector as IManipulator;
                    if (manipulator != null)
                    {
                        controlFlowPort.AddManipulator(manipulator);
                    }
                }
            }

            // 应用自定义样式
            ApplyControlFlowStyle(controlFlowPort);

            return controlFlowPort;
        }

        /// <summary>
        /// 为端口应用控制流样式
        /// </summary>
        private static void ApplyControlFlowStyle(Port port)
        {
            // 加载样式表
            if (s_StyleSheet != null && !port.styleSheets.Contains(s_StyleSheet))
            {
                port.styleSheets.Add(s_StyleSheet);
            }

            port.AddToClassList("control-flow-port");

            // 延迟设置connector样式
            port.schedule.Execute(() =>
            {
                SetupConnectorStyle(port);
            }).ExecuteLater(0);
        }

        /// <summary>
        /// 设置connector的样式
        /// 控制流端口：横向胶囊形状，体现方向性
        /// </summary>
        private static void SetupConnectorStyle(Port port)
        {
            var connector = port.Q("connector");
            if (connector != null)
            {
                bool isInput = port.direction == Direction.Input;

                // 设置为横向胶囊形状（椭圆）- 增大尺寸
                connector.style.width = 16;
                connector.style.height = 12;

                // 设置圆角，创建明显的方向性
                // 输入端口：左边完全圆润，右边尖锐 (
                // 输出端口：左边尖锐，右边完全圆润 )
                if (isInput)
                {
                    // 左边：完全圆润（圆角半径 = 高度的一半）
                    connector.style.borderTopLeftRadius = 6;
                    connector.style.borderBottomLeftRadius = 6;
                    // 右边：尖锐（圆角半径很小）
                    connector.style.borderTopRightRadius = 1;
                    connector.style.borderBottomRightRadius = 1;
                }
                else
                {
                    // 左边：尖锐（圆角半径很小）
                    connector.style.borderTopLeftRadius = 1;
                    connector.style.borderBottomLeftRadius = 1;
                    // 右边：完全圆润（圆角半径 = 高度的一半）
                    connector.style.borderTopRightRadius = 6;
                    connector.style.borderBottomRightRadius = 6;
                }

                // 边框样式 - 减小边框宽度使空心更明显
                Color borderColor = new Color(1f, 0.78f, 0.39f, 1f); // 橙黄色
                connector.style.borderLeftWidth = 1.5f;
                connector.style.borderRightWidth = 1.5f;
                connector.style.borderTopWidth = 1.5f;
                connector.style.borderBottomWidth = 1.5f;
                connector.style.borderLeftColor = borderColor;
                connector.style.borderRightColor = borderColor;
                connector.style.borderTopColor = borderColor;
                connector.style.borderBottomColor = borderColor;

                // 初始背景（未连接）
                connector.style.backgroundColor = Color.clear;

                // 移除旋转
                connector.style.rotate = new Rotate(new Angle(0, AngleUnit.Degree));

                // 隐藏内部cap元素
                var cap = connector.Q("cap");
                if (cap != null)
                {
                    cap.style.display = DisplayStyle.None;
                }

                // 监听连接状态变化
                SetupConnectionStateListener(port, connector);
            }
        }

        /// <summary>
        /// 设置连接状态监听，根据是否连接改变填充状态
        /// </summary>
        private static void SetupConnectionStateListener(Port port, VisualElement connector)
        {
            // 定期检查连接状态并更新样式
            port.schedule.Execute(() =>
            {
                bool isConnected = port.connected;

                // 未连接：空心（透明背景 + 边框）
                // 已连接：实心（填充背景 + 边框）
                Color fillColor = isConnected 
                    ? new Color(1f, 0.5f, 0.2f, 1f)      // 实心 - 深橙色填充（更醒目）
                    : Color.clear;                        // 空心 - 透明

                connector.style.backgroundColor = fillColor;
            }).Every(100); // 每100ms检查一次
        }

        // 保留protected构造函数以防需要继承
        protected ControlFlowPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, System.Type type)
            : base(portOrientation, portDirection, portCapacity, type)
        {
        }
    }


}
