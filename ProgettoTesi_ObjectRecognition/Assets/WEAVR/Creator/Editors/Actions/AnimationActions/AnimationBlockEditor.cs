using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(BaseAnimationBlock), true)]
    class AnimationBlockEditor : BaseAnimationBlockEditor
    {
        private static readonly int[] s_emptyArray = new int[0];

        private GUIContent m_headerTitle;
        private GUIContent m_execModeContent = new GUIContent();
        protected GUIContent m_asyncLabel = new GUIContent("Async", "Asynchronous Operation");
        protected GUIContent m_rewindContent = new GUIContent("Rev", "Reverses the action on context exit");

        // Debug Part 
        private string m_fullDrawDebugName;
        private string m_drawPropertiesDebugName;
        private string m_drawHeaderDebugName;
        private string m_fullDrawMiniPreviewDebugName;

        private int[] m_availableDataIds;
        private string[] m_availableDataIdNames = new string[0];
        public int[] AvailableDataIds {
            get => m_availableDataIds ?? s_emptyArray;
            set
            {
                value = value ?? s_emptyArray;
                if (m_availableDataIds != value)
                {
                    m_availableDataIds = value;
                    m_availableDataIdNames = new string[value.Length];
                    for (int i = 0; i < m_availableDataIds.Length; i++)
                    {
                        m_availableDataIdNames[i] = m_availableDataIds[i].ToString();
                    }
                }
            }
        }

        private int[] m_availableTargetIds;
        private string[] m_availableTargetIdNames = new string[0];
        public int[] AvailableTargetIds
        {
            get => m_availableTargetIds ?? s_emptyArray;
            set
            {
                value = value ?? s_emptyArray;
                if(m_availableTargetIds != value)
                {
                    m_availableTargetIds = value;
                    m_availableTargetIdNames = new string[value.Length];
                    for (int i = 0; i < m_availableTargetIds.Length; i++)
                    {
                        m_availableTargetIdNames[i] = m_availableTargetIds[i].ToString();
                    }
                }
            }
        }

        public virtual BaseAnimationBlock Model { get; protected set; }
        
        private List<string> m_hiddenProperties = new List<string>();

        protected virtual GUIContent HeaderContent
        {
            get
            {
                if (m_headerTitle == null)
                {
                    var animationDescription = ProcedureDefaults.Current.AnimationBlocksCatalogue.GetDescriptor(target as BaseAnimationBlock);
                    m_hiddenProperties.Clear();
                    if (animationDescription)
                    {
                        m_headerTitle = new GUIContent(animationDescription.Name, animationDescription.Icon, animationDescription.Description);
                        m_hiddenProperties.AddRange(animationDescription.HiddenProperties);
                    }
                    else
                    {
                        m_headerTitle = new GUIContent(target.GetType().Name);
                    }
                }
                return m_headerTitle;
            }
        }

        private bool m_showTargetSource;
        private bool m_expandHeader;
        private float m_collapsedHeaderHeight;
        private float m_expandedHeaderHeight;

        protected virtual float HeaderHeight => m_expandHeader ? m_expandedHeaderHeight : m_collapsedHeaderHeight;

        public bool IsExpanded => m_expandHeader;

        private float m_previewHeight;
        private float m_targetPreviewHeight;
        
        protected virtual bool HasMiniPreview => false;
        protected virtual float MiniPreviewHeight => 0;

        protected AnimationComposerEditor m_composerEditor;
        protected AnimationComposer m_composer;
        public AnimationComposerEditor ComposerEditor
        {
            get => m_composerEditor;
            set
            {
                if(m_composerEditor != value)
                {
                    m_composerEditor = value;
                    if (value)
                    {
                        m_composer = value.target as AnimationComposer;
                    }
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Model = target as BaseAnimationBlock;
            m_showTargetSource = target is ITargetingObject;
            if (Model)
            {
                float standardHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                m_expandedHeaderHeight = standardHeight * (m_showTargetSource ? 3 : 2);
                m_collapsedHeaderHeight = standardHeight;
            }

            m_fullDrawDebugName = $"{target.GetType().Name}::FullDraw()";
            m_drawHeaderDebugName = $"{target.GetType().Name}::DrawHeader()";
            m_drawPropertiesDebugName = $"{target.GetType().Name}::DrawProperties()";
            m_fullDrawMiniPreviewDebugName = $"{target.GetType().Name}::MiniPreviewDraw()";

            if(m_propertyTypes == null)
            {
                m_propertyTypes = new Dictionary<string, Type>();
            }
            if (target && m_headerTitle == null)
            {
                m_headerTitle = HeaderContent;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
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
            var property = serializedObject.FindProperty(nameof(BaseAnimationBlock.separator));
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
            var property = serializedObject.FindProperty(nameof(BaseAnimationBlock.separator));
            while (property.NextVisible(false))
            {
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

        private void DoHeaderLayout()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_track"));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_duration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_curve"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_targetFrom"));
            EditorGUILayout.EndHorizontal();

            if (serializedObject.ApplyModifiedProperties())
            {
                Model.Modified();
            }
        }

        public override void DrawFull(Rect rect)
        {
            DrawCollapsed(rect, true);

            DrawDebugLens(rect);
        }

        public void DrawCollapsed(Rect rect, bool expanded = false)
        {
            s_baseStyles.Refresh();
            if (m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            if (Event.current.type == EventType.Layout) { return; }

            //if (m_showTargetSource && Model.TargetSourceFrom != BaseAnimationBlock.DataSource.Self)
            //{
            //    if (0 <= Model.TargetSourceId && Model.TargetSourceId < m_composer.AnimationBlocks.Count)
            //    {
            //        (Model as ITargetingObject).Target = (m_composer.AnimationBlocks[Model.TargetSourceId] as ITargetingObject)?.Target;
            //    }
            //}

            float labelWidth = EditorGUIUtility.labelWidth;

            Profiler.BeginSample(m_fullDrawDebugName);

            serializedObject.Update();

            Profiler.BeginSample(m_drawHeaderDebugName);
            rect = DrawHeader(rect);
            Profiler.EndSample();

            if (expanded || m_expandHeader)
            {
                Profiler.BeginSample(m_drawPropertiesDebugName);

                EditorGUIUtility.labelWidth = 100;
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
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                Model.OnValidate();
                Model.Modified();
            }

            Profiler.EndSample();

            EditorGUIUtility.labelWidth = labelWidth;
        }
        
        protected virtual Rect DrawHeader(Rect rect)
        {
            var headerRect = rect;
            headerRect.height = EditorGUIUtility.singleLineHeight;

            rect.y += HeaderHeight + EditorGUIUtility.standardVerticalSpacing;
            rect.height -= HeaderHeight + EditorGUIUtility.standardVerticalSpacing;

            headerRect.width = rect.width - 120;
            bool expandHeader = EditorGUI.Foldout(headerRect, m_expandHeader, HeaderContent, s_baseStyles.headerLabel);
            if(expandHeader != m_expandHeader)
            {
                m_expandHeader = expandHeader;
                SceneView.RepaintAll();
            }
            EditorGUIUtility.labelWidth = 60;

            //headerRect.y += s_baseStyles.textToggle.margin.top;
            headerRect.x += headerRect.width;
            headerRect.width = 100;
            
            var curveProperty = serializedObject.FindProperty("m_curve");
            var durationProperty = serializedObject.FindProperty("m_duration");
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(headerRect, durationProperty);
            if (EditorGUI.EndChangeCheck())
            {
                curveProperty.animationCurveValue = curveProperty.animationCurveValue.Normalize(durationProperty.floatValue);
            }

            if (m_expandHeader)
            {
                EditorGUIUtility.labelWidth = 100;
                EditorGUI.indentLevel++;
                headerRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                headerRect.x = rect.x;
                headerRect.width = rect.width - 110;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(headerRect, curveProperty);
                if (EditorGUI.EndChangeCheck())
                {
                    curveProperty.animationCurveValue = curveProperty.animationCurveValue.Normalize(durationProperty.floatValue);
                }

                headerRect.x += rect.width - 100;
                headerRect.width = 80;
                EditorGUI.PropertyField(headerRect, serializedObject.FindProperty("m_track"), GUIContent.none);

                headerRect.x = rect.x;
                headerRect.width = rect.width - 20;
                headerRect.y += s_baseStyles.standardHeight;
                
                if (m_showTargetSource)
                {
                    EditorGUIUtility.labelWidth = 100;
                    SerializedProperty blockIdProperty = null;
                    var dataSourceProp = serializedObject.FindProperty("m_targetFrom");
                    if (dataSourceProp.enumValueIndex <= 1)
                    {
                        //EditorGUI.PropertyField(headerRect, dataSourceProp);
                        if (GUI.Button(EditorGUI.PrefixLabel(headerRect, s_dataSourceContent), dataSourceProp.enumDisplayNames[dataSourceProp.enumValueIndex], EditorStyles.popup))
                        {
                            ShowTargetSourcePopup(headerRect);
                        }
                    }
                    else
                    {
                        headerRect.width -= 40;
                        //EditorGUI.PropertyField(headerRect, dataSourceProp);
                        if (GUI.Button(EditorGUI.PrefixLabel(headerRect, s_targetSourceContent), dataSourceProp.enumDisplayNames[dataSourceProp.enumValueIndex], EditorStyles.popup))
                        {
                            ShowTargetSourcePopup(headerRect);
                        }
                        headerRect.x += headerRect.width;
                        headerRect.width = 40;
                        blockIdProperty = serializedObject.FindProperty("m_targetRefBlock");
                        //blockIdProperty.intValue = EditorGUI.IntPopup(headerRect, blockIdProperty.intValue, m_availableTargetIdNames, AvailableTargetIds);
                        if(GUI.Button(headerRect, blockIdProperty.intValue.ToString(), EditorStyles.popup))
                        {
                            ShowAvailableTargetSourcesId(headerRect);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
            return rect;
        }

        private void ShowAvailableTargetSourcesId(Rect headerRect)
        {
            int[] goodIndices = GetAvailableIndices(Model.TargetSourceFrom);
            GenericMenu menu = new GenericMenu();
            foreach (var index in goodIndices)
            {
                menu.AddItem(new GUIContent(index.ToString()), index == Model.TargetSourceId, 
                    () => m_preRenderAction = () => Model.TargetSourceId = index);
            }
            menu.DropDown(headerRect);
        }

        protected int[] GetAvailableIndices(BaseAnimationBlock.DataSource sourceType)
        {
            var blocks = m_composer.AnimationBlocks;
            var previousBlocks = blocks.Take(blocks.IndexOf(Model));

            int[] goodIndices = null;
            switch (sourceType)
            {
                case BaseAnimationBlock.DataSource.FromPrevious:
                    goodIndices = previousBlocks.Where(a => a is ITargetingObject).Select(a => blocks.IndexOf(a)).ToArray();
                    break;
                case BaseAnimationBlock.DataSource.FromPreviousInTrack:
                    goodIndices = previousBlocks.Where(a => a is ITargetingObject && a.Track == Model.Track).Select(a => blocks.IndexOf(a)).ToArray();
                    break;
                case BaseAnimationBlock.DataSource.FromBlockIndex:
                    goodIndices = blocks.Where(a => a is ITargetingObject && a != Model).Select(a => blocks.IndexOf(a)).ToArray();
                    break;
                default:
                    goodIndices = new int[0];
                    break;
            }

            return goodIndices;
        }

        private void ShowTargetSourcePopup(Rect headerRect)
        {
            var blocks = m_composer.AnimationBlocks;
            var previousBlocks = blocks.Take(blocks.IndexOf(Model));
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent(EditorTools.NicifyName(BaseAnimationBlock.DataSource.Self.ToString())),
                         Model.TargetSourceFrom == BaseAnimationBlock.DataSource.Self,
                         () => m_preRenderAction = () => Model.TargetSourceFrom = BaseAnimationBlock.DataSource.Self);

            if (previousBlocks.Any(a => a is ITargetingObject))
            {
                menu.AddItem(new GUIContent(EditorTools.NicifyName(BaseAnimationBlock.DataSource.FromPrevious.ToString())),
                             Model.TargetSourceFrom == BaseAnimationBlock.DataSource.FromPrevious,
                             () => m_preRenderAction = () => Model.TargetSourceFrom = BaseAnimationBlock.DataSource.FromPrevious);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(EditorTools.NicifyName(BaseAnimationBlock.DataSource.FromPrevious.ToString())));
            }
            if (previousBlocks.Any(a => a is ITargetingObject && a.Track == Model.Track))
            {
                menu.AddItem(new GUIContent(EditorTools.NicifyName(BaseAnimationBlock.DataSource.FromPreviousInTrack.ToString())),
                         Model.TargetSourceFrom == BaseAnimationBlock.DataSource.FromPreviousInTrack,
                         () => m_preRenderAction = () => Model.TargetSourceFrom = BaseAnimationBlock.DataSource.FromPreviousInTrack);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(EditorTools.NicifyName(BaseAnimationBlock.DataSource.FromPreviousInTrack.ToString())));
            }
            if (blocks.Any(a => a is ITargetingObject && a != Model))
            {
                menu.AddItem(new GUIContent(EditorTools.NicifyName(BaseAnimationBlock.DataSource.FromBlockIndex.ToString())),
                         Model.TargetSourceFrom == BaseAnimationBlock.DataSource.FromBlockIndex,
                         () => m_preRenderAction = () => Model.TargetSourceFrom = BaseAnimationBlock.DataSource.FromBlockIndex);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent(EditorTools.NicifyName(BaseAnimationBlock.DataSource.FromBlockIndex.ToString())));
            }
            menu.DropDown(headerRect);
        }

        public virtual void Draw(Rect rect)
        {
            var property = serializedObject.FindProperty("m_target");
            if (property != null)
            {
                EditorGUI.BeginDisabledGroup(Model.TargetSourceFrom != BaseAnimationBlock.DataSource.Self);
                DrawProperty(property, ref rect);
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                property = serializedObject.FindProperty(nameof(BaseAnimationBlock.separator));
            }
            if (property.NextVisible(false))
            {
                DrawProperties(rect, property);
            }
            //else
            //{
            //    GUI.Label(rect, Model.GetDescription(), s_baseStyles.descriptionLabel);
            //}
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
                EditorGUILayout.PropertyField(firstProperty);
            }
            while (firstProperty.NextVisible(false));
        }

        public float GetHeight()
        {
            m_targetPreviewHeight = HasMiniPreview ? MiniPreviewHeight : 0;
            if (Mathf.Abs(m_targetPreviewHeight - m_previewHeight) > 0.05f)
            {
                m_previewHeight = Mathf.MoveTowards(m_previewHeight, m_targetPreviewHeight, Time.deltaTime * 2);
                ProcedureObjectInspector.RepaintFull();
            }

            return GetHeightInternal() + HeaderHeight + EditorGUIUtility.standardVerticalSpacing 
                + (m_previewHeight > 1 ? m_previewHeight + EditorGUIUtility.standardVerticalSpacing : 0);
        }

        public float GetHeightCollapsed()
        {
            return m_expandHeader ? GetHeight() : HeaderHeight;
        }

        protected virtual float GetHeightInternal()
        {
            bool wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            serializedObject.Update();
            var property = serializedObject.FindProperty(nameof(BaseAnimationBlock.separator));
            float height = 0;
            while (property.NextVisible(false))
            {
                if (!m_hiddenProperties.Contains(property.name))
                {
                    height += GetPropertyHeight(property) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            EditorGUIUtility.wideMode = wasWide;
            return height <= 0 ? 0 : height - EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
