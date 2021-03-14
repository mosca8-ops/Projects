using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Maintenance
{
    [CustomPropertyDrawer(typeof(SwitchState))]
    public class SwitchStateDrawer : PropertyDrawer
    {

        private bool m_initialized = false;
        private Vector2 m_boxOffset1;
        private Vector2 m_boxOffset2;
        private float m_height;
        private GUIStyle m_boxStyle;
        private GUIStyle m_displayNameStyle;
        private float m_lastDisplayNameAlpha = 0;

        private SwitchState m_state;
        private SwitchState[] m_states;
        private List<SwitchState> m_arrayStates;

        private const float kLeftPaneWidth = 122;
        private const float kLabelWidth = 40;
        private const float kColorWidth = 10;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_initialized)
            {
                Initialize(property);
            }

            if (m_arrayStates != null)
            {
                label.text = label.text.Replace("Element ", "");
                int index = int.Parse(label.text);
                if(m_arrayStates.Count <= index)
                {
                    Initialize(property);
                    for (int i = 0; i < m_arrayStates.Count; i++)
                    {
                        if(m_arrayStates[i].displayName == "0" && i != 0)
                        {
                            m_arrayStates[i].displayName = i.ToString();
                        }
                    }
                }
                m_state = m_arrayStates[int.Parse(label.text)];
            }

            GUI.Box(position, GUIContent.none, m_boxStyle);
            var p = BoxContentRect(position);
            var leftRect = p;
            var rightRect = p;
            leftRect.width = kLeftPaneWidth;
            rightRect.x += kLeftPaneWidth + EditorGUIUtility.standardVerticalSpacing;
            rightRect.width -= kLeftPaneWidth + EditorGUIUtility.standardVerticalSpacing;
            GUI.Box(leftRect, GUIContent.none, m_boxStyle);
            GUI.Box(rightRect, GUIContent.none, m_boxStyle);

            // Left Part
            leftRect = BoxContentRect(leftRect);
            var tempRect = leftRect;
            tempRect.height = EditorGUIUtility.singleLineHeight;
            GUI.Label(tempRect, label, EditorStyles.boldLabel);
            float labelWidth = Mathf.Max(EditorStyles.boldLabel.CalcSize(label).x, kLabelWidth);

            tempRect.x += labelWidth;
            tempRect.width = leftRect.width - labelWidth - kColorWidth;
            property = property.Copy();
            var innerProperty = property.FindPropertyRelative(nameof(SwitchState.displayName));
            if (innerProperty.stringValue == label.text || string.IsNullOrEmpty(innerProperty.stringValue))
            {
                SetDisplayNameAlpha(0.2f);
                innerProperty.stringValue = EditorGUI.TextField(tempRect, label.text, m_displayNameStyle);
            }
            else
            {
                SetDisplayNameAlpha(1.0f);
                innerProperty.stringValue = EditorGUI.TextField(tempRect, innerProperty.stringValue, m_displayNameStyle);
            }
            //innerProperty.stringValue = EditorGUI.TextField(tempRect, innerProperty.stringValue, m_displayNameStyle);

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            tempRect.x += tempRect.width;
            tempRect.width = kColorWidth;
            innerProperty = property.FindPropertyRelative(nameof(SwitchState.displayColor));
            innerProperty.colorValue = EditorGUI.ColorField(tempRect, GUIContent.none, innerProperty.colorValue, false, true, false);
            EditorGUI.indentLevel = indentLevel;

            tempRect.x = leftRect.x;
            tempRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            tempRect.width = 42;

            innerProperty = property.FindPropertyRelative(nameof(SwitchState.isStable));
            innerProperty.boolValue = GUI.Toggle(tempRect, innerProperty.boolValue, "Stable", EditorStyles.miniButtonLeft);
            tempRect.x += tempRect.width;
            tempRect.width = leftRect.width - tempRect.width;
            innerProperty = property.FindPropertyRelative(nameof(SwitchState.isContinuous));
            innerProperty.boolValue = GUI.Toggle(tempRect, innerProperty.boolValue, "Continuous", EditorStyles.miniButtonRight);

            // Right Part
            rightRect = BoxContentRect(rightRect);
            rightRect.x -= 4;
            rightRect.width += 8;
            tempRect = rightRect;
            tempRect.height = EditorGUIUtility.singleLineHeight;
            tempRect.width = 30;
            GUI.Label(tempRect, @"ΔPos", EditorStyles.miniLabel);
            tempRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            GUI.Label(tempRect, @"ΔRot", EditorStyles.miniLabel);

            tempRect.x = rightRect.x + 32;
            tempRect.y = rightRect.y;
            tempRect.width = rightRect.width - 70;

            innerProperty = property.FindPropertyRelative(nameof(SwitchState.deltaPosition));
            innerProperty.vector3Value = EditorGUI.Vector3Field(tempRect, GUIContent.none, innerProperty.vector3Value);
            tempRect.x += tempRect.width + 2;
            tempRect.width = 36;

            bool wasEnabled = GUI.enabled;
            GUI.enabled = m_states.Length != 1 || m_state.Switch != null;

            if (m_arrayStates != null && m_state != null && GUI.Button(tempRect, "Save", EditorStyles.miniButton))
            {
                m_state.Snapshot();
            }
            else if (m_states.Length > 0 && GUI.Button(tempRect, "Save", EditorStyles.miniButton))
            {
                for (int i = 0; i < m_states.Length; i++)
                {
                    m_states[i].Snapshot();
                }
            }

            GUI.enabled = wasEnabled;

            tempRect.x = rightRect.x + 32;
            tempRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            tempRect.width = rightRect.width - 70;

            innerProperty = property.FindPropertyRelative(nameof(SwitchState.deltaEuler));
            innerProperty.vector3Value = EditorGUI.Vector3Field(tempRect, GUIContent.none, innerProperty.vector3Value);
            tempRect.x += tempRect.width + 2;
            tempRect.width = 36;

            if (m_arrayStates != null && m_state != null)
            {
                if (GUI.Button(tempRect, "Set", EditorStyles.miniButton))
                {
                    m_state.Restore();
                }
            }
            else
            {
                GUI.enabled = m_states.Length != 1 || m_state.Switch != null;
                if (m_states.Length == 1 && GUI.Button(tempRect, "Set", EditorStyles.miniButton))
                {
                    m_state.Restore();
                }
                GUI.enabled = wasEnabled;
            }
        }

        private void SetDisplayNameAlpha(float v)
        {
            if (v == m_lastDisplayNameAlpha) { return; }
            m_lastDisplayNameAlpha = v;

            SetDisplayNameAlpha(v, m_displayNameStyle.normal);
            SetDisplayNameAlpha(v, m_displayNameStyle.active);
            SetDisplayNameAlpha(v, m_displayNameStyle.focused);
            SetDisplayNameAlpha(v, m_displayNameStyle.hover);
            SetDisplayNameAlpha(v, m_displayNameStyle.onNormal);
            SetDisplayNameAlpha(v, m_displayNameStyle.onActive);
            SetDisplayNameAlpha(v, m_displayNameStyle.onFocused);
            SetDisplayNameAlpha(v, m_displayNameStyle.onHover);
        }

        private void SetDisplayNameAlpha(float v, GUIStyleState state)
        {
            Color temp = state.textColor;
            temp.a = v;
            state.textColor = temp;
        }

        private void Initialize(SerializedProperty property)
        {
            m_boxStyle = new GUIStyle("Box");
            m_boxOffset1.x = m_boxStyle.border.left + /*m_boxStyle.margin.left +*/ m_boxStyle.padding.left;
            m_boxOffset1.y = m_boxStyle.border.top + /*m_boxStyle.margin.top +*/ m_boxStyle.padding.top;

            m_boxOffset2.x = m_boxStyle.border.right + /*m_boxStyle.margin.right +*/ m_boxStyle.padding.right;
            m_boxOffset2.y = m_boxStyle.border.bottom + /*m_boxStyle.margin.bottom +*/ m_boxStyle.padding.bottom;

            m_displayNameStyle = new GUIStyle(EditorStyles.textField);

            m_initialized = true;

            m_height = (m_boxOffset1.y + m_boxOffset2.y + EditorGUIUtility.singleLineHeight) * 2
                     + EditorGUIUtility.standardVerticalSpacing;

            m_state = fieldInfo.GetValue(property.serializedObject.targetObject) as SwitchState;
            m_states = new SwitchState[property.serializedObject.targetObjects.Length];
            for (int i = 0; i < m_states.Length; i++)
            {
                m_states[i] = fieldInfo.GetValue(property.serializedObject.targetObjects[i]) as SwitchState;
            }

            if (m_state == null)
            {
                // Probably it is a list
                var list = fieldInfo.GetValue(property.serializedObject.targetObject) as IEnumerable<SwitchState>;
                if (list != null)
                {
                    m_arrayStates = new List<SwitchState>();
                    foreach (var elem in list)
                    {
                        m_arrayStates.Add(elem);
                    }
                    //m_state = list.ElementAt(int.Parse(property.displayName.Replace("Element ", "")));
                    m_states = m_arrayStates.ToArray();
                }
            }
        }

        private Rect BoxContentRect(Rect rect)
        {
            var r = rect;
            r.x += m_boxOffset1.x;
            r.y += m_boxOffset1.y;
            r.width -= m_boxOffset2.x + m_boxOffset1.x;
            r.height -= m_boxOffset2.y + m_boxOffset2.y;
            return r;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!m_initialized)
            {
                Initialize(property);
            }
            return m_height;
        }
    }
}
