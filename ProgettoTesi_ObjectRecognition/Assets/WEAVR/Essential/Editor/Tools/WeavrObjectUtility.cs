namespace TXT.WEAVR.Tools
{
    using System.Collections.Generic;
    using TXT.WEAVR.License;
    using UnityEditor;
    using UnityEngine;

    public class WeavrObjectUtility : EditorWindow
    {
        private static GUIStyle _foldoutBold;
        private static GUIStyle _redButton;
        private static GUIStyle _redSceneLabel;
        private static GUIStyle _greenSceneLabel;
        private static GUIStyle _whiteSceneLabel;

        private int _xSliderRotation = 9;
        private int _ySliderRotation = 9;
        private int _zSliderRotation = 9;
        private int _sliderStepSize = 5;

        private bool _showObjectUtility = false;
        private bool _showMeshUtility = false;
        private bool _showHierarchyUtility = false;

        private bool _showBBox = false;
        private bool _showLineToOrigin = false;
        private bool _showOriginDistance = false;

        private bool _freePivotPosition = false;
        private bool _freePivotRotation = false;

        private Vector3 _newPivotPosition;

        private Vector2 _scrollPosition;

        public enum RotationAxis
        {
            X,
            Y,
            Z
        }

        public enum PivotPointOrigin
        {
            Center,
            BottomCenterX,
            BottomCenterY,
            BottomCenterZ
        }

#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Utilities/WEAVR Object Utility")]
#endif
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            var mainWindow = EditorWindow.GetWindow<WeavrObjectUtility>("WEAVR Tools");
            mainWindow.minSize = new Vector2(250, 200);
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnFocus()
        {
            // Remove delegate listener if it has previously
            // been assigned.
            SceneView.duringSceneGui -= OnSceneGUI;
            // Add (or re-add) the delegate.
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDestroy()
        {
            // When the window is destroyed, remove the delegate
            // so that it will no longer do any drawing.
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_showBBox)
            {
                ShowBoundingBoxes(Selection.gameObjects);
            }
            if (_showLineToOrigin || _showOriginDistance)
            {
                ShowBoundsToOriginLine(_showLineToOrigin, _showOriginDistance, Selection.gameObjects);
            }

            //bool toolsAreHidden = Tools.hidden;
            Mesh mesh = _freePivotPosition ? GetMesh(Selection.activeGameObject) : null;
            if (mesh != null)
            {
                Tools.hidden = true;
                var selectedTransform = Selection.activeTransform;
                _newPivotPosition = Handles.PositionHandle(_newPivotPosition, selectedTransform.rotation);
                Handles.Label(_newPivotPosition, "Pivot Change", _newPivotPosition == selectedTransform.position ? _greenSceneLabel : _redSceneLabel);
            }
            else
            {
                Tools.hidden = false;
            }
            sceneView.Repaint();
        }

        private void OnGUI()
        {
            InitializeGUIStyles();

            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.position.height < 100)
            {
                EditorGUILayout.LabelField("Scene view is not active", EditorStyles.boldLabel);
                return;
            }

            bool hasSelection = Selection.gameObjects.Length > 0;
            bool singleSelection = Selection.gameObjects.Length == 1;
            bool multipleSelection = Selection.gameObjects.Length > 1;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _showBBox = GUILayout.Toggle(_showBBox, "Show Bounds", EditorStyles.toolbarButton);
            _showLineToOrigin = GUILayout.Toggle(_showLineToOrigin, "To Pivot Line", EditorStyles.toolbarButton);
            _showOriginDistance = GUILayout.Toggle(_showOriginDistance, "To Pivot Dst", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // General Info
            EditorGUILayout.HelpBox("Selected: " + Selection.gameObjects.Length, MessageType.None);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = hasSelection;
            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.LabelField("Select Children:");
            if (GUILayout.Button("First"))
            {
                SelectChildren(true);
            }

            if (GUILayout.Button("All"))
            {
                SelectChildren(false);
            }

            EditorGUILayout.EndHorizontal();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space();
            RenderObjectUtility();

            EditorGUILayout.Space();
            RenderMeshUtility(singleSelection);

            EditorGUILayout.Space();
            RenderHierarchyUtility();

            EditorGUILayout.EndScrollView();
        }

        private static void InitializeGUIStyles()
        {
            if (_foldoutBold == null)
            {
                _foldoutBold = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            if (_redButton == null)
            {
                _redButton = new GUIStyle(EditorStyles.miniButton);
                _redButton.fontStyle = FontStyle.Bold;
                _redButton.fontSize = 11;
                _redButton.wordWrap = true;
                _redButton.normal.textColor = /*_redButton.onNormal.textColor = _redButton.active.textColor = _redButton.onActive.textColor =*/ Color.red;
            }

            if (_redSceneLabel == null)
            {
                _redSceneLabel = new GUIStyle(EditorStyles.boldLabel);
                _redSceneLabel.normal.textColor = Color.red;
                _redSceneLabel.fontSize = 14;
            }

            if (_greenSceneLabel == null)
            {
                _greenSceneLabel = new GUIStyle(EditorStyles.boldLabel);
                _greenSceneLabel.normal.textColor = Color.green;
                _greenSceneLabel.fontSize = 14;
            }
        }

        private void RenderObjectUtility()
        {
            _showObjectUtility = EditorGUILayout.Foldout(_showObjectUtility, "Object Utility", _foldoutBold);
            if (_showObjectUtility)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Parent Sync:", "Transfer position data with parent"), GUILayout.Width(100));
                bool wasEnabled = GUI.enabled;
                GUI.enabled = Selection.activeTransform != null && Selection.activeTransform.parent != null;
                if (GUILayout.Button("Position"))
                {
                    TransferPositionToParent(Selection.activeTransform);
                }
                if (GUILayout.Button("Rotation"))
                {
                    TransferRotationToParent(Selection.activeTransform);
                }
                if (GUILayout.Button("Both"))
                {
                    TransferRotationToParent(Selection.activeTransform);
                }
                GUI.enabled = wasEnabled;
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RenderHierarchyUtility()
        {
            _showHierarchyUtility = EditorGUILayout.Foldout(_showHierarchyUtility, "Hierarchy Utility", _foldoutBold);
            if (_showHierarchyUtility)
            {
                if (GUILayout.Button("Wrap in Parent"))
                {
                    WrapInParent(Selection.transforms);
                }

                if (GUILayout.Button("Clean Names"))
                {
                    EscapeNames(Selection.objects);
                }
            }
        }

        private void RenderMeshUtility(bool singleSelection)
        {
            _showMeshUtility = EditorGUILayout.Foldout(_showMeshUtility, "Mesh Utility", _foldoutBold);
            if (_showMeshUtility)
            {
                bool wasEnabled = GUI.enabled;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("CAUTION: Modifying the values below will overwrite the 3D model. USE WITH CARE", MessageType.Warning);
                //if(GUILayout.Button("Make Copy", _redButton, GUILayout.Width(50), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2.2f))) {

                //}
                EditorGUILayout.EndHorizontal();

                Mesh mesh = singleSelection ? GetMesh(Selection.activeGameObject) : null;
                if (mesh != null)
                {
                    var selectedTransform = Selection.activeTransform;

                    if (selectedTransform.rotation != Quaternion.identity)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox("Object rotation is not set to identity, thus pivot free pivot change is not allowed", MessageType.None);
                        if (GUILayout.Button("Reset Rotation"))
                        {
                            selectedTransform.rotation = Quaternion.identity;
                        }
                        EditorGUILayout.EndHorizontal();

                        GUI.enabled = false;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    bool newBoolValue = GUILayout.Toggle(_freePivotPosition, "Free Position", _redButton);
                    if (newBoolValue != _freePivotPosition)
                    {
                        _freePivotPosition = newBoolValue;
                        _freePivotRotation &= !newBoolValue;

                        if (newBoolValue)
                        {
                            _newPivotPosition = selectedTransform.position;
                        }
                    }
                    if (_freePivotPosition)
                    {
                        _newPivotPosition = EditorGUILayout.Vector3Field("", _newPivotPosition);
                        if (_newPivotPosition != selectedTransform.position)
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("Apply"))
                            {
                                // Rotate to default rotation -> move the pivot -> rotate back to its rotation
                                var lastRotation = selectedTransform.rotation;
                                //selectedTransform.rotation = Quaternion.identity;
                                RepositionPivotPoint(_newPivotPosition - selectedTransform.position, Selection.activeGameObject);
                                //selectedTransform.rotation = lastRotation;
                                _newPivotPosition = selectedTransform.position;
                            }
                            if (GUILayout.Button("Cancel"))
                            {
                                _newPivotPosition = selectedTransform.position;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical();
                    newBoolValue = GUILayout.Toggle(_freePivotRotation, "Free Rotation", _redButton);
                    if (newBoolValue != _freePivotRotation)
                    {
                        _freePivotRotation = newBoolValue;
                        _freePivotPosition &= !newBoolValue;
                    }
                    if (_freePivotRotation)
                    {
                        Vector3 newPosition = EditorGUILayout.Vector3Field("", Selection.activeTransform.eulerAngles);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    GUI.enabled = !(_freePivotPosition || _freePivotRotation);
                }
                else
                {
                    _freePivotPosition = _freePivotRotation = false;
                }

                EditorGUILayout.LabelField("Rotate:");
                EditorGUI.indentLevel++;
                DrawAxisRotationButtons();
                EditorGUI.indentLevel--;


                EditorGUILayout.LabelField("Reposition:");
                EditorGUI.indentLevel++;
                if (GUILayout.Button("Center Pivot Point"))
                {
                    ResetPivotPoint(PivotPointOrigin.Center, Selection.gameObjects);
                }

                GUI.enabled &= singleSelection;
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Rebase X"))
                {
                    ResetPivotPoint(PivotPointOrigin.BottomCenterX, Selection.gameObjects);
                }
                if (GUILayout.Button("Rebase Y"))
                {
                    ResetPivotPoint(PivotPointOrigin.BottomCenterY, Selection.gameObjects);
                }
                if (GUILayout.Button("Rebase Z"))
                {
                    ResetPivotPoint(PivotPointOrigin.BottomCenterZ, Selection.gameObjects);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;

                GUI.enabled = wasEnabled;
            }
        }

        public static void TransferRotationToParent(Transform transform)
        {
            var targetChild = transform;
            var parent = targetChild.parent;
            if (parent == null) { return; }

            List<Transform> childrenToUndo = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                childrenToUndo.Add(parent.GetChild(i));
            }
            Undo.RecordObjects(childrenToUndo.ToArray(), "Rotated parent and children");
            var deltaRotation = targetChild.localRotation;
            var deltaInverseRotation = Quaternion.Inverse(deltaRotation);
            foreach (var child in childrenToUndo)
            {
                child.localRotation *= deltaInverseRotation;
            }
            parent.localRotation *= deltaRotation;
        }

        public static void TransferPositionToParent(Transform transform)
        {
            var targetChild = transform;
            var parent = targetChild.parent;
            if (parent == null) { return; }

            List<Transform> childrenToUndo = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                childrenToUndo.Add(parent.GetChild(i));
            }
            Undo.RecordObjects(childrenToUndo.ToArray(), "Repositioned parent and children");
            var deltaPosition = targetChild.localPosition;
            parent.position = targetChild.position;
            foreach (var child in childrenToUndo)
            {
                child.localPosition -= deltaPosition;
            }
        }

        public static void ShowBoundingBoxes(params GameObject[] gameObjects)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (var go in gameObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null) { continue; }
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) { continue; }
                Handles.color = Color.red;
                var lastMatrix = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(go.transform.position, go.transform.rotation, go.transform.lossyScale);
                Handles.DrawWireCube(mesh.bounds.center, mesh.bounds.size);
                Handles.matrix = lastMatrix;
            }
        }

        public static void ShowBoundsToOriginLine(bool showLine, bool showDistance, params GameObject[] gameObjects)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (var go in gameObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null) { continue; }
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) { continue; }
                var lastMatrix = Handles.matrix;
                Handles.matrix = Matrix4x4.TRS(go.transform.position, go.transform.rotation, go.transform.lossyScale);
                Handles.color = Color.white;
                if (showLine)
                {
                    Handles.DrawLine(mesh.bounds.center, Vector3.zero);
                }
                if (showDistance)
                {
                    Handles.Label(mesh.bounds.center, string.Format("{0:0000}m", mesh.bounds.center.magnitude), _greenSceneLabel ?? EditorStyles.whiteBoldLabel);
                }
                Handles.matrix = lastMatrix;
            }
        }

        public static void ResetPivotPoint(PivotPointOrigin origin, params GameObject[] gameObjects)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (var go in gameObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (!meshFilter) { continue; }
                var mesh = meshFilter.sharedMesh;
                if (!mesh) { continue; }
                Undo.RecordObject(mesh, "Reset pivot on mesh " + mesh.name);
                Vector3 newOrigin = mesh.bounds.center;
                switch (origin)
                {
                    case PivotPointOrigin.BottomCenterX:
                        newOrigin.x = mesh.bounds.min.x;
                        break;
                    case PivotPointOrigin.BottomCenterY:
                        newOrigin.y = mesh.bounds.min.y;
                        break;
                    case PivotPointOrigin.BottomCenterZ:
                        newOrigin.z = mesh.bounds.min.z;
                        break;
                }
                var offset = newOrigin;
                mesh.GetVertices(vertices);
                for (int i = 0; i < vertices.Count; i++)
                {
                    vertices[i] = vertices[i] - offset;
                }
                mesh.SetVertices(vertices);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    go.transform.GetChild(i).position -= offset;
                }
                go.transform.position += offset;
                vertices.Clear();
            }
        }

        public static Bounds GetBounds(Transform transform, float fallbackSize)
        {
            // Rotate object to identity, to get the correct bounds
            var prevRotation = transform.rotation;
            transform.rotation = Quaternion.identity;

            // Get the bounds
            Bounds bounds;
            var collider = transform.GetComponent<Collider>();
            if (collider == null)
            {
                collider = transform.GetComponentInChildren<Collider>(true);
            }
            if (collider != null)
            {
                bool wasEnabled = collider.enabled;
                collider.enabled = true;
                bounds = collider.bounds;
                collider.enabled = wasEnabled;
                bounds.extents = new Vector3(bounds.extents.x / transform.lossyScale.x,
                                        bounds.extents.y / transform.lossyScale.y,
                                        bounds.extents.z / transform.lossyScale.z);
            }
            else
            {
                var renderer = transform.GetComponent<Renderer>();
                if (renderer == null)
                {
                    renderer = transform.GetComponentInChildren<Renderer>(true);
                }
                if (renderer != null)
                {
                    bounds = renderer.bounds;
                    bounds.extents = new Vector3(bounds.extents.x / transform.lossyScale.x,
                                        bounds.extents.y / transform.lossyScale.y,
                                        bounds.extents.z / transform.lossyScale.z);
                }
                else
                {
                    bounds = new Bounds(transform.position, Vector3.one * fallbackSize);
                }
            }

            // Rotate to its previous rotation
            transform.rotation = prevRotation;
            return bounds;
        }

        private void DrawAxisRotationButtons()
        {
            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 10;
            // X
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X-Axis: ");
            float rotation = _xSliderRotation * _sliderStepSize;
            if (GUILayout.Button(string.Format(@"-{0}°", rotation)))
            {
                RotateMeshes(Selection.gameObjects, RotationAxis.X, -rotation);
            }
            _xSliderRotation = (int)GUILayout.HorizontalSlider(_xSliderRotation, 1, 90 / _sliderStepSize, GUILayout.MinWidth(50));
            if (GUILayout.Button(string.Format(@"+{0}°", rotation)))
            {
                RotateMeshes(Selection.gameObjects, RotationAxis.X, rotation);
            }
            EditorGUILayout.EndHorizontal();

            // Y
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Y-Axis: ");
            rotation = _ySliderRotation * _sliderStepSize;
            if (GUILayout.Button(string.Format(@"-{0}°", rotation)))
            {
                RotateMeshes(Selection.gameObjects, RotationAxis.Y, -rotation);
            }
            _ySliderRotation = (int)GUILayout.HorizontalSlider(_ySliderRotation, 1, 90 / _sliderStepSize, GUILayout.MinWidth(50));
            if (GUILayout.Button(string.Format(@"+{0}°", rotation)))
            {
                RotateMeshes(Selection.gameObjects, RotationAxis.Y, rotation);
            }
            EditorGUILayout.EndHorizontal();

            // Z
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Z-Axis: ");
            rotation = _zSliderRotation * _sliderStepSize;
            if (GUILayout.Button(string.Format(@"-{0}°", rotation)))
            {
                RotateMeshes(Selection.gameObjects, RotationAxis.Z, -rotation);
            }
            _zSliderRotation = (int)GUILayout.HorizontalSlider(_zSliderRotation, 1, 90 / _sliderStepSize, GUILayout.MinWidth(50));
            if (GUILayout.Button(string.Format(@"+{0}°", rotation)))
            {
                RotateMeshes(Selection.gameObjects, RotationAxis.Z, rotation);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private void SelectChildren(bool firstChildOnly)
        {
            Undo.RecordObjects(Selection.transforms, "Children Selected");
            List<GameObject> objs = new List<GameObject>();
            foreach (var transform in Selection.transforms)
            {
                if (transform.childCount == 0)
                {
                    objs.Add(transform.gameObject);
                }
                else if (firstChildOnly)
                {
                    objs.Add(transform.GetChild(0).gameObject);
                }
                else
                {
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        objs.Add(transform.GetChild(i).gameObject);
                    }
                }
            }
            Selection.objects = objs.ToArray();
        }

        public static void WrapInParent(params Transform[] transforms)
        {
            Undo.RecordObjects(transforms, "Reparented");
            foreach (var transform in transforms)
            {
                var newParent = new GameObject(transform.name + "_Parent");
                var newTransform = newParent.transform;
                newTransform.SetParent(transform.parent, false);
                newTransform.SetPositionAndRotation(transform.position, transform.rotation);
                newTransform.localScale = transform.localScale;
                transform.SetParent(newTransform, true);
            }
        }

        public static void RotateMeshes(IEnumerable<GameObject> gameObjects, RotationAxis axis, float degrees)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (var go in gameObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null) { continue; }
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) { continue; }
                Undo.RecordObject(mesh, "Rotated mesh " + mesh.name);
                Vector3 vectorAxis = go.transform.right;
                if (axis == RotationAxis.Y)
                {
                    vectorAxis = go.transform.up;
                }
                else if (axis == RotationAxis.Z)
                {
                    vectorAxis = go.transform.forward;
                }
                Quaternion rotation = Quaternion.AngleAxis(degrees, vectorAxis);
                var center = Vector3.zero;// go.transform.localPosition;
                mesh.GetVertices(vertices);
                for (int i = 0; i < vertices.Count; i++)
                {
                    vertices[i] = rotation * (vertices[i] - center) + center;
                }
                mesh.SetVertices(vertices);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                // Rotate collider as well if present
                RotateColliders(go, rotation);
                go.transform.Rotate(vectorAxis, -degrees);
                // Rotate children in the other direction
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    go.transform.GetChild(i).localPosition = rotation * go.transform.GetChild(i).localPosition;
                    go.transform.GetChild(i).Rotate(vectorAxis, degrees);
                    //go.transform.GetChild(i).RotateAround(go.transform.GetChild(i).localPosition, vectorAxis, degrees);
                }
                vertices.Clear();
            }
        }

        private static void RotateColliders(GameObject go, Quaternion rotation)
        {
            var colliders = go.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (collider is BoxCollider)
                {
                    var boxCollider = (BoxCollider)collider;
                    boxCollider.center = rotation * boxCollider.center;
                    boxCollider.size = rotation * boxCollider.size;
                }
                else if (collider != null)
                {
                    // Create serializer to copy values to new collider
                    var serializedCollider = new SerializedObject(collider);
                    // Add new collider
                    var newCollider = Undo.AddComponent(go, collider.GetType());
                    var serializedNewCollider = new SerializedObject(collider);
                    // Create iterator
                    var property = serializedCollider.GetIterator();
                    // Copy from old to new
                    while (property.Next(true))
                    {
                        serializedNewCollider.CopyFromSerializedProperty(property);
                    }
                    // Destroy old
                    Undo.DestroyObjectImmediate(collider);

                    //// Get "local space" bounds
                    //var localCenter = go.transform.InverseTransformPoint(collider.bounds.center);
                    //var localMin = go.transform.InverseTransformPoint(collider.bounds.min);
                    //var localMax = go.transform.InverseTransformPoint(collider.bounds.max);

                    //// Rotate them
                    //localCenter = rotation * (localCenter - center) + center;
                    //localMin = rotation * (localMin - center) + center;
                    //localMax = rotation * (localMax - center) + center;

                    //// "Move" them back to world space
                    //collider.bounds.SetMinMax(go.transform.TransformPoint(localMin), go.transform.TransformPoint(localMax));
                }
            }
        }

        public static void AlignMeshesToTransform(Transform transform, params GameObject[] gameObjects)
        {
            float angle;
            Vector3 axis;
            List<Vector3> vertices = new List<Vector3>();
            foreach (var go in gameObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null) { continue; }
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) { continue; }
                Undo.RecordObject(mesh, "Rotated mesh " + mesh.name);
                Quaternion rotation = Quaternion.FromToRotation(go.transform.up, transform.up);
                rotation.ToAngleAxis(out angle, out axis);
                // Check if rotation is in correct "direction"
                var lastRotation = go.transform.rotation;
                go.transform.Rotate(axis, -angle);
                if (Vector3.Dot(go.transform.up, transform.up) < 0)
                {
                    // Change rotation direction
                    go.transform.rotation = lastRotation;
                    rotation = Quaternion.FromToRotation(transform.up, go.transform.up);
                    rotation.ToAngleAxis(out angle, out axis);
                }
                else
                {
                    go.transform.rotation = lastRotation;
                }

                var center = go.transform.localPosition;
                mesh.GetVertices(vertices);
                for (int i = 0; i < vertices.Count; i++)
                {
                    vertices[i] = rotation * (vertices[i] - center) + center;
                }
                mesh.SetVertices(vertices);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();

                RotateColliders(go, rotation);

                // Rotate children in the other direction
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    go.transform.GetChild(i).Rotate(axis, angle);
                }

                go.transform.Rotate(axis, -angle);
                vertices.Clear();
            }
        }

        public static void RepositionPivotPoint(Vector3 newPosition, params GameObject[] gameObjects)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (var go in gameObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null) { continue; }
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) { continue; }
                Undo.RecordObject(mesh, "Moved pivot on mesh " + mesh.name);
                Vector3 newOrigin = mesh.bounds.center;
                var offset = Quaternion.Inverse(go.transform.rotation) * newPosition;
                //go.transform.rotation = Quaternion.identity;
                mesh.GetVertices(vertices);
                for (int i = 0; i < vertices.Count; i++)
                {
                    vertices[i] = vertices[i] - offset;
                }
                mesh.SetVertices(vertices);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    go.transform.GetChild(i).position -= offset;
                }
                go.transform.position += offset;
                vertices.Clear();
            }
        }

        public static Mesh[] GetMeshes(params GameObject[] gameObjects)
        {
            List<Mesh> meshes = new List<Mesh>();
            foreach (var go in gameObjects)
            {
                var meshFilter = go.GetComponent<MeshFilter>();
                if (meshFilter == null) { continue; }
                var mesh = meshFilter.sharedMesh;
                if (mesh != null)
                {
                    meshes.Add(mesh);
                }
            }
            return meshes.ToArray();
        }

        public static Mesh GetMesh(GameObject gameObject)
        {
            if (gameObject == null) { return null; }
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            return meshFilter != null ? meshFilter.sharedMesh : null;
        }

        public static void EscapeNames(IEnumerable<UnityEngine.Object> objects)
        {
            Undo.RecordObjects((UnityEngine.Object[])objects, "Renamed objects");
            foreach (var obj in objects)
            {
                obj.name = obj.name.Replace('.', '_').Replace('/', '_').Replace('\\', '_');
            }
        }

    }
}