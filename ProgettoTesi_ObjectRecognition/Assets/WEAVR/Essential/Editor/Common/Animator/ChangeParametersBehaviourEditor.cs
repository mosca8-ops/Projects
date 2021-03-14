namespace TXT.WEAVR.InteractionUI
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    [CustomEditor(typeof(ChangeParametersBehaviour))]
    public class ChangeParametersBehaviourEditor : Editor
    {
        private AnimatorController _controller;
        private int _pickerId = -1;
        private bool _syncRequested;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Utilities", EditorStyles.boldLabel);

            if (_syncRequested) {
                SyncParameters();
            }

            if (ObjectPicked()) {
                SyncArrayTypes();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Sync Animator", "Get the variables from the specified animator"))) {
                _pickerId = (int)Random.Range(0, 1000);
                EditorGUIUtility.ShowObjectPicker<AnimatorController>(_controller, true, null, _pickerId);
            }
            if (GUILayout.Button(new GUIContent("Clear All", "Clears all arrays"))) {
                ChangeParametersBehaviour behaviour = target as ChangeParametersBehaviour;
                behaviour.boolParameters.Clear();
                behaviour.floatParameters.Clear();
                behaviour.intParameters.Clear();
                behaviour.triggers.Clear();
            }
            EditorGUILayout.EndHorizontal();
        }

        private bool ObjectPicked() {
            if (Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == _pickerId) {

                _controller = EditorGUIUtility.GetObjectPickerObject() as AnimatorController;
                _pickerId = -1;
                
                return _controller != null;
            }
            return false;
        }

        private void SyncArrayTypes() {
            ChangeParametersBehaviour behaviour = target as ChangeParametersBehaviour;
            behaviour.boolParameters.ParametersType = AnimatorControllerParameterType.Bool;
            behaviour.floatParameters.ParametersType = AnimatorControllerParameterType.Float;
            behaviour.intParameters.ParametersType = AnimatorControllerParameterType.Int;
            behaviour.triggers.ParametersType = AnimatorControllerParameterType.Trigger;

            _syncRequested = true;
        }

        private void SyncParameters() {
            ChangeParametersBehaviour behaviour = target as ChangeParametersBehaviour;

            foreach (var parameter in _controller.parameters) {
                switch (parameter.type) {
                    case AnimatorControllerParameterType.Bool:
                        if (!behaviour.boolParameters.Contains(parameter.name)) {
                            behaviour.boolParameters.Add(parameter);
                        }
                        break;
                    case AnimatorControllerParameterType.Float:
                        if (!behaviour.floatParameters.Contains(parameter.name)) {
                            behaviour.floatParameters.Add(parameter);
                        }
                        break;
                    case AnimatorControllerParameterType.Int:
                        if (!behaviour.intParameters.Contains(parameter.name)) {
                            behaviour.intParameters.Add(parameter);
                        }
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        if (!behaviour.triggers.Contains(parameter.name)) {
                            behaviour.triggers.Add(parameter);
                        }
                        break;
                }
            }

            _syncRequested = false;
        }
    }
}

