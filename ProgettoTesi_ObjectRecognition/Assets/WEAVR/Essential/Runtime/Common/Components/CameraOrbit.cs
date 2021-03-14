using System;
using TXT.WEAVR;
using TXT.WEAVR.Core;
using TXT.WEAVR.InteractionUI;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Camera System/Camera Orbit")]
    public class CameraOrbit : MonoBehaviour, IWeavrSingleton
    {
        private const float k_InchToMm = 25.4f;
        private const float k_NearZero = 0.001f;

        #region Static Part

        private static CameraOrbit _instance = null;

        public static CameraOrbit Instance
        {
            get
            {
                if (_instance == null)
                {

                    _instance = FindObjectOfType<CameraOrbit>();
                    if (_instance == null)
                    {

                        WeavrDebug.Log(nameof(CameraOrbit), "Creation of CameraOrbit singleton object");

                        // If no object is active, then create a new one
                        GameObject go = new GameObject("CameraOrbit");
                        go.hideFlags = HideFlags.DontSave;
                        _instance = go.AddComponent<CameraOrbit>();
                    }

                    _instance.Awake();
                }

                return _instance;
            }
        }

        private static GameObject s_target;

        public static void StopAnyCameraTransitions()
        {
            if (_instance)
            {
                _instance.m_cameraShouldTransition = false;
            }
        }

        #endregion

        private Camera m_sourceCamera;
        public Camera SourceCamera
        {
            get
            {
                if (m_sourceCamera == null)
                {
                    m_sourceCamera = GetComponent<Camera>();

                    if (m_sourceCamera == null)
                    {
                        m_sourceCamera = Camera.main;
                    }

                }
                return m_sourceCamera ? m_sourceCamera : WeavrCamera.CurrentCamera;
            }
        }

        public bool PreferBounds { get; set; }

        private Transform m_tempTransform;
        private Transform TargetPoint
        {
            get
            {
                if (!m_tempTransform)
                {
                    m_tempTransform = new GameObject("Temp_CameraOrbitTarget").transform;
                    m_tempTransform.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_tempTransform;
            }
        }

        private Transform m_cameraPoint;
        private Transform CameraPoint
        {
            get
            {
                if (!m_cameraPoint)
                {
                    m_cameraPoint = new GameObject("Temp_CameraOrbit_CameraPoint").transform;
                    m_cameraPoint.gameObject.hideFlags = HideFlags.HideAndDontSave;
                    m_cameraPoint.SetParent(TargetPoint, false);
                    m_cameraPoint.localPosition = Vector3.forward * (-_distance);
                    m_cameraPoint.LookAt(m_tempTransform, Vector3.up);
                }
                return m_cameraPoint;
            }
        }

        public static GameObject DefaultTarget { get; set; }

        private bool m_isLocked;
        public bool IsLocked
        {
            get => m_isLocked;
            set
            {
                if (m_isLocked != value)
                {
                    m_isLocked = value;
                    LockStatusChanged?.Invoke(m_isLocked);
                }
            }
        }

        public GameObject Target
        {
            get => s_target;
            set
            {
                if (s_target != value)
                {
                    s_target = value;
                    if (s_target)
                    {
                        TargetPoint.SetPositionAndRotation(s_target.transform.position, s_target.transform.rotation);
                        TargetPoint.forward = SourceCamera.transform.forward;
                        if (m_useFocalPoint)
                        {
                            FixFocalPoint(SourceCamera.transform, TargetPoint, CameraPoint);
                        }
                        else if (PreferBounds)
                        {
                            var renderers = s_target.GetComponentsInChildren<Renderer>();
                            if (renderers.Length > 0)
                            {
                                Bounds bounds = renderers[0].bounds;
                                for (int i = 1; i < renderers.Length; i++)
                                {
                                    bounds.Encapsulate(renderers[i].bounds);
                                }
                                TargetPoint.position = bounds.center;
                            }
                        }
                    }
                    m_useFocalPoint = false;
                    m_useCurrentDistance = true;

                    if (m_gesturesPanel != null && s_target)
                    {
                        SetInitialValues();
                    }

                    TargetChanged?.Invoke(s_target);
                    OnTargetChange.Invoke(s_target);
                }
            }
        }

        public float DistanceToCamera
        {
            get => _distance;
            set
            {
                value = Mathf.Max(value, 0.1f);
                m_useCurrentDistance = false;
                if (_distance != value)
                {
                    _distance = value;
                    CameraPoint.localPosition = Vector3.forward * (-_distance);
                    CameraPoint.LookAt(TargetPoint);
                }
            }
        }

        [Serializable]
        public class OnTargetChangeEvent : UnityEvent<bool>
        {
        }

        public static event Action<GameObject> TargetChanged;
        public event Action<bool> LockStatusChanged;
        public OnTargetChangeEvent OnTargetChange = new OnTargetChangeEvent();

        [SerializeField]
        [FormerlySerializedAs(nameof(_sensitivity))]
        private float m_touchSensitivity = 0.2f;
        [SerializeField]
        private float m_mouseSensitivity = 3f;

        public float MouseSensitivity => m_mouseSensitivity / Screen.dpi * k_InchToMm;
        public float TouchSensitivity => m_touchSensitivity / Screen.dpi * k_InchToMm;

        [SerializeField]
        private float _distance = 5f;

        private float m_xRot = 0f;
        private float m_yRot = 0f;
        private Vector2 m_deltaPosition;
        private bool m_smoothMovement = true;
        private Vector2 m_smoothRotation;
        private Vector3 m_smoothTranslation;

        private float _sensitivity;
        private Quaternion m_lastCameraRotation;
        private Vector3 m_lastCameraPosition;

        private float _mouseZoomSpeed = 15.0f;
        private float _touchZoomSpeed = 0.1f;
        private float _zoomMinBound = 9.5f;
        private float _zoomMaxBound = 99.100f;

        private float m_linearVelocity = 5;
        private float m_angularVelocity = 120;

        private bool m_cameraShouldTransition;
        private bool m_useCurrentDistance;
        private bool m_useFocalPoint;
        private Action _toExecute;
        private ProcedureRunner m_runner;

        private CameraOrbit m_lastInstance;

        private IInteractablePanel m_gesturesPanel;
        private int m_panFingerId;
        private Vector3 m_lastPointerPosition;
        private Vector2 m_totalDeltaMove;

        public IInteractablePanel GesturesPanel
        {
            get => m_gesturesPanel;
            set
            {
                if (m_gesturesPanel != value)
                {
                    if (m_gesturesPanel != null)
                    {
                        // Remove hooks
                        m_gesturesPanel.Rotated -= GesturesPanel_Rotated;
                        m_gesturesPanel.Zoomed -= GesturesPanel_Zoomed;
                        m_gesturesPanel.Translated -= GesturesPanel_Translated;
                    }
                    m_gesturesPanel = value;
                    if (m_gesturesPanel != null)
                    {
                        // Add hooks
                        m_gesturesPanel.Rotated -= GesturesPanel_Rotated;
                        m_gesturesPanel.Rotated += GesturesPanel_Rotated;
                        m_gesturesPanel.Zoomed -= GesturesPanel_Zoomed;
                        m_gesturesPanel.Zoomed += GesturesPanel_Zoomed;
                        m_gesturesPanel.Translated -= GesturesPanel_Translated;
                        m_gesturesPanel.Translated += GesturesPanel_Translated;
                    }
                }
            }
        }

        public void UseCurrentDistance()
        {
            m_useCurrentDistance = true;
        }

        public void UseFocalPointAsTarget()
        {
            m_useFocalPoint = true;
        }

        public void Refocus()
        {
            if (Target && SourceCamera)
            {
                TargetPoint.SetPositionAndRotation(Target.transform.position, Target.transform.rotation);
                SourceCamera.transform.SetPositionAndRotation(CameraPoint.position, CameraPoint.rotation);
            }
        }

        public void ResetToDefault(bool evenIfDefaultIsNull = false)
        {
            if (DefaultTarget || evenIfDefaultIsNull)
            {
                Target = null;
                m_useFocalPoint = true;
                Target = DefaultTarget;
            }
        }

        private void OnDisable()
        {
            if (_instance == this)
            {
                _instance = m_lastInstance;
            }
        }

        void Awake()
        {
            m_lastInstance = _instance;
            _instance = this;
            m_sourceCamera = GetComponent<Camera>();
            if (Input.touchSupported)
            {
                _sensitivity = m_touchSensitivity;
            }
            else
            {
                _sensitivity = m_mouseSensitivity;
            }
        }

        void Start()
        {
            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
            }

            m_runner = this.TryGetSingleton<ProcedureRunner>();

            if (m_runner)
            {
                m_runner.ProcedureStarted -= Runner_ProcedureStarted;
                m_runner.ProcedureStarted += Runner_ProcedureStarted;
            }
        }


        void LateUpdate()
        {
            if (IsLocked || !SourceCamera || !Target)
            {
                return;
            }

            if (m_cameraShouldTransition)
            {
                m_cameraShouldTransition = !TransitionTransforms(SourceCamera.transform, CameraPoint, m_linearVelocity, m_angularVelocity);
            }

            if (m_gesturesPanel == null)
            {
                if (Input.touchSupported)
                {
                    UseTouch();
                }
                else
                {
                    UseMouse();
                }
            }
            else if (m_smoothMovement && (!IsAlmostZero(m_smoothRotation) || !IsAlmostZero(m_smoothTranslation)))
            {
                TargetPoint.position += m_smoothTranslation;
                SourceCamera.transform.position += m_smoothTranslation;
                m_xRot = Mathf.Clamp(m_xRot - m_smoothRotation.x, -85, 85);
                m_yRot += m_smoothRotation.y;

                MoveCameraPrecise();
                m_lastCameraPosition = SourceCamera.transform.position;
                m_lastCameraRotation = SourceCamera.transform.rotation;

                m_smoothRotation.x = Mathf.Lerp(m_smoothRotation.x, 0, 10 * Time.deltaTime);
                m_smoothRotation.y = Mathf.Lerp(m_smoothRotation.y, 0, 10 * Time.deltaTime);
                m_smoothTranslation = Vector3.Lerp(m_smoothTranslation, Vector3.zero, 10 * Time.deltaTime);
            }
        }

        private bool IsAlmostZero(Vector2 v)
        {
            return Mathf.Abs(v.x) < k_NearZero && Mathf.Abs(v.y) < k_NearZero;
        }

        private bool IsAlmostZero(Vector3 v)
        {
            return Mathf.Abs(v.x) < k_NearZero && Mathf.Abs(v.y) < k_NearZero && Mathf.Abs(v.z) < k_NearZero;
        }

        private void GesturesPanel_Translated(InputType inputType, Vector3 offset, Vector3 actual)
        {
            if (SourceCamera && Target && !IsLocked)
            {
                var camera = SourceCamera.transform;
                if (!m_cameraShouldTransition && (m_lastCameraPosition != camera.position || m_lastCameraRotation != camera.rotation))
                {
                    SetInitialValues();
                }

                if (m_smoothMovement)
                {
                    m_smoothTranslation = camera.right * -offset.x + camera.up * -offset.y;
                }
                else
                {
                    var moveVector = camera.right * -offset.x + camera.up * -offset.y;
                    TargetPoint.position += moveVector;
                    camera.position += moveVector;

                    MoveCameraPrecise();

                    m_lastCameraPosition = camera.position;
                    m_lastCameraRotation = camera.rotation;
                }
            }
        }

        private void GesturesPanel_Zoomed(InputType inputType, float offset, Vector2 zoomCenter)
        {
            if (SourceCamera && Target && !IsLocked)
            {
                Zoom(offset, 100);
            }
        }

        private void GesturesPanel_Rotated(InputType inputType, Vector3 offset, Vector3 actual)
        {
            if (SourceCamera && Target && !IsLocked)
            {
                var camera = SourceCamera.transform;
                if (!m_cameraShouldTransition && (m_lastCameraPosition != camera.position || m_lastCameraRotation != camera.rotation))
                {
                    SetInitialValues();
                }

                if (m_smoothMovement)
                {
                    m_smoothRotation = offset;
                }
                else
                {
                    m_xRot = Mathf.Clamp(m_xRot - offset.x, -85, 85);
                    m_yRot += offset.y;

                    MoveCameraPrecise();

                    m_lastCameraPosition = camera.position;
                    m_lastCameraRotation = camera.rotation;
                }
            }
        }


        private bool TransitionTransforms(Transform a, Transform b, float linearVelocity, float angularVelocity)
        {
            a.position = Vector3.MoveTowards(a.position, b.position, Time.deltaTime * linearVelocity);
            a.rotation = Quaternion.RotateTowards(a.rotation, b.rotation, Time.deltaTime * angularVelocity);

            return a.position == b.position && a.rotation == b.rotation;
        }

        private void UseTouch()
        {
            // Rotation
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.fingerId == m_panFingerId && touch.phase == TouchPhase.Moved)
                {
                    float deltaX = 0;
                    float deltaY = 0;
                    if (1 < Mathf.Abs(touch.deltaPosition.y * TouchSensitivity * 20) && Mathf.Abs(touch.deltaPosition.y * TouchSensitivity * 20) < 85)
                    {
                        deltaX = touch.deltaPosition.y * TouchSensitivity;
                    }
                    else
                    {
                        deltaX = 0;
                    }
                    deltaY = touch.deltaPosition.x * TouchSensitivity;

                    m_xRot = Mathf.Clamp(m_xRot - deltaX, -85, 85);
                    m_yRot += deltaY;
                    MoveCameraPrecise();
                }
                else if (touch.phase == TouchPhase.Began)
                {
                    m_panFingerId = touch.fingerId;
                    SetInitialValues();
                }
            }
            // Pinch to zoom
            else if (Input.touchCount == 2)
            {
                // get current touch positions
                Touch tZero = Input.GetTouch(0);
                Touch tOne = Input.GetTouch(1);
                // get touch position from the previous frame
                Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
                Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;

                float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
                float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);

                // get offset value
                float deltaDistance = oldTouchDistance - currentTouchDistance;
                Zoom(deltaDistance, _touchZoomSpeed);
            }
        }

        private void UseMouse()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            Zoom(scroll, _mouseZoomSpeed);

            // Panning
            if (Input.GetMouseButtonDown(1))
            {
                SetInitialValues();
                m_lastPointerPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(1))
            {
                Vector2 deltaPosition = Input.mousePosition - m_lastPointerPosition;
                m_totalDeltaMove += deltaPosition;
                m_lastPointerPosition = Input.mousePosition;
                var camera = SourceCamera.transform;
                var moveVector = camera.right * -deltaPosition.x * MouseSensitivity + camera.up * -deltaPosition.y * MouseSensitivity;
                TargetPoint.position += moveVector;
                camera.position += moveVector;
            }

            // Rotating
            else
            if (Input.GetMouseButtonDown(0))
            {
                m_lastPointerPosition = Input.mousePosition;
                SetInitialValues();
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 deltaPosition = Input.mousePosition - m_lastPointerPosition;
                m_totalDeltaMove += deltaPosition;

                float deltaX;
                if (1 < Mathf.Abs(deltaPosition.y * MouseSensitivity) && Mathf.Abs(deltaPosition.y * MouseSensitivity) < 85)
                {
                    deltaX = deltaPosition.y * MouseSensitivity;
                }
                else
                {
                    deltaX = 0;
                }

                float deltaY = deltaPosition.x * MouseSensitivity;

                m_lastPointerPosition = Input.mousePosition;
                m_xRot = Mathf.Clamp(m_xRot - deltaX, -85, 85);
                m_yRot += deltaY;
                MoveCameraPrecise();
            }
        }

        private void MoveCameraPrecise()
        {
            var camera = SourceCamera.transform;
            var target = TargetPoint;
            var cameraPoint = CameraPoint;
            var euler = target.localEulerAngles;

            if (m_useCurrentDistance)
            {
                DistanceToCamera = Vector3.Distance(target.position, camera.position);
            }
            if (m_useFocalPoint)
            {
                FixFocalPoint(camera, target, cameraPoint);
            }

            m_cameraShouldTransition = cameraPoint.position != camera.position || cameraPoint.rotation != camera.rotation;

            euler.x = m_xRot;
            target.localEulerAngles = euler;
            euler = target.eulerAngles;
            euler.y = m_yRot;
            target.eulerAngles = euler;

            if (m_cameraShouldTransition)
            {
                m_cameraShouldTransition = !TransitionTransforms(SourceCamera.transform, CameraPoint, m_linearVelocity, m_angularVelocity);
            }
            else
            {
                camera.position = cameraPoint.position;
                camera.rotation = cameraPoint.rotation;
            }
        }

        private void FixFocalPoint(Transform camera, Transform target, Transform cameraPoint)
        {
            m_useFocalPoint = false;
            target.position = camera.position + Vector3.Project(Target.transform.position - camera.position, camera.forward);
            target.forward = camera.forward;
            cameraPoint.position = camera.position;
            cameraPoint.rotation = camera.rotation;
            m_lastCameraPosition = camera.position;
            m_lastCameraRotation = camera.rotation;
        }

        private void SetInitialValues()
        {
            // Set distance to the current distance of the target
            //_distance = Vector3.Distance(SourceCamera.transform.position, Target.transform.position);

            // Set the x and y rotation to the new rotation relative to the pivot
            Vector3 pivotToHere = SourceCamera.transform.position - Target.transform.position;
            Vector3 tempVec = Vector3.ProjectOnPlane(pivotToHere, Vector3.up);
            if (pivotToHere.x > 0f)
                m_yRot = Vector3.Angle(Vector3.forward, tempVec) + 180f;
            else
                m_yRot = -Vector3.Angle(Vector3.forward, tempVec) + 180f;

            if (pivotToHere.y > 0f)
                m_xRot = Vector3.Angle(tempVec, pivotToHere);
            else
                m_xRot = -Vector3.Angle(tempVec, pivotToHere);
        }

        private void Zoom(float deltaMagnitudeDiff, float speed)
        {
            // set min and max value of Clamp function upon your requirement
            SourceCamera.fieldOfView = Mathf.Clamp(SourceCamera.fieldOfView - deltaMagnitudeDiff * speed, _zoomMinBound, _zoomMaxBound);
        }

        private void Runner_ProcedureStarted(ProcedureRunner runner, Procedure.Procedure procedure, ExecutionMode mode)
        {
            if (procedure && mode)
            {
                enabled = !mode.UsesWorldNavigation;
            }
        }

        public void EnableComponent(bool value)
        {
            enabled = value;
        }
    }
}
