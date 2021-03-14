using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(FlowCondition), true)]
    class FlowConditionEditor : ConditionEditor
    {

        FlowCondition m_flowCondition;
        GameObject m_target;
        
        private ConditionEditor m_previousEditor;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_flowCondition = target as FlowCondition;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

        }

        protected override float HeaderHeight => 0;

        protected override float GetHeightInternal()
        {
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }
            if(m_flowCondition.Condition && (m_flowCondition.Condition as IConditionsContainer)?.Children.Count > 0)
            {
                return Get(m_flowCondition.Child).GetHeight();
            }
            else if(m_flowCondition.Condition)
            {
                //Debug.Log("Eliminating object");
                m_flowCondition.Condition.DestroyAsset();
                m_flowCondition.Condition = null;
                m_flowCondition.Modified();
            }
            return EditorGUIUtility.singleLineHeight;
        }

        public override void Draw(Rect rect)
        {
            if (m_flowCondition.Condition)
            {
                var editor = Get(m_flowCondition.Condition) as ConditionEditor;
                if (m_previousEditor != editor)
                {
                    m_previousEditor = editor;
                    if (editor is ConditionNodeEditor)
                    {
                        (editor as ConditionNodeEditor).List_DrawFooterCallback = DrawFooter;
                    }
                }
                rect.height = editor.GetHeight();
                editor.DrawFull(rect);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight;
            }
            else
            {
                DrawTargetField(rect);
            }
        }

        private void DrawTargetField(Rect rect)
        {
            rect.width -= 20;
            m_target = EditorGUI.ObjectField(rect, "New Target", m_target, typeof(GameObject), true) as GameObject;

            if (m_target != null || GUI.Button(new Rect(rect.xMax, rect.y, 20, rect.height), GUIContent.none, EditorStyles.foldout))
            {
                // Check if menu is alreay being visible...
                ShowAddConditionMenu(rect, m_target, 0);
                m_target = null;
            }
        }

        private ConditionAnd CreateConditionAndWithGenericCondition(GameObject target)
        {
            var conditionAnd = ProcedureObject.Create<ConditionAnd>(m_flowCondition.Procedure);
            //Undo.RegisterCreatedObjectUndo(conditionAnd, "Created Condition");

            AddNewGenericToAndCondition(target, conditionAnd);
            return conditionAnd;
        }

        private ConditionAnd GetTopConditionAnd()
        {
            var conditionAnd = m_flowCondition.Condition as ConditionAnd;
            if (!conditionAnd)
            {
                conditionAnd = ProcedureObject.Create<ConditionAnd>(m_flowCondition.Procedure);
                //Undo.RegisterCreatedObjectUndo(conditionAnd, "Created Condition");
                //Undo.RegisterCompleteObjectUndo(m_flowCondition, "Added Condition");
                m_flowCondition.Condition = conditionAnd;
            }
            return conditionAnd;
        }

        private static void AddNewGenericToAndCondition(GameObject target, IConditionsContainer conditionsContainer)
        {
            var genericCondition = GenericCondition.Create(target);
            //Undo.RegisterCreatedObjectUndo(genericCondition, "Created Generic Condition");
            conditionsContainer.Add(genericCondition);
        }

        private static void AddNewGenericToAndCondition(GameObject target, IConditionParent conditionParent)
        {
            var genericCondition = GenericCondition.Create(target);
            //Undo.RegisterCreatedObjectUndo(genericCondition, "Created Generic Condition");
            conditionParent.Child = genericCondition;
        }

        protected void ShowAddConditionMenu(Rect rect, GameObject target, int index)
        {
            if(ProcedureDefaults.Current.ConditionsCatalogue.DescriptorsByPath.Count <= 1)
            {
                AddCondition(ProcedureDefaults.Current.ConditionsCatalogue.DescriptorsByPath.Values.FirstOrDefault(), target, index);
                return;
            }
            AddItemWindow.Show(rect, ProcedureDefaults.Current.ConditionsCatalogue, d => AddCondition(d as ConditionDescriptor, target, index));
        }

        private void AddCondition(ConditionDescriptor conditionDescriptor, GameObject target, int index)
        {
            m_preRenderAction = () =>
            {
                var newCondition = conditionDescriptor ? conditionDescriptor.Create() : GenericCondition.Create(target);
                if (newCondition is IConditionsContainer)
                {
                    var editor = Get(newCondition) as ConditionEditor;
                    editor.DelayTargetAssignment(target);
                }
                else if (newCondition is IConditionParent parent)
                {
                    var conditionAnd = ProcedureObject.Create<ConditionAnd>(m_flowCondition.Procedure);
                    //Undo.RegisterCreatedObjectUndo(conditionAnd, "Created Condition");
                    parent.Child = conditionAnd;
                    var editor = Get(conditionAnd) as ConditionEditor;
                    editor.DelayTargetAssignment(target);
                }
                if(newCondition is ITargetingObject targetting && WeavrEditor.Settings.GetValue("AutoTarget", true))
                {
                    targetting.Target = target;
                }

                m_flowCondition.RegisterProcedureObject(newCondition);

                newCondition.Procedure = m_flowCondition.Procedure;
                GetTopConditionAnd().Add(newCondition);
                m_flowCondition.Modified();
            };
        }

        private void DrawFooter(Rect rect)
        {
            rect.x += s_styles.notToggle.fixedWidth + 4;
            rect.width -= s_styles.notToggle.fixedWidth;
            rect.height = EditorGUIUtility.singleLineHeight;
            DrawTargetField(rect);
        }
    }
}
