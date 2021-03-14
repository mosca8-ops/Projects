using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ConditionNode), true)]
    class ConditionNodeEditor : ConditionEditor
    {
        protected List<ConditionEditor> m_editorsList;
        private float[] m_heights;
        private MultiSelectionReorderableList<BaseCondition> m_reorderableList;

        public MultiSelectionReorderableList<BaseCondition> ReorderableList
        {
            get
            {
                if(m_reorderableList == null)
                {
                    s_styles.Refresh();
                    m_reorderableList = new MultiSelectionReorderableList<BaseCondition>(target, (target as ConditionNode).Children, false, true, true, false)
                    {
                        onAddDropdownCallback = List_AddElement,
                        //drawElementBackgroundCallback = List_DrawElementBackground,
                        drawElementCallback = List_DrawElement,
                        drawHeaderCallback = List_DrawHeader,
                        elementHeightCallback = List_GetElementHeight,
                        onChangedCallback = List_OnChanged,
                        headerHeight = List_GetHeaderHeight,
                        selectionColor = WeavrStyles.Colors.selection,
                        drawNotVisibleElements = true,
                        drawFooterCallback = List_DrawFooter,
                        footerHeight = s_styles.addButton.fixedHeight + s_styles.addButton.margin.vertical,
                        showDefaultBackground = false,
                        onDeleteSelection = List_DeleteSelection,
                        drawNoneElementCallback = List_NoElementsDraw,
                        onElementsPaste = List_OnElementsPaste,
                    };
                }
                return m_reorderableList;
            }
        }

        private ReorderableList.FooterCallbackDelegate m_additionalDrawFooter;

        private void List_OnElementsPaste(IEnumerable<object> pastedElements)
        {
            pastedElements?.Select(e => e as ProcedureObject).AssignProcedureToTree(m_targetCondition.Procedure, addToAssets: true);
        }

        private void List_DrawFooter(Rect rect)
        {
            if(ReorderableList.onCanAddCallback == null || ReorderableList.onCanAddCallback(ReorderableList))
            {
                Rect buttonRect = new Rect(rect.x + rect.width - s_styles.addButton.fixedWidth - s_styles.addButton.margin.right,
                                       rect.y + s_styles.addButton.margin.top,
                                       s_styles.addButton.fixedWidth,
                                       rect.height - s_styles.addButton.margin.vertical);
                if (GUI.Button(buttonRect, "+", s_styles.addButton))
                {
                    List_AddElement(buttonRect, m_reorderableList);
                }
                if(m_additionalDrawFooter != null)
                {
                    rect.width -= s_styles.addButton.fixedWidth + s_styles.addButton.margin.horizontal;
                    m_additionalDrawFooter(rect);
                }
            }
        }

        public ReorderableList.FooterCallbackDelegate List_DrawFooterCallback 
        {
            get => m_additionalDrawFooter ?? List_DrawFooter;
            set
            {
                if (value != m_additionalDrawFooter)
                {
                    m_additionalDrawFooter = value;
                }
            }
        }

        public float List_FooterHeight
        {
            get => ReorderableList.footerHeight;
            set => ReorderableList.footerHeight = value;
        }

        protected virtual void List_OnChanged(ReorderableList list)
        {
            (target as ConditionNode).Modified();
        }

        protected virtual float List_GetElementHeight(int index)
        {
            return 0 <= index && index < m_editorsList.Count ? 
                m_editorsList[index].GetHeight() + EditorGUIUtility.standardVerticalSpacing * 2 + List_SeparatorHeight : 0;
        }

        protected virtual float List_GetHeaderHeight => 0;

        protected virtual void List_DrawHeader(Rect rect)
        {
            
        }

        private void List_NoElementsDraw(Rect rect)
        {
            GUI.Label(rect, "No conditions", EditorStyles.centeredGreyMiniLabel);
        }
        protected virtual float List_SeparatorHeight => EditorGUIUtility.singleLineHeight;

        protected virtual string List_GetSeparatorText(int elementIndex, bool isActive, bool isFocused)
        {
            return "---";
        }

        protected virtual void List_DrawSeparator(Rect rect, int elementIndex, bool isActive, bool isFocused)
        {
            GUI.Label(rect, List_GetSeparatorText(elementIndex, isActive, isFocused), s_styles.separatorLabel);
        }

        protected virtual void List_DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            Element_DrawCloseButton(rect, index, isActive, isFocused);
            if (index == 0)
            {
                rect.y += List_SeparatorHeight;
            }
            else
            {
                rect.height = List_SeparatorHeight;
                List_DrawSeparator(rect, index, isActive, isFocused);
                rect.y += rect.height;
            }

            if (!CanDragChildren)
            {
                rect.x += s_styles.notToggle.fixedWidth;
                rect.width -= s_styles.notToggle.fixedWidth;
            }

            rect.y += EditorGUIUtility.standardVerticalSpacing;
            rect.height = m_heights[index];
            m_editorsList[index].DrawFull(rect);

        }

        private void List_DeleteSelection(List<BaseCondition> selection)
        {
            m_preRenderAction = () =>
            {
                var parent = target as ConditionNode;
                Undo.RegisterCompleteObjectUndo(parent, "Removed Action");
                foreach (var item in selection)
                {
                    RemoveChild(parent, item);
                }
                parent.Modified();
            };
        }

        private void Element_DrawCloseButton(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.x += rect.width - s_styles.closeButton.fixedWidth - s_styles.closeButton.margin.right;
            rect.y += s_styles.closeButton.margin.top;
            rect.width = s_styles.closeButton.fixedWidth;
            rect.height = s_styles.closeButton.fixedHeight;
            if(GUI.Button(rect, @"✕", s_styles.closeButton))
            {
                var conditionToRemove = (target as ConditionNode).Children[index];
                m_preRenderAction = () =>
                {
                    if (!target) { return; }
                    Undo.RegisterCompleteObjectUndo(target, "Removed Action");
                    RemoveChild(target as ConditionNode, conditionToRemove);
                    (target as ConditionNode).Modified();
                };
            }
        }

        private void RemoveChild(BaseCondition parent, BaseCondition condition)
        {
            bool drillDown = false;
            if(parent is IConditionsContainer c && c.Children.Remove(condition))
            {
                drillDown = true;
            }
            else if(parent is IConditionParent p && p.Child == condition)
            {
                p.Child = null;
                drillDown = true;
            }

            if (drillDown)
            {
                if (condition is IConditionsContainer container)
                {
                    foreach (var child in container.Children.ToArray())
                    {
                        RemoveChild(condition, child);
                    }
                }
                else if (condition is IConditionParent newParent && newParent.Child)
                {
                    RemoveChild(newParent as BaseCondition, newParent.Child);
                }

                ProcedureObjectEditor.DestroyEditor(condition);
                if (condition && parent.Procedure)
                {
                    parent.Procedure.Graph.ReferencesTable.RemoveTargetCompletely(condition);
                }
                condition.DestroyAsset();
            }
        }

        private void List_DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }
            if (isFocused)
            {
                EditorGUI.DrawRect(rect, WeavrStyles.Colors.selection);
            }
        }


        protected virtual void List_AddElement(Rect buttonRect, ReorderableList list)
        {
            var lastTargettedCondition = (target as ConditionNode).Children.LastOrDefault(c => (c as ITargetingObject)?.Target) as ITargetingObject;
            ShowAddConditionMenu(buttonRect, m_delayedTarget ?? lastTargettedCondition?.Target, (target as ConditionNode).Children.Count - 1);
            m_delayedTarget = null;
        }

        #region [  ADD NEW CONDITION PART  ]

        protected void ShowAddConditionMenu(Rect rect, Object target, int index)
        {
            if (ProcedureDefaults.Current.ConditionsCatalogue.DescriptorsByPath.Count <= 1)
            {
                AddCondition(ProcedureDefaults.Current.ConditionsCatalogue.DescriptorsByPath.Values.FirstOrDefault(), target, index);
                return;
            }
            AddItemWindow.Show(rect, ProcedureDefaults.Current.ConditionsCatalogue, d => AddCondition(d as ConditionDescriptor, target, index));
        }

        private void AddCondition(ConditionDescriptor conditionDescriptor, Object targetObject, int index)
        {
            m_preRenderAction = () =>
            {
                BaseCondition newCondition = null;
                if(conditionDescriptor != null && conditionDescriptor.Sample != null)
                {
                    Type type = conditionDescriptor.Sample.GetType();
                    //var sameTypeConditions = (target as ConditionNode).Children.Where(c => (c is ITargetingObject) && c.GetType() == type);
                    var lastSameCondition = WeavrEditor.Settings.GetValue("CloneNewConditions", true) ? 
                                    (target as ConditionNode).Children
                                    .LastOrDefault(c => c.GetType() == type && c.Variant == conditionDescriptor.Variant) : 
                                    null;
                    newCondition = lastSameCondition ? Instantiate(lastSameCondition) : conditionDescriptor.Create();
                }
                else
                {
                    newCondition = GenericCondition.Create(targetObject.GetGameObject());
                }

                //Undo.RegisterCreatedObjectUndo(newCondition, "Created Condition");
                if(WeavrEditor.Settings.GetValue("AutoValues", true))
                {
                    newCondition.TryAssignSceneReferences((target as ConditionNode).Children.Take(index));
                }
                if (newCondition is IConditionsContainer)
                {
                    var editor = Get(newCondition) as ConditionEditor;
                    if (WeavrEditor.Settings.GetValue("AutoTarget", true))
                    {
                        editor.DelayTargetAssignment(targetObject);
                    }
                }
                else if (newCondition is IConditionParent parent)
                {
                    var conditionAnd = ProcedureObject.Create<ConditionAnd>(m_targetCondition.Procedure);
                    //Undo.RegisterCreatedObjectUndo(conditionAnd, "Created Condition");
                    parent.Child = conditionAnd;
                    var editor = Get(conditionAnd) as ConditionEditor;
                    if (WeavrEditor.Settings.GetValue("AutoTarget", true))
                    {
                        editor.DelayTargetAssignment(targetObject);
                    }
                }
                if (newCondition is ITargetingObject targetting && !targetting.Target && WeavrEditor.Settings.GetValue("AutoTarget", true))
                {
                    targetting.Target = targetObject;
                }

                (target as ConditionNode).RegisterProcedureObject(newCondition);

                if (newCondition is ICreatedCloneCallback clone)
                {
                    clone.OnCreatedByCloning();
                }


                newCondition.Procedure = m_targetCondition.Procedure;
                (target as ConditionNode).Add(newCondition);
            };
        }

        private BaseCondition PrepareChild(GameObject target, BaseCondition child)
        {
            if (child is IConditionsContainer && (child as IConditionsContainer).Children.Count == 0)
            {
                AddNewGenericToAndCondition(target, child as IConditionsContainer);
            }
            else if (child is IConditionParent)
            {
                AddNewGenericToAndCondition(target, child as IConditionParent);
            }
            else if (child == null)
            {
                child = CreateConditionAndWithGenericCondition(target);
            }
            return child;
        }

        private static void AddNewGenericToAndCondition(GameObject target, IConditionsContainer conditionsContainer)
        {
            var genericCondition = GenericCondition.Create(target);
            Undo.RegisterCreatedObjectUndo(genericCondition, "Created Generic Condition");
            conditionsContainer.Add(genericCondition);
        }

        private static void AddNewGenericToAndCondition(GameObject target, IConditionParent conditionParent)
        {
            var genericCondition = GenericCondition.Create(target);
            Undo.RegisterCreatedObjectUndo(genericCondition, "Created Generic Condition");
            conditionParent.Child = genericCondition;
        }

        private ConditionAnd CreateConditionAndWithGenericCondition(GameObject target)
        {
            var conditionAnd = ProcedureObject.Create<ConditionAnd>(m_targetCondition.Procedure);
            Undo.RegisterCreatedObjectUndo(conditionAnd, "Created Condition");

            AddNewGenericToAndCondition(target, conditionAnd);
            return conditionAnd;
        }

        #endregion

        protected virtual float LabelWidth => 80;

        protected virtual bool CanDragChildren => true;

        protected override float HeaderHeight => 0;

        public override bool ShouldDrawIsGlobalToggle => false;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (target)
            {
                (target as ConditionNode).OnModified -= ConditionsNode_OnModified;
                (target as ConditionNode).OnModified += ConditionsNode_OnModified;
                
                SyncEditors();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (target)
            {
                (target as ConditionNode).OnModified -= ConditionsNode_OnModified;
            }
        }

        protected virtual void ConditionsNode_OnModified(ProcedureObject obj)
        {
            SyncEditors();
        }

        private void SyncEditors()
        {
            if (m_editorsList == null) { m_editorsList = new List<ConditionEditor>(); }
            else { m_editorsList.Clear(); }

            foreach (var condition in (target as ConditionNode).Children)
            {
                if (condition)
                {
                    m_editorsList.Add(Get(condition) as ConditionEditor);
                }
            }

            m_heights = new float[m_editorsList.Count];
        }

        public override void OnInspectorGUI()
        {
            DrawFullLayout();
        }

        //protected override Rect DrawNotToggle(SerializedProperty property, Rect rect)
        //{
        //    if (CanDragChildren)
        //    {
        //        var notRect = rect;
        //        notRect.width = s_styles.notToggle.fixedWidth;
        //        property.boolValue = GUI.Toggle(notRect, property.boolValue, "NOT", s_styles.notToggle);
        //        rect.x += notRect.width;
        //        return rect;
        //    }
        //    return base.DrawNotToggle(property, rect);
        //}

        protected override float GetHeightInternal()
        {
            for (int i = 0; i < m_editorsList.Count; i++)
            {
                m_heights[i] = m_editorsList[i].GetHeight();
            }
            return ReorderableList.GetHeight();
        }

        protected override void DrawProperties(Rect rect, SerializedProperty firstProperty)
        {
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LabelWidth;
            ReorderableList.DoList(rect);
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}
