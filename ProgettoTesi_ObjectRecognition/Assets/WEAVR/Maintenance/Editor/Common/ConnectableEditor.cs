    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Editor;
    using TXT.WEAVR.Interaction;
    using UnityEditor;
    using UnityEngine;

namespace TXT.WEAVR.Maintenance
{

    [CustomEditor(typeof(AbstractConnectable), true)]
    public class ConnectableEditor : InteractiveBehaviourEditor
    {
        const float k_epsilon = 0.01f * 0.01f;
        AbstractConnectable m_target;

        private void OnEnable()
        {
            m_target = target as AbstractConnectable;
        }

        public override void OnInspectorGUI() {
            if (m_target.ConnectionPoint)
            {
                var localScale = m_target.ConnectionPoint.localScale;

                if (!AreSimilar(localScale, Vector3.one))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal("Box");
                    EditorGUILayout.HelpBox("The LOCAL scale of the connection point does not equal to one, this may cause issues with grab mechanics", MessageType.Warning);

                    var color = GUI.contentColor;
                    GUI.contentColor = Color.cyan;
                    if (GUILayout.Button("FIX", GUILayout.ExpandHeight(true), GUILayout.MinWidth(50)))
                    {
                        Undo.RegisterCompleteObjectUndo(m_target.ConnectionPoint, "Modified Connection Point");
                        m_target.ConnectionPoint.localScale = Vector3.one;
                    }
                    EditorGUILayout.EndHorizontal();
                    GUI.contentColor = color;
                }
                else if (!AreSimilar(m_target.ConnectionPoint.lossyScale, Vector3.one))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal("Box");
                    EditorGUILayout.HelpBox("The GLOBAL scale of the connection point does not equal to one, this may cause issues with grab mechanics", MessageType.Warning);

                    var color = GUI.contentColor;
                    GUI.contentColor = Color.cyan;
                    if (GUILayout.Button("FIX", GUILayout.ExpandHeight(true), GUILayout.MinWidth(50)))
                    {
                        m_target.ConnectionPoint.localScale = Vector3.one;

                        Transform parent = null;
                        if(m_target.ConnectionPoint.parent == m_target.transform)
                        {
                            parent = new GameObject(m_target.ConnectionPoint.name + "_Parent").transform;
                            //Undo.RegisterCreatedObjectUndo(parent.gameObject, "Created parent for connection point");
                        }
                        else
                        {
                            parent = m_target.ConnectionPoint.parent;
                            //Undo.RegisterCompleteObjectUndo(parent, "Modified parent for connection point");
                        }
                        //Undo.RegisterCompleteObjectUndo(m_target.ConnectionPoint, "Modified Connection Point");
                        m_target.ConnectionPoint.SetParent(m_target.transform, true);
                        m_target.ConnectionPoint.localScale = Vector3.one;
                        var lossyScale = m_target.ConnectionPoint.lossyScale;
                        if (AreSimilar(m_target.transform.lossyScale, Vector3.one))
                        {
                            if (Application.isPlaying)
                            {
                                Destroy(parent.gameObject);
                            }
                            else
                            {
                                DestroyImmediate(parent.gameObject);
                            }
                        }
                        else
                        {
                            parent.SetParent(m_target.ConnectionPoint, false);
                            ResetLocalTransform(parent);
                            parent.SetParent(m_target.transform, true);
                            parent.localScale = new Vector3(lossyScale.x != 0 ? 1f / lossyScale.x : 1f,
                                                            lossyScale.y != 0 ? 1f / lossyScale.y : 1f,
                                                            lossyScale.z != 0 ? 1f / lossyScale.z : 1f);
                            m_target.ConnectionPoint.SetParent(parent, true);
                            ResetLocalTransform(m_target.ConnectionPoint);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    GUI.contentColor = color;
                }
            }

            base.OnInspectorGUI();
        }

        private static void ResetLocalTransform(Transform parent)
        {
            parent.localPosition = Vector3.zero;
            parent.localRotation = Quaternion.identity;
            parent.localScale = Vector3.one;
        }

        private bool AreSimilar(Vector3 a, Vector3 b, float epsilon = k_epsilon)
        {
            return Mathf.Abs(a.x - b.x) < k_epsilon && Mathf.Abs(a.y - b.y) < k_epsilon && Mathf.Abs(a.z - b.z) < k_epsilon;
        }
    }
}