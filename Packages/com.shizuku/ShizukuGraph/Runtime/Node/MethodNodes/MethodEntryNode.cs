using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 函数入口节点
    /// 作为函数子图的执行起点，不接受控制流输入，有一个控制流输出。
    /// 输出端口根据函数的 InputParameters 动态生成（调用者传入的参数从这里流出）。
    /// </summary>
    [Serializable]
    public class MethodEntryNode : ShizukuRootNode
    {
        /// <summary>
        /// 所属函数的 GUID
        /// </summary>
        [SerializeField] public string MethodGUID;

        /// <summary>
        /// 动态输出端口列表（对应函数的输入参数）
        /// 调用者传入的值通过这些端口流出给函数内部节点使用
        /// </summary>
        [SerializeField]
        public List<MethodPort> OutputPorts = new List<MethodPort>();

        public override string Title => "▶ 函数入口";
        public override Color TitleBarColor => new Color(0.2f, 0.6f, 0.3f, 1f);

        public override void Init(INodeContext context)
        {
            base.Init(context);

            // 将动态端口注册到 SelfOutputPorts，使参数边系统能找到它们
            foreach (var methodPort in OutputPorts)
            {
                if (methodPort.Port != null && !SelfOutputPorts.Contains(methodPort.Port))
                {
                    SelfOutputPorts.Add(methodPort.Port);
                }
            }
        }

        /// <summary>
        /// 根据函数定义同步端口列表
        /// 保留名称和类型都匹配的已有端口（避免破坏已连接的边），
        /// 移除多余的，添加新增的。
        /// </summary>
        public void SyncPortsFromMethod(ShizukuMethod method)
        {
            var newPorts = new List<MethodPort>();

            foreach (var param in method.InputParameters)
            {
                // 查找已有的同名同类型端口
                var existing = OutputPorts.Find(p => p.Name == param.Name && p.Type == param.Type);
                if (existing != null)
                {
                    newPorts.Add(existing);
                }
                else
                {
                    newPorts.Add(new MethodPort(param.Name, param.Type, isOut: true));
                }
            }

            OutputPorts = newPorts;
        }
    }
}


