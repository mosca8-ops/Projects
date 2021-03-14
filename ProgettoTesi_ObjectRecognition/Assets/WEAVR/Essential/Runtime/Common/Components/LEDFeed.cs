using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [SelectionBase]
    [AddComponentMenu("WEAVR/Components/LED Feed")]
    public class LEDFeed : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        private Graphic m_graphic;
        [SerializeField]
        [HiddenBy(nameof(m_graphic), hiddenWhenTrue: true)]
        [Draggable]
        private Renderer m_renderer;
        [SerializeField]
        [HiddenBy(nameof(m_renderer))]
        private bool m_useEmissionColor = true;
        [SerializeField]
        [ColorUsage(true, true)]
        private Color[] m_colors;

        [RangeFrom(-1, nameof(m_colors), MaxOffset = -1)]
        [SerializeField]
        private int m_initialColor = -1;

        [Space]
        public UnityEventInt onLEDColorIndexChanged;
        public UnityEventColor onLEDColorChanged;

        private int m_currentLEDIndex;
        private Color? m_currentColor;
        private Color m_originalColor;
        private int m_lastColorIndex;

        private const string kEmissionColorKeyword = "_EMISSION";
        private static readonly int kEmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool IsOn
        {
            get { return m_currentColor.HasValue; }
            set
            {
                if (value)
                {
                    SwitchOn();
                }
                else
                {
                    SwitchOff();
                }
            }
        }

        public int CurrentLEDColorIndex
        {
            get { return m_currentLEDIndex; }
            set
            {
                if (m_currentLEDIndex != value && value < m_colors.Length)
                {
                    if (value < 0)
                    {
                        m_currentLEDIndex = -1;
                        SetColorInternal(null);
                    }
                    else
                    {
                        m_currentLEDIndex = value;
                        m_lastColorIndex = m_currentLEDIndex;
                        SetColorInternal(m_colors[value]);
                    }
                    onLEDColorIndexChanged.Invoke(m_currentLEDIndex);
                }
            }
        }

        public Color CurrentLEDColor => m_currentColor ?? Color.clear;

        private void OnValidate()
        {
            if (m_renderer == null)
            {
                m_renderer = GetComponentInChildren<Renderer>();
            }
        }

        public void SetColor(int index)
        {
            CurrentLEDColorIndex = index;
        }

        public void SetColor(Color color)
        {
            if (color == Color.clear)
            {
                CurrentLEDColorIndex = -1;
                return;
            }
            for (int i = 0; i < m_colors.Length; i++)
            {
                if (color == m_colors[i])
                {
                    CurrentLEDColorIndex = i;
                    return;
                }
            }
            m_currentLEDIndex = -1;
            SetColorInternal(color);
        }

        protected void SetColorInternal(Color? color)
        {
            m_currentColor = color;
            if (m_graphic != null)
            {
                m_graphic.color = color ?? Color.clear;
                onLEDColorChanged.Invoke(m_graphic.color);
            }
            else if (m_renderer != null)
            {
                if (m_renderer.material.HasProperty(kEmissionColorId))
                {
                    if (color.HasValue)
                    {
                        m_renderer.material.EnableKeyword(kEmissionColorKeyword);
                        m_renderer.material.SetColor(kEmissionColorId, color.Value);
                        onLEDColorChanged.Invoke(color.Value);
                    }
                    else
                    {
                        m_renderer.material.DisableKeyword(kEmissionColorKeyword);
                        onLEDColorChanged.Invoke(Color.clear);
                    }
                }
                else
                {
                    m_renderer.material.color = color ?? m_originalColor;
                    onLEDColorChanged.Invoke(m_renderer.material.color);
                }
            }
        }

        public void SwitchOff()
        {
            CurrentLEDColorIndex = -1;
        }

        public void SwitchOn()
        {
            CurrentLEDColorIndex = m_lastColorIndex;
        }

        // Use this for initialization
        void Start()
        {
            if (m_graphic == null && m_renderer == null)
            {
                m_renderer = GetComponentInChildren<Renderer>();
            }
            if (m_graphic == null && m_renderer != null)
            {
                m_originalColor = m_renderer.material.color;
                m_useEmissionColor &= m_renderer.material.HasProperty(kEmissionColorId);
            }
            CurrentLEDColorIndex = m_initialColor;
        }
    }
}
