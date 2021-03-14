#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using HoloToolkit.UI.Keyboard;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.InteractionUI
{
    [RequireComponent(typeof(InputField))]
    public class OpenKeyboard : MonoBehaviour, IPointerClickHandler

    {
        public InputField InputField;
        
        void Awake()
        {
            if (InputField == null)
            {
                InputField = GetComponent<InputField>();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Keyboard.Instance.OnClosedEvent.RemoveAllListeners();
            Keyboard.Instance.OnTextSubmittedEvent.RemoveAllListeners();
            Keyboard.Instance.OnTextSubmittedEvent.AddListener(OnTextSubmitted);
            Keyboard.Instance.OnClosedEvent.AddListener(OnKeyboardClosed);
            Keyboard.Instance.PresentKeyboard(InputField.text);
        }

        private void OnTextSubmitted()
        {
            InputField.text = Keyboard.Instance.InputField.text;
        }

        private void OnKeyboardClosed()
        {
            Keyboard.Instance.OnClosedEvent.RemoveAllListeners();
            Keyboard.Instance.OnTextSubmittedEvent.RemoveAllListeners();
        }
    }
}
#endif
