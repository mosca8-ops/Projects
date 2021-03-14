using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{

    public class LoadProcedureWizard : EditorWindow
    {
        public const string ResourcesRelativePath = "Creator/Resources/";

        private Toggle m_currentScene;
        private Toggle m_allScenes;
        private Label m_noProceduresLabel;
        private VisualElement m_proceduresList;

        private List<Procedure> m_loadedProcedures;

        private VisualTreeAsset m_procedureButtonTemplate;

        private Action<Procedure> m_currentCallback;

        public VisualElement Root => rootVisualElement;

        public IReadOnlyList<Procedure> AllProcedures
        {
            get
            {
                if(m_loadedProcedures == null)
                {
                    m_loadedProcedures = AssetDatabase.FindAssets($"t:{nameof(Procedure)}")
                        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                        .OrderByDescending(path => File.GetLastWriteTime(GetFullPath(path)))
                        .Select(path => AssetDatabase.LoadAssetAtPath<Procedure>(path))
                        .ToList();
                }
                return m_loadedProcedures;
            }
        }

        //[MenuItem("WEAVR/Procedures/Load Procedure")]
        public static void ShowWindow()
        {
            Show(true, ProcedureEditor.ShowAndEdit).ShowUtility();
        }

        public static void Show(Action<Procedure> loadCallback)
        {
            Show(true, loadCallback).Show();//.ShowAuxWindow();
        }

        private static LoadProcedureWizard Show(bool asTool, Action<Procedure> loadCallback)
        {
            //var procedureLoader = GetWindow<LoadProcedureWizard>(asTool, "Load Procedure");
            var procedureLoader = CreateInstance<LoadProcedureWizard>();
            procedureLoader.titleContent = new GUIContent("Load Procedure");
            procedureLoader.m_currentCallback = loadCallback;
            return procedureLoader;
        }

        static string UXMLResourceToPackage(string resourcePath)
        {
            return WeavrEditor.PATH + ResourcesRelativePath + resourcePath + ".uxml";
        }

        private void OnEnable()
        {
            if (!License.WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }
            
            maxSize = minSize = new Vector2(600, 420);

            string uxmlPath = UXMLResourceToPackage("uxml/LoadProcedureWizard");
            var tpl = EditorGUIUtility.Load(uxmlPath) as VisualTreeAsset;

            m_procedureButtonTemplate = EditorGUIUtility.Load(UXMLResourceToPackage("uxml/LoadProcedureButton")) as VisualTreeAsset;

            VisualElement thisWindow = new VisualElement();
            tpl?.CloneTree(thisWindow);
            Root.StretchToParentSize();
            Root.Add(thisWindow);

            thisWindow.StretchToParentSize();
            thisWindow.Q("window").StretchToParentSize();
            thisWindow.AddStyleSheetPath(EditorGUIUtility.isProSkin ? "LoadProcedureWizard" : "LoadProcedureWizard_lite");

            m_currentScene = thisWindow.Q<Toggle>("current-scene");
            m_allScenes = thisWindow.Q<Toggle>("all-scenes");

            m_currentScene.RegisterValueChangedCallback(evt => RebuildList(evt.newValue));
            m_allScenes.RegisterValueChangedCallback(evt => RebuildList(!evt.newValue));

            m_currentScene.Q("unity-checkmark").RemoveFromHierarchy();
            m_allScenes.Q("unity-checkmark").RemoveFromHierarchy();

            m_noProceduresLabel = thisWindow.Q<Label>("no-procedures-label");
            m_proceduresList = thisWindow.Q("procedures-list");

            RebuildList(true);
        }

        private void RebuildList(bool currentScene)
        {
            m_currentScene.SetValueWithoutNotify(currentScene);
            m_allScenes.SetValueWithoutNotify(!currentScene);

            m_proceduresList.Clear();

            m_noProceduresLabel.visible = AllProcedures.Count == 0;

            //m_proceduresList.Add(new ListView(m_loadedProcedures, 40, CreateProcedureButton, BindProcedureItem));
            var currentScenePath = SceneManager.GetActiveScene().path;
            var procedures = currentScene ? AllProcedures.Where(p => p.ScenePath == currentScenePath) : AllProcedures;

            foreach (var procedure in procedures)
            {
                if (procedure && procedure.Configuration && procedure.Graph)
                {
                    var procedureButton = CreateProcedureButton(procedure, () =>
                    {
                        m_currentCallback?.Invoke(procedure);
                        Close();
                    });
                    m_proceduresList.Add(procedureButton);
                }
            }

        }

        private VisualElement CreateProcedureButton(Procedure procedure, Action callback)
        {
            VisualElement button = new VisualElement();
            m_procedureButtonTemplate.CloneTree(button);
            BindProcedureItem(procedure, button.Q<Button>(), callback);

            return button;
        }

        private VisualElement CreateProcedureButton()
        {
            VisualElement button = new VisualElement();
            m_procedureButtonTemplate.CloneTree(button);
            return button;
        }

        private void BindProcedureItem(VisualElement element, int index)
        {
            if(element is Button button && index >= 0 && index < AllProcedures.Count)
            {
                BindProcedureItem(AllProcedures[index], button, () =>
                {
                    m_currentCallback?.Invoke(AllProcedures[index]);
                    Close();
                });
            }
        }

        private static void BindProcedureItem(Procedure procedure, Button button, Action callback)
        {
            button.clickable.clicked -= callback;
            button.clickable.clicked += callback;
            button.Q<Label>("config-type").text = procedure.Configuration?.ShortName;
            var execModes = button.Q("exec-modes");
            foreach(var execMode in procedure.ExecutionModes)
            {
                var modeLabel = new Label(execMode.ModeShortName);
                modeLabel.AddToClassList("exec-mode");
                execModes.Add(modeLabel);
            }

            button.Q<Label>("procedure-name").text = procedure.ProcedureName;
            button.Q<Label>("procedure-path").text = AssetDatabase.GetAssetPath(procedure);
            button.Q<Label>("procedure-scene").text = procedure.ScenePath;
            button.Q<Label>("nodes-count").text = procedure.Graph.Nodes.Count.ToString();
            var creationTime = File.GetCreationTime(GetFullPath(procedure));
            button.Q<Label>("procedure-date").text = $"{creationTime.ToShortDateString()} {creationTime.ToShortTimeString()}";

            var modifiedTime = File.GetLastWriteTime(GetFullPath(procedure));
            button.Q<Label>("procedure-modified-date").text = $"{modifiedTime.ToShortDateString()} {modifiedTime.ToShortTimeString()}";

            var languages = button.Q("languages");
            if (procedure.LocalizationTable)
            {
                foreach (var lang in procedure.LocalizationTable.Languages)
                {
                    if (lang.Icon)
                    {
                        var langImage = new Image();
                        langImage.image = lang.Icon;
                        langImage.AddToClassList("lang-icon");
                        langImage.AddToClassList("lang-element");
                        languages.Add(langImage);
                    }
                    else
                    {
                        var langLabel = new Label(lang.TwoLettersISOName);
                        langLabel.AddToClassList("lang-label");
                        langLabel.AddToClassList("lang-element");
                        languages.Add(langLabel);
                    }
                }
            }

            if (string.IsNullOrEmpty(procedure.Description))
            {
                button.Q<Label>("procedure-description").RemoveFromHierarchy();
            }
            else
            {
                button.Q<Label>("procedure-description").text = procedure.Description;
            }
        }
        
        private static string GetFullPath(string assetsPath) => Path.Combine(Application.dataPath.Replace("Assets", ""), assetsPath);
        private static string GetFullPath(Procedure procedure) => Path.Combine(Application.dataPath.Replace("Assets", ""), AssetDatabase.GetAssetPath(procedure));
    }
}