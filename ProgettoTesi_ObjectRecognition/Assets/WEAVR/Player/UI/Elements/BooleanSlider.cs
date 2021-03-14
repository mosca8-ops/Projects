using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [RequireComponent(typeof(Slider))]
    public class BooleanSlider : LabeledUIBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler
    {
        [SerializeField]
        [HideInInspector]
        private Slider m_slider;
        [SerializeField]
        private bool m_clickChangesStatus = true;

        [NonSerialized]
        private bool m_isDragging;
        [NonSerialized]
        private bool m_sliderChanged;
        [NonSerialized]
        private bool m_value;

        public Slider Slider => m_slider;

        public UnityEventBoolean onValueChanged;

        public bool Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_sliderChanged = true;
                    m_value = value;
                    if (m_slider)
                    {
                        m_slider.value = m_value ? 1 : 0;
                    }
                    onValueChanged?.Invoke(m_value);
                }
            }
        }

        protected override void Reset()
        {
            base.Reset();
            m_slider = GetComponent<Slider>();
            m_slider.wholeNumbers = true;
            m_slider.maxValue = 1;
            m_slider.minValue = 0;
        }

        // Start is called before the first frame update
        void Awake()
        {
            if (!m_slider)
            {
                m_slider = GetComponent<Slider>();
            }
            if (m_slider)
            {
                m_value = m_slider && m_slider.value != 0;
                m_slider.onValueChanged.AddListener(OnValueChanged);
            }
        }

        private void OnValueChanged(float value)
        {
            Value = value != 0;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!eventData.used 
                && !m_isDragging 
                && m_clickChangesStatus 
                && !m_sliderChanged)
            {
                Value = !Value;
                eventData.Use();
            }
            m_sliderChanged = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            m_isDragging = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_isDragging = true;
        }
    }
}
