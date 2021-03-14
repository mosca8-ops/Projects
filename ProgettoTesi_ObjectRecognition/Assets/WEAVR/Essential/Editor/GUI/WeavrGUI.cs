namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEditor;
    using UnityEngine;

    using Object = UnityEngine.Object;

    public static class WeavrGUI
    {
        internal static readonly Dictionary<UnityEngine.Object, PropertyPathField> _propertyPaths = new Dictionary<UnityEngine.Object, PropertyPathField>();
        internal static PopupDataKeeper _popupKeeper;

        internal class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle dragIcon;
            public float boxWidth;

            protected override void InitializeStyles(bool isProSkin)
            {
                dragIcon = WeavrStyles.ControlsSkin.FindStyle("draggable_icon") ?? new GUIStyle();
                box = WeavrStyles.ControlsSkin.FindStyle("draggable_box") ?? new GUIStyle("Box");
                boxWidth = box.fixedWidth > 0 ? box.fixedWidth : 16;
            }
        }

        private static Styles s_styles = new Styles();

        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="rect">Where to draw</param>
        /// <param name="value">The input value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(Rect rect, object value) {
            if(value != null) {
                return ValueField(rect, "", value, value.GetType());
            }
            return value;
        }

        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="rect">Where to draw</param>
        /// <param name="value">The input value</param>
        /// <param name="type">The type of the value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(Rect rect, object value, Type type) {
            return ValueField(rect, "", value, type);
        }

        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="rect">Where to draw</param>
        /// <param name="label">The label of the field</param>
        /// <param name="value">The input value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(Rect rect, string label, object value) {
            if (value != null) {
                return ValueField(rect, label, value, value.GetType());
            }
            return value;
        }
        
        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="rect">Where to draw</param>
        /// <param name="label">The label of the field</param>
        /// <param name="value">The input value</param>
        /// <param name="type">The type of the value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(Rect rect, string label, object value, Type type) {
            if(type == null) {
                return value != null ? ValueField(rect, label, value, value.GetType()) : null;
            }
            else if (type == typeof(float)) {
                return EditorGUI.FloatField(rect, label, PropertyConvert.ToFloat(value));
            }
            else if (type == typeof(bool)) {
                return EditorGUI.Toggle(rect, label, PropertyConvert.ToBoolean(value));
            }
            else if (type == typeof(int)) {
                return EditorGUI.IntField(rect, label, PropertyConvert.ToInt(value));
            }
            else if (type == typeof(byte)) {
                return (byte) EditorGUI.IntField(rect, label, PropertyConvert.ToInt(value));
            }
            else if (type == typeof(string)) {
                return EditorGUI.TextField(rect, label, value != null ? value.ToString() : "");
            }
            else if (type == typeof(double)) {
                return EditorGUI.DoubleField(rect, label, PropertyConvert.ToDouble(value));
            }
            else if (type == typeof(Vector2)) {
                return EditorGUI.Vector2Field(rect, label, PropertyConvert.ToVector2(value));
            }
            else if (type == typeof(Vector3)) {
                return EditorGUI.Vector3Field(rect, label, PropertyConvert.ToVector3(value));
            }
            else if (type == typeof(Vector4)) {
                return EditorGUI.Vector4Field(rect, label, PropertyConvert.ToVector4(value));
            }
            else if (type == typeof(Color)) {
                return EditorGUI.ColorField(rect, label, PropertyConvert.ToColor(value));
            }
            else if (type.IsEnum) {
                return EditorGUI.EnumPopup(rect, label, PropertyConvert.ToEnum(value, type));
            }
            else if(value is UnityEngine.Object || type.IsSubclassOf(typeof(UnityEngine.Object))) {
                return EditorGUI.ObjectField(rect, label, PropertyConvert.ToUnityObject(value), type, true);
            }
            return value;
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="rect">The rect where to draw it</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        public static string PropertyPathPopup(Rect rect, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath) {
            PropertyPathField propertyPathField = null;
            if(!_propertyPaths.TryGetValue(obj, out propertyPathField)) {
                propertyPathField = new PropertyPathField();
                _propertyPaths.Add(obj, propertyPathField);
            }
            return propertyPathField.DrawPropertyPathField(rect, obj, propertyPath, forEdit, showFullPath);
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="caller">The caller of this popup</param>
        /// <param name="rect">Where to draw</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        public static string PropertyPathPopup(UnityEngine.Object caller, Rect rect, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath) {
            PropertyPathField propertyPathField = null;
            if (!_propertyPaths.TryGetValue(caller, out propertyPathField)) {
                propertyPathField = new PropertyPathField();
                _propertyPaths.Add(caller, propertyPathField);
            }
            return propertyPathField.DrawPropertyPathField(rect, obj, propertyPath, forEdit, showFullPath);
        }

        /// <summary>
        /// Gets the property info of the property path field
        /// </summary>
        /// <param name="caller">The caller of this method. It is needed to identify which property path instance it is</param>
        /// <returns>The property info of the specified path</returns>
        public static PropertyPathField.PropertyIdentifier GetPropertyInfo(UnityEngine.Object caller) {
            PropertyPathField propertyPathField = null;
            if (!_propertyPaths.TryGetValue(caller, out propertyPathField)) {
                propertyPathField = new PropertyPathField();
                _propertyPaths.Add(caller, propertyPathField);
            }
            return propertyPathField.SelectedProperty;
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, Rect rect, T selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (GUI.Button(rect, selectedValue != null ? getName(selectedValue) : "", EditorStyles.popup)){
                GenericMenu menu = new GenericMenu();
                foreach(var elem in values) {
                    menu.AddItem(new GUIContent(getName(elem)), elem.Equals(selectedValue), () => _popupKeeper = new PopupDataKeeper(caller, elem, typeof(T)));
                }
                menu.DropDown(rect);
            }
            else if(_popupKeeper != null && _popupKeeper.key.Equals(caller) && typeof(T) == _popupKeeper.type) {
                selectedValue = (T)_popupKeeper.value;
                _popupKeeper = null;
            }
            return selectedValue;
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, Rect rect, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            T value = default(T);
            foreach(var elem in values) {
                if(getName(elem) == selectedValue) {
                    value = elem;
                    break;
                }
            }
            return Popup(caller, rect, value, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, Rect rect, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (GUI.Button(rect, selectedValue ?? "", EditorStyles.popup)) {
                GenericMenu menu = new GenericMenu();
                foreach (var elem in values) {
                    string name = getName(elem);
                    menu.AddItem(new GUIContent(name), elem.Equals(selectedValue), () => _popupKeeper = new PopupDataKeeper(caller, name, typeof(T)));
                }
                menu.DropDown(rect);
            }
            else if (_popupKeeper != null && _popupKeeper.key.Equals(caller) && _popupKeeper.type == typeof(T)) {
                selectedValue = (string)_popupKeeper.value;
                _popupKeeper = null;
            }
            return selectedValue;
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, Rect rect, string label, T selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (!string.IsNullOrEmpty(label)) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, Rect rect, GUIContent label, T selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (label != null) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, Rect rect, string label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (!string.IsNullOrEmpty(label)) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, Rect rect, GUIContent label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (label != null) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, Rect rect, string label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (!string.IsNullOrEmpty(label)) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return PopupString(caller, rect, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, Rect rect, GUIContent label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (label != null) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return PopupString(caller, rect, selectedValue, values, getName);
        }

        #region Popup with Styles

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, Rect rect, T selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (GUI.Button(rect, selectedValue != null ? getName(selectedValue) : "", style)) {
                GenericMenu menu = new GenericMenu();
                foreach (var elem in values) {
                    menu.AddItem(new GUIContent(getName(elem)), elem.Equals(selectedValue), () => _popupKeeper = new PopupDataKeeper(caller, elem, typeof(T)));
                }
                menu.DropDown(rect);
            }
            else if (_popupKeeper != null && _popupKeeper.key.Equals(caller) && typeof(T) == _popupKeeper.type) {
                selectedValue = (T)_popupKeeper.value;
                _popupKeeper = null;
            }
            return selectedValue;
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, Rect rect, string selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            T value = default(T);
            foreach (var elem in values) {
                if (getName(elem) == selectedValue) {
                    value = elem;
                    break;
                }
            }
            return Popup(caller, rect, value, values, getName, style);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, Rect rect, string selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (GUI.Button(rect, selectedValue ?? "", style)) {
                GenericMenu menu = new GenericMenu();
                foreach (var elem in values) {
                    string name = getName(elem);
                    menu.AddItem(new GUIContent(name), elem.Equals(selectedValue), () => _popupKeeper = new PopupDataKeeper(caller, name, typeof(T)));
                }
                menu.DropDown(rect);
            }
            else if (_popupKeeper != null && _popupKeeper.key.Equals(caller) && _popupKeeper.type == typeof(T)) {
                selectedValue = (string)_popupKeeper.value;
                _popupKeeper = null;
            }
            return selectedValue;
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, Rect rect, string label, T selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (!string.IsNullOrEmpty(label)) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName, style);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, Rect rect, GUIContent label, T selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (label != null) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName, style);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, Rect rect, string label, string selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (!string.IsNullOrEmpty(label)) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName, style);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, Rect rect, GUIContent label, string selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (label != null) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return Popup(caller, rect, selectedValue, values, getName, style);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, Rect rect, string label, string selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (!string.IsNullOrEmpty(label)) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return PopupString(caller, rect, selectedValue, values, getName, style);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="rect">The rect where to draw the popup</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <param name="style">The style of the control</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, Rect rect, GUIContent label, string selectedValue, IEnumerable<T> values, Func<T, string> getName, GUIStyle style) {
            if (label != null) {
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(labelRect, label);
                rect.width = rect.width - labelRect.width;
                rect.x += labelRect.width;
            }
            return PopupString(caller, rect, selectedValue, values, getName, style);
        }

        #endregion

        #region [  DRAGGABLE OBJECT FIELD  ]

        static Dictionary<object, (bool potentialDrag, bool dragStarted)> m_propertiesState = new Dictionary<object, (bool potentialDrag, bool dragStarted)>();
        static GUIContent s_dragContent = new GUIContent(string.Empty, "Drag this object generically onto other object fields.\nHold Control for specific object type (e.g. RigidBody only for RigidBody fields)");

        public static void ClearDraggableStates() => m_propertiesState.Clear();

        public static Object DraggableObjectField(object id, Rect position, GUIContent label, Object value, Type type, bool allowSceneObjects)
        {
            s_styles.Refresh();

            float boxWidth = s_styles.boxWidth - EditorGUI.indentLevel * 15f;

            if (label != null && label != GUIContent.none)
            {
                position = EditorGUI.PrefixLabel(position, label);
            }

            var dragArea = new Rect(position.x, position.y + s_styles.box.margin.top, s_styles.boxWidth, position.height - s_styles.box.margin.vertical);

            position.x += boxWidth;
            position.width -= boxWidth;

            value = EditorGUI.ObjectField(position, GUIContent.none, value, type, allowSceneObjects);

            var e = Event.current;
            var isFocused = e.control || e.command;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = value;
            s_dragContent.image = isFocused ? s_styles.dragIcon.focused.background : s_styles.dragIcon.normal.background;
            GUI.Box(dragArea, s_dragContent, s_styles.box);

            if (value && dragArea.Contains(e.mousePosition))
            {
                if (!m_propertiesState.TryGetValue(id, out (bool potentialDrag, bool dragStarted) state))
                {
                    state.potentialDrag = false;
                    state.dragStarted = false;
                }
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (!state.dragStarted)
                        {
                            state.potentialDrag = true;
                            e.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (state.potentialDrag && !state.dragStarted)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                            DragAndDrop.objectReferences = new Object[] { isFocused ? value : GetCompatibleObject(value) };
                            DragAndDrop.StartDrag("Move object");
                            state.dragStarted = true;
                            e.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (state.dragStarted)
                        {
                            state.dragStarted = false;
                            e.Use();
                        }
                        break;
                }

                m_propertiesState[id] = state;
            }
            else if (e.type != EventType.Layout)
            {
                m_propertiesState.Remove(id);
            }

            GUI.enabled = wasEnabled;

            return value;
        }

        private static Object GetCompatibleObject(Object obj) => obj is Component c ? c.gameObject : obj is GameObject go ? go : obj;

        #endregion

        internal class PopupDataKeeper
        {
            public object key;
            public object value;
            public Type type;

            public PopupDataKeeper(object key, object value, Type type) {
                this.key = key;
                this.value = value;
                this.type = type;
            }
        }
    }

    public static class WeavrGUILayout
    {
        private static Rect _lastPopupRect;

        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="value">The input value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(object value) {
            if (value != null) {
                return ValueField(null, value, value.GetType());
            }
            return value;
        }

        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="type">The type of the value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(object value, Type type) {
            return ValueField(null, value, type);
        }

        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="label">The label of the field</param>
        /// <param name="value">The input value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(string label, object value) {
            if (value != null) {
                return ValueField(label, value, value.GetType());
            }
            return value;
        }

        /// <summary>
        /// Draws a generic value field based on passed options
        /// </summary>
        /// <param name="label">The label of the field</param>
        /// <param name="value">The input value</param>
        /// <param name="type">The type of the value</param>
        /// <returns>The modified value</returns>
        public static object ValueField(string label, object value, Type type) {
            if (type == null) {
                return value != null ? ValueField(label, value, value.GetType()) : null;
            }
            else if (type == typeof(float)) {
                return EditorGUILayout.FloatField(label, PropertyConvert.ToFloat(value));
            }
            else if (type == typeof(bool)) {
                return EditorGUILayout.Toggle(label, PropertyConvert.ToBoolean(value));
            }
            else if (type == typeof(int)) {
                return EditorGUILayout.IntField(label, PropertyConvert.ToInt(value));
            }
            else if (type == typeof(byte)) {
                return EditorGUILayout.IntField(label, PropertyConvert.ToInt(value));
            }
            else if (type == typeof(string)) {
                return EditorGUILayout.TextField(label, value != null ? value.ToString() : "");
            }
            else if (type == typeof(double)) {
                return EditorGUILayout.FloatField(label, PropertyConvert.ToFloat(value));
            }
            else if (type == typeof(Vector2)) {
                return EditorGUILayout.Vector2Field(label, PropertyConvert.ToVector2(value));
            }
            else if (type == typeof(Vector3)) {
                return EditorGUILayout.Vector3Field(label, PropertyConvert.ToVector3(value));
            }
            else if (type == typeof(Vector4)) {
                return EditorGUILayout.Vector4Field(label, PropertyConvert.ToVector4(value));
            }
            else if (type == typeof(Color)) {
                return EditorGUILayout.ColorField(label, PropertyConvert.ToColor(value));
            }
            else if (type != null && type.IsEnum) {
                return EditorGUILayout.EnumPopup(label, PropertyConvert.ToEnum(value, type));
            }
            else if (value is UnityEngine.Object || type.IsSubclassOf(typeof(UnityEngine.Object))) {
                return EditorGUILayout.ObjectField(label, PropertyConvert.ToUnityObject(value), type, true);
            }
            return value;
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        public static string PropertyPathPopup(UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath) {
            PropertyPathField propertyPathField = null;
            if (!WeavrGUI._propertyPaths.TryGetValue(obj, out propertyPathField)) {
                propertyPathField = new PropertyPathField();
                WeavrGUI._propertyPaths.Add(obj, propertyPathField);
            }
            return propertyPathField.DrawPropertyPathField(obj, propertyPath, forEdit, showFullPath);
        }

        /// <summary>
        /// Draw the field responsible for selecting the property path
        /// </summary>
        /// <param name="caller">The caller of this popup</param>
        /// <param name="obj">The object to show the properties for</param>
        /// <param name="propertyPath">The current property path</param>
        /// <param name="forEdit">Whether to show properties for edit or for read only</param>
        /// <param name="showFullPath">Whether to show the full path or only the property name</param>
        public static string PropertyPathPopup(UnityEngine.Object caller, UnityEngine.Object obj, string propertyPath, bool forEdit, bool showFullPath) {
            PropertyPathField propertyPathField = null;
            if (!WeavrGUI._propertyPaths.TryGetValue(caller, out propertyPathField)) {
                propertyPathField = new PropertyPathField();
                WeavrGUI._propertyPaths.Add(caller, propertyPathField);
            }
            return propertyPathField.DrawPropertyPathField(obj, propertyPath, forEdit, showFullPath);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, T selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (GUILayout.Button(selectedValue != null ? getName(selectedValue) : "", EditorStyles.popup)) {
                GenericMenu menu = new GenericMenu();
                foreach (var elem in values) {
                    menu.AddItem(new GUIContent(getName(elem)), elem.Equals(selectedValue), 
                                 () => WeavrGUI._popupKeeper = new WeavrGUI.PopupDataKeeper(caller, elem, typeof(T)));
                }
                menu.DropDown(_lastPopupRect);
            }
            else if (WeavrGUI._popupKeeper != null && WeavrGUI._popupKeeper.key.Equals(caller) && WeavrGUI._popupKeeper.type == typeof(T)) {
                selectedValue = (T)WeavrGUI._popupKeeper.value;
                WeavrGUI._popupKeeper = null;
            }
            else if (Event.current.type == EventType.Repaint) {
                _lastPopupRect = GUILayoutUtility.GetLastRect();
            }
            return selectedValue;
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            T value = default(T);
            foreach (var elem in values) {
                if (getName(elem) == selectedValue) {
                    value = elem;
                    break;
                }
            }
            return Popup(caller, value, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (GUILayout.Button(selectedValue ?? "", EditorStyles.popup)) {
                GenericMenu menu = new GenericMenu();
                foreach (var elem in values) {
                    string name = getName(elem);
                    menu.AddItem(new GUIContent(name), elem.Equals(selectedValue), 
                                    () => WeavrGUI._popupKeeper = new WeavrGUI.PopupDataKeeper(caller, name, typeof(T)));
                }
                menu.DropDown(_lastPopupRect);
            }
            else if (WeavrGUI._popupKeeper != null && WeavrGUI._popupKeeper.key.Equals(caller) && WeavrGUI._popupKeeper.type == typeof(T)) {
                selectedValue = (string)WeavrGUI._popupKeeper.value;
                WeavrGUI._popupKeeper = null;
            }
            else if (Event.current.type == EventType.Repaint) {
                _lastPopupRect = GUILayoutUtility.GetLastRect();
            }
            return selectedValue;
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, string label, T selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (!string.IsNullOrEmpty(label)) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                var returnValue = Popup(caller, selectedValue, values, getName);
                EditorGUILayout.EndHorizontal();
                return returnValue;
            }
            return Popup(caller, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value</returns>
        public static T Popup<T>(object caller, GUIContent label, T selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (label != null) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                var returnValue = Popup(caller, selectedValue, values, getName);
                EditorGUILayout.EndHorizontal();
                return returnValue;
            }
            return Popup(caller, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, string label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (!string.IsNullOrEmpty(label)) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                var returnValue = Popup(caller, selectedValue, values, getName);
                EditorGUILayout.EndHorizontal();
                return returnValue;
            }
            return Popup(caller, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static T Popup<T>(object caller, GUIContent label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (label != null) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                var returnValue = Popup(caller, selectedValue, values, getName);
                EditorGUILayout.EndHorizontal();
                return returnValue;
            }
            return Popup(caller, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, string label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (!string.IsNullOrEmpty(label)) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                var returnValue = PopupString(caller, selectedValue, values, getName);
                EditorGUILayout.EndHorizontal();
                return returnValue;
            }
            return PopupString(caller, selectedValue, values, getName);
        }

        /// <summary>
        /// Creates a popup component for the selected values
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        /// <param name="caller">The caller for which to return the selected value</param>
        /// <param name="label">The label for this popup</param>
        /// <param name="selectedValue">The caller selected value</param>
        /// <param name="values">The available values</param>
        /// <param name="getName">The function to convert the element to a label option</param>
        /// <returns>The selected value as string</returns>
        public static string PopupString<T>(object caller, GUIContent label, string selectedValue, IEnumerable<T> values, Func<T, string> getName) {
            if (label != null) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                var returnValue = PopupString(caller, selectedValue, values, getName);
                EditorGUILayout.EndHorizontal();
                return returnValue;
            }
            return PopupString(caller, selectedValue, values, getName);
        }

        #region [  DRAGGABLE OBJECT FIELD  ]

        internal class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle dragIcon;
            public float boxWidth;

            protected override void InitializeStyles(bool isProSkin)
            {
                dragIcon = WeavrStyles.ControlsSkin.FindStyle("draggable_icon") ?? new GUIStyle();
                box = WeavrStyles.ControlsSkin.FindStyle("draggable_box") ?? new GUIStyle("Box");
                boxWidth = box.fixedWidth > 0 ? box.fixedWidth : 16;
            }
        }

        private static Styles s_styles = new Styles();

        static Dictionary<object, (bool potentialDrag, bool dragStarted)> m_propertiesState = new Dictionary<object, (bool potentialDrag, bool dragStarted)>();

        public static Object DraggableObjectField(object id, GUIContent label, Object value, Type type, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            s_styles.Refresh();

            float boxWidth = s_styles.boxWidth - EditorGUI.indentLevel * 15f;

            EditorGUILayout.BeginHorizontal();

            if (label != null && label != GUIContent.none)
            {
                EditorGUILayout.PrefixLabel(label);
            }

            value = EditorGUILayout.ObjectField(GUIContent.none, value, type, allowSceneObjects, options);

            var dragArea = GUILayoutUtility.GetRect(s_styles.boxWidth, EditorGUIUtility.singleLineHeight);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = value;
            GUI.Box(dragArea, s_styles.dragIcon.normal.background, s_styles.box);
            GUI.enabled = wasEnabled;
            var e = Event.current;

            if (value && dragArea.Contains(e.mousePosition))
            {
                if (!m_propertiesState.TryGetValue(id, out (bool potentialDrag, bool dragStarted) state))
                {
                    state.potentialDrag = false;
                    state.dragStarted = false;
                }
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (!state.dragStarted)
                        {
                            state.potentialDrag = true;
                            e.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (state.potentialDrag && !state.dragStarted)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                            DragAndDrop.objectReferences = new Object[] { GetCompatibleObject(value) };
                            DragAndDrop.StartDrag("Move object");
                            state.dragStarted = true;
                            e.Use();
                        }
                        break;
                    case EventType.MouseUp:
                        if (state.dragStarted)
                        {
                            state.dragStarted = false;
                            e.Use();
                        }
                        break;
                }

                m_propertiesState[id] = state;
            }
            else if (e.type != EventType.Layout)
            {
                m_propertiesState.Remove(id);
            }

            EditorGUILayout.EndHorizontal();

            return value;
        }

        private static Object GetCompatibleObject(Object obj) => obj is Component c ? c.gameObject : obj is GameObject go ? go : obj;

        #endregion
    }
}