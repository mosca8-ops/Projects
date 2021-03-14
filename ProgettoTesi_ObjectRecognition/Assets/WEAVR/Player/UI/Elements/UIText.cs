using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

#endif

namespace TXT.WEAVR.UI
{
    [Serializable]
    public class UIText
    {
        [SerializeField]
        private Text m_simpleText;
        [SerializeField]
        private TextMeshProUGUI m_tmpText;

        public Text SimpleTextComponent
        {
            get => m_simpleText;
            set
            {
                if(m_simpleText != value)
                {
                    m_simpleText = value;
                    if (value)
                    {
                        m_tmpText = null;
                    }
                }
            }
        }

        public TextMeshProUGUI TextMeshProComponent
        {
            get => m_tmpText;
            set
            {
                if(m_tmpText != value)
                {
                    m_tmpText = value;
                }
            }
        }

        public Component ActiveTextComponent => m_tmpText ? m_tmpText as Component : m_simpleText;

        public string Text
        {
            get => m_tmpText ? m_tmpText.text : m_simpleText ? m_simpleText.text : null;
            set
            {
                if (m_tmpText)
                {
                    m_tmpText.text = value;
                }
                else if (m_simpleText)
                {
                    m_simpleText.text = value;
                }
            }
        }

        public void FindReferences(GameObject go)
        {
            var tmpComponent = go.GetComponent<TextMeshProUGUI>();
            if (tmpComponent)
            {
                TextMeshProComponent = tmpComponent;
                return;
            }
            var textComponent = go.GetComponent<Text>();
            if (textComponent)
            {
                SimpleTextComponent = textComponent;
                return;
            }
            tmpComponent = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpComponent)
            {
                TextMeshProComponent = tmpComponent;
                return;
            }
            textComponent = go.GetComponentInChildren<Text>();
            if (textComponent)
            {
                SimpleTextComponent = textComponent;
                return;
            }
        }
    }

#if UNITY_EDITOR


    [CustomPropertyDrawer(typeof(UIText))]
    public class UITextDrawer : PropertyDrawer
    {
        private const string k_simpleText = "m_simpleText";
        private const string k_tmpText = "m_tmpText";

        private int m_controlId;
        private UIText m_uiText;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (m_uiText == null)
            {
                m_uiText = fieldInfo.GetValue(property.serializedObject.targetObject) as UIText;
            }

            var simpleText = property.FindPropertyRelative(k_simpleText);
            var tmpText = property.FindPropertyRelative(k_tmpText);

            var e = Event.current;
            var guiEnabled = GUI.enabled;
            var pickerRect = new Rect(position.xMax - 20, position.y, 20, position.height);

            // Handle the execute command
            if (e.commandName == "ObjectSelectorUpdated" && m_controlId == EditorGUIUtility.GetObjectPickerControlID())
            {
                var pickedObject = EditorGUIUtility.GetObjectPickerObject();

                if (!pickedObject)
                {
                    simpleText.objectReferenceValue = null;
                    tmpText.objectReferenceValue = null;
                }
                else if (pickedObject is Text)
                {
                    simpleText.objectReferenceValue = pickedObject;
                    tmpText.objectReferenceValue = null;
                }
                else if (pickedObject is TextMeshProUGUI)
                {
                    simpleText.objectReferenceValue = null;
                    tmpText.objectReferenceValue = pickedObject;
                }
            }

            // Decide which property to show
            bool showTmp = tmpText.objectReferenceValue || !simpleText.objectReferenceValue;

            // Handle the drag & drop
            if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && position.Contains(e.mousePosition))
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject go)
                    {
                        if (go.GetComponent<TextMeshProUGUI>())
                        {
                            showTmp = true;
                            break;
                        }
                        else if (go.GetComponent<Text>())
                        {
                            showTmp = false;
                            break;
                        }
                    }
                    else if (obj is Component c)
                    {
                        if (c is TextMeshProUGUI)
                        {
                            showTmp = true;
                            break;
                        }
                        else if (c is Text)
                        {
                            showTmp = false;
                            break;
                        }
                        else
                        {
                            if (c.GetComponent<TextMeshProUGUI>())
                            {
                                showTmp = true;
                                break;
                            }
                            else if (c.GetComponent<Text>())
                            {
                                showTmp = false;
                                break;
                            }
                        }
                    }
                }
            }

            GUI.enabled = !(e.type == EventType.MouseDown && e.button == 0 && pickerRect.Contains(e.mousePosition));
            if (showTmp)
            {
                simpleText.objectReferenceValue = null;
                tmpText.objectReferenceValue = EditorGUI.ObjectField(position, label, tmpText.objectReferenceValue, typeof(TextMeshProUGUI), true);
            }
            else
            {
                tmpText.objectReferenceValue = null;
                simpleText.objectReferenceValue = EditorGUI.ObjectField(position, label, simpleText.objectReferenceValue, typeof(Text), true);
            }
            GUI.enabled = guiEnabled;
            m_controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (GUI.Button(pickerRect, "T"))
            {
                EditorGUIUtility.ShowObjectPicker<Component>(tmpText.objectReferenceValue ? tmpText.objectReferenceValue : simpleText.objectReferenceValue,
                                                             true,
                                                             "t:Text t:TextMeshProUGUI",
                                                             m_controlId);
            }
        }
    }

#endif
}
