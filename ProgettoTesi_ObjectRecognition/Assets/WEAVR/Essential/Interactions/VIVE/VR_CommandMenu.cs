using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    [AddComponentMenu("WEAVR/VR/Advanced/Command Menu")]
    public class VR_CommandMenu : VR_GenericMenu
    {
        [Header("Components")]
        [SerializeField]
        private Transform m_buttonsPanel;
        [SerializeField]
        private Button m_buttonSample;
        
#if WEAVR_VR

        private List<GameObject> m_buttons;

        protected override void Reset()
        {
            base.Reset();
            m_isActive = false;
        }

        protected override void Clear()
        {
            base.Clear();
            if(m_buttons == null) { return; }
            foreach (var button in m_buttons)
            {
                Destroy(button);
            }
            m_buttons.Clear();
        }

        public VR_CommandMenu BeginMenu()
        {
            Clear();
            return this;
        }

        public VR_CommandMenu AddMenuItem(string name, UnityAction onClickCallback)
        {
            var newButton = Instantiate(m_buttonSample);
            newButton.onClick.AddListener(onClickCallback);
            newButton.onClick.AddListener(Hide);
            var textComponent = newButton.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = name;
            }
            newButton.transform.SetParent(m_buttonsPanel, false);
            newButton.gameObject.SetActive(true);
            m_buttons.Add(newButton.gameObject);
            return this;
        }

        // Use this for initialization
        protected override void Start()
        {
            base.Start();
            m_buttons = new List<GameObject>();
            IsActive = false;
        }

        public override void Show(Transform point = null)
        {
            m_canvas.gameObject.SetActive(true);
            base.Show(point);
            if(m_buttons.Count > 0)
            {
                m_buttons[0].GetComponent<Button>().Select();
            }
        }

#endif
    }
}
