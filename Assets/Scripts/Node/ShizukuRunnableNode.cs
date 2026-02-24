using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public abstract class ShizukuRunnableNode : ShizukuNormalNode
{
    

    public sealed override bool SupportControlInput => true;
    public sealed override bool SupportControlOutput => true;

    public override void Init(ShizukuGraphBase parentGraph)
    {
        base.Init(parentGraph);
    }

    public void Execute()
    {
        GetInputValues();
        OnExecute();

        if (OnSelectNextNode(out var guid))
        {
            if (_parentGraph.Guid2NodeMap.TryGetValue(guid, out var nextNode))
            {
                if (nextNode is ShizukuRunnableNode runnable)
                {
                    runnable.Execute();
                }
                else
                {
                    Debug.LogError($"Next node is not a runnable node: {guid}");
                }
            }
            else
            {
                Debug.LogError($"Next node not found: {guid}");
            }
        }
    }

    protected abstract void OnExecute();
    protected abstract bool OnSelectNextNode(out string nextNodeGUID);
}