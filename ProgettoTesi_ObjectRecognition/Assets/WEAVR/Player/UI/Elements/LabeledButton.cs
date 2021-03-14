using System.Collections;
using System.Collections.Generic;
using TMPro;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    public class LabeledButton : LabeledUIBehaviour
    {
        [SerializeField]
        private string m_subLabel;
        [SerializeField]
        [HiddenBy(nameof(m_tmpSubLabel), hiddenWhenTrue: true)]
        private Text m_textSubLabel;
        [SerializeField]
        [HiddenBy(nameof(m_textSubLabel), hiddenWhenTrue: true)]
        private TextMeshProUGUI m_tmpSubLabel;
        [SerializeField]
        [HideInInspector]
        private Button m_button;

        public string SubLabel
        {
            get => m_textSubLabel ? m_textSubLabel.text : m_tmpSubLabel ? m_tmpSubLabel.text : string.Empty;
            set
            {
                if (m_textSubLabel && m_textSubLabel.text != value)
                {
                    m_textSubLabel.text = value;
                }
                else if (m_tmpSubLabel && m_tmpSubLabel.text != value)
                {
                    m_tmpSubLabel.text = value;
                }
            }
        }

        public Button Button => m_button;

        public UnityEvent onClick => m_button ? m_button.onClick : null;

        protected override void Reset()
        {
            base.Reset();
            m_button = GetComponent<Button>();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SubLabel = m_subLabel;
        }

        protected override void Start()
        {
            base.Start();
            SubLabel = m_subLabel;
            if (!m_button)
            {
                m_button = GetComponent<Button>();
            }
        }
    }
}