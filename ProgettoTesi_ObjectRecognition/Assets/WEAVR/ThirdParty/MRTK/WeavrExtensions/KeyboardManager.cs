#if  WEAVR_EXTENSIONS_MRTK

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.InteractionUI
{
    public class KeyboardManager : MonoBehaviour
    {

#region Static Part
        public static KeyboardManager _instance = null;

        public static KeyboardManager Instance
        {
            get
            {
                if (_instance == null)
                {

                    _instance = FindObjectOfType<KeyboardManager>();
                    if (_instance == null)
                    {
                        Debug.Log("Creation of KeyboardManager singleton object");

                        // If no object is active, then create a new one
                        GameObject go = new GameObject("KeyboardManager");
                        _instance = go.AddComponent<KeyboardManager>();
                    }

                }

                return _instance;
            }
        }
#endregion

        private TouchScreenKeyboard m_keyboard;
        private InputField InputField;
        public static string KeyboardText = "";

        public void OpenSystemKeyboard(InputField field)
        {
            Debug.Log("Open Keyboard");
            InputField = field;
            m_keyboard = null;
            m_keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false);
            m_keyboard.text = InputField.text;
        }

        private void Update()
        {
            if(m_keyboard != null)
            {
                KeyboardText = m_keyboard.text;
                InputField.text = m_keyboard.text;
                if(TouchScreenKeyboard.visible)
                {
                    InputField.text = KeyboardText;
                }
                else
                {
                    m_keyboard = null;
                }
            }
        }
    }
}
#endif
