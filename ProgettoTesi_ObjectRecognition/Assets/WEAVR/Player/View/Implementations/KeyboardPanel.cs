
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TXT.WEAVR.Player.Views
{
    public class KeyboardPanel : MonoBehaviour
    {
        [Tooltip("Keyboard canvas")]
        [Draggable]
        public Canvas KeyboardCanvas;
        [Tooltip("Keyboard number panel")]
        [Draggable]
        public GameObject KeyNumberPanel;
        [Tooltip("Keyboard middle lowcase panel")]
        [Draggable]
        public GameObject KeyLowcasePanel;
        [Tooltip("Keyboard uppercase panel")]
        [Draggable]
        public GameObject KeyUppercasePanel;
        [Tooltip("Keyboard symbol panel")]
        [Draggable]
        public GameObject KeySymbolPanel;
        [Tooltip("Keyboard special buttons panel")]
        [Draggable]
        public GameObject KeySpecialButtonsPanel;
        [Header("Special Buttons")]
        [Tooltip("Button Backspace")]
        [Draggable]
        public Button KeyBackspaceButton;
        [Tooltip("Button Enter")]
        [Draggable]
        public Button KeyEnterButton;
        [Tooltip("Button Hide Keyboard")]
        [Draggable]
        public Button KeyHideButton;
    }
}
