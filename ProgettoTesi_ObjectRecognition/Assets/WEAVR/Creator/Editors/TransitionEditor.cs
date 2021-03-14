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
    [CustomEditor(typeof(BaseTransition), true)]
    class TransitionEditor : GraphObjectEditor, IAssetImporter, IPasteClient
    {
        private Vector2 m_scrollPosition;
        private MultiSelectionReorderableList<BaseAction> m_reorderableList;
        protected BaseTransition Transition => target as BaseTransition;

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

            public GUIStyle noActionsLabel;

            public GUIContent removeContent;

            public GUIContent transitionContent;

            public GUIStyle arrow;

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

                enterActionsContent = new GUIContent("Actions");
                exitActionsContent = new GUIContent("Exit Actions");

                noEnterActionsContent = new GUIContent("No actions");
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

                transitionContent = new GUIContent("Transition");
                arrow = WeavrStyles.EditorSkin2.FindStyle("transition_Arrow") ?? new GUIStyle("Box");
            }
        }

        private static Styles s_styles = new Styles();

        protected MultiSelectionReorderableList<BaseAction> ReorderableList
        {
            get
            {
                if (m_reorderableList == null)
                {
                    s_styles.Refresh();
                    m_reorderableList = new MultiSelectionReorderableList<BaseAction>(Transition, Transition.Actions, true, true, true, false)
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
                        onDeleteSelection = List_DeleteSelection,
                        onGetPasteIndexCallback = List_GetPasteIndex,
                        onElementsPaste = elems => elems?.Select(e => e as ProcedureObject).AssignProcedureToTree(Transition.Procedure, addToAssets: true),
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
                if (m_nextHoverIndex != value)
                {
                    m_nextHoverIndex = value;
                    if (m_nextHoverIndex < 0)
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
                if (m_justCreatedIndex != value)
                {
                    m_justCreatedIndex = value;
                    m_justCreatedHeight = 0;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            m_mouseOverIndex = -1;
            m_nextHoverIndex = -1;
            m_justCreatedIndex = -1;
            m_targetButtonHeight = 0;

            if (Transition)
            {
                Transition.OnModified -= Transition_OnModified;
                Transition.OnModified += Transition_OnModified;

                QueryActionsWithErrors();

                EditorApplication.update += UpdateAnimations;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            EditorApplication.update -= UpdateAnimations;
            if (Transition)
            {
                foreach (var elem in Transition.Actions)
                {
                    ProcedureObjectEditor.DestroyEditor(elem);
                }
            }
        }

        private void UpdateAnimations()
        {
            m_buttonHeight = Mathf.Lerp(m_buttonHeight, m_targetButtonHeight, Time.deltaTime * 2);
            bool targetReached = Mathf.Abs(m_buttonHeight - m_targetButtonHeight) < 0.5f;
            if (!targetReached || JustCreatedIndex >= 0)
            {
                ProcedureObjectInspector.Repaint(target);
            }
            else if (targetReached)
            {
                m_mouseOverIndex = m_nextHoverIndex;
            }
        }

        private void Transition_OnModified(ProcedureObject obj)
        {
            QueryActionsWithErrors();
        }

        private Rect m_scrollRect;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            if (m_scrollRect.height > 0)
            {
                ReorderableList.DoLayoutList(new Rect(m_scrollRect.position + m_scrollPosition, m_scrollRect.size), false);
            }
            else
            {
                ReorderableList.DoLayoutList();
            }
            ReorderableList.showDefaultBackground = false;

            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
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
                Undo.RegisterCompleteObjectUndo(Transition, "Added Action");
                Transition.Actions.Add(newAction);
                Transition.Modified();

                HoverIndex = -1;
                JustCreatedIndex = Transition.Actions.Count - 1;
            };
        }

        private void AddAction(ActionDescriptor actionDescriptor, int index)
        {
            m_preRenderAction = () =>
            {
                Type type = actionDescriptor.Sample.GetType();
                var previousActions = WeavrEditor.Settings.GetValue("CloneNewActions", true) ?
                                    Transition.Actions.Take(index) :
                                    new BaseAction[0];
                var lastSameAction = previousActions.LastOrDefault(a => a.GetType() == type && a.Variant == actionDescriptor.Variant);

                var newAction = lastSameAction ? Instantiate(lastSameAction) : actionDescriptor.Create();

                if (WeavrEditor.Settings.GetValue("AutoValues", false))
                {
                    newAction.TryAssignSceneReferences(previousActions);
                }
                if (newAction is ITargetingObject tAction && (WeavrEditor.Settings.GetValue("AutoTarget", true)))
                {
                    tAction.Target = (previousActions.LastOrDefault(a => a is ITargetingObject tObj && tObj.Target) as ITargetingObject)?.Target;
                }

                Transition.RegisterProcedureObject(newAction);

                if (newAction is ICreatedCloneCallback clone)
                {
                    clone.OnCreatedByCloning();
                }

                Undo.RegisterCreatedObjectUndo(newAction, "Created Action");
                Undo.RegisterCompleteObjectUndo(Transition, "Inserted Action");

                newAction.Procedure = Transition.Procedure;
                Transition.Actions.Insert(index, newAction);
                if (ProcedureObjectEditor.Get(newAction) is ISmartCreatedCallback autoFilled)
                {
                    autoFilled.OnSmartCreated(lastSameAction);
                }
                Transition.Modified();

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
            DrawAddButtonLarge(rect, Transition.Actions.Count);
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
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(index < 0)
            {
                s_styles.noActionsLabel.Draw(rect, s_styles.noEnterActionsContent, false, false, false, false);
                return;
            }

            var element = Transition.Actions[index];
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

        private void List_DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = Transition.Actions[index];
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            if (element is BaseAction)
            {
                //rect.x += s_styles.actionBox.margin.left + s_styles.actionBox.padding.left;
                rect.y += s_styles.actionBox.margin.top + s_styles.actionBox.padding.top;
                rect.height -= s_styles.actionBox.margin.vertical + s_styles.actionBox.padding.vertical;
                rect.width -= /*s_styles.actionBox.margin.horizontal + */s_styles.actionBox.padding.horizontal;
                Element_DrawRemoveButton(rect, index, isActive, isFocused);
                ActionEditor.Get(element as BaseAction).DrawFull(rect); // With RECT

                if (HoverIndex == index)
                {
                    DrawAddActionsButton(rect, index + 1);
                }
            }
            else
            {
                GUI.Label(rect, element.GetType().Name);
            }
        }

        private void DrawAddActionsButton(Rect rect, int index)
        {
            Rect buttonRect = new Rect(s_styles.addIntraActionButton.margin.left,
                                        rect.y + rect.height + s_styles.addIntraActionButton.fixedWidth * 0.5f - m_buttonHeight,
                                        s_styles.addIntraActionButton.fixedWidth,
                                        m_buttonHeight);

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

        private float List_GetElementHeight(int index)
        {
            var element = Transition.Actions[index];
            if (element is BaseAction)
            {
                float actionHeight = ActionEditor.Get(element as BaseAction).GetHeight();
                if (index == JustCreatedIndex)
                {
                    m_justCreatedHeight = Mathf.Lerp(m_justCreatedHeight, actionHeight, deltaTime);
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
            else
            {
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        //private void Element_DrawRemoveButton(Rect rect, int index, bool isActive, bool isFocused)
        //{
        //    var size = s_styles.removeButton.CalcSize(s_styles.removeContent);
        //    rect.x += rect.width - size.x + s_styles.removeButton.margin.left;
        //    rect.y += s_styles.removeButton.margin.top;
        //    rect.size = size;
        //    if (GUI.Button(rect, s_styles.removeContent, s_styles.removeButton))
        //    {
        //        m_preRenderAction = () =>
        //        {
        //            var element = Transition.Actions[index];
        //            Undo.RegisterCompleteObjectUndo(Transition, "Removed Action");
        //            if (Transition.Actions.Remove(element))
        //            {
        //                ProcedureObjectEditor.DestroyEditor(element);
        //                element.DestroyAsset();
        //            }
        //            Transition.Modified();
        //        };
        //    }
        //}

        private void List_DeleteSelection(List<BaseAction> selection)
        {
            m_preRenderAction = () =>
            {
                Undo.RegisterCompleteObjectUndo(Transition, "Removed Action");
                foreach (var element in selection)
                {
                    RemoveElement(element);
                }
                Transition.Modified();
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
                    Undo.RegisterCompleteObjectUndo(Transition, "Removed Action");
                    RemoveElement(Transition.Actions[index]);
                    Transition.Modified();
                };
            }
        }

        private void RemoveElement(BaseAction element)
        {
            if (Transition.Actions.Remove(element))
            {
                ProcedureObjectEditor.DestroyEditor(element);
                if (element && Transition.Procedure)
                {
                    Transition.Procedure?.Graph.ReferencesTable.RemoveTargetCompletely(element);
                }
                element.DestroyAsset();
            }
        }

        private void List_Changed(ReorderableList list)
        {
            //serializedObject.ApplyModifiedProperties();
            Transition.Modified();
        }
        
        protected override void DrawHeaderLayout()
        {
            s_styles.Refresh();

            bool wasEnabled = GUI.enabled;
            //GUILayout.Label("Transition", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if(Transition.From)
            {
                if(GUILayout.Button(Transition.From.Title, s_baseStyles.button))
                {
                    ProcedureEditor.Instance.Select(Transition.From, true);
                    //ProcedureObjectInspector.Selected = Transition.From;
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("None", s_baseStyles.button);
                GUI.enabled = wasEnabled;
            }

            GUILayout.Space(10);
            GUILayout.Box(s_styles.transitionContent, s_styles.arrow, GUILayout.Width(60));
            GUILayout.Space(10);

            if (Transition.To)
            {
                if (GUILayout.Button(Transition.To.Title, s_baseStyles.button))
                {
                    ProcedureEditor.Instance.Select(Transition.To, true);
                    //ProcedureObjectInspector.Selected = Transition.To;
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("None", s_baseStyles.button);
                GUI.enabled = wasEnabled;
            }

            if (m_errorActions.Count > 0)
            {
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField("Errors: " + m_errorActions.Count, s_styles.errorLabel);
                if (GUILayout.Button("Clear"))
                {
                    foreach (var action in m_errorActions)
                    {
                        action.ErrorMessage = null;
                    }
                }
                EditorGUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            //GUILayout.Label($"Weight: {serializedObject.FindProperty("m_weight").floatValue}");
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
            foreach (var element in Transition.GetFlowElements())
            {
                if (element == null) { continue; }
                element.StateChanged -= Element_StateChanged;
                element.StateChanged += Element_StateChanged;
                if (!string.IsNullOrEmpty(element.ErrorMessage))
                {
                    m_errorActions.Add(element);
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
            foreach (var elem in Transition.Actions)
            {
                if (ProcedureObjectEditor.Get(elem) is IAssetImporter worker)
                {
                    hasImported |= worker.TryImport(postImportCallback);
                }
            }
            return hasImported;
        }

        private int List_GetPasteIndex()
        {
            return Transition.Actions.Count - 1;
        }

        public void Paste(string serializedData)
        {
            m_reorderableList?.PasteSelection(serializedData);
        }
    }
}
