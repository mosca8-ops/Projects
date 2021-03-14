
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace TXT.WEAVR.Player.Views
{
    public class ExecutionModeElement : MonoBehaviour, IExecutionModeItem
    {
        [SerializeField]
        private TXT.WEAVR.Procedure.ExecutionMode m_mode;
        [Header("Components")]
        [Draggable]
        public Toggle toggle;
        [Draggable]
        public TextMeshProUGUI modeName;
        [Draggable]
        public TextMeshProUGUI modeDescription;
        [Draggable]
        public Image image;

        private Sprite m_defaultSprite;

        public event OnSelectedDelegate OnSelected;

        public string Label
        {
            get => modeName.text;
            set => modeName.text = value;
        }

        public Guid Id { get; set; }

        public string Description { 
            get => modeDescription ? modeDescription.text : string.Empty;
            set
            {
                if (modeDescription)
                {
                    modeDescription.text = value;
                }
            }
        }
        public bool IsSelected { get => toggle.isOn; set => toggle.isOn = value; }
        public Color Color { get; set; }
        public Texture2D Image { get => image.sprite.texture; set => image.sprite = value ? SpriteCache.Instance.Get(value) : m_defaultSprite; }
        public bool Enabled { get => toggle.interactable; set => toggle.interactable = value; }
        public bool IsVisible { get => toggle.gameObject.activeInHierarchy; set => toggle.gameObject.SetActive(value); }
        public string ModeId { get => m_mode ? m_mode.ModeName : null; }

        public void Clear()
        {
            toggle.isOn = false;
        }

        private void Start()
        {
            toggle.onValueChanged.RemoveListener(GroupSelected);
            toggle.onValueChanged.AddListener(GroupSelected);
        }

        private void OnEnable()
        {
            var group = GetComponentInParent<ToggleGroup>();
            if (group)
            {
                toggle.group = group;
            }
        }

        private void GroupSelected(bool value)
        {
            if (value)
            {
                OnSelected?.Invoke(this);
            }
        }
    }
}

