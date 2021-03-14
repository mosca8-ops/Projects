using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ReferenceTable))]
    public class ReferenceTableEditor : UnityEditor.Editor
    {
        private class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle shortFoldout;
            public GUIStyle targetButton;
            public GUIStyle lockButton;
            public GUIStyle scenePath;

            protected override void InitializeStyles(bool isProSkin)
            {
                box = new GUIStyle("Box");
                shortFoldout = new GUIStyle(EditorStyles.foldout);
                shortFoldout.fixedWidth = 1;
                targetButton = WeavrStyles.EditorSkin2.FindStyle("underlinedButton") ?? new GUIStyle(EditorStyles.miniButton);
                lockButton = new GUIStyle("Button")
                {
                    fontStyle = FontStyle.Bold,
                };

                scenePath = new GUIStyle(targetButton);
                scenePath.padding.top = -8;
                scenePath.fixedHeight = 14;
                scenePath.fontSize = 12;
            }
        }

        private class Labels
        {
            public GUIContent listBullet = new GUIContent(@"●");
            public GUIContent forceRestore = new GUIContent(" Force Restore", WeavrStyles.Icons["WarningIcon"]);
            public GUIContent clearColors = new GUIContent(" Clear Colors", WeavrStyles.Icons.Close);
            public GUIContent @lock = new GUIContent( " Lock  ", WeavrStyles.Icons["lock"]);
            public GUIContent unlock = new GUIContent(" Unlock", WeavrStyles.Icons["unlock"]);
            public GUIContent corruptedReference = new GUIContent("Corrupted Reference");
            public GUIContent corruptedTarget = new GUIContent("Corrupted Target");
            public GUIContent was = new GUIContent(" was: ");
        }

        private class Colors
        {
            public static readonly Color lightRed = new Color(1f, 0.5f, 0.5f, 1f);
        }

        private Styles m_styles = new Styles();
        private Labels m_labels;
        
        private bool m_showReferences;
        private bool m_lockedReferences;

        private Vector2 m_scrollPosition;
        private string m_searchString;

        private ReferenceTable m_table;

        private Dictionary<object, bool> m_foldouts;
        private Dictionary<ProcedureObject, GraphObject> m_containers;

        private HashSet<ReferenceItem> m_validReplacements;
        private HashSet<ReferenceItem> m_invalidReplacements;

        private void OnEnable()
        {
            m_table = target as ReferenceTable;
            m_foldouts = new Dictionary<object, bool>();
            m_lockedReferences = true;
            m_containers = new Dictionary<ProcedureObject, GraphObject>();

            m_validReplacements = new HashSet<ReferenceItem>();
            m_invalidReplacements = new HashSet<ReferenceItem>();

            m_labels = new Labels();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            m_styles.Refresh();

            var color = GUI.color;
            var contentColor = GUI.contentColor;
            GUILayout.BeginVertical(m_styles.box);

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            m_showReferences = EditorGUILayout.Foldout(m_showReferences, $"References  [{m_table.References.Count}]", true);
            GUILayout.FlexibleSpace();

            if (!m_table.SceneData.IsCurrentlyOpen())
            {
                GUILayout.Label($"Scene: {m_table.SceneData.Name}");
                EditorGUILayout.EndHorizontal();

                if (m_showReferences)
                {
                    EditorGUI.indentLevel++;
                    var references = m_table.References;
                    for (int i = 0; i < references.Count; i++)
                    {
                        var reference = references[i];
                        if (!reference)
                        {
                            GUILayout.Label(m_labels.corruptedReference, WeavrStyles.RedLeftBoldLabel);
                            continue;
                        }
                        EditorGUILayout.BeginHorizontal(m_styles.box);
                        GUILayout.Label(reference.referenceScenePath);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{reference.Targets.Count} Targets");
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                return;
            }

            if(GUILayout.Button(m_labels.forceRestore))
            {
                m_table.ForceRestoreFromJSON();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            if(m_invalidReplacements.Count > 0 || m_validReplacements.Count > 0)
            {
                if(GUILayout.Button(m_labels.clearColors))
                {
                    m_validReplacements.Clear();
                    m_invalidReplacements.Clear();
                }
            }


            GUI.contentColor = m_lockedReferences ? Colors.lightRed : Color.green;
            if(GUILayout.Button(m_lockedReferences ? m_labels.unlock : m_labels.@lock, m_styles.lockButton))
            {
                m_lockedReferences = !m_lockedReferences;
            }
            EditorGUILayout.EndHorizontal();
            GUI.contentColor = contentColor;

            if (m_showReferences)
            {
                GUILayout.Space(4);
                EditorGUILayout.BeginHorizontal("Box");
                float labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;
                m_searchString = EditorGUILayout.TextField("Search", m_searchString);
                bool doSearch = !string.IsNullOrEmpty(m_searchString);
                if (doSearch && GUILayout.Button("X", GUILayout.Width(20)))
                {
                    m_searchString = string.Empty;
                }
                EditorGUIUtility.labelWidth = labelWidth;
                EditorGUILayout.EndHorizontal();
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                if (Selection.activeObject == m_table.Procedure)
                {
                    EditorGUI.indentLevel++;
                }
                bool wasEnabled = GUI.enabled;
                var references = m_table.References;
                for (int i = 0; i < references.Count; i++)
                {
                    var reference = references[i];
                    if (!reference)
                    {
                        GUILayout.Label(m_labels.corruptedReference, WeavrStyles.RedLeftBoldLabel);
                        continue;
                    }
                    if(doSearch && reference.reference 
                        && !reference.reference.name.ToLower().Contains(m_searchString.ToLower()) 
                        && m_searchString.SimilarityDistanceTo(reference.reference.name) > 3)
                    {
                        continue;
                    }
                    EditorGUILayout.BeginVertical(m_styles.box);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical(GUILayout.MaxWidth(16));
                    if(!m_foldouts.TryGetValue(reference, out bool refFoldout))
                    {
                        m_foldouts[reference] = false;
                    }

                    GUI.color = m_validReplacements.Contains(reference) ? Color.green :
                                m_invalidReplacements.Contains(reference) ? Color.red :
                                color;

                    if(refFoldout != EditorGUILayout.Foldout(refFoldout, $"Ref {i + 1}", false, m_styles.shortFoldout))
                    {
                        refFoldout = !refFoldout;
                        m_foldouts[reference] = refFoldout;
                    }

                    EditorGUILayout.EndVertical();
                    GUI.enabled = !m_lockedReferences;
                    if (!reference.reference)
                    {
                        GUI.color = Color.red;
                    }
                    var newReference = EditorGUILayout.ObjectField(reference.reference,
                                                reference.reference ? reference.reference.GetType() : (reference.type ?? typeof(Object)),
                                                true);
                    GUI.color = color;
                    if(newReference && newReference != reference.reference)
                    {
                        ApplyNewReference(reference, newReference);
                    }
                    GUI.enabled = wasEnabled;
                    GUILayout.Label($"{reference.Targets.Count} Targets");
                    EditorGUILayout.EndHorizontal();

                    if (!newReference && !string.IsNullOrEmpty(reference.referenceScenePath))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(32);
                        //GUILayout.Label(m_labels.was);
                        //GUILayout.Label(reference.referenceScenePath, m_styles.scenePath);
                        GUILayout.Label(GetScenePathString(reference.referenceScenePath, reference.type), m_styles.scenePath);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }

                    if (refFoldout)
                    {
                        var targets = reference.Targets;
                        for (int j = 0; j < targets.Count; j++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(40);
                            GUILayout.Label(m_labels.listBullet, GUILayout.Width(10));
                            var target = targets[j];
                            if (!target || !target.Target)
                            {
                                GUILayout.Label(m_labels.corruptedTarget, WeavrStyles.RedLeftBoldLabel);
                                EditorGUILayout.EndHorizontal();
                                continue;
                            }
                            if (!m_containers.TryGetValue(target.Target, out GraphObject container))
                            {
                                m_containers[target.Target] = target.Target.TryGetStep();
                            }
                            if (GUILayout.Button(GetTargetString(container, target), m_styles.targetButton))
                            {
                                if (ProcedureObjectInspector.Selected == target?.Target?.Procedure as object)
                                {
                                    ProcedureEditor.Instance.Highlight(target.Target, false);
                                }
                                else
                                {
                                    ProcedureEditor.Instance?.Select(target.Target);
                                }
                            }
                            //GUILayout.Label($"{target.Target.GetType().Name}: {EditorTools.NicifyName(target.FieldPath)}");
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.EndVertical();
                }
                if (Selection.activeObject == m_table.Procedure)
                {
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private string GetTargetString(GraphObject container, ReferenceTarget target)
        {
            return "<color=#ffa500ff><b>" + (container is IProcedureStep gn ? 
                Clamp($"[{gn.Number} {gn.Title}]", 20) : 
                container is BaseTransition bt ?
                Clamp($"[{bt.From?.Title} to {bt.To?.Title}]", 20) : 
                container ? $"[{container.Title}]" : "-- none --") + "</b></color>"
                + $" {target.Target.GetType().Name}: {EditorTools.NicifyName(target.FieldPath)}";
        }

        private string GetScenePathString(string scenePath, Type componentType)
        {
            return $"was  <color=#69B1FFff><b>{scenePath}</b></color>  <color=#4E85BFff>[{componentType?.Name}]</color>";
        }

        private void ApplyNewReference(ReferenceItem reference, Object newReference)
        {
            var references = m_table.References;
            var refTransform = reference.reference?.GetGameObject()?.transform;
            var newTransform = newReference?.GetGameObject()?.transform;

            m_validReplacements.Clear();
            m_invalidReplacements.Clear();

            Dictionary<ReferenceItem, Object> childReferences = null;
            List<ReferenceItem> unsuccessfulItems = null;

            if(!refTransform && newTransform && !string.IsNullOrEmpty(reference.referenceScenePath))
            {
                var allRelatedReferences = references.Where(r => r != reference && r.referenceScenePath == reference.referenceScenePath);
                if(allRelatedReferences.Count() > 0 && EditorUtility.DisplayDialog("Replace related references", "Do you want to replace other references pointing to the same object as well?", "Yes", "No"))
                {
                    unsuccessfulItems = new List<ReferenceItem>();
                    foreach (var r in allRelatedReferences)
                    {
                        Object newRef = r.type == null ? null : r.type == typeof(GameObject) ? newTransform.gameObject as Object : newTransform.GetComponent(r.type);
                        if(newRef)
                        {
                            r.reference = newRef;
                            m_validReplacements.Add(m_table.Merge(r));
                        }
                        else
                        {
                            unsuccessfulItems.Add(r);
                        }
                    }
                }
            }
            else if (newTransform && refTransform && references.Any(r => r != reference && (r.reference?.GetGameObject()?.transform.IsChildOf(refTransform) ?? false)))
            {
                if (EditorUtility.DisplayDialog("Replace children", "Do you want to replace children references as well?", "Yes", "No"))
                {
                    childReferences = new Dictionary<ReferenceItem, Object>();
                    unsuccessfulItems = new List<ReferenceItem>();
                    var childItems = references.Where(r => r != reference 
                                    && (r.reference?.GetGameObject() == refTransform.gameObject 
                                        || (r.reference?.GetGameObject()?.transform.IsChildOf(refTransform) ?? false)));
                    
                    foreach(var child in childItems)
                    {
                        try
                        {
                            if(child.reference is GameObject go)
                            {
                                if(!go.transform.IsChildOf(newTransform)
                                   && TryGetSimilar(refTransform, go.transform, newTransform, out Transform similar))
                                {
                                    childReferences[child] = similar.gameObject;
                                }
                                else
                                {
                                    unsuccessfulItems.Add(child);
                                }
                            }
                            else if(child.reference is Component c)
                            {
                                if (!c.transform.IsChildOf(newTransform) 
                                    && TryGetSimilar(refTransform, c.transform, newTransform, out Transform similar, c.GetType()))
                                {
                                    var similarComponent = similar.GetComponent(c.GetType());
                                    if (similarComponent)
                                    {
                                        childReferences[child] = similarComponent;
                                    }
                                    else
                                    {
                                        unsuccessfulItems.Add(child);
                                    }
                                }
                            }
                            else
                            {
                                unsuccessfulItems.Add(child);
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                }
            }

            reference.reference = newReference;
            m_validReplacements.Add(m_table.Merge(reference));

            if (childReferences != null)
            {
                Debug.Log($"Merging children and components of {newReference?.name} --> [{childReferences.Count} out of {childReferences.Count + unsuccessfulItems.Count}]");
                foreach (var pair in childReferences)
                {
                    var r = pair.Key;
                    r.reference = pair.Value;
                    m_validReplacements.Add(m_table.Merge(r));
                }
                Debug.Log($"Merging successful");
            }

            if (unsuccessfulItems != null && unsuccessfulItems.Count > 0)
            {
                string unsuccessfulItemsString = "Items not merged: ";
                foreach (var item in unsuccessfulItems)
                {
                    m_invalidReplacements.Add(item);
                    unsuccessfulItemsString += $"{item.reference?.name}  |  ";
                }
                Debug.Log(unsuccessfulItemsString);
            }
        }

        private bool TryGetSimilar(Transform rootA, Transform childA, Transform rootB, out Transform childB, Type componentType = null)
        {
            if(childA == rootA)
            {
                childB = rootB;
                return true;
            }

            childB = null;

            List<Transform> parentsA = new List<Transform>();
            var parent = childA;
            while(parent && parent != rootA)
            {
                parentsA.Add(parent);
                parent = parent.parent;
            }

            parentsA.Remove(rootA);

            parentsA.Reverse();

            var parentB = rootB;
            foreach(var pA in parentsA)
            {
                var pB = parentB.Find(pA.name);
                if (!pB || (componentType != null && !pB.GetComponent(componentType)))
                {
                    // Get the children from the next 2 levels
                    var levelChildren = GetChildrenInLevels(parentB, 2);

                    // Order by similarity score and get the one with the highest score
                    var (child, similarity) = levelChildren.Select(t => (t, t.GetSimilarityScore(pA)))
                                                           .OrderByDescending(p => p.Item2)
                                                           .FirstOrDefault();
                    if(similarity > 0.5f)
                    {
                        pB = child;
                    }

                    // Try with names similarity test
                    //foreach(var child in levelChildren)
                    //{
                    //    if(AreSimilar(child, pA, true))
                    //    {
                    //        pB = child;
                    //        break;
                    //    }
                    //}

                    // Try with similar components values

                }

                parentB = pB;
                if (!parentB) { break; }
            }

            childB = parentB;

            return childB;
        }

        private bool AreSimilar(Transform a, Transform b, bool testChildren)
        {
            if(a.name.SimilarityDistanceTo(b.name) <= 2)
            {
                return HaveSimilarComponents(a, b, 0.25f) || (testChildren && HaveSimilarChildren(a, b, 0.5f));
            }
            return HaveSimilarComponents(a, b) || (testChildren && HaveSimilarChildren(a, b));
        }

        private bool HaveSimilarComponents(Transform a, Transform b, float similarityRatio = 0.5f)
        {
            var cA = a.GetComponents<Component>().Select(c => c.GetType());
            var cB = b.GetComponents<Component>().Select(c => c.GetType());
            return cA.Intersect(cB).Count() > cA.Union(cB).Count() * similarityRatio;
        }

        private bool HaveSimilarChildren(Transform a, Transform b, float similarityRatio = 0.75f)
        {
            var cA = GetChildrenInLevels(a, 1);
            var cB = GetChildrenInLevels(b, 1);

            var min = cA.Count() > cB.Count() ? cB : cA;
            var max = cA.Count() < cB.Count() ? cB : cA;

            int minCount = min.Count();
            int maxCount = max.Count();
            // Number of children is different
            if(maxCount == 0 || minCount < Mathf.CeilToInt(maxCount * similarityRatio))
            { return false; }

            var remainingChildren = new List<Transform>(max);
            foreach(var child in min)
            {
                var candidate = remainingChildren.FirstOrDefault(c => AreSimilar(c, child, false));
                if (candidate) { remainingChildren.Remove(candidate); }
            }

            return remainingChildren.Count <= Mathf.CeilToInt(maxCount * similarityRatio);
        }

        private IEnumerable<Transform> GetChildrenInLevels(Transform t, int levels)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < t.childCount; i++)
            {
                children.Add(t.GetChild(i));
            }
            if(levels > 1)
            {
                var prevChildren = children.ToArray();
                foreach(var child in prevChildren)
                {
                    children.AddRange(GetChildrenInLevels(child, levels - 1));
                }
            }
            return children;
        }

        private string Clamp(string s, int maxLength, string endVal = "..")
        {
            return s.Length > maxLength ? s.Substring(0, maxLength) + endVal : s;
        }

        private class ReferenceInfo
        {
            public bool foldout;
            public Dictionary<ReferenceTarget, BaseNode> blocksNodes;

            public ReferenceInfo()
            {
                foldout = false;
                blocksNodes = new Dictionary<ReferenceTarget, BaseNode>();
            }

            public void Refresh(Procedure procedure, ReferenceItem item)
            {
                blocksNodes.Clear();
                foreach(var target in item.Targets)
                {

                }
            }
        }
    }
}
