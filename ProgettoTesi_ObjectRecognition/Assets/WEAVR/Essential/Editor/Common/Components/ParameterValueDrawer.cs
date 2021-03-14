using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Core;
using System.Linq;
using System.Reflection;

using UnityObject = UnityEngine.Object;
using System.Text;
using TXT.WEAVR.Procedure;

namespace TXT.WEAVR
{
    [CustomPropertyDrawer(typeof(ParameterValue))]
    public class ParameterValueDrawer : PropertyDrawer
    {
        private const string k_GameObjectName = "_GenericValue_ShadowObject_";
        private const HideFlags k_HiddenAndEditable = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;

        private static readonly Type[] s_skipAttributeTypes =
        {
            typeof(SerializeField),
            typeof(SpaceAttribute),
            typeof(TooltipAttribute),
            typeof(HeaderAttribute),
        };

        

        private class ParameterData
        {
            public bool initialized;
            public bool updateType;
            public int paramId;
            public Type parameterType;
            public Dictionary<GenericValue.ValueType, int> enumIndices;
            public ParameterValue target;
            public string[] enumValues;
            public Func<object, object> targetGetter;
            public VariableFieldDrawer variableDrawer;
        }

        private Dictionary<string, ParameterData> m_params = new Dictionary<string, ParameterData>();

