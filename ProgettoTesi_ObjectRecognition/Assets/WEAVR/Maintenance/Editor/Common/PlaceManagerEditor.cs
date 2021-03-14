namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Editor;
    using TXT.WEAVR.Interaction;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(AbstractPlaceManager), true)]
    public class PlaceManagerEditor : InteractiveBehaviourEditor
    {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            //GUI.color = Color.green;
            if(GUILayout.Button("Create place points", GUILayout.Height(24))) {
                CreatePlacePoints();
            }
            //GUI.color = Color.cyan;
            if(GUILayout.Button("Sync with Placeables", GUILayout.Height(24))) {
                SyncPlaceables();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void SyncPlaceables() {
            int syncCount = 0;
            var thisPlaceManager = target as AbstractPlaceManager;
            foreach(var placeable in FindObjectsOfType<AbstractPlaceable>()) {
                if(placeable.PlaceManagers == null || !placeable.PlaceManagers.Contains(thisPlaceManager)) {
                    if (placeable.ValidPlacements.HasInputClass(thisPlaceManager.ObjectClass))
                    {
                        placeable.PlaceManagers.Add(thisPlaceManager);
                        syncCount++;
                    }
                }
            }
            if (EditorWindow.focusedWindow != null) {
                if (syncCount > 0) {
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent("Updated " + syncCount + " Placeables"));
                }
                else {
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent("Everything in sync"));
                }
            }
        }

        private void OnSceneGUI()
        {
            
        }

        private void CreatePlacePoints() {
            var placeManager = target as AbstractPlaceManager;
            // First remove all place points
            foreach(var placePoint in placeManager.GetComponentsInChildren<PlacePoint>(true)) {
                DestroyImmediate(placePoint.gameObject);
            }
            placeManager.slots.Clear();

            var lastRotation = placeManager.transform.rotation;
            placeManager.transform.rotation = Quaternion.identity;

            // Get bounds
            var bounds = placeManager.gameObject.GetBounds();
            float xLength = bounds.size.x / placeManager.slotColumns;
            float yLength = bounds.size.z / placeManager.slotRows;

            Vector3 startPoint = bounds.extents * 0.8f;
            
            // Create place points
            for (int r = 0; r < placeManager.slotRows; r++) {
                for (int c = 0; c < placeManager.slotColumns; c++) {
                    Vector3 position = startPoint;
                    position.x -= r * xLength + xLength * 0.5f;
                    position.z -= c * yLength + yLength * 0.5f;
                    var gameObject = new GameObject(string.Format("Place_{0}_{1}", r, c));
                    gameObject.transform.SetParent(placeManager.transform, true);
                    gameObject.transform.up = Vector3.up;
                    gameObject.transform.localPosition = position;
                    var placePoint = gameObject.AddComponent<PlacePoint>();
                    placePoint.animatePlacing = placeManager.animatePlacing;
                    placeManager.slots.Add(placePoint);
                }
            }

            placeManager.transform.rotation = lastRotation;
        }
    }
}