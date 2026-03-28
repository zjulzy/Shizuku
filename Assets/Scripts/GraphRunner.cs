using UnityEngine;

public class GraphRunner : MonoBehaviour
{
    public ShizukuGraphBase GraphAsset;

    void Start()
    {
        if (GraphAsset != null)
        {
            // 运行时克隆 SO，避免多个 GraphRunner 引用同一份图资产导致状态共享
            GraphAsset = Instantiate(GraphAsset);
            GraphAsset.name = $"{GraphAsset.name}_{GetInstanceID()}";
            GraphAsset.Init();
        }
    }

    void Update()
    {
        if (GraphAsset != null)
        {
            GraphAsset.Update();
        }
    }

    void OnDestroy()
    {
        if (GraphAsset != null)
        {
            Destroy(GraphAsset);
            GraphAsset = null;
        }
    }
}