        public ParameterValue GetTarget(SerializedProperty property)
        {
            var data = GetParameterData(property);
            if (data.target == null)
            {
                data.target = data.targetGetter?.Invoke(property.serializedObject.targetObject) as ParameterValue;
            }
            return data.target;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return false;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var data = GetParameterData(property);
            data.updateType = false;

            if (!data.initialized)
            {
                Initialize(property);
            }

            int paramId = property.FindPropertyRelative("m_paramId").intValue;
            if(data.paramId != paramId)
            {
                data.paramId = paramId;
                data.updateType = true;
                data.parameterType = null;
                data.target = null;
            }

            SerializedProperty innerProperty = null;
            var typeProperty = property.FindPropertyRelative("m_valueType");
            var typenameProperty = property.FindPropertyRelative("m_typename");
            
            if (data.parameterType == null || typenameProperty.stringValue != data.parameterType.AssemblyQualifiedName)
            {
                ResetValues(property);
                data.parameterType = Type.GetType(typenameProperty.stringValue);
                data.updateType = true;
            }
            //data.m_parameterType = propertyType;
            var propertyType = data.parameterType;


            var wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            //EditorGUI.BeginChangeCheck();

            if (propertyType == typeof(bool))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_boolValue"), label, true);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Boolean];
                }
            }
            else if (propertyType == typeof(int))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_intValue"), label, true);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Integer];
                }
            }
            else if (propertyType == typeof(byte))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_byteValue"), label, true);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Byte];
                }
            }
            else if (propertyType == typeof(short))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_shortValue"), label, true);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Short];
                }
            }
            else if (propertyType == typeof(float) || propertyType == typeof(double))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_floatValue"), label, true);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Float];
                }
            }
            else if (propertyType == typeof(string))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_stringValue"), label, true);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.String];
                }
            }
            else if (propertyType != null && propertyType.IsEnum)
            {
                innerProperty = property.FindPropertyRelative("m_intValue");
                if (data.enumValues == null)
                {
                    data.enumValues = Enum.GetNames(propertyType);
                }
                innerProperty.intValue = EditorGUI.Popup(EditorGUI.PrefixLabel(position, label), innerProperty.intValue, data.enumValues);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Enum];
                }
            }
            else if (propertyType == typeof(AnimationCurve))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_animationCurveValue"), label, true);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.AnimationCurve];
                }
            }
            else if (propertyType == typeof(Vector2))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                innerProperty.vector4Value = EditorGUI.Vector2Field(position, label, innerProperty.vector4Value);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Vector2];
                }
            }
            else if (propertyType == typeof(Vector3))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                if (data.variableDrawer == null) { data.variableDrawer = new VariableFieldDrawer(); }
                if (!data.variableDrawer.TryDrawField(position, property.FindPropertyRelative("m_variable"), label, out position))
                {
                    innerProperty.vector4Value = EditorGUI.Vector3Field(position, label, innerProperty.vector4Value);
                }
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Vector3];
                }
            }
            else if (propertyType == typeof(Vector4) || propertyType == typeof(Quaternion))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                innerProperty.vector4Value = EditorGUI.Vector4Field(position, label, innerProperty.vector4Value);
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Vector4];
                }
            }
            else if (propertyType == typeof(Color))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                if (data.variableDrawer == null) { data.variableDrawer = new VariableFieldDrawer(); }
                if (!data.variableDrawer.TryDrawField(position, property.FindPropertyRelative("m_variable"), label, out position))
                {
                    innerProperty.vector4Value = EditorGUI.ColorField(position, label, innerProperty.vector4Value);
                }
                if (data.updateType)
                {
                    typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.Color];
                }
            }
            else if (propertyType != null && propertyType.IsSubclassOf(typeof(UnityObject)))
            {
                var target = GetTarget(property);
                if (data.variableDrawer == null) { data.variableDrawer = new VariableFieldDrawer(); }
                if (data.variableDrawer.TryDrawField(position, property.FindPropertyRelative("m_variable"), label, out position))
                {
                    if (data.updateType)
                    {
                        typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.ObjectReference];
                    }
                }
                else
                {
                    if (target != null && target.OnObjectChanged != null)
                    {
                        innerProperty = property.FindPropertyRelative("m_objectValue");
                        var objValue = innerProperty.objectReferenceValue ? innerProperty.objectReferenceValue : target.UnityObjectRef;
                        var newObj = EditorGUI.ObjectField(position, label, objValue, propertyType, true);
                        if (newObj != objValue || data.updateType)
                        {
                            var path = innerProperty.propertyPath;
                            var prevObj = objValue;
                            //innerProperty.objectReferenceValue = newObj;
                            target.OnObjectChanged(path, newObj, prevObj);
                        }
                        if (data.updateType)
                        {
                            typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.ObjectReference];
                        }
                    }
                    else if (property.serializedObject.context && (propertyType.IsSubclassOf(typeof(GameObject)) || propertyType.IsSubclassOf(typeof(Component))))
                    {
                        innerProperty = property.FindPropertyRelative("m_expObjectValue");
                        innerProperty.exposedReferenceValue = EditorGUI.ObjectField(position, label, innerProperty.exposedReferenceValue, propertyType, true);
                        if (data.updateType)
                        {
                            typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.ExposedObjectReference];
                        }
                    }
                    else
                    {
                        innerProperty = property.FindPropertyRelative("m_objectValue");
                        innerProperty.objectReferenceValue = EditorGUI.ObjectField(position, label, innerProperty.objectReferenceValue, propertyType, true);
                        if (data.updateType)
                        {
                            typeProperty.enumValueIndex = data.enumIndices[GenericValue.ValueType.ObjectReference];
                        }
                    }
                }
            }
            //else if(propertyType.IsSerializable)
            //{
            //    innerProperty = property.FindPropertyRelative("m_stringValue");
            //    if (m_genericHandler == null)
            //    {
            //        m_genericHandler = new GenericHandler(m_sourceProperty, innerProperty.stringValue);
            //        EditorApplication.update -= UpdateDrawerState;
            //        EditorApplication.update += UpdateDrawerState;
            //    }
            //    m_genericHandler.Draw(position, label);
            //    if (m_genericHandler.HasChanged)
            //    {
            //        innerProperty.stringValue = m_genericHandler.SerializedValue();
            //        if (property.serializedObject.targetObject is ProcedureObject)
            //        {
            //            (property.serializedObject.targetObject as ProcedureObject).Modified();
            //        }
            //    }

            //    m_disposeDeadline = EditorApplication.timeSinceStartup + 3;

            //    if (data.updateType)
            //    {
            //        typeProperty.enumValueIndex = data.m_enumIndices[GenericValue.ValueType.Generic];
            //    }

            //    //innerProperty = property.FindPropertyRelative("m_intValue");
            //    //innerProperty.intValue = EditorGUI.IntField(position, label, innerProperty.intValue);
            //    //if (data.updateType)
            //    //{
            //    //    typeProperty.enumValueIndex = data.m_enumIndices[GenericValue.ValueType.Generic];
            //    //}
            //}
            else
            {
                bool wasRichText = EditorStyles.label.richText;
                EditorStyles.label.richText = true;
                EditorGUI.LabelField(position, label.text, $"Unable to display <color=#ffa500ff>{label.text}</color>");
                EditorStyles.label.richText = wasRichText;

                //EditorGUI.EndChangeCheck();
                EditorGUIUtility.wideMode = wasWide;
                return;
            }

            //EditorGUI.EndChangeCheck();
            //if (EditorGUI.EndChangeCheck() || property.serializedObject.ApplyModifiedProperties())
            //{
            //    if (property.serializedObject.targetObject is ProcedureObject)
            //    {
            //        (property.serializedObject.targetObject as ProcedureObject).Modified();
            //    }
            //}

            EditorGUIUtility.wideMode = wasWide;
        }

        private ParameterData GetParameterData(SerializedProperty property)
        {
            if (!m_params.TryGetValue(property.propertyPath, out ParameterData data))
            {
                data = new ParameterData();
                m_params[property.propertyPath] = data;
            }
            return data;
        }

        private void ResetValues(SerializedProperty property)
        {
            var data = GetParameterData(property);

            data.enumValues = null;
            property.FindPropertyRelative("m_objectValue").objectReferenceValue = null;
            //property.FindPropertyRelative("m_typename").stringValue = null;
            //property.FindPropertyRelative("m_stringValue").stringValue = null;

            //SetGradient(property, null);
        }

        private void Initialize(SerializedProperty property)
        {
            var data = GetParameterData(property);
            data.targetGetter = property.GetValueGetter();
            data.initialized = true;
            data.updateType = true;

            int index = 0;
            data.enumIndices = new Dictionary<GenericValue.ValueType, int>();
            foreach (GenericValue.ValueType value in Enum.GetValues(typeof(GenericValue.ValueType)))
            {
                data.enumIndices[value] = index++;
            }

            //data.m_parameterType = GetTarget(property)?.Type;
        }

        private Property GetPropertyObject(SerializedProperty property, string source)
        {
            var getter = property.serializedObject.targetObject.GetType().FieldGetNoThrow(source) ??
                                         property.serializedObject.targetObject.GetType().PropertyGetNoThrow(source);
            
            return getter?.Invoke(property.serializedObject.targetObject) as Property;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var data = GetParameterData(property);
            
            if (!data.initialized)
            {
                data.initialized = true;
                Initialize(property);

                return data.parameterType == null ? base.GetPropertyHeight(property, label) : GetHeightByType(property, label);
            }
            
            return data.parameterType == null ? base.GetPropertyHeight(property, label) 
                 : GetHeightByType(property, label);
        }

        private float GetHeightByType(SerializedProperty property, GUIContent label)
        {
            var data = GetParameterData(property);

            var propertyType = data.parameterType;

            if (propertyType == typeof(string))
            {
                return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_stringValue"), label, true);
            }
            else if (propertyType == typeof(Gradient))
            {
                return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_gradientValue"), label, true);
            }
            else if (propertyType == typeof(AnimationCurve))
            {
                return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_animationCurveValue"), label, true);
            }
            return base.GetPropertyHeight(property, label);
        }
    }
}
