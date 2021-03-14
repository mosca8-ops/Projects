
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using TXT.WEAVR.UI;

namespace TXT.WEAVR.Player.Views
{
    public class ButtonItem : MonoBehaviour, IClickItem
    {
        [Header("Components")]
        [Draggable]
        public LabeledButton button;
        [Draggable]
        public Image background;
        [Draggable]
        public Image icon;

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

        private Action m_onClick;
        public event Action OnClick
        {
            add => m_onClick += value;
            remove => m_onClick -= value;
        }

        private void Awake()
        {
            if (icon)
            {
                m_defaultSprite = icon.sprite;
            }
            button.onClick.AddListener(Clicked);
        }

        private void Clicked() => m_onClick?.Invoke();

        public void Clear()
        {
            
        }
    }
}

