using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(BillboardModifier), true)]
    class BillboardModifierEditor : ProcedureObjectEditor
    {
        private GUIContent m_headerTitle;

        public virtual BillboardModifier Model { get; protected set; }
        public virtual Object Target { get; protected set; }

        protected override void OnEnable()
        {
            base.OnEnable();

            Model = target as BillboardModifier;

            if (target is ITargetingObject tObj)
            {
                m_headerTitle = new GUIContent(tObj.Target ? tObj.Target.GetGameObject()?.name : Model.ElementName);
                Target = tObj.Target;
            }
        }

        public void GetTargetFrom(Billboard sample)
        {
            Model.GetTargetFrom(sample);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            DrawFullLayout();
        }

        public void DrawFullLayout()
        {

        }

        public override void DrawFull(Rect rect)
        {
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            if(Event.current.type == EventType.Layout) { return; }
            
            bool wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            Draw(rect);
            EditorGUIUtility.wideMode = wasWide;

            DrawDebugLens(rect);
        }

        public virtual void Draw(Rect rect)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_enabled");
            var headerRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            bool enabled = property.boolValue = EditorGUI.ToggleLeft(headerRect, m_headerTitle, property.boolValue);

            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            headerRect.x += EditorGUIUtility.labelWidth;
            headerRect.width = rect.width - EditorGUIUtility.labelWidth;
            EditorGUI.ObjectField(headerRect, Target, Target ? Target.GetType() : typeof(Object), true);
            GUI.enabled = wasEnabled;

            if (enabled)
            {
                property = serializedObject.FindProperty(nameof(BillboardModifier.separator));
                if (property.NextVisible(false)) {
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    rect.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    DrawProperties(rect, property);
                }
            }
            if (serializedObject.ApplyModifiedProperties())
            {
                Model.OnValidate();
                Model.Modified();
            }
        }

        protected virtual void DrawProperties(Rect rect, SerializedProperty firstProperty)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            var propRect = rect;
            do
            {
                DrawProperty(firstProperty, ref propRect);
            }
            while (firstProperty.NextVisible(false));
            EditorGUIUtility.labelWidth = labelWidth;
        }

        protected virtual void DrawPropertiesLayout(SerializedProperty firstProperty)
        {
            do
            {
                EditorGUILayout.PropertyField(firstProperty, firstProperty.isExpanded);
            }
            while (firstProperty.NextVisible(false));
        }

        public bool IsValid => Model/* && Target*/;

        public float GetHeight()
        {
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (Model.Enabled)
            {
                bool wasWide = EditorGUIUtility.wideMode;
                EditorGUIUtility.wideMode = true;
                serializedObject.Update();
                var property = serializedObject.FindProperty(nameof(BillboardModifier.separator));
                while (property.NextVisible(false))
                {
                    height += GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
                }
                EditorGUIUtility.wideMode = wasWide;
            }
            return height;
        }
    }
}
