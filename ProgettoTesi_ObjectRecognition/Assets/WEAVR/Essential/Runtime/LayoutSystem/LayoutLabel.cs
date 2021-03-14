using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.LayoutSystem
{
    [Serializable]
    public class UnityEventString : UnityEvent<string> { }

    [RequireComponent(typeof(Text))]
    [AddComponentMenu("WEAVR/Layout System/Layout Label")]
    public class LayoutLabel : BaseLayoutItem
    {
        [SerializeField]
        [Draggable]
        protected Text m_text;

        [Space]
        [SerializeField]
        protected UnityEventString m_onTextChanged;

        public string Text
        {
            get { return m_text?.text; }
            set
            {
                if(m_text != null && m_text.text != value)
                {
                    m_text.text = value;
                    m_onTextChanged.Invoke(value);
                }
            }
        }

        public override void Clear()
        {
            Text = string.Empty;
        }

        public override void ResetToDefaults()
        {
            Clear();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if(m_text == null)
            {
                m_text = GetComponent<Text>();
            }
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            if (m_text == null)
            {
                m_text = GetComponent<Text>();
            }
        }
    }
}
