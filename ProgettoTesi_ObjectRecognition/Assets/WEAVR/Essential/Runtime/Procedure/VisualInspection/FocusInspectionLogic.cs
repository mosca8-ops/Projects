using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("WEAVR/Procedures/Visual Inspection/Focus Inspection Marker")]
    public class FocusInspectionLogic : AbstractInspectionLogic, IProgressElement, IVisualInspectionEvents, IFocusInspectionLogic
    {
        [SerializeField]
        [Draggable]
        private BoxCollider m_boundsHandle;
        [SerializeField]
        [Tooltip("Whether to keep accumulating the inspecting time rather than beginning from scratch when losing focus")]
        private bool m_accumulativeInspection = false;
        [SerializeField]
        private float m_inspectionTime = 2f;
        [SerializeField]
        private float m_inspectionDistance = 2;

        private float m_time;
        private float m_distance;
        private bool m_accumulate;

        private float m_nextTimeout;
        private float m_accumulator;
        private float m_progress;
        private bool m_isInspected;

        private bool m_isTracking = true;
        private bool m_targetIsVisible;

        private IVisualInspector m_inspector;

        private Transform m_target;
        private Bounds m_defaultBounds = new Bounds(Vector3.zero, new Vector3(0.1f, 0.1f, 0.1f));

        public event Action<GameObject> InspectionTargetChanged;
        public event Action OnInspectionStarted;
        public event Action<float> OnOngoingInspectionNormalized;
        public event Action OnInspectionLost;
        public event Action OnInspectionDone;

        public float InspectionTime
        {
            get { return m_time; }
            set
            {
                if (m_time != value)
                {
                    m_time = m_inspectionTime = Mathf.Max(0, value);
                    ResetInspection();
                }
            }
        }

        public float InspectionDistance
        {
            get { return m_distance; }
            set
            {
                if (m_distance != value)
                {
                    m_distance = m_inspectionDistance = Mathf.Max(0.1f, value);
                    ResetInspection();
                }
            }
        }

        public bool IsAccumulativeInspection
        {
            get { return m_accumulate; }
            set
            {
                if (m_accumulate != value)
                {
                    m_accumulate = m_accumulativeInspection = value;
                    ResetInspection();
                }
            }
        }

        private void ResetInspection()
        {
            m_isInspected = false;
            m_accumulator = 0;
            m_nextTimeout = Time.time + m_time;
            Progress = 0;
            m_targetIsVisible = false;
            IsInspectedGlobally = false;
        }

        private void RestoreDefaults()
        {
            m_time = m_inspectionTime;
            m_distance = m_inspectionDistance;
            m_accumulate = m_accumulativeInspection;
        }

        public bool IsInspectedGlobally { get; set; }

        public override bool IsInspected
        {
            get { return m_isInspected; }
            protected set
            {
                if (m_isInspected != value)
                {
                    m_isInspected = value;
                    if (value)
                    {
                        IsInspectedGlobally = true;
                        OnInspectionDone?.Invoke();
                        TargetToInspect = null;
                    }
                }
            }
        }

        public GameObject TargetToInspect
        {
            get => m_target ? m_target.gameObject : null;
            protected set
            {
                if (!m_target || m_target.gameObject != value)
                {
                    m_target = value ? value.transform : null;
                    if (m_target)
                    {

                    }
                    else
                    {
                        RestoreDefaults();
                    }
                    ResetInspection();
                    InspectionTargetChanged?.Invoke(value);
                }
            }
        }

        public override bool TargetIsVisible
        {
            get { return m_targetIsVisible; }
            protected set
            {
                if (m_targetIsVisible != value)
                {
                    m_targetIsVisible = value;
                    if (value)
                    {
                        OnInspectionStarted?.Invoke();
                    }
                    else
                    {
                        OnInspectionLost?.Invoke();
                    }
                }
            }
        }

        public float Progress
        {
            get => m_progress;
            set
            {
                if (m_progress != value)
                {
                    m_progress = Mathf.Clamp01(value);
                    OnOngoingInspectionNormalized?.Invoke(m_progress);
                }
            }
        }

        private BoxCollider BoundsHandle
        {
            get
            {
                if (!m_boundsHandle)
                {
                    m_boundsHandle = new GameObject("BoundsHandle").AddComponent<BoxCollider>(); 
                    m_boundsHandle.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                    m_boundsHandle.transform.SetParent(transform, false);
                    m_boundsHandle.isTrigger = true;
                    //m_boundsHandle.enabled = false;
                    m_boundsHandle.transform.localPosition = Vector3.zero;
                    m_boundsHandle.transform.localRotation = Quaternion.identity;
                    m_boundsHandle.transform.localScale = Vector3.one;
                }
                return m_boundsHandle;
            }
        }

        private void Reset()
        {
            m_boundsHandle = new GameObject("BoundsHandle").AddComponent<BoxCollider>();
            m_boundsHandle.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            m_boundsHandle.transform.SetParent(transform, false);
            m_boundsHandle.isTrigger = true;
            //m_boundsHandle.enabled = false;
            m_boundsHandle.transform.localPosition = Vector3.zero;
            m_boundsHandle.transform.localRotation = Quaternion.identity;
            m_boundsHandle.transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (!m_isTracking || !m_target || m_inspector == null) { return; }

            TargetIsVisible = m_inspector.CanSee(m_boundsHandle.bounds, m_distance);

            if (m_accumulate)
            {
                if (TargetIsVisible)
                {
                    m_accumulator += Time.deltaTime;
                }
                IsInspected = m_accumulator >= m_time;
                if (!IsInspected && m_time > 0)
                {
                    Progress = Mathf.Clamp01(m_accumulator / m_time);
                }
            }
            else if (!TargetIsVisible)
            {
                IsInspected = false;
                m_nextTimeout = Time.time + m_time;
            }
            else if (!IsInspected)
            {
                IsInspected = Time.time >= m_nextTimeout;
                if (m_time > 0)
                {
                    Progress = 1f - Mathf.Clamp01((m_nextTimeout - Time.time) / m_time);
                }
            }
        }

        protected Bounds GetTargetBounds(GameObject target)
        {
            Bounds bounds = m_defaultBounds;
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

        public override void ResetValues()
        {
            base.ResetValues();
            m_time = m_inspectionTime;
            m_distance = m_inspectionDistance;
            m_accumulate = m_accumulativeInspection;
            TargetToInspect = null;
        }

        public void ResetProgress()
        {
            Progress = 0;
        }

        public override void InspectTarget(IVisualInspector inspector, GameObject target, Pose localPose, Bounds? bounds)
        {
            transform.SetParent(target.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            m_inspector = inspector;
            var b = bounds ?? GetTargetBounds(target);
            BoundsHandle.size = b.size;
            BoundsHandle.center = b.center;

            TargetToInspect = target;
        }
    }
}
