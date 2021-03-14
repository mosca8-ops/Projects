using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Procedure
{
    public class DataGraph<N, E>
    {
        public delegate bool IsValid(N node, E edge);
        public delegate N GetNodeDelegate(E edge);
        public delegate float GetWeightDelegate(E edge);
        public delegate IEnumerable<E> GetEdgesDelegate(N node);

        private Dictionary<N, DataNode> m_nodes;
        private Dictionary<E, DataNode> m_edges;
        private E[,] m_edgesGraph;
        private float[,] m_weightGraph;
        private List<DataNode> m_roots;
        private int m_progressiveIndex;

        public IsValid Validation { get; private set; }
        public GetNodeDelegate GetSource { get; private set; }
        public GetNodeDelegate GetDest { get; private set; }
        public GetWeightDelegate GetWeight { get; private set; }
        public GetEdgesDelegate GetEdges { get; private set; }

        public IReadOnlyDictionary<N, DataNode> Nodes => m_nodes;
        public IReadOnlyDictionary<E, DataNode> Edges => m_edges;

        #region [  CONSTRUCTORS  ]

        public DataGraph(GetEdgesDelegate getEdgesCallback,
                         GetNodeDelegate getDestNodeCallback) : this(getEdgesCallback, null, getDestNodeCallback, null, null)
        {

        }

        public DataGraph(GetEdgesDelegate getEdgesCallback,
                         GetNodeDelegate getSourceNodeCallback,
                         GetNodeDelegate getDestNodeCallback,
                         IsValid validationCallback,
                         GetWeightDelegate getWeightCallback)
        {
            m_roots = new List<DataNode>();
            m_nodes = new Dictionary<N, DataNode>();
            m_edges = new Dictionary<E, DataNode>();
            GetEdges = getEdgesCallback;
            GetSource = getSourceNodeCallback ?? (c => m_edges[c].Data);
            GetDest = getDestNodeCallback;
            Validation = validationCallback ?? ((N, C) => true);
            GetWeight = getWeightCallback ?? (c => 1);
        }

        public DataGraph(GetEdgesDelegate getEdgesCallback,
                         GetNodeDelegate getSourceNodeCallback,
                         GetNodeDelegate getDestNodeCallback,
                         IsValid validationCallback) : this(getEdgesCallback, getSourceNodeCallback, getDestNodeCallback, validationCallback, null)
        {
            
        }

        public DataGraph(GetEdgesDelegate getEdgesCallback,
                         GetNodeDelegate getSourceNodeCallback,
                         GetNodeDelegate getDestNodeCallback,
                         GetWeightDelegate getWeightCallback) : this(getEdgesCallback, getSourceNodeCallback, getDestNodeCallback, null, getWeightCallback)
        {

        }

        public DataGraph(GetEdgesDelegate getEdgesCallback,
                         GetNodeDelegate getSourceNodeCallback,
                         GetNodeDelegate getDestNodeCallback) : this(getEdgesCallback, getSourceNodeCallback, getDestNodeCallback, null, null)
        {

        }

        public DataGraph(GetEdgesDelegate getEdgesCallback,
                         GetNodeDelegate getDestNodeCallback,
                         IsValid validationCallback) : this(getEdgesCallback, null, getDestNodeCallback, validationCallback, null)
        {

        }

        public DataGraph(GetEdgesDelegate getEdgesCallback,
                         GetNodeDelegate getDestNodeCallback,
                         GetWeightDelegate getWeightCallback) : this(getEdgesCallback, null, getDestNodeCallback, null, getWeightCallback)
        {

        }

        #endregion

        #region [  ALGORITHMS  ]

        private static int MinimumDistance(float[] distance, bool[] shortestPathTreeSet, int verticesCount)
        {
            float min = float.MaxValue;
            int minIndex = 0;

            for (int v = 0; v < verticesCount; ++v)
            {
                if (!shortestPathTreeSet[v] && distance[v] <= min)
                {
                    min = distance[v];
                    minIndex = v;
                }
            }

            return minIndex;
        }

        private static void DijkstraAlgorithm(float[,] graph, int source, int verticesCount)
        {
            float[] distance = new float[verticesCount];
            bool[] shortestPathTreeSet = new bool[verticesCount];

            for (int i = 0; i < verticesCount; ++i)
            {
                distance[i] = float.MaxValue;
                shortestPathTreeSet[i] = false;
            }

            distance[source] = 0;

            for (int count = 0; count < verticesCount - 1; ++count)
            {
                int u = MinimumDistance(distance, shortestPathTreeSet, verticesCount);
                shortestPathTreeSet[u] = true;

                for (int v = 0; v < verticesCount; ++v)
                {
                    if (!shortestPathTreeSet[v] && graph[u, v] != 0 && distance[u] != float.MaxValue && distance[u] + graph[u, v] < distance[v])
                    {
                        distance[v] = distance[u] + graph[u, v];
                    }
                }
            }
        }

        public void BuildDijkstraShortestPath(N source)
        {
            if (!m_nodes.TryGetValue(source, out DataNode s))
            {
                return;
            }

            if (s.ShortestPaths != null && s.ShortestPaths.All(p => p != null))
            {
                return;
            }

            int verticesCount = m_nodes.Count;
            Path[] paths = new Path[verticesCount];
            float[] distance = new float[verticesCount];
            bool[] shortestPathTreeSet = new bool[verticesCount];

            for (int i = 0; i < verticesCount; ++i)
            {
                paths[i] = new Path();
                distance[i] = float.MaxValue;
                shortestPathTreeSet[i] = false;
            }

            distance[s.Index] = 0;

            for (int count = 0; count < verticesCount - 1; ++count)
            {
                int u = MinimumDistance(distance, shortestPathTreeSet, verticesCount);
                shortestPathTreeSet[u] = true;

                for (int v = 0; v < verticesCount; ++v)
                {
                    if (!shortestPathTreeSet[v] && m_weightGraph[u, v] != 0 && distance[u] != float.MaxValue && distance[u] + m_weightGraph[u, v] < distance[v])
                    {
                        distance[v] = distance[u] + m_weightGraph[u, v];
                        // TODO: Finish this part of the algorithm
                    }
                }
            }
        }

        public bool InvalidatePath(N source, N dest)
        {
            if (m_nodes.TryGetValue(source, out DataNode s) && m_nodes.TryGetValue(dest, out DataNode d) && s.ShortestPaths != null)
            {
                s.ShortestPaths[d.Index] = null;
                return true;
            }
            return false;
        }

        private void InvalidatePath(DataNode s, DataNode d)
        {
            if (s.ShortestPaths != null)
            {
                s.ShortestPaths[d.Index] = null;
            }
        }

        public void UpdateWeights()
        {
            bool shouldUpdateMatrix = false;
            foreach(var node in m_nodes)
            {
                for (int i = 0; i < node.Value.Edges.Count; i++)
                {
                    var edge = node.Value.Edges[i];
                    var newWeight = GetWeight(edge.connection);
                    if(edge.weight != newWeight)
                    {
                        shouldUpdateMatrix = true;
                        edge.weight = newWeight;
                        node.Value.Edges[i] = edge;

                        if(node.Value.NextNodes[i] != null)
                        {
                            InvalidatePath(node.Value, node.Value.NextNodes[i]);
                        }
                    }
                }
            }

            if (shouldUpdateMatrix)
            {
                UpdateWeightMatrix();
            }
        }

        public Path ShortestPath(N source, N dest)
        {
            if(source == null || dest == null || !m_nodes.TryGetValue(source, out DataNode s) || !m_nodes.TryGetValue(dest, out DataNode d))
            {
                return null;
            }

            if(s.ShortestPaths != null && s.ShortestPaths[d.Index] != null)
            {
                return s.ShortestPaths[d.Index];
            }

            Profiler.BeginSample("DataGraph::ShortestPath");

            var path = ShortestPath(s, d).link;

            Profiler.EndSample();

            return path;
            //var (link, totalWeight) = ShortestPath(s, d);
            //if(link == null)
            //{
            //    return null;
            //}

            //List<Link> links = new List<Link>() { link };

            //if(link.NextLink == null) { return null; }

            //while(link.NextLink != null)
            //{
            //    link = link.NextLink;
            //    links.Add(link);
            //}

            //Path path = new Path(links);
            //if(s.ShortestPaths == null)
            //{
            //    s.ShortestPaths = new Path[m_nodes.Count];
            //}
            //s.ShortestPaths[d.Index] = path;
            //return path;
        }

        private (Link link, float weight) ShortestPath(DataNode a, DataNode b) => ShortestPath(a, b, new HashSet<DataNode>());

        private (Link link, float weight) ShortestPath(DataNode a, DataNode b, HashSet<DataNode> openLoops)
        {
            Link link = null;
            if (a.ShortestPaths == null)
            {
                a.ShortestPaths = new Path[m_nodes.Count];
            }
            else if(a.ShortestPaths[b.Index] != null)
            {
                var path = a.ShortestPaths[b.Index];
                return (path, path.TotalWeight);
            }

            link = new Link(a.Data);
            float weight = float.MaxValue;
            openLoops.Add(a);
            for (int i = 0; i < a.NextNodes.Count; i++)
            {
                var nextNode = a.NextNodes[i];
                if(nextNode == a)
                {
                    continue;
                }
                if (nextNode == b && a.Edges[i].weight < weight)
                {
                    link.Edge = a.Edges[i];
                    link.NextLink = new Link(b.Data);
                    weight = a.Edges[i].weight;
                }
                else if (nextNode != null && !openLoops.Contains(nextNode))
                {
                    var (newLink, totalWeight) = ShortestPath(nextNode, b, openLoops);
                    if (totalWeight + a.Edges[i].weight < weight)
                    {
                        link.Edge = a.Edges[i];
                        link.NextLink = newLink;
                        weight = a.Edges[i].weight + totalWeight;
                    }
                }
            }
            openLoops.Remove(a);

            a.ShortestPaths[b.Index] = link;
            return (link, weight);
        }

        public void BuildFullShortestPath()
        {
            // TODO: IMPLEMENT THE FULL SHORTEST PATH TO BE USED LATER
        }

        #endregion

        #region [  NODES RETRIEVAL  ]

        private DataNode GetDataNode(N node) => node != null ? m_nodes[node] : null;
        private DataNode GetNodeA(E connection) => GetDataNode(GetSource(connection));
        private DataNode GetNodeB(E connection) => GetDataNode(GetDest(connection));

        #endregion


        #region [  BUILD UP  ]

        public DataGraph<N, E> Build(params N[] rootNodes)
        {
            m_nodes.Clear();
            m_edges.Clear();

            Profiler.BeginSample("DataGraph::Build");

            m_progressiveIndex = 0;
            m_roots = rootNodes.Select(r => new DataNode(r, m_progressiveIndex++)).ToList();

            foreach (var root in m_roots)
            {
                if (root.Data != null && !m_nodes.ContainsKey(root.Data))
                {
                    m_nodes[root.Data] = root;
                    BuildRecursive(root);
                }
            }

            m_edgesGraph = new E[m_nodes.Count, m_nodes.Count];
            m_weightGraph = new float[m_nodes.Count, m_nodes.Count];

            // Build the weight and connection matrices
            UpdateWeightMatrix();

            Profiler.EndSample();

            return this;
        }

        private void UpdateWeightMatrix()
        {
            Profiler.BeginSample("DataPath::UpdateWeightMatrix");

            foreach (var nodeA in m_nodes.Values)
            {
                foreach (var e in nodeA.Edges)
                {
                    var nodeB = GetNodeB(e.connection);
                    if (nodeB != null)
                    {
                        var weight = e.weight;
                        if (m_edgesGraph[nodeA.Index, nodeB.Index] == null || weight < m_weightGraph[nodeA.Index, nodeB.Index])
                        {
                            m_edgesGraph[nodeA.Index, nodeB.Index] = e.connection;
                            m_weightGraph[nodeA.Index, nodeB.Index] = weight;
                        }
                    }
                }
            }

            Profiler.EndSample();
        }

        public void PrintWeightMatrix(Action<string> printer)
        {
            StringBuilder sb = new StringBuilder();
            var orderedNodes = m_nodes.Values.OrderBy(n => n.Index).Select(n => n.Data).ToArray();
            sb.Append("   ");
            for (int i = 0; i < orderedNodes.Length; i++)
            {
                sb.Append(orderedNodes[i]).Append("  ");
            }
            sb.AppendLine();
            for (int i = 0; i < m_weightGraph.GetLength(0); i++)
            {
                sb.Append(orderedNodes[i]).Append("  ");
                for (int j = 0; j < m_weightGraph.GetLength(1); j++)
                {
                    sb.Append(m_weightGraph[i, j]).Append("  ");
                }
                sb.AppendLine();
            }
            printer(sb.ToString());
        }

        private DataNode BuildRecursive(DataNode node)
        {
            foreach(var c in GetEdges(node.Data))
            {
                m_edges[c] = node;
                node.Edges.Add((c, GetWeight(c)));
                N nextData = GetDest(c);
                if(nextData != null)
                {
                    if (!m_nodes.TryGetValue(nextData, out DataNode nextNode))
                    {
                        nextNode = new DataNode(nextData, m_nodes.Count);
                        m_nodes[nextData] = nextNode;
                        nextNode = BuildRecursive(nextNode);
                    }
                    node.NextNodes.Add(nextNode);
                }
            }
            return node;
        }

        #endregion


        public class DataNode
        {
            public int Index { get; private set; }
            public N Data { get; private set; }
            public List<DataNode> NextNodes { get; private set; }
            public List<(E connection, float weight)> Edges { get; private set; }
            public Path[] ShortestPaths { get; set; }

            public DataNode(N data, int index)
            {
                Index = index;
                Data = data;
                Edges = new List<(E, float weight)>();
                NextNodes = new List<DataNode>();
            }

            public override string ToString()
            {
                return $"{Index} --[ {Edges.Count} ]--> {NextNodes.Count}";
            }
        }

        public class Link
        {
            public N Data { get; private set; }
            public (E edge, float weight) Edge { get; internal set; }
            public Link NextLink { get; set; }
            public float TotalWeight => Edge.weight + NextLink?.TotalWeight ?? 0;

            public Link(N from)
            {
                Data = from;
            }

            public Link(DataNode from, DataNode to)
            {
                Data = from.Data;
                Edge = from.Edges[from.NextNodes.IndexOf(to)];
            }
        }

        public class Path
        {
            private List<Link> m_links;
            private HashSet<N> m_nodes;
            private float m_totalWeight;
            private bool m_sealed;
            private DataNode m_lastNode;

            public N Source => m_links[0].Data;
            public N Destination => m_links[m_links.Count - 1].Data;
            public IReadOnlyList<Link> Links => m_links;

            public IEnumerable<N> NodesOnly => Links.Select(d => d.Data);
            public float TotalWeight => m_totalWeight;

            public Path()
            {
                m_links = new List<Link>();
                m_nodes = new HashSet<N>();
                m_totalWeight = 0;
                m_sealed = false;
            }

            public Path(IEnumerable<Link> links) : this(new List<Link>(links))
            {
                
            }

            public Path(List<Link> links)
            {
                m_links = links;
                m_totalWeight = links.Sum(l => l.Edge.weight);
                m_sealed = true;
            }

            public void Seal() => m_sealed = true;

            public void AddNode(DataNode node)
            {
                if (m_sealed) { return; }
                if(m_lastNode == null)
                {
                    m_lastNode = node;
                }
                else if(!m_nodes.Contains(node.Data))
                {
                    var link = new Link(m_lastNode, node);
                    m_nodes.Add(node.Data);
                    m_links.Add(link);
                    m_totalWeight += link.Edge.weight;
                }
            }

            public List<T> Convert<T>(Func<N, T> convertNode, Func<E, T> convertEdge)
            {
                List<T> list = new List<T>();
                foreach (var link in Links)
                {
                    if(convertNode != null)
                    {
                        var d = convertNode(link.Data);
                        if(d != null)
                        {
                            list.Add(d);
                        }
                    }
                    if(convertEdge != null)
                    {
                        var e = convertEdge(link.Edge.edge);
                        if(e != null)
                        {
                            list.Add(e);
                        }
                    }
                }
                return list;
            }

            public List<T> Convert<T>()
            {
                List<T> list = new List<T>();
                foreach (var link in Links)
                {
                    if (link.Data is T d && d != null)
                    {
                        list.Add(d);
                    }
                    if (link.Edge.edge is T e && e != null)
                    {
                        list.Add(e);
                    }
                }
                return list;
            }

            public static implicit operator Link(Path path)
            {
                return path.Links.FirstOrDefault();
            }

            public static implicit operator Path(Link link)
            {
                if(link == null) { return null; }
                List<Link> links = new List<Link>() { link };

                if (link.NextLink == null) { return null; }

                while (link.NextLink != null)
                {
                    link = link.NextLink;
                    links.Add(link);
                }

                return new Path(links);
            }
        }
    }

}