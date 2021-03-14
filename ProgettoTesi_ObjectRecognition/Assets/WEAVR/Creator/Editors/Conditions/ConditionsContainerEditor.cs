using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;

using System.Linq;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(FlowConditionsContainer))]
    class ConditionsContainerEditor : UnityEditor.Editor
    {
        protected class Styles : BaseStyles
        {
            public GUIStyle notToggle;
            public GUIStyle removeButton;
            public GUIStyle separatorLabel;
            public GUIStyle addButton;

            public GUIContent removeContent;

            protected override void InitializeStyles(bool isProSkin)
            {
                notToggle = WeavrStyles.EditorSkin2.FindStyle("conditionEditor_notToggle");
                removeButton = WeavrStyles.EditorSkin2.FindStyle("conditionsContainer_removeButton") ??
                                        new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                separatorLabel = WeavrStyles.EditorSkin2.FindStyle("conditionsContainer_separatorLabel") ??
                                        new GUIStyle(EditorStyles.centeredGreyMiniLabel);

                addButton = WeavrStyles.EditorSkin2.FindStyle("conditionAddButton") ?? new GUIStyle("Button");

                removeContent = new GUIContent(@"✕");
            }
        }

        protected static Styles s_styles = new Styles();

        protected Action m_prerenderAction;
        protected FlowConditionsContainer m_container;

        protected GUIContent m_newConditionLabel = new GUIContent("+ Condition");

        protected List<ConditionEditor> m_editors;
        private float[] m_heights;
        private MultiSelectionReorderableList<FlowCondition> m_reorderableList;

        private MultiSelectionReorderableList<FlowCondition> ReorderableList
        {
            get
            {
                if (m_reorderableList == null)
                {
                    s_styles.Refresh();
                    m_reorderableList = new MultiSelectionReorderableList<FlowCondition>(target, m_container.Conditions, CanDragChildren, true, true, false)
                    {
                        onAddDropdownCallback = List_AddElement,
                        //drawElementBackgroundCallback = List_DrawElementBackground,
                        drawElementCallback = List_DrawElement,
                        //drawHeaderCallback = List_DrawHeader,
                        elementHeightCallback = List_GetElementHeight,
                        onChangedCallback = List_OnChanged,
                        //headerHeight = List_GetHeaderHeight,
                        //drawElementBackgroundCallback = List_DrawElementBackground,
                        showDefaultBackground = false,
                        drawNotVisibleElements = true,
                        selectionColor = WeavrStyles.Colors.selection,
                        headerHeight = 1,
                        onElementsPaste = List_PasteElements,

                        drawFooterCallback = List_DrawFooter,
                        onDeleteSelection = List_DeleteSelection,
                        footerHeight = s_styles.addButton.fixedHeight + s_styles.addButton.margin.vertical,
                    };
                }
                return m_reorderableList;
            }
        }
        
        private void List_DrawFooter(Rect rect)
        {
            float width = s_styles.addButton.CalcSize(m_newConditionLabel).x;
            rect.x += rect.width - width - s_styles.addButton.margin.right;
            rect.width = width;
            rect.y += s_styles.addButton.margin.top;
            rect.height -= s_styles.addButton.margin.vertical;

            if(GUI.Button(rect, "+ Condition", s_styles.addButton))
            {
                List_AddElement(rect, m_reorderableList);
            }
        }

        private void List_PasteElements(IEnumerable<object> pastedElements)
        {
            var conditions = pastedElements.Where(e => e is BaseCondition && !(e is FlowCondition)).Select(e => e as BaseCondition);

            if(conditions.Count() == 0) { return; }

            int index = ReorderableList.index >= 0 && ReorderableList.index < m_container.Conditions.Count ? ReorderableList.index : m_container.Conditions.Count - 1;

            var flowCondition = m_container.Conditions[index];
            var child = flowCondition.Child;

            if(child != null && !(child is IConditionsContainer)) { return; }

            if(child == null)
            {
                child = ProcedureObject.Create<ConditionAnd>(flowCondition.Procedure);
                Undo.RegisterCreatedObjectUndo(child, "Created Condition");
                Undo.RegisterCompleteObjectUndo(flowCondition, "Added Condition");
                flowCondition.Condition = child;
            }

            child?.AssignProcedureToTree(flowCondition.Procedure, addToAssets: true);
            var childContainer = child as IConditionsContainer;
            if(childContainer == null) { return; }

            foreach(var condition in conditions)
            {
                childContainer.Add(condition);
                condition.AssignProcedureToTree(child.Procedure, addToAssets: true);
            }
        }

        private void List_OnChanged(ReorderableList list)
        {
            (target as FlowConditionsContainer).Modified();
            SyncEditors();
        }

        private float List_GetElementHeight(int index)
        {
            return m_heights.Length > 0 && index < m_heights.Length ? m_heights[index] + EditorGUIUtility.standardVerticalSpacing * 3 + List_SeparatorHeight : 0;
        }

        private float List_GetHeaderHeight => 0;

        private void List_DrawHeader(Rect rect)
        {

        }

        private float List_SeparatorHeight => s_styles.separatorLabel.fixedHeight;

        private void List_DrawSeparator(Rect rect, int elementIndex, bool isActive, bool isFocused)
        {
            GUI.Label(rect, $"Exit Condition {elementIndex + 1}", s_styles.separatorLabel);
        }

        private void List_DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if(index < 0 || index >= m_editors.Count)
            {
                return;
            }
            if (m_reorderableList.count > 1)
            {
                Element_DrawRemoveButton(rect, index, isActive, isFocused);
            }
            rect.height = List_SeparatorHeight;
            List_DrawSeparator(rect, index, isActive, isFocused);
            rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            rect.height = m_heights[index];
            m_editors[index].DrawFull(rect);
        }

        private void List_DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (isFocused)
            {
                EditorGUI.DrawRect(rect, WeavrStyles.Colors.selection);
            }
        }

        private void List_DeleteSelection(List<FlowCondition> selection)
        {
            m_prerenderAction = () =>
            {
                Undo.RegisterCompleteObjectUndo(target, "Removed Condition");
                foreach(var elem in selection)
                {
                    RemoveCondition(elem);
                }
                (target as FlowConditionsContainer).Modified();
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
                var conditionToRemove = (target as FlowConditionsContainer).Conditions[index];
                m_prerenderAction = () =>
                {
                    //Undo.RegisterCompleteObjectUndo(target, "Removed Condition");
                    RemoveCondition(conditionToRemove);
                    (target as FlowConditionsContainer).Modified();
                };
            }
        }

        private void RemoveCondition(FlowCondition condition)
        {
            var container = target as FlowConditionsContainer;
            if (container.Conditions.Remove(condition))
            {
                if (condition.Child)
                {
                    Undo.RegisterCompleteObjectUndo(condition, "Removed Action");
                    RemoveChild(condition, condition.Child);
                }
                ProcedureObjectEditor.DestroyEditor(condition);
                if (condition && container.Procedure)
                {
                    container.Procedure.Graph.ReferencesTable.RemoveTargetCompletely(condition);
                }
                condition.DestroyAsset();
            }
        }

        private void RemoveChild(BaseCondition parent, BaseCondition condition)
        {
            bool drillDown = false;
            if (parent is IConditionsContainer c && c.Children.Remove(condition))
            {
                drillDown = true;
            }
            else if (parent is IConditionParent p && p.Child == condition)
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
                if (condition)
                {
                    parent.Procedure?.Graph.ReferencesTable.RemoveTargetCompletely(condition);
                }
                condition.DestroyAsset();
            }
        }

        private void List_AddElement(Rect buttonRect, ReorderableList list)
        {
            (target as FlowConditionsContainer).Conditions.Add(ProcedureObject.Create<FlowCondition>(m_container.Procedure));
            (target as FlowConditionsContainer).Modified();
        }

        private bool CanDragChildren => true;

        private void OnEnable()
        {
            if (!target) { return; }
            m_container = target as FlowConditionsContainer;
            m_container.OnModified -= ConditionsContainerEditor_OnModified;
            m_container.OnModified += ConditionsContainerEditor_OnModified;
            SyncEditors();
        }

        private void OnDisable()
        {
            if (!target || !m_container) { return; }
            m_container.OnModified -= ConditionsContainerEditor_OnModified;
        }

        private void ConditionsContainerEditor_OnModified(ProcedureObject obj)
        {
            SyncEditors();
        }

        private void SyncEditors()
        {
            if(m_editors == null){ m_editors = new List<ConditionEditor>(); }
            else { m_editors.Clear(); }

            foreach(var condition in (target as FlowConditionsContainer).Conditions)
            {
                m_editors.Add(ProcedureObjectEditor.Get(condition) as ConditionEditor);
            }

            m_heights = new float[m_editors.Count];
        }

        public override void OnInspectorGUI()
        {
            DrawFullLayout();
        }

        public void DrawFullLayout()
        {
            
        }

        public void DrawFull(Rect rect)
        {
            s_styles.Refresh();
            if(m_prerenderAction != null)
            {
                m_prerenderAction();
                m_prerenderAction = null;
            }
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 90;
            bool wasWide = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            ReorderableList.DoList(rect);
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUIUtility.wideMode = wasWide;
        }

        public float GetHeight()
        {
            for (int i = 0; i < m_editors.Count; i++)
            {
                m_heights[i] = m_editors[i].GetHeight();
            }

            //return height;

            return ReorderableList.GetHeight();
        }
    }
}
