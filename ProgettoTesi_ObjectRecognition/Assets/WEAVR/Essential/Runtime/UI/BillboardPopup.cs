using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("")]
    [Obsolete("Use new Billboard instead")]
    public class BillboardPopup : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        protected Text m_textComponent;
        [SerializeField]
        [Draggable]
        protected Popup3D m_popup;

        protected bool m_isVisible;
        public bool IsVisible {
            get {
                return m_popup.IsVisible;
            }
            set {
                m_popup.IsVisible = value;
            }
        }

        public string Text {
            get {
                return m_textComponent.text;
            }
            set {
                m_textComponent.text = value;
            }
        }

        private void OnValidate() {
            if(m_textComponent == null) {
                m_textComponent = GetComponentInChildren<Text>(true);
            }
            if(m_popup == null) {
                m_popup = GetComponentInChildren<Popup3D>(true);
            }
        }

        public void Show(GameObject go, string text) {
            m_textComponent.text = text;
            m_popup.Show(go.transform);
        }

        public void Hide() {
            m_popup.Hide();
        }

        // Use this for initialization
        void Start() {
            if (m_textComponent == null) {
                m_textComponent = GetComponentInChildren<Text>(true);
            }
            if (m_popup == null) {
                m_popup = GetComponentInChildren<Popup3D>(true);
            }
        }
    }
}
