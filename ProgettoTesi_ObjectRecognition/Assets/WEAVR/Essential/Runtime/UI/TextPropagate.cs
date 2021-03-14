using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/UI/Propagate Texts")]
    public class TextPropagate : MonoBehaviour
    {
        [SerializeField]
        [TextArea(4, 8)]
        private string m_text;
        [Space]
        [SerializeField]
        private Text[] m_textComponents;
        [Space]
        [SerializeField]
        private UnityEventString m_onTextChanged;

        public string Text
        {
            get => m_text;
            set
            {
                if (m_text != value)
                {
                    ApplyText(value);
                }
            }
        }

        private void ApplyText(string value)
        {
            m_text = value;
            for (int i = 0; i < m_textComponents.Length; i++)
            {
                if (m_textComponents[i])
                {
                    m_textComponents[i].text = value;
                }
            }
            m_onTextChanged?.Invoke(value);
        }

        private void OnValidate()
        {
            ApplyText(m_text);
        }

        // Start is called before the first frame update
        void Start()
        {
            ApplyText(m_text);
        }
    }
}