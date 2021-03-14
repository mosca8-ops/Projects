using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.UI
{
    //[RequireComponent(typeof(TMP_InputField))]
    public class LabeledInputField : LabeledUIBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private TMP_InputField m_inputField;

        public TMP_InputField InputFiled => m_inputField;

        public UnityEventString onValueChanged;

        public UnityEventString onEndEdited;

        public string Value
        {
            get => m_inputField ? m_inputField.text : null;
            set
            {
                if (m_inputField)
                {
                    m_inputField.text = value;
                }
            }
        }

        protected override void Reset()
        {
            base.Reset();
            m_inputField = GetComponentInChildren<TMP_InputField>();
        }

        // Start is called before the first frame update
        void Awake()
        {
            if (!m_inputField)
            {
                m_inputField = GetComponent<TMP_InputField>();
            }
            m_inputField.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(string value)
        {
            onValueChanged?.Invoke(value);
        }

        private void EndEdit(string value)
        {
            onEndEdited?.Invoke(value);
        } 
    }
}
