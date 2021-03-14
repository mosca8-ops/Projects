namespace TXT.WEAVR.UI
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("WEAVR/UI/Text Format Filter")]
    public class TextFormatFilter : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        private Text m_target;
        [SerializeField]
        private string m_format = "{0}";
        private string m_value;

        public string Text {
            get { return m_target.text; }
            set {
                m_value = value;
                m_target.text = string.Format(m_format, value);
            }
        }

        private void OnValidate()
        {
            if (m_target == null)
            {
                m_target = GetComponent<Text>();
            }
        }

        // Use this for initialization
        void Start()
        {
            OnValidate();
            if (!m_format.Contains("{0") && !m_format.Contains("}"))
            {
                m_format = "{0}"; // Fallback format
            }
        }

        public void AppendFormatted(string value)
        {
            m_value += value;
            Text = m_value;
        }

        public void Backspace()
        {
            if (m_value.Length > 0)
            {
                m_value = m_value.Substring(0, m_value.Length - 1);
                Text = m_value;
            }
        }

        public void ClearFormatted()
        {
            Text = string.Empty;
        }

        public void ClearFull()
        {
            m_target.text = m_value = string.Empty;
        }
    }
}