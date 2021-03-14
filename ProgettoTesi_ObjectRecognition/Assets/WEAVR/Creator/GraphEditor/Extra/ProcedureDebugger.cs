using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TXT.WEAVR.Procedure
{

    public class ProcedureDebugger : VisualElement
    {
        private const string k_tempProcedureName = "_ProcedureTester_";

        ProcedureHierarchyDrawer m_hierarchyDrawer;
        private Procedure m_procedure;
        private bool m_isTesting;
        private bool m_testingWasAltered;

        public event Action<ProcedureDebugger, bool> InTestChanged;
        public event Action<ProcedureDebugger, bool> FastForwardDebugChanged;
        public event Action<ProcedureDebugger, bool> SaveStateChanged;

        public Procedure Procedure
        {
            get => m_procedure;
            set
            {
                if (m_procedure != value)
                {
                    m_testingWasAltered = false;
                    m_procedure = value;
                    HierarchyDrawer.Procedure = m_procedure;
                    ClearRunnerProcedureIfNeeded();
                    Clear();
                    Rebuild();
                }
            }
        }

        public ProcedureHierarchyDrawer HierarchyDrawer
        {
            get
            {
                if (m_hierarchyDrawer == null)
                {
                    m_hierarchyDrawer = new ProcedureHierarchyDrawer();
                }
                return m_hierarchyDrawer;
            }
        }

        private ProcedureRunner m_procedureRunner;
        public ProcedureRunner CurrentProcedureRunner
        {
            get
            {
                if (!m_procedureRunner)
                {
                    m_procedureRunner = Weavr.GetInCurrentScene<ProcedureRunner>();
                }
                return m_procedureRunner;
            }
        }

        private ProcedureRunner m_tempProcedureRunner;
        public ProcedureRunner TempProcedureRunner
        {
            get
            {
                if (!m_tempProcedureRunner)
                {
                    var alreadyExisting = GameObject.Find(k_tempProcedureName);
                    if (alreadyExisting)
                    {
                        m_tempProcedureRunner = alreadyExisting.GetComponent<ProcedureRunner>();
                    }
                    if (!m_tempProcedureRunner)
                    {
                        m_tempProcedureRunner = new GameObject(k_tempProcedureName).AddComponent<ProcedureRunner>();
                        //m_tempProcedureRunner.gameObject.hideFlags |= HideFlags.DontSave;
                    }
                    HierarchyDrawer.ProcedureRunner = m_tempProcedureRunner;
                }
                return m_tempProcedureRunner;
            }
        }

        private ProcedureTestPanel m_testPanel;
        public ProcedureTestPanel TestPanel
        {
            get
            {
                if (!m_testPanel)
                {
                    foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        m_testPanel = root.GetComponentInChildren<ProcedureTestPanel>(true);
                        if (m_testPanel) { break; }
                    }
                }
                return m_testPanel;
            }
        }

        private ProcedureStateManager m_procedureStateManager;
        public ProcedureStateManager ProcedureStateManager
        {
            get
            {
                if (!m_procedureStateManager)
                {
                    m_procedureStateManager = Weavr.GetInCurrentScene<ProcedureStateManager>();
                }
                return m_procedureStateManager;
            }
        }

        public bool IsTestActive
        {
            get => m_isTesting;
            set
            {
                if (m_isTesting != value)
                {
                    m_isTesting = value;
                    TestCurrentProcedure(m_isTesting);
                    this.Q<Toggle>("fast-forward-toggle")?.SetEnabled(IsTestActive);
                    IsFastForwardDebugActive = m_procedureRunner && m_procedureRunner.StartFromDebugStep;
                    this.Q<Toggle>("save-state-toggle")?.SetEnabled(IsTestActive);
                    IsSaveStateActive = m_procedureRunner && ProcedureStateManager.SaveState;
                    EditorApplication.RepaintHierarchyWindow();

                    InTestChanged?.Invoke(this, m_isTesting);

                    m_testingWasAltered = true;
                }
            }
        }

        public bool IsFastForwardDebugActive
        {
            get => CurrentProcedureRunner && CurrentProcedureRunner.CurrentProcedure == m_procedure && CurrentProcedureRunner.StartFromDebugStep;
            set
            {
                if (CurrentProcedureRunner && CurrentProcedureRunner.CurrentProcedure == m_procedure)
                {
                    value &= IsTestActive;
                    this.Q<Toggle>("fast-forward-toggle")?.SetValueWithoutNotify(value);
                    CurrentProcedureRunner.StartFromDebugStep = value;

                    FastForwardDebugChanged?.Invoke(this, value);
                }
            }
        }

        public bool IsSaveStateActive
        {
            get => CurrentProcedureRunner && CurrentProcedureRunner.CurrentProcedure == m_procedure && ProcedureStateManager.SaveState;
            set
            {
                if (CurrentProcedureRunner && CurrentProcedureRunner.CurrentProcedure == m_procedure)
                {
                    value &= IsTestActive;
                    this.Q<Toggle>("save-state-toggle")?.SetValueWithoutNotify(value);
                    ProcedureStateManager.SaveState = value;

                    SaveStateChanged?.Invoke(this, value);
                }
            }
        }

        public ProcedureDebugger()
        {
            name = "procedure-debugger";
            this.AddStyleSheetPath("ProcedureDebugger");

            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);
            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);

            EditorApplication.hierarchyWindowItemOnGUI -= DrawProcedurePlay;
            EditorApplication.hierarchyWindowItemOnGUI += DrawProcedurePlay;

            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
        }

        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange state)
        {
            //if (state == PlayModeStateChange.EnteredPlayMode && Procedure)
            //{
            //    if(Procedure.Graph.DebugStartNodes.Count > 0)
            //    {
            //        Debug.Log("Starting from debug step..");
            //        TempProcedureRunner.StartFromDebugStep = true;
            //        //TempProcedureRunner.StartProcedureFromDebugStep(Procedure, null);
            //    }
            //}
        }

        private void DrawProcedurePlay(int instanceID, Rect rect)
        {
            if (m_isTesting && Event.current.type == EventType.Repaint)
            {
                HierarchyDrawer.DrawProcedureHierarchyElement(instanceID, rect, m_tempProcedureRunner);
            }
        }

        private void AttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.hierarchyWindowItemOnGUI -= DrawProcedurePlay;
            EditorApplication.hierarchyWindowItemOnGUI += DrawProcedurePlay;

            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;

            if (IsTestActive && TestPanel)
            {
                TestPanel.gameObject.SetActive(true);
            }
        }

        private void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            Cleanup();
            EditorApplication.hierarchyWindowItemOnGUI -= DrawProcedurePlay;
            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
        }

        private void Rebuild()
        {
            if (!Procedure) { return; }

            m_procedureRunner = m_procedureRunner ? m_procedureRunner : Weavr.TryGetInCurrentScene<ProcedureRunner>();
            if (CurrentProcedureRunner)
            {
                if (CurrentProcedureRunner.CurrentProcedure == Procedure)
                {
                    IsTestActive = CurrentProcedureRunner.StartWhenReady;
                    IsFastForwardDebugActive = CurrentProcedureRunner.StartFromDebugStep;
                    IsSaveStateActive = ProcedureStateManager.SaveState;
                }
                else
                {
                    m_procedureRunner = null;
                }
            }

            Toggle toggleProcedureTest = new ToolbarToggle();
            toggleProcedureTest.name = "procedure-test-toggle";
            toggleProcedureTest.text = "Test";
            toggleProcedureTest.AddToClassList("focus-toggle");
            toggleProcedureTest.RegisterCallback<ChangeEvent<bool>>(e => IsTestActive = e.newValue);
            toggleProcedureTest.SetValueWithoutNotify(m_isTesting);
            Add(toggleProcedureTest);

            PopupField<ExecutionMode> executionModes = new PopupField<ExecutionMode>(Procedure.ExecutionModes,
                                                                Procedure.ValidDefaultExecutionMode,
                                                                e => e.ModeName, e => e.ModeName);
            executionModes.RegisterValueChangedCallback(OnExecutionModeChanged);
            executionModes.name = "execModePopup";
            Add(executionModes);


            Toggle toggleFastForward = new ToolbarToggle();
            toggleFastForward.name = "fast-forward-toggle";
            toggleFastForward.tooltip = "Toggle Fast-Forward Debug";
            toggleFastForward.RegisterCallback<ChangeEvent<bool>>(e => IsFastForwardDebugActive = e.newValue);
            toggleFastForward.Children().FirstOrDefault()?.Add(new Image()
            {
                name = "fast-forward-icon"
            });
            Add(toggleFastForward);

            toggleFastForward.SetValueWithoutNotify(IsFastForwardDebugActive);
            toggleFastForward.SetEnabled(IsTestActive);

            Toggle toggleSaveState = new ToolbarToggle();
            toggleSaveState.name = "save-state-toggle";
            toggleSaveState.tooltip = "Toggle Save State";
            toggleSaveState.AddToClassList("toolbarItem");
            toggleSaveState.RegisterCallback<ChangeEvent<bool>>(e => IsSaveStateActive = e.newValue);
            toggleSaveState.Children().FirstOrDefault()?.Add(new Image()
            {
                name = "save-state-icon"
            });
            Add(toggleSaveState);

            toggleSaveState.SetValueWithoutNotify(IsSaveStateActive);
            toggleSaveState.SetEnabled(IsTestActive);

            if (IsTestActive && TestPanel)
            {
                TestPanel.gameObject.SetActive(true);
            }
        }

        private void OnExecutionModeChanged(ChangeEvent<ExecutionMode> evt)
        {
            if (Procedure)
            {
                Procedure.DefaultExecutionMode = evt.newValue;
            }
        }

        private void TestCurrentProcedure(bool value)
        {
            var toggle = this.Q<Toggle>("procedure-test-toggle");
            toggle?.SetValueWithoutNotify(value);
            if (value)
            {
                if (TestPanel)
                {
                    TestPanel.gameObject.SetActive(true);
                }
                else if (ProcedureDefaults.Current.ProcedureCanvasTester && ProcedureDefaults.Current.ProcedureCanvasTester.TestPanelSample)
                {
                    var sceneGO = GameObject.Instantiate(ProcedureDefaults.Current.ProcedureCanvasTester.TestPanelSample.gameObject);
                    sceneGO.name = sceneGO.name.Replace("(Clone)", "");
                    sceneGO.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontUnloadUnusedAsset | HideFlags.HideInInspector;
                    sceneGO.tag = "EditorOnly";
                    for (int i = 0; i < sceneGO.transform.childCount; i++)
                    {
                        HideHierarchy(sceneGO.transform.GetChild(i));
                    }
                }
                m_procedureRunner = GameObject.FindObjectOfType<ProcedureRunner>();
                if (m_procedureRunner == null)
                {
                    m_procedureRunner = TempProcedureRunner;
                }

                if (m_hierarchyDrawer != null)
                {
                    m_hierarchyDrawer.ProcedureRunner = m_procedureRunner;
                }
            }
            else
            {
                Cleanup();
            }

            if (m_hierarchyDrawer != null)
            {
                ConsiderProcedureForTesting(m_hierarchyDrawer.ProcedureRunner);
                EditorApplication.RepaintHierarchyWindow();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        private void Cleanup()
        {
            ProcedureTestPanel.DisableSceneInstance();
            try
            {
                if (TestPanel)
                {
                    TestPanel.gameObject.SetActive(false);
                }
                ClearRunnerProcedureIfNeeded();
            }
            finally
            {
                if (m_tempProcedureRunner)
                {
                    DestroyTempProcedureRunner();
                    if (m_hierarchyDrawer != null)
                    {
                        m_hierarchyDrawer.ProcedureRunner = null;
                    }
                }
            }
        }

        private void ClearRunnerProcedureIfNeeded()
        {
            //if (m_testingWasAltered && CurrentProcedureRunner && CurrentProcedureRunner.CurrentProcedure == Procedure)
            //{
            //    CurrentProcedureRunner.CurrentProcedure = null;
            //}
        }

        private static void HideHierarchy(Transform sceneGO, HideFlags flags = HideFlags.HideInHierarchy | HideFlags.HideInInspector)
        {
            sceneGO.gameObject.hideFlags = flags;
            for (int i = 0; i < sceneGO.childCount; i++)
            {
                HideHierarchy(sceneGO.GetChild(i), flags);
            }
        }

        private void ConsiderProcedureForTesting(ProcedureRunner procedureRunner = null)
        {
            procedureRunner = procedureRunner ? procedureRunner : GameObject.FindObjectOfType<ProcedureRunner>();
            if (procedureRunner == null && m_isTesting)
            {
                procedureRunner = TempProcedureRunner;
            }
            if (!m_isTesting && !EditorApplication.isPlayingOrWillChangePlaymode && m_tempProcedureRunner)
            {
                DestroyTempProcedureRunner();
                m_hierarchyDrawer.ProcedureRunner = null;
            }
            else if (procedureRunner)
            {
                procedureRunner.CurrentProcedure = Procedure;
                procedureRunner.StartWhenReady = m_isTesting;

                m_hierarchyDrawer.ProcedureRunner = procedureRunner;
            }
        }

        private void DestroyTempProcedureRunner()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && m_tempProcedureRunner)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(m_tempProcedureRunner.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(m_tempProcedureRunner.gameObject);
                }
            }
        }
    }
}
