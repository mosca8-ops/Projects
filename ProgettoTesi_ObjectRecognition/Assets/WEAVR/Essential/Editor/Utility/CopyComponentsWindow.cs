using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{

    public class CopyComponentsWindow : EditorWindow
    {
        const float k_IndentPixels = 15;
        const float k_LinePickupDistance = 4;
        const float k_PortPickupDistance = 7;
        static readonly Color k_HoverGOColor = new Color(0.9f, 0.1f, 0.1f, 0.25f);

        const float k_NameWeight = 0.5f;
        const float k_ComponentWeight = 0.05f;
        const float k_ChildrenWeight = 0.45f;
        const float k_ThresholdWeight = 0.2f;

        [MenuItem("WEAVR/Utilities/Copy Components", priority = 5)]
        private static void ShowWindow()
        {
            GetWindow<CopyComponentsWindow>().Show();
        }

        private struct Options
        {
            public OptionalInt maxLevel;
            public bool createChildren;
            public bool showHidden;

            public bool showWeights;
            public float nameWeight;
            public float componentWeight;
            public float hierarchyWeight;
        }

        private class Link
        {
            public GameObject to;
            public float score;
        }

        private GameObject m_from;
        private GameObject m_to;

        private Options m_options;

        private Vector2 m_scrollPos;

        private Type[] m_typesBlackList = { typeof(Renderer), typeof(MeshFilter), typeof(UniqueID) };
        private Type[] m_reflectionCopy = { typeof(AudioSource), typeof(Collider), typeof(Rigidbody), typeof(Animator), typeof(Camera) };

        private List<Action> m_postCopyActions = new List<Action>();
        private List<(Component c, string arrayPath)> m_arraysToTest = new List<(Component c, string arrayPath)>();
        private Dictionary<GameObject, Link> m_links = new Dictionary<GameObject, Link>();
        private Dictionary<(GameObject from, GameObject to), float> m_scores = new Dictionary<(GameObject from, GameObject to), float>();
        private Dictionary<GameObject, Rect> m_fromRects = new Dictionary<GameObject, Rect>();
        private Dictionary<GameObject, Rect> m_toRects = new Dictionary<GameObject, Rect>();
        private Dictionary<GameObject, List<(GameObject obj, float depth)>> m_newObjectsLists = new Dictionary<GameObject, List<(GameObject obj, float depth)>>();
        private Dictionary<GameObject, GameObject> m_newObjects = new Dictionary<GameObject, GameObject>();
        private HashSet<Transform> m_applyTransforms = new HashSet<Transform>();

        private GUIStyle m_iconStyle;
        private GUIStyle m_labelStyle;
        private Color m_newObjColor;

        private struct ActiveLink
        {
            public Color color;
            public GameObject active;
            public GameObject hoverLink;
            public GameObject clickedLink;
        }

        private ActiveLink m_activeLink;

        private void OnEnable()
        {
            minSize = new Vector2(800, 600);
            titleContent = new GUIContent("Copy Components");

            m_options.maxLevel = 4;
            m_options.maxLevel.enabled = false;
            m_options.showHidden = true;

            m_options.showWeights = false;
            m_options.nameWeight = k_NameWeight;
            m_options.componentWeight = k_ComponentWeight;
            m_options.hierarchyWeight = k_ChildrenWeight;
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (m_iconStyle == null)
            {
                m_iconStyle = new GUIStyle()
                {
                    margin = new RectOffset(0, 0, 0, 0),
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                };
                m_labelStyle = new GUIStyle(EditorStyles.label);

                if (m_from && m_to)
                {
                    m_links.Clear();
                    MakeLink(m_from, m_to);
                    MakeAutomaticLinks(m_from.transform, m_to.transform);
                }
            }

            m_activeLink.color = EditorGUIUtility.isProSkin ? Color.cyan : Color.grey;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("Options", EditorStyles.boldLabel);
            m_options.showHidden = EditorGUILayout.Toggle("Show Hidden Objects", m_options.showHidden);
            m_options.createChildren = EditorGUILayout.Toggle("Create Children", m_options.createChildren);
            m_options.maxLevel.enabled = EditorGUILayout.Toggle("Limit Depth", m_options.maxLevel.enabled);
            if (m_options.maxLevel.enabled)
            {
                m_options.maxLevel.value = EditorGUILayout.IntField("Max Depth", m_options.maxLevel.value);
            }

            m_options.showWeights = EditorGUILayout.Foldout(m_options.showWeights, "Automatic Linking Weights", true);
            if (m_options.showWeights)
            {
                DrawWeights();
            }

            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical("Box", GUILayout.Width(600));
            GUILayout.Label("Usage Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
@"This tool manages automatic copying of components between two hierarchies of objects.
The tool will attempt to find similarities in the hierarchies and link them automatically. Use 'Reset Links' to reapply automatic linking.
Any link can be deleted by clicking on it. A new link can be created by dragging an edge from the left-hand ports and dropping it onto the right-hand ones.
To rename the linked objects use the 'Rename' button, this is useful to keep animations consistent.
'Copy Self' button will copy only the roots of the hierarchies. 'Copy  Linked' will copy all the linked objects instead."
, MessageType.None);

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Layout)
            {
                m_fromRects.Clear();
                m_toRects.Clear();
            }

            if (m_options.createChildren)
            {
                m_newObjColor = EditorGUIUtility.isProSkin ? Color.green : Color.green;
            }
            
            EditorGUILayout.BeginHorizontal("GroupBox", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Left Part
            EditorGUILayout.BeginVertical("GroupBox", GUILayout.ExpandWidth(true));
            var from = EditorGUILayout.ObjectField("From Object", m_from, typeof(GameObject), true) as GameObject;
            if (from != m_from)
            {
                m_from = from;
                m_links.Clear();
                if (m_from && m_to)
                {
                    MakeLinks(m_from, m_to, true, true);
                    //MakeLink(m_from, m_to);
                    //MakeAutomaticLinks(m_from.transform, m_to.transform);
                }
            }
            if (m_from)
            {
                DrawTree(m_from.transform, 0, 0, true);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(70);

            // Right Part
            EditorGUILayout.BeginVertical("GroupBox", GUILayout.ExpandWidth(true), GUILayout.MinWidth(340));
            var to = EditorGUILayout.ObjectField("To Object", m_to, typeof(GameObject), true) as GameObject;
            if (m_to != to)
            {
                m_to = to;
                m_links.Clear();
                if (m_from && m_to)
                {
                    MakeLinks(m_from, m_to, true, true);
                    //MakeLink(m_from, m_to);
                    //MakeAutomaticLinks(m_from.transform, m_to.transform);
                }
            }
            if (m_to)
            {
                if (m_to.gameObject.scene.isLoaded)
                {
                    DrawTree(m_to.transform, 0, 0, false);
                }
                else
                {
                    m_to = null;
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            DrawLinks();

            if (Event.current.type == EventType.Repaint
                && m_activeLink.hoverLink
                && m_fromRects.TryGetValue(m_activeLink.hoverLink, out Rect fromRect)
                && m_links.TryGetValue(m_activeLink.hoverLink, out Link link)
                && m_toRects.TryGetValue(link.to, out Rect toRect))
            {
                EditorGUI.DrawRect(GUIUtility.ScreenToGUIRect(fromRect), k_HoverGOColor);
                EditorGUI.DrawRect(GUIUtility.ScreenToGUIRect(toRect), k_HoverGOColor);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal("Box");
            var scoreRect = GUILayoutUtility.GetRect(120, 16);
            if (Event.current.type == EventType.Repaint && m_activeLink.hoverLink && m_links.TryGetValue(m_activeLink.hoverLink, out Link hoveredLink))
            {
                GUI.Label(scoreRect, $"Similarity: {hoveredLink.score}");
            }
            GUILayout.FlexibleSpace();
            if (m_from && m_to && GUILayout.Button("Reset Links"))
            {
                m_links.Clear();
                MakeLinks(m_from, m_to, true, true);
                //MakeLink(m_from, m_to);
                //MakeAutomaticLinks(m_from.transform, m_to.transform);
            }
            bool wasEnabled = GUI.enabled;
            GUI.enabled = m_links.Count > 0;
            if (GUILayout.Button("Rename"))
            {
                foreach (var pair in m_links)
                {
                    pair.Value.to.name = pair.Key.name;
                }
            }
            GUI.enabled = wasEnabled;
            GUILayout.Space(20);
            if (GUILayout.Button("Copy Self"))
            {
                CopyGameObject(m_from, m_to, m_from.transform, m_to.transform);
                ClearEmptyObjects();
            }
            GUI.enabled = m_links.Count > 0;
            if (GUILayout.Button("Copy Linked"))
            {
                CopyLinked(m_from.transform, m_to.transform);
                ClearEmptyObjects();
            }
            if (GUILayout.Button("Add New Objects"))
            {
                CopyLinked(m_from.transform, m_to.transform);
                ClearEmptyObjects();
            }
            GUI.enabled = wasEnabled;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWeights()
        {
            float totalWeight = m_options.nameWeight + m_options.componentWeight + m_options.hierarchyWeight;

            float tempWeight = EditorGUILayout.FloatField("Name", m_options.nameWeight);
            if (tempWeight != m_options.nameWeight)
            {
                float localWeight = m_options.componentWeight + m_options.hierarchyWeight + tempWeight;
                m_options.nameWeight = tempWeight;
                //if (localWeight != 0)
                //{
                //    m_options.componentWeight /= localWeight;
                //    m_options.hierarchyWeight /= localWeight;
                //}
            }

            tempWeight = EditorGUILayout.FloatField("Components", m_options.componentWeight);
            if (tempWeight != m_options.componentWeight)
            {
                float localWeight = 1 - tempWeight;
                m_options.componentWeight = tempWeight;
                //if (localWeight != 0)
                //{
                //    m_options.nameWeight /= localWeight;
                //    m_options.hierarchyWeight /= localWeight;
                //}
            }

            tempWeight = EditorGUILayout.FloatField("Hierarchy", m_options.hierarchyWeight);
            if (tempWeight != m_options.hierarchyWeight)
            {
                float localWeight = 1 - tempWeight;
                m_options.hierarchyWeight = tempWeight;
                //if (localWeight != 0)
                //{
                //    m_options.componentWeight /= localWeight;
                //    m_options.nameWeight /= localWeight;
                //}
            }
        }

        private void DrawLinks()
        {
            var mousePos = EditorGUIUtility.ScreenToGUIPoint(GUIUtility.GUIToScreenPoint(Event.current.mousePosition));
            var e = Event.current;

            var color = Handles.color;
            Handles.color = m_activeLink.color;
            foreach (var rect in m_fromRects)
            {
                var gr = GUIUtility.ScreenToGUIRect(rect.Value);
                var center = new Vector3(gr.xMax + 10, gr.y + gr.height * 0.5f, 0);
                if (rect.Key == m_activeLink.active || (!m_activeLink.active && Vector2.Distance(center, mousePos) < k_PortPickupDistance))
                {
                    Handles.color = Color.green;
                    Handles.CircleHandleCap(0, center, Quaternion.identity, 4, EventType.Repaint);
                    Handles.color = m_activeLink.color;

                    if (e.type == EventType.MouseDrag)
                    {
                        m_activeLink.active = rect.Key;
                    }
                }
                else
                {
                    Handles.CircleHandleCap(0, center, Quaternion.identity, 3, EventType.Repaint);
                }
            }

            GameObject hoveringGameObject = null;
            foreach (var rect in m_toRects)
            {
                var gr = GUIUtility.ScreenToGUIRect(rect.Value);
                var center = new Vector3(gr.xMin - 10, gr.y + gr.height * 0.5f, 0);

                if (m_activeLink.active && Vector2.Distance(center, mousePos) < k_PortPickupDistance)
                {
                    Handles.color = Color.green;
                    Handles.CircleHandleCap(0, center, Quaternion.identity, 4, EventType.Repaint);
                    Handles.color = m_activeLink.color;

                    hoveringGameObject = rect.Key;
                }
                else
                {
                    Handles.CircleHandleCap(0, center, Quaternion.identity, 3, EventType.Repaint);
                }
            }
            Handles.color = color;

            if (m_activeLink.active && e.type == EventType.MouseUp)
            {
                if (hoveringGameObject)
                {
                    //foreach (var key in m_links.Where(k => k.Value.to == hoveringGameObject).Select(k => k.Key).ToArray())
                    //{
                    //    m_links.Remove(key);
                    //}

                    if (hoveringGameObject.transform.childCount > 0 && m_activeLink.active.transform.childCount > 0 && EditorUtility.DisplayDialog("Link Children", "Link the children as well?", "Yes", "No"))
                    {
                        ClearHierarchyLinksFromDestination(hoveringGameObject);
                        MakeLinks(m_activeLink.active, hoveringGameObject, false, true);
                        //MakeAutomaticLinks(m_activeLink.active.transform, hoveringGameObject.transform, false);
                    }
                    else
                    {
                        MakeLink(m_activeLink.active, hoveringGameObject);
                    }
                }
                else
                {
                    m_links.Remove(m_activeLink.active);
                }

                m_activeLink.active = null;
            }

            GameObject linkToRemove = null;

            m_activeLink.hoverLink = null;

            foreach (var link in m_links)
            {
                if (!m_options.showHidden && link.Key.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                {
                    continue;
                }

                if (m_fromRects.TryGetValue(link.Key, out Rect fromRect) && m_toRects.TryGetValue(link.Value.to, out Rect toRect))
                {
                    fromRect = GUIUtility.ScreenToGUIRect(fromRect);
                    toRect = GUIUtility.ScreenToGUIRect(toRect);

                    var fromPoint = new Vector3(fromRect.xMax + 10, fromRect.y + fromRect.height * 0.5f, 0);
                    var toPoint = new Vector3(toRect.xMin - 10, toRect.y + toRect.height * 0.5f, 0);
                    var fromTangent = new Vector3(fromPoint.x + 10, fromPoint.y, 0);
                    var toTangent = new Vector3(toPoint.x - 10, toPoint.y, 0);

                    Handles.CircleHandleCap(0, fromPoint, Quaternion.identity, 2, EventType.Repaint);
                    Handles.CircleHandleCap(0, toPoint, Quaternion.identity, 2, EventType.Repaint);

                    bool isCloseToLine = false;

                    if (!m_activeLink.active)
                    {
                        var distanceToLine = HandleUtility.DistancePointBezier(mousePos, fromPoint, toPoint, fromTangent, toTangent);
                        isCloseToLine = !m_activeLink.hoverLink && distanceToLine < k_LinePickupDistance;
                    }

                    Handles.DrawBezier(fromPoint,
                                       toPoint,
                                       fromTangent,
                                       toTangent,
                                       isCloseToLine ? Color.red : m_activeLink.color,
                                       null,
                                       isCloseToLine ? 4 : 3);

                    if (isCloseToLine)
                    {
                        m_activeLink.hoverLink = link.Key;
                        if(e.type == EventType.MouseDown)
                        {
                            m_activeLink.clickedLink = m_activeLink.hoverLink;
                        }
                        else if (m_activeLink.clickedLink == m_activeLink.hoverLink && e.type == EventType.MouseUp)
                        {
                            linkToRemove = link.Key;
                            m_activeLink.clickedLink = null;
                        }
                    }
                }
            }

            if (linkToRemove && !hoveringGameObject)
            {
                if (linkToRemove.transform.childCount > 0 && EditorUtility.DisplayDialog("Delete Children", "Delete links from children as well?", "Yes", "No"))
                {
                    ClearHierarchyLinks(linkToRemove);
                }
                m_links.Remove(linkToRemove);
            }

            if (m_activeLink.active && m_fromRects.TryGetValue(m_activeLink.active, out Rect activeRect))
            {
                activeRect = GUIUtility.ScreenToGUIRect(activeRect);
                var activePoint = new Vector3(activeRect.xMax + 10, activeRect.y + activeRect.height * 0.5f, 0);
                Handles.DrawBezier(new Vector3(activeRect.xMax + 10, activeRect.y + activeRect.height * 0.5f, 0),
                                   mousePos,
                                   new Vector3(activePoint.x + 10, activePoint.y, 0),
                                   new Vector3(mousePos.x - 10, mousePos.y, 0),
                                   Color.green,
                                   null,
                                   4);
            }
        }

        private void ClearHierarchyLinksFromDestination(GameObject root)
        {
            foreach (var key in m_links.Where(k => k.Value.to == root).Select(k => k.Key).ToArray())
            {
                m_links.Remove(key);
            }

            for (int i = 0; i < root.transform.childCount; i++)
            {
                ClearHierarchyLinksFromDestination(root.transform.GetChild(i).gameObject);
            }
        }

        private void ClearHierarchyLinks(GameObject root)
        {
            m_links.Remove(root);

            for (int i = 0; i < root.transform.childCount; i++)
            {
                ClearHierarchyLinks(root.transform.GetChild(i).gameObject);
            }
        }

        private void MakeLink(GameObject from, GameObject to, float score = 1)
        {
            foreach (var pair in m_links.Where(k => k.Value.to == to).ToArray())
            {
                m_scores.Remove((pair.Key, pair.Value.to));
                m_links.Remove(pair.Key);
            }

            m_links[from] = new Link()
            {
                to = to,
                score = Mathf.Clamp01(score)
            };

            m_scores[(from, to)] = score;
        }

        private void AddToList(List<(GameObject go, int depth)> list, Dictionary<GameObject, (int index, int depth)> indices, GameObject to)
        {
            list.Clear();
            indices.Clear();
            list.Add((to, 0));
            indices[to] = (0, 0);

            for (int i = 0; i < to.transform.childCount; i++)
            {
                indices[to.transform.GetChild(i).gameObject] = (list.Count, 1);
                list.Add((to.transform.GetChild(i).gameObject, 1));
                AddToListRecursive(list, indices, to.transform.GetChild(i), 2);
            }
        }

        private void AddToListRecursive(List<(GameObject go, int depth)> list, Dictionary<GameObject, (int index, int depth)> indices, Transform current, int depth)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                indices[current.GetChild(i).gameObject] = (list.Count, depth);
                list.Add((current.GetChild(i).gameObject, depth));
                AddToListRecursive(list, indices, current.GetChild(i), depth + 1);
            }
        }

        private void MakeLinks(GameObject nodeA, GameObject nodeB, bool considerExisting, bool recomputeNewObjects)
        {
            MakeLink(nodeA, nodeB);
            MakeAutomaticLinks(nodeA.transform, nodeB.transform, considerExisting);
            if (recomputeNewObjects)
            {
                ComputeRequiredNewObjects();
            }
        }

        private void ComputeRequiredNewObjects()
        {
            if (m_from && m_to)
            {
                m_newObjectsLists.Clear();
                ComputeRequiredNewObjects(m_from.transform, m_to.transform, 1);
            }
        }

        private void ComputeRequiredNewObjects(Transform node, Transform lastValidObject, int depth)
        {
            if(m_links.TryGetValue(node.gameObject, out Link link))
            {
                lastValidObject = link.to.transform;
            }
            for (int i = 0; i < node.childCount; i++)
            {
                var child_i = node.GetChild(i);
                if (!m_links.ContainsKey(child_i.gameObject) && !GetChildrenInLevels(lastValidObject, 2).Any(c => Similarity.ComputeSimilarity(child_i, c, 0.75f, 0.05f, 0.2f) > 0.95f))
                {
                    if (!m_newObjectsLists.TryGetValue(lastValidObject.gameObject, out List<(GameObject obj, float depth)> list))
                    {
                        list = new List<(GameObject obj, float depth)>();
                        m_newObjectsLists[lastValidObject.gameObject] = list;
                    }
                    list.Add((child_i.gameObject, depth));
                    m_newObjects[child_i.gameObject] = lastValidObject.gameObject;
                }
                ComputeRequiredNewObjects(child_i, lastValidObject, depth + 1);
            }
        }

        private void MakeAutomaticLinks(Transform rootA, Transform rootB, bool considerExisting = true)
        {
            for (int i = 0; i < rootA.childCount; i++)
            {
                var from = rootA.GetChild(i);
                if (TryGetMostSimilarObject(from, GetChildrenInLevels(rootB, 2), out (Transform target, float score) mostSimilar, true))
                {
                    if (considerExisting)
                    {
                        var existingLinkPair = m_links.FirstOrDefault(l => l.Value.to == mostSimilar.target.gameObject);
                        MakeLink(from.gameObject, mostSimilar.target.gameObject, mostSimilar.score);
                        MakeAutomaticLinks(from, mostSimilar.target, considerExisting);
                        if(existingLinkPair.Key && TryGetMostSimilarObject(existingLinkPair.Key.transform, GetChildrenInLevels(rootB, 2), out (Transform target, float score) sMostSimilar, considerExisting))
                        {
                            MakeLink(existingLinkPair.Key, sMostSimilar.target.gameObject, sMostSimilar.score);
                            MakeAutomaticLinks(existingLinkPair.Key.transform, sMostSimilar.target, considerExisting);
                        }
                    }
                    else
                    {
                        MakeLink(from.gameObject, mostSimilar.target.gameObject, mostSimilar.score);
                        MakeAutomaticLinks(from, mostSimilar.target, considerExisting);
                    }
                }
            }
        }

        private bool TryGetMostSimilarObject(Transform source, IEnumerable<Transform> list, out (Transform target, float score) mostSimilar, bool considerExisting)
        {
            var similarityScores = list.Select(target => (target, ComputeSimilarity(source, target))).OrderByDescending(p => p.Item2).ToArray();

            mostSimilar = default;

            if (similarityScores.Length > 0)
            {
                if (considerExisting)
                {
                    for (int i = 0; i < similarityScores.Length; i++)
                    {
                        var similarity = similarityScores[i];
                        var targetGO = similarity.target.gameObject;
                        var existingLink = m_links.Values.FirstOrDefault(l => l.to == targetGO);
                        if (existingLink == null || existingLink.score < similarity.Item2)
                        {
                            mostSimilar = (similarity.target, similarity.Item2);
                            break;
                        }
                    }
                }
                else
                {
                    mostSimilar = (similarityScores[0].target, similarityScores[0].Item2);
                }
            }

            return mostSimilar.target && mostSimilar.target.childCount > 0 ? mostSimilar.score > k_ThresholdWeight : mostSimilar.score > m_options.nameWeight;
        }

        private float ComputeSimilarity(Transform source, Transform dest)
        {
            var normalSimilarity = Similarity.ComputeSimilarity(source, dest, m_options.nameWeight, m_options.componentWeight, m_options.hierarchyWeight);

            if(!source.parent && !dest.parent) { return normalSimilarity; }

            List<Transform> parentsChainA = new List<Transform>();
            List<Transform> parentsChainB = new List<Transform>();

            var parent = source.parent;
            while(parent && parent != m_from.transform)
            {
                parentsChainA.Add(parent);
                parent = parent.parent;
            }

            parent = dest.parent;
            while (parent && parent != m_to.transform)
            {
                parentsChainB.Add(parent);
                parent = parent.parent;
            }

            if(parentsChainA.Count == 0 && parentsChainB.Count == 0)
            {
                return normalSimilarity;
            }

            float factor = 1;
            float factorSum = 0;
            float parentsSimilarity = 0;
            for (int i = 0; i < parentsChainA.Count && i < parentsChainB.Count; i++)
            {
                parentsSimilarity += Similarity.ComputeNameSimilarity(parentsChainA[i], parentsChainB[i]) * factor;
                factorSum += factor;
                factor *= 0.5f;
            }

            if(parentsChainA.Count != parentsChainB.Count)
            {
                parentsSimilarity += (1f - Mathf.Abs(parentsChainB.Count - parentsChainA.Count) / Mathf.Max(parentsChainB.Count, parentsChainA.Count)) * 0.25f;
                factorSum += 0.25f;
            }

            if(factorSum > 0)
            {
                parentsSimilarity /= factorSum;
            }

            return normalSimilarity * 0.8f + parentsSimilarity * 0.2f;
        }

        private IEnumerable<Transform> GetChildrenInLevels(Transform t, int levels)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < t.childCount; i++)
            {
                children.Add(t.GetChild(i));
            }
            if (levels > 1)
            {
                var prevChildren = children.ToArray();
                foreach (var child in prevChildren)
                {
                    children.AddRange(GetChildrenInLevels(child, levels - 1));
                }
            }
            return children;
        }

        private void DrawTree(Transform node, float indent, int level, bool isFromSide, bool hidden = false, bool isNewObject = false)
        {
            DrawElement(node, indent, isFromSide, hidden, false);
            if (m_options.maxLevel.enabled && level >= m_options.maxLevel.value) { return; }

            for (int i = 0; i < node.childCount; i++)
            {
                var child_i = node.GetChild(i);
                if (m_options.showHidden || !child_i.gameObject.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                {
                    DrawTree(child_i, indent + k_IndentPixels, level + 1, isFromSide, hidden || child_i.gameObject.hideFlags.HasFlag(HideFlags.HideInHierarchy), false);
                }
            }

            if(!isFromSide && m_options.createChildren && m_newObjectsLists.TryGetValue(node.gameObject, out List<(GameObject obj, float depth)> newObjects))
            {
                foreach(var newObj in newObjects.ToArray())
                {
                    DrawElement(newObj.obj.transform, newObj.depth * k_IndentPixels, isFromSide, hidden || newObj.obj.hideFlags.HasFlag(HideFlags.HideInHierarchy), true);
                }
            }
        }

        private void DrawElement(Transform elem, float startPoint, bool isFromSide, bool hidden, bool isNewObject)
        {
            bool wasEnabled = GUI.enabled;

            EditorGUILayout.BeginHorizontal();

            if (isNewObject)
            {
                GUI.enabled = !hidden;
                var color = GUI.color;
                //GUI.color = Color.red;
                if(GUILayout.Button("X", EditorStyles.miniButton, GUILayout.Width(18), GUILayout.Height(14)))
                {
                    var listToRemoveFrom = m_newObjectsLists.Values.FirstOrDefault(p => p.Any(e => e.obj == elem.gameObject));
                    if(listToRemoveFrom != null)
                    {
                        m_newObjects.Remove(elem.gameObject);
                        listToRemoveFrom.RemoveAt(listToRemoveFrom.FindIndex(p => p.obj == elem.gameObject));
                    }
                }
                GUILayout.Space(startPoint - 22);
                GUI.color = m_newObjColor;
                GUILayout.Label(elem.gameObject.name);
                GUI.color = color;
            }
            else
            {
                GUI.enabled = !hidden;
                GUILayout.Space(startPoint);
                GUILayout.Label(elem.gameObject.name);
            }

            var renderer = elem.gameObject.GetComponent<Renderer>();
            if (renderer)
            {
                var r = GUILayoutUtility.GetRect(14, 14);
                r.x -= 4;
                r.y += 3;
                GUI.Label(r, new GUIContent(EditorGUIUtility.ObjectContent(renderer, renderer.GetType()).image), m_iconStyle);
                GUILayout.Space(6);
            }

            GUI.enabled = wasEnabled;

            GUILayout.FlexibleSpace();
            foreach (var cs in elem.GetComponents<Component>())
            {
                if (cs && !m_typesBlackList.Any(t => t.IsAssignableFrom(cs.GetType())))
                {
                    var icon = EditorGUIUtility.ObjectContent(cs, cs.GetType()).image;
                    if (icon)
                    {
                        var rect = GUILayoutUtility.GetRect(18, 16);
                        if (cs is Transform t && isFromSide)
                        {
                            var apply = m_applyTransforms.Contains(t);
                            if (apply != GUI.Toggle(new Rect(rect.x - 2, rect.y, rect.width + 2, rect.height), apply, GUIContent.none, EditorStyles.miniButton))
                            {
                                if (apply)
                                {
                                    m_applyTransforms.Remove(t);
                                }
                                else
                                {
                                    m_applyTransforms.Add(t);
                                }
                            }
                        }
                        GUI.Label(rect, new GUIContent(icon, cs.GetType().Name), m_iconStyle);
                        //GUI.DrawTexture(rect, icon);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            if (!isNewObject)
            {
                if (isFromSide)
                {
                    m_fromRects[elem.gameObject] = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                }
                else
                {
                    m_toRects[elem.gameObject] = GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect());
                }
            }
        }

        private void CopyLinked(Transform from, Transform to)
        {
            if(m_options.createChildren && m_newObjectsLists.Count > 0)
            {
                AddNewObjects();
            }
            foreach (var pair in m_links)
            {
                AddNeededComponents(pair.Key, pair.Value.to);
            }
            foreach (var pair in m_links)
            {
                CopyGameObject(pair.Key, pair.Value.to, from, to);
            }
        }

        private void AddNewObjects()
        {
            var toLinks = m_links.Values;
            var alreadyAdded = new HashSet<GameObject>();
            foreach (var pair in m_newObjectsLists)
            {
                if (!toLinks.Any(l => l.to == pair.Key))
                {
                    continue;
                }
                foreach (var sample in pair.Value)
                {
                    AddNewObjectsToHierarchy(pair.Key, sample.obj, alreadyAdded);
                }
            }
            m_newObjectsLists.Clear();
            m_newObjects.Clear();
        }

        private void AddNewObjectsToHierarchy(GameObject from, GameObject to, HashSet<GameObject> alreadyProcessed)
        {
            if (alreadyProcessed.Contains(to) || m_links.ContainsKey(to) || !m_newObjects.ContainsKey(to))
            {
                return;
            }
            alreadyProcessed.Add(to);

            var newGO = new GameObject(to.name, to.GetComponents<Component>().Where(c => c && !(c is Transform)).Select(c => c.GetType()).ToArray());
            newGO.SetActive(to.activeSelf);
            newGO.hideFlags = to.hideFlags;
            newGO.isStatic = to.isStatic;
            newGO.layer = to.layer;
            newGO.tag = to.tag;

            newGO.transform.localScale = to.transform.lossyScale;
            newGO.transform.SetPositionAndRotation(to.transform.position, to.transform.rotation);
            newGO.transform.SetParent(from.transform, true);

            MakeLink(to, newGO);

            for (int i = 0; i < to.transform.childCount; i++)
            {
                var child_i = to.transform.GetChild(i);
                AddNewObjectsToHierarchy(newGO, child_i.gameObject, alreadyProcessed);
            }
        }

        private void CopyGameObject(GameObject from, GameObject to, Transform fromRoot, Transform toRoot)
        {
            var fromComponents = from.GetComponents<Component>().Where(c => !m_typesBlackList.Any(b => b.IsAssignableFrom(c.GetType()))).ToList();
            foreach (var c in fromComponents)
            {
                if (c is Transform && !m_applyTransforms.Contains(from.transform))
                {
                    continue;
                }
                var toC = to.GetComponent(c.GetType());
                if (!toC)
                {
                    if (c is Transform) { continue; }
                    toC = to.AddComponent(c.GetType());
                }
                CopyComponent(c, toC, fromRoot, toRoot);
            }
            to.SetActive(from.activeSelf);
            to.hideFlags = from.hideFlags;
            to.isStatic = from.isStatic;
            to.layer = from.layer;
            to.tag = from.tag;
        }

        private void AddNeededComponents(GameObject from, GameObject to)
        {
            var fromComponents = from.GetComponents<Component>().Where(c => !m_typesBlackList.Any(b => b.IsAssignableFrom(c.GetType()))).ToList();
            foreach (var c in fromComponents)
            {
                if (c is Transform && !m_applyTransforms.Contains(from.transform))
                {
                    continue;
                }
                var toC = to.GetComponent(c.GetType());
                if (!toC)
                {
                    if (c is Transform) { continue; }
                    toC = to.AddComponent(c.GetType());
                }
            }
        }

        private void ClearEmptyObjects()
        {
            foreach (var pair in m_arraysToTest)
            {
                var so = new SerializedObject(pair.c);
                so.Update();
                var array = so.FindProperty(pair.arrayPath);
                for (int i = 0; i < array.arraySize; i++)
                {
                    var elem = array.GetArrayElementAtIndex(i);

                    if (elem.propertyType == SerializedPropertyType.ObjectReference && !elem.objectReferenceValue)
                    {
                        array.DeleteArrayElementAtIndex(i--);
                    }
                }
                so.ApplyModifiedProperties();
            }
        }

        private void CopyComponent(Component from, Component to, Transform fromRoot, Transform toRoot)
        {
            if (m_reflectionCopy.Any(t => t.IsAssignableFrom(from.GetType())))
            {
                foreach (var propertyInfo in from.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy))
                {
                    try
                    {
                        if (propertyInfo.CanWrite && propertyInfo.CanRead && !propertyInfo.IsSpecialName && propertyInfo.GetAttribute<ObsoleteAttribute>() == null && propertyInfo.Name != "name")
                        {
                            propertyInfo.SetValue(to, propertyInfo.GetValue(from));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Unable to copy property {propertyInfo.Name}");
                    }
                }
                foreach (var fieldInfo in from.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy))
                {
                    try
                    {
                        if (!fieldInfo.IsInitOnly && !fieldInfo.IsSpecialName && fieldInfo.GetAttribute<ObsoleteAttribute>() == null && fieldInfo.Name != "m_Name")
                        {
                            fieldInfo.SetValue(to, fieldInfo.GetValue(from));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Unable to copy field {fieldInfo.Name}");
                    }
                }
                return;
            }

            if (from is AudioSource s)
            {
                (to as AudioSource).clip = s.clip;
                (to as AudioSource).loop = s.loop;
                (to as AudioSource).playOnAwake = s.playOnAwake;
            }

            var fromSO = new SerializedObject(from);
            fromSO.Update();
            var toSO = new SerializedObject(to);
            toSO.Update();

            var fromProp = fromSO.FindProperty("m_Script");

            while (fromProp != null && fromProp.Next(fromProp.propertyType == SerializedPropertyType.Generic))
            {
                if (fromProp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var objValue = fromProp.objectReferenceValue;
                    GameObject go = objValue is GameObject g ? g : objValue is Component c ? c.gameObject : null;
                    if (go && go.transform.IsChildOf(fromRoot) && go.scene.isLoaded)
                    {
                        if (go == from.gameObject)
                        {
                            SetSceneObject(to.gameObject, toSO, fromProp, objValue);
                        }
                        else if (m_links.TryGetValue(go, out Link link))
                        {
                            SetSceneObject(link.to, toSO, fromProp, objValue);
                        }
                        else
                        {
                            // Post Action
                            string rootPath = fromRoot.gameObject.GetHierarchyPath();
                            string childPath = go.GetHierarchyPath();
                            string relChildPath = to.gameObject.GetHierarchyPath();
                            m_postCopyActions.Add(() =>
                            {
                                // Find in subtree

                            });
                        }
                    }
                    else
                    {
                        toSO.FindProperty(fromProp.propertyPath).objectReferenceValue = objValue;
                    }
                }
                else
                {
                    if (fromProp.isArray && fromProp.propertyType != SerializedPropertyType.String)
                    {
                        m_arraysToTest.Add((to, fromProp.propertyPath));
                        var toProp = toSO.FindProperty(fromProp.propertyPath);
                        if (toProp.arraySize > fromProp.arraySize)
                        {
                            for (int i = toProp.arraySize; i > fromProp.arraySize; i--)
                            {
                                toProp.DeleteArrayElementAtIndex(i);
                            }
                        }
                        else if (toProp.arraySize < fromProp.arraySize)
                        {
                            while (toProp.arraySize < fromProp.arraySize)
                            {
                                toProp.InsertArrayElementAtIndex(toProp.arraySize);
                            }
                        }
                        //toSO.FindProperty(fromProp.propertyPath).arraySize = fromProp.arraySize;
                    }
                    else
                    {
                        TryCopyValueFrom(toSO.FindProperty(fromProp.propertyPath), fromProp);
                    }
                }
            }

            toSO.ApplyModifiedProperties();
        }

        private static void SetSceneObject(GameObject target, SerializedObject toSO, SerializedProperty fromProp, UnityEngine.Object objValue)
        {
            try
            {
                if (objValue is Component)
                {
                    toSO.FindProperty(fromProp.propertyPath).objectReferenceValue = target.GetComponent(objValue.GetType());
                }
                else
                {
                    toSO.FindProperty(fromProp.propertyPath).objectReferenceValue = target;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void TryCopyValueFrom(SerializedProperty propA, SerializedProperty propB)
        {
            if (propA == null || propB == null)
            {
                Debug.LogError($"Unable to copy from {propB?.propertyPath} to {propA?.propertyPath}");
                return;
            }
            else if (propA.propertyType != propB.propertyType)
            {
                Debug.LogError($"Different property types A: {propB?.propertyPath} B: {propA?.propertyPath}");
                return;
            }
            switch (propA.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    propA.boolValue = propB.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    propA.floatValue = propB.floatValue;
                    break;
                case SerializedPropertyType.Integer:
                    propA.intValue = propB.intValue;
                    break;
                case SerializedPropertyType.String:
                    propA.stringValue = propB.stringValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    propA.objectReferenceValue = propB.objectReferenceValue;
                    break;
                case SerializedPropertyType.Color:
                    propA.colorValue = propB.colorValue;
                    break;
                case SerializedPropertyType.Vector3:
                    propA.vector3Value = propB.vector3Value;
                    break;
                case SerializedPropertyType.Quaternion:
                    propA.quaternionValue = propB.quaternionValue;
                    break;
                case SerializedPropertyType.Vector2:
                    propA.vector2Value = propB.vector2Value;
                    break;
                case SerializedPropertyType.Enum:
                    propA.enumValueIndex = propB.enumValueIndex;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    propA.animationCurveValue = propB.animationCurveValue;
                    break;
                //case SerializedPropertyType.ArraySize:
                //    propA.arraySize = propB.arraySize;
                //    break;
                case SerializedPropertyType.Bounds:
                    propA.boundsValue = propB.boundsValue;
                    break;
                case SerializedPropertyType.Rect:
                    propA.rectValue = propB.rectValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    propA.boundsIntValue = propB.boundsIntValue;
                    break;
                case SerializedPropertyType.Character:
                    propA.intValue = propB.intValue;
                    break;
                case SerializedPropertyType.ExposedReference:
                    propA.exposedReferenceValue = propB.exposedReferenceValue;
                    break;

                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.LayerMask:
                    propA.intValue = propB.intValue;
                    break;
                case SerializedPropertyType.RectInt:
                    propA.rectIntValue = propB.rectIntValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    propA.vector2IntValue = propB.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    propA.vector3IntValue = propB.vector3IntValue;
                    break;
                case SerializedPropertyType.Vector4:
                    propA.vector4Value = propB.vector4Value;
                    break;
            }
        }
    }
}
