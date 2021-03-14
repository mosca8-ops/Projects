using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Procedure
{
    [Obsolete("Use Visual Inspector instead")]
    [AddComponentMenu("")]
    public class VisualInspection : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        private Camera m_targetCamera;
        [SerializeField]
        [Draggable]
        private Renderer m_rendererToCheck;
        [SerializeField]
        private bool m_trackAlways = false;
        [SerializeField]
        private bool m_accumulativeInspection = false;
        [SerializeField]
        private bool m_deeperInspection = true;
        [SerializeField]
        private float m_minInspectionTime = 2f;
        [SerializeField]
        private float m_maxInspectionDistance = 2;
        [SerializeField]
        [Range(0.01f, 1)]
        private float m_focusWidth = 0.5f;
        [SerializeField]
        [Range(0.01f, 1)]
        private float m_focusHeight = 0.5f;

        private float m_paddingInPixelsWidth = 100;
        private float m_paddingInPixelsHeight = 100;

        [Space]
        [SerializeField]
        private Events m_events;

        public UnityEventRenderer OnRendererChanged => m_events.onRendererChanged;
        public UnityEvent OnInspectionStarted => m_events.onInspectionStarted;
        public UnityEventFloat OnOngoingInspectionNormalized => m_events.onOngoingInspectionNormalized;
        public UnityEvent OnInspectionLost => m_events.onInspectionLost;
        public UnityEvent OnInspectionDone => m_events.onInspectionDone;

        private float m_nextTimeout;
        private float m_accumulator;
        private bool m_isInspected;

        private bool m_isTracking;
        private bool m_rendererIsVisible;

        public float MinInspectionTime
        {
            get { return m_minInspectionTime; }
            set
            {
                if (m_minInspectionTime != value)
                {
                    m_minInspectionTime = Mathf.Max(0, value);
                    ResetInspection();
                }
            }
        }

        public float MaxInspectionDistance
        {
            get { return m_maxInspectionDistance; }
            set
            {
                if (m_maxInspectionDistance != value)
                {
                    m_maxInspectionDistance = Mathf.Max(0.1f, value);
                    ResetInspection();
                }
            }
        }

        public bool IsAccumulativeInspection
        {
            get { return m_accumulativeInspection; }
            set
            {
                if (m_accumulativeInspection != value)
                {
                    m_accumulativeInspection = value;
                    ResetInspection();
                }
            }
        }

        public bool CanStartInspection
        {
            get { return m_trackAlways || m_isTracking; }
            set
            {
                if (m_isTracking != value)
                {
                    m_isTracking = m_trackAlways || value;
                    ResetInspection();
                }
            }
        }

        private void ResetInspection()
        {
            m_isInspected = false;
            m_accumulator = 0;
            m_nextTimeout = Time.time + m_minInspectionTime;
            RendererIsVisible = false;
            IsInspectedGlobally = false;
        }

        public bool IsInspectedGlobally { get; set; }

        public bool IsInspected
        {
            get { return m_isInspected; }
            private set
            {
                if (m_isInspected != value)
                {
                    m_isInspected = value;
                    if (value)
                    {
                        IsInspectedGlobally = true;
                        OnInspectionDone.Invoke();
                    }
                }
            }
        }

        public bool RendererIsVisible
        {
            get { return m_rendererIsVisible; }
            set
            {
                if (m_rendererIsVisible != value)
                {
                    m_rendererIsVisible = value;
                    if (value)
                    {
                        OnInspectionStarted.Invoke();
                    }
                    else
                    {
                        OnInspectionLost.Invoke();
                    }
                }
            }
        }

        public Renderer RendererToInspect
        {
            get { return m_rendererToCheck; }
            set
            {
                if (m_rendererToCheck != value)
                {
                    m_rendererToCheck = value;
                    ResetInspection();
                    OnRendererChanged.Invoke(value);
                }
            }
        }

        private void OnValidate()
        {
            //if(m_rendererToCheck == null)
            //{
            //    m_rendererToCheck = GetComponentInChildren<Renderer>();
            //}
            if (m_targetCamera == null)
            {
                m_targetCamera = GetComponent<Camera>();
            }
            m_minInspectionTime = Mathf.Max(0, m_minInspectionTime);
            m_maxInspectionDistance = Mathf.Max(0.1f, m_maxInspectionDistance);
        }

        // Use this for initialization
        void Start()
        {
            m_isTracking = m_trackAlways;
            OnValidate();

            m_paddingInPixelsWidth = m_targetCamera.scaledPixelWidth * (1 - m_focusWidth) * 0.5f;
            m_paddingInPixelsHeight = m_targetCamera.scaledPixelHeight * (1 - m_focusHeight) * 0.5f;
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_isTracking || m_rendererToCheck == null) { return; }

            //RendererIsVisible = m_rendererToCheck.isVisible 
            //    && Vector3.Distance(transform.position, m_rendererToCheck.transform.position) < m_maxInspectionDistance;
            RendererIsVisible = IsVisibleOnScreen(m_rendererToCheck.bounds.center, m_targetCamera);

            if (m_accumulativeInspection)
            {
                if (RendererIsVisible)
                {
                    m_accumulator += Time.deltaTime;
                }
                IsInspected = m_accumulator >= m_minInspectionTime;
                if (!IsInspected && m_minInspectionTime > 0)
                {
                    OnOngoingInspectionNormalized.Invoke(Mathf.Clamp01(m_accumulator / m_minInspectionTime));
                }
            }
            else if (!RendererIsVisible)
            {
                IsInspected = false;
                m_nextTimeout = Time.time + m_minInspectionTime;
            }
            else if (!IsInspected)
            {
                IsInspected = Time.time >= m_nextTimeout;
                if (m_minInspectionTime > 0)
                {
                    OnOngoingInspectionNormalized.Invoke(1f - Mathf.Clamp01((m_nextTimeout - Time.time) / m_minInspectionTime));
                }
            }
        }

        public void ForceInspectionDone()
        {
            StartCoroutine(ForcedDoneInspection());
        }

        private IEnumerator ForcedDoneInspection()
        {
            IsInspected = true;
            yield return new WaitForEndOfFrame();
            IsInspected = true;
        }

        private bool IsVisibleOnScreen(Vector3 point, Camera cam)
        {
            var projected = cam.WorldToScreenPoint(point);
            return projected.z >= 0
                && projected.x >= m_paddingInPixelsWidth && projected.x <= cam.scaledPixelWidth - m_paddingInPixelsWidth
                && projected.y >= m_paddingInPixelsHeight && projected.y <= cam.scaledPixelHeight - m_paddingInPixelsHeight
                && Vector3.Distance(point, cam.transform.position) < m_maxInspectionDistance
                && (!m_deeperInspection || m_rendererToCheck.isVisible);
        }

        [Serializable]
        public class UnityEventRenderer : UnityEvent<Renderer> { }

        [Serializable]
        private struct Events
        {
            public UnityEventRenderer onRendererChanged;
            public UnityEvent onInspectionStarted;
            public UnityEventFloat onOngoingInspectionNormalized;
            public UnityEvent onInspectionLost;
            public UnityEvent onInspectionDone;
        }
    }
}
