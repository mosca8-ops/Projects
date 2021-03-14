namespace TXT.WEAVR.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(WorldBounds))]
    public class WorldBoundsEditor : Editor
    {
        private EditorCoroutine m_coroutine;

        private WorldBounds m_lastTarget;
        private static GUIContent s_updateRaycastersContent = new GUIContent("Update Raycasters", "Removes the bounds layers from all raycasters");

        private void OnEnable()
        {
            if (m_lastTarget != target)
            {
                m_lastTarget = target as WorldBounds;
                m_coroutine = EditorCoroutine.StartCoroutine(m_lastTarget.RefreshBounds());
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            WorldBounds worldBounds = target as WorldBounds;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Visibility", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
            worldBounds.VisibileInHierarchy = GUILayout.Toggle(worldBounds.VisibileInHierarchy, "Hierarchy", EditorStyles.miniButtonLeft);
            worldBounds.VisibleInScene = GUILayout.Toggle(worldBounds.VisibleInScene, "Scene", EditorStyles.miniButtonRight);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Color guiColor = GUI.color;
            GUI.color = Color.yellow;
            if (GUILayout.Button(s_updateRaycastersContent))
            {
                WorldBounds.RemoveBoundsLayersFromRaycasts();
            }
            GUI.color = Color.green;
            GUI.enabled = !worldBounds.IsRunning;
            if (GUILayout.Button("Generate Bounds"))
            {
                EditorCoroutine.StopCoroutine(m_coroutine);
                m_coroutine = EditorCoroutine.StartCoroutine(worldBounds.BuildBoundsCoroutine());
            }
            GUI.enabled = true;
            if (worldBounds.IsRunning)
            {
                GUI.color = Color.red;
                if (GUILayout.Button("Cancel"))
                {
                    EditorCoroutine.StopCoroutine(m_coroutine);
                    worldBounds.CancelBuild();
                }
            }
            else
            {
                GUI.color = Color.cyan;
                if (GUILayout.Button("Clear Bounds"))
                {
                    m_coroutine = EditorCoroutine.StartCoroutine(worldBounds.ClearBounds());
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUI.color = guiColor;
            if (worldBounds.IsRunning)
            {
                Rect progressRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                EditorGUI.ProgressBar(progressRect, worldBounds.OperationProgress, worldBounds.OperationText);
                Repaint();
            }
            else
            {
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }
        }

        private void OnSceneGUI()
        {
            WorldBounds worldBounds = target as WorldBounds;
            if (!worldBounds.VisibleInScene) { return; }
            for (int i = 0; i < worldBounds.OccupiedSpaces.Count; i++)
            {
                OccupiedSpace space = worldBounds.OccupiedSpaces[i];
                if (space == null || !space.gameObject.activeInHierarchy) { continue; }
                if (space.Trigger != null)
                {

                    if (space.Trigger is BoxCollider)
                    {
                        Handles.matrix = Matrix4x4.TRS(space.transform.position, space.transform.rotation, space.transform.lossyScale);
                        Handles.DrawWireCube((space.Trigger as BoxCollider).center, (space.Trigger as BoxCollider).size);
                    }
                    else
                    {
                        Handles.DrawWireCube(space.Trigger.bounds.center, space.Trigger.bounds.size);
                    }
                }
                else
                {
                    Handles.Label(space.transform.position, "Error", WeavrStyles.RedLeftBoldLabel);
                }
            }
        }
    }
}
