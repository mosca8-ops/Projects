using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Procedure;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomEditor(typeof(ConditionalComponent))]
    public class ConditionalComponentEditor : UnityEditor.Editor
    {
        private BaseConditionEditor m_conditionEditor;

        private Object RootCondition
        {
            get => serializedObject.FindProperty("m_condition").objectReferenceValue;
            set
            {
                if (RootCondition != value)
                {
                    serializedObject.FindProperty("m_condition").objectReferenceValue = value;
                }
            }
        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            if (m_conditionEditor)
            {
                ProcedureObjectEditor.DestroyEditor(m_conditionEditor.target as BaseCondition);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            if (!RootCondition)
            {
                RootCondition = ProcedureObject.Create<ConditionAnd>(null);
            }
            else if (!m_conditionEditor)
            {
                m_conditionEditor = ProcedureObjectEditor.Get(RootCondition as BaseCondition);
            }

            if (m_conditionEditor)
            {
                m_conditionEditor.OnInspectorGUI();
            }

            if ((RootCondition as BaseCondition))
            {
                EditorGUILayout.HelpBox((RootCondition as BaseCondition)?.ToFullString(), MessageType.None, true);
            }

            GUILayout.Space(8);

            var property = serializedObject.FindProperty("m_autoEvaluate");
            do
            {
                EditorGUILayout.PropertyField(property);
            }
            while (property.NextVisible(false));

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }
    }
}
