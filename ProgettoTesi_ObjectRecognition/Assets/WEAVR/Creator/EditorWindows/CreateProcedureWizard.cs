using System.Collections.Generic;
using System.IO;
using System.Linq;
using TXT.WEAVR.License;
using TXT.WEAVR.Localization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    public class CreateProcedureWizard : EditorWindow
    {
        public const string ResourcesRelativePath = "Creator/Resources/";
        public const string k_DefaultFolderPath = "Procedures/";

        private VisualElement m_templatesList;
        private VisualElement m_modesList;
        private VisualElement m_currentExecutionMode;
        private VisualElement m_languagesList;
        private VisualElement m_configDataContainer;
        private Label m_templateDescription;
        private Label m_errorLabel;
        private Label m_folderPath;
        private TextField m_procedureName;
        private TextField m_filename;

        private ProcedureConfig m_customTemplate;
        private ProcedureConfig m_selectedTemplate;
        private List<ExecutionMode> m_selectedModes;
        private List<Language> m_selectedLanguages;
        private UnityEditor.Editor m_execModeEditor;

        private Button m_confirmButton;
        
        public VisualElement Root => rootVisualElement;

        [MenuItem("Assets/Create/WEAVR/Procedure")]
        private static void CreateProcedure()
        {
            ShowWindow();
        }


        [MenuItem("WEAVR/Procedures/Create Procedure", priority = 0)]
        public static void ShowWindow()
        {
            ShowWizard();
        }

        private static void ShowWizard()
        {
            GetWindow<CreateProcedureWizard>(true, "Create Procedure Wizard");
        }

        static string UXMLResourceToPackage(string resourcePath)
        {
            return WeavrEditor.PATH + ResourcesRelativePath + resourcePath + ".uxml";
        }

        static string GetProcedureFullPath(string procedureName)
        {
            return k_DefaultFolderPath + procedureName;
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            if (m_selectedModes == null)
            {
                m_selectedModes = new List<ExecutionMode>();
            }
            if (m_selectedLanguages == null)
            {
                m_selectedLanguages = new List<Language>();
            }
            maxSize = minSize = new Vector2(600, 720);

            string uxmlPath = UXMLResourceToPackage("uxml/CreateProcedureWizard");
            var tpl = EditorGUIUtility.Load(uxmlPath) as VisualTreeAsset;

            VisualElement thisWindow = new VisualElement();
            tpl?.CloneTree(thisWindow);
            Root.StretchToParentSize();
            Root.Add(thisWindow);

            thisWindow.StretchToParentSize();
            thisWindow.Q("window").StretchToParentSize();
            thisWindow.AddStyleSheetPath(EditorGUIUtility.isProSkin ? "CreateProcedureWizard" : "CreateProcedureWizard_lite");

            m_errorLabel = thisWindow.Q<Label>("error-label");
            m_procedureName = thisWindow.Q<TextField>("procedure-name");
            m_procedureName.RegisterValueChangedCallback(ProcedureNameChanged);

            m_folderPath = thisWindow.Q<Label>("folderpath");
            m_folderPath.text = k_DefaultFolderPath;
            m_filename = thisWindow.Q<TextField>("filename");
            m_filename.RegisterValueChangedCallback(FilenameChanged);
            m_filename.value = m_procedureName.value = m_procedureName.text;

            m_templatesList = thisWindow.Q("templates-list");
            m_modesList = thisWindow.Q("modes-list");
            m_modesList.parent.visible = false;
            m_languagesList = thisWindow.Q("languages-list");
            m_languagesList.parent.visible = false;
            m_configDataContainer = thisWindow.Q("data-container");
            m_configDataContainer.visible = false;

            m_templateDescription = thisWindow.Q<Label>("template-description");

            m_confirmButton = thisWindow.Q<Button>("create-button");
            m_confirmButton.clickable.clicked += CreateButton_Clicked; ;
            m_confirmButton.SetEnabled(false);

            var cancelButton = thisWindow.Q<Button>("cancel-button");
            cancelButton.clickable.clicked += () => Close();

            if (!Directory.Exists($"{Application.dataPath}/{k_DefaultFolderPath}"))
            {
                Directory.CreateDirectory($"{Application.dataPath}/{k_DefaultFolderPath}");
            }

            InitializeTemplates();
            InitializeLanguages(ProcedureDefaults.Current.Languages);
        }

        private void FilenameChanged(ChangeEvent<string> evt)
        {
            UpdateConfirmButton(evt.newValue);
        }

        private void ProcedureNameChanged(ChangeEvent<string> evt)
        {
            m_filename.value = evt.newValue;
        }

        private bool ValidateProcedureFilename(string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                m_filename.AddToClassList("error");
                m_errorLabel.text = "Please insert a procedure name";
                return false;
            }
            else if (File.Exists($"{Application.dataPath}/{GetProcedureFullPath(newValue)}.asset"))
            {
                m_filename.AddToClassList("error");
                m_errorLabel.text = $"Procedure {newValue} already exists";
                return false;
            }
            else
            {
                m_filename.RemoveFromClassList("error");
                m_errorLabel.text = string.Empty;
                return true;
            }
        }

        private void CreateButton_Clicked()
        {
            var assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/" + GetProcedureFullPath(m_filename.value));
            var newProcedure = CreateInstance<Procedure>();
            newProcedure.name = m_procedureName.value;

            var templateCopy = Instantiate(m_selectedTemplate);
            templateCopy.name = templateCopy.name.Replace("(Clone)", "");
            templateCopy.ExecutionModes.Clear();
            templateCopy.ExecutionModes.AddRange(m_selectedModes);
            templateCopy.DefaultExecutionMode = m_selectedModes.Contains(m_selectedTemplate.DefaultExecutionMode) ? 
                                                m_selectedTemplate.DefaultExecutionMode :
                                                m_selectedModes.First();
            templateCopy.HintsReplayExecutionMode = m_selectedModes.Contains(m_selectedTemplate.HintsReplayExecutionMode) ?
                                                m_selectedTemplate.HintsReplayExecutionMode :
                                                m_selectedModes.FirstOrDefault(m => m.CanReplayHints) ?? m_selectedModes.First();
            var serObj = new SerializedObject(templateCopy);
            serObj.Update();
            serObj.FindProperty("m_templateDescription").stringValue = string.Empty;
            serObj.ApplyModifiedProperties();

            var localizationTable = ProcedureDefaults.Current.LocalizationTable ?
                                    Instantiate(ProcedureDefaults.Current.LocalizationTable) :
                                    CreateInstance<LocalizationTable>();
            localizationTable.name = localizationTable.name.Replace("(Clone)", "");
            localizationTable.Languages.Clear();
            localizationTable.Languages.AddRange(ProcedureDefaults.Current.Languages.Intersect(m_selectedLanguages));

            serObj = new SerializedObject(newProcedure);
            serObj.Update();
            serObj.FindProperty("m_procedureName").stringValue = m_procedureName.value;
            serObj.FindProperty("m_localizationTable").objectReferenceValue = localizationTable;
            serObj.FindProperty("m_config").objectReferenceValue = templateCopy;
            serObj.ApplyModifiedProperties();

            AssetDatabase.CreateAsset(newProcedure, assetPath + ".asset");
            newProcedure.Graph.ReferencesTable.AdaptToCurrentScene();
            newProcedure.FullSave();
            ProcedureEditor.ShowAndEdit(newProcedure);
            Selection.activeObject = newProcedure;
            Close();
        }

        private void InitializeTemplates()
        {
            foreach (var template in ProcedureDefaults.Current.Templates)
            {
                var button = new Button();
                button.clickable.clicked += () => SetTemplate(button, template);
                button.AddToClassList("selection-button");
                button.text = template.ShortName;
                button.Add(new Label(template.Template));
                m_templatesList.Add(button);
            }

            //var customTemplateButton = new Button();
            //customTemplateButton.clickable.clicked += () => SetTemplate(customTemplateButton, null);
            //customTemplateButton.AddToClassList("template-button");
            //customTemplateButton.text = "Custom";

            //m_templatesList.Add(customTemplateButton);
        }

        private void InitializeExecutionModes(IEnumerable<ExecutionMode> modes)
        {
            m_modesList.Clear();
            m_selectedModes.Clear();

            foreach (var mode in modes)
            {
                var button = new Button();
                button.clickable.clicked += () => SetActiveExecutionMode(button, mode);
                button.AddToClassList("selection-button");
                button.AddToClassList("selected");
                button.text = mode.ModeShortName;

                button.Add(new Label(mode.ModeName));

                m_modesList.Add(button);
                m_selectedModes.Add(mode);
            }

            if (modes.Count() == 0)
            {
                var customTemplateButton = new Button();
                customTemplateButton.clickable.clicked += () => SetActiveExecutionMode(customTemplateButton, null);
                customTemplateButton.AddToClassList("selection-button");
                customTemplateButton.text = "+";
                customTemplateButton.style.fontSize = 32;
                customTemplateButton.Add(new Label("Add new"));

                m_modesList.Add(customTemplateButton);
            }
        }

        private void InitializeLanguages(IEnumerable<Language> languages)
        {
            m_languagesList.Clear();
            m_selectedLanguages.Clear();

            foreach (var language in languages)
            {
                var button = new Button();
                button.clickable.clicked += () => SetActiveLanguage(button, language);
                button.AddToClassList("selection-button");
                //button.AddToClassList("selected");
                button.text = language.Name;
                button.Add(new Image() { image = language.Icon });
                button.Add(new Label(language.DisplayName));
                button.style.overflow = Overflow.Hidden;

                m_languagesList.Add(button);
                //m_selectedLanguages.Add(language);
            }

            if (languages.Count() == 0)
            {
                var customTemplateButton = new Button();
                customTemplateButton.clickable.clicked += () => SetActiveLanguage(customTemplateButton, null);
                customTemplateButton.AddToClassList("selection-button");
                customTemplateButton.text = "+";
                customTemplateButton.style.fontSize = 32;
                customTemplateButton.Add(new Label("Add new"));
                customTemplateButton.style.overflow = Overflow.Hidden;
                m_languagesList.Add(customTemplateButton);
            }
            else
            {
                m_languagesList.Q<Button>().AddToClassList("selected");
                m_selectedLanguages.Add(languages.First());
            }
        }

        private void SetActiveExecutionMode(Button button, ExecutionMode mode)
        {
            if (m_selectedModes.Contains(mode))
            {
                m_selectedModes.Remove(mode);
                button.RemoveFromClassList("selected");
            }
            else
            {
                m_selectedModes.Add(mode);
                button.AddToClassList("selected");
            }
            //foreach (var b in m_modesList.Query<Button>().ToList())
            //{
            //    b.RemoveFromClassList("selected");
            //}
            //button.AddToClassList("selected");

            UpdateConfirmButton();
        }

        private void SetActiveLanguage(Button button, Language language)
        {
            if (m_selectedLanguages.Contains(language))
            {
                m_selectedLanguages.Remove(language);
                button.RemoveFromClassList("selected");
            }
            else
            {
                m_selectedLanguages.Add(language);
                button.AddToClassList("selected");
            }
            //foreach (var b in m_modesList.Query<Button>().ToList())
            //{
            //    b.RemoveFromClassList("selected");
            //}
            //button.AddToClassList("selected");

            UpdateConfirmButton();
        }

        private void SetTemplate(Button button, ProcedureConfig template)
        {
            m_modesList.parent.visible = true;
            m_languagesList.parent.visible = true;

            foreach (var b in m_templatesList.Query<Button>().ToList())
            {
                b.RemoveFromClassList("selected");
            }
            button.AddToClassList("selected");

            if (template)
            {
                m_selectedTemplate = template;
                m_configDataContainer.visible = false;
                InitializeExecutionModes(template.ExecutionModes);
                m_templateDescription.text = template.TemplateDescription;
            }
            else
            {
                //InitializeExecutionModes(ProcedureDefaults.Current.ExecutionModes);
                if (!m_customTemplate)
                {
                    m_customTemplate = CreateInstance<ProcedureConfig>();
                }
                m_selectedTemplate = m_customTemplate;
                m_configDataContainer.visible = true;
                InitializeExecutionModes(new ExecutionMode[0]);
                m_templateDescription.text = "Create a completely custom procedure";
            }

            UpdateConfirmButton();
        }

        private void UpdateConfirmButton(string procedureFileName = null)
        {
            m_confirmButton.SetEnabled(ValidateProcedureFilename(procedureFileName ?? m_filename.text)
                && m_selectedModes.Count > 0
                && m_selectedLanguages.Count > 0);
        }

        private void OnLostFocus()
        {
            //if (m_actionsCatalogueEditor)
            //{
            //    DestroyImmediate(m_actionsCatalogueEditor);
            //}
            //m_actionsCatalogueEditor = null;
        }

        private void OnDestroy()
        {
            if (m_customTemplate)
            {
                DestroyImmediate(m_customTemplate);
            }
        }
    }
}
