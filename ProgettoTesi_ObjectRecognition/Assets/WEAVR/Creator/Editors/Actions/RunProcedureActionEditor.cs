using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(RunProcedureAction), true)]
    class RunProcedureActionEditor : ActionEditor
    {
        private GUIContent m_procedureLabel = new GUIContent("Procedure");
        private GUIContent m_execModeLabel = new GUIContent("Mode");
        private GUIContent m_resetSceneLabel = new GUIContent("Reset Scene");

        protected RunProcedureAction Action { get; private set; }
        protected bool? m_sceneIsValid;

        private Action m_setModeAction;

        protected override void OnEnable()
        {
            base.OnEnable();
            Action = target as RunProcedureAction;

            EditorBuildSettings.sceneListChanged -= EditorBuildSettings_sceneListChanged;
            EditorBuildSettings.sceneListChanged += EditorBuildSettings_sceneListChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EditorBuildSettings.sceneListChanged -= EditorBuildSettings_sceneListChanged;
        }

        private void EditorBuildSettings_sceneListChanged()
        {
            if(Action && Action.ProcedureToRun && Action.Procedure && Action.ProcedureToRun.ScenePath != Action.Procedure.ScenePath)
            {
                m_sceneIsValid = EditorBuildSettings.scenes.Any(s => s.path == Action.ProcedureToRun.ScenePath);
            }
        }

        protected override void DrawProperties(Rect rect, SerializedProperty targetProperty)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;

            var procedureProperty = serializedObject.FindProperty("m_procedureToRun");
            var modeProperty = serializedObject.FindProperty("m_executionMode");

            if(m_setModeAction != null)
            {
                m_setModeAction();
                m_setModeAction = null;
            }

            rect.height = EditorGUIUtility.singleLineHeight;
            var procedureRect = procedureProperty.objectReferenceValue ? new Rect(rect.x, rect.y, rect.width - 42, rect.height) : rect;
            var newProcedure = EditorGUI.ObjectField(procedureRect, m_procedureLabel, procedureProperty.objectReferenceValue, typeof(Procedure), true) as Procedure;
            
            if(procedureProperty.objectReferenceValue && GUI.Button(new Rect(rect.xMax - 40, rect.y, 40, rect.height), "Open", EditorStyles.miniButton))
            {
                if(EditorUtility.DisplayDialog("Open Procedure", $"Are you sure you want to open {(procedureProperty.objectReferenceValue as Procedure).ProcedureName} ?", "Open", "Cancel"))
                {
                    ProcedureEditor.Instance.LoadProcedure(procedureProperty.objectReferenceValue as Procedure, false);
                    return;
                }
            }
            else if (newProcedure != procedureProperty.objectReferenceValue)
            {
                procedureProperty.objectReferenceValue = newProcedure;
                modeProperty.objectReferenceValue = null;
                m_sceneIsValid = null;
            }

            if (newProcedure)
            {
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var buttonRect = EditorGUI.PrefixLabel(rect, m_execModeLabel);
                if (GUI.Button(buttonRect, modeProperty.objectReferenceValue ? (modeProperty.objectReferenceValue as ExecutionMode).ModeName : "Current", EditorStyles.popup))
                {
                    GenericMenu menu = new GenericMenu();
                    if (newProcedure.ExecutionModes.Contains(Action.Procedure.DefaultExecutionMode))
                    {
                        menu.AddItem(new GUIContent("Current"), 
                                     !modeProperty.objectReferenceValue, 
                                     () => m_setModeAction = () => serializedObject.FindProperty("m_executionMode").objectReferenceValue = null);
                    }
                    foreach(var mode in newProcedure.ExecutionModes)
                    {
                        menu.AddItem(new GUIContent(mode.ModeName), 
                                        mode == modeProperty.objectReferenceValue, 
                                        () => m_setModeAction = () => serializedObject.FindProperty("m_executionMode").objectReferenceValue = mode);
                    }
                    menu.DropDown(buttonRect);
                }

                if(newProcedure.ScenePath == Action.Procedure.ScenePath)
                {
                    var resetScene = serializedObject.FindProperty("m_resetScene");
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, resetScene);
                }
            }

            if(m_sceneIsValid == false && newProcedure)
            {
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                rect.height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.HelpBox(rect, $"Please add the {newProcedure.SceneName} to build settings", MessageType.Warning);
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        protected override float GetHeightInternal()
        {
            var line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (!Action.ProcedureToRun)
            {
                return line;
            }
            if (Action.Procedure.ScenePath != Action.ProcedureToRun.ScenePath)
            {
                if (!m_sceneIsValid.HasValue)
                {
                    m_sceneIsValid = EditorBuildSettings.scenes.Any(s => s.path == Action.ProcedureToRun.ScenePath);
                }
                return m_sceneIsValid == true ? line * 2 : line * 4;
            }
            return line * 3;
        }
    }
}
