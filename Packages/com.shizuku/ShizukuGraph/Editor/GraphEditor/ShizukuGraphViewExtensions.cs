using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;


namespace Shizuku.Graph.Editor
{
    using Shizuku.Graph;
    using Shizuku.Core;
    public static class ShizukuGraphViewExtensions
    {
        /// <summary>
        /// 检查添加这条边是否会形成环
        /// 使用DFS（深度优先搜索）检测从目标节点是否能回到源节点
        /// </summary>
        public static bool WouldCreateCycle(this ShizukuGraphView graph, Edge newEdge)
        {
            var sourceNode = newEdge.output.node;
            var targetNode = newEdge.input.node;

            // 使用DFS检查从targetNode出发是否能到达sourceNode
            var visited = new HashSet<Node>();
            return HasPathDFS(targetNode, sourceNode, visited);
        }

        /// <summary>
        /// 深度优先搜索：检查从startNode是否存在路径到达targetNode
        /// </summary>
        private static bool HasPathDFS(Node startNode, Node targetNode, HashSet<Node> visited)
        {
            if (startNode == targetNode)
            {
                return true; // 找到了从target到source的路径，说明会形成环
            }

            if (visited.Contains(startNode))
            {
                return false; // 已经访问过这个节点
            }

            visited.Add(startNode);

            // 遍历当前节点的所有输出边
            var outputPorts = startNode.outputContainer.Query<Port>().ToList();
            foreach (var port in outputPorts)
            {
                if (port.connected)
                {
                    foreach (var edge in port.connections)
                    {
                        var nextNode = edge.input.node;
                        if (HasPathDFS(nextNode, targetNode, visited))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
