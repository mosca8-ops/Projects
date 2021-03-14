namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(CockpitLever), true)]
    public class CockpitLeverEditor : CockpitElementEditor
    {
        private const float minInteractiveStateBoxSide = 0.02f;
        private const float minStateSqrDistance = 0.005f * 0.005f;

        private CockpitLever _cockpitLever;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
        }

        protected override bool DrawFooter(bool withAnimator) {
            if (!base.DrawFooter(withAnimator)) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            }

            _cockpitLever = target as CockpitLever;

            if (GUILayout.Button("Generate Interactive States")) {
                var collider = _cockpitLever.GetComponent<Collider>();
                bool destroyCollider = false;
                if(collider == null) {
                    destroyCollider = true;
                    collider = GenerateBoxCollider(_cockpitLever.gameObject, minBoxColliderSide, true);
                }
                foreach (var iState in _cockpitLever.EditorInteractiveStates) {
                    if (iState != null) {
                        DestroyImmediate(iState.transform.parent.gameObject);
                        DestroyImmediate(iState.gameObject);
                    }
                }
                _cockpitLever.EditorInteractiveStates.Clear();

                // Create a sibling of this lever, to hold the states fixed
                var parent = new GameObject(_cockpitLever.name + "_InteractiveStates");
                if (_cockpitLever.transform.parent != null) {
                    parent.transform.SetParent(_cockpitLever.transform.parent, false);
                }

                int stateIndex = 0;
                foreach(var state in _cockpitLever.EditorStates) {
                    var newGO = new GameObject("State_" + state.state);
                    newGO.transform.SetParent(parent.transform, false);
                    var interactiveState = newGO.AddComponent<InteractiveElementState>();
                    //if (!state.useAnimator) {
                        newGO.transform.localPosition = state.position;
                        newGO.transform.localRotation = state.rotation;
                        var boxCollider = GenerateBoxCollider(newGO, minInteractiveStateBoxSide, true);
                        boxCollider.size = new Vector3(collider.bounds.size.x, minBoxColliderSide, collider.bounds.size.z);
                        interactiveState.pointerCollider = boxCollider;
                        interactiveState.state = state;
                    //}
                    //else {
                    //    // Complex logic... find parameters end states and get the last frame position
                         // For now just use the positions
                    //}

                    stateIndex++;

                    // Check if already existing
                    if (CheckIfCloseToNeighbors(newGO)) {
                        DestroyImmediate(newGO);
                        continue;
                    }

                    _cockpitLever.EditorInteractiveStates.Add(interactiveState);
                }

                if (destroyCollider) {
                    DestroyImmediate(collider);
                }
            }

            if(_cockpitLever.EditorInteractiveStates.Count > 0 && GUILayout.Button("Remove Interactive States")) {
                foreach (var iState in _cockpitLever.EditorInteractiveStates) {
                    if (iState != null) {
                        //DestroyImmediate(iState.gameObject);
                        DestroyImmediate(iState.transform.parent.gameObject);
                        break;
                    }
                }
                _cockpitLever.EditorInteractiveStates.Clear();
            }

            return true;
        }

        private bool CheckIfCloseToNeighbors(GameObject newGO) {
            foreach (var iState in _cockpitLever.EditorInteractiveStates) {
                if ((iState.transform.position - newGO.transform.position).sqrMagnitude < minStateSqrDistance) {
                    return true;
                }
            }
            return false;
        }
    }
}