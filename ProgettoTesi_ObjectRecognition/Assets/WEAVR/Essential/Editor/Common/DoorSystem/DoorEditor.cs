using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomEditor(typeof(AbstractDoor), true)]
    [CanEditMultipleObjects]
    public class AbstractDoorEditor : UnityEditor.Editor
    {
        private AbstractDoor m_door;
        private AbstractDoor[] m_doors;
        private bool m_isEditTimeReady;

        private Vector3[] m_defaultPositions;
        private Quaternion[] m_defaultRotations;
        private float m_currentOpeningProgress;

        protected static GUIStyle s_toggleButtonStyle;

        private bool m_automaticRestore;

        private void OnEnable()
        {
            m_door = target as AbstractDoor;
            if(targets.Length > 1)
            {
                m_doors = targets.Cast<AbstractDoor>().ToArray();
            }
            else
            {
                m_doors = new AbstractDoor[] { m_door };
            }
            m_isEditTimeReady = target.GetType().GetCustomAttribute<ExecuteInEditMode>() != null;

            m_currentOpeningProgress = 0;

            if (m_defaultPositions == null)
            {
                m_defaultPositions = new Vector3[targets.Length];
            }
            if (m_defaultRotations == null)
            {
                m_defaultRotations = new Quaternion[targets.Length];
            }

            SaveDefaults();

            m_automaticRestore = false;
        }

        private void OnDisable()
        {
            if (m_automaticRestore)
            {
                RestoreDefaults();
            }
        }

        private void OnDestroy()
        {
            if(m_automaticRestore && m_defaultPositions != null && m_doors != null && m_defaultRotations != null)
            {
                RestoreDefaults();
            }
        }

        private void SaveDefaults()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_defaultPositions[i] = m_doors[i].transform.localPosition;
                    m_defaultRotations[i] = m_doors[i].transform.localRotation;
                }
            }
            m_automaticRestore = false;
        }

        private void RestoreDefaults()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].transform.localPosition = m_defaultPositions[i];
                    m_doors[i].transform.localRotation = m_defaultRotations[i];
                }
            }
            m_automaticRestore = false;
        }

        private float OpenProgress
        {
            get { return m_currentOpeningProgress; }
            set
            {
                value = Mathf.Clamp01(value);
                if(m_currentOpeningProgress != value)
                {
                    m_currentOpeningProgress = value;
                    m_automaticRestore = true;
                    for (int i = 0; i < m_doors.Length; i++)
                    {
                        m_doors[i].DebugSetDoorPosition(m_currentOpeningProgress);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            // Draw class type
            serializedObject.Update();
            EditorGUILayout.Space();
            var lastColor = GUI.color;
            var lastContentColor = GUI.contentColor;
            bool wasEnabled = GUI.enabled;

            DrawSnapshotControls();

            DrawStateControls();

            DrawBlockControls();

            GUI.color = lastColor;

            var baseProperty = serializedObject.FindProperty("m_canBeLocked");
            EditorGUILayout.PropertyField(baseProperty);
            DrawInspector(baseProperty);
            if (serializedObject.ApplyModifiedProperties())
            {
                m_automaticRestore = true;
            }
        }

        protected virtual void DrawBlockControls()
        {
            if (s_toggleButtonStyle == null)
            {
                s_toggleButtonStyle = new GUIStyle("Button");
                s_toggleButtonStyle.wordWrap = true;
                s_toggleButtonStyle.fontSize = 10;
            }

            SerializedProperty property = null;

            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("When Fully Open", EditorStyles.centeredGreyMiniLabel);
            property = serializedObject.FindProperty("m_blockOnFullyOpened");
            property.boolValue = GUILayout.Toggle(property.boolValue, "Block", s_toggleButtonStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            GUILayout.Label("When Closed", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginHorizontal();
            property = serializedObject.FindProperty("m_blockOnClosed");
            property.boolValue = GUILayout.Toggle(property.boolValue, "Block", s_toggleButtonStyle);
            property = serializedObject.FindProperty("m_snapOnClosed");
            property.boolValue = GUILayout.Toggle(property.boolValue, "Snap", s_toggleButtonStyle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        protected virtual void DrawStateControls()
        {
            if (Application.isPlaying || m_isEditTimeReady)
            {
                var lastColor = GUI.color;
                var lastContentColor = GUI.contentColor;

                var door = target as AbstractDoor;
                EditorGUILayout.BeginHorizontal("Box");
                GUILayout.Label("State: ", GUILayout.ExpandWidth(false));
                if (door.IsLocked)
                {
                    GUI.color = Color.red;
                    GUILayout.Label("LOCKED");
                }
                else if (door.IsClosed)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("CLOSED");
                }
                else if (door.IsFullyOpened)
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label("FULLY OPEN");
                }
                else
                {
                    GUI.color = Color.green;
                    GUILayout.Label("OPEN");
                }

                GUI.color = lastColor;
                GUILayout.FlexibleSpace();

                if (door.IsLocked)
                {
                    GUI.contentColor = Color.green;
                    if (GUILayout.Button("Unlock"))
                    {
                        door.Unlock();
                    }
                }
                else if (door.IsClosed)
                {
                    GUI.contentColor = Color.red;
                    if (GUILayout.Button("Lock"))
                    {
                        door.Lock();
                    }
                    GUI.contentColor = Color.green;
                    if (GUILayout.Button("Open"))
                    {
                        door.Open();
                    }
                }
                else
                {
                    GUI.contentColor = Color.yellow;
                    if (GUILayout.Button("Close"))
                    {
                        door.Close();
                    }
                }

                GUI.contentColor = lastContentColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        protected virtual void DrawSnapshotControls()
        {
            bool wasEnabled = GUI.enabled;

            EditorGUILayout.BeginVertical("Box");

            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));

                EditorGUILayout.BeginHorizontal("Box");
                GUILayout.Label("Default Pose", EditorStyles.miniLabel, GUILayout.Width(70));
                if (GUILayout.Button("Save", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    SaveDefaults();
                }
                if (GUILayout.Button("Restore", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    RestoreDefaults();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("Box");
                GUILayout.Label("Closed", EditorStyles.miniLabel, GUILayout.Width(40));
                OpenProgress = GUILayout.HorizontalSlider(OpenProgress, 0, 1, GUILayout.Height(12));
                GUILayout.Label("Open", EditorStyles.miniLabel, GUILayout.Width(40));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();

                if (m_automaticRestore)
                {
                    var color = GUI.contentColor;
                    GUI.contentColor = Color.cyan;
                    GUILayout.Label("Will automatically restore default pose", EditorStyles.centeredGreyMiniLabel);
                    GUI.contentColor = color;
                }
            }

            EditorGUILayout.BeginHorizontal(/*"Box", */GUILayout.ExpandWidth(true));
            //GUILayout.FlexibleSpace();

            float maxColumnWidth = (EditorGUIUtility.currentViewWidth - 80) * 0.5f;

            EditorGUILayout.BeginVertical("Box");
            if (GUILayout.Button("Mark Closed", EditorStyles.miniButton))
            {
                m_door.SnapshotClosed();
                m_automaticRestore = true;
            }
            GUI.enabled = false;
            var pointProperty = serializedObject.FindProperty("m_closedLocalPosition");
            EditorGUILayout.Vector3Field(GUIContent.none, pointProperty.vector3Value, GUILayout.MaxWidth(maxColumnWidth));
            GUI.enabled = wasEnabled;
            EditorGUILayout.EndVertical();

            //GUILayout.Space(2);

            EditorGUILayout.BeginVertical("Box");
            if (GUILayout.Button("Mark Open", EditorStyles.miniButton))
            {
                m_door.SnapshotFullyOpen();
                m_automaticRestore = true;
            }
            GUI.enabled = false;
            pointProperty = serializedObject.FindProperty("m_openedLocalPosition");
            EditorGUILayout.Vector3Field(GUIContent.none, pointProperty.vector3Value, GUILayout.MaxWidth(maxColumnWidth));
            GUI.enabled = wasEnabled;
            EditorGUILayout.EndVertical();

            //GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawInspector(SerializedProperty currentProperty)
        {
            while (currentProperty.NextVisible(false))
            {
                EditorGUILayout.PropertyField(currentProperty, true);
            }
        }
    }
}
