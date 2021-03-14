
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using TXT.WEAVR.UI;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Player.Views
{
    public class ToggleItem : MonoBehaviour, IClickItem, ISwitchItem
    {
        [Header("Components")]
        [Draggable]
        public LabeledButton button;
        [Draggable]
        public Image background;
        [Draggable]
        public Image icon;
        [SerializeField]
        private bool m_isOn;

        [Header("State ON")]
        [Draggable]
        public Sprite iconWhenOn;
        public OptionalString m_labelWhenOn;

        [Header("State OFF")]
        [Draggable]
        public Sprite iconWhenOff;
        public OptionalString m_labelWhenOff;

        private Sprite m_defaultSprite;

        public Guid Id { get; set; }
        public string Label { get => button.Label; set => button.Label = value; }
        public Color Color 
        { 
            get => background ? background.color : Color.clear;
            set
            {
                if (background) { background.color = value; }
            }
        }

        public Texture2D Image 
        { 
            get => icon && icon.sprite ? icon.sprite.texture : null; 
            set
            {
                if (icon)
                {
                    icon.sprite = value ? value.CreateSprite() : m_defaultSprite;
                }
            }
        }
        public bool Enabled { get => button.Button.interactable; set => button.Button.interactable = value; }
        public bool IsVisible { get => button.gameObject.activeInHierarchy; set => button.gameObject.SetActive(value); }
        public bool IsOn {
            get => m_isOn;
            set
            {
                if(m_isOn != value)
                {
                    SetStateSilently(value);
                    m_onStateChanged?.Invoke(m_isOn);
                }
            }
        }

        private Action m_onClick;
        public event Action OnClick
        {
            add => m_onClick += value;
            remove => m_onClick -= value;
        }

        private OnValueChanged<bool> m_onStateChanged;
        public event OnValueChanged<bool> StateChanged
        {
            add => m_onStateChanged += value;
            remove => m_onStateChanged -= value;
        }

        private void Awake()
        {
            if (icon)
            {
                m_defaultSprite = icon.sprite;
            }
            button.onClick.AddListener(Clicked);
        }

        private void Start()
        {
            SetStateSilently(m_isOn);
        }

        private void Clicked()
        {
            IsOn = !IsOn;
            m_onClick?.Invoke();
        }

        public void Clear()
        {
            m_onClick = null;
            m_onStateChanged = null;
        }

        public void SetStateSilently(bool isOn)
        {
            m_isOn = isOn;
            if (icon)
            {
                icon.sprite = m_isOn && iconWhenOn ? iconWhenOn : !m_isOn && iconWhenOff ? iconWhenOff : icon.sprite;
            }
            if (m_isOn && m_labelWhenOn.enabled)
            {
                button.Label = m_labelWhenOn;
            }
            else if (!m_isOn && m_labelWhenOff.enabled)
            {
                button.Label = m_labelWhenOff;
            }
        }
    }
}

