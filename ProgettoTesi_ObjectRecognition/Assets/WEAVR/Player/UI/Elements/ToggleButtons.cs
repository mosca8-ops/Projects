using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Player.Views;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TXT.WEAVR.UI
{
    public class ToggleButtons : MonoBehaviour, IResetState, IClickItem, ISwitchItem
    {
        [SerializeField]
        private bool m_interactable = true;
        [SerializeField]
        private Button m_buttonOn;
        [SerializeField]
        private Button m_buttonOff;
        [SerializeField]
        private bool m_startValue;

        [SerializeField]
        private UnityEventBoolean m_onValueChanged;

        private OnValueChanged<bool> m_valueChanged;
        public event OnValueChanged<bool> ValueChanged
        {
            add => m_valueChanged += value;
            remove => m_valueChanged -= value;
        }
        public event OnValueChanged<bool> StateChanged
        {
            add => m_valueChanged += value;
            remove => m_valueChanged -= value;
        }

        private Action m_onClick;
        public event Action OnClick
        {
            add => m_onClick += value;
            remove => m_onClick -= value;
        }

        private bool m_active;

        private bool m_value;
        public bool Value
        {
            get => m_value;
            set
            {
                if (m_value != value)
                {
                    m_value = value;
                    m_buttonOff.gameObject.SetActive(value);
                    m_buttonOn.gameObject.SetActive(!value);
                    m_buttonOff.interactable = Interactable;
                    m_buttonOn.interactable = Interactable;

                    m_onValueChanged?.Invoke(value);
                    m_valueChanged?.Invoke(value);
                    m_onClick?.Invoke();
                }
            }
        }

        public bool Interactable
        {
            get => m_active;
            set
            {
                if(m_active != value)
                {
                    m_active = value;
                    m_buttonOff.interactable = value;
                    m_buttonOn.interactable = value;
                }
            }
        }

        public bool IsOn { get => Value; set => Value = value; }
        public Guid Id { get; set; }
        public string Label { get => TryGetActiveComponent(out Text text) ? text.text : null; set { } }
        public Color Color { get; set; }
        public Texture2D Image { get => TryGetActiveComponent(out Image image) && image.sprite ? image.sprite.texture : null; set { } }
        bool IViewItem.Enabled { get => Interactable; set => Interactable = value; }
        public bool IsVisible { get => gameObject.activeInHierarchy; set => gameObject.SetActive(value); }

        private void Reset()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            m_buttonOn = buttons.Length > 0 ? buttons[0] : null;
            m_buttonOff = buttons.Length > 1 ? buttons[1] : null;
        }

        private void Start()
        {
            if (!m_buttonOn || !m_buttonOff)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                m_buttonOn = !m_buttonOn && buttons.Length > 0 ? buttons[0] : m_buttonOn;
                m_buttonOff = !m_buttonOff && buttons.Length > 1 ? buttons[1] : m_buttonOff;
            }

            m_active = !m_interactable;
            Interactable = m_interactable;

            m_value = !m_startValue;
            Value = m_startValue;
            m_buttonOn.onClick.AddListener(On_Clicked);
            m_buttonOff.onClick.AddListener(Off_Clicked);
        }

        private bool TryGetActiveComponent<T>(out T text)
        {
            text = default;
            if (m_value && m_buttonOff)
            {
                text = m_buttonOff.GetComponentInChildren<T>();
            }
            else if (!m_value && m_buttonOn)
            {
                text = m_buttonOn.GetComponentInChildren<T>();
            }

            return !Equals(text, default);
        }


        private void Off_Clicked()
        {
            Value = false;
        }

        private void On_Clicked()
        {
            Value = true;
        }

        public void ResetState()
        {
            m_active = !m_interactable;
            Interactable = m_interactable;
            m_value = !m_startValue;
            Value = m_startValue;
        }

        public void SetStateSilently(bool isOn)
        {
            m_value = isOn;
            m_buttonOff.gameObject.SetActive(isOn);
            m_buttonOn.gameObject.SetActive(!isOn);
            m_buttonOff.interactable = Interactable;
            m_buttonOn.interactable = Interactable;
        }

        public void Clear()
        {
            m_valueChanged = null;
            m_onClick = null;
        }
    }
}