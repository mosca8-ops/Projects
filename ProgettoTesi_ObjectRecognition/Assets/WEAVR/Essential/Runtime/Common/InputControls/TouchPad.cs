using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.InputControls
{
	[RequireComponent(typeof(Image))]
    [AddComponentMenu("")]
	public class TouchPad : AbstractTouchControl, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		// Options for which axes to use
		public enum AxisOption
		{
			Both, // Use both
			Horizontal, // Only horizontal
			Vertical // Only vertical
		}


		public enum ControlStyle
		{
			Absolute, // operates from teh center of the image
			Relative, // operates from the center of the initial touch
			Swipe, // swipe to touch touch no maintained center
		}


		public AxisOption axesToUse = AxisOption.Both; // The options for the axes that the still will use
		public ControlStyle controlStyle = ControlStyle.Absolute; // control style to use
		public string horizontalAxisName = "Horizontal"; // The name given to the horizontal axis for the cross platform input
		public string verticalAxisName = "Vertical"; // The name given to the vertical axis for the cross platform input
		public float Xsensitivity = 1f;
		public float Ysensitivity = 1f;

        [Space]
        public bool normalizeAxes = true; // Whether to normalize pointer delta or not
        public AxisOption priorityAxes = AxisOption.Both; // The priority axes to not filter out
        public float priorityThreshold = 0.1f; // The threshold to block the lower priority axes

        [Space]
        [Tooltip("Will always show the touchpad. Otherwise only the first time will be visible")]
        public bool showAlways = true;
        [Common.EnableIfComponentExists(typeof(Animator))]
        public float showHintsDelay = 4f; // The inactivity time to show the hints

        [Space]
        [SerializeField]
        private bool m_debug;
        [SerializeField]
        [ShowAsReadOnly]
        private float m_xValue;
        [SerializeField]
        [ShowAsReadOnly]
        private float m_yValue;


		Vector3 m_StartPos;
		Vector2 m_PreviousDelta;
		Vector3 m_JoytickOutput;
		bool m_UseX; // Toggle for using the x axis
		bool m_UseY; // Toggle for using the Y axis
		CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis; // Reference to the joystick in the cross platform input
		CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis; // Reference to the joystick in the cross platform input
		bool m_Dragging;
        bool m_initialized;
		int m_touchIndex = -10;
		int? m_fingerId = null;
		Vector2 m_PreviousTouchPos; // swipe style control touch

        private float m_scaleX;
        private float m_scaleY;
        private Vector3 m_Center;
        private Image m_Image;
		Vector3 m_PreviousMouse;

        private CanvasGroup m_canvasGroup;

        private IEventSystemHandler m_surrogateHandler;

        private List<RaycastResult> m_raycasts = new List<RaycastResult>();

        private static readonly Touch k_EmptyTouch = new Touch();

        private Canvas m_canvas;

        private void OnValidate()
        {
            
        }

        void OnEnable()
		{
			CreateVirtualAxes();
            if (m_initialized)
            {
                FullReveal();
            }
		}

        void Start()
        {
            m_Image = GetComponent<Image>();
            m_Center = m_Image.transform.position;
            m_canvas = GetComponentInParent<Canvas>();

            m_canvasGroup = GetComponent<CanvasGroup>();
            m_initialized = true;
            //animateAlways &= m_animator;
        }

        void CreateVirtualAxes()
		{
			// set axes to use
			m_UseX = (axesToUse == AxisOption.Both || axesToUse == AxisOption.Horizontal);
			m_UseY = (axesToUse == AxisOption.Both || axesToUse == AxisOption.Vertical);

			// create new axes based on axes to use
			if (m_UseX)
			{
				m_HorizontalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(horizontalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_HorizontalVirtualAxis);
			}
			if (m_UseY)
			{
				m_VerticalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(verticalAxisName);
				CrossPlatformInputManager.RegisterVirtualAxis(m_VerticalVirtualAxis);
			}
		}

		void UpdateVirtualAxes(Vector3 value)
		{
            if (normalizeAxes)
            {
                value = value.normalized;
            }
			if (m_UseX)
			{
				m_HorizontalVirtualAxis.Update(value.x);
			}

			if (m_UseY)
			{
				m_VerticalVirtualAxis.Update(value.y);
			}
		}


        public override bool CanHandlePointerDown(PointerEventData data)
        {
            return transform is RectTransform r 
                && ((m_canvas.worldCamera && RectTransformUtility.RectangleContainsScreenPoint(r, data.position, m_canvas.worldCamera)) 
                        ||  r.rect.Contains(r.InverseTransformPoint(data.position)));
            //return RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, data.position);
        }

		public void OnPointerDown(PointerEventData data)
        {
            if (m_Dragging)
            {
                return;
            }
            
            if (!TryFindSurrogateHandler(data))
            {
                data.Use();
            }
            else if(m_surrogateHandler is IPointerDownHandler pointerDown)
            {
                UpdateVirtualAxes(Vector3.zero);
                if (m_surrogateHandler is IPointerEnterHandler pointerEnter)
                {
                    pointerEnter.OnPointerEnter(data);
                }
                pointerDown.OnPointerDown(data);
            }
        }

        private void InitializeTouchPadMovement(PointerEventData data)
        {
            if(m_animator)
            {
                StopAllCoroutines();
                FaintReveal();
                StartCoroutine(DelayedHide());
            }
            else if(m_canvasGroup)
            {
                m_canvasGroup.alpha = 0;
            }

            m_Dragging = true;
            m_touchIndex = data.pointerId;

            if (Application.isEditor)
            {
                m_PreviousMouse = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
            }
            else
            {
                if (controlStyle != ControlStyle.Absolute)
                {
                    m_Center = data.position;
                }

                if (Input.touchCount > m_touchIndex && m_touchIndex >= 0)
                {
                    m_fingerId = Input.GetTouch(m_touchIndex).fingerId;
                    m_PreviousTouchPos = Input.GetTouch(m_touchIndex).position;
                }
                else
                {
                    m_fingerId = null;
                }
            }
        }

        private bool TryFindSurrogateHandler(PointerEventData data)
        {
            m_raycasts.Clear();
            EventSystem.current.RaycastAll(data, m_raycasts);

            foreach (var result in m_raycasts)
            {
                if (!result.gameObject || result.gameObject == gameObject) { continue; }
                m_surrogateHandler = result.gameObject.GetComponentInParent<IEventSystemHandler>();
                if (m_surrogateHandler != null 
                    && (m_surrogateHandler is IBeginDragHandler 
                        || m_surrogateHandler is IPointerClickHandler 
                        || m_surrogateHandler is IPointerDownHandler 
                        || m_surrogateHandler is IPointerUpHandler)) {
                    return true;
                }
            }
            return false;
        }

        void Update()
		{
            if (m_debug)
            {
                m_xValue = m_HorizontalVirtualAxis.GetValue;
                m_yValue = m_VerticalVirtualAxis.GetValue;
            }
			if (!m_Dragging)
			{
				return;
			}
            if (m_fingerId.HasValue && TryGetTouch(out Touch touch))
            {
                if (transform.hasChanged)
                {
                    var size = (transform as RectTransform).rect.size;
                    float lengthToNormalize = Mathf.Max(size.x, size.y);
                    m_scaleX = Xsensitivity / lengthToNormalize;
                    m_scaleY = Ysensitivity / lengthToNormalize;
                    transform.hasChanged = false;
                }

                Vector2 pointerDelta;

                if (controlStyle == ControlStyle.Swipe)
                {
                    m_Center = m_PreviousTouchPos;
                    m_PreviousTouchPos = touch.position;
                }
                pointerDelta = new Vector2(touch.position.x - m_Center.x, touch.position.y - m_Center.y);
                if (priorityAxes == AxisOption.Horizontal)
                {
                    if (pointerDelta.normalized.x > priorityThreshold)
                    {
                        pointerDelta.y = 0;
                    }
                }
                else if (priorityAxes == AxisOption.Vertical)
                {
                    if (pointerDelta.normalized.y > priorityThreshold)
                    {
                        pointerDelta.x = 0;
                    }
                }
                UpdateVirtualAxes(new Vector3(pointerDelta.x * m_scaleX, pointerDelta.y * m_scaleY, 0));
            }
            else if (Application.isEditor)
            {
                if (transform.hasChanged)
                {
                    var size = (transform as RectTransform).rect.size;
                    float lengthToNormalize = Mathf.Max(size.x, size.y);
                    m_scaleX = Xsensitivity / lengthToNormalize;
                    m_scaleY = Ysensitivity / lengthToNormalize;
                    transform.hasChanged = false;
                }
                Vector2 pointerDelta = new Vector2(Input.mousePosition.x - m_PreviousMouse.x, Input.mousePosition.y - m_PreviousMouse.y);
                if (controlStyle == ControlStyle.Swipe)
                {
                    m_PreviousMouse = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
                }
                UpdateVirtualAxes(new Vector3(pointerDelta.x * m_scaleX, pointerDelta.y * m_scaleY, 0));
            }
            else
            {
                Debug.LogError($"Cannot get touch. Id = {m_touchIndex}  | FingerId = {m_fingerId} | Count = {Input.touchCount}");
                EndTouchPhase();
            }
        }

        private void EndTouchPhase(bool clearSurrogate = true)
        {
            m_Dragging = false;
            m_touchIndex = -10;
            m_fingerId = null;
            if (clearSurrogate)
            {
                m_surrogateHandler = null;
            }
            UpdateVirtualAxes(Vector3.zero);

            if (showAlways)
            {
                if (m_animator)
                {
                    StopAllCoroutines();
                    StartCoroutine(DelayedFullReveal());
                }
                else if(m_canvasGroup)
                {
                    m_canvasGroup.alpha = 1;
                }
            }
        }

        private IEnumerator DelayedFullReveal()
        {
            yield return new WaitForSeconds(showHintsDelay);
            FullReveal();
        }

        private IEnumerator DelayedHide()
        {
            yield return new WaitForSeconds(showHintsDelay);
            Hide();
        }

        private bool TryGetTouch(out Touch touch)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                touch = Input.GetTouch(i);
                if(touch.fingerId == m_fingerId)
                {
                    return true;
                }
            }
            touch = k_EmptyTouch;
            return false;
        }

        public void OnPointerUp(PointerEventData data)
		{
            if(m_surrogateHandler as Component && !m_Dragging)
            {
                if(m_surrogateHandler is IPointerClickHandler clickHandler)
                {
                    clickHandler.OnPointerClick(data);
                }
                if (m_surrogateHandler is IPointerUpHandler upHandler)
                {
                    upHandler.OnPointerUp(data);
                }
                if (m_surrogateHandler is IPointerExitHandler exitHandler)
                {
                    exitHandler.OnPointerExit(data);
                }
            }
            if (!(m_surrogateHandler is IEndDragHandler))
            {
                EndTouchPhase();
            }

            // The end drag will handle the end of touch phase

            if (Application.isEditor)
            {
                m_PreviousMouse = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
            }
            data.Use();
		}

		void OnDisable()
		{
            StopAllCoroutines();
            if (CrossPlatformInputManager.AxisExists(horizontalAxisName))
            {
                CrossPlatformInputManager.UnRegisterVirtualAxis(horizontalAxisName);
            }

            if (CrossPlatformInputManager.AxisExists(verticalAxisName))
            {
                CrossPlatformInputManager.UnRegisterVirtualAxis(verticalAxisName);
            }
		}

        public void OnBeginDrag(PointerEventData eventData)
        {
            IDragHandler dragHandler;
            if (m_surrogateHandler is Component component && (dragHandler = component.GetComponentInParent<IDragHandler>()) != null)
            {
                var requireOnPointerDown = dragHandler != m_surrogateHandler;
                EndTouchPhase(false);
                m_surrogateHandler = dragHandler;
                if (requireOnPointerDown)
                {
                    (m_surrogateHandler as IPointerEnterHandler)?.OnPointerEnter(eventData);
                    (m_surrogateHandler as IPointerDownHandler)?.OnPointerDown(eventData);
                }
                if (m_surrogateHandler as Component && m_surrogateHandler is IPointerClickHandler clickHandler)
                {
                    clickHandler.OnPointerClick(eventData);
                }
                if (m_surrogateHandler as Component && m_surrogateHandler is IBeginDragHandler beginDragHandler)
                {
                    beginDragHandler.OnBeginDrag(eventData);
                }
            }
            else // TODO: This may cause issues with our drag logic
            //if (!eventData.used)
            {
                InitializeTouchPadMovement(eventData);
                if (m_surrogateHandler is IPointerExitHandler pointerExit)
                {
                    eventData.pointerEnter = (m_surrogateHandler as Component).gameObject;
                    pointerExit.OnPointerExit(eventData);
                    (m_surrogateHandler as IPointerUpHandler)?.OnPointerUp(eventData);
                    (m_surrogateHandler as IDeselectHandler)?.OnDeselect(eventData);
                    m_surrogateHandler = null;
                }
            }
        }



        public void OnDrag(PointerEventData eventData)
        {
            if (!m_Dragging && m_surrogateHandler as Component && m_surrogateHandler is IDragHandler dragHandler)
            {
                dragHandler.OnDrag(eventData);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_Dragging && m_surrogateHandler as Component && m_surrogateHandler is IEndDragHandler dragHandler)
            {
                dragHandler.OnEndDrag(eventData);
            }
            EndTouchPhase();
        }
    }
}