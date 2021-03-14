using System;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.UI;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("WEAVR/Procedures/Navigation Arrow")]
    public class NavigationArrow : MonoBehaviour
    {

        #region [  STATIC PART  ]

        private static List<NavigationArrow> s_navigators = new List<NavigationArrow>();
        public static NavigationArrow Current => s_navigators.Count > 0 ? s_navigators[s_navigators.Count - 1] : null;

        #endregion

        [Draggable]
        public Camera activeCamera;
        public float minDistanceToTarget = 10;
        public float showAfter = 3;
        public float followLinearSpeed = 0.5f;
        public float followAngularSpeed = 90;
        [Range(0, 200)]
        public float paddingInPixels = 50;

        [Space]
        [Draggable]
        public GameObject arrow;
        [Draggable]
        public Transform pointingArrow;
        public string triggerShow = "Show";
        public string triggerHide = "Hide";

        private Animator m_animator;
        private bool m_isVisible;
        private float m_showUpTime;

        private GameObject m_target;
        private Transform m_shadowPointingArrow;

        public GameObject Target
        {
            get { return m_target; }
            set
            {
                if (m_target != value)
                {
                    if (!m_target)
                    {
                        HideArrow();
                    }
                    m_target = value;
                    if (m_target)
                    {
                        m_renderer = m_target.GetComponent<Renderer>();
                        if (!m_renderer)
                        {
                            m_collider = m_target.GetComponent<Collider>();
                        }
                        m_showUpTime = Time.time + showAfter;
                    }
                }
            }
        }

        private Collider m_collider;
        private Renderer m_renderer;

        private void OnValidate()
        {
            if (arrow != null && pointingArrow == null)
            {
                pointingArrow = arrow.transform;
            }
            minDistanceToTarget = Mathf.Max(0.5f, minDistanceToTarget);
        }

        private void Awake()
        {
            if (!s_navigators.Contains(this))
            {
                s_navigators.Add(this);
            }
        }

        private void OnDestroy()
        {
            s_navigators.Remove(this);
        }

        // Use this for initialization
        void Start()
        {
            OnValidate();

            m_animator = arrow.GetComponentInChildren<Animator>(true);

            m_shadowPointingArrow = new GameObject("PointingArrow_Shadow").transform;
            m_shadowPointingArrow.hideFlags = HideFlags.HideAndDontSave;
            m_shadowPointingArrow.SetParent(pointingArrow.parent, false);
            m_shadowPointingArrow.SetPositionAndRotation(pointingArrow.position, pointingArrow.rotation);

            if (!activeCamera)
            {
                activeCamera = GetComponent<Camera>();
            }
        }

        private void OnDisable()
        {

        }

        private void OnEnable()
        {
            OnDisable();
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_target) {
                if (m_isVisible)
                {
                    HideArrow();
                }
                return; 
            }
            if (!WeavrManager.ShowNavigationHints)
            {
                if (m_isVisible)
                {
                    HideArrow();
                }
                return;
            }
            Vector3 worldPoint = GetWorldPoint();
            bool targetIsVisible = IsVisibleOnScreen(worldPoint, activeCamera);
            if (m_isVisible)
            {
                if (targetIsVisible)
                {
                    HideArrow();
                }
                else
                {
                    m_shadowPointingArrow.LookAt(worldPoint);
                    pointingArrow.localPosition = Vector3.Lerp(pointingArrow.localPosition,
                                                               m_shadowPointingArrow.localPosition,
                                                               Time.deltaTime * followLinearSpeed);
                    pointingArrow.localRotation = Quaternion.Slerp(pointingArrow.localRotation,
                                                                   m_shadowPointingArrow.localRotation,
                                                                   Time.deltaTime * followAngularSpeed);
                }
            }
            else if (targetIsVisible)
            {
                m_showUpTime = Time.time + showAfter;
            }
            else if (Time.time >= m_showUpTime)
            {
                ShowArrow();
            }
        }

        private Vector3 GetWorldPoint()
        {
            return m_renderer ? m_renderer.bounds.center : m_collider ? m_collider.bounds.center : m_target.transform.position;
        }

        private bool IsVisibleOnScreen(Vector3 position, Camera cam)
        {
            var projected = cam.WorldToScreenPoint(position);
            return projected.z >= 0
                && projected.x >= paddingInPixels && projected.x <= cam.scaledPixelWidth - paddingInPixels
                && projected.y >= paddingInPixels && projected.y <= cam.scaledPixelHeight - paddingInPixels
                && Vector3.Distance(position, cam.transform.position) < minDistanceToTarget;
        }

        private void HideArrow()
        {
            if (m_animator != null)
            {
                m_animator.SetTrigger(triggerHide);
            }
            else
            {
                arrow.SetActive(false);
            }
            m_isVisible = false;
        }

        private void ShowArrow()
        {
            if (m_animator != null && arrow.activeInHierarchy)
            {
                m_animator.ResetTrigger(triggerHide);
                m_animator.SetTrigger(triggerShow);
            }
            else
            {
                arrow.SetActive(true);
            }
            m_isVisible = true;
        }
    }
}