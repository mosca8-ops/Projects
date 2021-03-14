using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

using DrawerPair = System.Collections.Generic.KeyValuePair
                    <System.Type, System.Type>;

namespace TXT.WEAVR
{
    [CustomPropertyDrawer(typeof(AnimatedValue), true)]
    public class AnimatedValueDrawer : PropertyDrawer
    {
        protected static GUIContent s_animateContent = new GUIContent(string.Empty, "Animate this property");
        protected static GUIContent s_overrideContent = new GUIContent(string.Empty, "Override");
        protected static GUIContent s_reverseContent = new GUIContent(string.Empty, "Reverse this value on exit");

        private static List<DrawerPair> s_drawerPairs;
        private static List<DrawerPair> DrawerPairs
        {
            get
            {
                if(s_drawerPairs == null)
                {
                    s_drawerPairs = new List<DrawerPair>();
                    var allAnimatedDrawers = EditorTools.GetAllSubclasses(typeof(AnimatedValueDrawer)).Reverse();
                    var typeGetter = typeof(CustomPropertyDrawer).FieldGet("m_Type");
                    foreach(var drawer in allAnimatedDrawers)
                    {
                        var propertyDrawerAttribute = drawer.GetAttribute<CustomPropertyDrawer>();
                        if (propertyDrawerAttribute != null)
                        {
                            s_drawerPairs.Add(new DrawerPair(typeGetter(propertyDrawerAttribute) as Type, drawer));
                        }
                    }
                }
                return s_drawerPairs;
            }
        }

        public static AnimatedValueDrawer GetDrawer(Type animatedType)
        {
            if (!typeof(AnimatedValue).IsAssignableFrom(animatedType))
            {
                return null;
            }
            foreach(var drawerPair in DrawerPairs)
            {
                if (drawerPair.Key.IsAssignableFrom(animatedType))
                {
                    return Activator.CreateInstance(drawerPair.Value) as AnimatedValueDrawer;
                }
            }
            return null;
        }

        protected class Styles : BaseStyles
        {
            public GUIStyle animateToggle;
            public GUIStyle overrideToggle;
            public GUIStyle reverseToggle;
            public GUIStyle controlsStyle;
            public GUIStyle label;

            public Rect animateToggleRect;
            public Rect overrideToggleRect;
            public Rect reverseToggleRect;

            protected override void InitializeStyles(bool isProSkin)
            {
                animateToggle = WeavrStyles.ControlsSkin.FindStyle("animatedValue_animateToggle");
                overrideToggle = WeavrStyles.ControlsSkin.FindStyle("animatedValue_overrideToggle");
                reverseToggle = WeavrStyles.ControlsSkin.FindStyle("animatedValue_reverseToggle");
                controlsStyle = WeavrStyles.ControlsSkin.FindStyle("animatedValue_controlsRect");
                label = WeavrStyles.ControlsSkin.FindStyle("animatedValue_label");

                animateToggleRect = new Rect(0, 0,
                                      s_styles.animateToggle.fixedWidth,
                                      s_styles.animateToggle.fixedHeight);
                overrideToggleRect = new Rect(0, 0,
                                      s_styles.overrideToggle.fixedWidth,
                                      s_styles.overrideToggle.fixedHeight);
                reverseToggleRect = new Rect(0, 0,
                                      s_styles.reverseToggle.fixedWidth,
                                      s_styles.reverseToggle.fixedHeight);

            }
        }

        protected static Styles s_styles = new Styles();

        protected float m_targetHeight;
        private bool m_initialized;
        private bool m_isOptional;
        private bool m_drawReversibleToggle;
        private string m_sourceCurveName;
        private string m_sourceDurationName;

        private string m_tooltip;

        private Func<bool> m_canBeAnimatedQuery;
        private Func<float> m_getDuration;
        private Func<AnimationCurve> m_getCurve;

        private AnimationCurve m_lastCurve;

        public virtual void FetchAttributes(IEnumerable<WeavrAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute is ReversibleAttribute)
                {
                    m_drawReversibleToggle = true;
                }
                else if (attribute is AnimationDataFromAttribute)
                {
                    m_sourceCurveName = (attribute as AnimationDataFromAttribute).CurveProperty;
                    m_sourceDurationName = (attribute as AnimationDataFromAttribute).DurationProperty;
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_initialized)
            {
                m_initialized = true;
                Initialize(property);
            }

            if (string.IsNullOrEmpty(label.tooltip))
            {
                label.tooltip = m_tooltip;
            }

            if(m_canBeAnimatedQuery != null && !m_canBeAnimatedQuery())
            {
                property.FindPropertyRelative("m_animate").boolValue = false;
                DrawTarget(position, property.FindPropertyRelative("m_target"), label);
                return;
            }

            float durationX = position.x + EditorGUIUtility.labelWidth;

            float indentOffset = EditorGUI.indentLevel * 15;

