using TMPro;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    public class LabeledUIBehaviour : MonoBehaviour, ILabeledElement
    {
        [SerializeField]
        private string m_label;
        [SerializeField]
        [HiddenBy(nameof(m_tmpLabel), hiddenWhenTrue: true)]
        private Text m_textLabel;
        [SerializeField]
        [HiddenBy(nameof(m_textLabel), hiddenWhenTrue: true)]
        private TextMeshProUGUI m_tmpLabel;

        public string Label
        {
            get => m_textLabel ? m_textLabel.text : m_tmpLabel ? m_tmpLabel.text : string.Empty;
            set
            {
                m_label = value;
                if(m_textLabel && m_textLabel.text != value)
                {
                    m_textLabel.text = value;
                }
                else if(m_tmpLabel && m_tmpLabel.text != value)
                {
                    m_tmpLabel.text = value;
                }
            }
        }

        public TextMeshProUGUI TextMeshProComponent => m_tmpLabel;
        public Text UnityTextComponent => m_textLabel;

        protected virtual void Reset()
        {
            m_tmpLabel = null;
            m_textLabel = null;
            m_tmpLabel = GetComponentInChildren<TextMeshProUGUI>(true);
            if (!m_tmpLabel)
            {
                m_textLabel = GetComponentInChildren<Text>(true);
                if (m_textLabel)
                {
                    m_label = m_textLabel.text;
                }
            }
            else
            {
                m_label = m_tmpLabel.text;
            }
        }

        protected virtual void OnValidate()
        {
            Label = m_label;
        }

        protected virtual void Start()
        {
            Label = m_label;
        }
    }
}