using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.UI
{
    public abstract class VR_AbstractMenu : MonoBehaviour
    {
        [SerializeField]
        protected bool m_isActive = true;

        [Header("Canvas")]
        [SerializeField]
        protected Canvas m_canvas;
        [SerializeField]
        protected Transform m_canvasWorldPoint;

        protected bool m_isVisible;

        public bool IsVisible {
            get { return m_isVisible; }
            set {
                if (m_isVisible != value)
                {
                    m_isVisible = value;
                    if (value) { Show(); }
                    else { Hide(); }
                }
            }
        }

        public bool IsActive
        {
            get { return m_isActive; }
            set
            {
                if (m_isActive != value)
                {
                    m_isActive = value;
                    if (!value)
                    {
                        IsVisible = false;
                    }
                }
            }
        }

        protected virtual void OnValidate()
        {
            if (!m_canvas)
            {
                m_canvas = GetComponentInChildren<Canvas>(true);
            }

            if (m_canvas && m_canvas.worldCamera)
            {
                m_canvas.worldCamera = WeavrCamera.CurrentCamera;
            }

            if(m_canvasWorldPoint && m_canvas)
            {
                if(m_canvas.transform.parent == transform)
                {
                    m_canvasWorldPoint = m_canvas.transform;
                }
                else
                {
                    m_canvasWorldPoint = m_canvas.transform.parent;
                }
            }
        }

        protected virtual void Clear()
        {

        }

        public virtual void Hide()
        {
            Clear();
            m_canvas.gameObject.SetActive(false);
            m_isVisible = false;
            //if(!m_canvasWorldPoint.IsChildOf(transform)) { m_canvasWorldPoint.SetParent(transform, false); }
        }

        public virtual void Show(Transform point = null)
        {
            m_isVisible = true;
            if (point != null && !m_canvasWorldPoint.IsChildOf(point))
            {
                m_canvasWorldPoint.SetParent(point, false);
            }
            m_canvas.gameObject.SetActive(true);
        }

        // Use this for initialization
        protected virtual void Start()
        {
            if(m_canvasWorldPoint == null && m_canvas != null)
            {
                if (m_canvas.transform.parent == transform)
                {
                    m_canvasWorldPoint = m_canvas.transform;
                }
                else
                {
                    m_canvasWorldPoint = m_canvas.transform.parent;
                }
            }
        }
    }
}
