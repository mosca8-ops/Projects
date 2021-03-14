using System;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Procedure;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    public enum PivotAxis
    {
        CameraAlign,
        Up,
        Free,
    }

    [ExecuteInEditMode]
    [AddComponentMenu("WEAVR/Components/Billboard")]
    public class Billboard : MonoBehaviour, IActiveProgressElement
    {
        protected static readonly Vector3 s_defaultOffset = new Vector3(0, 0.25f, 0);
        protected const int k_OverlayComparison = (int)UnityEngine.Rendering.CompareFunction.Always;

        [SerializeField]
        [Tooltip("Whether to show the billboard over all other scene objects or not")]
        private bool m_ignoreRenderDepth;
        [SerializeField]
        private OptionalVector3 m_startPointOffset;
        [SerializeField]
        private OptionalVector3 m_endPointOffset = s_defaultOffset;
        private float m_pointsDistance;

        [SerializeField]
        [HideInInspector]
        private Canvas m_canvas;
        [SerializeField]
        [HideInInspector]
        private CanvasGroup m_canvasGroup;
        [SerializeField]
        [InspectorName("Main Text")]
        [HiddenBy(nameof(m_textElement), hiddenWhenTrue: true)]
        [Type(typeof(ITextComponent))]
        private Component m_textComponent;
        [SerializeField]
        [InspectorName("Main Text")]
        [HiddenBy(nameof(m_textComponent), hiddenWhenTrue: true)]
        private Text m_textElement;
        [SerializeField]
        private bool m_showConnectingLine;

        [Space]
        [SerializeField]
        private AdvancedSettings m_advanced;

        [Space]
        [SerializeField]
        [ShowIf(nameof(HasLineRenderer))]
        private LineRenderingSettings m_lineSettings;
        [SerializeField]
        [HideInInspector]
        private LineRenderer m_lineRenderer;
        private GradientAlphaKey[] m_originalAlphaKeys;
        private GradientAlphaKey[] m_copyAlphaKeys;
        private Color m_targetPointColor;
        protected Vector3[] m_cornerPoints = new Vector3[4];

        [SerializeField]
        [HideInInspector]
        private bool m_allSet;

        [SerializeField]
        [HideInInspector]
        private BillboardProgressBar m_progressBar;

        private Transform m_target;
        private Quaternion m_targetOrientation = Quaternion.identity;
        private bool m_isVisible;
        private Quaternion m_targetDeltaRotation;

        private Vector3 m_showPoint;
        private Vector3 m_canvasPositionTarget;
        private Vector3 m_canvasTargetScale;
        private float m_canvasAlphaTarget;
        private float m_currentAlpha;
        private float m_inverseAnimDuration;

        private Dictionary<int, BillboardElement> m_elements;
        private int m_maxId;

        private bool m_isVR;

        private Renderer m_targetRenderer;
        private Transform m_scaler;
        private Transform m_canvasTransform;

        private Transform m_targetPoint;

        public event Action<Billboard, bool> ChangedVisibility;

        //public Transform Scaler => m_scaler;
        public Camera Camera => WeavrCamera.CurrentCamera;

        public IReadOnlyDictionary<int, BillboardElement> Elements
        {
            get
            {
                if (m_elements == null)
                {
                    UpdateElements();
                }
                return m_elements;
            }
        }

        public Transform Target => m_target;

        public string Text
        {
            get => m_textComponent && m_textComponent is ITextComponent textComponent ? textComponent.Text : m_textElement ? m_textElement.text : null;
            set
            {
                if (m_textComponent && m_textComponent is ITextComponent textComponent)
                {
                    textComponent.Text = value;
                }
                else if (m_textElement && m_textElement.text != value)
                {
                    m_textElement.text = value;
                }
            }
        }

        public bool IsVisible
        {
            get => m_isVisible;
            set
            {
                if (m_isVisible != value)
                {
                    m_isVisible = value;
                    if (value && m_target)
                    {
                        ShowOn(m_target.gameObject, m_targetOrientation);
                    }
                    else if (!value)
                    {
                        Hide();
                    }
                }
            }
        }

        public bool IgnoreRenderDepth
        {
            get => m_ignoreRenderDepth;
            set
            {
                if (m_ignoreRenderDepth != value)
                {
                    m_ignoreRenderDepth = value;
                    ApplyIgnoreRenderDepth(true);
                }
            }
        }

        public Vector3? StartPoint
        {
            get => m_startPointOffset.value;
            set
            {
                if (m_startPointOffset.value != value)
                {
                    if (value.HasValue)
                    {
                        m_startPointOffset.enabled = true;
                        m_startPointOffset.value = value.Value;
                        m_pointsDistance = Vector3.Distance(m_startPointOffset.enabled ?
                                            m_startPointOffset.value : Vector3.zero, m_endPointOffset.enabled ?
                                            m_endPointOffset.value : s_defaultOffset);
                    }
                    else { m_startPointOffset.enabled = false; }
                }
            }
        }

        public Vector3? EndPoint
        {
            get => m_endPointOffset.value;
            set
            {
                if (m_endPointOffset.value != value)
                {
                    if (value.HasValue)
                    {
                        m_endPointOffset.enabled = true;
                        m_endPointOffset.value = value.Value;
                        m_pointsDistance = Vector3.Distance(m_startPointOffset.enabled ?
                                            m_startPointOffset.value : Vector3.zero, m_endPointOffset.enabled ?
                                            m_endPointOffset.value : s_defaultOffset);
                    }
                    else { m_startPointOffset.enabled = false; }
                }
            }
        }

        public bool ShowConnectingLine
        {
            get => m_showConnectingLine;
            set
            {
                if (m_showConnectingLine != value)
                {
                    m_showConnectingLine = value;
                    if (m_lineRenderer)
                    {
                        m_lineRenderer.enabled = m_showConnectingLine;
                    }
                    if (m_lineSettings.targetPoint)
                    {
                        m_lineSettings.targetPoint.gameObject.SetActive(m_showConnectingLine);
                    }
                }
            }
        }

        public OptionalSpan WorldSize
        {
            get => m_advanced.worldSizeLimits;
            set
            {
                if (m_advanced.worldSizeLimits.value != value.value || m_advanced.worldSizeLimits.enabled != value.enabled)
                {
                    m_advanced.worldSizeLimits.enabled = value.enabled;
                    if (value.enabled)
                    {
                        m_advanced.worldSizeLimits.value = value.value;
                    }
                }
            }
        }

        public OptionalSpan VisibleDistance
        {
            get => m_advanced.distanceLimits;
            set
            {
                if (m_advanced.distanceLimits.value != value.value || m_advanced.distanceLimits.enabled != value.enabled)
                {
                    m_advanced.distanceLimits.enabled = value.enabled;
                    if (value.enabled)
                    {
                        m_advanced.distanceLimits.value = value.value;
                    }
                }
            }
        }

        public bool LookAtCamera
        {
            get => m_advanced.lookAtCamera;
            set
            {
                if (m_advanced.lookAtCamera != value)
                {
                    m_advanced.lookAtCamera = value;
                }
            }
        }

        public bool KeepSameOrientation
        {
            get => m_advanced.keepRelativePosition;
            set
            {
                if (m_advanced.keepRelativePosition != value)
                {
                    m_advanced.keepRelativePosition = value;
                }
            }
        }

        public bool DynamicSize
        {
            get => m_advanced.dynamicSize;
            set
            {
                if (m_advanced.dynamicSize != value)
                {
                    m_advanced.dynamicSize = value;
                }
            }
        }

        public PivotAxis RotationAxis
        {
            get => m_advanced.pivotAxis;
            set
            {
                if (m_advanced.pivotAxis != value)
                {
                    m_advanced.pivotAxis = value;
                }
            }
        }

        public float Progress
        {
            get => m_progressBar ? m_progressBar.Progress : 0;
            set
            {
                if (m_progressBar)
                {
                    m_progressBar.Progress = value;
                }
            }
        }

        public void RefreshElements()
        {
            ClearInvalidElements();
            UpdateElements();
        }

        public void ClearInvalidElements()
        {
            //m_elements.Clear();
            List<int> keysToRemove = null;
            if (m_elements != null)
            {
                foreach (var pair in m_elements)
                {
                    if (!pair.Value) 
                    {
                        if(keysToRemove == null)
                        {
                            keysToRemove = new List<int>();
                        }
                        keysToRemove.Add(pair.Key); 
                    }
                }
                if (keysToRemove != null)
                {
                    foreach (var keyToRemove in keysToRemove)
                    {
                        m_elements.Remove(keyToRemove);
                    }
                }
            }
        }

        private void UpdateElements()
        {
            if (m_elements == null) { m_elements = new Dictionary<int, BillboardElement>(); }
            m_maxId = 0;
            foreach (var element in GetComponentsInChildren<BillboardElement>(true))
            {
                if (element.ID <= 0 || (m_elements.TryGetValue(element.ID, out BillboardElement existing) && existing != element))
                {
                    while (m_elements.TryGetValue(m_maxId, out existing) && existing != element) { m_maxId++; }
                    element.ID = m_maxId;
                }
                m_elements[element.ID] = element;
                if (m_maxId < element.ID)
                {
                    m_maxId = element.ID;
                }
            }
        }

        public T GetElement<T>() where T : Component
        {
            return Elements.Select(e => e.Value.GetComponent<T>()).FirstOrDefault();
        }

        private void OnValidate()
        {
            if (m_advanced == null)
            {
                m_advanced = new AdvancedSettings();
            }
            if (m_lineSettings == null)
            {
                m_lineSettings = new LineRenderingSettings();
            }
            if (!m_canvas)
            {
                m_canvas = GetComponentInChildren<Canvas>(true);
            }
            if (m_canvas && (!m_canvasGroup || m_canvasGroup.gameObject != m_canvas.gameObject))
            {
                m_canvasGroup = m_canvas.GetComponent<CanvasGroup>();
                if (!m_canvasGroup)
                {
                    m_canvasGroup = m_canvas.gameObject.AddComponent<CanvasGroup>();
                }
            }
            if (!m_lineRenderer)
            {
                m_lineRenderer = GetComponent<LineRenderer>();
            }
            if (m_lineRenderer && m_lineSettings.targetPoint && m_lineSettings.useLineColor)
            {
                m_lineSettings.targetPoint.color = m_lineRenderer.startColor;
            }
            
            UpdateElements();
            m_inverseAnimDuration = m_advanced.animateDuration <= 0 ? 1 : 1f / m_advanced.animateDuration;

            if (!m_progressBar)
            {
                m_progressBar = GetComponentInChildren<BillboardProgressBar>(true);
            }
            m_allSet = m_canvas && m_canvasGroup && Camera;
        }

        private void ApplyIgnoreRenderDepth(bool forced)
        {
            if (Application.isPlaying)
            {
                if (m_textComponent is ITextComponent tComponent)
                {
                    tComponent.IsOverlay = m_ignoreRenderDepth;
                }
                if (m_ignoreRenderDepth)
                {
                    foreach (var graphic in GetComponentsInChildren<Graphic>(true))
                    {
                        Material updatedMaterial = new Material(graphic.materialForRendering);
                        updatedMaterial.SetInt("unity_GUIZTestMode", k_OverlayComparison);
                        graphic.material = updatedMaterial;
                    }
                }
                else if (forced)
                {
                    foreach (var graphic in GetComponentsInChildren<Graphic>(true))
                    {
                        graphic.material = null;
                    }
                }
            }
        }

        private bool HasLineRenderer()
        {
            return m_lineRenderer;
        }

        private void Awake()
        {
            if(!m_textComponent || !(m_textComponent is ITextComponent))
            {
                if (!m_textElement)
                {
                    m_textElement = GetComponentInChildren<Text>(true);
                }
            }
        }

        protected virtual void Start()
        {
            if (!m_allSet)
            {
                OnValidate();
            }
            m_inverseAnimDuration = m_advanced.animateDuration <= 0 ? 1 : 1f / m_advanced.animateDuration;
            m_pointsDistance = Vector3.Distance(m_startPointOffset.enabled ?
                m_startPointOffset.value : Vector3.zero, m_endPointOffset.enabled ?
                m_endPointOffset.value : s_defaultOffset);

            if (!m_progressBar)
            {
                m_progressBar = GetComponentInChildren<BillboardProgressBar>(true);
            }

            m_isVR = UnityEngine.XR.XRSettings.enabled;

            ApplyIgnoreRenderDepth(false);
        }

        private void OnEnable()
        {
            if (m_lineRenderer)
            {
                m_originalAlphaKeys = new GradientAlphaKey[m_lineRenderer.colorGradient.alphaKeys.Length];
                m_copyAlphaKeys = new GradientAlphaKey[m_lineRenderer.colorGradient.alphaKeys.Length];
                m_lineRenderer.colorGradient.alphaKeys.CopyTo(m_originalAlphaKeys, 0);
                m_lineRenderer.colorGradient.alphaKeys.CopyTo(m_copyAlphaKeys, 0);

                if (m_lineRenderer && m_lineSettings.targetPoint)
                {
                    if (m_lineSettings.useLineColor)
                    {
                        m_lineSettings.targetPoint.color = m_lineRenderer.startColor;
                    }
                    m_targetPointColor = m_lineSettings.targetPoint.color;
                }

                if (m_lineRenderer)
                {
                    m_lineRenderer.enabled = m_showConnectingLine;
                }
                if (m_lineSettings.targetPoint)
                {
                    m_lineSettings.targetPoint.gameObject.SetActive(m_showConnectingLine);
                }
            }

            m_canvasTransform = m_canvas.transform;
            m_scaler = m_canvasTransform.parent != transform ? m_canvasTransform.parent : m_canvasTransform;

            if (m_canvas)
            {
                m_canvas.worldCamera = Camera;
            }
        }

        private void OnDisable()
        {
            if (m_isVisible) { Hide(); }
        }

        private void OnDestroy()
        {
            if (m_targetPoint && !m_targetPoint.IsChildOf(transform))
            {
                DestroyTargetPoint();
            }
        }

        public void CopyValuesFrom(Billboard other)
        {
            m_ignoreRenderDepth = other.m_ignoreRenderDepth;

            m_startPointOffset.enabled = other.m_startPointOffset.enabled;
            m_startPointOffset.value = other.m_startPointOffset.value;

            m_endPointOffset.enabled = other.m_endPointOffset.enabled;
            m_endPointOffset.value = other.m_endPointOffset.value;

            m_advanced.animateDuration = other.m_advanced.animateDuration;
            m_advanced.distanceLimits.enabled = other.m_advanced.distanceLimits.enabled;
            m_advanced.distanceLimits.value = other.m_advanced.distanceLimits.value;
            m_advanced.dynamicSize = other.m_advanced.dynamicSize;
            m_advanced.fadeDistance.enabled = other.m_advanced.fadeDistance.enabled;
            m_advanced.fadeDistance.value = other.m_advanced.fadeDistance.value;
            m_advanced.keepRelativePosition = other.m_advanced.keepRelativePosition;
            m_advanced.lookAtCamera = other.m_advanced.lookAtCamera;
            m_advanced.pivotAxis = other.m_advanced.pivotAxis;
            m_advanced.sizeToScreenRatio = other.m_advanced.sizeToScreenRatio;
            m_advanced.worldSizeLimits.enabled = other.m_advanced.worldSizeLimits.enabled;
            m_advanced.worldSizeLimits.value = other.m_advanced.worldSizeLimits.value;

            if (m_lineRenderer)
            {
                m_originalAlphaKeys.CopyTo(m_copyAlphaKeys, 0);
                if (m_lineSettings.targetPoint)
                {
                    m_lineSettings.targetPoint.color = other.m_targetPointColor;
                }
            }
        }

        public void ShowOn(GameObject go)
        {
            ShowOn(go, Quaternion.identity);
        }

        public void ShowOn(GameObject go, Quaternion goOrientation)
        {
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
            var newTarget = go ? go.transform : null;
            if (newTarget != m_target)
            {
                m_canvasTransform.rotation = Quaternion.identity;
                transform.rotation = Quaternion.identity;
                m_scaler.localPosition = m_startPointOffset.enabled ? m_startPointOffset.value : Vector3.zero;
                m_canvasGroup.alpha = 0;
                m_scaler.localScale = m_canvasTargetScale = Vector3.one;
            }
            m_target = newTarget;
            m_targetRenderer = null;
            if (newTarget)
            {
                m_targetOrientation = goOrientation;
                m_targetRenderer = newTarget.GetComponent<Renderer>();
                m_targetDeltaRotation = transform.rotation * Quaternion.Inverse(newTarget.rotation);// * Quaternion.Inverse(transform.rotation);
                m_isVisible = true;
                m_canvasPositionTarget = m_endPointOffset.enabled ? (m_target.rotation * Quaternion.Inverse(goOrientation)) * m_endPointOffset.value : s_defaultOffset;
                m_canvasAlphaTarget = 1;

                if (m_showConnectingLine && m_lineRenderer)
                {
                    m_lineRenderer.enabled = true;
                    if (m_lineSettings.targetPoint && m_lineSettings.targetPointSprite
                        && m_lineSettings.targetPoint.sprite != m_lineSettings.targetPointSprite)
                    {
                        m_lineSettings.targetPoint.sprite = m_lineSettings.targetPointSprite;
                    }

                    CreateTargetPoint(newTarget);

                    UpdateLineRenderingPointsLite();
                }
                ChangedVisibility?.Invoke(this, true);
            }
            m_currentAlpha = m_canvasGroup.alpha;
            m_pointsDistance = Vector3.Distance(m_startPointOffset.enabled ?
                m_startPointOffset.value : Vector3.zero, m_endPointOffset.enabled ?
                m_endPointOffset.value : s_defaultOffset);
        }

        private void CreateTargetPoint(Transform newTarget)
        {
            if (!m_targetPoint)
            {
                m_targetPoint = new GameObject("Billboard_TargetPoint").transform;
            }
            m_targetPoint.gameObject.hideFlags = HideFlags.HideAndDontSave;
            m_targetPoint.SetParent(newTarget, false);
            if (m_startPointOffset.enabled)
            {
                m_targetPoint.position = newTarget.position + (newTarget.rotation * Quaternion.Inverse(m_targetOrientation)) * m_startPointOffset.value;
            }
            else if (m_targetRenderer)
            {
                m_targetPoint.position = m_targetRenderer.bounds.center;
            }
            else
            {
                m_targetPoint.position = newTarget.position;
            }
        }

        public void Hide()
        {
            DestroyTargetPoint();
            //if (m_targetPoint && !m_targetPoint.IsChildOf(transform)) { m_targetPoint.SetParent(transform); }
            m_isVisible = false;
            m_canvasAlphaTarget = 0;
            m_canvasPositionTarget = m_startPointOffset.enabled ? m_startPointOffset.value : Vector3.zero;

            if (!m_target)
            {
                m_currentAlpha = 0;
                ResetLineSettings();
                ChangedVisibility?.Invoke(this, false);
            }

            ResetProgress();
        }

        private void DestroyTargetPoint()
        {
            if (m_targetPoint)
            {
                if (Application.isPlaying) { Destroy(m_targetPoint.gameObject); }
                else { DestroyImmediate(m_targetPoint.gameObject); }
                m_targetPoint = null;
            }
        }

        public void Update()
        {
            Update(Time.deltaTime);
        }

        public void Update(float dt)
        {
            if (!Application.isPlaying) { return; }

            float prevAlpha = m_canvasGroup.alpha;
            if (m_target)
            {
                m_scaler.localPosition = Vector3.MoveTowards(m_scaler.localPosition, m_canvasPositionTarget, dt * m_pointsDistance * m_inverseAnimDuration);
                m_canvasGroup.alpha = m_currentAlpha = Mathf.MoveTowards(m_currentAlpha, m_canvasAlphaTarget, dt * m_inverseAnimDuration);

                if (m_canvasGroup.alpha == 0 && !m_isVisible)
                {
                    m_target = null;
                    ResetLineSettings();
                    ChangedVisibility?.Invoke(this, false);
                }
                else if (!m_isVisible)
                {
                    ApplyLineAlpha(m_currentAlpha);
                }
            }

            if (!m_isVisible || !m_target)
            {

                return;
            }

            transform.position = m_target.position;
            if (m_advanced.keepRelativePosition)
            {
                transform.rotation = Quaternion.identity;
            }
            else
            {
                transform.rotation = m_target.rotation * m_targetDeltaRotation;
            }

            var camera = Camera;
            if (m_canvas) { m_canvas.worldCamera = camera; }

            if (!camera || !camera.enabled)
            {
                m_canvasTransform.up = Vector3.up;
                return;
            }

            // Get a Vector that points from the target to the main camera.
            Vector3 canvasToCamera = camera.transform.position - m_canvasTransform.position;

            if (canvasToCamera.sqrMagnitude < camera.nearClipPlane * camera.nearClipPlane)
            {
                return;
            }

            if (m_advanced.lookAtCamera)
            {
                // Adjust for the pivot axis.
                switch (m_advanced.pivotAxis)
                {
                    case PivotAxis.CameraAlign:
                        if (m_isVR)
                        {
                            m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);
                        }
                        else
                        {
                            m_canvasTransform.eulerAngles = camera.transform.eulerAngles;
                        }
                        break;
                    case PivotAxis.Up:
                        canvasToCamera.y = 0.0f;
                        m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);
                        break;
                    case PivotAxis.Free:
                    default:
                        // No changes needed.
                        m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);
                        break;
                }
            }

            //m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);

            if (m_advanced.distanceLimits.enabled)
            {
                float canvasToCameraDistance = canvasToCamera.magnitude;
                if (!m_advanced.distanceLimits.value.IsValid(canvasToCamera.magnitude))
                {
                    m_canvasGroup.alpha = 0;
                }
                else if (m_advanced.fadeDistance.enabled && m_advanced.fadeDistance.value != 0)
                {
                    m_canvasGroup.alpha = Mathf.Clamp01(Mathf.Min((canvasToCameraDistance - m_advanced.distanceLimits.value.min) / m_advanced.fadeDistance.value, m_currentAlpha));
                }
            }
            else if (m_advanced.fadeDistance.enabled && m_advanced.fadeDistance.value != 0)
            {
                m_canvasGroup.alpha = Mathf.Clamp01(Mathf.Min((canvasToCamera.magnitude - camera.nearClipPlane) / m_advanced.fadeDistance.value, m_currentAlpha));
            }

            if (m_advanced.dynamicSize)
            {
                DynamicallyRescale(camera);
                m_scaler.localScale = m_advanced.springyResize ? Vector3.Lerp(m_scaler.localScale, m_canvasTargetScale, dt * 15) : m_canvasTargetScale;
            }

            if (m_showConnectingLine && m_lineRenderer)
            {
                if (m_canvasGroup.alpha != prevAlpha)
                {
                    ApplyLineAlpha(m_canvasGroup.alpha);
                }
                //if (m_startPointOffset.enabled)
                //{
                //    m_showPoint = m_startPointOffset.value + m_target.position;
                //}
                //else if (m_targetRenderer)
                //{
                //    m_showPoint = m_targetRenderer.bounds.center;
                //}
                //else
                //{
                //    m_showPoint = m_target.position;
                //}

                m_showPoint = m_targetPoint.position;

                if (m_lineSettings.dynamicLineScaling)
                {
                    ApplyLineWidth(camera);
                }
                if (m_advanced.dynamicSize || m_advanced.keepRelativePosition)
                {
                    UpdateLineRenderingPointsLite();
                }
                else
                {
                    m_lineRenderer.SetPosition(0, m_lineRenderer.transform.InverseTransformPoint(m_showPoint));
                }

                if (m_lineSettings.targetPoint)
                {
                    UpdateTargetPoint(camera);
                }
            }

        }

        private void ResetLineSettings()
        {
            if (m_lineRenderer)
            {
                m_lineRenderer.enabled = false;
                var colorGradient = m_lineRenderer.colorGradient;
                colorGradient.alphaKeys = m_originalAlphaKeys;
                m_lineRenderer.colorGradient = colorGradient;

                if (m_lineSettings.targetPoint)
                {
                    m_lineSettings.targetPoint.color = m_targetPointColor;
                }
            }
        }

        public void PreviewOn(GameObject target, Camera camera)
        {
            Hide();
            ShowOn(target, target.transform.rotation);

            m_scaler.localPosition = m_canvasPositionTarget;
            m_canvasGroup.alpha = m_canvasAlphaTarget;

            UpdatePreview(camera);
        }

        public void UpdatePreview(Camera camera)
        {
            transform.position = m_target.position;
            if (m_advanced.keepRelativePosition)
            {
                transform.rotation = Quaternion.identity;
            }
            else
            {
                transform.rotation = m_target.rotation * m_targetDeltaRotation;
            }

            if (!camera)
            {
                m_canvasTransform.up = Vector3.up;
                return;
            }

            // Get a Vector that points from the target to the main camera.
            Vector3 canvasToCamera = camera.transform.position - m_canvasTransform.position;

            if (canvasToCamera.sqrMagnitude < camera.nearClipPlane * camera.nearClipPlane)
            {
                return;
            }

            if (m_advanced.lookAtCamera)
            {
                // Adjust for the pivot axis.
                switch (m_advanced.pivotAxis)
                {
                    case PivotAxis.CameraAlign:
                        if (m_isVR)
                        {
                            m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);
                        }
                        else
                        {
                            m_canvasTransform.eulerAngles = camera.transform.eulerAngles;
                        }
                        break;
                    case PivotAxis.Up:
                        canvasToCamera.y = 0.0f;
                        m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);
                        break;
                    case PivotAxis.Free:
                    default:
                        m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);
                        break;
                }
            }

            //m_canvasTransform.rotation = Quaternion.LookRotation(-canvasToCamera);
            float prevAlpha = m_canvasGroup.alpha;
            if (m_advanced.distanceLimits.enabled)
            {
                float canvasToCameraDistance = canvasToCamera.magnitude;
                if (!m_advanced.distanceLimits.value.IsValid(canvasToCameraDistance))
                {
                    m_canvasGroup.alpha = 0;
                }
                else if (m_advanced.fadeDistance.enabled && m_advanced.fadeDistance.value != 0)
                {
                    m_canvasGroup.alpha = Mathf.Clamp01((canvasToCameraDistance - m_advanced.distanceLimits.value.min) / m_advanced.fadeDistance.value);
                }
            }
            else if (m_advanced.fadeDistance.enabled && m_advanced.fadeDistance.value != 0)
            {
                m_canvasGroup.alpha = Mathf.Clamp01((canvasToCamera.magnitude - camera.nearClipPlane) / m_advanced.fadeDistance.value);
            }

            if (m_advanced.dynamicSize && camera)
            {
                DynamicallyRescale(camera);
                m_scaler.localScale = m_canvasTargetScale;
            }

            if (m_showConnectingLine && m_lineRenderer)
            {
                if (m_canvasGroup.alpha != prevAlpha)
                {
                    ApplyLineAlpha(m_canvasGroup.alpha);
                }
                //if (m_startPointOffset.enabled)
                //{
                //    m_showPoint = m_startPointOffset.value + m_target.position;
                //}
                //else if (m_targetRenderer)
                //{
                //    m_showPoint = m_targetRenderer.bounds.center;
                //}
                //else
                //{
                //    m_showPoint = m_target.position;
                //}

                m_showPoint = m_targetPoint.position;

                if (m_lineSettings.dynamicLineScaling && camera)
                {
                    ApplyLineWidth(camera);
                }
                if (m_advanced.dynamicSize || m_advanced.keepRelativePosition)
                {
                    UpdateLineRenderingPointsLite();
                }
                else
                {
                    m_lineRenderer.SetPosition(0, m_lineRenderer.transform.InverseTransformPoint(m_showPoint));
                }

                if (m_lineSettings.targetPoint)
                {
                    UpdateTargetPoint(camera);
                }
            }
        }

        private void ApplyLineAlpha(float alpha)
        {
            for (int i = 0; i < m_copyAlphaKeys.Length; i++)
            {
                m_copyAlphaKeys[i].alpha = Mathf.Min(alpha, m_originalAlphaKeys[i].alpha);
            }
            var colorGradient = m_lineRenderer.colorGradient;
            colorGradient.alphaKeys = m_copyAlphaKeys;
            m_lineRenderer.colorGradient = colorGradient;
            if (m_lineSettings.targetPoint)
            {
                var color = m_lineSettings.targetPoint.color;
                color.a = Mathf.Min(alpha, m_targetPointColor.a);
                m_lineSettings.targetPoint.color = color;
            }
        }

        protected virtual void DynamicallyRescale(Camera camera)
        {
            (m_canvasTransform as RectTransform).GetWorldCorners(m_cornerPoints);
            float currentLength = Vector3.Distance(m_cornerPoints[2], m_cornerPoints[1]);
            float pixels = camera.scaledPixelWidth * m_advanced.sizeToScreenRatio;
            float targetLength = WeavrUIHelper.GetLengthOfPixelsAt(m_canvasTransform.position, camera, pixels);
            float ratio = m_advanced.worldSizeLimits.value.Clamp(targetLength) / currentLength;
            if (!float.IsInfinity(ratio) && Math.Abs(ratio - 1) > 0.001f)
            {
                m_canvasTargetScale *= Mathf.Abs(ratio);
            }
        }

        private void UpdateLineRenderingPointsLite()
        {
            m_lineRenderer.SetPosition(0, m_showPoint);

            if (m_lineSettings.closestPoint && m_lineSettings.canvasTargetPoints.Length > 0)
            {
                m_lineRenderer.positionCount = 2;
                var closestPoint = m_lineSettings.canvasTargetPoints[0].position;
                float distance = Vector3.Distance(m_showPoint, m_lineSettings.canvasTargetPoints[0].position);
                for (int i = 1; i < m_lineSettings.canvasTargetPoints.Length; i++)
                {
                    float newDistance = Vector3.Distance(m_showPoint, m_lineSettings.canvasTargetPoints[i].position);
                    if (newDistance < distance)
                    {
                        distance = newDistance;
                        closestPoint = m_lineSettings.canvasTargetPoints[i].position;
                    }
                }
                m_lineRenderer.SetPosition(1, closestPoint);
            }
            else
            {
                m_lineRenderer.positionCount = m_lineSettings.canvasTargetPoints.Length + 1;
                if (m_lineSettings.canvasTargetPoints.Length == 1)
                {
                    m_lineRenderer.SetPosition(1, m_lineSettings.canvasTargetPoints[0].position);
                }
                else if (m_lineSettings.canvasTargetPoints.Length == 2)
                {
                    float firstPointDistance = Vector3.Distance(m_showPoint, m_lineSettings.canvasTargetPoints[0].position);
                    float lastPointDistance = Vector3.Distance(m_showPoint, m_lineSettings.canvasTargetPoints[1].position);
                    if (firstPointDistance < lastPointDistance)
                    {
                        m_lineRenderer.SetPosition(1, m_lineSettings.canvasTargetPoints[0].position);
                        m_lineRenderer.SetPosition(2, m_lineSettings.canvasTargetPoints[1].position);
                    }
                    else
                    {
                        m_lineRenderer.SetPosition(1, m_lineSettings.canvasTargetPoints[1].position);
                        m_lineRenderer.SetPosition(2, m_lineSettings.canvasTargetPoints[0].position);
                    }
                }
                else
                {
                    for (int i = 0; i < m_lineSettings.canvasTargetPoints.Length; i++)
                    {
                        m_lineRenderer.SetPosition(i + 1, m_lineSettings.canvasTargetPoints[i].position);
                    }
                }
            }
        }

        private void UpdateTargetPoint(Camera camera)
        {
            m_lineSettings.targetPoint.size = Vector3.one * WeavrUIHelper.GetLengthOfPixelsAt(m_showPoint, camera, m_lineSettings.targetPointSize.x);
            m_lineSettings.targetPoint.transform.position = m_showPoint;
            m_lineSettings.targetPoint.transform.LookAt(camera.transform);
        }

        private void ApplyLineWidth(Camera camera)
        {
            float pixelsWidth = WeavrUIHelper.GetLengthOfPixelsAt(m_showPoint, camera, m_lineSettings.linePixelWidth);
            m_lineRenderer.widthMultiplier = pixelsWidth;
        }

        public void ResetProgress()
        {
            Progress = 0;
        }

        [Serializable]
        protected class AdvancedSettings
        {
            [Tooltip("The duration of the animation")]
            public float animateDuration = 1f;
            [Tooltip("Keep the billboard at the same position wrt the target")]
            public bool keepRelativePosition = true;

            [Tooltip("Whether to always face the camera or not")]
            public bool lookAtCamera = true;
            [HiddenBy(nameof(lookAtCamera))]
            [Tooltip("Specifies the axis about which the object will rotate.")]
            public PivotAxis pivotAxis = PivotAxis.Up;

            [Tooltip("Whether to dinamically resize the popup or not")]
            public bool dynamicSize = false;

            [Tooltip("Animate the resizing with a spring animation")]
            [HiddenBy(nameof(dynamicSize))]
            public bool springyResize = true;
            [Tooltip("The percentage of the screen width to match")]
            [HiddenBy(nameof(dynamicSize))]
            [Range(0.01f, 1)]
            public float sizeToScreenRatio = 0.2f;
            [Tooltip("The size limits of the billboard in world space")]
            [HiddenBy(nameof(dynamicSize))]
            public OptionalSpan worldSizeLimits = new Span(0.2f, 0.6f);
            public OptionalSpan distanceLimits = new Span(0.5f, 10f);

            [Tooltip("The distance to fade the billboard with respect to camera or distance limits")]
            public OptionalFloat fadeDistance = 0.5f;

        }

        [Serializable]
        private class LineRenderingSettings
        {
            public bool dynamicLineScaling;
            public float linePixelWidth = 2;

            [Tooltip("Trace the line from the origin to the closest point on canvas")]
            public bool closestPoint = true;
            [Draggable]
            //[HiddenBy(nameof(closestPoint))]
            public Transform[] canvasTargetPoints;

            [Draggable]
            [CanBeGenerated("TargetPoint", Relationship.Child)]
            public SpriteRenderer targetPoint;
            [Tooltip("Whether to use the line renderer start color as sprite color or not")]
            [HiddenBy(nameof(targetPoint))]
            public bool useLineColor = false;
            [Tooltip("The sprite sample to apply on target")]
            [HiddenBy(nameof(targetPoint))]
            public Sprite targetPointSprite;
            [Tooltip("The size in pixels of the target sprite")]
            [HiddenBy(nameof(targetPoint))]
            public Vector2 targetPointSize = new Vector2(16, 16);

        }
    }
}