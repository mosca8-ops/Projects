using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/UI/Change UI Color")]
    public class ChangeUIColor : MonoBehaviour
    {
        [SerializeField]
        private Graphic m_graphic;
        [SerializeField]
        private OptionalColor m_startColor;

        public Color Color { get => m_graphic.color; set => m_graphic.color = value; }
        public float Red { get => m_graphic.color.r; set => m_graphic.color = new Color(value, m_graphic.color.g, m_graphic.color.b, m_graphic.color.a); }
        public float Green { get => m_graphic.color.g; set => m_graphic.color = new Color(m_graphic.color.r, value ,m_graphic.color.b, m_graphic.color.a); }
        public float Blue { get => m_graphic.color.b; set => m_graphic.color = new Color(m_graphic.color.r, m_graphic.color.r, value, m_graphic.color.a); }
        public float Alpha { get => m_graphic.color.a; set => m_graphic.color = new Color(m_graphic.color.r, m_graphic.color.g, m_graphic.color.b, value); }

        private void Reset()
        {
            m_graphic = GetComponentInChildren<Graphic>();
        }

        private void OnValidate()
        {
            if (!m_graphic)
            {
                m_graphic = GetComponentInChildren<Graphic>();
            }
        }
        
        void Start()
        {
            OnValidate();
            if (m_startColor.enabled)
            {
                Color = m_startColor.value;
            }
        }
    }
}
