using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Text Editing/Selectable Text")]
    public abstract class SelectableText : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        protected Graphic m_background;
        [SerializeField]
        [Draggable]
        protected Text m_foreground;

        [Header("Selected")]
        [SerializeField]
        protected Color m_bgSelected;
        [SerializeField]
        protected Color m_fgSelected;

        [Header("Unselected")]
        [SerializeField]
        protected Color m_bgUnselected;
        [SerializeField]
        protected Color m_fgUnselected;


        protected virtual void Reset()
        {
            var textComponent = GetComponent<Text>();
            if (textComponent)
            {
                if (Application.isPlaying)
                {
                    Destroy(textComponent);
                }
                else
                {
                    DestroyImmediate(textComponent);
                }
            }
            m_foreground = transform.Find("Text")?.GetComponent<Text>();
            if (!m_foreground)
            {
                var fg = new GameObject("Text").AddComponent<RectTransform>();
                SetAndStretchToParentSize(fg, transform as RectTransform);
                m_foreground = fg.gameObject.AddComponent<Text>();
                m_foreground.color = Color.black;
            }
            m_background = transform.Find("Background")?.GetComponent<Graphic>();
            if (!m_background)
            {
                var bg = new GameObject("Background").AddComponent<RectTransform>();
                SetAndStretchToParentSize(bg, transform as RectTransform);
                m_background = bg.gameObject.AddComponent<Image>();
                m_background.color = new Color(1, 1, 1, 0.5f);

                bg.SetAsFirstSibling();
            }
        }

        protected virtual void OnValidate()
        {
            if (!m_foreground)
            {
                m_foreground = transform.Find("Text")?.GetComponent<Text>() ?? GetComponentInChildren<Text>();
            }
            if(m_background && m_foreground && m_background.transform.IsChildOf(transform) && m_foreground.transform.IsChildOf(transform))
            {
                m_background.transform.SetAsFirstSibling();
            }
        }

        protected virtual void Start()
        {
            if (m_background)
            {
                m_background.color = m_bgUnselected;
            }
            m_foreground.color = m_fgUnselected;
        }

        private bool m_isSelected = false;
        public bool IsSelected
        {
            get => m_isSelected;
            set
            {
                if(m_isSelected != value)
                {
                    m_isSelected = value;
                    if (m_isSelected)
                    {
                        if (m_background)
                        {
                            m_background.color = m_bgSelected;
                        }
                        m_foreground.color = m_fgSelected;
                    }
                    else
                    {
                        if (m_background)
                        {
                            m_background.color = m_bgUnselected;
                        }
                        m_foreground.color = m_fgUnselected;
                    }
                }
            }
        }

        public void SetAndStretchToParentSize(RectTransform m_rect, RectTransform m_parent)
        {
            m_rect.transform.SetParent(m_parent, false);
            m_rect.anchoredPosition = Vector2.zero;
            m_rect.anchorMin = new Vector2(0, 0);
            m_rect.anchorMax = new Vector2(1, 1);
            m_rect.pivot = new Vector2(0.5f, 0.5f);
            m_rect.sizeDelta = m_parent.rect.size;
        }
    }
}