            s_styles.animateToggleRect.x = position.x + s_styles.animateToggle.margin.left + indentOffset;
            s_styles.animateToggleRect.y = position.y + s_styles.animateToggle.margin.top;
            float toggleFullWidth = s_styles.animateToggleRect.width + s_styles.animateToggle.margin.horizontal + indentOffset;
            position.x += toggleFullWidth;
            position.width -= toggleFullWidth;
            position.height = m_targetHeight;

            var animateProperty = property.FindPropertyRelative("m_animate");
            var targetProperty = property.FindPropertyRelative("m_target");
            bool canBeAnimated = CanBeAnimated(targetProperty);
            bool expand = animateProperty.boolValue && canBeAnimated;
            if (canBeAnimated)
            {
                animateProperty.boolValue = GUI.Toggle(s_styles.animateToggleRect, animateProperty.boolValue, s_animateContent, s_styles.animateToggle);
            }
            else
            {
                animateProperty.boolValue = false;
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= toggleFullWidth;

            int indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            DrawTarget(position, targetProperty, label);

            EditorGUIUtility.labelWidth = labelWidth;


            if (expand)
            {

                var guiWasEnabled = GUI.enabled;
                var guiBackgroundColor = GUI.backgroundColor;

                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;

                var controlsRect = new Rect(position.x, position.y, 20, position.height);
                var overrideProperty = property.FindPropertyRelative("m_overrideDuration");
                var durationProperty = property.FindPropertyRelative("m_duration");
                var curveProperty = property.FindPropertyRelative("m_curve");

                EditorGUI.BeginChangeCheck();
                if (m_drawReversibleToggle)
                {
                    s_styles.reverseToggleRect.x = controlsRect.x + s_styles.reverseToggle.margin.left;
                    s_styles.reverseToggleRect.y = controlsRect.y + s_styles.reverseToggle.margin.top;

                    var reverseProperty = property.FindPropertyRelative("m_reversible");
                    reverseProperty.boolValue = GUI.Toggle(s_styles.reverseToggleRect, reverseProperty.boolValue, s_reverseContent, s_styles.reverseToggle);

                    //controlsRect.x += s_styles.reverseToggleRect.width + s_styles.reverseToggle.margin.horizontal;
                }


                //s_styles.overrideToggleRect.x = controlsRect.x + s_styles.overrideToggle.margin.left;
                s_styles.overrideToggleRect.x = Mathf.Min(controlsRect.x + s_styles.overrideToggle.margin.left, 
                                                          durationX - 50 - s_styles.overrideToggleRect.width - s_styles.overrideToggle.margin.right);
                s_styles.overrideToggleRect.y = controlsRect.y + s_styles.overrideToggle.margin.top;

                if (m_getDuration != null || m_sourceDurationName != null)
                {
                    overrideProperty.boolValue = GUI.Toggle(s_styles.overrideToggleRect, overrideProperty.boolValue, s_overrideContent, s_styles.overrideToggle);
                }
                else
                {
                    overrideProperty.boolValue = false;
                }

                controlsRect.x = s_styles.overrideToggleRect.x + s_styles.overrideToggleRect.width + s_styles.overrideToggle.margin.right;
                controlsRect.width = 50;
                GUI.Label(controlsRect, "Duration", s_styles.label);

                //controlsRect.x += controlsRect.width;
                controlsRect.x = durationX;
                controlsRect.width = 60;

                if ((m_getDuration == null && m_sourceDurationName == null) || overrideProperty.boolValue)
                {
                    durationProperty.floatValue = Mathf.Abs(EditorGUI.DelayedFloatField(controlsRect, durationProperty.floatValue));
                }
                else
                {
                    GUI.enabled = false;
                    GUI.backgroundColor = WeavrStyles.Colors.cyan;
                    durationProperty.floatValue = Mathf.Abs(EditorGUI.FloatField(controlsRect, m_getDuration != null ? m_getDuration() : property.serializedObject.FindProperty(m_sourceDurationName).floatValue));
                    GUI.enabled = guiWasEnabled;
                    GUI.backgroundColor = guiBackgroundColor;
                }

                s_styles.overrideToggleRect.x = controlsRect.x + controlsRect.width + s_styles.overrideToggle.margin.left;
                overrideProperty = property.FindPropertyRelative("m_overrideCurve");
                if (m_getCurve != null || m_sourceCurveName != null)
                {
                    overrideProperty.boolValue = GUI.Toggle(s_styles.overrideToggleRect, overrideProperty.boolValue, s_overrideContent, s_styles.overrideToggle);
                }
                else
                {
                    overrideProperty.boolValue = false;
                }
                controlsRect.x += controlsRect.width + s_styles.overrideToggleRect.width + s_styles.overrideToggle.margin.horizontal;
                controlsRect.width = 40;
                GUI.Label(controlsRect, "Curve", s_styles.label);
                controlsRect.x += controlsRect.width;
                controlsRect.width = position.width - (controlsRect.x - position.x) - 1;
                if ((m_getCurve == null && m_sourceCurveName == null) || overrideProperty.boolValue)
                {
                    curveProperty.animationCurveValue = EditorGUI.CurveField(controlsRect, curveProperty.animationCurveValue);

                    if (EditorGUI.EndChangeCheck())
                    {
                        curveProperty.animationCurveValue = curveProperty.animationCurveValue.Normalize(durationProperty.floatValue);
                    }
                }
                else
                {
                    GUI.enabled = false;
                    GUI.backgroundColor = WeavrStyles.Colors.cyan;
                    var curveValue = EditorGUI.CurveField(controlsRect, m_getCurve != null ? m_getCurve() : property.serializedObject.FindProperty(m_sourceCurveName).animationCurveValue);

                    if (EditorGUI.EndChangeCheck() || m_lastCurve == null || !m_lastCurve.IsSimilarTo(curveValue))
                    {
                        m_lastCurve = curveValue.Duplicate();
                        curveProperty.animationCurveValue = curveValue.Duplicate().Normalize(durationProperty.floatValue);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    position.y--;
                    s_styles.controlsStyle.Draw(position, false, false, false, false);
                }

                GUI.enabled = guiWasEnabled;
                GUI.backgroundColor = guiBackgroundColor;
            }

            EditorGUI.indentLevel = indentLevel;
        }

        protected virtual bool CanBeAnimated(SerializedProperty targetProperty)
        {
            return !m_isOptional || targetProperty.FindPropertyRelative(nameof(Optional.enabled)).boolValue;
        }

        protected virtual void DrawTarget(Rect position, SerializedProperty targetProperty, GUIContent label)
        {
            EditorGUI.PropertyField(position, targetProperty, label, true);
        }

        protected virtual void Initialize(SerializedProperty property)
        {
            FetchAttributes(property.GetAttributesInParents<WeavrAttribute>());

            var tooltipAttr = property.GetAttributeInParents<TooltipAttribute>();
            if(tooltipAttr != null)
            {
                m_tooltip = tooltipAttr.tooltip;
            }

            var dataAttribute = fieldInfo.GetAttribute<AnimationDataFromAttribute>();
            m_sourceCurveName = dataAttribute?.CurveProperty ?? m_sourceCurveName;
            m_sourceDurationName = dataAttribute?.DurationProperty ?? m_sourceDurationName;

            if (!string.IsNullOrEmpty(m_sourceCurveName))
            {
                var otherProperty = property.serializedObject.FindProperty(m_sourceCurveName);
                if (otherProperty == null || otherProperty.propertyType != SerializedPropertyType.AnimationCurve)
                {
                    m_getCurve = Delegate.CreateDelegate(typeof(Func<AnimationCurve>), property.serializedObject.targetObject, m_sourceCurveName) as Func<AnimationCurve>;
                    m_sourceCurveName = null;
                    if (m_getCurve == null)
                    {
                        Debug.LogWarning($"[{property.serializedObject.targetObject.name}.{fieldInfo.Name}]: Field or Method '{m_sourceCurveName}' not found or not of AnimationCurve type");
                    }
                }
            }

            if (!string.IsNullOrEmpty(m_sourceDurationName))
            {
                var otherProperty = property.serializedObject.FindProperty(m_sourceDurationName);
                if (otherProperty == null || otherProperty.propertyType != SerializedPropertyType.Float)
                {
                    m_getDuration = Delegate.CreateDelegate(typeof(Func<float>), property.serializedObject.targetObject, m_sourceDurationName) as Func<float>;
                    m_sourceDurationName = null;
                    if (m_getDuration == null)
                    {
                        Debug.LogWarning($"[{property.serializedObject.targetObject.name}.{fieldInfo.Name}]: Field or Method '{m_sourceDurationName}' not found or not of AnimationCurve type");
                    }
                }
            }

            var targetType = property.FindPropertyRelative("m_target").GetPropertyType();
            if(targetType != null)
            {
                m_isOptional = targetType.IsSubclassOf(typeof(Optional));
            }
            m_drawReversibleToggle |= fieldInfo.GetAttribute<ReversibleAttribute>() != null;

            var canBeAnimatedAttr = fieldInfo.GetAttribute<CanBeAnimatedIfAttribute>();
            if(canBeAnimatedAttr != null)
            {
                m_canBeAnimatedQuery = Delegate.CreateDelegate(typeof(Func<bool>), property.serializedObject.targetObject, canBeAnimatedAttr.MethodName) as Func<bool>;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            s_styles?.Refresh();
            var targetProperty = property.FindPropertyRelative("m_target");
            m_targetHeight = GetTargetPropertyHeight(targetProperty, label);
            float extraHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + s_styles.animateToggle.margin.vertical;
            return property.FindPropertyRelative("m_animate").boolValue ? m_targetHeight + extraHeight : m_targetHeight;
        }

        protected virtual float GetTargetPropertyHeight(SerializedProperty targetProperty, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(targetProperty);
        }
    }
}