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
    [CustomPropertyDrawer(typeof(GenericValue))]
    public class GenericValueDrawer : PropertyDrawer
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

        private bool m_initialized;
        private Property m_sourceProperty;
        private string m_sourcePropertyName;
        private string m_previousPath;
        private Type m_previousType;
        private UnityObject m_previousTarget;
        private bool m_getNameFromProperty;
        private string m_propertyNameLabel;
        private Dictionary<GenericValue.ValueType, int> m_enumIndices;

        private GenericHandler m_genericHandler;
        private ShadowPropertyHandler m_shadow;

        private GenericValue m_target;
        private Func<object, object> m_gradientGetter;
        private Action<object, object> m_gradientSetter;

        private VariableFieldDrawer m_variableDrawer;

        private string[] m_enumValues;

        private double m_disposeDeadline;

        private ShadowPropertyHandler Shadow
        {
            get
            {
                if (m_shadow == null)
                {
                    m_shadow = new ShadowPropertyHandler();
                }
                if (m_shadow.property == null && m_shadow.isEligible)
                {
                    if (m_sourceProperty != null && m_sourceProperty.PropertyType != null)
                    {
                        m_shadow.TryRetrieveProperty(m_sourceProperty);
                    }
                }
                return m_shadow;
            }
        }

        public GenericValue GetTarget(SerializedProperty property)
        {
            if (m_target == null)
            {
                m_target = fieldInfo.GetValue(property.serializedObject.targetObject) as GenericValue;
            }
            return m_target;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return m_genericHandler == null && base.CanCacheInspectorGUI(property);
        }



        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_initialized)
            {
                m_initialized = true;
                m_propertyNameLabel = label.text;
                Initialize(property);
            }

            if (m_sourceProperty == null)
            {
                m_previousTarget = null;
                var color = GUI.color;
                GUI.color = Color.red;
                EditorGUI.LabelField(position, label.text, $"INVALID DATA");
                GUI.color = color;
                return;
            }
            else if (m_sourceProperty.PropertyType == null || !m_sourceProperty.Target)
            {
                m_previousTarget = null;
                var style = EditorStyles.centeredGreyMiniLabel;
                bool wasRichText = style.richText;
                style.richText = true;
                EditorGUI.LabelField(position, label.text, $"Nothing selected for <color=#ffa500ff>{m_sourcePropertyName}</color>", style);
                style.richText = wasRichText;
                return;
            }
            else if (!m_previousTarget)
            {
                m_previousTarget = m_sourceProperty.Target;
                Shadow.TryRetrieveProperty(m_sourceProperty);
            }

            SerializedProperty innerProperty = null;
            var typeProperty = property.FindPropertyRelative("m_valueType");
            var propertyType = m_sourceProperty.PropertyType;

            bool updateType = false;

            if (m_previousPath != m_sourceProperty.Path || m_previousType != propertyType)
            {
                ResetValues(property);
                property.FindPropertyRelative("m_typename").stringValue = propertyType.AssemblyQualifiedName;
                updateType = true;
                m_previousPath = m_sourceProperty.Path;
                m_previousType = propertyType;

                Shadow.TryRetrieveProperty(m_sourceProperty);

                m_propertyNameLabel = m_getNameFromProperty ? EditorTools.NicifyName(m_previousPath.Substring(m_previousPath.LastIndexOf('.') + 1)) : label.text;
            }

            if (!string.IsNullOrEmpty(m_propertyNameLabel))
            {
                label.text = m_propertyNameLabel;
            }

            var wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;

            EditorGUI.BeginChangeCheck();

            bool isSpecialDrawer = false;

            var shadowProperty = Shadow.property;
            if (shadowProperty != null && shadowProperty.propertyType != SerializedPropertyType.Generic)
            {
                isSpecialDrawer = true;
                shadowProperty.serializedObject.Update();

                if (m_shadow.requireSync)
                {
                    m_shadow.requireSync = false;
                    switch (shadowProperty.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            shadowProperty.intValue = property.FindPropertyRelative("m_intValue").intValue;
                            break;
                        case SerializedPropertyType.Boolean:
                            shadowProperty.boolValue = property.FindPropertyRelative("m_boolValue").boolValue;
                            break;
                        case SerializedPropertyType.Float:
                            shadowProperty.floatValue = property.FindPropertyRelative("m_floatValue").floatValue;
                            break;
                        case SerializedPropertyType.String:
                            shadowProperty.stringValue = property.FindPropertyRelative("m_stringValue").stringValue;
                            break;
                        case SerializedPropertyType.ExposedReference:
                            shadowProperty.exposedReferenceValue = property.FindPropertyRelative("m_expObjectValue").exposedReferenceValue;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            shadowProperty.objectReferenceValue = property.FindPropertyRelative("m_objectValue").objectReferenceValue;
                            break;
                        //case SerializedPropertyType.LayerMask:
                        //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                        //    break;
                        case SerializedPropertyType.Enum:
                            shadowProperty.enumValueIndex = property.FindPropertyRelative("m_intValue").intValue;
                            break;
                        case SerializedPropertyType.Color:
                            shadowProperty.colorValue = property.FindPropertyRelative("m_vectorValue").vector4Value;
                            break;
                        case SerializedPropertyType.Vector2:
                            shadowProperty.vector2Value = property.FindPropertyRelative("m_vectorValue").vector4Value;
                            break;
                        case SerializedPropertyType.Vector3:
                            shadowProperty.vector3Value = property.FindPropertyRelative("m_vectorValue").vector4Value;
                            break;
                        case SerializedPropertyType.Quaternion:
                        case SerializedPropertyType.Vector4:
                            shadowProperty.vector4Value = property.FindPropertyRelative("m_vectorValue").vector4Value;
                            break;
                        //case SerializedPropertyType.Rect:
                        //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                        //    break;
                        case SerializedPropertyType.Character:
                            shadowProperty.stringValue = property.FindPropertyRelative("m_stringValue").stringValue;
                            break;
                        case SerializedPropertyType.AnimationCurve:
                            shadowProperty.animationCurveValue = property.FindPropertyRelative("m_animationCurveValue").animationCurveValue;
                            break;
                            //case SerializedPropertyType.Bounds:
                            //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                            //    break;
                            //case SerializedPropertyType.Vector2Int:
                            //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                            //    break;
                            //case SerializedPropertyType.Vector3Int:
                            //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                            //    break;
                            //case SerializedPropertyType.RectInt:
                            //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                            //    break;
                            //case SerializedPropertyType.BoundsInt:
                            //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                            //    break;
                    }
                }

                EditorGUI.PropertyField(position, shadowProperty);

                switch (shadowProperty.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.FindPropertyRelative("m_intValue").intValue = shadowProperty.intValue;
                        break;
                    case SerializedPropertyType.Boolean:
                        property.FindPropertyRelative("m_boolValue").boolValue = shadowProperty.boolValue;
                        break;
                    case SerializedPropertyType.Float:
                        property.FindPropertyRelative("m_floatValue").floatValue = shadowProperty.floatValue;
                        break;
                    case SerializedPropertyType.String:
                        property.FindPropertyRelative("m_stringValue").stringValue = shadowProperty.stringValue;
                        break;
                    case SerializedPropertyType.ExposedReference:
                        property.FindPropertyRelative("m_expObjectValue").exposedReferenceValue = shadowProperty.exposedReferenceValue;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        property.FindPropertyRelative("m_objectValue").objectReferenceValue = shadowProperty.objectReferenceValue;
                        break;
                    //case SerializedPropertyType.LayerMask:
                    //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                    //    break;
                    case SerializedPropertyType.Enum:
                        property.FindPropertyRelative("m_intValue").intValue = shadowProperty.enumValueIndex;
                        break;
                    case SerializedPropertyType.Color:
                        property.FindPropertyRelative("m_vectorValue").vector4Value = shadowProperty.colorValue;
                        break;
                    case SerializedPropertyType.Vector2:
                        property.FindPropertyRelative("m_vectorValue").vector4Value = shadowProperty.vector2Value;
                        break;
                    case SerializedPropertyType.Vector3:
                        property.FindPropertyRelative("m_vectorValue").vector4Value = shadowProperty.vector3Value;
                        break;
                    case SerializedPropertyType.Quaternion:
                    case SerializedPropertyType.Vector4:
                        property.FindPropertyRelative("m_vectorValue").vector4Value = shadowProperty.vector4Value;
                        break;
                    //case SerializedPropertyType.Rect:
                    //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                    //    break;
                    case SerializedPropertyType.Character:
                        property.FindPropertyRelative("m_stringValue").stringValue = shadowProperty.stringValue;
                        break;
                    case SerializedPropertyType.AnimationCurve:
                        property.FindPropertyRelative("m_animationCurveValue").animationCurveValue = shadowProperty.animationCurveValue;
                        break;
                    //case SerializedPropertyType.Bounds:
                    //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                    //    break;
                    //case SerializedPropertyType.Vector2Int:
                    //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                    //    break;
                    //case SerializedPropertyType.Vector3Int:
                    //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                    //    break;
                    //case SerializedPropertyType.RectInt:
                    //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                    //    break;
                    //case SerializedPropertyType.BoundsInt:
                    //    property.FindPropertyRelative("m_vectorValue").vector4Value = m_shadowProperty.colorValue;
                    //    break;
                    default:
                        isSpecialDrawer = false;
                        break;
                }

                shadowProperty.serializedObject.ApplyModifiedProperties();
            }

            if (isSpecialDrawer)
            {
                m_shadow.Refresh();
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[(GenericValue.ValueType)shadowProperty.propertyType];
                }
            }
            else if (propertyType == typeof(bool))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_boolValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Boolean];
                }
            }
            else if (propertyType == typeof(int))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_intValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Integer];
                }
            }
            else if (propertyType == typeof(byte))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_byteValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Byte];
                }
            }
            else if (propertyType == typeof(short))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_shortValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Short];
                }
            }
            else if (propertyType == typeof(float) || propertyType == typeof(double))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_floatValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Float];
                }
            }
            else if (propertyType == typeof(string))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_stringValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.String];
                }
            }
            else if (propertyType.IsEnum)
            {
                innerProperty = property.FindPropertyRelative("m_intValue");
                if (m_enumValues == null)
                {
                    m_enumValues = Enum.GetNames(propertyType);
                }
                innerProperty.intValue = EditorGUI.Popup(position, innerProperty.intValue, m_enumValues);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Enum];
                }
            }
            else if (propertyType == typeof(Gradient))
            {
                SetGradient(property, EditorGUI.GradientField(position, label, GetGradient(property)));
                //EditorGUI.PropertyField(position, property.FindPropertyRelative("m_gradientValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Gradient];
                }
            }
            else if (propertyType == typeof(AnimationCurve))
            {
                EditorGUI.PropertyField(position, property.FindPropertyRelative("m_animationCurveValue"), label, true);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.AnimationCurve];
                }
            }
            else if (propertyType == typeof(Vector2))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                innerProperty.vector4Value = EditorGUI.Vector2Field(position, label, innerProperty.vector4Value);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Vector2];
                }
            }
            else if (propertyType == typeof(Vector3))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                if (m_variableDrawer == null) { m_variableDrawer = new VariableFieldDrawer(); }
                if (!m_variableDrawer.TryDrawField(position, property.FindPropertyRelative("m_variable"), label, out position))
                {
                    innerProperty.vector4Value = EditorGUI.Vector3Field(position, label, innerProperty.vector4Value);
                }
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Vector3];
                }
            }
            else if (propertyType == typeof(Vector4) || propertyType == typeof(Quaternion))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                innerProperty.vector4Value = EditorGUI.Vector4Field(position, label, innerProperty.vector4Value);
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Vector4];
                }
            }
            else if (propertyType == typeof(Color))
            {
                innerProperty = property.FindPropertyRelative("m_vectorValue");
                if (m_variableDrawer == null) { m_variableDrawer = new VariableFieldDrawer(); }
                if (!m_variableDrawer.TryDrawField(position, property.FindPropertyRelative("m_variable"), label, out position))
                {
                    innerProperty.vector4Value = EditorGUI.ColorField(position, label, innerProperty.vector4Value);
                }
                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Color];
                }
            }
            else if (propertyType.IsSubclassOf(typeof(UnityObject)))
            {
                var target = GetTarget(property);
                if (m_variableDrawer == null) { m_variableDrawer = new VariableFieldDrawer(); }
                if (m_variableDrawer.TryDrawField(position, property.FindPropertyRelative("m_variable"), label, out position))
                {
                    if (updateType)
                    {
                        typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.ObjectReference];
                    }
                }
                else
                {
                    if (target != null && target.OnObjectChanged != null)
                    {
                        innerProperty = property.FindPropertyRelative("m_objectValue");
                        //var newObj = EditorGUI.ObjectField(position, label, innerProperty.objectReferenceValue, propertyType, true);
                        var newObj = WeavrGUI.DraggableObjectField(this, position, label, innerProperty.objectReferenceValue, propertyType, true);
                        if (newObj != innerProperty.objectReferenceValue)
                        {
                            var path = innerProperty.propertyPath;
                            var prevObj = innerProperty.objectReferenceValue;
                            innerProperty.objectReferenceValue = newObj;
                            target.OnObjectChanged(path, newObj, prevObj);
                        }
                        if (updateType)
                        {
                            typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.ObjectReference];
                        }
                    }
                    else if (property.serializedObject.context && (propertyType.IsSubclassOf(typeof(GameObject)) || propertyType.IsSubclassOf(typeof(Component))))
                    {
                        innerProperty = property.FindPropertyRelative("m_expObjectValue");
                        //innerProperty.exposedReferenceValue = EditorGUI.ObjectField(position, label, innerProperty.exposedReferenceValue, propertyType, true);
                        innerProperty.exposedReferenceValue = WeavrGUI.DraggableObjectField(this, position, label, innerProperty.exposedReferenceValue, propertyType, true);
                        if (updateType)
                        {
                            typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.ExposedObjectReference];
                        }
                    }
                    else
                    {
                        innerProperty = property.FindPropertyRelative("m_objectValue");
                        //innerProperty.objectReferenceValue = EditorGUI.ObjectField(position, label, innerProperty.objectReferenceValue, propertyType, true);
                        innerProperty.objectReferenceValue = WeavrGUI.DraggableObjectField(this, position, label, innerProperty.objectReferenceValue, propertyType, true);
                        if (updateType)
                        {
                            typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.ObjectReference];
                        }
                    }
                }
            }
            else if (propertyType.IsSerializable)
            {
                innerProperty = property.FindPropertyRelative("m_stringValue");
                if (m_genericHandler == null)
                {
                    m_genericHandler = new GenericHandler(m_sourceProperty, innerProperty.stringValue);
                    EditorApplication.update -= UpdateDrawerState;
                    EditorApplication.update += UpdateDrawerState;
                }
                m_genericHandler.Draw(position, label);
                if (m_genericHandler.HasChanged)
                {
                    innerProperty.stringValue = m_genericHandler.SerializedValue();
                    if (property.serializedObject.targetObject is ProcedureObject)
                    {
                        (property.serializedObject.targetObject as ProcedureObject).Modified();
                    }
                }

                m_disposeDeadline = EditorApplication.timeSinceStartup + 3;

                if (updateType)
                {
                    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Generic];
                }

                //innerProperty = property.FindPropertyRelative("m_intValue");
                //innerProperty.intValue = EditorGUI.IntField(position, label, innerProperty.intValue);
                //if (updateType)
                //{
                //    typeProperty.enumValueIndex = m_enumIndices[GenericValue.ValueType.Generic];
                //}
            }
            else
            {
                bool wasRichText = EditorStyles.label.richText;
                EditorStyles.label.richText = true;
                EditorGUI.LabelField(position, label.text, $"Unable to display <color=#ffa500ff>{label.text}</color>");
                EditorStyles.label.richText = wasRichText;

                EditorGUI.EndChangeCheck();
                EditorGUIUtility.wideMode = wasWide;
                return;
            }

            if (EditorGUI.EndChangeCheck() || property.serializedObject.ApplyModifiedProperties())
            {
                if (property.serializedObject.targetObject is ProcedureObject)
                {
                    (property.serializedObject.targetObject as ProcedureObject).Modified();
                }
            }

            EditorGUIUtility.wideMode = wasWide;
        }

        private void ResetValues(SerializedProperty property)
        {
            EditorApplication.update -= UpdateDrawerState;
            m_enumValues = null;
            m_genericHandler?.Dispose();
            m_genericHandler = null;
            property.FindPropertyRelative("m_objectValue").objectReferenceValue = null;
            property.FindPropertyRelative("m_typename").stringValue = null;
            property.FindPropertyRelative("m_stringValue").stringValue = null;
            property.FindPropertyRelative("m_gradientValue").stringValue = null;

            //SetGradient(property, null);
        }

        private void SetGradient(SerializedProperty property, Gradient value)
        {
            var target = GetTarget(property);
            if (target != null && m_gradientSetter != null)
            {
                m_gradientSetter(target, value);
            }
        }

        private Gradient GetGradient(SerializedProperty property)
        {
            var target = GetTarget(property);
            return target != null && m_gradientGetter != null ? m_gradientGetter(target) as Gradient : null;
        }

        ~GenericValueDrawer()
        {
            m_shadow?.Dispose();
            m_shadow = null;
            m_genericHandler?.Dispose();
            m_genericHandler = null;
        }

        private void Initialize(SerializedProperty property)
        {
            EditorApplication.update -= UpdateDrawerState;
            EditorApplication.update += UpdateDrawerState;

            int index = 0;
            m_enumIndices = new Dictionary<GenericValue.ValueType, int>();
            foreach (GenericValue.ValueType value in Enum.GetValues(typeof(GenericValue.ValueType)))
            {
                m_enumIndices[value] = index++;
            }

            // Get the special attribute first
            var typeFromAttribute = fieldInfo.GetAttribute<GenericValueTypeFromAttribute>();
            if (typeFromAttribute != null && !string.IsNullOrEmpty(typeFromAttribute.TypeSource))
            {
                m_getNameFromProperty = typeFromAttribute.GetNameFromProperty;
                m_sourceProperty = GetPropertyObject(property, typeFromAttribute.TypeSource);
                m_sourcePropertyName = EditorTools.NicifyName(typeFromAttribute.TypeSource);
            }

            // Then find properties path
            if (m_sourceProperty == null)
            {
                foreach (var field in property.serializedObject.targetObject.GetType().GetFields())
                {
                    if (field.FieldType == typeof(Property))
                    {
                        m_sourceProperty = field.GetValue(property.serializedObject.targetObject) as Property;
                        m_sourcePropertyName = EditorTools.NicifyName(field.Name);
                        break;
                    }
                }
            }

            if (m_sourceProperty != null)
            {
                m_previousPath = m_sourceProperty.Path;
                m_previousType = m_sourceProperty.PropertyType;

                if (!string.IsNullOrEmpty(m_previousPath))
                {
                    m_propertyNameLabel = m_getNameFromProperty ? EditorTools.NicifyName(m_previousPath.Substring(m_previousPath.LastIndexOf('.') + 1)) : string.Empty;
                }

                //m_sourceProperty.Initialize();
                //Shadow.TryRetrieveProperty(m_sourceProperty);
            }

            m_gradientGetter = typeof(GenericValue).PropertyGet("GradientValue");
            m_gradientSetter = typeof(GenericValue).PropertySet("GradientValue");
        }

        private void UpdateDrawerState()
        {
            if (EditorApplication.timeSinceStartup > m_disposeDeadline)
            {
                m_genericHandler?.Dispose();
                m_genericHandler = null;
                EditorApplication.update -= UpdateDrawerState;
            }
        }

        private Property GetPropertyObject(SerializedProperty property, string source)
        {
            var getter = property.serializedObject.targetObject.GetType().FieldGetNoThrow(source) ??
                                         property.serializedObject.targetObject.GetType().PropertyGetNoThrow(source);

            return getter?.Invoke(property.serializedObject.targetObject) as Property;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!m_initialized)
            {
                m_initialized = true;
                Initialize(property);

                return m_sourceProperty == null || m_sourceProperty.PropertyType == null ?
                            base.GetPropertyHeight(property, label) : GetHeightByType(property, label);
            }

            return m_sourceProperty == null || m_sourceProperty.PropertyType == null ? base.GetPropertyHeight(property, label)
                 : Shadow.property != null ? EditorGUI.GetPropertyHeight(m_shadow.property, true) : GetHeightByType(property, label);
        }

        private float GetHeightByType(SerializedProperty property, GUIContent label)
        {
            var propertyType = m_sourceProperty.PropertyType;

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
            return m_genericHandler != null ? m_genericHandler.GetHeight(label) : base.GetPropertyHeight(property, label);
        }

        private class ShadowPropertyHandler : IDisposable
        {
            private SerializedProperty m_serializedProperty;
            private bool m_isEligible;

            public bool requireSync;

            private GameObject m_gameObject;
            private double m_deadline;

            public SerializedProperty property => m_serializedProperty;
            public bool isEligible => m_isEligible;

            public ShadowPropertyHandler()
            {
                Refresh();
                EditorApplication.update -= UpdateDrawerState;
                EditorApplication.update += UpdateDrawerState;
            }

            public void DelayRetrieval()
            {
                m_isEligible = true;
            }

            public void Refresh()
            {
                m_deadline = EditorApplication.timeSinceStartup + 2;
            }

            public bool TryRetrieveProperty(Property sourceProperty)
            {
                if (sourceProperty.MemberInfo is FieldInfo fieldInfo && (fieldInfo.IsPublic || fieldInfo.GetAttribute<SerializeField>() != null))
                {
                    var attributes = fieldInfo.GetCustomAttributes<PropertyAttribute>()
                                              .Select(a => a.GetType())
                                              .Count(t => !s_skipAttributeTypes.Any(st => st.IsAssignableFrom(t)));

                    if (attributes > 0)
                    {
                        var component = sourceProperty.TryExtractComponent();
                        if (component)
                        {
                            var newGO = m_serializedProperty != null
                                                ? m_serializedProperty.serializedObject.targetObject.GetGameObject()
                                                : new GameObject(k_GameObjectName).AddComponent(component.GetType()).gameObject;

                            newGO.hideFlags = k_HiddenAndEditable;
                            var shadowComponent = newGO.GetComponent(component.GetType());
                            if (newGO != m_gameObject)
                            {
                                DisposeProperty();
                            }

                            // Copy complete components
                            component.CopyTo(shadowComponent);

                            m_gameObject = newGO;
                            m_serializedProperty = new SerializedObject(shadowComponent).FindProperty(sourceProperty.GetClearPath());

                            if (m_serializedProperty != null)
                            {
                                Refresh();

                                EditorApplication.update -= UpdateDrawerState;
                                EditorApplication.update += UpdateDrawerState;

                                requireSync = true;
                                m_isEligible = true;
                                return true;
                            }
                        }
                    }
                }

                DisposeProperty();
                m_isEligible = false;
                return false;
            }

            private void UpdateDrawerState()
            {
                if (EditorApplication.timeSinceStartup > m_deadline)
                {
                    DisposeProperty();
                    EditorApplication.update -= UpdateDrawerState;
                }
            }

            private void DisposeProperty()
            {
                if (m_serializedProperty != null)
                {
                    var go = m_gameObject ?? m_serializedProperty.serializedObject.targetObject.GetGameObject();
                    UnityObject.DestroyImmediate(m_serializedProperty.serializedObject.targetObject.GetGameObject());
                    m_serializedProperty.serializedObject.Dispose();
                    m_serializedProperty?.Dispose();
                    m_serializedProperty = null;
                }
                requireSync = false;
                m_gameObject = null;
            }

            public void Dispose()
            {
                DisposeProperty();
            }
        }

        private class GenericHandler : IDisposable
        {
            private string m_memberName;
            private SerializedProperty m_property;
            private Func<string> m_serializationFunction;

            public bool HasChanged { get; private set; }

            public GenericHandler(Property property, string serialization)
            {
                m_memberName = EditorTools.NicifyName(property.MemberInfo?.Name);
                if (!property.Target)
                {
                    return;
                }

                var closestObject = property.TryExtractComponent() ?? property.Target;
                var closestPropertyIndex = 0;
                if (!closestObject)
                {
                    return;
                }

                var splits = property.GetClearPath().Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                object lastObject = closestObject;
                for (int i = 0; i < splits.Length; i++)
                {
                    var memberPath = splits[i];
                    var getter = lastObject.GetType().PropertyGetNoThrow(memberPath) ?? lastObject.GetType().FieldGetNoThrow(memberPath);
                    lastObject = getter(lastObject);
                    if (lastObject == null)
                    {
                        closestObject = null;
                        break;
                    }
                    if (lastObject is UnityObject)
                    {
                        closestObject = lastObject as UnityObject;
                        closestPropertyIndex = i + 1;
                    }
                }

                if (!closestObject)
                {
                    return;
                }

                closestObject = UnityObject.Instantiate(closestObject);
                if (!string.IsNullOrEmpty(serialization))
                {
                    var deserializedValue = JsonUtility.FromJson(serialization, property.PropertyType);
                    if (deserializedValue != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = closestPropertyIndex; i < splits.Length; i++)
                        {
                            sb.Append('.').Append(splits[i]);
                        }

                        closestObject.GetType().ValuePathSet(sb.ToString().Substring(1))?.Invoke(closestObject, deserializedValue);
                    }
                }

                lastObject = closestObject;
                SerializedObject serObj = new SerializedObject(closestObject);
                List<Func<object, object>> getters = new List<Func<object, object>>();
                for (int i = closestPropertyIndex; i < splits.Length; i++)
                {
                    var memberPath = splits[i];

                    var memberInfo = DelegateFactory.GetMemberInfo(lastObject.GetType(), memberPath);
                    FieldInfo fieldInfo = memberInfo as FieldInfo;
                    if (memberInfo is PropertyInfo)
                    {
                        fieldInfo = GetSimilarFieldInfo(lastObject.GetType(), (memberInfo as PropertyInfo).PropertyType);
                        lastObject = (memberInfo as PropertyInfo).GetValue(lastObject);
                        getters.Add((memberInfo as PropertyInfo).FastGetter());
                    }
                    else if (memberInfo is FieldInfo)
                    {
                        lastObject = fieldInfo.GetValue(lastObject);
                        getters.Add(fieldInfo.FastGetter());
                    }

                    if (fieldInfo == null)
                    {
                        if (m_property != null)
                        {
                            Dispose();
                        }
                        else
                        {
                            UnityObject.DestroyImmediate(serObj.targetObject);
                            serObj?.Dispose();
                        }
                        break;
                    }

                    m_property = m_property != null ? m_property.FindPropertyRelative(fieldInfo.Name) : serObj.FindProperty(fieldInfo.Name);

                    if (m_property == null)
                    {
                        UnityObject.DestroyImmediate(serObj.targetObject);
                        serObj?.Dispose();
                    }
                }

                if (m_property != null)
                {
                    var objectToHide = closestObject.GetGameObject() ?? closestObject;
                    objectToHide.hideFlags = k_HiddenAndEditable;
                    objectToHide.hideFlags &= ~HideFlags.DontUnloadUnusedAsset;
                    m_serializationFunction = () =>
                    {
                        object result = getters.Count > 0 ? getters[0](closestObject) : string.Empty;
                        for (int i = 1; i < getters.Count; i++)
                        {
                            result = getters[i](result);
                        }
                        return JsonUtility.ToJson(result);
                    };
                }
            }

            private FieldInfo GetSimilarFieldInfo(Type ownerType, Type memberType)
            {
                return ownerType.GetFields().FirstOrDefault(f => f.FieldType.IsAssignableFrom(memberType));
            }

            public string SerializedValue()
            {
                return m_serializationFunction?.Invoke();
            }

            public void Dispose()
            {
                if (m_property != null && m_property.serializedObject != null && m_property.serializedObject.targetObject)
                {
                    var gameObject = m_property.serializedObject.targetObject.GetGameObject();
                    if (gameObject != null)
                    {
                        UnityObject.DestroyImmediate(gameObject);
                    }
                    else
                    {
                        UnityObject.DestroyImmediate(m_property.serializedObject.targetObject);
                    }
                    m_property.serializedObject?.Dispose();
                    m_property?.Dispose();
                    m_property = null;
                }
            }

            public void Draw(Rect position, GUIContent label)
            {
                HasChanged = false;
                if (m_property == null)
                {
                    bool wasRichText = EditorStyles.label.richText;
                    EditorStyles.label.richText = true;
                    EditorGUI.LabelField(position, label.text, $"Unable to display <color=#ffa500ff>{m_memberName}</color>");
                    EditorStyles.label.richText = wasRichText;
                    return;
                }
                m_property.serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, m_property, label, true);
                m_property.serializedObject.ApplyModifiedProperties();
                if (EditorGUI.EndChangeCheck())
                {
                    HasChanged = true;
                }
            }

            public float GetHeight(GUIContent label)
            {
                return m_property != null ? EditorGUI.GetPropertyHeight(m_property, label, true) : EditorGUIUtility.singleLineHeight;
            }
        }
    }
}
