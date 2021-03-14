using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(BaseCondition), true)]
    class ConditionEditor : BaseConditionEditor
    {
        
        protected static GUIContent s_tempContent = new GUIContent();
        protected static GUIContent m_isGlobalContent = new GUIContent(string.Empty, "Whether this condition is shared across network or not");

        protected List<string> m_hiddenProperties = new List<string>();
        protected Object m_delayedTarget;

        private float m_headerHeight;

        protected GUIContent m_headerContent = new GUIContent();

        public virtual bool ShouldDrawNotToggle => true;
        public virtual bool ShouldDrawIsGlobalToggle => m_targetCondition && m_targetCondition.CanBeShared && m_targetCondition.Procedure;

        protected virtual float HeaderHeight => m_headerHeight;

        protected BaseCondition m_targetCondition;
        public GUIStyle BoxStyle { get; private set; }
        public bool IsSelected { get; set; }

        protected override void OnEnable()
        {
            base.OnEnable();

            if(m_propertyTypes == null)
            {
                m_propertyTypes = new Dictionary<string, Type>();
            }
            m_targetCondition = target as BaseCondition;
            if (m_targetCondition)
            {
                BoxStyle = !Application.isPlaying ? s_styles.negativeBox : m_targetCondition.NetworkValue == true ? s_styles.positiveGlobalBox : m_targetCondition.CachedValue == true ? s_styles.positiveBox : s_styles.negativeBox;
                m_targetCondition.EvaluationChanged -= Condition_EvaluationChanged;
                m_targetCondition.EvaluationChanged += Condition_EvaluationChanged;
                var conditionDescription = ProcedureDefaults.Current.ConditionsCatalogue.GetDescriptor(m_targetCondition);
                m_hiddenProperties.Clear();
                if (conditionDescription)
                {
                    m_headerContent.text = conditionDescription.Name;
                    m_hiddenProperties.AddRange(conditionDescription.HiddenProperties);
                }
                else
                {
                    m_headerContent.text = EditorTools.NicifyName(target.GetType().Name);
                }
                m_headerHeight = string.IsNullOrEmpty(m_headerContent.text) ? 0 : k_defaultHeaderHeight;
            }
        }

        private void Condition_EvaluationChanged(BaseCondition condition, bool newValue)
        {
            BoxStyle = condition.NetworkValue == true ? s_styles.positiveGlobalBox : newValue ? s_styles.positiveBox : s_styles.negativeBox;
        }

        protected override void OnDisable()
        {
            if (target)
            {
                m_targetCondition.EvaluationChanged -= Condition_EvaluationChanged;
                if (target is IConditionsContainer container)
                {
                    foreach (var elem in container.Children)
                    {
                        DestroyEditor(elem);
                    }
                }
                else if(target is IConditionParent thisAsParent)
                {
                    DestroyEditor(thisAsParent.Child);
                }
            }
            base.OnDisable();
        }

        private void CollectConditionsForEditors(HashSet<BaseCondition> conditions, BaseCondition currentCondition)
        {
            if (target is IConditionsContainer container)
            {
                foreach (var elem in container.Children)
                {
                    if (elem)
                    {
                        conditions.Add(elem);
                        CollectConditionsForEditors(conditions, elem);
                    }
                }
            }
            else if (target is IConditionParent thisAsParent && thisAsParent.Child)
            {
                conditions.Add(thisAsParent.Child);
                CollectConditionsForEditors(conditions, thisAsParent.Child);
            }
        }

        public void DelayTargetAssignment(Object target)
        {
            m_delayedTarget = target;
        }

        public override void OnInspectorGUI()
        {
            DrawFullLayout();
        }

        public void DrawFullLayout()
        {
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            float height = GetHeight();
            var rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40, height);
            DrawFull(rect);
            GUILayout.Space(8);
        }

        public virtual void DrawLayout()
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_isNegated");
            property.NextVisible(false);
            DrawPropertiesLayout(property);
            if (serializedObject.ApplyModifiedProperties())
            {
                m_targetCondition.OnValidate();
                m_targetCondition.Modified();
            }
        }

        public virtual void DrawLayoutSelective(List<string> propertiesToHide)
        {
            s_styles.Refresh();
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_canCacheValue");
            while (property.NextVisible(false))
            {
                bool isHidden = propertiesToHide.Contains(property.name);
                EditorGUILayout.BeginHorizontal();
                if (isHidden == GUILayout.Toggle(!isHidden, GUIContent.none, GUILayout.Width(20)))
                {
                    if (!isHidden)
                    {
                        propertiesToHide.Add(property.name);
                    }
                    else
                    {
                        propertiesToHide.Remove(property.name);
                    }
                }
                EditorGUILayout.PropertyField(property);
                EditorGUILayout.EndHorizontal();
            }
            if (serializedObject.ApplyModifiedProperties())
            {
                m_targetCondition.OnValidate();
                m_targetCondition.Modified();
            }
        }

        public override void DrawFull(Rect rect)
        {
            var debugRect = rect;
            s_styles.Refresh();
            if (m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            if (m_delayedTarget && target is ITargetingObject tObj)
            {
                tObj.Target = m_delayedTarget;
                m_delayedTarget = null;
            }

            if (Event.current.type == EventType.Layout) { return; }

            DrawBox(rect);
            if (HeaderHeight > 0)
            {
                rect.y += HeaderHeight;
                rect.height -= HeaderHeight;
            }
            Draw(rect);

            DrawDebugLens(debugRect);
        }

        private void DrawBox(Rect rect)
        {
            if(Event.current.type != EventType.Repaint) { return; }

            var box = BoxStyle ?? s_styles.negativeBox;
            rect.width += box.border.horizontal;
            rect.height += box.border.vertical;
            rect.x -= box.border.left;
            rect.y -= box.border.top;
            box.Draw(rect, false, false, false, false);
            if (HeaderHeight > 0)
            {
                GUI.Label(new Rect(rect.x, rect.y, rect.width, HeaderHeight), m_headerContent, s_styles.header);
            }
        }

        public virtual void Draw(Rect rect)
        {
            serializedObject.Update();
            SerializedProperty property = null;
            if (ShouldDrawIsGlobalToggle)
            {
                property = serializedObject.FindProperty("m_isGlobal");
                DrawIsGlobalToggle(property, HeaderHeight > 0 ? new Rect(rect.x, rect.y - HeaderHeight, rect.width, rect.height + HeaderHeight) : rect);
            }
            property = serializedObject.FindProperty("m_isNegated");
            if (ShouldDrawNotToggle)
            {
                DrawNotToggle(property, HeaderHeight > 0 ? new Rect(rect.x, rect.y - HeaderHeight, rect.width, rect.height + HeaderHeight) : rect);
            }
            property.NextVisible(false);
            DrawProperties(rect, property);
            if (serializedObject.ApplyModifiedProperties())
            {
                m_targetCondition.OnValidate();
                m_targetCondition.Modified();
            }
        }

        protected virtual Rect DrawNotToggle(SerializedProperty property, Rect rect)
        {
            var notRect = rect;
            notRect.width = s_styles.notToggle.fixedWidth;
            notRect.x -= notRect.width;
            property.boolValue = GUI.Toggle(notRect, property.boolValue, "NOT", s_styles.notToggle);
            return rect;
        }

        protected virtual Rect DrawIsGlobalToggle(SerializedProperty property, Rect rect)
        {
            var isGlobalRect = rect;
            isGlobalRect.width = s_styles.isGlobalToggle.fixedWidth;
            isGlobalRect.height = s_styles.isGlobalToggle.fixedHeight;
            isGlobalRect.x += s_styles.isGlobalToggle.margin.left;
            isGlobalRect.y += s_styles.isGlobalToggle.margin.top;
            property.boolValue = GUI.Toggle(isGlobalRect, property.boolValue, m_isGlobalContent, s_styles.isGlobalToggle);
            return rect;
        }

        protected virtual void DrawProperties(Rect rect, SerializedProperty firstProperty)
        {
            var propRect = rect;
            do
            {
                if (!m_hiddenProperties.Contains(firstProperty.name))
                {
                    DrawProperty(firstProperty, ref propRect);
                }
                else if (firstProperty.propertyType == SerializedPropertyType.ObjectReference
                    && firstProperty.objectReferenceValue
                    && firstProperty.GetAttribute<DoNotAutofillAttribute>() != null)
                {
                    firstProperty.objectReferenceValue = null;
                }
            }
            while (firstProperty.NextVisible(false));
        }
        
        protected virtual void DrawPropertiesLayout(SerializedProperty firstProperty)
        {
            do
            {
                EditorGUILayout.PropertyField(firstProperty);
            }
            while (firstProperty.NextVisible(false));
        }

        public override float GetHeight()
        {
            return GetHeightInternal();
        }

        protected virtual float GetHeightInternal()
        {
            s_styles.Refresh();
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_isNegated");
            float height = HeaderHeight;
            while (property.NextVisible(false))
            {
                if (!m_hiddenProperties.Contains(property.name))
                {
                    height += GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            return height;
        }
    }
}
