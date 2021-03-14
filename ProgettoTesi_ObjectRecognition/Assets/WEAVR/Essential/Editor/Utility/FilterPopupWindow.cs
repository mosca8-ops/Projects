using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{
    public class FilterPopupWindow : EditorWindow
    {
        private static FilterPopupWindow _lastOpened;
        private Func<bool> _drawFunc;

        static FilterPopupWindow Create() {
            FilterPopupWindow window = ScriptableObject.CreateInstance<FilterPopupWindow>();
            return window;
        }

        private void Init(GUIContent guiContent, IEnumerable<object> objects, Func<object> bla) {

        }

        public static void ClosePopup() {
            if (_lastOpened != null) {
                _lastOpened.Close();
            }
        }

        public static void Show(Rect position, Func<bool> drawMethod) {
            if (_lastOpened != null) {
                _lastOpened.Close();
            }
            _lastOpened = CreateInstance<FilterPopupWindow>();
            _lastOpened._drawFunc = drawMethod;
            _lastOpened.position = position;
            _lastOpened.ShowPopup();
        }

        void OnGUI() {
            if (_drawFunc == null || _drawFunc()) {
                Close();
            }
        }

        private class Popup<T>
        {
            public Action<T> onSelect;
            public List<DataWrapper<T>> objects;

            public Popup(IEnumerable<T> data, Func<T, string> valueFunc, Func<T, bool> showFunc, Action<T> onSelectFunc, char separator) {
                objects = new List<DataWrapper<T>>();
                int index = 0;
                foreach (T elem in data) {
                    if (showFunc(elem)) {
                        GUIContent guiContent = new GUIContent(valueFunc(elem));
                        if(guiContent == null) { continue; }
                        objects.Add(new DataWrapper<T>() {
                            Id = guiContent.text ?? (index++).ToString(),
                            guiContent = guiContent,
                            data = elem
                        });
                    }
                }
                onSelect = onSelectFunc;
            }

            public Popup(IEnumerable<T> data, Func<T, GUIContent> valueFunc, Func<T, bool> showFunc, Action<T> onSelectFunc, char separator) {
                objects = new List<DataWrapper<T>>();
                int index = 0;
                foreach (T elem in data) {
                    if (showFunc(elem)) {
                        GUIContent guiContent = valueFunc(elem);
                        if (guiContent == null) { continue; }
                        objects.Add(new DataWrapper<T>() {
                            Id = guiContent.text ?? (index++).ToString(),
                            guiContent = guiContent,
                            data = elem
                        });
                    }
                }
                onSelect = onSelectFunc;
            }
        }

        private class DataWrapper<T>
        {
            public string Id;
            public GUIContent guiContent;
            public T data;
        }
    }
}