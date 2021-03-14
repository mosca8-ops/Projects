using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(GenericNode), true)]
    class GenericNodeEditor : GraphObjectEditor, IAssetImporter, IEditorWindowClient, IPasteClient
    {
        private const int k_ConditionEditorTopSpace = 20;
        private Vector2 m_scrollPosition;
        private MultiSelectionReorderableList<GraphObject> m_reorderableList;
        protected GenericNode Node => target as GenericNode;

        protected ConditionsContainerEditor m_conditionsContainerEditor;
        protected UnityEditor.Editor m_hintEditor;

        // Animation Variable
        private int m_mouseOverIndex;
        private int m_nextHoverIndex;
        private float m_buttonHeight;
        private float m_targetButtonHeight;

        private int m_justCreatedIndex;
        private float m_justCreatedHeight;

        private Action m_preRenderAction;

        private class Styles : BaseStyles
        {
            public GUIStyle idLabel;
            public GUIStyle errorLabel;
            public GUIStyle removeButton;
            public GUIStyle conditionsContainer;
            public GUIStyle separatorLabel;

            public GUIStyle actionBox;
            public GUIStyle redActionBox;
            public GUIStyle addIntraActionButton;
            public GUIStyle addActionButton;

            public GUIContent enterActionsContent;
            public GUIContent exitActionsContent;

            public GUIContent noEnterActionsContent;
            public GUIContent noExitActionsContent;

            public GUIContent precheck = new GUIContent("PRE-CHECK", "Whether to check the conditions before entering the node or not");
            public GUIContent mandatory = new GUIContent("MANDATORY", "Whether to make this step mandatory or not");

            public GUIStyle noActionsLabel;

            public GUIContent removeContent;

            public GUIContent mandatoryContent;

            protected override void InitializeStyles(bool isProSkin)
            {
                idLabel = WeavrStyles.EditorSkin2.FindStyle("nodeInspector_IdLabel") ?? new GUIStyle(WeavrStyles.TitleBlackHugeLabel);
                errorLabel = WeavrStyles.EditorSkin2.FindStyle("nodeInspector_ErrorLabel") ?? new GUIStyle(WeavrStyles.RedLeftLabel);
                removeButton = WeavrStyles.EditorSkin2.FindStyle("genericNode_removeButton") ?? EditorStyles.miniButton;

                conditionsContainer = WeavrStyles.EditorSkin2.FindStyle("conditionsContainer") ?? new GUIStyle("Box");
                separatorLabel = WeavrStyles.EditorSkin2.FindStyle("genericNode_separatorLabel") ??
                                        new GUIStyle(EditorStyles.centeredGreyMiniLabel);

                actionBox = WeavrStyles.EditorSkin2.FindStyle("actionBox") ?? new GUIStyle("Box");
                redActionBox = WeavrStyles.EditorSkin2.FindStyle("actionBox_Red") ?? new GUIStyle("Box");
                addIntraActionButton = WeavrStyles.EditorSkin2.FindStyle("actionIntraAddButton") ?? new GUIStyle("Button");
                addActionButton = WeavrStyles.EditorSkin2.FindStyle("actionAddButton") ?? new GUIStyle("Button");

                enterActionsContent = new GUIContent("Enter Actions");
                exitActionsContent = new GUIContent("Exit Actions");

                noEnterActionsContent = new GUIContent("No enter actions");
                noExitActionsContent = new GUIContent("No exit actions");

                noActionsLabel = WeavrStyles.EditorSkin2.FindStyle("genericNode_noActionsLabel");
                if (noActionsLabel == null)
                {
                    noActionsLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                    {
                        fixedHeight = EditorGUIUtility.singleLineHeight,
                    };
                }

                removeContent = new GUIContent(@"✕");

                mandatoryContent = new GUIContent("Mandatory");
            }
        }

        private static Styles s_styles = new Styles();

        protected MultiSelectionReorderableList<GraphObject> ReorderableList
        {
            get
            {
                if (m_reorderableList == null)
                {
                    s_styles.Refresh();
                    m_reorderableList = new MultiSelectionReorderableList<GraphObject>(Node, Node.FlowElements, true, true, true, false)
                    {
                        onChangedCallback = List_Changed,
                        elementHeightCallback = List_GetElementHeight,
                        drawElementCallback = List_DrawElement,
                        drawElementBackgroundCallback = List_DrawElementBackground,
                        drawHeaderCallback = List_DrawHeader,
                        headerHeight = s_styles.separatorLabel.fixedHeight,
                        footerHeight = s_styles.addActionButton.fixedHeight + s_styles.addActionButton.margin.vertical,
                        drawFooterCallback = List_DrawFooter,
                        drawNoneElementCallback = List_DrawNoElement,
                        onAddCallback = List_OnAdd,
                        showDefaultBackground = false,
                        onAddDropdownCallback = List_OnAddDropDownCallback,
                        isSelectable = List_IsSelectable,
                        onDeleteSelection = List_DeleteSelection,
                        onGetPasteIndexCallback = List_GetPasteIndex,
                        onElementsPaste = List_PasteElements,
                    };
                }
                return m_reorderableList;
            }
        }
        
        protected int HoverIndex
        {
            get => m_mouseOverIndex;
            set
            {
                if(m_nextHoverIndex != value)
                {
                    m_nextHoverIndex = value;
                    if(m_nextHoverIndex < 0)
                    {
                        m_targetButtonHeight = 0;
                    }
                    else
                    {
                        m_buttonHeight = 0;
                        m_mouseOverIndex = value;
                        m_targetButtonHeight = s_styles.addIntraActionButton?.fixedWidth ?? 16;
                        ProcedureObjectInspector.Repaint(target);
                    }
                }
            }
        }

        protected int JustCreatedIndex
        {
            get => m_justCreatedIndex;
            set
            {
                if(m_justCreatedIndex != value)
                {
                    m_justCreatedIndex = value;
                    m_justCreatedHeight = 0;
                }
            }
        }

        public EditorWindow Window {
            get => null;
            set => ReorderableList.Window = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_mouseOverIndex = -1;
            m_nextHoverIndex = -1;
            m_justCreatedIndex = -1;
            m_targetButtonHeight = 0;
            if (Node)
            {
                Node.OnModified -= Node_OnModified;
                Node.OnModified += Node_OnModified;

                QueryActionsWithErrors();

                EditorApplication.update += UpdateAnimations;
            }
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();

            EditorApplication.update -= UpdateAnimations;
            if (Node)
            {
                foreach(var elem in Node.FlowElements)
                {
                    ProcedureObjectEditor.DestroyEditor(elem);
                }
            }
        }
        
        private void UpdateAnimations()
        {
            m_buttonHeight = Mathf.Lerp(m_buttonHeight, m_targetButtonHeight, Time.deltaTime * 2f);
            bool targetReached = Mathf.Abs(m_buttonHeight - m_targetButtonHeight) < 0.5f;
            if (!targetReached || JustCreatedIndex >= 0)
            {
                ProcedureObjectInspector.Repaint(target);
            }
            else if(targetReached)
            {
                m_mouseOverIndex = m_nextHoverIndex;
            }
        }

        private void Node_OnModified(ProcedureObject obj)
        {
            QueryActionsWithErrors();
        }

        private Rect m_scrollRect;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 90;
            serializedObject.Update();

            if (!Node.Step)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_description"));
            }

            if (Node.Hint != null)
            {

                if (m_hintEditor == null)
                {
                    m_hintEditor = CreateEditor(Node.Hint);
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Hint", s_baseStyles.sectionLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                m_hintEditor.OnInspectorGUI();
                //GUILayout.Space(4);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                (target as GenericNode).Modified();
            }

            EditorGUIUtility.labelWidth = labelWidth;

            if (m_scrollRect.height > 0)
            {
                ReorderableList.DoLayoutList(new Rect(m_scrollRect.position + m_scrollPosition, m_scrollRect.size), true);
            }
            else
            {
                ReorderableList.DoLayoutList();
            }
            ReorderableList.showDefaultBackground = false;
            
            EditorGUILayout.EndScrollView();

            if(Event.current.type == EventType.Repaint)
            {
                m_scrollRect = GUILayoutUtility.GetLastRect();
            }

            if (HoverIndex >= 0 && Event.current.type == EventType.Repaint)
            {
                var lastRect = GUILayoutUtility.GetLastRect();
                if (lastRect.width > 10 && !lastRect.Contains(Event.current.mousePosition))
                {
                    HoverIndex = -1;
                }
            }

        }

        private void List_PasteElements(IEnumerable<object> elems)
        {
            foreach (var elem in elems)
            {
                if (elem is ProcedureObject pObj)
                {
                    pObj.AssignProcedureToTree(Node.Procedure);
                    pObj.SaveToProcedure(Node.Procedure);
                }
            }
        }

        private bool List_IsSelectable(GraphObject elem)
        {
            return elem is BaseAction;
        }
        
        private void List_OnAddDropDownCallback(Rect buttonRect, ReorderableList list)
        {
            ShowAddActionMenu(buttonRect, list.count - 1);
        }

        private void AddAction(ActionDescriptor actionDescriptor)
        {
            m_preRenderAction = () =>
            {
                var newAction = actionDescriptor.Create();
                Undo.RegisterCreatedObjectUndo(newAction, "Created Action");
                Undo.RegisterCompleteObjectUndo(Node, "Added Action");
                Node.FlowElements.Add(newAction);
                Node.Modified();

                HoverIndex = -1;
                JustCreatedIndex = Node.FlowElements.Count - 1;
            };
        }

        private void AddAction(ActionDescriptor actionDescriptor, int index)
        {
            m_preRenderAction = () =>
            {
                Type type = actionDescriptor?.Sample.GetType();
                var previousActions = WeavrEditor.Settings.GetValue("CloneNewActions", true) ? 
                                    Node.FlowElements.Take(index).Where(e => e is BaseAction).Select(e => e as BaseAction) : 
                                    new BaseAction[0];
                var lastSameAction = previousActions.LastOrDefault(a => a.GetType() == type && a.Variant == actionDescriptor.Variant);
                
                var newAction = lastSameAction ? Instantiate(lastSameAction) : actionDescriptor.Create();

                if(WeavrEditor.Settings.GetValue("AutoValues", false))
                {
                    newAction.TryAssignSceneReferences(previousActions);
                }
                if(newAction is ITargetingObject tAction && (WeavrEditor.Settings.GetValue("AutoTarget", true)))
                {
                    tAction.Target = (previousActions.LastOrDefault(a => a is ITargetingObject tObj && tObj.Target) as ITargetingObject)?.Target;
                    if (!tAction.Target)
                    {
                        tAction.Target = Selection.activeObject;
                    }
                }

                Node.RegisterProcedureObject(newAction);

                if(newAction is ICreatedCloneCallback clone)
                {
                    clone.OnCreatedByCloning();
                }

                Undo.RegisterCreatedObjectUndo(newAction, "Created Action");
                Undo.RegisterCompleteObjectUndo(Node, "Inserted Action");
                newAction.Procedure = Node.Procedure;
                Node.FlowElements.Insert(index, newAction);
                if(ProcedureObjectEditor.Get(newAction) is ISmartCreatedCallback smartCreated)
                {
                    smartCreated.OnSmartCreated(lastSameAction);
                }
                Node.Modified();

                HoverIndex = -1;
                JustCreatedIndex = index;
            };
        }

        private void List_OnAdd(ReorderableList list)
        {
            
        }

        private void List_DrawNoElement(Rect rect)
        {
            
        }

        private void List_DrawFooter(Rect rect)
        {
            rect.x += 4;
            rect.width -= 8;
            DrawAddButtonLarge(rect, Node.FlowElements.Count);
        }

        private void DrawAddButtonLarge(Rect rect, int index)
        {
            if (GUI.Button(rect, "Add Action", s_styles.addActionButton))
            {
                ShowAddActionMenu(rect, index);
            }
        }
        
        private void List_DrawHeader(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                s_styles.separatorLabel.Draw(rect, s_styles.enterActionsContent, false, false, false, false);
                m_reorderableList.showDefaultBackground = true;
            }
        }

        private void List_DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            //var min = GUIUtility.GUIToScreenPoint(rect.min);
            //var max = GUIUtility.GUIToScreenPoint(rect.max);
            //if (max.y < 0 || min.y > Screen.height + 40) { return; }

            var element = Node.FlowElements[index];
            //if (element is FlowConditionsContainer)
            //{
            //    //DrawConditionsBackground(rect, index);
            //}
            //else 
            if (element is BaseAction)
            {
                var style = string.IsNullOrEmpty((element as BaseAction).ErrorMessage) ? s_styles.actionBox : s_styles.redActionBox;
                rect.x += style.margin.left;
                rect.y += style.margin.top;
                rect.width -= style.margin.horizontal;
                rect.height -= style.margin.vertical;

                bool isHovering = rect.Contains(Event.current.mousePosition);

                style.Draw(rect, isHovering, false, isFocused, false);

                if (isHovering)
                {
                    HoverIndex = index;
                }
            }
        }

        private void DrawConditionsBackground(Rect rect, int index, bool updateRect)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (updateRect)
            {
                rect.x -= 20;
                rect.width += 30;
            }

            EditorGUI.DrawRect(rect, WeavrStyles.Colors.WindowBackground);
            
            rect.y += k_ConditionEditorTopSpace + s_styles.addActionButton.margin.vertical + s_styles.addActionButton.fixedHeight;

            rect.height -= s_styles.separatorLabel.fixedHeight 
                         + s_styles.addActionButton.fixedHeight 
                         + s_styles.addActionButton.margin.vertical
                         + k_ConditionEditorTopSpace;
            s_styles.conditionsContainer.Draw(rect, GUIContent.none, false, false, false, false);

            rect.y += rect.height;
            rect.height = s_styles.separatorLabel.fixedHeight;
            s_styles.separatorLabel.Draw(rect, s_styles.exitActionsContent, false, false, false, false);
        }

        private void List_DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            //var min = GUIUtility.GUIToScreenPoint(rect.min);
            //var max = GUIUtility.GUIToScreenPoint(rect.max);
            //if(max.y < 0 || min.y > Screen.height + 40) { return; }

            var element = Node.FlowElements[index];
            //Debug.Log($"[GUITEST]: [{index}] -> [{min.y} - {max.y}] [{rect.min.y} - {rect.max.y}] vs Screen [{Screen.height}]");
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            if (element is BaseAction)
            {
                //rect.x += s_styles.actionBox.margin.left + s_styles.actionBox.padding.left;
                rect.y += s_styles.actionBox.margin.top + s_styles.actionBox.padding.top;
                rect.height -= s_styles.actionBox.margin.vertical + s_styles.actionBox.padding.vertical;
                rect.width -= /*s_styles.actionBox.margin.horizontal + */s_styles.actionBox.padding.horizontal;
                Element_DrawRemoveButton(rect, index, isActive, isFocused);
                ProcedureObjectEditor.Get(element as BaseAction).DrawFull(rect); // With RECT

                if (HoverIndex == index)
                {
                    DrawAddActionsButton(rect, index + 1);
                }
            }
            else if(element is FlowConditionsContainer)
            {
                DrawConditionsBackground(rect, index, true);
                
                DrawAddButtonLarge(new Rect(rect.x - 16, 
                                            rect.y + s_styles.addActionButton.margin.top, 
                                            rect.width + 18, 
                                            s_styles.addActionButton.fixedHeight), 
                                   index);

                rect.height -= s_styles.separatorLabel.fixedHeight 
                             + s_styles.addActionButton.margin.vertical 
                             + s_styles.addActionButton.fixedHeight 
                             + k_ConditionEditorTopSpace;
                rect.y += s_styles.addActionButton.margin.vertical + s_styles.addActionButton.fixedHeight + k_ConditionEditorTopSpace;

                GetConditionsEditor(element).DrawFull(s_styles.conditionsContainer.GetContentRect(rect));
            }
            else
            {
                GUI.Label(rect, element.GetType().Name);
            }
        }

        private void DrawAddActionsButton(Rect rect, int index)
        {
            Rect buttonRect = new Rect( s_styles.addIntraActionButton.margin.left,
                                        rect.y + rect.height + s_styles.addIntraActionButton.fixedWidth * 0.5f - m_buttonHeight,
                                        s_styles.addIntraActionButton.fixedWidth,
                                        m_buttonHeight );

            if (Event.current.type == EventType.Repaint && buttonRect.Contains(Event.current.mousePosition))
            {
                rect.y += rect.height + s_styles.actionBox.margin.bottom * 0.5f + EditorGUIUtility.standardVerticalSpacing;
                rect.x = buttonRect.x + buttonRect.width;
                rect.height = s_styles.actionBox.margin.bottom - s_styles.actionBox.margin.top;
                EditorGUI.DrawRect(rect, s_styles.addIntraActionButton.onActive.textColor);
            }

            if (GUI.Button(buttonRect, "+", s_styles.addIntraActionButton))
            {
                ShowAddActionMenu(rect, index);
            }
        }

        protected void ShowAddActionMenu(Rect rect, int index)
        {
            AddItemWindow.Show(rect, ProcedureDefaults.Current.ActionsCatalogue, d => AddAction(d as ActionDescriptor, index));
        }

        protected ConditionsContainerEditor GetConditionsEditor(GraphObject element)
        {
            if (m_conditionsContainerEditor == null)
            {
                m_conditionsContainerEditor = CreateEditor(element) as ConditionsContainerEditor;
            }
            return m_conditionsContainerEditor;
        }

        private float List_GetElementHeight(int index)
        {
            var element = Node.FlowElements[index];
            if (element is BaseAction)
            {
                float actionHeight = ActionEditor.Get(element as BaseAction).GetHeight();
                if (index == JustCreatedIndex)
                {
                    m_justCreatedHeight = Mathf.Lerp(m_justCreatedHeight, actionHeight, Time.deltaTime * 0.45f);
                    if (Mathf.Abs(m_justCreatedHeight - actionHeight) > 0.5f)
                    {
                        actionHeight = m_justCreatedHeight;
                    }
                    else
                    {
                        JustCreatedIndex = -1;
                    }
                }
                return actionHeight
                    + EditorGUIUtility.standardVerticalSpacing
                    + s_styles.actionBox.margin.vertical
                    + s_styles.actionBox.padding.vertical; // With RECT
            }
            else if(element is FlowConditionsContainer)
            {
                return s_styles.conditionsContainer.CalcScreenHeight(GetConditionsEditor(element).GetHeight())
                    + s_styles.separatorLabel.fixedHeight
                    + EditorGUIUtility.standardVerticalSpacing
                    + k_ConditionEditorTopSpace
                    + s_styles.addActionButton.margin.vertical
                    + s_styles.addActionButton.fixedHeight;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private int List_GetPasteIndex()
        {
            return Node.FlowElements.FindIndex(o => o is FlowConditionsContainer);
        }

        private void List_DeleteSelection(List<GraphObject> selection)
        {
            m_preRenderAction = () =>
            {
                Undo.RegisterCompleteObjectUndo(Node, "Removed Action");
                foreach (var element in selection)
                {
                    RemoveElement(element);
                }
                Node.Modified();
            };
        }

        private void Element_DrawRemoveButton(Rect rect, int index, bool isActive, bool isFocused)
        {
            var size = s_styles.removeButton.CalcSize(s_styles.removeContent);
            rect.x += rect.width - size.x + s_styles.removeButton.margin.left;
            rect.y += s_styles.removeButton.margin.top;
            rect.size = size;
            if (GUI.Button(rect, s_styles.removeContent, s_styles.removeButton))
            {
                m_preRenderAction = () =>
                {
                    Undo.RegisterCompleteObjectUndo(Node, "Removed Action");
                    RemoveElement(Node.FlowElements[index]);
                    Node.Modified();
                };
            }
        }

        private void RemoveElement(GraphObject element)
        {
            if (Node.FlowElements.Remove(element))
            {
                ProcedureObjectEditor.DestroyEditor(element);
                if (element && Node.Procedure)
                {
                    Node.Procedure.Graph.ReferencesTable.RemoveTargetCompletely(element);
                }
                element.DestroyAsset();
            }
        }

        private void List_Changed(ReorderableList list)
        {
            Node.Modified();
        }

        protected override void DrawHeaderLayout()
        {
            EditorGUIUtility.labelWidth = 60;
            bool wasEnabled = GUI.enabled;
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_title"), GUILayout.ExpandWidth(true));
            GUI.enabled = !Node.Step;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_number"), GUILayout.ExpandWidth(true));
            GUI.enabled = wasEnabled;
            EditorGUILayout.EndVertical();
            
            EditorGUIUtility.labelWidth = 50;
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(80));
            var property = serializedObject.FindProperty("m_precheck");
            property.boolValue = GUILayout.Toggle(property.boolValue, s_styles.precheck, s_baseStyles.textToggle);
            property = serializedObject.FindProperty("m_isMandatory");
            property.boolValue = GUILayout.Toggle(property.boolValue, s_styles.mandatory, s_baseStyles.textToggle);
            EditorGUILayout.EndVertical();
            if (m_errorActions.Count > 0)
            {
                s_styles.Refresh();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Errors: " + m_errorActions.Count, s_styles.errorLabel);
                if (GUILayout.Button("Clear", s_baseStyles.button))
                {
                    foreach (var action in m_errorActions)
                    {
                        action.ErrorMessage = null;
                    }
                    m_errorActions.Clear();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndHorizontal();
        }

        private List<IFlowElement> m_errorActions;
        private void QueryActionsWithErrors()
        {
            if (m_errorActions == null)
            {
                m_errorActions = new List<IFlowElement>();
            }
            else
            {
                m_errorActions.Clear();
            }
            foreach (var element in Node.FlowElements)
            {
                if (element is IFlowElement flowElem)
                {
                    flowElem.StateChanged -= Element_StateChanged;
                    flowElem.StateChanged += Element_StateChanged;
                    if (!string.IsNullOrEmpty(flowElem.ErrorMessage))
                    {
                        m_errorActions.Add(flowElem);
                    }
                }
            }
        }

        private void Element_StateChanged(IFlowElement element, ExecutionState newState)
        {
            QueryActionsWithErrors();
        }

        public bool TryImport(List<Action> postImportCallback)
        {
            bool hasImported = false;
            foreach(var elem in Node.FlowElements)
            {
                if (ProcedureObjectEditor.Get(elem) is IAssetImporter worker)
                {
                    hasImported |= worker.TryImport(postImportCallback);
                }
            }
            return hasImported;
        }

        public void Paste(string serializedData)
        {
            m_reorderableList?.PasteSelection(serializedData);
        }
    }
}
