using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class DottedLine : MonoBehaviour
    {
        [SerializeField]
        private float m_lineThickness = 0.1f;
        [SerializeField]
        private Transform m_transformA;
        [SerializeField]
        private Transform m_transformB;

        [Space]
        [SerializeField]
        private AttachPoint[] m_attachments;

        [Space]
        [SerializeField]
        [DisabledBy(nameof(m_transformA), disableWhenTrue: true)]
        private Vector3 m_pointA;
        [SerializeField]
        [DisabledBy(nameof(m_transformB), disableWhenTrue: true)]
        private Vector3 m_pointB;

        [SerializeField]
        [Space]
        private Animator m_animator;
        [SerializeField]
        [HiddenBy(nameof(m_animator))]
        private OptionalFloat m_hideTimeout;
        [SerializeField]
        [HiddenBy(nameof(m_animator))]
        private string m_showTrigger = "Show";
        [SerializeField]
        [HiddenBy(nameof(m_animator))]
        private string m_hideTrigger = "Hide";

        [SerializeField]
        [HideInInspector]
        private LineRenderer m_line;

        private bool m_rebuildLine;
        private Coroutine m_hideCoroutine;

        public Transform TranformA { get => m_transformA; set => m_transformA = value; }
        public Transform TranformB { get => m_transformB; set => m_transformB = value; }
        public Vector3 PointA
        {
            get => m_pointA;
            set
            {
                m_pointA = value;
                m_transformA = null;
                m_rebuildLine = true;
            }
        }
        public Vector3 PointB
        {
            get => m_pointB;
            set
            {
                m_pointB = value;
                m_transformB = null;
                m_rebuildLine = true;
            }
        }

        public Gradient LineGradient
        {
            get => m_line.colorGradient;
            set { if (value != null) m_line.colorGradient = value; }
        }

        private void Reset()
        {
            m_animator = GetComponent<Animator>();
            m_hideTimeout = 1;
            m_hideTimeout.enabled = false;
        }

        private void OnValidate()
        {
            m_line = GetComponent<LineRenderer>();
            m_line.startWidth = m_lineThickness;
            m_line.endWidth = m_lineThickness;
            m_line.useWorldSpace = true;
            m_line.widthCurve = new AnimationCurve(new Keyframe(0, m_lineThickness));
            m_rebuildLine = true;
        }

        void Start()
        {
            if (!m_line)
            {
                m_line = GetComponent<LineRenderer>();
            }
            m_line.startWidth = m_lineThickness;
            m_line.endWidth = m_lineThickness;
            m_line.widthCurve = new AnimationCurve(new Keyframe(0, m_lineThickness));
            m_line.useWorldSpace = true;
        }

        private void OnEnable()
        {
            m_rebuildLine = true;
        }

        public void Show()
        {
            StopHideCoroutine();
            gameObject.SetActive(true);
            ResetAnimatorTrigger(m_hideTrigger);
            SetAnimatorTrigger(m_showTrigger);
        }

        private void SetAnimatorTrigger(string trigger)
        {
            if (m_animator && !string.IsNullOrEmpty(trigger))
            {
                m_animator.SetTrigger(trigger);
            }
        }

        private void ResetAnimatorTrigger(string trigger)
        {
            if (m_animator && !string.IsNullOrEmpty(trigger))
            {
                m_animator.ResetTrigger(trigger);
            }
        }

        public void Hide()
        {
            StopHideCoroutine();
            if(m_hideTimeout.enabled && m_animator)
            {
                ResetAnimatorTrigger(m_showTrigger);
                SetAnimatorTrigger(m_hideTrigger);
                m_hideCoroutine = StartCoroutine(HideCoroutine(m_hideTimeout.value));
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void StopHideCoroutine()
        {
            if(m_hideCoroutine != null)
            {
                StopCoroutine(m_hideCoroutine);
                m_hideCoroutine = null;
            }
        }

        private IEnumerator HideCoroutine(float timeout)
        {
            yield return new WaitForSeconds(timeout);
            gameObject.SetActive(false);
            m_hideCoroutine = null;
        }

        private void SetPointA(Vector3 point)
        {
            if (m_pointA != point)
            {
                m_pointA = point;
                m_rebuildLine = true;
            }
        }

        private void SetPointB(Vector3 point)
        {
            if (m_pointB != point)
            {
                m_pointB = point;
                m_rebuildLine = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (m_transformA)
            {
                SetPointA(m_transformA.position);
            }
            if (m_transformB)
            {
                SetPointB(m_transformB.position);
            }

            if (m_rebuildLine)
            {
                m_rebuildLine = false;
                var difference = m_pointB - m_pointA;
                var direction = difference.normalized;
                float distance = difference.magnitude;
                CreatePoints(direction, distance, m_lineThickness);
                ReattachObjects(direction);
            }
        }

        private void ReattachObjects(Vector3 direction)
        {
            for (int i = 0; i < m_attachments.Length; i++)
            {
                if (m_attachments[i].attachObject)
                {
                    m_attachments[i].attachObject.position = Vector3.Lerp(m_pointA, m_pointB, m_attachments[i].normalizedPosition);
                    if (m_attachments[i].rotateObject)
                    {
                        m_attachments[i].attachObject.forward = direction;
                    }
                }
            }
        }

        private Vector3[] CreatePoints(Vector3 direction, float length, float width)
        {
            int points = (int)(length / width);
            Vector3 amountToAdd = direction * length / (points);

            Vector3[] positions = new Vector3[points + 1];
            positions[0] = m_pointA;
            for (int i = 1; i < points; i++)
            {
                positions[i] = positions[i - 1] + amountToAdd;
            }
            positions[points] = m_pointB;

            m_line.positionCount = points + 1;
            m_line.SetPositions(positions);

            return positions;
        }

        [Serializable]
        private struct AttachPoint
        {
            [Range(0, 1)]
            public float normalizedPosition;
            public bool rotateObject;
            public Transform attachObject;
        }
    }
}