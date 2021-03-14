namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    public class GenericPopupWindow : EditorWindow
    {
        private static GenericPopupWindow _lastOpened;
        private Func<bool> _drawFunc;

        //[MenuItem("Example/ShowPopup Example")]
        static void Init()
        {
            GenericPopupWindow window = ScriptableObject.CreateInstance<GenericPopupWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
            window.ShowPopup();
        }

        public static void ClosePopup()
        {
            if (_lastOpened != null)
            {
                _lastOpened.Close();
            }
        }

        public static void Show(Rect position, Func<bool> drawMethod)
        {
            if (_lastOpened != null)
            {
                _lastOpened.Close();
            }
            _lastOpened = CreateInstance<GenericPopupWindow>();
            _lastOpened._drawFunc = drawMethod;
            _lastOpened.position = position;
            _lastOpened.ShowPopup();
        }

        void OnGUI()
        {
            if (_drawFunc == null || _drawFunc())
            {
                Close();
            }
        }
    }
}