namespace TXT.WEAVR.UI
{
    using System;
    using System.Collections;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [RequireComponent(typeof(SpaceOccupier))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SpringJoint))]
    [AddComponentMenu("")]
    public class Popup3D : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        protected Camera m_canvasCamera;

        [SerializeField]
        [Draggable]
        protected Canvas m_mainCanvas;

        [SerializeField]
        [Tooltip("Use fixed point for the popup whenever it is possible")]
        protected bool m_useFixedPoint = true;

        [SerializeField]
        [Tooltip("Whether to always face the camera or not")]
        protected bool m_lookAtCamera = true;

        [SerializeField]
        [Tooltip("Whether to dinamically resize the popup or not")]
        protected bool m_dynamicSize = false;

        [SerializeField]
        [Tooltip("The percentage of the screen width to match")]
        [HiddenBy(nameof(m_dynamicSize))]
        [Range(0.01f, 1)]
        protected float m_sizeToScreenRatio = 0.2f;
        [SerializeField]
        [Tooltip("The size limits of the billboard in world space")]
        [HiddenBy(nameof(m_dynamicSize))]
        protected Span m_worldSizeLimits = new Span(0.2f, 0.6f);

        [SerializeField]
        [Tooltip("Whether to fade the popup based on distance to camera or not")]
        protected bool m_fadeByDistance = true;
        [SerializeField]
        [HiddenBy(nameof(m_fadeByDistance))]
        protected float m_fadeDistanceToCamera = 1;

        [SerializeField]
        [Space]
        [Tooltip("Object to scale when resizing dynamically")]
        [HiddenBy(nameof(m_dynamicSize))]
        [Draggable]
        protected Transform m_canvasScaler;

        [SerializeField]
        [Tooltip("The force to apply when first showing popup, for greater reveal effect")]
        protected Vector3 m_pushOutForce = Vector3.up * 10;

        [SerializeField]
        [Tooltip("The force to add for push out effect towards the camera")]
        protected float m_pushOutForceToCamera = 5f;

        protected Animator m_animator;

        protected Transform m_lastTarget;
        protected Vector3 m_showPoint;

        protected SpaceOccupier m_spaceOccupier;
        protected Rigidbody m_rigidBody;
        protected SpringJoint m_springJoint;

        protected Transform m_defaultParent;

        protected Vector3[] m_cornerPoints;

        protected Transform m_fixedPoint;
        protected Collider m_collider;
        protected Renderer m_targetRenderer;
        protected bool m_colliderWasTrigger;

        protected CanvasGroup m_canvasGroup;

        /// <summary>
        /// The object to scale the canvas
        /// </summary>
        public virtual Transform CanvasScaler {
            get { return m_canvasScaler; }
            set {
                if (m_canvasScaler != value)
                {
                    m_canvasScaler = value;
                }
            }
        }

        protected bool m_isVisible;
        public bool IsVisible {
            get { return m_isVisible; }
            set {
                if (m_isVisible != value)
                {
                    m_isVisible = value;
                    if (value) { Show(m_showPoint); }
                    else { Hide(); }
                }
            }
        }

        protected virtual void OnValidate()
        {
            if (m_mainCanvas == null)
            {
                m_mainCanvas = GetComponentInChildren<Canvas>(true);
                if (m_mainCanvas != null)
                {
                    m_canvasCamera = m_mainCanvas.worldCamera;
                }
            }
            if (m_canvasCamera == null)
            {
                m_canvasCamera = Camera.main;
                if (m_canvasCamera == null && m_mainCanvas != null)
                {
                    m_canvasCamera = m_mainCanvas.worldCamera ?? (Camera.allCamerasCount > 0 ? Camera.allCameras[0] : null);
                }
            }
            if (m_mainCanvas != null && m_mainCanvas.worldCamera != m_canvasCamera)
            {
                m_mainCanvas.worldCamera = m_canvasCamera;
            }
        }

        public virtual void Hide()
        {
            if (transform != null && m_mainCanvas != null)
            {
                transform.SetParent(m_defaultParent, true);

                m_lastTarget = null;
                m_mainCanvas.gameObject.SetActive(false);

                m_isVisible = false;
            }
        }

        public virtual void Show(Transform point, bool asChild = false)
        {
            if (asChild) { transform.SetParent(point, true); }

            var fixedPoint = m_useFixedPoint ? point.GetComponent<PopupPoint>() : null;
            if (fixedPoint != null && fixedPoint.enabled)
            {
                m_fixedPoint = fixedPoint.point;
                m_rigidBody.isKinematic = true;
                if (m_collider != null)
                {
                    m_colliderWasTrigger = m_collider.isTrigger;
                    m_collider.isTrigger = true;
                }
                InternalShowFixed(m_fixedPoint, fixedPoint.origin ?? point);
            }
            else
            {
                m_fixedPoint = null;
                m_rigidBody.isKinematic = false;
                if (m_collider != null)
                {
                    m_collider.isTrigger = m_colliderWasTrigger;
                }
                var renderer = point.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    InternalShow(renderer.bounds.center, point);
                }
                else
                {
                    InternalShow(point.position, point);
                }
            }
        }

        public virtual void Show(Vector3 point)
        {
            InternalShow(point, null);
        }

        protected virtual void InternalShowFixed(Transform point, Transform context)
        {
            m_isVisible = true;

            m_lastTarget = context;
            transform.position = point.position;
            transform.LookAt(m_canvasCamera.transform);
            m_targetRenderer = context.GetComponentInChildren<Renderer>();
            m_showPoint = m_targetRenderer != null ? m_targetRenderer.bounds.center : context.transform.position;

            m_mainCanvas.gameObject.SetActive(true);

            if (m_animator != null)
            {
                m_animator.SetTrigger("Show");
            }
        }

        protected virtual void InternalShow(Vector3 point, Transform context)
        {
            m_isVisible = true;

            var relativeRay = m_canvasCamera.transform.position - point;
            RaycastHit hit;
            if (Physics.Linecast(m_canvasCamera.transform.position, point, out hit, WorldBounds.GetOccupancySpacePassiveLayer()))
            {
                transform.position = hit.point;
            }
            else if (context != null)
            {
                var renderer = context.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    var extents = renderer.bounds.extents;
                    transform.position = point + relativeRay.normalized * extents.magnitude;
                }
                else
                {
                    transform.position = point + relativeRay * 0.1f;
                }
            }
            else
            {
                //var relativeRay = m_canvasCamera.transform.position - point;
                transform.position = point + relativeRay * 0.1f;
            }
            transform.LookAt(m_canvasCamera.transform);

            m_showPoint = point;

            m_lastTarget = context;
            m_springJoint.connectedAnchor = point;
            m_mainCanvas.gameObject.SetActive(true);

            Vector3 pushoutForce = m_pushOutForce;
            float altitudeDelta = m_canvasCamera.transform.position.y - point.y;
            pushoutForce.y *= Mathf.Clamp(altitudeDelta, -1, 1);

            m_rigidBody.AddForce(pushoutForce + relativeRay.normalized * Mathf.Min(m_pushOutForceToCamera, relativeRay.magnitude), ForceMode.VelocityChange);

            UpdateOccupancyVolumeDelayed(0.021f);

            if (m_animator != null)
            {
                m_animator.SetTrigger("Show");
            }
        }

        protected virtual void Start()
        {
            m_collider = GetComponentInChildren<Collider>();
            m_defaultParent = transform.parent;
            m_canvasGroup = m_mainCanvas.GetComponentInChildren<CanvasGroup>(true);
            if (m_canvasScaler == null)
            {
                m_canvasScaler = transform;
            }
        }

        protected virtual void CameraChanged(Camera newCamera)
        {
            if (newCamera != null && m_mainCanvas != null)
            {
                m_canvasCamera = newCamera;
                m_mainCanvas.worldCamera = newCamera;
            }
        }

        // Use this for initialization
        protected virtual void OnEnable()
        {
            m_spaceOccupier = GetComponent<SpaceOccupier>();
            m_rigidBody = GetComponent<Rigidbody>();
            m_springJoint = GetComponent<SpringJoint>();

            m_animator = GetComponent<Animator>();

            m_rigidBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            m_rigidBody.drag = 10;
            m_rigidBody.mass = 0.000001f;

            m_cornerPoints = new Vector3[4];
            UpdateOccupancyVolume();

            if (WeavrCamera.CurrentCamera)
            {
                CameraChanged(WeavrCamera.CurrentCamera);
            }

            WeavrCamera.CameraChanged -= CameraChanged;
            WeavrCamera.CameraChanged += CameraChanged;

            if (m_canvasScaler == null)
            {
                m_canvasScaler = transform;
            }
        }

        protected virtual void UpdateOccupancyVolume()
        {
            if (m_spaceOccupier == null || m_mainCanvas == null) return;
            (m_mainCanvas.transform as RectTransform).GetLocalCorners(m_cornerPoints);
            Vector3 center = (m_cornerPoints[0] + m_cornerPoints[2]) * 0.5f + m_mainCanvas.transform.localPosition;
            Vector3 size = new Vector3(Mathf.Abs(m_cornerPoints[2].x - m_cornerPoints[1].x) * m_mainCanvas.transform.localScale.x,
                                       Mathf.Abs(m_cornerPoints[1].y - m_cornerPoints[0].y) * m_mainCanvas.transform.localScale.y,
                                       0.05f);
            m_spaceOccupier.ResizeOccupancyVolume(center, size);
        }

        protected virtual void UpdateOccupancyVolumeDelayed(float delay)
        {
            StartCoroutine(UpdateOccupancyVolumeCoroutine(delay));
        }

        protected virtual IEnumerator UpdateOccupancyVolumeCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateOccupancyVolume();
        }

        protected virtual void InternalUpdate(float dt)
        {
            if (m_lookAtCamera)
            {
                transform.LookAt(m_canvasCamera.transform);
            }
            if (m_fixedPoint)
            {
                m_showPoint = m_targetRenderer != null ? m_targetRenderer.bounds.center : m_lastTarget.position;
                transform.position = m_fixedPoint.position;
            }
            else if (m_lastTarget != null)
            {
                m_showPoint = m_lastTarget.position;
                m_springJoint.anchor = m_showPoint;
                m_springJoint.connectedAnchor = m_lastTarget.position;

                //transform.position = m_showPoint;
            }
            if (m_dynamicSize && m_canvasCamera != null)
            {
                DynamicallyRescale();
            }
            if (m_fadeByDistance && m_canvasGroup != null)
            {
                float distanceToCamera = Vector3.Distance(m_canvasCamera.transform.position, transform.position);
                float span = m_fadeDistanceToCamera - m_canvasCamera.nearClipPlane;
                m_canvasGroup.alpha = Mathf.Clamp01((distanceToCamera - m_canvasCamera.nearClipPlane) / span);
            }
        }

        protected virtual void DynamicallyRescale()
        {
            (m_mainCanvas.transform as RectTransform).GetWorldCorners(m_cornerPoints);
            float currentLength = Vector3.Distance(m_cornerPoints[2], m_cornerPoints[1]);
            float pixels = m_canvasCamera.scaledPixelWidth * m_sizeToScreenRatio;
            float targetLength = WeavrUIHelper.GetLengthOfPixelsAt(m_canvasScaler.position, m_canvasCamera, pixels);
            float ratio = m_worldSizeLimits.Clamp(targetLength) / currentLength;
            if (!float.IsInfinity(ratio) && Math.Abs(ratio - 1) > 0.01f)
            {
                m_canvasScaler.localScale *= ratio;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsVisible)
            {
                InternalUpdate(Time.fixedDeltaTime);
            }
        }
    }
}