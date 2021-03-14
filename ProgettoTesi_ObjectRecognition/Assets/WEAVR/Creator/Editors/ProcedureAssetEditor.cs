using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(Procedure))]
    class ProcedureAssetEditor : GraphObjectEditor
    {
        private class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle shortFoldout;

            protected override void InitializeStyles(bool isProSkin)
            {
                shortFoldout = new GUIStyle(EditorStyles.foldout);
                shortFoldout.fixedWidth = 1;
            }
        }

        private Styles m_styles = new Styles();

        private GUIContent m_editContent;
        private GUIContent m_nameContent;
        private Procedure m_procedure;

        private List<ExecutionMode> m_helpExecutionModes;

        private UnityEditor.Editor m_refTableEditor;
        private UnityEditor.Editor m_locTableEditor;

        private Action m_preRenderAction;

        private ProcedureRunner m_runner;

        private Vector2 m_scrollPos;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_editContent = new GUIContent("Edit");
            m_nameContent = new GUIContent("Name");
            m_procedure = target as Procedure;
            m_runner = Weavr.TryGetInCurrentScene<ProcedureRunner>();
        }

        private void OnDestroy()
        {
            DestroyEditor(m_refTableEditor);
            DestroyEditor(m_locTableEditor);
        }

        private void DestroyEditor(UnityEditor.Editor editor)
        {
            if (editor)
            {
                if (Application.isPlaying) Destroy(editor);
                else DestroyImmediate(editor);
            }
        }

        public override void OnInspectorGUI()
        {
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            float labelWidth = EditorGUIUtility.labelWidth;

            serializedObject.Update();
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_procedureName"), m_nameContent);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_description"));
            EditorGUIUtility.labelWidth = labelWidth;

            if (targets.Length == 1)
            {
                if (m_helpExecutionModes == null)
                {
                    m_helpExecutionModes = m_procedure.ExecutionModes.Where(e => e.CanReplayHints).ToList();
                }
                if (m_helpExecutionModes.Count > 0)
                {
                    m_procedure.HintsReplayExecutionMode = WeavrGUILayout.Popup(this,
                                                            "Help Execution Mode",
                                                            m_procedure.HintsReplayExecutionMode,
                                                            m_helpExecutionModes, e => e.ModeName);
                }
                else
                {

                }

                GUILayout.Space(10);
                if (!ProcedureEditor.Instance || ProcedureEditor.Instance.LastProcedure != target)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Edit", GUILayout.Width(100)))
                    {
                        ProcedureEditor.ShowWindow();
                        ProcedureEditor.Instance.LastProcedure = target as Procedure;
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(10);
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            var previewImage = serializedObject.FindProperty("m_media.previewImage");
            previewImage.objectReferenceValue = EditorGUILayout.ObjectField(GUIContent.none, 
                                                                            previewImage.objectReferenceValue, 
                                                                            typeof(Texture2D), 
                                                                            false, 
                                                                            GUILayout.Width(80), 
                                                                            GUILayout.Height(80));
            GUILayout.Label("PREVIEW", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80));
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

            

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

            SerializedProperty nextProperty = null;

            var mediaProperty = serializedObject.FindProperty("m_media");
            if (mediaProperty != null)
            {
                nextProperty = mediaProperty.Copy();
                nextProperty.NextVisible(false);
                mediaProperty.Next(true);
                if(mediaProperty.name == "previewImage")
                {
                    mediaProperty.Next(false);
                }
                if (nextProperty.propertyPath != mediaProperty.propertyPath)
                {
                    EditorGUILayout.BeginVertical("Box");
                    GUILayout.Label("Media", EditorStyles.centeredGreyMiniLabel);
                    do
                    {
                        EditorGUILayout.PropertyField(mediaProperty);
                    }
                    while (mediaProperty.NextVisible(false) && mediaProperty.propertyPath != nextProperty.propertyPath);
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(10);
                }
            }

            var capabilitiesProperty = serializedObject.FindProperty("m_capabilities");
            if (capabilitiesProperty != null)
            {
                nextProperty = capabilitiesProperty.Copy();
                nextProperty.NextVisible(false);
                capabilitiesProperty.Next(true);
                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Capabilities", EditorStyles.centeredGreyMiniLabel);
                do
                {
                    EditorGUILayout.PropertyField(capabilitiesProperty);
                }
                while (capabilitiesProperty.NextVisible(false) && capabilitiesProperty.propertyPath != nextProperty.propertyPath);
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);
            }

            var networkProperty = serializedObject.FindProperty("m_networkConfig");
            if (networkProperty != null)
            {
                nextProperty = networkProperty.Copy();
                nextProperty.NextVisible(false);
                networkProperty.Next(true);
                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Network Configuration", EditorStyles.centeredGreyMiniLabel);
                do
                {
                    EditorGUILayout.PropertyField(networkProperty);
                }
                while (networkProperty.NextVisible(false) && networkProperty.propertyPath != nextProperty.propertyPath);
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);
            }

            var environmentProperty = serializedObject.FindProperty("m_environment");
            if (environmentProperty != null)
            {
                nextProperty = environmentProperty.Copy();
                nextProperty.NextVisible(false);
                environmentProperty.Next(true);
                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Environment", EditorStyles.centeredGreyMiniLabel);
                do
                {
                    if (environmentProperty.name == nameof(Procedure.Environment.additiveScenes))
                    {
                        DrawAdditiveScenesPart(environmentProperty);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(environmentProperty);
                    }
                }
                while (environmentProperty.NextVisible(false) && environmentProperty.propertyPath != nextProperty.propertyPath);
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);
            }

            if (targets.Length == 1)
            {
                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("Languages Table", EditorStyles.centeredGreyMiniLabel);
                if (!m_locTableEditor && m_procedure.LocalizationTable)
                {
                    m_locTableEditor = CreateEditor(m_procedure.LocalizationTable);
                }
                m_locTableEditor.OnInspectorGUI();
                EditorGUILayout.EndVertical();

                GUILayout.Space(10);

                EditorGUILayout.BeginVertical("Box");
                GUILayout.Label("References Table", EditorStyles.centeredGreyMiniLabel);
                if (!m_refTableEditor && m_procedure.Graph?.ReferencesTable)
                {
                    m_refTableEditor = CreateEditor(m_procedure.Graph.ReferencesTable);
                }
                m_refTableEditor.OnInspectorGUI();
                EditorGUILayout.EndVertical();
            }

            if (serializedObject.ApplyModifiedProperties() && m_procedure)
            {
                m_procedure.Modified();
            }

            if(targets.Length == 1 && m_runner && m_runner.RunningProcedure == m_procedure)
            {
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("RUNNING FLOWS");
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                foreach(var flow in m_runner.RunningFlows)
                {
                    if (flow)
                    {
                        DrawFlow(flow);
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();

            Repaint();
        }

        private void DrawAdditiveScenesPart(SerializedProperty scenesProperty)
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(scenesProperty.displayName);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add"))
            {
                scenesProperty.arraySize++;
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < scenesProperty.arraySize; i++)
            {
                DrawAdditiveScene(scenesProperty, i);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAdditiveScene(SerializedProperty array, int index)
        {
            var sceneProperty = array.GetArrayElementAtIndex(index);
            EditorGUILayout.BeginHorizontal();
            SceneAsset sceneObject = string.IsNullOrEmpty(sceneProperty.stringValue) ? 
                                    null : 
                                    AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneProperty.stringValue);

            sceneObject = EditorGUILayout.ObjectField(sceneObject, typeof(SceneAsset), false) as SceneAsset;
            sceneProperty.stringValue = sceneObject ? AssetDatabase.GetAssetPath(sceneObject) : null;
            if (sceneObject)
            {
                using (new EditorGUI.DisabledScope(!m_procedure.GetScene().isLoaded))
                {
                    var scene = SceneManager.GetSceneByPath(sceneProperty.stringValue);
                    if (scene.isLoaded)
                    {
                        if (GUILayout.Button("Unload", EditorStyles.miniButton, GUILayout.Width(60)))
                        {
                            SceneManager.UnloadSceneAsync(scene);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Load", EditorStyles.miniButton, GUILayout.Width(60)))
                        {
                            EditorSceneManager.OpenScene(sceneProperty.stringValue, OpenSceneMode.Additive);
                        }
                    }
                }
            }

            GUILayout.Space(4);
            var color = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                array.DeleteArrayElementAtIndex(index);
            }
            GUI.backgroundColor = color;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFlow(ExecutionFlow flow)
        {
            GUILayout.BeginVertical("Box");
            EditorGUILayout.ObjectField(flow, typeof(ExecutionFlow), true);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("ID", flow.Id.ToString());
            EditorGUILayout.LabelField("Is Primary", flow.IsPrimaryFlow.ToString());
            EditorGUILayout.LabelField("Current Context", flow.CurrentContext?.ToString());
            EditorGUILayout.LabelField("Next Context", flow.NextContext?.ToString());
            EditorGUILayout.LabelField("Async Elements", flow.FullyAsyncElements?.Count.ToString());
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }

        private string Clamp(string s, int maxLength, string endVal = "..")
        {
            return s.Length > maxLength ? s.Substring(0, maxLength) + endVal : s;
        }

        protected override void DrawHeaderLayout()
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Label(target.name);
            GUILayout.Space(-4);
            GUILayout.BeginHorizontal();
            GUILayout.Label(m_procedure.Configuration.Template, EditorStyles.boldLabel);
            GUILayout.Space(40);
            if (GUILayout.Button("Change"))
            {
                GenericMenu menu = new GenericMenu();
                foreach(var config in ProcedureDefaults.Current.Templates)
                {
                    if (config.Template != m_procedure.Configuration.Template || config.ShortName != m_procedure.Configuration.ShortName)
                    {
                        menu.AddItem(new GUIContent($"Change to {config.Template}"), false, () => m_preRenderAction = () => ChangeConfigTo(config));
                    }
                }
                menu.ShowAsContext();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            //if(Event.current.type == EventType.Repaint)
            //{
            //    var r = GUILayoutUtility.GetLastRect();
            //    r.y += r.height - EditorGUIUtility.singleLineHeight - 6;
            //    r.height = EditorGUIUtility.singleLineHeight;
            //    r.x += 44;
            //    GUI.Label(r, m_procedure.Configuration.Template, EditorStyles.boldLabel);
            //}
        }

        private void ChangeConfigTo(ProcedureConfig config)
        {
            var templateCopy = Instantiate(config);
            templateCopy.name = templateCopy.name.Replace("(Clone)", "");
            templateCopy.ExecutionModes.Clear();
            templateCopy.ExecutionModes.AddRange(config.ExecutionModes);
            templateCopy.DefaultExecutionMode = config.DefaultExecutionMode;
            templateCopy.HintsReplayExecutionMode = config.HintsReplayExecutionMode;

            var serObj = new SerializedObject(templateCopy);
            serObj.Update();
            serObj.FindProperty("m_templateDescription").stringValue = string.Empty;
            serObj.ApplyModifiedProperties();

            templateCopy.SaveAsAsset(m_procedure);

            serObj = new SerializedObject(m_procedure);
            serObj.Update();
            serObj.FindProperty("m_config").objectReferenceValue = templateCopy;
            serObj.ApplyModifiedProperties();

            // TODO: Make semi-automatic conversion


            //m_procedure.FullSave();
        }

        public override bool HasPreviewGUI() => true;

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            GUI.Label(r, $@"
STATISTICS: {m_procedure.Configuration.ShortName} {m_procedure.ProcedureName} 
      - Scene: {m_procedure.SceneName}
      - Steps: {m_procedure.Graph.Steps.Count + m_procedure.Graph.Nodes.Count(n => n && !n.Step && n is IProcedureStep)}
      - Languages: {m_procedure.LocalizationTable.Languages.Count}
      - References: {m_procedure.Graph.ReferencesTable.References.Count}

    GRAPH:
     - Groups: {m_procedure.Graph.Steps.Count}
     - Nodes : {m_procedure.Graph.Nodes.Count}
        - Actions   : {m_procedure.Graph.Nodes.Sum(n => (n as GenericNode)?.FlowElements.Count(f => f is BaseAction))}
        - Conditions: {m_procedure.Graph.Nodes.Sum(n => (n as IFlowProvider)?.GetFlowElements()?.Sum(f => f is FlowConditionsContainer container ? container.Conditions.Count : 0))}
     - Edges : {m_procedure.Graph.Transitions.Count}
        - Actions   : {m_procedure.Graph.Transitions.Sum(n => (n as IFlowProvider)?.GetFlowElements().Count(f => f is BaseAction))}", EditorStyles.whiteLabel);
        }
    }
}
