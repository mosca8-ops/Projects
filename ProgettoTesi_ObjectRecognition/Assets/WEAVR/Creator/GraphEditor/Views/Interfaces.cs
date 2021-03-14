using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    interface IMasterPort
    {
        void EdgesCreated(GraphView graphView, Port input, Port output, List<Edge> edgesToCreate);
        bool DeleteEdge(GraphView graphView, GraphElement edge);
    }

    interface IBadgeClient
    {
        void ClearBadge();
    }
}
