using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Localization
{
    [CustomPropertyDrawer(typeof(LocalizedItem), useForChildren: true)]
    public class LocalizedItemDrawer : ComposablePropertyDrawer
    {
        private static GUIStyle s_boxStyle;
        private static GUIContent s_defaultValueContent = new GUIContent("Default");

        // Profiler strings
        private string m_drawFullDebugName;
        private string m_getHeightDebugName;
        private string m_drawMainValueDebugName;
        private string m_drawOtherValuesDebugName;

        private GUIContent[] m_labels;
        private float[] m_heights;
        private float m_defaultValueHeight;
        private float m_targetHeight;
        private IEnumerable<Language> m_languageSet;
        private System.Func<object, object> m_itemGetter;

        private SerializedProperty m_mainKey;
        private SerializedProperty m_mainValue;
        private SerializedProperty m_fallbackValue;
        private bool m_hasMultipleValues;

        private bool? m_autofill;
        private bool m_primaryHasChanged;
        private HashSet<int> m_indicesToAdapt = new HashSet<int>();
        private bool m_adaptDefaultValue;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(Event.current.type == EventType.Layout) { return; }

            if (m_hasMultipleValues)
            {
                Profiler.BeginSample(m_drawFullDebugName);

                var value = m_mainValue;
                var rect = position;

                Profiler.BeginSample(m_drawMainValueDebugName);
                if (m_autofill == true)
                {
                    EditorGUI.BeginChangeCheck();
                    DrawMainValueWithExpand(ref rect, property, m_mainKey, value, label, m_targetHeight);
                    m_primaryHasChanged = EditorGUI.EndChangeCheck();
                }
                else
                {
                    DrawMainValueWithExpand(ref rect, property, m_mainKey, value, label, m_targetHeight);
                }
                Profiler.EndSample();

                var defaultProperty = property.FindPropertyRelative("m_defaultValue");

                if (m_adaptDefaultValue)
                {
                    defaultProperty.TryCopyValueFrom(value);
                }

                if (property.isExpanded)
                {
                    if(Event.current.type == EventType.Repaint)
                    {
                        if(s_boxStyle == null) { s_boxStyle = "Box"; }
                        s_boxStyle.Draw(new Rect(position.x + 15, 
                                                 rect.y + rect.height, 
                                                 position.width - 14, 
                                                 rect.height + m_defaultValueHeight 
                                                             + EditorGUIUtility.standardVerticalSpacing 
                                                             + EditorGUIUtility.standardVerticalSpacing 
                                                             + EditorGUIUtility.standardVerticalSpacing), 
                                        false, false, false, false);
                    }
                    EditorGUI.indentLevel++;
                    var keyProperty = property.FindPropertyRelative("m_key");
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    rect.x = position.x;
                    rect.width = position.width - 52;
                    rect.height = EditorGUIUtility.singleLineHeight;


                    EditorGUI.PropertyField(rect, keyProperty);

                    rect.x += rect.width + 2;
                    rect.width = 50;

                    if (GUI.Button(rect, "Find"))
                    {

                    }

                    rect.x = position.x;
                    rect.width = position.width;
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    rect.height = m_defaultValueHeight;

                    TargetPropertyField(rect, m_mainKey, defaultProperty, s_defaultValueContent, property.isExpanded);

                    //rect.x = position.x;
                    //rect.width = position.width;
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                    var dictionary = property.FindPropertyRelative("m_values");
                    if (dictionary != null)
                    {
                        var values = dictionary.FindPropertyRelative("values");
                        var keys = dictionary.FindPropertyRelative("keys");

                        Profiler.BeginSample(m_drawOtherValuesDebugName);

                        if (m_autofill == true)
                        {
                            if (m_primaryHasChanged)
                            {
                                foreach (var index in m_indicesToAdapt)
                                {
                                    values.GetArrayElementAtIndex(index).TryCopyValueFrom(value);
                                }
                            }
                            m_primaryHasChanged = false;

                            for (int i = 0; i < values.arraySize; i++)
                            {
                                value = values.GetArrayElementAtIndex(i);
                                rect.height = m_heights[i];
                                EditorGUI.BeginChangeCheck();
                                TargetPropertyField(rect, keys.GetArrayElementAtIndex(i), value, m_labels[i], property.isExpanded);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (value.TryGetValue() != null)
                                    {
                                        m_indicesToAdapt.Remove(i);
                                    }
                                    else
                                    {
                                        m_indicesToAdapt.Add(i);
                                    }
                                }
                                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < values.arraySize; i++)
                            {
                                value = values.GetArrayElementAtIndex(i);
                                rect.height = m_heights[i];
                                TargetPropertyField(rect, keys.GetArrayElementAtIndex(i), value, m_labels[i], property.isExpanded);
                                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                            }
                        }

                        Profiler.EndSample();
                    }

                    //rect.height = 1;
                    //EditorGUI.DrawRect(rect, Color.black);

                    EditorGUI.indentLevel--;
                }
                else if (m_autofill == true && m_primaryHasChanged)
                {
                    var vals = property.FindPropertyRelative("m_values").FindPropertyRelative("values");
                    foreach (var index in m_indicesToAdapt)
                    {
                        vals.GetArrayElementAtIndex(index).TryCopyValueFrom(value);
                    }
                    m_primaryHasChanged = false;
                }

                if(m_fallbackValue != null)
                {
                    property.FindPropertyRelative("m_defaultValue").TryCopyValueFrom(m_fallbackValue);
                }
                Profiler.EndSample();
            }
            else
            {
                property.isExpanded = false;
                TargetPropertyField(position, m_mainKey, m_mainValue ?? property, label, false);
            }
        }

        protected virtual void DrawMainValueWithExpand(ref Rect rect, SerializedProperty property, SerializedProperty key, SerializedProperty value, GUIContent label, float targetHeight)
        {
            float rectWidth = rect.width;
            rect.width = EditorGUIUtility.labelWidth;
            rect.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            rect.x += rect.width;
            rect.width = rectWidth - rect.width - 48;
            rect.height = targetHeight;

            TargetPropertyField(rect, key, value, GUIContent.none, false);

            rect.x += rect.width + 2;
            rect.width = 46;
            GUI.Label(rect, LocalizationManager.Current.CurrentLanguage ? LocalizationManager.Current.CurrentLanguage.Name : "--", EditorStyles.centeredGreyMiniLabel);
        }

        protected virtual void TargetPropertyField(Rect position, SerializedProperty key, SerializedProperty value, GUIContent label, bool isExpanded)
        {
            EditorGUI.PropertyField(position, value, label);
        }

        private bool GetDefaultValue(SerializedProperty property, out SerializedProperty keyProperty, out SerializedProperty valueProperty)
        {
            m_fallbackValue = null;
            if(m_drawMainValueDebugName == null)
            {
                var propertyTypeName = (property.GetPropertyType() ?? typeof(LocalizedItem)).Name;
                m_drawFullDebugName = $"{propertyTypeName}::Draw()";
                m_getHeightDebugName = $"{propertyTypeName}::GetHeight()";
                m_drawMainValueDebugName = $"{propertyTypeName}::DrawMainValue()";
                m_drawOtherValuesDebugName = $"{propertyTypeName}::DrawOtherValues()";
            }

            keyProperty = null;
            valueProperty = property.FindPropertyRelative("m_defaultValue");

            var languages = LocalizationManager.CurrentTable ? LocalizationManager.CurrentTable.Languages : null;
            if(languages == null)
            {
                return false;
            }

            if (!m_autofill.HasValue)
            {
                m_autofill = property.GetAttributeInParents<AutoFillAttribute>() != null 
                    && (valueProperty.propertyType == SerializedPropertyType.String 
                     || valueProperty.propertyType == SerializedPropertyType.ObjectReference 
                     || valueProperty.propertyType == SerializedPropertyType.Generic);
            }

            var dictionary = property.FindPropertyRelative("m_values");
            if (dictionary != null)
            {
                var keys = dictionary.FindPropertyRelative("keys");
                if(m_languageSet != languages || keys.arraySize != languages.Count)
                {
                    if(m_itemGetter == null)
                    {
                        m_itemGetter = property.serializedObject.targetObject.GetType().FieldPathGet(property.propertyPath);
                    }
                    foreach(var t in property.serializedObject.targetObjects)
                    {
                        //(fieldInfo.GetValue(t) as LocalizedItem).UpdateLanguages();
                        (m_itemGetter(t) as LocalizedItem).UpdateLanguages();
                    }
                    m_labels = null;
                    m_heights = null;
                    m_languageSet = languages;

                    var serializedObject = property.serializedObject;
                    serializedObject.Update();
                    m_indicesToAdapt.Clear();
                    var vals = dictionary.FindPropertyRelative("values");

                    var mainProperty = GetMainProperty(property);

                    m_adaptDefaultValue = (valueProperty.propertyType == SerializedPropertyType.String 
                                            && string.IsNullOrEmpty(valueProperty.stringValue))
                            || valueProperty.TryGetValue() == null
                            || (mainProperty != null && SerializedProperty.DataEquals(valueProperty, mainProperty));

                    for (int i = 0; i < vals.arraySize; i++)
                    {
                        var iterProperty = vals.GetArrayElementAtIndex(i);
                        if((iterProperty.propertyType == SerializedPropertyType.String && string.IsNullOrEmpty(iterProperty.stringValue))
                            || iterProperty.TryGetValue() == null 
                            || (mainProperty != null && SerializedProperty.DataEquals(iterProperty, mainProperty)))
                        {
                            m_indicesToAdapt.Add(i);
                        }
                        if (vals.GetArrayElementAtIndex(i).TryGetValue() == null)
                        {
                            m_indicesToAdapt.Add(i);
                        }
                    }
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    
                    return false;
                }

                m_languageSet = languages;

                if (m_labels == null)
                {
                    m_labels = new GUIContent[languages.Count];
                    m_heights = new float[languages.Count];
                    for (int i = 0; i < languages.Count; i++)
                    {
                        m_labels[i] = new GUIContent(languages[i].DisplayName);
                    }
                }

                var values = dictionary.FindPropertyRelative("values");
                string language = LocalizationManager.Current.CurrentLanguage ? LocalizationManager.Current.CurrentLanguage.Name : null;
                string defaultLanguage = LocalizationManager.Current.DefaultLanguage ? LocalizationManager.Current.DefaultLanguage.Name : null;

                for (int i = 0; i < keys.arraySize; i++)
                {
                    var key = keys.GetArrayElementAtIndex(i);
                    if(key.stringValue == defaultLanguage)
                    {
                        m_fallbackValue = values.GetArrayElementAtIndex(i);
                    }
                    if (key.stringValue == language)
                    {
                        var elemProperty = values.GetArrayElementAtIndex(i);
                        valueProperty.TryCopyValueFrom(elemProperty);
                        valueProperty = elemProperty;
                        keyProperty = key;
                        
                        return true;
                    }
                }
            }
            
            return false;
        }

        private bool GetPairAt(SerializedProperty property, int index, out SerializedProperty key, out SerializedProperty value)
        {
            var dictionary = property.FindPropertyRelative("m_values");
            if (dictionary != null)
            {
                var keys = dictionary.FindPropertyRelative("keys");
                if (keys != null && keys.isArray && keys.arraySize > index)
                {
                    var values = dictionary.FindPropertyRelative("values");
                    key = keys.GetArrayElementAtIndex(index);
                    value = values.GetArrayElementAtIndex(index);
                    return true;
                }
            }
            key = null;
            value = null;
            return false;
        }

        protected virtual float GetTargetPropertyHeight(SerializedProperty value)
        {
            return EditorGUI.GetPropertyHeight(value);
        }

        protected SerializedProperty GetMainProperty(SerializedProperty property)
        {
            var keys = GetKeyProperty(property);

            string language = LocalizationManager.Current.CurrentLanguage ? LocalizationManager.Current.CurrentLanguage.Name : null;

            for (int i = 0; i < keys.arraySize; i++)
            {
                if (keys.GetArrayElementAtIndex(i).stringValue == language)
                {
                    return GetValueProperty(property).GetArrayElementAtIndex(i);
                }
            }
            return null;
        }

        protected SerializedProperty GetKeyProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_values")?.FindPropertyRelative("keys");
        }

        protected SerializedProperty GetValueProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("m_values")?.FindPropertyRelative("values");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            m_hasMultipleValues = GetDefaultValue(property, out m_mainKey, out m_mainValue);

            m_targetHeight = GetTargetPropertyHeight(m_mainValue);
            float height = m_targetHeight + EditorGUIUtility.standardVerticalSpacing;
            if (property.isExpanded)
            {
                var values = GetValueProperty(property);

                m_defaultValueHeight = GetTargetPropertyHeight(property.FindPropertyRelative("m_defaultValue"));

                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
                        + m_defaultValueHeight + EditorGUIUtility.standardVerticalSpacing;

                if(values != null && values.arraySize > 0)
                {
                    if(m_heights == null)
                    {
                        m_heights = new float[values.arraySize];
                    }
                    for (int i = 0; i < values.arraySize; i++)
                    {
                        float localHeight = GetTargetPropertyHeight(values.GetArrayElementAtIndex(i));
                        m_heights[i] = localHeight;
                        height += localHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }

            return height;
        }
    }
}
