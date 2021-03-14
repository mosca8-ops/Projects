using System;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.InteractionUI
{
    [AddComponentMenu("WEAVR/Setup/Interactable Panel")]
    [DefaultExecutionOrder(-28000)]
    public class InteractablePanel : MonoBehaviour, IInteractablePanel
    {
        private const float k_InchToMm = 25.4f;
        [SerializeField]
        [Tooltip("Whether to allow either only zoom or pan or both together at the same time")]
        private bool m_exclusiveZoom = false;
        [SerializeField]
        private bool m_debug = true;
        [SerializeField]
        [InfoBox(InfoBoxAttribute.InfoIconType.Information, "Having enabled raycasts will prevent this panel from capturing gestures")]
        private bool m_allowChildrenRaycasts = false;

        [Header("Mouse")]
        [SerializeField]
        private float m_mouseZoomSpeed = 0.05f;

        [Header("Touch", order = 1)]
        [InfoBox(InfoBoxAttribute.InfoIconType.Information,
                 "The touch sensitivity is based on <b>millimeters on screen surface</b>", order = 2)]
        [SerializeField]
        [Tooltip("Zoom sensitivity for touch input. [Meters/mm]")]
        [MeasureLabel("[fov/mm]")]
        private float m_touchZoomSensitivity = 0.02f;
        [SerializeField]
        [MeasureLabel("[m/mm]")]
        private float m_touchPanSesitivity = 0.1f;
        [SerializeField]
        [MeasureLabel("[deg/mm]")]
        private float m_touchRotateSesitivity = 5f;

        private Action m_action;
        private Func<bool> m_pointerIsDown;
        private Func<bool> m_pointerIsUp;
        private Func<Vector2> m_pointerPositionGetter;
        private bool m_isMyObject;
        private bool m_isPointerDown;
        private bool m_isPointerOver;
        private Vector2 m_totalDeltaMove;
        private RectTransform m_rect;
        private PointerEventData m_eventData;
        private InputType m_currentInputType;

        private bool m_zoomStarted;
        private bool m_panStarted;
        private GameObject m_pointerDownTarget;
        private GameObject m_dragTarget;
        private List<RaycastResult> m_raycasts = new List<RaycastResult>();

        private Vector3 m_lastPointerPosition;

        private int m_panFingerId;
        private Vector2[] m_currentZoomPositions;
        private Vector2[] m_lastZoomPositions;

        private float m_xRot = 0f;
        private float m_yRot = 0f;

        private float m_touchZoomFactor;
        private float m_touchPanFactor;
        private float m_touchRotateFactor;
        private float m_nonClickThreshold;

        private LowPassFilter m_touchZoomOffsetFilter = new LowPassFilter(2);
        private LowPassVector2Filter m_panOffsetFilter = new LowPassVector2Filter(2);

        public event RotateEvent Rotated;
        public event TranslateEvent Translated;
        public event ZoomEvent Zoomed;
        public event ClickEvent Clicked;

        private void OnValidate()
        {
            UpdateTouchFactors();
            DisableAnyRaycasts();
        }

        private void DisableAnyRaycasts()
        {
            var graphics = m_allowChildrenRaycasts ? GetComponents<Graphic>() : GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                if (graphic)
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        private void UpdateTouchFactors()
        {
            m_touchPanFactor = m_touchPanSesitivity / Screen.dpi * k_InchToMm;
            m_touchRotateFactor = m_touchRotateSesitivity / Screen.dpi * k_InchToMm;
            m_touchZoomFactor = m_touchZoomSensitivity / Screen.dpi * k_InchToMm;
            m_nonClickThreshold = Screen.dpi / (0.003f * k_InchToMm);
        }

        void Start()
        {
            m_rect = transform as RectTransform;
            m_eventData = new PointerEventData(EventSystem.current);

            m_currentZoomPositions = new Vector2[2];
            m_lastZoomPositions = new Vector2[2];

            ResetInput();
            UpdateTouchFactors();
            DisableAnyRaycasts();

            if (m_debug)
            {
                Rotated += (InputType inputType, Vector3 offset, Vector3 actual) =>
                {
                    WeavrDebug.Log(this, $"Rotating [{offset}][{actual}] with [{Enum.GetName(typeof(InputType), inputType)}]");
                };
                Translated += (InputType inputType, Vector3 offset, Vector3 actual) =>
                {
                    WeavrDebug.Log(this, $"Translated [{offset}][{actual}] with [{Enum.GetName(typeof(InputType), inputType)}]");
                };
                Zoomed += (InputType inputType, float offset, Vector2 center) =>
                {
                    WeavrDebug.Log(this, $"Zooming [{offset}][{center}] with [{Enum.GetName(typeof(InputType), inputType)}]");
                };
                Clicked += (i, screenPosition) =>
                {
                    WeavrDebug.Log(this, $"Clicked at [{screenPosition}] with {i}");
                };
            }
        }

        public void ResetInput()
        {
            if (Input.touchSupported && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                m_action = HandleTouch;
                m_pointerIsDown = TouchIsDown;
                m_pointerIsUp = TouchUp;
                m_pointerPositionGetter = GetTouchPosition;
                m_currentInputType = InputType.Touch;
            }
            else
            {
                m_action = HandleMouse;
                m_pointerIsDown = MouseIsDown;
                m_pointerIsUp = MouseUp;
                m_pointerPositionGetter = GetMousePosition;
                m_currentInputType = InputType.Mouse;
            }
        }

        private bool MouseUp()
        {
            return Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1);
        }

        private bool TouchUp()
        {
            return Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended;
        }

        private bool TouchIsDown()
        {
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

        private bool MouseIsDown()
        {
            return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.mouseScrollDelta.y != 0;
        }

        private Vector2 GetTouchPosition()
        {
            return Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
        }

        private Vector2 GetMousePosition()
        {
            return Input.mousePosition;
        }

        void LateUpdate()
        {
            if (m_pointerIsDown() && IsPointerOverPanel(m_pointerPositionGetter()) && !IsPointerOverOtherObjects(m_pointerPositionGetter()))
            {
                m_isMyObject = true;
                m_isPointerDown = true;
                m_totalDeltaMove = Vector2.zero;
            }
            else if(m_pointerIsUp())
            {
                m_isMyObject = false;
                if (m_isPointerDown)
                {
                    Clicked?.Invoke(m_currentInputType, m_pointerPositionGetter());
                    m_isPointerDown = false;
                }
            }

            if (m_isMyObject)
            {
                m_action.Invoke();
            }
        }

        static float GetScreenValue(float distanceInPixels, float sensitivity)
        {
            return distanceInPixels / Screen.dpi * sensitivity;
        }

        void HandleMouse()
        {
            if (m_isPointerOver && Input.mouseScrollDelta.y != 0)
            {
                m_isPointerDown = false;
                Zoomed?.Invoke(InputType.Mouse, Input.mouseScrollDelta.y * m_mouseZoomSpeed, Input.mousePosition);
            }
            // Panning
            if (Input.GetMouseButtonDown(1))
            {
                m_lastPointerPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(1))
            {
                Vector2 deltaPosition = Input.mousePosition - m_lastPointerPosition;
                m_totalDeltaMove += deltaPosition;
                m_isPointerDown &= m_totalDeltaMove.sqrMagnitude < m_nonClickThreshold; //pixels
                Translated?.Invoke(InputType.Mouse, new Vector3(deltaPosition.x * m_touchPanFactor,
                                                                deltaPosition.y * m_touchPanFactor, 0),
                                                       Input.mousePosition);

                m_lastPointerPosition = Input.mousePosition;
            }

            // Rotating
            else
            if (Input.GetMouseButtonDown(0))
            {
                m_lastPointerPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                Vector2 deltaPosition = Input.mousePosition - m_lastPointerPosition;
                m_totalDeltaMove += deltaPosition;
                m_isPointerDown &= m_totalDeltaMove.sqrMagnitude < m_nonClickThreshold; //pixels

                if (1 < Mathf.Abs(deltaPosition.y * m_touchRotateFactor) && Mathf.Abs(deltaPosition.y * m_touchRotateFactor) < 85)
                {
                    m_xRot = deltaPosition.y * m_touchRotateFactor;
                }
                else
                {
                    m_xRot = 0;
                }

                m_yRot = deltaPosition.x * m_touchRotateFactor;

                Rotated?.Invoke(InputType.Mouse, new Vector3(m_xRot, m_yRot, 0), Input.mousePosition);

                m_lastPointerPosition = Input.mousePosition;
            }
        }

        void HandleTouch()
        {
            switch (Input.touchCount)
            {
                case 1: // Rotating
                    Touch touch = Input.GetTouch(0);
                    if (touch.fingerId == m_panFingerId && touch.phase == TouchPhase.Moved)
                    {
                        m_isPointerDown = false;
                        if (/*1 < Mathf.Abs(touch.deltaPosition.y * m_touchRotateFactor) && */Mathf.Abs(touch.deltaPosition.y * m_touchRotateFactor) < 85)
                        {
                            m_xRot = touch.deltaPosition.y * m_touchRotateFactor;
                        }
                        else
                        {
                            m_xRot = 0;
                        }
                        m_yRot = touch.deltaPosition.x * m_touchRotateFactor;

                        Rotated?.Invoke(InputType.Touch, new Vector3(m_xRot, m_yRot, 0), touch.position);
                    }
                    else if (touch.phase == TouchPhase.Began)
                    {
                        m_panFingerId = touch.fingerId;
                    }
                    break;

                case 2: // Zooming & Panning
                    m_isPointerDown = false;
                    m_currentZoomPositions[0] = Input.GetTouch(0).position;
                    m_currentZoomPositions[1] = Input.GetTouch(1).position;
                    if (TouchIsValid(Input.GetTouch(0)) && TouchIsValid(Input.GetTouch(1)))
                    {
                        Vector2 newCenter = (m_currentZoomPositions[0] + m_currentZoomPositions[1]) * 0.5f;
                        Vector2 oldCenter = (m_lastZoomPositions[0] + m_lastZoomPositions[1]) * 0.5f;

                        // Zoom based on the distance between the new positions compared to the 
                        // distance between the previous positions.
                        float newDistance = Vector2.Distance(m_currentZoomPositions[0], m_currentZoomPositions[1]);
                        float oldDistance = Vector2.Distance(m_lastZoomPositions[0], m_lastZoomPositions[1]);
                        float offset = (newDistance - oldDistance) * 0.5f * m_touchZoomFactor;

                        if (m_exclusiveZoom)
                        {
                            if(!m_panStarted && Mathf.Abs(offset) < 2f)
                            {
                                Zoomed?.Invoke(InputType.Touch, m_touchZoomOffsetFilter.Filter(offset), newCenter);
                                m_zoomStarted |= Mathf.Abs(offset) > 0.05f;
                            }
                            else if (!m_zoomStarted)
                            {
                                Vector2 panOffset = newCenter - oldCenter;
                                Translated?.Invoke(InputType.Touch, m_panOffsetFilter.Filter(panOffset * m_touchPanFactor), newCenter);
                                m_panStarted |= panOffset.sqrMagnitude > 0.001f;
                            }
                        }
                        else
                        {
                            if (Mathf.Abs(offset) < 2f)
                            {
                                Zoomed?.Invoke(InputType.Touch, m_touchZoomOffsetFilter.Filter(offset), newCenter);
                            }

                            // Panning part
                            Vector2 panOffset = newCenter - oldCenter;
                            Translated?.Invoke(InputType.Touch, m_panOffsetFilter.Filter(panOffset * m_touchPanFactor), newCenter);
                        }
                    }
                    else
                    {
                        m_touchZoomOffsetFilter.Clear();
                        m_panOffsetFilter.Clear();
                        m_zoomStarted = false;
                        m_panStarted = false;
                    }

                    m_lastZoomPositions[0] = m_currentZoomPositions[0];
                    m_lastZoomPositions[1] = m_currentZoomPositions[1];
                    break;

                default:

                    break;
            }
        }

        private static bool TouchIsValid(in Touch touch)
        {
            return touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
        }

        private bool IsPointerOverPanel(Vector2 position)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(m_rect, position))
            {
                m_isPointerOver = true;
                return true;
            }
            m_isPointerOver = false;
            return false;
        }

        private bool IsPointerOverOtherObjects(Vector2 position)
        {
            //m_eventData = new PointerEventData(EventSystem.current);
            m_eventData.position = position;
            m_raycasts.Clear();
            EventSystem.current.RaycastAll(m_eventData, m_raycasts);
            if (m_raycasts.Count > 0)
            {
                foreach (var raycast in m_raycasts)
                {
                    if (raycast.gameObject.GetComponentInParent<IEventSystemHandler>() != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool Active { 
            get => gameObject.activeSelf; 
            set => gameObject.SetActive(value); 
        }
    }
}
