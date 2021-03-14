namespace TXT.WEAVR.Interaction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.UI;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public abstract class AbstractMenu3D : MonoBehaviour
    {
        #region [  STATIC PART  ]
        private static AbstractMenu3D s_currentlyShown;
        #endregion

        [Header("Canvas")]
        [Draggable]
        public Camera canvasCamera;
        [SerializeField]
        [Draggable]
        protected Canvas m_mainCanvas;
        [SerializeField]
        [Tooltip("The object which can scale the canvas, usually its parent")]
        [Draggable]
        protected Transform m_canvasScaler;
        [SerializeField]
        [Tooltip("The default width of the canvas in screen percentage")]
        [Range(0.01f, 1f)]
        protected float m_canvasToScreenRatio = 0.2f;

        [SerializeField]
        [Draggable]
        protected Popup3D m_popup;

        protected bool m_isShowingUp;
        protected Action m_onHide;
        
        public bool IsVisible {
            get { return m_popup.IsVisible; }
            set {
                if(m_popup.IsVisible != value) {
                    m_popup.IsVisible = value;
                    if(value) { Show(); }
                    else { Hide(); }
                }
            }
        }

        protected virtual void OnValidate() {
            if (!m_mainCanvas) {
                m_mainCanvas = GetComponentInChildren<Canvas>(true);
                if (m_mainCanvas) {
                    canvasCamera = m_mainCanvas.worldCamera;
                }
            }
            if (!canvasCamera) {
                canvasCamera = Camera.main;
                if (!canvasCamera && m_mainCanvas) {
                    canvasCamera = m_mainCanvas.worldCamera ?? FindObjectOfType<Camera>();
                }
            }
            if (m_mainCanvas && m_mainCanvas.worldCamera != canvasCamera) {
                m_mainCanvas.worldCamera = canvasCamera;
            }
            if(!m_canvasScaler && m_mainCanvas) {
                m_canvasScaler = m_mainCanvas.transform.parent;
            }
            if(!m_popup) {
                m_popup = GetComponent<Popup3D>();
            }
            if(m_popup && m_canvasScaler) {
                m_popup.CanvasScaler = m_canvasScaler;
            }
            m_canvasToScreenRatio = Mathf.Clamp01(m_canvasToScreenRatio);
        }

        protected virtual void Clear() {
            
        }

        public virtual void Hide() {
            Clear();

            if(s_currentlyShown == this) { s_currentlyShown = null; }
            m_popup.Hide();

            if(m_onHide != null)
            {
                m_onHide();
                m_onHide = null;
            }
        }

        public virtual void Show(Transform point, bool adaptScale, bool attachAsChild = false, Action onHideCallback = null) {
            if(s_currentlyShown != null && s_currentlyShown != this) {
                s_currentlyShown.Hide();
            }
            s_currentlyShown = this;
            m_isShowingUp = true;

            if (onHideCallback != null)
            {
                if (m_onHide == null)
                {
                    m_onHide = onHideCallback;
                }
                else
                {
                    m_onHide += onHideCallback;
                }
            }

            m_popup.Show(point, attachAsChild);
            if (adaptScale) {
                WeavrUIHelper.RescaleWorldCanvasToPixels(m_mainCanvas, m_canvasScaler, canvasCamera, m_canvasToScreenRatio * Screen.width);
            }
        }

        protected Bounds? GetBounds(Transform point) {
            Collider collider = point.GetComponent<Collider>() ?? point.GetComponentInChildren<Collider>();
            if (collider != null) {
                return collider.bounds;
            }
            else {
                Renderer renderer = point.GetComponent<Renderer>() ?? point.GetComponentInChildren<Renderer>();
                if (renderer != null) {
                    return renderer.bounds;
                }
            }

            return null;
        }

        public virtual void Show() {
            m_isShowingUp = true;
            transform.localScale = Vector3.one;
            m_popup.IsVisible = false;
            m_popup.IsVisible = true;
        }

        public virtual void Show(Vector3 point) {
            m_isShowingUp = true;
            transform.localScale = Vector3.one;
            m_popup.Show(point);
        }

        // Use this for initialization
        protected virtual void Start() {
            if(m_popup == null) {
                m_popup = GetComponent<Popup3D>();
            }
        }

        protected virtual void LateUpdate()
        {
            if (IsVisible && !m_isShowingUp && !EventSystem.current.currentSelectedGameObject && !EventSystem.current.alreadySelecting)
            {
                if (Input.touchCount > 0)
                {
                    bool shouldHide = true;
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        var touch = Input.GetTouch(i);
                        if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                        {
                            shouldHide = false;
                            break;
                        }
                    }
                    if (shouldHide)
                    {
                        Hide();
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    Hide();
                }
            }
            m_isShowingUp = false;
        }
    }
}