using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shizuku.Graph
{
    /// <summary>
    /// 函数返回节点
    /// 作为函数子图的执行终点，接受控制流输入，没有控制流输出。
    /// 输入端口根据函数的 OutputParameters 动态生成（函数的返回值从这里收集）。
    /// </summary>
    [Serializable]
    public class MethodReturnNode : ShizukuNormalNode
    {
        /// <summary>
        /// 所属函数的 GUID
        /// </summary>
        [SerializeField] public string MethodGUID;

        /// <summary>
        /// 动态输入端口列表（对应函数的输出参数/返回值）
        /// 函数内部计算的结果通过这些端口流入，供调用者读取
        /// </summary>
        [SerializeField]
        public List<MethodPort> InputPorts = new List<MethodPort>();

        public override string Title => "◀ 函数返回";
        public override Color TitleBarColor => new Color(0.6f, 0.2f, 0.3f, 1f);

        // 有控制流输入（执行到此节点表示函数结束），没有控制流输出
        public sealed override bool SupportControlInput => true;
        public sealed override bool SupportControlOutput => false;

        public override void Init(ShizukuGraphBase parentGraph)
        {
            base.Init(parentGraph);

            // 将动态端口注册到 SelfInputPorts，使参数边系统能找到它们
            foreach (var methodPort in InputPorts)
            {
                if (methodPort.Port != null && !SelfInputPorts.Contains(methodPort.Port))
                {
                    SelfInputPorts.Add(methodPort.Port);
                }
            }
        }

        /// <summary>
        /// 根据函数定义同步端口列表
        /// </summary>
        public void SyncPortsFromMethod(ShizukuMethod method)
        {
            var newPorts = new List<MethodPort>();

            foreach (var param in method.OutputParameters)
            {
                var existing = InputPorts.Find(p => p.Name == param.Name && p.Type == param.Type);
                if (existing != null)
                {
                    newPorts.Add(existing);
                }
                else
                {
                    newPorts.Add(new MethodPort(param.Name, param.Type, isOut: false));
                }
            }

            InputPorts = newPorts;
        }

        /// <summary>
        /// 收集所有返回值（运行时由 InvokeMethodNode 调用）
        /// </summary>
        public Dictionary<string, object> CollectReturnValues()
        {
            // 先拉取上游的值
            GetInputValues();

            var result = new Dictionary<string, object>();
            foreach (var methodPort in InputPorts)
            {
                if (methodPort.Port != null)
                {
                    result[methodPort.Name] = methodPort.Port.GetSelfValue();
                }
            }
            return result;
        }
    }
}

