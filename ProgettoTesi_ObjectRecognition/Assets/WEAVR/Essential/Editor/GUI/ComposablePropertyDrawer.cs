using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Editor
{
    public abstract class ComposablePropertyDrawer : PropertyDrawer
    {

        private static readonly string s_PropertyFieldClassName = "unity-property-field";
        private static readonly string s_WrapperClassName = "unity-property-field-wrapper";
        private static readonly string s_LabelClassName = "unity-property-field-label";
        private static readonly string s_InputClassName = "unity-property-field-input";

        private PropertyDrawer m_nextDrawer;
        private bool m_needsInitialization = true;

        private string m_tooltip;
        private PropertyDrawer m_baseDrawer;

        protected PropertyDrawer BaseDrawer => m_baseDrawer;

        protected PropertyDrawer NextDrawer
        {
            get
            {
                if (m_needsInitialization)
                {
                    m_needsInitialization = false;
                    GetNextDrawer();
                }
                return m_nextDrawer;
            }
        }

        private PropertyField m_defaultPropertyField;

        protected GUIContent AddTooltip(GUIContent label)
        {
            if (label != GUIContent.none && string.IsNullOrEmpty(label.tooltip))
            {
                label.tooltip = m_tooltip;
            }
            return label;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var nextDrawer = NextDrawer;
            if (label != GUIContent.none && string.IsNullOrEmpty(label.tooltip))
            {
                label.tooltip = m_tooltip;
            }
            if(m_nextDrawer != null)
            {
                m_nextDrawer.OnGUI(position, property, label);
            }
            else if(m_baseDrawer != null && m_baseDrawer != this)
            {
                m_baseDrawer.OnGUI(position, property, label);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, property.isExpanded);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return NextDrawer != null ? m_nextDrawer.GetPropertyHeight(property, label)
                                      : m_baseDrawer != null ? m_baseDrawer.GetPropertyHeight(property, label)
                                                             : EditorGUI.GetPropertyHeight(property, label, true);
        }
        

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return NextDrawer != null ? m_nextDrawer.CreatePropertyGUI(property) 
                                      : m_baseDrawer != null ? m_baseDrawer.CreatePropertyGUI(property)
                                      : base.CreatePropertyGUI(property);
        }

        private PropertyField GetDefaultPropertyField(SerializedProperty property)
        {
            if(m_defaultPropertyField == null)
            {
                m_defaultPropertyField = new PropertyField(property);
            }
            return m_defaultPropertyField;
        }

        private void GetNextDrawer()
        {
            if (attribute == null) { return; }
            var tooltipAttr = fieldInfo.GetAttribute<TooltipAttribute>();
            if(tooltipAttr != null)
            {
                m_tooltip = tooltipAttr.tooltip;
            }
            if (m_baseDrawer == null && s_baseDrawers.TryGetValue(fieldInfo.FieldType, out Type baseType))
            {
                m_baseDrawer = (PropertyDrawer)Activator.CreateInstance(baseType);
                s_fieldSetter(m_baseDrawer, fieldInfo);
                s_attributeSetter(m_baseDrawer, null);
            }
            var nextAttribute = GetNextAttribute(fieldInfo, attribute);
            Type drawerType = null;
            if(nextAttribute == null
                || !s_attributeDrawers.TryGetValue(nextAttribute.GetType(), out drawerType)) { return; }
            
            m_nextDrawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
            s_fieldSetter(m_nextDrawer, fieldInfo);
            s_attributeSetter(m_nextDrawer, nextAttribute);
            //if(s_baseDrawers.TryGetValue(attribute.GetType(), out Type baseDrawerType) && baseDrawerType != null)
            //{
            //    m_baseDrawer = (PropertyDrawer)Activator.CreateInstance(baseDrawerType);
            //    s_fieldSetter(m_baseDrawer, fieldInfo);
            //    s_attributeSetter(m_baseDrawer, attribute);
            //}
        }

        private static Dictionary<Type, Type> s_attributeDrawers;
        private static Dictionary<Type, Type> s_baseDrawers;
        private static Action<object, FieldInfo> s_fieldSetter;
        private static Action<object, PropertyAttribute> s_attributeSetter;
        private static Func<object, object> s_typeGetter;

        [InitializeOnLoadMethod]
        private static void InitializeStaticDictionary()
        {
            Type propertyDrawerType = typeof(PropertyDrawer);
            s_fieldSetter = propertyDrawerType.GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue;
            s_attributeSetter = propertyDrawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue;
            s_typeGetter = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue;



            s_attributeDrawers = new Dictionary<Type, Type>();
            s_baseDrawers = new Dictionary<Type, Type>();
            foreach(var type in EditorTools.GetAllAssemblyTypes())
            {
                if (type.IsSubclassOf(propertyDrawerType))
                {
                    var customEditorAttributeList = type.GetCustomAttributes<CustomPropertyDrawer>();
                    foreach (var customEditorAttribute in customEditorAttributeList)
                    {
                        if (customEditorAttribute != null)
                        {
                            var attributeType = s_typeGetter(customEditorAttribute) as Type;
                            if (attributeType != null && attributeType.IsSubclassOf(typeof(PropertyAttribute))
                                && attributeType != typeof(TooltipAttribute) && attributeType != typeof(ContextMenuItemAttribute))
                            {
                                s_attributeDrawers[attributeType] = type;
                            }
                            else if (attributeType != null && attributeType.Namespace != null && attributeType.Namespace.StartsWith("TXT.WEAVR"))
                            {
                                s_baseDrawers[attributeType] = type;
                            }
                        }
                    }
                }
            }

            // Run again for special base drawers 
            var baseDrawers = new Dictionary<Type, Type>(s_baseDrawers);
            foreach(var key in baseDrawers.Keys.OrderBy(k => k.GetDepth()).ToArray())
            {
                Type drawerType = baseDrawers[key];
                foreach(var child in key.GetAllSubclasses())
                {
                    s_baseDrawers[child] = drawerType;
                }
                s_baseDrawers[key] = drawerType;
            }
        }

        private static PropertyAttribute GetNextAttribute(FieldInfo fieldInfo, PropertyAttribute attribute)
        {
            var attributes = fieldInfo.GetCustomAttributes<PropertyAttribute>(true);
            foreach (var attr in attributes)
            {
                if(attr is TooltipAttribute || attr is ContextMenuItemAttribute)
                {
                    continue;
                }
                else if(attr.GetType() != attribute.GetType() && attr.order > attribute.order)
                {
                    return attr;
                }
            }

            bool found = false;
            foreach (var attr in attributes)
            {
                if (attr is TooltipAttribute || attr is ContextMenuItemAttribute)
                {
                    continue;
                }
                else if (!found && attr.GetType() == attribute.GetType())
                {
                    found = true;
                }
                else if (found && attr.GetType() != attribute.GetType())
                {
                    return attr;
                }
            }
            return null;
        }

        public static Action<PropertyDrawer, T> FastSetter<T>(FieldInfo fieldInfo)
        {
            var sourceParam = Expression.Parameter(typeof(PropertyDrawer));
            var valueParam = Expression.Parameter(typeof(T));
            var convertedValueExpr = Expression.Convert(valueParam, fieldInfo.FieldType);
            Expression returnExpression = Expression.Assign(Expression.Field(sourceParam, fieldInfo), valueParam);
            var lambda = Expression.Lambda(typeof(Action<PropertyDrawer, T>),
                returnExpression, sourceParam, valueParam);
            return (Action<PropertyDrawer, T>)lambda.Compile();
        }

        static Action<S, T> FastSetter<S, T>(FieldInfo field)
        {
            string methodName = field.ReflectedType.FullName + ".set_" + field.Name;
            DynamicMethod setterMethod = new DynamicMethod(methodName, null, new Type[2] { typeof(S), typeof(T) }, true);
            ILGenerator gen = setterMethod.GetILGenerator();
            if (field.IsStatic)
            {
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, field);
            }
            gen.Emit(OpCodes.Ret);
            return (Action<S, T>)setterMethod.CreateDelegate(typeof(Action<S, T>));
        }
    }
}