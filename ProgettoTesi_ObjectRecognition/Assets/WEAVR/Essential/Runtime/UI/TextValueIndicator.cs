using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("WEAVR/UI/Value Indicator Text")]
    public class TextValueIndicator : ValueIndicator
    {
        [SerializeField]
        [Draggable]
        private Text m_textComponent;
        [SerializeField]
        private string m_format = "0.00";
        [Header("Colors")]
        [SerializeField]
        private OptionalColor m_normal;
        [SerializeField]
        private OptionalColor m_valid;
        [SerializeField]
        private OptionalColor m_critical;

        public Color? NormalColor { get => m_normal; set => m_normal = value; }
        public Color? ValidColor { get => m_valid; set => m_valid = value; }
        public Color? CriticalColor { get => m_critical; set => m_critical = value; }

        public override string Measure { get; set; }
        public override string MeasureUnit { get; set; }

        private Color m_fallbackColor;

        private void OnValidate()
        {
            if (!m_textComponent)
            {
                m_textComponent = GetComponent<Text>();
            }
            if (m_normal.enabled)
            {
                m_textComponent.color = m_normal.value;
            }
        }

        void Start()
        {
            if (!m_textComponent)
            {
                m_textComponent = GetComponent<Text>();
            }
            if (m_normal.enabled)
            {
                m_fallbackColor = m_textComponent.color = m_normal.value;
            }
            else
            {
                m_fallbackColor = m_textComponent.color;
            }
        }

        public override void SetValue(float value)
        {
            m_textComponent.text = !string.IsNullOrEmpty(m_format) ? value.ToString(m_format) : value.ToString();
        }

        public override void SetValue(float value, ValueImportance valueImportance)
        {
            base.SetValue(value, valueImportance);
            switch (valueImportance)
            {
                case ValueImportance.Normal:
                    if (m_normal.enabled) { m_textComponent.color = m_normal.value; }
                    else { m_textComponent.color = m_fallbackColor; }
                    break;
                case ValueImportance.Valid:
                    if (m_valid.enabled) { m_textComponent.color = m_valid.value; }
                    else { m_textComponent.color = m_fallbackColor; }
                    break;
                case ValueImportance.Critical:
                    if (m_critical.enabled) { m_textComponent.color = m_critical.value; }
                    else { m_textComponent.color = m_fallbackColor; }
                    break;
            }
        }
    }
}