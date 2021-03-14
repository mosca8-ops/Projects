
#if WEAVR_PROCEDURE_LEGACY

namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.LayoutSystem;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Video;

    [Serializable]
    public class LayoutNode : StepNode
    {
        private const float k_conditionsPanelWidth = 90;
        private const float k_minConditionsHeight = 20;
        private const float k_maxConditionsHeight = 40;
        private const float k_actionsPanelWidth = 160;

        private const float k_nodeWidth = k_actionsPanelWidth + k_conditionsPanelWidth;

        private class Styles : BaseStyles
        {
            public GUIStyle actionTitle;
            public GUIStyle actionLabelTextArea;
            public GUIStyle ttsActionPanel;
            public float ttsToggleWidth = 45;
            public float ttsPreviewWidth = 75;


            private GUIStyle m_imageSizeStyle;
            private Vector2 m_imagePickerSize = new Vector2(100, 100);
            private GUIStyle m_actionsPanelStyle;
            private GUIStyle m_conditionsPanelStyle;

            public Vector2 imagePickerSize
            {
                get
                {
                    if (m_imageSizeStyle != null)
                    {
                        m_imagePickerSize.x = m_imageSizeStyle.fixedWidth > 0 ? m_imageSizeStyle.fixedWidth : 100;
                        m_imagePickerSize.y = m_imageSizeStyle.fixedHeight > 0 ? m_imageSizeStyle.fixedHeight : 100;
                    }
                    return m_imagePickerSize;
                }
            }


            public float actionsPanelWidth => m_actionsPanelStyle != null && m_actionsPanelStyle.fixedWidth > 0 ? 
                                              m_actionsPanelStyle.fixedWidth : k_actionsPanelWidth;
            public float conditionsPanelWidth => m_conditionsPanelStyle != null && m_conditionsPanelStyle.fixedWidth > 0 ? 
                                                 m_conditionsPanelStyle.fixedWidth : k_conditionsPanelWidth;
            public float nodeWidth => actionsPanelWidth + conditionsPanelWidth;

            public Styles() : base()
            {
                
            }

            protected override void InitializeStyles(bool isProSkin)
            {
                actionTitle = WeavrStyles.EditorSkin.FindStyle("layoutNode_actionTitle") ?? EditorStyles.miniLabel;
                actionLabelTextArea = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true
                };
                var actionTextAreaFont = WeavrStyles.EditorSkin.FindStyle("layoutNode_actionTextAreaFont");
                if(actionTextAreaFont != null)
                {
                    actionLabelTextArea.font = actionTextAreaFont.font;
                    actionLabelTextArea.fontSize = actionTextAreaFont.fontSize;
                    actionLabelTextArea.fontStyle = actionTextAreaFont.fontStyle;
                }

                m_actionsPanelStyle = WeavrStyles.EditorSkin.FindStyle("layoutNode_actionsPanel");
                m_conditionsPanelStyle = WeavrStyles.EditorSkin.FindStyle("layoutNode_conditionsPanel");
                m_imageSizeStyle = WeavrStyles.EditorSkin.FindStyle("layoutNode_imagePicker");

                var skin = GUI.skin;
                GUI.skin = null;
                ttsActionPanel = new GUIStyle("Box");
                GUI.skin = skin;
            }
        }

        private static Styles s_styles = new Styles();


        [SerializeField]
        protected ObjectFieldWrapper m_containerWrapper;

        [NonSerialized]
        protected LayoutContainer m_container;
        [NonSerialized]
        protected LayoutData m_data;

        [NonSerialized]
        private bool m_hookedToEvents = false;

        [SerializeField]
        protected bool m_useTTS;
        [SerializeField]
        protected bool m_previewTTS;

        private TTSAction m_ttsAction;
        private GUIContent m_textTTSContent = new GUIContent();
        [NonSerialized]
        private GUIContent m_useTTSContent;
        [NonSerialized]
        private GUIContent m_previewTTSContent;

        protected bool PreviewTTS
        {
            get { return m_previewTTS; }
            set
            {
                if(m_previewTTS != value)
                {
                    m_previewTTS = value;
                    if (value)
                    {
                        TTSAction.Play();
                    }
                    else
                    {
                        TTSAction.Stop();
                    }
                }
            }
        }

        protected GUIContent UseTTSGUIContent
        {
            get
            {
                if(m_useTTSContent == null)
                {
                    m_useTTSContent = new GUIContent(" TTS", WeavrStyles.Icons["speechBubble"], "Enable/Disable Text to Speech");
                }
                return m_useTTSContent;
            }
        }

        protected GUIContent PreviewTTSGUIContent
        {
            get
            {
                if (m_previewTTSContent == null)
                {
                    m_previewTTSContent = new GUIContent(" Preview", WeavrStyles.Icons["play"], "Whether to preview or not the TTS");
                }
                return m_previewTTSContent;
            }
        }

        protected TTSAction TTSAction
        {
            get
            {
                if(m_ttsAction == null)
                {
                    // First search for it into stepaction
                    foreach(var action in stepActions.actions)
                    {
                        if(action is TTSAction)
                        {
                            m_ttsAction = action as TTSAction;
                            break;
                        }
                    }
                    if(m_ttsAction == null)
                    {
                        // Add it
                        m_ttsAction = stepActions.AddNewAction(typeof(TTSAction)) as TTSAction;
                        if (!exitStepActions.actions.Any(a => a is StopAllAsyncActions))
                        {
                            exitStepActions.AddNewAction(typeof(StopAllAsyncActions));
                        }
                    }
                }
                return m_ttsAction;
            }
            set
            {
                if(m_ttsAction != value)
                {
                    if(value == null)
                    {
                        stepActions.RemoveAction(m_ttsAction);
                        exitStepActions.RemoveAction(exitStepActions.actions.LastOrDefault(a => a is StopAllAsyncActions));
                    }
                    m_ttsAction = value;
                    if(value != null && !exitStepActions.actions.Any(a => a is StopAllAsyncActions))
                    {
                        exitStepActions.AddNewAction(typeof(StopAllAsyncActions));
                    }
                }
            }
        }

        protected LayoutData Data
        {
            get
            {
                if(m_data == null)
                {
                    m_data = new LayoutData();
                    //m_container = m_containerWrapper.Object as LayoutContainer;
                    if (m_container == null && m_containerWrapper.Object as LayoutContainer)
                    {
                        m_container = m_containerWrapper.Object as LayoutContainer;
                        m_data.Update(this, m_container);
                    }
                    else if(stepActions.actions.Count > 0)
                    {
                        foreach (var a in stepActions.actions)
                        {
                            if (a is SetValueAction && ((SetValueAction)a).PropertyValue.ObjectWrapper.Object is LayoutContainer)
                            {
                                m_container = ((SetValueAction)a).PropertyValue.ObjectWrapper.Object as LayoutContainer;
                                m_containerWrapper.Object = m_container;
                                m_data.Update(this, m_container);
                            }
                        }
                    }
                }
                //if(!m_data.initialized && m_container == null && m_containerWrapper.Object is LayoutContainer)
                //{
                //    m_container = m_containerWrapper.Object as LayoutContainer;
                //    m_data.Update(this, m_container);
                //}
                return m_data;
            }
        }
        
        public LayoutContainer LayoutContainer
        {
            get {
                if(m_container == null && m_containerWrapper.Object != null)
                {
                    m_container = m_containerWrapper.Object as LayoutContainer;
                    if(m_container != null)
                    {
                        Selected -= LayoutNode_Selected;
                        Selected += LayoutNode_Selected;

                        Deselected -= LayoutNode_Deselected;
                        Deselected += LayoutNode_Deselected;
                    }
                }
                return m_container;
            }
            set
            {
                if(m_container != value)
                {
                    m_container = value;
                    m_containerWrapper.Object = m_container;
                    this.ClearUndoProperty(ExitConditions);
                    Data.Initialize(this, m_container);

                    Title = m_container?.name;
                }
            }
        }

        public ObjectFieldWrapper ContainerWrapper => m_containerWrapper;

        public override List<ExitCondition> ExitConditions
        {
            get
            {
                if (_exitConditions == null)
                {
                    _exitConditions = new List<ExitCondition>();
                }
                return _exitConditions;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (m_containerWrapper == null)
            {
                m_containerWrapper = ObjectFieldWrapper.Create(typeof(LayoutContainer));
                Undo.RegisterCreatedObjectUndo(m_containerWrapper, "Created container wrapper");
            }

            m_container = m_containerWrapper.Object as LayoutContainer;

            HookEvents();
            //if (m_data == null)
            //{
            //    m_data = new LayoutData();
            //}

            //if(m_containerWrapper.Object is LayoutContainer)
            //{
            //    m_container = m_containerWrapper.Object as LayoutContainer;
            //    m_data.Update(this, m_container);
            //}
        }

        private void HookEvents()
        {
            UnhookEvents();

            Selected += LayoutNode_Selected;
            Deselected += LayoutNode_Deselected;

            m_hookedToEvents = true;
        }

        private void UnhookEvents()
        {
            Selected -= LayoutNode_Selected;
            Deselected -= LayoutNode_Deselected;

            m_hookedToEvents = false;
        }

        private void LayoutNode_Deselected()
        {
            if (LayoutContainer != null && !Application.isPlaying)
            {
                LayoutContainer.ResetToDefaults();
                //LayoutContainer.IsCurrentlyActive = false;
                if (m_useTTS && m_ttsAction != null)
                {
                    m_ttsAction.Stop();
                }
            }
        }

        private void LayoutNode_Selected()
        {
            if (LayoutContainer != null && !Application.isPlaying)
            {
                LayoutContainer.IsCurrentlyActive = true;
                if(m_useTTS && PreviewTTS && TTSAction != null)
                {
                    TTSAction.Play();
                }
            }
        }

        protected override void InitializeProperties() {
            base.InitializeProperties();
        }

        public override void OnSavedAsAsset() {
            base.OnSavedAsAsset();
            m_containerWrapper.SaveAsAsset(this);
        }

        public override void Draw(Rect pos) {

            s_styles.Refresh();

            GUI.enabled = !Application.isPlaying;

            if (!m_hookedToEvents)
            {
                HookEvents();
            }

            Rect lineRect = pos;
            lineRect.height = m_defaultLineHeight;
            lineRect.y = 0;

            //lineRect.width = 10;
            //isMandatory = GUI.Toggle(lineRect, isMandatory, new GUIContent("", "Is Mandatory"));

            lineRect.width = s_idLabelStyle.fixedWidth;
            lineRect.x = pos.width - lineRect.width;
            EditorGUI.LabelField(lineRect, Id, s_idLabelStyle);

            lineRect.x = pos.x;
            lineRect.y = pos.y;
            lineRect.width = pos.width - s_styles.conditionsPanelWidth - 4;
            m_actionsSize = new Vector2(s_styles.actionsPanelWidth, 0);

            // Draw actions step
            m_inputConnectionPoint.x = NodeRect.xMin + GUI.skin.window.margin.left;
            m_inputConnectionPoint.y = NodeRect.center.y;

            //lineRect = DrawStepActions(lineRect, stepActions, "Actions");
            //lineRect = DrawStepActions(lineRect, exitStepActions, "Exit Actions");
            lineRect.y += 10;
            DrawItems(lineRect);

            //lineRect.y += 4;

            lineRect = pos;
            lineRect.width = s_styles.conditionsPanelWidth;
            lineRect.height = m_actionsSize.y - 20;
            lineRect.x += m_actionsSize.x;
            lineRect.y += 10;


            lineRect.x += 4;

            //HorizontalLine(lineRect);

            // Draw condition
            //lineRect = DrawConditions(pos, lineRect);
            lineRect = DrawConditions(lineRect);

            lineRect.x -= 4;
            VerticalLine(lineRect);

            //lineRect.y += lineRect.height;

            m_actionsSize.y = Mathf.Max(lineRect.height + m_defaultLineHeight, m_actionsSize.y);

            //HorizontalLine(lineRect);

            // Draw the exit actions
            if (IsSelected && GUI.enabled)
            {
                Data.UpdatePreviewValues();
            }

            GUI.enabled = true;
        }

        public override ExitCondition AddExitCondition() {
            var newCondition = base.AddExitCondition();
            if(newCondition == null) { return null; }


            return newCondition;
        }

        private Rect DrawConditions(Rect lineRect)
        {
            Rect conditionRect = lineRect;
            //conditionRect.y += 4;

            Color backgroundColor = Color.clear;
            GUIContent tempContent = new GUIContent();

            float height = Mathf.Max(lineRect.height / ExitConditions.Count, k_minConditionsHeight);
            //conditionRect.height = Mathf.Min(height, k_maxConditionsHeight) - EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < Data.buttons.Count; i++)
            {
                var buttonWrapper = m_data.buttons[i];
                var condition = buttonWrapper.condition;

                if(condition == null && this != null) {
                    buttonWrapper.Initialize(this);
                    condition = buttonWrapper.condition;
                    if (condition == null)
                    {
                        continue;
                    }
                }

                tempContent.text = buttonWrapper.button.Label;
                conditionRect.height = Mathf.Clamp(s_conditionLabelStyle.CalcHeight(tempContent, conditionRect.width), k_minConditionsHeight, k_maxConditionsHeight) 
                                     - EditorGUIUtility.standardVerticalSpacing;

                backgroundColor = condition.backgroundColor ?? (condition.IsSelected ? s_conditionRectStyle.focused.textColor
                                                                           : s_conditionRectStyle.normal.textColor);
                s_conditionLabelStyle.normal.textColor = condition.backgroundColor.HasValue || condition.IsSelected ?
                            s_conditionLabelStyle.focused.textColor : s_conditionLabelStyle.active.textColor;
                EditorGUI.DrawRect(conditionRect, backgroundColor);
                //GUI.Label(conditionRect, tempContent, s_conditionLabelStyle);
                buttonWrapper.Label = EditorGUI.TextField(conditionRect, buttonWrapper.Label, s_conditionLabelStyle);
                RepositionConnection(conditionRect, condition);
                conditionRect.y += Mathf.Max(height, conditionRect.height) + EditorGUIUtility.standardVerticalSpacing;

                StepConnection connection = null;
                buttonWrapper.ShouldBeVisible = _connectionsDictionary.TryGetValue(condition, out connection) 
                                        && connection != null 
                                        && connection.NodeB != null;
            }
            lineRect.height = (conditionRect.y - lineRect.y);
            return lineRect;
        }

        private void RepositionConnection(Rect conditionRect, ExitCondition condition) {
            StepConnection connection = null;
            if(!_connectionsDictionary.TryGetValue(condition, out connection) || connection == null) {
                // Update dictionary (Hack to avoid undo issues)
                // Rebuild the dictionary
                _connectionsDictionary.Clear();
                for (int i = 0; i < ExitConditions.Count; i++) {
                    _connectionsDictionary[ExitConditions[i]] = _outputConnections[i] as StepConnection;
                }
                return;
            }
            Vector2 connectionPoint = new Vector2(NodeRect.x + NodeRect.width - GUI.skin.window.margin.right,
                                                  NodeRect.y + conditionRect.y + conditionRect.height * 0.5f);
            connection.StartPoint = connectionPoint;
        }

        private void DrawItems(Rect rect)
        {
            float rectX = rect.x;
            float width = rect.width;
            if (m_useTTS && TTSAction != null)
            {
                m_textTTSContent.text = TTSAction.Text;
                float textHeight = s_styles.actionLabelTextArea.CalcHeight(m_textTTSContent, rect.width);
                rect.height = 2 * EditorGUIUtility.singleLineHeight + 4 * EditorGUIUtility.standardVerticalSpacing
                            + textHeight;
                GUI.Box(rect, GUIContent.none, s_styles.ttsActionPanel);
                //rect.width = width - 2 * EditorGUIUtility.standardVerticalSpacing;
                rect.x += EditorGUIUtility.standardVerticalSpacing;
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width = s_styles.ttsToggleWidth;
                m_useTTS = GUI.Toggle(rect, m_useTTS, UseTTSGUIContent, EditorStyles.miniButtonLeft);

                rect.x += rect.width;
                rect.width = s_styles.ttsPreviewWidth;
                PreviewTTS = GUI.Toggle(rect, PreviewTTS, PreviewTTSGUIContent, EditorStyles.miniButtonRight);
                
                rect.x = rectX + EditorGUIUtility.standardVerticalSpacing;
                rect.width = width - 2 * EditorGUIUtility.standardVerticalSpacing;
                rect.y += rect.height;
                //rect.width = width - s_styles.ttsToggleWidth - rect.width - 2 * EditorGUIUtility.standardVerticalSpacing;
                TTSAction.DrawVoiceSelection(rect, false);

                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.height = textHeight;
                TTSAction.Text = EditorGUI.TextArea(rect, TTSAction.Text, s_styles.actionLabelTextArea);

                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.x = rectX;
            }
            else
            {
                PreviewTTS = false;
                TTSAction = null;

                rect.height = EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing;
                GUI.Box(rect, GUIContent.none, s_styles.ttsActionPanel);

                rect.x += EditorGUIUtility.standardVerticalSpacing;
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width = s_styles.ttsToggleWidth;
                m_useTTS = GUI.Toggle(rect, m_useTTS, UseTTSGUIContent, EditorStyles.miniButton);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

                rect.width = width;
                rect.x = rectX;
            }


            foreach(var item in Data.passiveElements)
            {
                rect.height = item.GetHeight(rect.width);
                item.Draw(rect);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            }
            m_actionsSize.y = rect.y;
        }

        private void VerticalLine(Rect rect)
        {
            //rect.x += 5;
            //rect.width -= 10;
            rect.width = 1;
            rect.height -= 4;
            EditorGUI.DrawRect(rect, nodeColor == FocusColor.None ? Color.gray : CurrentNodeStyle.normal.textColor);
            rect.x++;
            EditorGUI.DrawRect(rect, Color.black);
        }

        public override BaseNode OnRemove() {
            base.OnRemove();

            LayoutNode_Deselected();

            Selected -= LayoutNode_Selected;
            Deselected -= LayoutNode_Deselected;

            return this;
        }

        protected override Rect GetContentSize(Rect pos) {
            //pos.width = Mathf.Clamp(m_actionsSize.x + k_conditionsPanelWidth, 180, s_nodeMaxSize.x);
            pos.width = s_styles.nodeWidth;
            pos.height = m_actionsSize.y;
            return pos;
        }

        public override void OnAssetLoad() {
            
        }

        public override bool CopyFrom(object source) {
            LayoutNode other = source as LayoutNode;
            if (other != null && base.CopyFrom(source)) {
                // Main part
                

                return true;
            }
            return false;
        }

        protected class LayoutData
        {
            public List<ButtonWrapper> buttons;
            public List<LabelWrapper> labels;
            public List<ImageWrapper> images;
            public List<GenericImageWrapper> genericImages;
            public List<VideoWrapper> videos;
            public List<InputFieldWrapper> inputFields;

            public List<ElementWrapper> passiveElements;
            public List<ElementWrapper> allElements;

            public bool initialized;

            public LayoutData()
            {
                buttons = new List<ButtonWrapper>();
                labels = new List<LabelWrapper>();
                images = new List<ImageWrapper>();
                genericImages = new List<GenericImageWrapper>();
                videos = new List<VideoWrapper>();
                inputFields = new List<InputFieldWrapper>();

                passiveElements = new List<ElementWrapper>();
                allElements = new List<ElementWrapper>();
            }

            public void UpdatePreviewValues()
            {
                for (int i = 0; i < allElements.Count; i++)
                {
                    allElements[i].UpdatePreviewValues();
                }
            }

            public void Update(LayoutNode node, LayoutContainer container)
            {
                foreach(var item in container.LayoutItems)
                {
                    if(item is LayoutButton)
                    {
                        var button = new ButtonWrapper(item as LayoutButton);
                        button.Update(node);
                        buttons.Add(button);

                        allElements.Add(button);
                    }
                    else if(item is LayoutLabel)
                    {
                        var elem = new LabelWrapper(item as LayoutLabel);
                        elem.Update(node);
                        passiveElements.Add(elem);
                        labels.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutImage)
                    {
                        var elem = new ImageWrapper(item as LayoutImage);
                        elem.Update(node);
                        passiveElements.Add(elem);
                        images.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutGenericImage)
                    {
                        var elem = new GenericImageWrapper(item as LayoutGenericImage);
                        elem.Update(node);
                        passiveElements.Add(elem);
                        genericImages.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutVideo)
                    {
                        var elem = new VideoWrapper(item as LayoutVideo);
                        elem.Update(node);
                        passiveElements.Add(elem);
                        videos.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutInputField)
                    {
                        //var elem = new LabelWrapper(item as LayoutLabel);
                        //elem.Update(node);
                        //passiveElements.Add(elem);
                        //labels.Add(elem);

                        //allElements.Add(button);
                    }
                }
                initialized = true;
            }

            public void Initialize(LayoutNode node, LayoutContainer container)
            {

                foreach (var item in container.LayoutItems)
                {
                    if (item is LayoutButton)
                    {
                        var button = new ButtonWrapper(item as LayoutButton);
                        button.Initialize(node);
                        buttons.Add(button);

                        allElements.Add(button);
                    }
                    else if (item is LayoutLabel)
                    {
                        var elem = new LabelWrapper(item as LayoutLabel);
                        elem.Initialize(node);
                        passiveElements.Add(elem);
                        labels.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutImage)
                    {
                        var elem = new ImageWrapper(item as LayoutImage);
                        elem.Initialize(node);
                        passiveElements.Add(elem);
                        images.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutGenericImage)
                    {
                        var elem = new GenericImageWrapper(item as LayoutGenericImage);
                        elem.Initialize(node);
                        passiveElements.Add(elem);
                        genericImages.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutVideo)
                    {
                        var elem = new VideoWrapper(item as LayoutVideo);
                        elem.Initialize(node);
                        passiveElements.Add(elem);
                        videos.Add(elem);

                        allElements.Add(elem);
                    }
                    else if (item is LayoutInputField)
                    {
                        //var elem = new LabelWrapper(item as LayoutLabel);
                        //elem.Update(node);
                        //passiveElements.Add(elem);
                        //labels.Add(elem);

                        //allElements.Add(elem);
                    }
                }

                var action = node.stepActions.AddNewAction(typeof(SetValueAction)) as SetValueAction;
                action.PropertyValue.ObjectWrapper.Object = container;
                action.PropertyValue.PropertyPath.SetPropertyPathByName(container, nameof(LayoutContainer.IsCurrentlyActive));
                action.PropertyValue.Value = true;

                //action = node.exitStepActions.AddNewAction(typeof(SetValueAction)) as SetValueAction;
                //action.PropertyValue.ObjectWrapper.Object = container;
                //action.PropertyValue.PropertyPath.SetPropertyPathByName(container, nameof(LayoutContainer.IsCurrentlyActive));
                //action.PropertyValue.Value = false;

                initialized = true;
            }
        }

        protected abstract class ElementWrapper
        {
            public SetValueAction action;
            public SetValueAction exitAction;
            public BaseLayoutItem target;

            protected LayoutNode node;

            protected GUIContent m_targetName;
            protected float m_targetNameHeight;

            public ElementWrapper(BaseLayoutItem target)
            {
                this.target = target;
                m_targetName = new GUIContent(target.name);
            }

            public abstract void UpdatePreviewValues();

            public abstract void Initialize(LayoutNode node);

            public virtual void Draw(Rect rect)
            {
                float height = rect.height;
                rect.height = m_targetNameHeight;
                EditorGUI.LabelField(rect, m_targetName, s_styles.actionTitle);
                rect.height = height - m_targetNameHeight - EditorGUIUtility.standardVerticalSpacing;
                rect.y += m_targetNameHeight + EditorGUIUtility.standardVerticalSpacing;
                InnerDraw(rect);
            }

            protected abstract void InnerDraw(Rect rect);

            public virtual float GetHeight(float width)
            {
                m_targetNameHeight = s_styles.actionTitle.CalcHeight(m_targetName, width);
                return m_targetNameHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            public virtual void Update(LayoutNode node)
            {
                this.node = node;
                foreach (var a in node.stepActions.actions)
                {
                    if (a is SetValueAction && ((SetValueAction)a).PropertyValue.ObjectWrapper.Object == target)
                    {
                        action = a as SetValueAction;
                        return;
                    }
                }
            }

            protected virtual SetValueAction FindAction(string propertyName)
            {
                return FindAction(node, propertyName);
            }

            protected virtual SetValueAction FindAction(LayoutNode node, string propertyName)
            {
                foreach (var a in node.stepActions.actions)
                {
                    if (a is SetValueAction 
                        && ((SetValueAction)a).PropertyValue.ObjectWrapper.Object == target
                        && ((SetValueAction)a).PropertyValue.SelectedPropertyPath != null
                        && ((SetValueAction)a).PropertyValue.SelectedPropertyPath.Contains(propertyName))
                    {
                        return a as SetValueAction;
                    }
                }
                return null;
            }

            protected void InitializeEnterAction(LayoutNode node, string propertyName)
            {
                this.node = node;
                action = RegisterSetValueAction(node.stepActions, propertyName);
            }

            protected void InitializeExitAction(LayoutNode node, string propertyName)
            {
                this.node = node;
                exitAction = RegisterSetValueAction(node.exitStepActions, propertyName);
            }

            protected SetValueAction RegisterSetValueAction(StepActionContainer container, string propertyName)
            {
                var action = container.AddNewAction(typeof(SetValueAction)) as SetValueAction;
                action.PropertyValue.ObjectWrapper.Object = target;
                action.PropertyValue.PropertyPath.SetPropertyPathByName(target, propertyName);
                return action;
            }

        }

        protected class LabelWrapper : ElementWrapper
        {
            public LayoutLabel label;

            private GUIContent m_text;
            public string Text
            {
                get { return m_text.text; }
                set
                {
                    if(m_text.text != value)
                    {
                        m_text.text = value;
                        action.PropertyValue.Value = m_text.text;
                    }
                }
            }

            public LabelWrapper(LayoutLabel label) : base(label)
            {
                this.label = label;
                m_text = new GUIContent();
            }

            protected override void InnerDraw(Rect rect)
            {
                Text = EditorGUI.TextArea(rect, Text, s_styles.actionLabelTextArea);
            }

            public override float GetHeight(float width)
            {
                return EditorStyles.textArea.CalcHeight(m_text, width)
                     + base.GetHeight(width);
            }

            public override void Initialize(LayoutNode node)
            {
                InitializeEnterAction(node, nameof(LayoutLabel.Text));
            }

            public override void Update(LayoutNode node)
            {
                base.Update(node);
                m_text.text = action.PropertyValue.Value as string;
            }

            public override void UpdatePreviewValues()
            {
                if(label == null)
                {
                    label = action.PropertyValue.ObjectWrapper.Object as LayoutLabel;
                }
                label.Text = Text;
            }
        }

        protected class ImageWrapper : ElementWrapper
        {
            public LayoutImage image;

            private Sprite m_sprite;
            public Sprite Sprite
            {
                get { return m_sprite; }
                set
                {
                    if(m_sprite != value)
                    {
                        m_sprite = value;
                        action.PropertyValue.Value = m_sprite;
                    }
                }
            }

            public ImageWrapper(LayoutImage image) : base(image)
            {
                this.image = image;
            }

            protected override void InnerDraw(Rect rect)
            {
                var center = rect.center;
                rect.size = s_styles.imagePickerSize;
                rect.center = center;
                var skin = GUI.skin;
                GUI.skin = null;
                Sprite = EditorGUI.ObjectField(rect, GUIContent.none, Sprite, typeof(Sprite), false) as Sprite;
                GUI.skin = skin;
            }

            public override float GetHeight(float width)
            {
                return base.GetHeight(width) + s_styles.imagePickerSize.y;
            }

            public override void Initialize(LayoutNode node)
            {
                InitializeEnterAction(node, nameof(LayoutImage.Sprite));
            }

            public override void Update(LayoutNode node)
            {
                base.Update(node);
                m_sprite = action.PropertyValue.Value as Sprite;
            }

            public override void UpdatePreviewValues()
            {
                if (image == null)
                {
                    image = action.PropertyValue.ObjectWrapper.Object as LayoutImage;
                }
                image.Sprite = Sprite;
            }
        }

        protected class GenericImageWrapper : ElementWrapper
        {
            public LayoutGenericImage image;

            private Texture2D m_texture;
            public Texture2D Texture
            {
                get { return m_texture; }
                set
                {
                    if (m_texture != value)
                    {
                        m_texture = value;
                        action.PropertyValue.Value = m_texture;
                    }
                }
            }

            public GenericImageWrapper(LayoutGenericImage image) : base(image)
            {
                this.image = image;
            }

            protected override void InnerDraw(Rect rect)
            {
                var center = rect.center;
                rect.size = s_styles.imagePickerSize;
                rect.center = center;
                var skin = GUI.skin;
                GUI.skin = null;
                Texture = EditorGUI.ObjectField(rect, GUIContent.none, Texture, typeof(Texture2D), false) as Texture2D;
                GUI.skin = skin;
            }

            public override float GetHeight(float width)
            {
                return base.GetHeight(width) + s_styles.imagePickerSize.y;
            }

            public override void Initialize(LayoutNode node)
            {
                InitializeEnterAction(node, nameof(LayoutGenericImage.Texture));
            }

            public override void Update(LayoutNode node)
            {
                base.Update(node);
                m_texture = action.PropertyValue.Value as Texture2D;
            }

            public override void UpdatePreviewValues()
            {
                if (image == null)
                {
                    image = action.PropertyValue.ObjectWrapper.Object as LayoutGenericImage;
                }
                image.Texture = Texture;
            }
        }

        protected class VideoWrapper : ElementWrapper
        {
            public LayoutVideo video;

            protected GUIContent m_loopContent = new GUIContent("Loop", "Whether to loop the video or not");
            protected GUIContent m_pinDescriptionContent = new GUIContent("Pin Description", "Whether to make the description always visible or not");

            private Texture2D m_texture;
            public Texture2D Texture
            {
                get { return m_texture; }
                set
                {
                    if (m_texture != value)
                    {
                        m_texture = value;
                        if (value != null)
                        {
                            action.PropertyValue.PropertyPath.SetPropertyPathByName(video, nameof(LayoutVideo.Texture)); 
                            action.PropertyValue.Value = m_texture;
                        }
                    }
                }
            }

            private VideoClip m_videoClip;
            public VideoClip Video
            {
                get { return m_videoClip; }
                set
                {
                    if(m_videoClip != value)
                    {
                        m_videoClip = value;
                        if (value != null)
                        {
                            action.PropertyValue.PropertyPath.SetPropertyPathByName(video, nameof(LayoutVideo.Video));
                            action.PropertyValue.Value = m_videoClip;
                        }
                        else
                        {
                            //action.PropertyValue.PropertyPath.SetPropertyPathByName(video, nameof(LayoutVideo.Texture));
                            action.PropertyValue.Value = null;
                        }
                    }
                }
            }

            private SetValueAction m_loopAction;
            protected SetValueAction LoopAction
            {
                get
                {
                    if(m_loopAction == null)
                    {
                        m_loopAction = FindAction(nameof(LayoutVideo.LoopVideo));
                        if(m_loopAction == null)
                        {
                            m_loopAction = RegisterSetValueAction(node.stepActions, nameof(LayoutVideo.LoopVideo));
                            m_loopAction.PropertyValue.Value = IsLooping;
                        }
                        else
                        {
                            m_isLooping = (bool)m_loopAction.PropertyValue.Value;
                        }
                    }
                    return m_loopAction;
                }
            }

            private bool m_isLooping;
            public bool IsLooping
            {
                get { return m_isLooping; }
                set
                {
                    if(m_isLooping != value)
                    {
                        m_isLooping = value;
                        LoopAction.PropertyValue.Value = value;
                    }
                }
            }

            private SetValueAction m_pinDescriptionAction;
            protected SetValueAction PinDescriptionAction
            {
                get
                {
                    if (m_pinDescriptionAction == null)
                    {
                        m_pinDescriptionAction = FindAction(nameof(LayoutVideo.PinDescription));
                        if (m_pinDescriptionAction == null)
                        {
                            m_pinDescriptionAction = RegisterSetValueAction(node.stepActions, nameof(LayoutVideo.PinDescription));
                            m_pinDescriptionAction.PropertyValue.Value = PinDescription;
                        }
                        else
                        {
                            m_pinDescription = (bool)m_pinDescriptionAction.PropertyValue.Value;
                        }
                    }
                    return m_pinDescriptionAction;
                }
            }

            private bool m_pinDescription;
            public bool PinDescription
            {
                get { return m_pinDescription; }
                set
                {
                    if (m_pinDescription != value)
                    {
                        m_pinDescription = value;
                        PinDescriptionAction.PropertyValue.Value = value;
                    }
                }
            }

            public VideoWrapper(LayoutVideo video) : base(video)
            {
                this.video = video;
            }

            protected override void InnerDraw(Rect rect)
            {
                float width = rect.width;
                rect.height = EditorGUIUtility.singleLineHeight;
                Video = EditorGUI.ObjectField(rect, GUIContent.none, Video, typeof(VideoClip), false) as VideoClip;
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.width = 40;
                IsLooping = GUI.Toggle(rect, IsLooping, m_loopContent, EditorStyles.miniButtonLeft);
                rect.x += rect.width;
                rect.width = width - 40;
                bool wasEnabled = GUI.enabled;
                GUI.enabled &= video.HasDescription;
                PinDescription = GUI.Toggle(rect, PinDescription, m_pinDescriptionContent, EditorStyles.miniButtonRight);
                GUI.enabled = wasEnabled;
            }

            public override float GetHeight(float width)
            {
                return base.GetHeight(width) + 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            }

            public override void Initialize(LayoutNode node)
            {
                InitializeEnterAction(node, nameof(LayoutVideo.Video));
                m_loopAction = RegisterSetValueAction(node.stepActions, nameof(LayoutVideo.LoopVideo));
                m_pinDescriptionAction = RegisterSetValueAction(node.stepActions, nameof(LayoutVideo.PinDescription));
            }

            public override void Update(LayoutNode node)
            {
                base.Update(node);
                //m_texture = action.PropertyValue.Value as Texture2D;
                m_videoClip = action.PropertyValue.Value as VideoClip;
                m_isLooping = (bool)LoopAction.PropertyValue.Value;
                m_pinDescription = (bool)PinDescriptionAction.PropertyValue.Value;
            }

            public override void UpdatePreviewValues()
            {
                if (video == null)
                {
                    video = action.PropertyValue.ObjectWrapper.Object as LayoutVideo;
                }
                //video.Texture = Texture;
                video.Video = Video;
                video.LoopVideo = IsLooping;
                video.PinDescription = PinDescription;
            }
        }

        protected class ButtonWrapper : ElementWrapper
        {
            public LayoutButton button;
            public ExitCondition condition;
            
            public bool ShouldBeVisible
            {
                get
                {
                    return m_isVisibleAction == null || (bool)m_isVisibleAction.PropertyValue.Value;
                }
                set
                {
                    if (!value)
                    {
                        IsVisibleAction.PropertyValue.Value = false;
                    }
                    else if(m_isVisibleAction != null)
                    {
                        node.stepActions.RemoveAction(m_isVisibleAction);
                    }
                }
            }

            private SetValueAction m_isVisibleAction;
            protected SetValueAction IsVisibleAction
            {
                get
                {
                    if (m_isVisibleAction == null)
                    {
                        m_isVisibleAction = FindAction(nameof(LayoutButton.IsVisible));
                        if (m_isVisibleAction == null)
                        {
                            m_isVisibleAction = RegisterSetValueAction(node.stepActions, nameof(LayoutButton.IsVisible));
                            m_isVisibleAction.PropertyValue.Value = false;
                        }
                    }
                    return m_isVisibleAction;
                }
            }

            private string m_label;
            public string Label
            {
                get { return m_label; }
                set
                {
                    if (m_label != value)
                    {
                        m_label = value;
                        action.PropertyValue.Value = m_label;
                    }
                }
            }

            public ButtonWrapper(LayoutButton button) : base(button)
            {
                this.button = button;
                m_label = button.name;
            }

            protected override void InnerDraw(Rect rect)
            {
                
            }

            public override float GetHeight(float width)
            {
                return 40;
            }

            public override void Initialize(LayoutNode node)
            {
                InitializeEnterAction(node, nameof(LayoutButton.Label));
                action.PropertyValue.Value = button.name;
                condition = node.AddExitCondition();

                var propertyWrapper = PropertyValueWrapper.CreateInstance<PropertyValueWrapper>();
                propertyWrapper.ObjectWrapper.Object = button;
                propertyWrapper.PropertyPath.SetPropertyPathByName(button, nameof(LayoutButton.Clicked));
                propertyWrapper.Value = true;
                condition.AddPropertyWrapper(propertyWrapper);
                
                //condition.PropertyWrappers[0].ObjectWrapper.Object = button;
                //condition.PropertyWrappers[0].PropertyPath.SetPropertyPath(button, nameof(LayoutButton.Clicked));
            }

            public override void Update(LayoutNode node)
            {
                base.Update(node);
                m_label = action?.PropertyValue?.Value as string;
                foreach(var condition in node.ExitConditions)
                {
                    if(condition.PropertyWrappers.Count > 0 && condition.PropertyWrappers[0].ObjectWrapper.Object == button)
                    {
                        this.condition = condition;
                        return;
                    }
                }
            }

            public override void UpdatePreviewValues()
            {
                if (button == null)
                {
                    button = action.PropertyValue.ObjectWrapper.Object as LayoutButton;
                }
                button.Label = Label;
                button.IsVisible = ShouldBeVisible;
            }
        }

        protected class InputFieldWrapper : ElementWrapper
        {
            public LayoutInputField inputField;

            public InputFieldWrapper(LayoutInputField inputField) : base(inputField)
            {
                this.inputField = inputField;
            }

            protected override void InnerDraw(Rect rect)
            {
                
            }

            public override float GetHeight(float width)
            {
                return 20;
            }

            public override void Initialize(LayoutNode node)
            {
                throw new NotImplementedException();
            }

            public override void UpdatePreviewValues()
            {
                throw new NotImplementedException();
            }
        }
    }
}

#endif