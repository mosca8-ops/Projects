namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    [StateDrawer(typeof(PositionState))]
    public class PositionStateDrawer : BaseDiscreteStateDrawer
    {
        private static GUIContent _deltaContent;
        
        private static Vector2 _snapVector;
        private PositionState _positionState;

        protected bool _showAdditionalInfo;

        public override void SetTargets(params BaseState[] targets) {
            WarningMessage = null;

            base.SetTargets(targets);

            if(_deltaContent == null) {
                _deltaContent = new GUIContent("Delta Pose", "Whether to use delta position and rotation or local ones");
            }
            _lineSkipHeight = EditorGUIUtility.singleLineHeight + 2;
            _positionState = (PositionState)Target;

            _handleColor = Color.magenta;
            _snapVector = new Vector2(0.005f, 0.005f);

            if(_positionState.triggerZone == null) {
                WarningMessage = "Trigger zone not set";
            }
        }

        public override void OnInspectorGUI(Rect rect) {
            base.OnInspectorGUI(rect);

            // Draw trigger zone
            EditorGUI.LabelField(_rects[0], "Trigger Zone");
            if (_positionState.triggerZone != null) {
                Rect rightRect = _rects[1];
                rightRect.width -= 60;
                var triggerZone = _positionState.triggerZone;
                _positionState.triggerZone = EditorGUI.ObjectField(rightRect, _positionState.triggerZone, typeof(StateTriggerZone), true) as StateTriggerZone;
                rightRect.x += rightRect.width;
                rightRect.width = 60;
                if (_positionState.triggerZone != triggerZone || GUI.Button(rightRect, "Delete")) {
                    // Remove the trigger zone
                    triggerZone.Delete();
                }
            }
            else {
                Rect rightRect = _rects[1];
                rightRect.width -= 60;
                _positionState.triggerZone = EditorGUI.ObjectField(rightRect, _positionState.triggerZone, typeof(StateTriggerZone), true) as StateTriggerZone;
                rightRect.x += rightRect.width;
                rightRect.width = 60;
                if(GUI.Button(rightRect, "Create")) {
                    // Create trigger zone
                    CreateTriggerZone(_positionState);
                }
            }
            
            if(_positionState.triggerZone != null) {
                WarningMessage = null;
                WarningFixAction = null;
            }
            else {
                WarningMessage = "Trigger zone not set";
                WarningFixAction = () => CreateTriggerZone(_positionState);
            }

            if (_positionState.Owner.UseAnimator) {
                return;
            }

            MoveDownOneLine();

            _showAdditionalInfo = EditorGUI.Foldout(_rects[2], _showAdditionalInfo, "Advanced");
            if(!_showAdditionalInfo) { return; }

            MoveDownOneLine();

            bool wasEnabled = GUI.enabled;
            
            bool isDelta = EditorGUI.ToggleLeft(_rects[0], _deltaContent, _positionState.isDelta);
            if(isDelta != _positionState.isDelta) {
                _positionState.isDelta = isDelta;
                if (_positionState.positionEnabled) {
                    UpdatePosition(_positionState.Owner.transform.localPosition, _positionState.position);
                }
                if (_positionState.rotationEnabled) {
                    UpdateRotation(_positionState.Owner.transform.localRotation, _positionState.rotation);
                }
            }
            EditorGUIUtility.labelWidth = 80;
            _positionState.moveTime = EditorGUI.Slider(_rects[1], "Move Time", _positionState.moveTime, 0, 4);
            EditorGUIUtility.labelWidth = _labelWidth;

            MoveDownOneLine();

            _positionState.positionEnabled = EditorGUI.ToggleLeft(_rects[0], "Position", _positionState.positionEnabled);
            GUI.enabled = _positionState.positionEnabled;
            Rect vectorRect = _rects[1];
            Rect buttonRect = _rects[1];
            vectorRect.width -= 40;
            buttonRect.width = 36;
            buttonRect.x += vectorRect.width + 4;
            _positionState.position = EditorGUI.Vector3Field(vectorRect, "", _positionState.position);
            if(GUI.Button(buttonRect, "Set")) {
                UpdatePosition(_positionState.Owner.transform.localPosition, Vector3.zero);
            }

            _rects[0].y += _lineSkipHeight;
            vectorRect.y = buttonRect.y = _rects[2].y = _rects[1].y = _rects[0].y;
            GUI.enabled = wasEnabled;
            _positionState.rotationEnabled = EditorGUI.ToggleLeft(_rects[0], "Rotation", _positionState.rotationEnabled);
            GUI.enabled = _positionState.rotationEnabled;
            _positionState.rotation.eulerAngles = EditorGUI.Vector3Field(vectorRect, "", _positionState.rotation.eulerAngles);
            if (GUI.Button(buttonRect, "Set")) {
                UpdateRotation(_positionState.Owner.transform.localRotation, Quaternion.identity);
            }
            
            GUI.enabled = wasEnabled;

            MoveDownOneLine();

            _positionState.isStable = EditorGUI.ToggleLeft(_rects[0], "Stable", _positionState.isStable);
            if (!_positionState.isStable) {
                _positionState.fallbackState = WeavrGUI.Popup(_positionState, _rects[1], "Back to", _positionState.fallbackState, _positionState.Owner.States, s => s.Name);
            }
        }

        private void UpdatePosition(Vector3 startPoint, Vector3 delta) {
            Undo.RecordObject(_positionState, "Updated position");
            _positionState.position = _positionState.isDelta
                                                    ? _positionState.Owner.transform.localPosition - startPoint
                                                    : startPoint + delta;
            Undo.FlushUndoRecordObjects();
        }

        private void UpdateRotation(Quaternion startPoint, Quaternion delta) {
            Undo.RecordObject(_positionState, "Updated rotation");
            _positionState.rotation = _positionState.isDelta
                                                    ? _positionState.Owner.defaultLocalRotation * Quaternion.Inverse(startPoint)
                                                    : startPoint * delta;
            Undo.FlushUndoRecordObjects();
        }

        public override float GetHeight() {
            return _positionState.Owner.UseAnimator ? base.GetHeight() + _lineSkipHeight : 
                (_showAdditionalInfo ? base.GetHeight() + _lineSkipHeight * 6 :  base.GetHeight() + _lineSkipHeight * 2);
        }

        protected static void CreateTriggerZone(PositionState state) {
            Bounds bounds = GetBounds(state.Owner);
            bounds.extents = new Vector3(Mathf.Max(bounds.extents.x, min_bounds_size_m),
                                        Mathf.Max(bounds.extents.y * 0.5f, min_bounds_size_m),
                                        Mathf.Max(bounds.extents.z, min_bounds_size_m));

            // Check if sibling with triggers exists
            Transform triggers = null;
            if(state.Owner.transform.parent == null) {
                triggers = state.Owner.transform;
            }
            else {
                string triggersName = state.Owner.transform.gameObject.name + "_Triggers";
                for (int i = 0; i < state.Owner.transform.parent.childCount; i++) {
                    var child = state.Owner.transform.parent.GetChild(i);
                    if(child.gameObject.name == triggersName) {
                        triggers = child;
                        break;
                    }
                }
                if(triggers == null) {
                    // Needs to be created
                    GameObject triggersGO = new GameObject(triggersName);
                    triggers = triggersGO.transform;
                    triggers.SetParent(state.Owner.transform.parent, false);
                    triggers.localPosition = state.Owner.defaultLocalPosition;
                    triggers.localRotation = state.Owner.defaultLocalRotation;
                }
            }

            // Create game object
            GameObject triggerObj = new GameObject(state.Name + "_Trigger");
            triggerObj.transform.SetParent(triggers, false);
            triggerObj.transform.position = bounds.center;

            // Add trigger zone component
            StateTriggerZone zone = Undo.AddComponent<StateTriggerZone>(triggerObj);
            zone.state = state;
            state.triggerZone = zone;

            // Create collider
            BoxCollider collider = Undo.AddComponent<BoxCollider>(triggerObj);
            collider.isTrigger = true;
            collider.size = bounds.extents * 2;
            zone.transform.position = new Vector3(bounds.center.x, collider.bounds.min.y, bounds.center.z);

            zone.Collider = collider;
        }

        public override void OnSceneGUI() {
            if (_positionState.triggerZone == null || !(_positionState.triggerZone.Collider is BoxCollider)) {
                return;
            }
            var zone = _positionState.triggerZone;
            Color lastColor = Handles.color;
            BoxCollider collider = (BoxCollider)zone.Collider;
            float minSideLength = Mathf.Min(collider.size.x, collider.size.z) * zone.transform.lossyScale.x * 0.5f;
            float handleSizeUnscaled = handle_size * minSideLength * 2;
            //var newPosition = Handles.PositionHandle(zone.transform.position, zone.transform.rotation);
            if (Event.current.control || Event.current.command){
                Handles.color = Color.red;
                Handles.Label(zone.transform.position, "Set " + _positionState.Name, SceneStateText);
                if (Handles.Button(zone.transform.position, Quaternion.identity, minSideLength, minSideLength, Handles.RectangleHandleCap)) {
                    if (_positionState.positionEnabled) { UpdatePosition(_positionState.Owner.transform.localPosition, Vector3.zero); }
                    if (_positionState.rotationEnabled) { UpdateRotation(_positionState.Owner.transform.localRotation, Quaternion.identity); }
                }
            }
            else {
                Handles.color = _handleColor;
                var lastMatrix = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(zone.transform.position, zone.transform.rotation, zone.transform.lossyScale);
                Handles.DrawWireCube(Vector3.zero, collider.size);
                ShowResizeHandles(collider);
                Handles.matrix = lastMatrix;

                if ((zone.transform.position - Camera.current.transform.position).sqrMagnitude < max_label_sqr_distance) {
                    Handles.Label(zone.transform.position, _positionState.Name, SceneStateText);
                }
                var newPosition = Handles.Slider2D(zone.transform.position, zone.transform.up, zone.transform.right, zone.transform.forward, handleSizeUnscaled, Handles.CircleHandleCap, 0);
                if (newPosition != zone.transform.position) {
                    Undo.RecordObject(zone.transform, "Moved handle");
                    zone.transform.position = newPosition;
                }
            }
            Handles.color = lastColor;
        }

        private static void ShowResizeHandles(BoxCollider collider) {
            float colliderLengthY = collider.size.y * 0.5f;
            float colliderLengthX = collider.size.x * 0.5f;
            float scaleX = Handles.ScaleValueHandle(colliderLengthX, Vector3.right * colliderLengthX, Quaternion.identity, colliderLengthY, Handles.CubeHandleCap, 0);
            if (colliderLengthX != scaleX) {
                Undo.RecordObject(collider, "Trigger zone resized");
                collider.size = new Vector3(collider.size.x + (scaleX - colliderLengthX), collider.size.y, collider.size.z);
            }
            else {
                scaleX = Handles.ScaleValueHandle(colliderLengthX, Vector3.left * colliderLengthX, Quaternion.identity, colliderLengthY, Handles.CubeHandleCap, 0);
                if (colliderLengthX != scaleX) {
                    Undo.RecordObject(collider, "Trigger zone resized");
                    float delta = colliderLengthX - scaleX;
                    collider.size = new Vector3(collider.size.x + delta, collider.size.y, collider.size.z);
                }
            }
            float colliderLengthZ = collider.size.z * 0.5f;
            float scaleZ = Handles.ScaleValueHandle(colliderLengthZ, Vector3.forward * colliderLengthZ, Quaternion.identity, colliderLengthY, Handles.CubeHandleCap, 0);
            if (colliderLengthZ != scaleZ) {
                Undo.RecordObject(collider, "Trigger zone resized");
                collider.size = new Vector3(collider.size.x, collider.size.y, collider.size.z + (scaleZ - colliderLengthZ));
            }
            else {
                scaleZ = Handles.ScaleValueHandle(colliderLengthZ, Vector3.back * colliderLengthZ, Quaternion.identity, colliderLengthY, Handles.CubeHandleCap, 0);
                if (colliderLengthZ != scaleZ) {
                    Undo.RecordObject(collider, "Trigger zone resized");
                    collider.size = new Vector3(collider.size.x, collider.size.y, collider.size.z + (scaleZ - colliderLengthZ));
                }
            }
            float scaleY = Handles.ScaleValueHandle(colliderLengthY, Vector3.up * colliderLengthY, Quaternion.Euler(collider.transform.up), colliderLengthY, Handles.CubeHandleCap, 0);
            if (colliderLengthY != scaleY) {
                Undo.RecordObject(collider, "Trigger zone resized");
                float delta = (scaleY - colliderLengthY) * 0.5f;
                collider.size = new Vector3(collider.size.x, collider.size.y + delta, collider.size.z);
                Undo.RecordObject(collider.transform, "Trigger zone resized");
                collider.transform.localPosition = new Vector3(collider.transform.localPosition.x, 
                                                               collider.transform.localPosition.y + delta, 
                                                               collider.transform.localPosition.z);
            }
        }
    }
}
