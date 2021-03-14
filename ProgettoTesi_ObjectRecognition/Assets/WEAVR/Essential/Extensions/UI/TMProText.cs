using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WEAVR_TMPRO
using TextComponent = TMPro.TextMeshProUGUI;
#else
using TextComponent = UnityEngine.Component;
#endif

namespace TXT.WEAVR.UI
{

    public class TMProText : MonoBehaviour
#if WEAVR_TMPRO
        , ITextComponent
#endif
    {
        [SerializeField]
        private TextComponent m_textComponent;
        [SerializeField]
        private Material m_overlayMaterial;
        private Material m_normalMaterial;

#if WEAVR_TMPRO
        public string Text { 
            get => m_textComponent ? m_textComponent.text : null;
            set { if (m_textComponent) m_textComponent.text = value; }
        }

        public bool IsOverlay
        {
            get => m_textComponent && m_textComponent.material == m_normalMaterial;
            set
            {
                if (m_textComponent && m_overlayMaterial)
                {
                    m_textComponent.fontMaterial = value ? m_overlayMaterial : m_normalMaterial;
                }
            }
        }

        public Color Color { 
            get => m_textComponent.color;
            set => m_textComponent.color = value;
        }

        private void Reset()
        {
            m_textComponent = GetComponentInChildren<TextComponent>();
        }

        private void Awake()
        {
            if (!m_textComponent)
            {
                m_textComponent = GetComponent<TextComponent>();
            }
            if (m_textComponent)
            {
                m_normalMaterial = m_textComponent.fontMaterial;
            }
        }
#endif
    }
}
