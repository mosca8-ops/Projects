using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ProcedureStateManager))]
    public class ProcedureStateManagerEditor : UnityEditor.Editor
    {
        private ProcedureStateManager m_serializationManager;
        private bool m_foldout;
        private List<IProcedureStep> m_steps;

        private void OnEnable()
        {
            m_serializationManager = (ProcedureStateManager)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            m_serializationManager.SaveState = EditorGUILayout.Toggle("Save State", m_serializationManager.SaveState);

            if (m_serializationManager.SaveState)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Save state in progress.");
            }
            else
            {
                if (EditorApplication.isPlaying)
                {
                    if (ProcedureRunner.Current != null && ProcedureRunner.Current.CurrentProcedure != null)
                    {
                        DrawSteps();
                    }
                    else
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("No procedure selected.");
                    }
                }
                else
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Playmode needed to show steps.");
                }
            }
        }

        private void DrawSteps()
        {
            if (m_steps == null)
                m_steps = GetSteps();

            m_foldout = EditorGUILayout.Foldout(m_foldout, "Procedure Steps");

            if (m_foldout)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("Box");

                foreach (var step in m_steps)
                {
                    GUILayout.Space(5f);
                    if (GUILayout.Button(step.Number + " - " + step.Title))
                        m_serializationManager.GoToStep(step.StepGUID);
                }

                EditorGUILayout.EndVertical();
            }
        }

        private List<IProcedureStep> GetSteps()
        {
            var steps = new List<IProcedureStep>();
            foreach (var node in ProcedureRunner.Current.CurrentProcedure.Graph.Nodes)
            {
                if (node is GenericNode genericNode)
                {
                    if (!steps.Contains(genericNode.ProcedureStep) && !ProcedureStateUtility.IsStringNullEmptyOrWhitespace(genericNode.ProcedureStep.Number))
                        steps.Add(genericNode.ProcedureStep);
                }
            }
            steps = steps.OrderBy(s => ProcedureStateUtility.PadNumbers(s.Number)).ToList();
            return steps;
        }

    }
}

