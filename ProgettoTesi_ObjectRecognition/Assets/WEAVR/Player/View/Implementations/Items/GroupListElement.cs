
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace TXT.WEAVR.Player.Views
{
    public class GroupListElement : MonoBehaviour, IGroupItem
    {
        [Header("Components")]
        [Draggable]
        public Toggle GroupElementToggle;
        [Draggable]
        public TextMeshProUGUI GroupName;
        [Draggable]
        public Image GroupStatusNew;

        public event OnSelectedDelegate OnSelected;

        public string Label
        {
            get => GroupName.text;
            set => GroupName.text = value;
        }

        public Guid Id { get; set; }

        public bool IsNew
        {
            get => GroupStatusNew && GroupStatusNew.gameObject.activeInHierarchy;
            set
            {
                if (GroupStatusNew)
                {
                    GroupStatusNew.gameObject.SetActive(value);
                }
            }
        }

        public string Description { get; set; }
        public bool IsSelected { get => GroupElementToggle.isOn; set => GroupElementToggle.isOn = true; }
        public Color Color { get; set; }
        public Texture2D Image { get; set; }
        public bool Enabled { get => GroupElementToggle.interactable; set => GroupElementToggle.interactable = value; }
        public bool IsVisible { get => GroupElementToggle.gameObject.activeInHierarchy; set => GroupElementToggle.gameObject.SetActive(value); }

        public void Clear()
        {
            GroupElementToggle.isOn = false;
            if (GroupStatusNew)
            {
                GroupStatusNew.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            GroupElementToggle.onValueChanged.RemoveListener(GroupSelected);
            GroupElementToggle.onValueChanged.AddListener(GroupSelected);
        }

        private void OnEnable()
        {
            var group = GetComponentInParent<ToggleGroup>();
            if (group)
            {
                GroupElementToggle.group = group;
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

