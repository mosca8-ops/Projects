using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("WEAVR/UI/Long Press Button")]
    public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        [MeasureLabel("Seconds")]
        private float m_invokeEvery = 0.2f;
        [Space]
        [SerializeField]
        private UnityEventFloat m_onPress;
        
        private bool m_isPressed = false;
        private float m_pressTime;
        private float m_lastSampleTime;
        private Button m_button;

        public Button Button
        {
            get
            {
                if (!m_button) { m_button = GetComponent<Button>(); }
                return m_button;
            }
        }

        public UnityEventFloat OnPress => m_onPress;

        private void OnEnable()
        {
            m_isPressed = false;
        }

        private void OnDisable()
        {
            m_isPressed = false;
        }

        void Update()
        {
            if (m_isPressed && m_button.interactable && Time.unscaledTime - m_lastSampleTime >= m_invokeEvery)
            {
                m_lastSampleTime = Time.unscaledTime;
                m_onPress.Invoke(Time.unscaledTime - m_pressTime);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Button.interactable)
            {
                m_isPressed = true;
                m_pressTime = Time.unscaledTime;
                m_lastSampleTime = Time.unscaledTime;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_isPressed = false;
        }
    }
}
