namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    [StateDrawer(typeof(PushState))]
    public class PushStateDrawer : BaseDiscreteStateDrawer
    {
        private PushState _pushState;

        protected bool _showAdditionalInfo;
        protected bool _showEvents;
        protected float _eventsHeight;

        protected SerializedObject _serializedObject;

        public override void SetTargets(params BaseState[] targets) {
            base.SetTargets(targets);
            _lineSkipHeight = EditorGUIUtility.singleLineHeight + 2;
            _pushState = (PushState)Target;
            _serializedObject = new SerializedObject(_pushState);

            _handleColor = Color.magenta;
        }

        public override void OnInspectorGUI(Rect rect) {
            base.OnInspectorGUI(rect);

            // Draw trigger zone
            _pushState.hasTriggerState = EditorGUI.ToggleLeft(_rects[0], "Trigger", _pushState.hasTriggerState);
            if (_pushState.hasTriggerState) {
                _pushState.triggerState = WeavrGUI.Popup(_pushState, _rects[1], _pushState.triggerState, _pushState.Owner.States, s => s.Name);
            }
            else {
                EditorGUI.LabelField(_rects[1], "Triggered from any state", EditorStyles.centeredGreyMiniLabel);
            }
            if (!_pushState.Owner.UseAnimator) {
                // Build Advanced info
                MoveDownOneLine();
                _showAdditionalInfo = EditorGUI.Foldout(_rects[2], _showAdditionalInfo, "Advanced");
                if (_showAdditionalInfo) {
                    DrawAdvancedInfo();
                }
            }
            
            // Build the event
            MoveDownOneLine();
            _showEvents = EditorGUI.Foldout(_rects[2], _showEvents, "Events");
            if (_showEvents) {
                DrawEvents();
            }
        }

        private void DrawEvents() {
            SerializedProperty eventProperty = _serializedObject.FindProperty("onPush");

            if (eventProperty != null) {
                EditorGUILayout.PropertyField(eventProperty);
                EditorGUI.GetPropertyHeight(eventProperty);
            }
            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawAdvancedInfo() {
            // Change sizes
            float newLabelWidth = 90;
            EditorGUIUtility.labelWidth = newLabelWidth;
            _rects[0].width = newLabelWidth;
            _rects[1].x = _rects[0].x + _rects[0].width + _separatorWidth;
            _rects[1].width = _rects[2].width - _rects[0].width - _separatorWidth;

            MoveDownOneLine();

            bool wasEnabled = GUI.enabled;


            _pushState.isStable = EditorGUI.ToggleLeft(_rects[0], "Is Stable", _pushState.isStable);
            GUI.enabled = _pushState.HasValue;
            _pushState.continuosValue = EditorGUI.ToggleLeft(_rects[1], "Continuosly Update Value", _pushState.continuosValue);
            GUI.enabled = wasEnabled;

            MoveDownOneLine();

            EditorGUI.LabelField(_rects[0], "Move Time");
            _pushState.moveTime = EditorGUI.Slider(_rects[1], _pushState.moveTime, 0, 2);

            MoveDownOneLine();

            EditorGUI.LabelField(_rects[0], "Delta Position");
            Rect vectorRect = _rects[1];
            Rect buttonRect = _rects[1];
            vectorRect.width -= 40;
            buttonRect.width = 36;
            buttonRect.x += vectorRect.width + 4;
            _pushState.deltaPosition = EditorGUI.Vector3Field(vectorRect, "", _pushState.deltaPosition);
            if (GUI.Button(buttonRect, "Set")) {
                Undo.RecordObject(_pushState, "Moved button reach position");
                _pushState.deltaPosition = _pushState.Owner.transform.localPosition - _pushState.Owner.defaultLocalPosition;
            }
        }

        public override float GetHeight() {
            float additionalInfoHeight = _showAdditionalInfo ? base.GetHeight() + _lineSkipHeight * 5 : base.GetHeight() + _lineSkipHeight * 2;
            float eventsHeight = _showEvents ? _lineSkipHeight * 2 : _lineSkipHeight;
            return _pushState.Owner.UseAnimator ? base.GetHeight() + _lineSkipHeight + eventsHeight : additionalInfoHeight + eventsHeight;
        }

        public override void OnSceneGUI() {
            Color lastColor = Handles.color;
            
            var lastMatrix = Handles.matrix;
            var bounds = GetBounds(_pushState.Owner);
            
            Handles.matrix = Matrix4x4.TRS(_pushState.Owner.transform.position, _pushState.Owner.transform.rotation * _pushState.Owner.defaultLocalRotation, _pushState.Owner.transform.lossyScale);
            var globalDefaultPosition = _pushState.Owner.defaultLocalPosition - _pushState.Owner.transform.localPosition;
            globalDefaultPosition.y += bounds.max.y - _pushState.Owner.transform.position.y;
            var finalButtonPosition = globalDefaultPosition + _pushState.deltaPosition;
            var wireCubeSize = new Vector3(bounds.size.x, Mathf.Epsilon, bounds.size.z);
            Handles.color = Color.green;
            Handles.DrawWireCube(globalDefaultPosition, wireCubeSize);
            Handles.color = _handleColor;
            Handles.DrawAAPolyLine(5, globalDefaultPosition, finalButtonPosition);
            Handles.DrawWireCube(finalButtonPosition, wireCubeSize);
            var newPosition = Handles.Slider(finalButtonPosition, Vector3.up * handle_size * bounds.size.x, handle_size * bounds.size.x, Handles.CircleHandleCap, 0);
            if (newPosition != finalButtonPosition) {
                Undo.RecordObject(_pushState, "Moved button reach position");
                _pushState.deltaPosition = (newPosition - globalDefaultPosition);
            }
            if ((_pushState.Owner.transform.position - Camera.current.transform.position).sqrMagnitude < max_label_sqr_distance) {
                Handles.Label(newPosition, _pushState.Name, SceneStateText);
            }
            Handles.matrix = lastMatrix;
            Handles.color = lastColor;
        }
    }
}
