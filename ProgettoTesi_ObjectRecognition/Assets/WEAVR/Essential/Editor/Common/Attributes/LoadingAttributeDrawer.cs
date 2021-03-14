using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Editor
{
    [CustomPropertyDrawer(typeof(LoadingAttribute), true)]
    public class LoadingAttributeDrawer : ComposablePropertyDrawer
    {
        const string k_inspectorWindowType = "UnityEditor.InspectorWindow";

        private class Styles : BaseStyles
        {
            public GUIStyle isLoading;
            public GUIStyle progress;
            public Color backgroundColor;

            protected override void InitializeStyles(bool isProSkin)
            {
                isLoading = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                };

                progress = new GUIStyle(EditorStyles.boldLabel)
                {
                    fixedHeight = 2
                };

                backgroundColor = isProSkin ? WeavrStyles.Colors.windowBackgroundDark : WeavrStyles.Colors.windowBackgroundLite;
                backgroundColor.a = 0.8f;
            }
        }

        private static Styles s_styles = new Styles();

        bool m_initialized;

        private GUIContent m_label;
        private Func<bool> m_isLoading;
        private Func<float> m_progress;

        private EditorWindow m_inspector;

        public override bool CanCacheInspectorGUI(SerializedProperty property) => !m_isLoading?.Invoke() ?? true;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!m_initialized)
            {
                s_styles.Refresh();
                m_initialized = true;
                Initialize(property);
            }

            bool isLoading = m_isLoading?.Invoke() ?? false;

            if(!isLoading)
            {
                base.OnGUI(position, property, label);
                return;
            }

            var color = GUI.contentColor;
            var newColor = color;
            newColor.a *= 0.75f;
            GUI.contentColor = newColor;

            var wasEnabled = GUI.enabled;
            GUI.enabled = false;
            base.OnGUI(position, property, label);
            GUI.enabled = wasEnabled;

            EditorGUI.DrawRect(position, s_styles.backgroundColor);
            GUI.Label(position, m_label, s_styles.isLoading);

            if(m_progress != null)
            {
                EditorGUI.DrawRect(new Rect(position.x,
                                            position.y + position.height - s_styles.progress.fixedHeight,
                                            position.width * Mathf.Clamp01(m_progress()),
                                            s_styles.progress.fixedHeight), s_styles.progress.normal.textColor);
            }

            GUI.contentColor = color;
            m_inspector.Repaint();
        }

        private void Initialize(SerializedProperty property)
        {
            if (attribute is LoadingAttribute attr && !string.IsNullOrEmpty(attr.IsLoadingMethodName))
            {
                m_label = new GUIContent(string.IsNullOrEmpty(attr.Label) ? "Loading" : attr.Label, WeavrStyles.Icons["loading"]);
                m_isLoading = Delegate.CreateDelegate(typeof(Func<bool>), property.serializedObject.targetObject, attr.IsLoadingMethodName) as Func<bool>;
                if(m_isLoading != null && !string.IsNullOrEmpty(attr.LoadingProgressMethodName))
                {
                    m_progress = Delegate.CreateDelegate(typeof(Func<float>), property.serializedObject.targetObject, attr.LoadingProgressMethodName) as Func<float>;
                }
                m_inspector = ShowEditorWindowWithTypeName(k_inspectorWindowType);
            }
        }

        public static EditorWindow ShowEditorWindowWithTypeName(string windowTypeName)
        {
            var windowType = typeof(UnityEditor.Editor).Assembly.GetType(windowTypeName);
            return EditorWindow.GetWindow(windowType);
        }
    }
}
