using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shizuku.Graph.Editor
{
    internal sealed class AuthoringPort : Port
    {
        internal AuthoringPort(Orientation orientation, Direction direction, Capacity capacity, Type type)
            : base(orientation, direction, capacity, type)
        {
            m_EdgeConnector = new EdgeConnector<Edge>(new AuthoringEdgeListener());
            this.AddManipulator(m_EdgeConnector);
        }
    }

    internal sealed class AuthoringEdgeListener : IEdgeConnectorListener
    {
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            var source = edge.output ?? edge.input;
            var view = source?.node?.GetFirstAncestorOfType<ShizukuGraphView>();
            if (view != null) view.OpenAuthoringSearch(view.contentViewContainer.WorldToLocal(position), view.PanelToScreen(position), source);
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            if (graphView is ShizukuGraphView view && edge.input != null && edge.output != null)
            {
                // Remove only the temporary preview; the view owns validation and replacement.
                edge.input.Disconnect(edge); edge.output.Disconnect(edge);
                if (edge.parent != null) edge.RemoveFromHierarchy();
                view.ConnectAuthoringPorts(edge.output, edge.input);
            }
        }
    }
}
