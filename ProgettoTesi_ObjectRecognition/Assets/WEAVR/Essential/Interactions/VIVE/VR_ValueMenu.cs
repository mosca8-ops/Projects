using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("WEAVR/VR/Advanced/Value Menu")]
    public class VR_ValueMenu : VR_AbstractMenu
    {

        [Header("Menu")]
        [SerializeField]
        private bool m_disablePointer = true;
        [SerializeField]
        private Text m_fieldText;
        [SerializeField]
        private Text m_valueText;
        //[SerializeField]
        //[Tooltip("How fast to allow mAction change when scrolling")]
        //private float m_scrollUpdateRate = 0.1f;

        [SerializeField]
        [HideInInspector]
        private Color m_defaultValueColor;
#if WEAVR_VR

        private Func<string> m_valueGetter;
        private Func<Color> m_colorGetter;


        protected override void OnValidate()
        {
            base.OnValidate();

            FindTexts();
        }

        private void FindTexts()
        {
            if (m_fieldText == null)
            {
                m_fieldText = GetComponentInChildren<Text>();
            }
            if (m_valueText == null)
            {
                foreach (var text in GetComponentsInChildren<Text>())
                {
                    if (text != m_fieldText)
                    {
                        m_valueText = text;
                        break;
                    }
                }
            }

            m_defaultValueColor = m_valueText != null ? m_valueText.color : Color.clear;
        }

        protected override void Start()
        {
            base.Start();
            FindTexts();
        }

        public void Show(string valueName, Func<object> getValueCallback, Func<Color> getColorCallback = null)
        {
            Show(null, valueName, getValueCallback, getColorCallback);
        }

        public void Show(string valueName, Func<string> getValueCallback, Func<Color> getColorCallback = null)
        {
            Show(null, valueName, getValueCallback, getColorCallback);
        }

        public void Show(Transform point, string valueName, Func<object> getValueCallback, Func<Color> getColorCallback = null)
        {
            Show(null, valueName, () => getValueCallback()?.ToString(), getColorCallback);
        }

        public void Show(Transform point, string valueName, Func<string> getValueCallback, Func<Color> getColorCallback = null)
        {
            m_fieldText.text = valueName;
            m_valueGetter = getValueCallback;
            m_colorGetter = getColorCallback;

            m_valueText.text = m_valueGetter()?.ToString();
            m_valueText.color = m_colorGetter != null ? m_colorGetter() : m_defaultValueColor;

            m_canvas.gameObject.SetActive(true);
            base.Show(point);

            if (m_disablePointer)
            {
                var pointer = GetComponent<WorldPointer>();
                if (pointer != null)
                {
                    pointer.enabled = false;
                }
            }
        }

        private void Update()
        {
            if(IsVisible && m_valueGetter != null)
            {
                m_valueText.text = m_valueGetter()?.ToString();
                if(m_colorGetter != null)
                {
                    m_valueText.color = m_colorGetter();
                }
            }
        }

        public override void Hide()
        {
            base.Hide();
            m_valueGetter = null;
            m_colorGetter = null;
            m_canvas.gameObject.SetActive(false);

            if (m_disablePointer)
            {
                var pointer = GetComponent<WorldPointer>();
                if (pointer != null)
                {
                    pointer.enabled = true;
                }
            }
        }

#endif
    }
}