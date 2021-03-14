namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    [CustomEditor(typeof(CockpitElement), true)]
    public class CockpitElementEditor : Editor
    {
        protected const float minBoxColliderSide = 0.025f;

        private int _lastStateIndex;
        private bool _showStates = true;

        protected CockpitElement _cockpitElement;
        private List<StateInfo> _statesInfo;

        private string[] _animatorParameters;

        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private CockpitElement _lastTarget;

        //[SerializeField]
        //private PropertyValueWrapper _propertyWrapper;

        //public PropertyValueWrapper PropertyWrapper {
        //    get {
        //        if (_propertyWrapper == null) {
        //            _propertyWrapper = CreateInstance<PropertyValueWrapper>();
        //            if (AssetDatabase.Contains(this)) {
        //                AssetDatabase.AddObjectToAsset(_propertyWrapper, this);
        //            }
        //        }
        //        return _propertyWrapper;
        //    }
        //}

        private void OnEnable() {
            if (_cockpitElement == null || target != _lastTarget) {
                _cockpitElement = target as CockpitElement;
                _originalPosition = _cockpitElement.transform.position;
                _originalRotation = _cockpitElement.transform.rotation;
                _lastTarget = _cockpitElement;
            }
            if(_cockpitElement.EditorAnimator != null && _cockpitElement.EditorAnimator.runtimeAnimatorController != null) {
                // Get the states
                _animatorParameters = new string[_cockpitElement.EditorAnimator.parameterCount];
                for (int i = 0; i < _animatorParameters.Length; i++) {
                    _animatorParameters[i] = _cockpitElement.EditorAnimator.GetParameter(i).name;
                }
            }

            _statesInfo = new List<StateInfo>();
            if (_cockpitElement.EditorStates.Count > 0) {
                foreach (var state in _cockpitElement.EditorStates) {
                    var stateInfo = new StateInfo() {
                        followRotation = state.rotation == _cockpitElement.transform.localRotation,
                        parameterIndex = -1,
                    };
                    if(_animatorParameters != null && !string.IsNullOrEmpty(state.animatorParameterName)) {
                        for (int i = 0; i < _animatorParameters.Length; i++) {
                            if(_animatorParameters[i] == state.animatorParameterName) {
                                stateInfo.parameterIndex = i;
                            }
                        }
                    }
                    _statesInfo.Add(stateInfo);
                }
            }
        }

        public override void OnInspectorGUI() {
            //WeavrGUILayout.PropertyPathPopup(target, null, false, true);
            if(targets.Length > 1) {
                EditorGUILayout.LabelField("Multiple objects cannot be edited");
                if (!Application.isPlaying) {
                    DrawFooter(targets as IEnumerable<CockpitElement>, true);
                }
            }

            //Rect pos = EditorGUILayout.GetControlRect(GUILayout.Height(PropertyWrapper.GetHeight(EditorGUIUtility.singleLineHeight)));
            ////pos.x = 0;
            ////pos.y = EditorGUILayout.g
            ////pos.width = EditorGUIUtility.currentViewWidth;

            BindingDrawer.DrawBinding(_cockpitElement.Binding, true);

            if (!_cockpitElement.EditorEditableStates) {
                base.OnInspectorGUI();

                DrawFooter(false);
                return;
            }

            if (_cockpitElement.EditorStates.Count == 0) {
                GenerateStates(_cockpitElement);
            }

            _cockpitElement.StateChangeTime = EditorGUILayout.Slider("State Change Time: ", _cockpitElement.StateChangeTime, 0.1f, 5f);

            Enum initialState = _cockpitElement.EditorEnumState;
            if(initialState != null) {
                _cockpitElement.EditorEnumState = EditorGUILayout.EnumPopup("Start State: ", initialState);
            }

            _showStates = EditorGUILayout.Foldout(_showStates, "States");

            if (_showStates) {
                for (int i = 0; i < _cockpitElement.EditorStates.Count; i++) {
                    // Draw each state here
                    DrawState(i);
                }
            }

            if (!Application.isPlaying) {
                DrawFooter(true);
            }
        }

        protected virtual bool DrawFooter(bool withAnimator) {
            bool didNotMove = _originalRotation == _cockpitElement.transform.rotation && _originalPosition == _cockpitElement.transform.position;
            if (_cockpitElement.GetComponent<Collider>() != null && (!withAnimator || _cockpitElement.GetComponent<Animator>() != null)
                && didNotMove) {
                return false;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            if (!didNotMove && GUILayout.Button("Restore Transform")) {
                _cockpitElement.transform.position = _originalPosition;
                _cockpitElement.transform.rotation = _originalRotation;
            }
            DrawGenerators(_cockpitElement, withAnimator);
            return true;
        }

        protected virtual bool DrawFooter(IEnumerable<CockpitElement> elements, bool withAnimator) {
            List<CockpitElement> testElements = new List<CockpitElement>();
            foreach (var element in elements) {
                var animator = element.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null) {
                    testElements.Add(element);
                }
            }

            bool missingAnimators = testElements.Count > 0;
            if(missingAnimators) {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
                var animatorController = EditorGUILayout.ObjectField("Add Animator: ", null, typeof(AnimatorController), true) as AnimatorController;
                if (animatorController != null) {
                    foreach (var element in testElements) {
                        var animator = element.GetComponent<Animator>();
                        if(animator == null) {
                            animator = element.gameObject.AddComponent<Animator>();
                        }
                        animator.runtimeAnimatorController = animatorController;
                    }
                }
            }

            testElements.Clear();
            foreach (var element in elements) {
                if (element.GetComponent<Collider>() == null) {
                    testElements.Add(element);
                }
            }
            
            if(testElements.Count > 0) {
                if (!missingAnimators) {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

                    if (GUILayout.Button("Generate Colliders")) {
                        foreach (var element in testElements) {
                            GenerateBoxCollider(element.gameObject, minBoxColliderSide, true);
                        }
                    }
                }

                testElements.Clear();
            }
            return true;
        }

        protected virtual void DrawGenerators(CockpitElement cockpitElement, bool withAnimator) {
            if (withAnimator) {
                var animator = cockpitElement.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null) {
                    var animatorController = EditorGUILayout.ObjectField("Add Animator: ", null, typeof(RuntimeAnimatorController), true) as RuntimeAnimatorController;
                    if (animatorController != null) {
                        if (animator == null) {
                            animator = cockpitElement.gameObject.AddComponent<Animator>();
                        }
                        animator.runtimeAnimatorController = animatorController;
                    }
                }
            }
            if (cockpitElement.GetComponent<Collider>() == null && GUILayout.Button("Generate Collider")) {
                GenerateBoxCollider(cockpitElement.gameObject, minBoxColliderSide, true);
            }
        }

        protected BoxCollider GenerateBoxCollider(GameObject gameObject, float minSideLength, bool isTrigger) {
            var boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = isTrigger;
            boxCollider.size = new Vector3(Mathf.Max(boxCollider.size.x, minSideLength),
                                           Mathf.Max(boxCollider.size.y, minSideLength),
                                           Mathf.Max(boxCollider.size.z, minSideLength));

            return boxCollider;
        }

        protected virtual void DrawState(int index) {
            var state = _cockpitElement.EditorStates[index];
            EditorGUILayout.BeginVertical(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight * 3));
            {
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(state.state, EditorStyles.boldLabel, GUILayout.MaxWidth(60));
                    if (_cockpitElement.EditorAnimator != null && _cockpitElement.EditorAnimator.runtimeAnimatorController != null) {
                        EditorGUIUtility.labelWidth = 100;
                        state.useAnimator = EditorGUILayout.Toggle("Animator State: ", state.useAnimator);
                    }
                    EditorGUIUtility.labelWidth = 50;
                    state.Value = WeavrGUILayout.ValueField("Value:", state.Value, _cockpitElement.Binding.type);
                }
                EditorGUILayout.EndHorizontal();
                DrawStepInternals(index, state);
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
            EditorGUILayout.EndVertical();
        }

        protected virtual void DrawStepInternals(int index, ElementState state) {
            if (state.useAnimator) {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUIUtility.labelWidth = 60;
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("Parameter: ");
                    var animator = _cockpitElement.GetComponent<Animator>();
                    if (_animatorParameters == null || _animatorParameters.Length == 0) {
                        var
                        _animatorParameters = new string[animator.parameterCount];
                        for (int i = 0; i < _animatorParameters.Length; i++) {
                            _animatorParameters[i] = animator.GetParameter(i).name;
                        }
                    }
                    if (_animatorParameters.Length > 0) {
                        int stateIndex = EditorGUILayout.Popup(_statesInfo[index].parameterIndex, _animatorParameters);
                        if (stateIndex != _statesInfo[index].parameterIndex) {
                            _statesInfo[index].parameterIndex = stateIndex;
                            state.animatorParameterName = animator.GetParameter(stateIndex).name;
                            state.parameter = new Common.AnimatorParameter(animator.GetParameter(stateIndex));
                        }
                        if (stateIndex >= 0 && state.parameter == null) {
                            state.animatorParameterName = animator.GetParameter(stateIndex).name;
                            state.parameter = new Common.AnimatorParameter(animator.GetParameter(stateIndex));
                        }
                        if (state.parameter != null) {
                            switch (state.parameter.type) {
                                case AnimatorControllerParameterType.Bool:
                                    state.parameter.boolValue = EditorGUILayout.Toggle(state.parameter.boolValue);
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    state.parameter.numericValue = EditorGUILayout.IntField((int)(state.parameter.numericValue));
                                    break;
                                case AnimatorControllerParameterType.Float:
                                    state.parameter.numericValue = EditorGUILayout.FloatField(state.parameter.numericValue);
                                    break;
                                default:
                                    EditorGUILayout.LabelField("Trigger");
                                    break;
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUIUtility.labelWidth = 60;
                    GUILayout.Space(20);
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.BeginHorizontal();
                        state.position = EditorGUILayout.Vector3Field("Position: ", state.position);
                        if (GUILayout.Button("x", GUILayout.MaxWidth(20))) {
                            state.position = Vector3.zero;
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        var euler = EditorGUILayout.Vector3Field("Rotation: ", state.rotation.eulerAngles);
                        if (euler != state.rotation.eulerAngles) {
                            state.rotation.eulerAngles = euler;
                            _statesInfo[index].followRotation = false;
                        }
                        //else if (_statesInfo[index].followRotation) {
                        //    state.rotation = _cockpitElement.transform.localRotation;
                        //}
                        if (GUILayout.Button("x", GUILayout.MaxWidth(20))) {
                            state.rotation = _cockpitElement.transform.localRotation;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    if (GUILayout.Button("Set", GUILayout.ExpandHeight(true))) {
                        Undo.RecordObject(_cockpitElement, "Set pose");
                        state.position = _cockpitElement.transform.localPosition;
                        state.rotation = _cockpitElement.transform.localRotation;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        protected virtual void GenerateStates(CockpitElement element) {
            foreach (var e in Enum.GetValues(element.EditorEnumState.GetType())){
                element.EditorStates.Add(new ElementState() {
                    state = e.ToString(),
                    rotation = element.transform.localRotation
                });

                _statesInfo.Add(new StateInfo() {
                    followRotation = true,
                    parameterIndex = _animatorParameters != null && _animatorParameters.Length > 0 ? 0 : -1,
                });
            }
        }

        public class StateInfo {
            public bool followRotation;
            public int parameterIndex;
        }

    }
}