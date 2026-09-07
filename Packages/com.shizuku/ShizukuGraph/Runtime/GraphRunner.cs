using UnityEngine;

namespace Shizuku.Graph
{
    using Shizuku.Core;
    public class GraphRunner : MonoBehaviour
    {
        public ShizukuGraphBase GraphAsset;

        [SerializeField, HideInInspector]
        private ShizukuGraphBase _runtimeGraph;

        void Start()
        {
            if (_runtimeGraph != null)
                return;

            if (GraphAsset != null)
            {
                // 运行时克隆 SO，避免多个 GraphRunner 引用同一份图资产导致状态共享
                _runtimeGraph = Instantiate(GraphAsset);
                _runtimeGraph.name = $"{GraphAsset.name}_{GetInstanceID()}";
                _runtimeGraph.Init(gameObject);
            }
        }

        void Update()
        {
            if (_runtimeGraph != null)
            {
                _runtimeGraph.Update();
            }
        }

        void OnDestroy()
        {
            if (_runtimeGraph != null)
            {
                _runtimeGraph.DisposeRuntime();
                if (Application.isPlaying)
                    Destroy(_runtimeGraph);
                else
                    DestroyImmediate(_runtimeGraph);
                _runtimeGraph = null;
            }
        }
    }


}
