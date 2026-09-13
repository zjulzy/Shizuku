using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;

    [System.Serializable]
    public sealed class GraphSceneVariableBinding
    {
        [SerializeField]
        private string _variableGuid;

        [SerializeField]
        private UnityEngine.Object _value;

        public string VariableGuid => _variableGuid;
        public UnityEngine.Object Value => _value;

        public GraphSceneVariableBinding(string variableGuid, UnityEngine.Object value)
        {
            _variableGuid = variableGuid;
            _value = value;
        }

        internal void SetValue(UnityEngine.Object value)
        {
            _value = value;
        }
    }

    public class GraphRunner : MonoBehaviour
    {
        public ShizukuGraphBase GraphAsset;

        [SerializeField, HideInInspector]
        private System.Collections.Generic.List<GraphSceneVariableBinding> _sceneVariableBindings = new();

        [System.NonSerialized]
        private ShizukuGraphRuntime<ShizukuGraphBase> _runtime;

        public System.Collections.Generic.IReadOnlyList<GraphSceneVariableBinding> SceneVariableBindings => _sceneVariableBindings;
        public ShizukuGraphBase RuntimeGraph => _runtime?.Instance;

        public bool TryGetSceneVariableBinding(string variableGuid, out UnityEngine.Object value)
        {
            var binding = _sceneVariableBindings.Find(item =>
                item != null && item.VariableGuid == variableGuid);
            value = binding?.Value;
            return binding != null;
        }

        public bool SetSceneVariableBinding(string variableGuid, UnityEngine.Object value)
        {
            var variable = GraphAsset?.GetVariableByGUID(variableGuid);
            if (variable == null || !IsSupportedSceneVariable(variable, value))
                return false;

            var existing = _sceneVariableBindings.Find(item =>
                item != null && item.VariableGuid == variableGuid);

            if (value == null)
            {
                if (existing != null)
                    _sceneVariableBindings.Remove(existing);
                return true;
            }

            if (existing != null)
                existing.SetValue(value);
            else
                _sceneVariableBindings.Add(new GraphSceneVariableBinding(variableGuid, value));

            return true;
        }

        public int RemoveInvalidSceneVariableBindings()
        {
            if (GraphAsset == null)
            {
                var removedCount = _sceneVariableBindings.Count;
                _sceneVariableBindings.Clear();
                return removedCount;
            }

            return _sceneVariableBindings.RemoveAll(binding =>
            {
                if (binding == null)
                    return true;

                var variable = GraphAsset.GetVariableByGUID(binding.VariableGuid);
                return variable == null || !IsSupportedSceneVariable(variable, binding.Value);
            });
        }

        void Start()
        {
            if (_runtime != null)
                return;

            if (GraphAsset != null)
            {
                _runtime = ShizukuGraphRuntime<ShizukuGraphBase>.Create(
                    GraphAsset,
                    gameObject,
                    afterInitialize: ApplySceneVariableBindings);
            }
        }

        private void ApplySceneVariableBindings(ShizukuGraphBase runtimeGraph)
        {
            foreach (var binding in _sceneVariableBindings)
            {
                if (binding == null || binding.Value == null)
                    continue;

                var variable = runtimeGraph.GetVariableByGUID(binding.VariableGuid);
                if (variable == null)
                {
                    Debug.LogWarning($"[GraphRunner] 场景变量绑定指向不存在的 GUID '{binding.VariableGuid}'。", this);
                    continue;
                }

                switch (variable.Type)
                {
                    case VariableType.GameObject when binding.Value is GameObject gameObjectValue:
                        runtimeGraph.SetVariableGameObject(variable.GUID, gameObjectValue);
                        break;
                    case VariableType.Transform when binding.Value is Transform transformValue:
                        runtimeGraph.SetVariableTransform(variable.GUID, transformValue);
                        break;
                    default:
                        Debug.LogWarning(
                            $"[GraphRunner] 变量 '{variable.Name}' 的场景绑定类型与 {variable.Type} 不匹配，已忽略。",
                            this);
                        break;
                }
            }
        }

        private static bool IsSupportedSceneVariable(GraphVariable variable, UnityEngine.Object value)
        {
            if (value == null)
                return variable.Type == VariableType.GameObject || variable.Type == VariableType.Transform;

            return variable.Type switch
            {
                VariableType.GameObject => value is GameObject,
                VariableType.Transform => value is Transform,
                _ => false,
            };
        }

        void Update()
        {
            _runtime?.Tick();
        }

        void OnDestroy()
        {
            _runtime?.Dispose();
            _runtime = null;
        }
    }


}
