using System;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 可序列化的分组数据
/// </summary>
namespace Shizuku.Graph
{
    using Shizuku.Core;
    [Serializable]
    public class GroupData
    {
        [SerializeField]
        public string GUID;

        [SerializeField]
        public string Title;

        [SerializeField]
        public float4 PositionAndSize; // x, y, width, height

        public GroupData()
        {
            GUID = System.Guid.NewGuid().ToString();
            Title = "新建分组";
            PositionAndSize = new float4(0, 0, 300, 200);
        }

        public GroupData(string title, float4 positionAndSize)
        {
            GUID = System.Guid.NewGuid().ToString();
            Title = title;
            PositionAndSize = positionAndSize;
        }
    }


}
