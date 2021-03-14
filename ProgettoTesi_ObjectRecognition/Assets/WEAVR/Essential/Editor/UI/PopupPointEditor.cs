using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.UI;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor.UI
{

    [CustomEditor(typeof(PopupPoint))]
    public class PopupPointEditor : UnityEditor.Editor
    {
        private PopupPoint m_popupPoint;
        private Renderer m_renderer;
        private bool m_renderInScene;

        private Tool m_lastTool;

        public bool RenderInScene
        {
            get { return m_renderInScene; }
            set
            {
                if (m_renderInScene != value)
                {
                    m_renderInScene = value;
                    SceneView.duringSceneGui -= OnSceneGUI;
                    if (m_renderInScene)
                    {
                        UnityEditor.Tools.hidden = true;
                        SceneView.duringSceneGui += OnSceneGUI;
                    }
                    else
                    {
                        UnityEditor.Tools.hidden = false;
                    }
                    SceneView.lastActiveSceneView?.Repaint();
                }
            }
        }

        private class Styles : BaseStyles
        {
            public GUIStyle point;
            public GUIStyle origin;
            public GUIStyle distance;

            protected override void InitializeStyles(bool isProSkin)
            {
                point = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.LowerCenter,
                };
                point.normal.textColor = Color.cyan;

                origin = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.LowerCenter,
                };

                distance = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    //alignment = TextAnchor.MiddleCenter,
                };
                distance.normal.textColor = new Color(0.2f, 1f, 0.2f, 0.5f);
            }
        }

        private static Styles s_styles = new Styles();

        private void OnEnable()
        {
            m_lastTool = UnityEditor.Tools.current;
            m_popupPoint = target as PopupPoint;
            if (m_popupPoint != null)
            {
                m_renderer = m_popupPoint.GetComponent<Renderer>();
            }

            RenderInScene = false;
        }

        private void OnDisable()
        {
            UnityEditor.Tools.hidden = false;
            RenderInScene = false;
        }

        private void OnDestroy()
        {
            RenderInScene = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUI.enabled = m_popupPoint.point != null;
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            RenderInScene = GUILayout.Toggle(m_renderInScene, "Scene View", "Button", GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!m_renderInScene || m_popupPoint.point == null)
            {
                UnityEditor.Tools.hidden = false;
                return;
            }

            s_styles.Refresh();

            UnityEditor.Tools.hidden = m_popupPoint.origin != null;
            //UnityEditor.Tools.hidden = true;
            //var currentTool = UnityEditor.Tools.current;
            //UnityEditor.Tools.current = Tool.View;

            if (m_popupPoint.point.position == m_popupPoint.transform.position)
            {
                m_popupPoint.point.position += Vector3.one * 0.1f;
            }

            var origin = m_popupPoint.origin != null ? m_popupPoint.origin.position :
                                                        m_renderer != null ? m_renderer.bounds.center :
                                                        m_popupPoint.transform.position;
            var originHandleSize = HandleUtility.GetHandleSize(origin);
            var originSize = originHandleSize * 0.075f;
            var matrix = Handles.matrix;
            //Handles.matrix = Matrix4x4.TRS(Vector3.zero, m_popupPoint.transform.rotation, Vector3.one);
            //Handles.matrix = Matrix4x4.TRS(m_popupPoint.transform.position, m_popupPoint.transform.rotation, m_popupPoint.transform.lossyScale);
            //origin = m_popupPoint.transform.InverseTransformPoint(origin);
            Handles.color = Color.white;
            Handles.DrawSolidDisc(origin, Vector3.Normalize(Camera.current.transform.position - origin), originSize);
            Handles.DrawWireCube(origin, Vector3.one /** originHandleSize*/ * 0.25f);
            Handles.Label(origin, "Origin", s_styles.origin);
            if (m_popupPoint.origin != null)
            {
                m_popupPoint.origin.position = Handles.PositionHandle(m_popupPoint.origin.position, Quaternion.identity);
            }

            Handles.DrawAAPolyLine(4, m_popupPoint.point.position, origin);

            Handles.Label((m_popupPoint.point.position + origin) * 0.5f, $"{Vector3.Distance(origin, m_popupPoint.point.position):0.000} m", s_styles.distance);

            var pointHandleSize = HandleUtility.GetHandleSize(m_popupPoint.point.position);
            var pointSize = pointHandleSize * 0.075f;


            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(m_popupPoint.point.position, Vector3.Normalize(Camera.current.transform.position - m_popupPoint.point.position), pointSize);

            Handles.DrawWireCube(m_popupPoint.point.position, Vector3.one * 0.25f /** pointHandleSize*/);
            Handles.Label(m_popupPoint.point.position, "Popup Point", s_styles.point);
            m_popupPoint.point.position = Handles.PositionHandle(m_popupPoint.point.position, Quaternion.identity);

            Handles.matrix = matrix;

            //UnityEditor.Tools.current = currentTool;
            //UnityEditor.Tools.hidden = false;
        }
    }
}