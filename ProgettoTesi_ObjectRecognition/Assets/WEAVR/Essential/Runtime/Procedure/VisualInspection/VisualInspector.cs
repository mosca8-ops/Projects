using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("WEAVR/Procedures/Visual Inspection/Visual Inspector")]
    public class VisualInspector : MonoBehaviour, IVisualInspector
    {
        #region [  STATIC PART  ]

        private static List<VisualInspector> s_inspectors = new List<VisualInspector>();
        public static VisualInspector Main => s_inspectors.Count > 0 ? s_inspectors[s_inspectors.Count - 1] : null;

        #endregion


        [SerializeField]
        [Draggable]
        private Camera m_camera;
        [SerializeField]
        [Tooltip("Ratio of focusing field of view with respect to the camera field of view. " +
            "Value 1 gets the entire field of view of the camera when inspecting, " +
            "while value 0.05 gets only the central 5% of the field of view of the camera")]
        [Range(0.05f, 1)]
        private float m_focusFovFactor = 0.5f;
        [SerializeField]
        private bool m_isAllowedToInspect = true;
        [SerializeField]
        private bool m_debugFrustum = false;
        private float m_fovFactor;

        // Frustum planes
        private Plane[] m_frustumPlanes = new Plane[6];
        private Camera m_actualCamera;

        public float FieldOfViewFactor
        {
            get => m_fovFactor;
            set
            {
                if (m_fovFactor != value)
                {
                    m_fovFactor = m_focusFovFactor = Mathf.Clamp(value, 0.05f, 1);
                }
            }
        }


        public bool IsAllowedToInspect
        {
            get { return m_isAllowedToInspect; }
            set
            {
                if (m_isAllowedToInspect != value)
                {
                    m_isAllowedToInspect = value;
                }
            }
        }

        private void OnValidate()
        {
            if (!m_camera)
            {
                m_camera = GetComponent<Camera>();
            }
            FieldOfViewFactor = m_focusFovFactor;
        }

        private void Awake()
        {
            if (!s_inspectors.Contains(this))
            {
                s_inspectors.Add(this);
            }
        }

        private void OnDestroy()
        {
            s_inspectors.Remove(this);
        }

        void Start()
        {
            OnValidate();

            if (m_camera)
            {
                m_actualCamera = m_camera;
            }
            else
            {
                m_actualCamera = new GameObject("VisualInspector_Camera").AddComponent<Camera>();
                m_actualCamera.enabled = false;
                m_actualCamera.transform.SetParent(transform);
                m_actualCamera.transform.localPosition = Vector3.zero;
                m_actualCamera.transform.localRotation = Quaternion.identity;
                m_actualCamera.transform.localScale = Vector3.one;
            }
        }

        public bool CanSee(Bounds bounds, float maxDistance)
        {
            if (m_frustumPlanes == null || m_frustumPlanes.Length < 6)
            {
                m_frustumPlanes = new Plane[6];
            }
            UpdateFrustumPlanes(maxDistance);
            if (m_debugFrustum)
            {
                m_lastDistance = maxDistance;
                m_lastBounds = bounds;
                m_isInspecting = true;
            }
            return GeometryUtility.TestPlanesAABB(m_frustumPlanes, bounds);
        }

        private Bounds m_lastBounds;
        private float m_lastDistance;
        private bool m_isInspecting;
        private void OnDrawGizmos()
        {
            if (!m_debugFrustum || !m_actualCamera || !m_isInspecting) { return; }
            float cameraFov = m_actualCamera.fieldOfView;
            float prevFarPlane = m_actualCamera.farClipPlane;
            // Draw Frustums
            var lastMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(m_actualCamera.transform.position, m_actualCamera.transform.rotation, m_actualCamera.transform.lossyScale);
            Gizmos.color = Color.green;
            Gizmos.DrawFrustum(Vector3.zero, m_actualCamera.fieldOfView, m_actualCamera.farClipPlane, m_actualCamera.nearClipPlane, m_actualCamera.aspect);

            m_actualCamera.fieldOfView *= m_fovFactor;
            m_actualCamera.farClipPlane = m_lastDistance;

            Gizmos.color = Color.red;
            Gizmos.DrawFrustum(Vector3.zero, m_actualCamera.fieldOfView, m_actualCamera.farClipPlane, m_actualCamera.nearClipPlane, m_actualCamera.aspect);

            Gizmos.matrix = lastMatrix;


            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(m_actualCamera.transform.position, m_actualCamera.transform.position + m_actualCamera.transform.forward * m_lastDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(m_lastBounds.center, m_lastBounds.size);
            m_actualCamera.farClipPlane = prevFarPlane;
            m_actualCamera.fieldOfView = cameraFov;

            m_isInspecting = false;
        }

        private void UpdateFrustumPlanes(float distance)
        {
            float cameraFov = m_actualCamera.fieldOfView;
            float prevFarPlane = m_actualCamera.farClipPlane;
            m_actualCamera.fieldOfView *= m_fovFactor;
            m_actualCamera.farClipPlane = distance;
            GeometryUtility.CalculateFrustumPlanes(m_actualCamera, m_frustumPlanes);
            m_actualCamera.farClipPlane = prevFarPlane;
            m_actualCamera.fieldOfView = cameraFov;
        }
    }
}
