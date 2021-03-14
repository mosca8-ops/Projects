using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("WEAVR/UI/Value Indicator Panel")]
    public class TextPanelValueIndicator : ValueIndicator
    {
        [SerializeField]
        private string m_format = "0.00";
        [SerializeField]
        [Draggable]
        private Text m_valueComponent;
        [SerializeField]
        [Draggable]
        private Text m_measureComponent;
        [SerializeField]
        [Draggable]
        private Text m_measureUnitComponent;
        [SerializeField]
        private OptionalFloat m_visibilityTimeout;

        [Header("Colors")]
        [SerializeField]
        private OptionalColor m_normal;
        [SerializeField]
        private OptionalColor m_valid;
        [SerializeField]
        private OptionalColor m_critical;

        [Space]
        [SerializeField]
        [Draggable]
        private AudioSource m_audioOnChange;

        [Space]
        [SerializeField]
        [Draggable]
        private CanvasGroup m_canvasGroup;

        private Canvas m_canvas;

        private float m_nextHideTime;

        public Color? NormalColor { get => m_normal; set => m_normal = value; }
        public Color? ValidColor { get => m_valid; set => m_valid = value; }
        public Color? CriticalColor { get => m_critical; set => m_critical = value; }

        public override string Measure {
            get => m_measureComponent ? m_measureComponent.text : string.Empty;
            set { if (m_measureComponent) m_measureComponent.text = value; }
        }

        public override string MeasureUnit {
            get => m_measureUnitComponent ? m_measureUnitComponent.text : string.Empty;
            set { if (m_measureUnitComponent) m_measureUnitComponent.text = value; }
        }

        private Color m_fallbackColor;

        private void Reset()
        {
            
        }

        private void OnValidate()
        {
            if (!m_valueComponent)
            {
                m_valueComponent = GetComponent<Text>();
            }
            if (m_normal != null && m_normal.enabled)
            {
                m_valueComponent.color = m_normal.value;
            }
            if (!m_canvasGroup)
            {
                m_canvasGroup = GetComponentInParent<CanvasGroup>();
                if(!m_canvasGroup)
                {
                    m_canvasGroup = GetComponentInChildren<CanvasGroup>();
                }
            }
        }

        void Awake()
        {
            m_canvas = GetComponentInChildren<Canvas>();
            if (!m_valueComponent)
            {
                m_valueComponent = GetComponent<Text>();
            }
            if (m_normal.enabled)
            {
                m_fallbackColor = m_valueComponent.color = m_normal.value;
            }
            else
            {
                m_fallbackColor = m_valueComponent.color;
            }
            if (!m_canvasGroup)
            {
                m_canvasGroup = GetComponentInParent<CanvasGroup>();
                if (!m_canvasGroup)
                {
                    m_canvasGroup = GetComponentInChildren<CanvasGroup>();
                }
            }
        }

        public override void SetValue(float value)
        {
            if (m_visibilityTimeout.enabled) 
            {
                if (!m_canvas.gameObject.activeSelf)
                {
                    StartCoroutine(Reveal(0.5f));
                }
                m_nextHideTime = Time.time + m_visibilityTimeout.value;
                StartCoroutine(Hide(0.5f));
            }
            m_valueComponent.text = !string.IsNullOrEmpty(m_format) ? value.ToString(m_format) : value.ToString();
            if (m_audioOnChange && m_audioOnChange.clip) { m_audioOnChange.Play(); }
        }

        public override void SetValue(float value, ValueImportance valueImportance)
        {
            base.SetValue(value, valueImportance);
            switch (valueImportance)
            {
                case ValueImportance.Normal:
                    if (m_normal.enabled) { m_valueComponent.color = m_normal.value; }
                    else { m_valueComponent.color = m_fallbackColor; }
                    break;
                case ValueImportance.Valid:
                    if (m_valid.enabled) { m_valueComponent.color = m_valid.value; }
                    else { m_valueComponent.color = m_fallbackColor; }
                    break;
                case ValueImportance.Critical:
                    if (m_critical.enabled) { m_valueComponent.color = m_critical.value; }
                    else { m_valueComponent.color = m_fallbackColor; }
                    break;
            }
        }

        private IEnumerator Reveal(float time)
        {
            m_canvas.gameObject.SetActive(true);
            if (m_canvasGroup)
            {
                if (time > 0)
                {
                    float factor = 1 / time;
                    while (time > 0)
                    {
                        time -= Time.deltaTime;
                        m_canvasGroup.alpha = Mathf.Lerp(1, m_canvasGroup.alpha, time * factor);
                        yield return null;
                    }
                }
                else
                {
                    m_canvasGroup.alpha = 0;
                }
            }
        }

        private IEnumerator Hide(float time)
        {
            while (m_nextHideTime > Time.time)
            {
                yield return new WaitForSeconds(m_nextHideTime - Time.time);
            }
            if (m_canvasGroup)
            {
                if (time > 0)
                {
                    float factor = 1 / time;
                    while (time > 0)
                    {
                        time -= Time.deltaTime;
                        m_canvasGroup.alpha = Mathf.Lerp(m_canvasGroup.alpha, 0, 1 - time * factor);
                        yield return null;
                    }
                }
                else
                {
                    m_canvasGroup.alpha = 0;
                }
            }
            m_canvas.gameObject.SetActive(false);
        }
    }
}