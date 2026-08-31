using System;
using Shizuku.Core;
using UnityEngine;

namespace Shizuku.Graph
{
    // ============================================================
    // SetActive — 设置 GameObject 的激活状态
    // ============================================================
    [Serializable]
    [NodeMenuItem("场景/Set Active", Description = "设置 GameObject 激活状态")]
    public class SetActiveNode : ShizukuRunnableNode
    {
        public override Color TitleBarColor => new Color(0.3f, 0.7f, 0.5f, 1f);

        [SerializeReference]
        private GameObjectParameterEdgePort _target = new() { IsOut = false, Name = "target" };

        [SerializeReference]
        private BoolParameterEdgePort _active = new() { IsOut = false, Name = "active" };

        [SerializeField]
        private ChainPort _nextPort = new() { Name = "next" };

        protected override void OnExecute()
        {
            if (_target.Value != null)
                _target.Value.SetActive(_active.Value);
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _nextPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }
    }

    // ============================================================
    // FindGameObject — 按名称查找场景中的 GameObject
    // ============================================================
    [Serializable]
    [NodeMenuItem("场景/Find GameObject", Description = "按名称查找 GameObject")]
    public class FindGameObjectNode : ShizukuValueNode
    {
        public override Color TitleBarColor => new Color(0.3f, 0.7f, 0.5f, 1f);

        [SerializeReference]
        private StringParameterEdgePort _name = new() { IsOut = false, Name = "name" };

        [SerializeReference]
        private GameObjectParameterEdgePort _result = new() { IsOut = true, Name = "result" };

        protected override void OnComputeOutputValues()
        {
            _result.Value = GameObject.Find(_name.Value);
        }
    }

    // ============================================================
    // GetComponent — 从 GameObject 上获取组件（输出为 GameObject 自身，用于链式操作）
    // ============================================================
    [Serializable]
    [NodeMenuItem("场景/Has Component", Description = "检查 GameObject 是否拥有指定组件")]
    public class HasComponentNode : ShizukuValueNode
    {
        public override Color TitleBarColor => new Color(0.3f, 0.7f, 0.5f, 1f);

        [SerializeReference]
        private GameObjectParameterEdgePort _target = new() { IsOut = false, Name = "target" };

        [SerializeReference]
        private StringParameterEdgePort _componentName = new() { IsOut = false, Name = "component" };

        [SerializeReference]
        private BoolParameterEdgePort _hasComponent = new() { IsOut = true, Name = "has" };

        protected override void OnComputeOutputValues()
        {
            if (_target.Value != null && !string.IsNullOrEmpty(_componentName.Value))
            {
                _hasComponent.Value = _target.Value.GetComponent(_componentName.Value) != null;
            }
            else
            {
                _hasComponent.Value = false;
            }
        }
    }

    // ============================================================
    // GetTransform — 获取 GameObject 的 Transform
    // ============================================================
    [Serializable]
    [NodeMenuItem("场景/Get Transform", Description = "获取 GameObject 的 Transform")]
    public class GetTransformNode : ShizukuValueNode
    {
        public override Color TitleBarColor => new Color(0.3f, 0.7f, 0.5f, 1f);

        [SerializeReference]
        private GameObjectParameterEdgePort _target = new() { IsOut = false, Name = "target" };

        [SerializeReference]
        private TransformParameterEdgePort _transform = new() { IsOut = true, Name = "transform" };

        protected override void OnComputeOutputValues()
        {
            _transform.Value = _target.Value != null ? _target.Value.transform : null;
        }
    }

    // ============================================================
    // GetPosition / SetPosition — Transform 位置读写
    // ============================================================
    [Serializable]
    [NodeMenuItem("场景/Get World Position", Description = "获取 Transform 的世界坐标")]
    public class GetPositionNode : ShizukuValueNode
    {
        public override Color TitleBarColor => new Color(0.3f, 0.7f, 0.5f, 1f);

        [SerializeReference]
        private TransformParameterEdgePort _transform = new() { IsOut = false, Name = "transform" };

        [SerializeReference]
        private Vector3ParameterEdgePort _position = new() { IsOut = true, Name = "position" };

        protected override void OnComputeOutputValues()
        {
            _position.Value = _transform.Value != null ? _transform.Value.position : Vector3.zero;
        }
    }

    [Serializable]
    [NodeMenuItem("场景/Set World Position", Description = "设置 Transform 的世界坐标")]
    public class SetPositionNode : ShizukuRunnableNode
    {
        public override Color TitleBarColor => new Color(0.3f, 0.7f, 0.5f, 1f);

        [SerializeReference]
        private TransformParameterEdgePort _transform = new() { IsOut = false, Name = "transform" };

        [SerializeReference]
        private Vector3ParameterEdgePort _position = new() { IsOut = false, Name = "position" };

        [SerializeField]
        private ChainPort _nextPort = new() { Name = "next" };

        protected override void OnExecute()
        {
            if (_transform.Value != null)
                _transform.Value.position = _position.Value;
        }

        protected override bool OnSelectNextNode(out string nextNodeGUID)
        {
            nextNodeGUID = _nextPort.NextNodeGuid;
            return !string.IsNullOrEmpty(nextNodeGUID);
        }
    }
}

