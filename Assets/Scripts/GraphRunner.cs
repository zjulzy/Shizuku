using UnityEngine;

public class GraphRunner : MonoBehaviour
{
    public ShizukuGraphBase GraphAsset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GraphAsset.Init();
    }

    // Update is called once per frame
    void Update()
    {
        GraphAsset.Update();
    }
}
