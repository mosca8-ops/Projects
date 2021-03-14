using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(VisualInspectCondition))]
    class VisualInpsectionConditionEditor : ConditionEditor
    {
        private const float k_minSqrBoundsSize = 0.02f * 0.02f;
        private static readonly Color k_activeBoundsColor = new Color(1f, 0f, 0f, 0.6f);
        private static readonly Color k_inactiveBoundsColor = new Color(1f, 0f, 0f, 0.3f);
        private static readonly Color k_disabledBoundsColor = new Color(1f, 0f, 0f, 0.1f);

        private VisualInspectCondition m_condition;
        private bool m_showPreview;
        private bool m_showBounds;
        private bool m_showMarker;
        private float m_baseHeight;

        private GameObject m_lastTarget;
        private Renderer m_renderer;
        private Collider m_collider;

        private Renderer m_boundsVis;
        private bool m_useCustomBounds;

        private IVisualMarker m_marker;
        private Transform m_markerTransform;

        private Tool m_prevTool;

        private bool ShowBounds
        {
            get => m_showBounds;
            set
            {
                if (m_showBounds != value)
                {
                    m_showBounds = value;
                    if (m_showBounds)
                    {
                        //m_showMarker = false;
                        SetBoundVisColor(k_activeBoundsColor);
                        m_prevTool = UnityEditor.Tools.current;
                        UnityEditor.Tools.current = Tool.None;
                        SceneView.duringSceneGui -= OnSceneGUI;
                        SceneView.duringSceneGui += OnSceneGUI;
                    }
                    else if(!m_showMarker)
                    {
                        SetBoundVisColor(k_inactiveBoundsColor);
                        UnityEditor.Tools.current = m_prevTool;
                        SceneView.duringSceneGui -= OnSceneGUI;
                    }
                }
            }
        }

        private bool ShowMarker
        {
            get => m_showMarker;
            set
            {
                if (m_showMarker != value)
                {
                    m_showMarker = value;
                    if (m_showMarker)
                    {
                        //m_showBounds = false;
                        //SetBoundVisColor(UseCustomBounds ? k_inactiveBoundsColor : k_disabledBoundsColor);
                        m_prevTool = UnityEditor.Tools.current;
                        UnityEditor.Tools.current = Tool.None;
                        SceneView.duringSceneGui -= OnSceneGUI;
                        SceneView.duringSceneGui += OnSceneGUI;
                    }
                    else if(!m_showBounds)
                    {
                        UnityEditor.Tools.current = m_prevTool;
                        SceneView.duringSceneGui -= OnSceneGUI;
                    }
                }
            }
        }

        private void SetBoundVisColor(Color color)
        {
            if (m_boundsVis)
            {
                if (Application.isPlaying)
                {
                    m_boundsVis.material.color = color;
                }
                else
                {
                    m_boundsVis.sharedMaterial.color = color;
                }
            }
        }

        private GameObject Target
        {
            get => m_lastTarget;
            set
            {
                if(m_lastTarget != value)
                {
                    m_lastTarget = value;
                    serializedObject.Update();
                    if (m_lastTarget)
                    {
                        Bounds = GetTargetBounds(m_lastTarget);
                    }
                    ResetMarkerPoseToBounds(Bounds);
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        protected Bounds Bounds
        {
            get => serializedObject.FindProperty("m_bounds").boundsValue;
            set => serializedObject.FindProperty("m_bounds").boundsValue = value;
        }

        protected bool DistanceEnabled => serializedObject.FindProperty("m_distance").FindPropertyRelative(nameof(Optional.enabled)).boolValue;
        protected bool CanEditMarker => serializedObject.FindProperty("m_showMarker").FindPropertyRelative(nameof(Optional.enabled)).boolValue;
        protected bool UseCustomBounds
        {
            get => m_useCustomBounds;
            set
            {
                if(m_useCustomBounds != value)
                {
                    m_useCustomBounds = value;
                    SetBoundVisColor(m_useCustomBounds ? k_inactiveBoundsColor : k_disabledBoundsColor);
                }
            }
        }

        protected float Distance
        {
            get => serializedObject.FindProperty("m_distance").FindPropertyRelative("value").floatValue;
            set => serializedObject.FindProperty("m_distance").FindPropertyRelative("value").floatValue = value;
        }

        protected Vector3 MarkerPosition
        {
            get => serializedObject.FindProperty("m_markerPosition").vector3Value;
            set => serializedObject.FindProperty("m_markerPosition").vector3Value = value;
        }

        protected Quaternion MarkerRotation
        {
            get => serializedObject.FindProperty("m_markerRotation").quaternionValue;
            set => serializedObject.FindProperty("m_markerRotation").quaternionValue = value;
        }

        protected Vector3 MarkerScale
        {
            get => serializedObject.FindProperty("m_markerScale").vector3Value;
            set => serializedObject.FindProperty("m_markerScale").vector3Value = value;
        }

        protected void ResetMarkerPose()
        {
            MarkerPosition = Vector3.zero;
            MarkerRotation = Quaternion.identity;
            MarkerScale = Vector3.one;
        }

        protected void ResetMarkerPoseToBounds(Bounds bounds)
        {
            MarkerPosition = bounds.center;
            MarkerRotation = Quaternion.identity;
            MarkerScale = bounds.size;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_condition = target as VisualInspectCondition;
            m_lastTarget = m_condition.Target as GameObject;
            m_useCustomBounds = m_condition.UseCustomBounds;
            ValidateBoundsAndMarker();
        }

        protected override void OnDisable()
        {
            EditorApplication.update -= RefreshPreview;
            Undo.undoRedoPerformed -= UndoPerformed;
            if (m_condition)
            {
                if (m_markerTransform)
                {
                    m_showPreview = false;
                    DestroyPreview();
                }
                ShowBounds = false;
                ShowMarker = false;
            }
            base.OnDisable();
        }

        public override void Draw(Rect rect)
        {
            if (m_condition && (UseCustomBounds || CanEditMarker) && m_condition.Target)
            {
                rect.height = m_baseHeight;
                EditorGUIUtility.labelWidth += 50;
                base.Draw(rect);
                UseCustomBounds = m_condition.UseCustomBounds;
                Target = m_condition.Target as GameObject;
                EditorGUIUtility.labelWidth -= 50;
                rect.y += m_baseHeight - HeaderHeight + EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                DrawMiniPreview(rect);
            }
            else
            {
                if (m_showPreview)
                {
                    ShowMarker = ShowBounds = m_showPreview = false;
                    EditorApplication.update -= RefreshPreview; 
                    Undo.undoRedoPerformed -= UndoPerformed;
                    DestroyPreview();
                }
                EditorGUIUtility.labelWidth += 50;
                base.Draw(rect);
                Target = m_condition.Target as GameObject;
                EditorGUIUtility.labelWidth -= 50;
            }
        }

        protected override float GetHeightInternal()
        {
            if (m_condition && (UseCustomBounds || CanEditMarker) && m_condition.Target)
            {
                m_baseHeight = base.GetHeightInternal();
                return m_baseHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            return base.GetHeightInternal();
        }

        private void DestroyPreview()
        {
            if (m_markerTransform)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_markerTransform.gameObject);
                }
                else
                {
                    DestroyImmediate(m_markerTransform.gameObject);
                }
                m_marker = null;
                m_markerTransform = null;
            }
            if (m_boundsVis)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_boundsVis.sharedMaterial);
                    Destroy(m_boundsVis.material);
                    Destroy(m_boundsVis.gameObject);
                }
                else
                {
                    DestroyImmediate(m_boundsVis.sharedMaterial);
                    DestroyImmediate(m_boundsVis.gameObject);
                }
            }
        }

        private bool ReapplyPreview()
        {
            if (m_marker != null || m_markerTransform || m_boundsVis)
            {
                DestroyPreview();
            }

            var sample = m_condition.Marker ?? (VisualInspectionPool.Current ? VisualInspectionPool.Current.DefaultSample : null);

            if (sample != null && sample is Component c)
            {
                var previewMarker = Instantiate(c);
                previewMarker.gameObject.SetActive(true);
                previewMarker.gameObject.hideFlags = m_condition.MarkerIsVisibleInHierarchy ? HideFlags.DontSave : HideFlags.HideAndDontSave;

                foreach (var renderer in previewMarker.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = new Material(renderer.sharedMaterial);
                }

                m_marker = previewMarker as IVisualInspectionMarker;
                m_markerTransform = previewMarker.transform;
            }
            else
            {
                Debug.Log("[VisualInspectCondition]: Unable to preview a null VisualInspectorMarker Sample");
            }

            m_boundsVis = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<Renderer>();
            m_boundsVis.gameObject.name = "TEMP_VisualInspectionBoundsVIS";
            m_boundsVis.gameObject.hideFlags = HideFlags.HideAndDontSave;
            if (m_boundsVis.GetComponent<Collider>())
            {
                if (Application.isPlaying)
                {
                    Destroy(m_boundsVis.GetComponent<Collider>());
                }
                else
                {
                    DestroyImmediate(m_boundsVis.GetComponent<Collider>());
                }
            }
            m_boundsVis.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_boundsVis.receiveShadows = false;

            ValidateBoundsAndMarker();
            if (Target)
            {
                m_boundsVis.transform.SetParent(Target.transform, false);
            }
            m_boundsVis.transform.localPosition = Bounds.center;
            m_boundsVis.transform.localScale = Bounds.size;


            var material = new Material(m_boundsVis.sharedMaterial);
            material.MakeTransparent();
            material.color = !UseCustomBounds ? k_disabledBoundsColor : m_showBounds ? k_activeBoundsColor : k_inactiveBoundsColor;
            m_boundsVis.sharedMaterial = material;
            if (Application.isPlaying)
            {
                m_boundsVis.material = material;
            }

            SetMarkerTarget();
            RefreshPreview();

            return true;
        }

        private void ValidateBoundsAndMarker()
        {
            serializedObject.Update();
            if (Bounds.size.sqrMagnitude < k_minSqrBoundsSize)
            {
                Bounds = GetTargetBounds(Target);
                if (Bounds.size.sqrMagnitude < k_minSqrBoundsSize)
                {
                    Bounds = new Bounds(Bounds.center, new Vector3(0.1f, 0.1f, 0.1f));
                }
            }
            if (MarkerScale.magnitude < 0.0001f)
            {
                ResetMarkerPoseToBounds(Bounds);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void SetMarkerTarget()
        {
            if (m_markerTransform && Target && SceneView.lastActiveSceneView?.camera)
            {
                if (m_condition.MarkerColor.enabled)
                {
                    m_marker.Color = m_condition.MarkerColor.value;
                }
                if (m_condition.MarkerLookAtInspector.enabled)
                {
                    m_marker.LookAtInspector = m_condition.MarkerLookAtInspector.value;
                }
                if (m_condition.MarkerText.enabled)
                {
                    m_marker.Text = m_condition.MarkerText.value;
                }
                m_marker.SetTarget(Target, m_condition.MarkerPose);
                SceneView.RepaintAll();
            }
        }

        protected void DrawMiniPreview(Rect r)
        {
            r.x += r.width - 60;
            r.width = 60;
            var showScenePreview = GUI.Toggle(r, m_showPreview, "Preview", m_showPreview ? EditorStyles.miniButtonRight : EditorStyles.miniButton);

            if (showScenePreview != m_showPreview)
            {
                m_showPreview = showScenePreview && (UseCustomBounds || CanEditMarker);
                ShowBounds &= m_showPreview;
                ShowMarker &= m_showPreview;

                ValidateBoundsAndMarker();

                if (m_showPreview)
                {
                    EditorApplication.update -= RefreshPreview;
                    EditorApplication.update += RefreshPreview;
                    Undo.undoRedoPerformed -= UndoPerformed;
                    Undo.undoRedoPerformed += UndoPerformed;
                    m_showPreview = ReapplyPreview();
                }
                if (!m_showPreview)
                {
                    EditorApplication.update -= RefreshPreview;
                    Undo.undoRedoPerformed -= UndoPerformed;
                    DestroyPreview();
                }
            }

            if (!m_showPreview) {
                return;
            }

            bool wasEnable = GUI.enabled;
            ShowBounds &= GUI.enabled = m_condition.Target && UseCustomBounds;

            if (UseCustomBounds)
            {
                r.x -= r.width;
                ShowBounds = GUI.Toggle(r, m_showBounds, "Bounds", EditorStyles.miniButtonMid);
            }
            else
            {
                SetBoundVisColor(k_disabledBoundsColor);
            }

            GUI.enabled = m_condition.Target;

            if (CanEditMarker)
            {
                ShowMarker &= GUI.enabled;
                r.x -= r.width;
                ShowMarker = GUI.Toggle(r, m_showMarker, "Marker", EditorStyles.miniButtonMid);
            }

            r.x -= r.width;
            if (GUI.Button(r, "Reset", EditorStyles.miniButtonLeft))
            {
                DestroyPreview();
                serializedObject.Update();

                Bounds = GetTargetBounds(m_lastTarget);
                ResetMarkerPoseToBounds(Bounds);

                serializedObject.ApplyModifiedProperties();

                if (m_showPreview)
                {
                    ReapplyPreview();
                }
            }

            GUI.enabled = wasEnable;
        }

        private void UndoPerformed()
        {
            SetMarkerTarget();
        }

        private void RefreshPreview()
        {
            if (m_markerTransform && Target && SceneView.lastActiveSceneView?.camera)
            {
                if (m_condition.MarkerColor.enabled)
                {
                    m_marker.Color = m_condition.MarkerColor.value;
                }
                if (m_condition.MarkerLookAtInspector.enabled)
                {
                    m_marker.LookAtInspector = m_condition.MarkerLookAtInspector.value;
                }
                if (m_condition.MarkerText.enabled)
                {
                    m_marker.Text = m_condition.MarkerText.value;
                }
                (m_marker as AbstractInspectionMarker)?.OnUpdate(SceneView.lastActiveSceneView?.camera.transform);
            }

            ValidateBoundsAndMarker();
            //if (Target && m_boundsVis)
            //{
            //    m_boundsVis.transform.localPosition = Bounds.center;
            //    m_boundsVis.transform.localScale = Bounds.size;
            //}
        }

        protected Bounds GetTargetBounds(GameObject target)
        {
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f));
            if (!target) { return bounds; }
            var meshFilters = target.GetComponentsInChildren<MeshFilter>().Where(r => r.sharedMesh).ToArray();
            if (meshFilters.Length > 0)
            {
                bounds = meshFilters[0].sharedMesh.bounds;
                bounds.center = target.transform.InverseTransformPoint(meshFilters[0].transform.TransformPoint(bounds.center));
                bounds.size = target.transform.InverseTransformVector(meshFilters[0].transform.TransformVector(bounds.size)).Abs();
                for (int i = 1; i < meshFilters.Length; i++)
                {
                    var rbounds = meshFilters[i].sharedMesh.bounds;
                    rbounds.center = target.transform.InverseTransformPoint(meshFilters[i].transform.TransformPoint(rbounds.center));
                    rbounds.size = target.transform.InverseTransformVector(meshFilters[i].transform.TransformVector(rbounds.size)).Abs();
                    bounds.Encapsulate(rbounds);
                }
            }
            else
            {
                var collider = target.GetComponentInChildren<Collider>();
                if (collider)
                {
                    if (collider is BoxCollider boxCollider)
                    {
                        bounds.center = target.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center));
                        bounds.size = target.transform.InverseTransformPoint(boxCollider.transform.TransformPoint(boxCollider.center)).Abs();
                    }
                    else
                    {
                        bounds = collider.bounds;
                        bounds.center = target.transform.InverseTransformPoint(bounds.center);
                        bounds.size = target.transform.InverseTransformVector(bounds.size).Abs();
                    }
                }
            }

            return bounds;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!Target) { return; }
            
            if (ShowBounds)
            {
                DrawBoundsHandles(Target);
            }
            else if (ShowMarker)
            {
                DrawMarkerHandles(Target);
            }
        }

        private void DrawBoundsHandles(GameObject targetGo)
        {
            if (!m_boundsVis) { return; }

            var currentTool = UnityEditor.Tools.current;
            m_prevTool = currentTool == Tool.Move || currentTool == Tool.Scale ? currentTool : m_prevTool;
            UnityEditor.Tools.current = Tool.None;

            var prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(Target.transform.position, m_boundsVis.transform.rotation, Vector3.one);

            serializedObject.Update();
            var position = m_boundsVis.transform.localPosition;
            var scale = m_boundsVis.transform.localScale;
            var rotation = UnityEditor.Tools.pivotRotation == PivotRotation.Global ? Quaternion.Inverse(Target.transform.rotation) : Quaternion.identity;

            EditorGUI.BeginChangeCheck();
            switch (m_prevTool)
            {
                case Tool.Scale:
                    scale = Handles.ScaleHandle(scale.sqrMagnitude < k_minSqrBoundsSize ? new Vector3(0.05f, 0.05f, 0.05f) : scale, position, rotation, HandleUtility.GetHandleSize(position));
                    break;
                default:
                    position = Handles.PositionHandle(position, rotation);
                    break;
            }

            if (DistanceEnabled)
            {
                float minBoundsSize = 0;// Mathf.Min(boundsSize.x, boundsSize.y, boundsSize.z);
                Distance = Handles.RadiusHandle(rotation, position, Distance + minBoundsSize) - minBoundsSize;
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (m_showMarker)
                {
                    MarkerPosition += position - m_boundsVis.transform.localPosition;
                    MarkerScale += scale - m_boundsVis.transform.localScale;
                }
                m_boundsVis.transform.localPosition = position;
                m_boundsVis.transform.localScale = scale;
                Bounds = new Bounds(position, scale);
                serializedObject.ApplyModifiedProperties();
                if (m_showMarker)
                {
                    SetMarkerTarget();
                    RefreshPreview();
                }
            }

            Handles.matrix = prevMatrix;
        }

        private void DrawMarkerHandles(GameObject targetGo)
        {
            var currentTool = UnityEditor.Tools.current;
            m_prevTool = currentTool == Tool.Move || currentTool == Tool.Rotate || currentTool == Tool.Scale ? currentTool : m_prevTool;
            UnityEditor.Tools.current = Tool.None;

            var prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(Target.transform.position, Target.transform.rotation, Vector3.one);
            serializedObject.Update();
            if(MarkerScale.sqrMagnitude < 0.000001f)
            {
                ResetMarkerPoseToBounds(Bounds);
            }
            var markerPosition = MarkerPosition;
            var markerScale = MarkerScale;
            var markerRotation = UnityEditor.Tools.pivotRotation == PivotRotation.Global ? Quaternion.Inverse(Target.transform.rotation) : MarkerRotation;
            if(markerRotation.x == 0 && markerRotation.y == 0 && markerRotation.z == 0 && markerRotation.w == 0)
            {
                markerRotation = Quaternion.identity;
            }
            EditorGUI.BeginChangeCheck();
            switch (m_prevTool)
            {
                case Tool.Rotate:
                    MarkerRotation = Handles.RotationHandle(MarkerRotation, markerPosition);
                    break;
                case Tool.Scale:
                    MarkerScale = Handles.ScaleHandle(markerScale.sqrMagnitude < k_minSqrBoundsSize ? new Vector3(0.05f, 0.05f, 0.05f) : markerScale, markerPosition, markerRotation, HandleUtility.GetHandleSize(markerPosition));
                    if(MarkerScale.sqrMagnitude < k_minSqrBoundsSize)
                    {
                        MarkerScale = markerScale;
                    }
                    break;
                default:
                    MarkerPosition = Handles.PositionHandle(markerPosition, markerRotation);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SetMarkerTarget();
                RefreshPreview();
            }
            Handles.matrix = prevMatrix;
        }
    }
}