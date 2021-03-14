
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class MenuButtons : MonoBehaviour
    {
        [Tooltip("Menu Buttons panel")]
        [Draggable]
        public GameObject MenuButtonsPanel;
        [Tooltip("Back Button panel")]
        [Draggable]
        public GameObject BackPanel;
        [Header("Menu Toggles")]
        [Draggable]
        public Toggle HomeToggle;
        [Draggable]
        public Toggle LibraryToggle;
        [Draggable]
        public Toggle MultiplayerToggle;
        [Draggable]
        public Toggle NotificationsToggle;
        [Draggable]
        public Toggle SettingsToggle;
        [Header("Back Button")]
        [Draggable]
        public Button BackButton;

    }
}