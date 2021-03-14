namespace TXT.WEAVR.Cockpit {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    [StateDrawer(typeof(BaseDiscreteState))]
    public class BaseDiscreteStateDrawer : BaseStateDrawer {
        protected const float min_bounds_size = 0.3f;   // in % wrt bounding box length
        protected const float min_bounds_size_m = 0.01f;   // in metres
        protected const float max_label_sqr_distance = 1;
        protected const float handle_size = 0.2f;   // in % wrt bounding box length
        protected const float handle_size_unscaled = 0.008f;   // in worlds dimensions

        protected float _valueFieldHeight;
        protected float _animatorParameterFieldHeight;
        protected float _baseHeight;
        
        protected BaseDiscreteState _state;

        private static GUIStyle _sceneStateText;
        protected static GUIStyle SceneStateText {
            get {
                if (_sceneStateText == null) {
                    _sceneStateText = new GUIStyle(EditorStyles.boldLabel);
                    _sceneStateText.alignment = TextAnchor.MiddleCenter;
                    Color color = Color.green;
                    //color.a = 0.7f;
                    _sceneStateText.normal.textColor = color;
                    _sceneStateText.clipping = TextClipping.Overflow;
                    _sceneStateText.fontSize = 16;
                    _sceneStateText.fixedHeight = 0;
                }
                return _sceneStateText;
            }
        }

        public override void SetTargets(params BaseState[] targets) {
            base.SetTargets(targets);
            _state = (BaseDiscreteState)Target;

            if (_state.UseOwnerEvents && _state.Owner.GetComponentInChildren<Collider>() == null) {
                WarningMessage = "No collider found for interaction";
                WarningFixAction = () => _state.Owner.gameObject.AddComponent<BoxCollider>();
            }
        }

        public override void OnInspectorGUI(Rect rect) {
            base.OnInspectorGUI(rect);

            // Check for notifications
            if(_state.UseOwnerEvents && _state.Owner.GetComponentInChildren<Collider>() == null) {
                WarningMessage = "No collider found for interaction";
                WarningFixAction = () => _state.Owner.gameObject.AddComponent<BoxCollider>();
            }

            rect.y = _rects[2].y = _rects[1].y = _rects[0].y;
            rect.height = _valueFieldHeight;
            DrawValuePart(rect);
            rect.y = _rects[0].y;
            if (_state.Owner.UseAnimator) {
                DrawAnimatorParameterPart(rect);
                rect.y = _rects[0].y;
            }
        }

        protected virtual void DrawAnimatorParameterPart(Rect rect) {
            EditorGUI.LabelField(_rects[0], "Parameter");
            var parameters = _state.Owner.animator.runtimeAnimatorController != null ? ((AnimatorController)_state.Owner.animator.runtimeAnimatorController).parameters : null;
            if(parameters == null) {
                EditorGUI.LabelField(_rects[1], "Animator not set");
                MoveDownOneLine();
                return;
            }
            Rect rightRect = _rects[1];
            rightRect.width *= 0.6f;
            var parameter = WeavrGUI.Popup(_state, rightRect, _state.AnimatorParameter.name, parameters, a => a.name);
            if(parameter != null && parameter.name != _state.AnimatorParameter.name) {
                _state.AnimatorParameter.Update(parameter);
            }
            else if(parameter == null) {
                _state.AnimatorParameter.name = null;
            }
            if (_state.AnimatorParameter.name != null) {
                rightRect.x += rightRect.width;
                rightRect.width = _rects[1].width - rightRect.width;
                switch (_state.AnimatorParameter.type) {
                    case AnimatorControllerParameterType.Bool:
                        _state.AnimatorParameter.boolValue = EditorGUI.Toggle(rightRect, _state.AnimatorParameter.boolValue);
                        break;
                    case AnimatorControllerParameterType.Int:
                        _state.AnimatorParameter.numericValue = EditorGUI.IntField(rightRect, (int)(_state.AnimatorParameter.numericValue));
                        break;
                    case AnimatorControllerParameterType.Float:
                        _state.AnimatorParameter.numericValue = EditorGUI.FloatField(rightRect, _state.AnimatorParameter.numericValue);
                        break;
                    default:
                        EditorGUI.LabelField(rightRect, "Trigger");
                        break;
                }
            }

            MoveDownOneLine();
        }

        public override float GetHeight() {
            _valueFieldHeight = GetValueFieldHeight();
            return base.GetHeight() + GetAnimatorParametrFieldHeight() + _valueFieldHeight + 2;
        }

        protected virtual float GetValueFieldHeight() {
            return EditorGUIUtility.singleLineHeight;
        }

        protected virtual float GetAnimatorParametrFieldHeight() {
            return _state.Owner.UseAnimator ? EditorGUIUtility.singleLineHeight + 2 : 0;
        }

        protected virtual void DrawValuePart(Rect rect) {
            _state.HasValue = EditorGUI.ToggleLeft(_rects[0], "Value", _state.HasValue);
            if (_state.HasValue) {
                _rects[1].height = _valueFieldHeight;
                DrawValueField(_rects[1]);
            }
            else {
                EditorGUI.LabelField(_rects[1], "Disabled", EditorStyles.centeredGreyMiniLabel);
            }
            _rects[0].y += _valueFieldHeight + 2;
            _rects[2].y = _rects[1].y = _rects[0].y;
        }

        protected override void DrawBindingSelection(Rect rect) {
            if (_state.HasValue) {
                base.DrawBindingSelection(rect);
            }
        }

        protected virtual void DrawValueField(Rect fieldRect) {
            var type = Target.EffectiveBinding.type;
            if(type == null) {
                return;
            }
            
            _state.Value = WeavrGUI.ValueField(fieldRect, _state.Value, type);
        }

        protected static Bounds GetBounds(BaseCockpitElement element) {
            // Rotate object to identity, to get the correct bounds
            var prevRotation = element.transform.rotation;
            element.transform.rotation = Quaternion.identity;

            // Get the bounds
            Bounds bounds;
            var gameObject = element.gameObject;
            var collider = gameObject.GetComponent<Collider>();
            if (collider == null) {
                collider = gameObject.GetComponentInChildren<Collider>(true);
            }
            if (collider != null) {
                bool wasEnabled = collider.enabled;
                collider.enabled = true;
                bounds = collider.bounds;
                collider.enabled = wasEnabled;
                bounds.extents = new Vector3(bounds.extents.x / element.transform.lossyScale.x,
                                        bounds.extents.y / element.transform.lossyScale.y,
                                        bounds.extents.z / element.transform.lossyScale.z);
            }
            else {
                var renderer = gameObject.GetComponent<Renderer>();
                if (renderer == null) {
                    renderer = gameObject.GetComponentInChildren<Renderer>(true);
                }
                if (renderer != null) {
                    bounds = renderer.bounds;
                    bounds.extents = new Vector3(bounds.extents.x / element.transform.lossyScale.x,
                                        bounds.extents.y / element.transform.lossyScale.y,
                                        bounds.extents.z / element.transform.lossyScale.z);
                }
                else {
                    bounds = new Bounds(gameObject.transform.position, Vector3.one * min_bounds_size);
                }
            }

            // Rotate to its previous rotation
            element.transform.rotation = prevRotation;
            return bounds;
        }
    }
}