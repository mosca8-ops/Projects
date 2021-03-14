using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(BaseAction), true)]
    class ActionEditor : BaseActionEditor
    {
        protected static GUIContent m_isGlobalContent = new GUIContent(string.Empty, "Whether this action is shared across network or not");

        private GUIContent m_headerTitle;
        private GUIContent m_execModeContent = new GUIContent();
        protected GUIContent m_asyncLabel = new GUIContent("Async", "Asynchronous Operation");
        protected GUIContent m_rewindContent = new GUIContent("Rev", "Reverses the action on context exit");

        // Debug Part 
        private string m_fullDrawDebugName;
        private string m_drawPropertiesDebugName;
        private string m_drawHeaderDebugName;
        private string m_fullDrawMiniPreviewDebugName;

        public virtual BaseAction Model { get; protected set; }
        
        private List<string> m_hiddenProperties = new List<string>();

        protected virtual GUIContent HeaderContent
        {
            get
            {
                if (m_headerTitle == null)
                {
                    var actionDescription = ProcedureDefaults.Current.ActionsCatalogue.GetDescriptor(target as BaseAction);
                    m_hiddenProperties.Clear();
                    if (actionDescription)
                    {
                        m_headerTitle = new GUIContent(" " + actionDescription.Name, actionDescription.Icon, actionDescription.Description);
                        m_hiddenProperties.AddRange(actionDescription.HiddenProperties);
                    }
                    else
                    {
                        m_headerTitle = new GUIContent(" " + target.GetType().Name);
                    }

                    if (IsGlobal.HasValue)
                    {
                        m_hiddenProperties.Add(IsGlobalFieldName);
                    }

                    for (int i = 0; i < Model.ExecutionModes.Count; i++)
                    {
                        if (!Model.ExecutionModes[i])
                        {
                            Model.ExecutionModes.RemoveAt(i--);
                        }
                    }
                }
                return m_headerTitle;
            }
        }

        protected virtual float HeaderHeight => 20;

        private float m_previewHeight;
        private float m_targetPreviewHeight;
        
        protected virtual bool HasMiniPreview => false;
        protected virtual float MiniPreviewHeight => 0;


        private ISerializedNetworkProcedureObject m_networkObject;
        protected bool? IsGlobal => m_networkObject?.IsGlobal;
        protected string IsGlobalFieldName => m_networkObject?.IsGlobalFieldName;

        protected override void OnEnable()
        {
            base.OnEnable();

            Model = target as BaseAction;

            m_fullDrawDebugName = $"{target.GetType().Name}::FullDraw()";
            m_drawHeaderDebugName = $"{target.GetType().Name}::DrawHeader()";
            m_drawPropertiesDebugName = $"{target.GetType().Name}::DrawProperties()";
            m_fullDrawMiniPreviewDebugName = $"{target.GetType().Name}::MiniPreviewDraw()";

            if (target is ISerializedNetworkProcedureObject serNetObj && !string.IsNullOrEmpty(serNetObj.IsGlobalFieldName))
            {
                m_networkObject = serNetObj;
            }

            if(m_propertyTypes == null)
            {
                m_propertyTypes = new Dictionary<string, Type>();
            }
            if (target && m_headerTitle == null)
            {
                m_headerTitle = HeaderContent;
            }
        }

        #region [  MINI PREVIEW PART  ]

        protected virtual void DrawMiniPreview(Rect r)
        {
            
        }

        #endregion

        public override void OnInspectorGUI()
        {
            DrawFullLayout();
        }

        public void DrawFullLayout()
        {

        }

        public virtual void DrawLayout(bool showAuxData = false)
        {
            s_baseStyles.Refresh();
            if (showAuxData)
            {
                DoHeaderLayout();
            }
            serializedObject.Update();
            var property = serializedObject.FindProperty(nameof(BaseAction.separator));
            property.NextVisible(false);
            DrawPropertiesLayout(property);
            if (serializedObject.ApplyModifiedProperties())
            {
                Model.OnValidate();
                Model.Modified();
            }
        }

        public virtual void DrawLayoutSelective(List<string> propertiesToHide)
        {
            s_baseStyles.Refresh();
            DoHeaderLayout();
            serializedObject.Update();
            var property = serializedObject.FindProperty(nameof(BaseAction.separator));
            while (property.NextVisible(false))
            {
                if(IsGlobal.HasValue && property.propertyPath == IsGlobalFieldName)
                {
                    continue;
                }

                bool isHidden = propertiesToHide.Contains(property.name);
                EditorGUILayout.BeginHorizontal();
                if(isHidden == GUILayout.Toggle(!isHidden, GUIContent.none, GUILayout.Width(20)))
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
                Model.OnValidate();
                Model.Modified();
            }
        }

        protected void DoHeaderLayout()
        {
            GUILayout.BeginHorizontal(GUILayout.MaxHeight(20));
            GUILayout.Label("Execution: ");
            GUILayout.FlexibleSpace();

            var execModes = ProcedureDefaults.Current.ExecutionModes;

            bool modified = false;

            // Rewind part
            if (target is IFlowContextClosedElement)
            {
                var rewinder = target as IFlowContextClosedElement;
                bool rewind = GUILayout.Toggle(rewinder.RevertOnExit, m_rewindContent, s_baseStyles.textToggle);
                if (rewind != rewinder.RevertOnExit)
                {
                    Undo.RegisterCompleteObjectUndo(Model, "Changed Rewind");
                    modified = true;
                    rewinder.RevertOnExit = rewind;
                }
            }

            var async = GUILayout.Toggle(Model.AsyncThread != 0, m_asyncLabel, s_baseStyles.textToggle) ? -1 : 0;
            if (async != Model.AsyncThread)
            {
                if (!modified)
                {
                    Undo.RegisterCompleteObjectUndo(Model, "Changed Async");
                }
                modified = true;
                Model.AsyncThread = async;
            }

            GUILayout.Space(8);

            foreach (var execMode in execModes)
            {
                m_execModeContent.text = execMode.ModeShortName;
                m_execModeContent.tooltip = execMode.ModeName;

                bool containsMode = Model.ExecutionModes.Contains(execMode);
                if (containsMode != GUILayout.Toggle(containsMode, m_execModeContent, s_baseStyles.textToggle))
                {
                    Undo.RegisterCompleteObjectUndo(Model, "Changed Execution Mode");
                    if (containsMode)
                    {
                        // Remove it from exec modes
                        Model.ExecutionModes.Remove(execMode);
                        modified = true;
                    }
                    else if (!Model.ExecutionModes.Contains(execMode))
                    {
                        Model.ExecutionModes.Add(execMode);
                        modified = true;
                    }
                }
                GUILayout.Space(4);
            }


            if (modified)
            {
                Model.Modified();
            }
            GUILayout.EndHorizontal();

            if (IsGlobal.HasValue)
            {
                var isGlobalProperty = serializedObject.FindProperty(IsGlobalFieldName);
                if (isGlobalProperty != null)
                {
                    serializedObject.Update();
                    isGlobalProperty.boolValue = EditorGUILayout.Toggle("Is Global:", IsGlobal.Value);
                    if (serializedObject.ApplyModifiedProperties())
                    {
                        Model.OnValidate();
                        Model.Modified();
                    }

                    GUILayout.Space(2);
                }
            }
        }

        public override void DrawFull(Rect rect)
        {
            var debugRect = rect;
            s_baseStyles.Refresh();
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            if(Event.current.type == EventType.Layout) { return; }

            Profiler.BeginSample(m_fullDrawDebugName);

            Profiler.BeginSample(m_drawHeaderDebugName);
            rect = DrawActionHeader(rect);
            Profiler.EndSample();

            Profiler.BeginSample(m_drawPropertiesDebugName);

            rect.height -= m_previewHeight;
            bool wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            Draw(rect);
            EditorGUIUtility.wideMode = wasWide;

            Profiler.EndSample();

            if (m_previewHeight > EditorGUIUtility.standardVerticalSpacing + 1)
            {
                Profiler.BeginSample(m_fullDrawMiniPreviewDebugName);
                rect.y += rect.height/* + EditorGUIUtility.standardVerticalSpacing*/;
                rect.height = m_previewHeight;
                DrawMiniPreview(rect);
                Profiler.EndSample();
            }

            Profiler.EndSample();

            DrawDebugLens(debugRect);
        }

        protected virtual Rect DrawActionHeader(Rect rect)
        {
            var headerRect = rect;
            headerRect.height = HeaderHeight;

            rect.y += headerRect.height + EditorGUIUtility.standardVerticalSpacing;
            rect.height -= headerRect.height + EditorGUIUtility.standardVerticalSpacing;

            GUI.Label(headerRect, HeaderContent, s_baseStyles.headerLabel);

            var e = Event.current;
            if(e.type == EventType.MouseUp && e.button == 1 && headerRect.Contains(e.mousePosition)
                && ProcedureDefaults.Current.ActionsCatalogue.Descriptors.TryGetValue(Model.GetType(), out List<ActionDescriptor> descriptors) && descriptors.Count > 1)
            {
                GenericMenu menu = new GenericMenu();
                foreach(var descriptor in descriptors)
                {
                    if(descriptor.Variant != Model.Variant)
                    {
                        var newVariant = descriptor.Variant;
                        menu.AddItem(new GUIContent($"Replace with/{descriptor.Name}"), false, () => m_preRenderAction = () =>
                        {
                            Undo.RecordObject(Model, "Changed Variant");
                            Model.Variant = newVariant;
                            m_headerTitle = null;
                        });
                    }
                }
                menu.DropDown(headerRect);
                e.Use();
            }

            headerRect.y += s_baseStyles.textToggle.margin.top;
            headerRect.x += headerRect.width;
            headerRect.width = 32;
            var execModes = ProcedureView.Current != null && ProcedureView.Current.CurrentProcedure ? 
                        ProcedureView.Current.CurrentProcedure.ExecutionModes : ProcedureDefaults.Current.ExecutionModes;

            bool modified = false;

            float contentWidth = 0;

            // Execution Modes part
            for (int i = execModes.Count - 1; i >= 0; i--)
            {
                var execMode = execModes[i];

                if (!execMode) {
                    continue;
                }

                m_execModeContent.text = execMode.ModeShortName;
                m_execModeContent.tooltip = execMode.ModeName;

                contentWidth = s_baseStyles.textToggle.CalcSize(m_execModeContent).x + 1;
                headerRect.x -= headerRect.width + contentWidth + s_baseStyles.textToggle.margin.left;
                headerRect.width = contentWidth;

                bool containsMode = Model.ExecutionModes.Contains(execMode);
                if (containsMode != GUI.Toggle(headerRect, containsMode, m_execModeContent, s_baseStyles.textToggle))
                {
                    Undo.RegisterCompleteObjectUndo(Model, "Changed Execution Mode");
                    if (containsMode)
                    {
                        // Remove it from exec modes
                        Model.ExecutionModes.Remove(execMode);
                        modified = true;
                    }
                    else if (!Model.ExecutionModes.Contains(execMode))
                    {
                        Model.ExecutionModes.Add(execMode);
                        modified = true;
                    }
                }
            }


            // Async part
            contentWidth = s_baseStyles.textToggle.CalcSize(m_asyncLabel).x + 1;
            headerRect.x -= contentWidth + 10;
            headerRect.width = contentWidth;
            var async = GUI.Toggle(headerRect, Model.AsyncThread != 0, m_asyncLabel, s_baseStyles.textToggle) ? -1 : 0;
            if (async != Model.AsyncThread)
            {
                if (!modified)
                {
                    modified = true;
                    Undo.RegisterCompleteObjectUndo(Model, "Changed Async");
                }
                Model.AsyncThread = async;
            }

            // Rewind part
            if(target is IFlowContextClosedElement)
            {
                var rewinder = target as IFlowContextClosedElement;
                contentWidth = s_baseStyles.textToggle.CalcSize(m_rewindContent).x + 1;
                headerRect.x -= contentWidth + 8;
                headerRect.width = contentWidth;
                bool rewind = GUI.Toggle(headerRect, rewinder.RevertOnExit, m_rewindContent, s_baseStyles.textToggle);
                if (rewind != rewinder.RevertOnExit)
                {
                    if (!modified)
                    {
                        modified = true;
                        Undo.RegisterCompleteObjectUndo(Model, "Changed Rewind");
                    }
                    rewinder.RevertOnExit = rewind;
                }
            }

            if (modified)
            {
                Model.OnValidate();
                Model.Modified();
            }

            return rect;
        }

        public virtual void Draw(Rect rect)
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty(nameof(BaseAction.separator));
            if (property.NextVisible(false))
            {
                DrawProperties(rect, property);
            }
            else
            {
                GUI.Label(rect, Model.GetDescription(), s_baseStyles.descriptionLabel);
            }

            if (IsGlobal.HasValue)
            {
                var isGlobalProperty = serializedObject.FindProperty(IsGlobalFieldName);
                if (isGlobalProperty != null)
                {
                    DrawIsGlobalToggle(rect, isGlobalProperty);
                }
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                Model.OnValidate();
                Model.Modified();
            }
        }

        protected virtual void DrawIsGlobalToggle(Rect rect, SerializedProperty property)
        {
            rect.width = s_baseStyles.isGlobalToggle.fixedWidth;
            rect.height = s_baseStyles.isGlobalToggle.fixedHeight;
            rect.x = s_baseStyles.isGlobalToggle.margin.left + 3;
            rect.y += s_baseStyles.isGlobalToggle.margin.top - 8;
            property.boolValue = GUI.Toggle(rect, property.boolValue, m_isGlobalContent, s_baseStyles.isGlobalToggle);
        }

        protected virtual void DrawProperties(Rect rect, SerializedProperty firstProperty)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            var propRect = rect;
            do
            {
                if (!m_hiddenProperties.Contains(firstProperty.name))
                {
                    Profiler.BeginSample("PropertyDraw(): " + firstProperty.name);
                    DrawProperty(firstProperty, ref propRect);
                    Profiler.EndSample();
                }
                else if(firstProperty.propertyType == SerializedPropertyType.ObjectReference 
                    && firstProperty.objectReferenceValue 
                    && firstProperty.GetAttribute<DoNotAutofillAttribute>() != null)
                {
                    firstProperty.objectReferenceValue = null;
                }
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

        public override float GetHeight()
        {
            m_targetPreviewHeight = HasMiniPreview ? MiniPreviewHeight : 0;
            if (Mathf.Abs(m_targetPreviewHeight - m_previewHeight) > 0.05f)
            {
                m_previewHeight = Mathf.MoveTowards(m_previewHeight, m_targetPreviewHeight, Time.deltaTime * m_targetPreviewHeight * 0.1f);
                ProcedureObjectInspector.RepaintFull();
            }

            return GetHeightInternal() + HeaderHeight + EditorGUIUtility.standardVerticalSpacing 
                + (m_previewHeight > 1 ? m_previewHeight + EditorGUIUtility.standardVerticalSpacing : 0);
        }

        protected virtual float GetHeightInternal()
        {
            bool wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            serializedObject.Update();
            var property = serializedObject.FindProperty(nameof(BaseAction.separator));
            float height = 0;
            while (property.NextVisible(false))
            {
                if (!m_hiddenProperties.Contains(property.name))
                {
                    height += GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            EditorGUIUtility.wideMode = wasWide;
            return height <= 0 ? EditorGUIUtility.singleLineHeight : height - EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
