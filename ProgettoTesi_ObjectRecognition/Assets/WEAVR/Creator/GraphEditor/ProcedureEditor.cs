using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using TXT.WEAVR.License;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{

    class ProcedureEditorSettings : IWeavrSettingsClient
    {
        public string SettingsSection => "Procedure Editor";

        public IEnumerable<ISettingElement> Settings => new Setting[]{
            ("AutoTarget", true, "When creating new actions or conditions, the target is set from previous action/condition", SettingsFlags.EditableInEditor),
            ("AutoValues", true, "When creating new actions or conditions, their values will be set automatically from previous actions/conditions values", SettingsFlags.EditableInEditor),
            ("CloneNewActions", true, "If true, the newly created action copies all the values from previous action of the same type", SettingsFlags.EditableInEditor),
            ("CloneNewConditions", true, "If true, the newly created condition copies all the values from previous condition of the same type", SettingsFlags.EditableInEditor),
            ("AllowImport", false, "Whether to allow or not the import of legacy procedures", SettingsFlags.EditableInEditor),
            ("LogErrors", false, "Whether to log the errors to the console or not", SettingsFlags.EditableInEditor),
            ("DataGraphLogic", true, "Whether to use the graph logic for various calculations (e.g. reacheability, shortest-path, etc.)", SettingsFlags.EditableInEditor),
        };
    }

    public class ProcedureEditor : EditorWindow
    {
        public static ProcedureEditor Instance { get; private set; }

        private static List<(GUIContent command, System.Action action)> s_additionalButtons = new List<(GUIContent command, System.Action action)>();

        public static void RegisterCommand(GUIContent command, System.Action action)
        {
            foreach(var (com, func) in s_additionalButtons)
            {
                if(com == command && func == action)
                {
                    return;
                }
            }
            s_additionalButtons.Add((command, action));
            if (s_currentEditor && s_currentEditor.ProcedureView != null)
            {
                s_currentEditor.ProcedureView.AddAdditionalButtons((command, action));
            }
        }

        public static void UnregisterCommand(GUIContent command, System.Action action)
        {
            for (int i = 0; i < s_additionalButtons.Count; i++)
            {
                if(s_additionalButtons[i].command == command && s_additionalButtons[i].action == action)
                {
                    s_additionalButtons.RemoveAt(i);
                    if (s_currentEditor && s_currentEditor.ProcedureView != null)
                    {
                        s_currentEditor.ProcedureView.RemoveAdditionalButtons((command, action));
                    }
                    return;
                }
            }
        }

        private static ProcedureEditor s_currentEditor;

        private VisualElement m_root;
        public VisualElement Root {
            get {
                if (m_root == null)
                {
                    m_root = rootVisualElement;
                }
                return m_root;
            }
        }

        private ProcedureView m_procedureView;
        internal ProcedureView ProcedureView {
            get => m_procedureView;
            private set {
                if (m_procedureView != value)
                {
                    m_procedureView = value;
                }
            }
        }

        [SerializeField]
        private Procedure m_lastProcedure;
        public Procedure LastProcedure {
            get => m_lastProcedure;
            set {
                if (m_lastProcedure != value)
                {
                    m_lastProcedure = value;
                    if (m_lastProcedure && m_lastProcedure.ExecutionModes.Count == 0)
                    {
                        foreach (var mode in ProcedureDefaults.Current.ExecutionModes)
                        {
                            m_lastProcedure.ExecutionModes.Add(mode);
                        }
                    }
                    ProcedureView.CurrentProcedure = value;
                }
            }
        }

        [SerializeField]
        private List<Procedure> m_proceduresStack = new List<Procedure>();


        private Procedure m_beginProcedure;
        public bool HasPreviousProcedures => m_proceduresStack.Count > 0;

        public void LoadPreviousProcedure()
        {
            if(m_proceduresStack.Count > 0)
            {
                LastProcedure = m_proceduresStack.Last();
                m_proceduresStack.RemoveAt(m_proceduresStack.Count - 1);
            }
        }

        public void LoadProcedure(Procedure procedure, bool exclusive)
        {
            if (exclusive)
            {
                m_proceduresStack.Clear();
            }
            else if (LastProcedure)
            {
                m_proceduresStack.Add(LastProcedure);
            }
            LastProcedure = procedure;
        }

        public bool IsInTest => ProcedureView.Debugger.IsTestActive;

        [MenuItem("WEAVR/Procedures/Procedure Editor", priority = 0)]
        public static void ShowWindow()
        {
            GetWindow<ProcedureEditor>(); // this spawns the window and OnEnable() is fired immediately.
        }

        public static ProcedureEditor GetOrCreate()
        {
            if (!Instance)
            {
                ShowWindow();
            }
            return Instance;
        }

        public void Select(ProcedureObject item)
        {
            ProcedureView?.Select(item);
        }

        public void Highlight(ProcedureObject item, bool focusAsWell = true)
        {
            ProcedureView?.Highlight(item, focusAsWell);
        }

        public void ResetSelection()
        {
            if (ProcedureView != null)
            {
                ProcedureView.selection.Clear();
            }
        }


#if UNITY_2019_3
        private void Update()
        {
            if(ProcedureView != null && ProcedureView.CurrentProcedure && ProcedureView.CurrentProcedure.RequiresReset)
            {
                ProcedureView.Reset(false);
                ProcedureView.CurrentProcedure.RequiresReset = false;
            }
        }
#endif

        public void Select(GraphObject obj, bool focusOnElement = false)
        {
            if (ProcedureView != null)
            {
                ProcedureView.Select(obj, focusOnElement);
            }
        }

        private void OnEnable()
        {
            if (!WeavrLE.IsValid())
            {
                DestroyImmediate(this);
                return;
            }

            EditorApplication.wantsToQuit -= EditorApplication_WantsToQuit;
            EditorApplication.wantsToQuit += EditorApplication_WantsToQuit;

            Instance = this;
            Instance.minSize = new Vector2(500, 300);
            Instance.titleContent = new GUIContent("Procedure Editor", WeavrStyles.Icons["logo_icon"]);

            ProcedureView = new ProcedureView();
            ProcedureView.StretchToParentSize();

            Root.Add(ProcedureView);
            ProcedureView.AddAdditionalButtons(s_additionalButtons.ToArray());
            ProcedureView.CurrentProcedure = LastProcedure;

            //Selection.selectionChanged += SelectionChanged;
            //graphView.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f);


            //TestTutorials();

            RunProcedureAction.BeforeLoadingProcedure -= ActionLoadedProcedure;
            RunProcedureAction.BeforeLoadingProcedure += ActionLoadedProcedure;
        }

        private void OnFocus()
        {
            s_currentEditor = this;
        }

        private bool m_editorIsQuitting;
        private bool EditorApplication_WantsToQuit()
        {
            if (LastProcedure)
            {
                int response = EditorUtility.DisplayDialogComplex("Save Procedure", $"Do you want to save the procedure {LastProcedure.ProcedureName}?", "Save", "Cancel", "Don't Save");
                if (response == 1)
                {
                    // Cancel
                    return false;
                }
                if (response == 0)
                {
                    LastProcedure.Save(true);
                    //AssetDatabase.SaveAssets();
                }
            }
            m_editorIsQuitting = true;
            return true;
        }


        private void ActionLoadedProcedure(bool willLoadScene)
        {
            if (willLoadScene)
            {
                SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
                SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            }
            else
            {
                ProcedureRunner.Current.ProcedureStarted -= ProcedureRunner_ProcedureStarted;
                ProcedureRunner.Current.ProcedureStarted += ProcedureRunner_ProcedureStarted;
            }

            if (!m_beginProcedure)
            {
                m_beginProcedure = LastProcedure;
            }

            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
        }

        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.EnteredEditMode && m_beginProcedure)
            {
                LoadProcedure(m_beginProcedure, true);
                m_beginProcedure = null;
                EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            }
        }

        private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var runner = Weavr.TryGetInScene<ProcedureRunner>(scene);
            runner.ProcedureStarted -= ProcedureRunner_ProcedureStarted;
            runner.ProcedureStarted += ProcedureRunner_ProcedureStarted;
        }

        private void ProcedureRunner_ProcedureStarted(ProcedureRunner runner, Procedure procedure, ExecutionMode mode)
        {
            runner.ProcedureStarted -= ProcedureRunner_ProcedureStarted;
            LoadProcedure(procedure, !LastProcedure || procedure.ScenePath != LastProcedure.ScenePath);
        }

        [OnOpenAsset(1)]
        public static bool LoadProcedure(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is Procedure procedure)
            {
                GetOrCreate().LastProcedure = procedure;
                return true;
            }
            return false;
        }

        private void SelectionChanged()
        {
            if (Selection.activeObject is Procedure)
            {
                LastProcedure = Selection.activeObject as Procedure;
            }
            else if (Selection.activeGameObject?.GetComponent<ExecutionFlowsEngine>())
            {

            }
        }

        public static void ShowAndEdit(Procedure procedure)
        {
            GetOrCreate().LastProcedure = procedure;
        }

        private void OnDestroy()
        {
            //ProcedureView?.OnDestroy();
            if (!m_editorIsQuitting && LastProcedure && EditorUtility.DisplayDialog("Save Procedure", $"Do you want to save the procedure {LastProcedure.ProcedureName}?", "Save", "Don't Save"))
            {
                LastProcedure.Save(true);
            }
            EditorApplication.wantsToQuit -= EditorApplication_WantsToQuit;
            ProcedureTestPanel.DisableSceneInstance();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            RunProcedureAction.BeforeLoadingProcedure -= ActionLoadedProcedure;

            ProcedureObjectInspector.ResetSelection();
        }
    }
}