using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Localization;
using TXT.WEAVR.Packaging;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using ISelectable = UnityEditor.Experimental.GraphView.ISelectable;
using TXT.WEAVR.Common;

namespace TXT.WEAVR.Procedure
{

    class ProcedureView : GraphView, IControlledElement<ProcedureController>, UnityEditor.Build.IPreprocessBuildWithReport
    {
        private const string KEY_LAST_VIEWPORT = "ProcedureView_LastViewport";


        #region [  STATIC PART  ]

        public static ProcedureView Current => ProcedureEditor.Instance ? ProcedureEditor.Instance.ProcedureView : null;

        public void OnPreprocessBuild(BuildTarget target, string path)
        {
            ProcedureMessage.Show("Build in progress..", "Please be patient");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            ProcedureMessage.Show("Build in progress..", "Please be patient");
        }

        [PostProcessBuild]
        public static void PostBuildNotify(BuildTarget target, string pathToBuiltProject)
        {
            if (Current?.CurrentProcedure)
            {
                Current.DelayedRestore();
            }
        }

        public class ProcedureModicationProcessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                var procedureView = Current;
                if (procedureView == null || string.IsNullOrEmpty(procedureView.CurrentProcedurePath)) { return; }

                string currentProcedurePath = procedureView.CurrentProcedurePath;
                foreach (string str in deletedAssets)
                {
                    if (str == currentProcedurePath)
                    {
                        procedureView.CurrentProcedure = null;
                        return;
                    }
                }

                for (int i = 0; i < movedAssets.Length; i++)
                {
                    if (currentProcedurePath == movedFromAssetPaths[i])
                    {
                        procedureView.CurrentProcedurePath = movedAssets[i];
                        return;
                    }
                }
            }
        }


        #endregion


        VisualElement m_toolbar;
        //VisualElement m_procedureToolbar;
        VisualElement m_messages;
        ErrorHandlerView m_errorView;
        ProcedureViewFooter m_footer;
        MiniMap m_minimap;

        List<VisualElement> m_fastForwardPath;
        float m_nextWeightToApply;

        ProcedureSettings m_currentSettings;
        ProcedureViewport m_procedureViewport;
        ProcedureDebugger m_procedureDebugger;
        ProcedureFlowsInfo m_procedureFlowsInfo;

        GraphLabel m_procedureTitleLabel;

        internal ProcedureDebugger Debugger => m_procedureDebugger;
        internal ProcedureViewFooter Footer => m_footer;
        internal ProcedureFlowsInfo FlowsInfo => m_procedureFlowsInfo;

        private ProcedureController m_controller;

        public ProcedureController Controller
        {
            get => m_controller;
            set
            {
                if (m_controller != value)
                {
                    if (m_controller != null)
                    {
                        DisconnectController();
                    }
                    m_controller = value;
                    if (m_controller != null)
                    {
                        ConnectController();
                    }
                }
            }
        }

        Controller IControlledElement.Controller => m_controller;

        private string m_currentProcedurePath;
        private Procedure m_currentProcedure;

        public Procedure CurrentProcedure
        {
            get => m_currentProcedure;
            set
            {
                if (!ReferenceEquals(m_currentProcedure, value))
                {
                    ProcedureObjectInspector.ResetSelection();
                    m_currentProcedure = value;
                    m_procedureViewport = null;
                    m_messages.Clear();
                    m_messages.visible = !m_currentProcedure;
                    m_footer.StatsVisibility = m_currentProcedure;
                    m_errorView?.RemoveFromHierarchy();

                    if (m_currentProcedure)
                    {
                        try
                        {
                            m_currentProcedurePath = AssetDatabase.GetAssetPath(m_currentProcedure);
                            m_procedureDebugger.Procedure = value;
                            m_procedureFlowsInfo.Procedure = value;
                            m_footer.CurrentProcedure = m_currentProcedure;
                            ValidateProcedure();
                        }
                        catch (Exception e)
                        {
                            ShowError(e);
                            Controller = null;
                            ClearGraphView();
                        }
                    }
                    else
                    {
                        Controller = null;
                        m_currentProcedurePath = null;
                        ProcedureMessage.ShowFormat(m_messages, "Please Select An Asset", "or either #load# or #create# a procedure",
                                        () => LoadProcedureWizard.Show(ProcedureEditor.ShowAndEdit),
                                        () => CreateProcedureWizard.ShowWindow());
                    }
                    DelayedUpdateProcedureToolbar();
                }
            }
        }

        public string CurrentProcedurePath
        {
            get => m_currentProcedurePath;
            internal set
            {
                if (m_currentProcedurePath != value)
                {
                    m_currentProcedurePath = value;
                    if (!string.IsNullOrEmpty(m_currentProcedurePath))
                    {
                        CurrentProcedure = AssetDatabase.LoadMainAssetAtPath(m_currentProcedurePath) as Procedure;
                    }
                }
            }
        }

        private Dictionary<Controller, VisualElement> m_controlledElements = new Dictionary<Controller, VisualElement>();
        private Dictionary<GraphObjectController, BaseNodeView> m_controlledNodes = new Dictionary<GraphObjectController, BaseNodeView>();
        private Dictionary<GraphObjectController, BaseNodeView> m_startNodes = new Dictionary<GraphObjectController, BaseNodeView>();
        private Dictionary<GraphObjectController, BaseNodeView> m_debugStartNodes = new Dictionary<GraphObjectController, BaseNodeView>();
        private Dictionary<GraphObjectController, VisualElement> m_controlledNodesSteps = new Dictionary<GraphObjectController, VisualElement>();
        private Dictionary<GraphObjectController, StepView> m_controlledSteps = new Dictionary<GraphObjectController, StepView>();
        private Dictionary<TransitionController, FlowEdge> m_controlledFlowEdges = new Dictionary<TransitionController, FlowEdge>();
        private Dictionary<Controller, Port> m_outputFlowPorts = new Dictionary<Controller, Port>();

        public Dictionary<Controller, VisualElement> ControlledElements => m_controlledElements;
        public Dictionary<GraphObjectController, BaseNodeView> ControlledNodes => m_controlledNodes;
        public Dictionary<GraphObjectController, BaseNodeView> StartNodes => m_startNodes;
        public Dictionary<GraphObjectController, BaseNodeView> DebugStartNodes => m_debugStartNodes;
        public Dictionary<GraphObjectController, VisualElement> ControlledNodesSteps => m_controlledNodesSteps;
        public Dictionary<GraphObjectController, StepView> ControlledSteps => m_controlledSteps;
        public Dictionary<TransitionController, FlowEdge> ControlledFlowEdges => m_controlledFlowEdges;
        private Dictionary<Controller, Port> OutputFlowPorts => m_outputFlowPorts;

        private List<(GUIContent command, System.Action action)> m_additionalButtons = new List<(GUIContent command, Action action)>();

        public ProcedureView()
        {
            SetupZoom(0.05f, 2);
            this.zoomerMaxElementCountWithPixelCacheRegen = 100000;

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);

            Insert(0, new GridBackground());

            this.AddStyleSheetPath("ProcedureView");
            this.AddStyleSheetPath("ProcedureView" + (EditorGUIUtility.isProSkin ? "_dark" : "_lite"));

            m_minimap = AddMinimap(120, 120);

            m_errorView = new ErrorHandlerView();

            m_procedureTitleLabel = new GraphLabel();
            m_procedureTitleLabel.name = "procedure-title-label";

            BuildToolbar();

            m_messages = new VisualElement()
            {
                name = "messages-container"
            };

            ProcedureMessage.ShowFormat(m_messages, "Please Select An Asset", "or either #load# or #create# a procedure",
                                        () => LoadProcedureWizard.Show(ProcedureEditor.ShowAndEdit),
                                        () => CreateProcedureWizard.ShowWindow());
            Add(m_messages);

            m_procedureDebugger = new ProcedureDebugger();
            m_procedureFlowsInfo = new ProcedureFlowsInfo();
            Add(m_procedureFlowsInfo);

            Image weavrLogo = new Image()
            {
                name = "weavr-logo-image"
            };

            Add(weavrLogo);

            m_footer = new ProcedureViewFooter();
            Add(m_footer);

            viewDataKey = nameof(ProcedureView);

            viewTransformChanged += ProcedureViewChanged;

            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;

            SceneManager.activeSceneChanged += (prevScene, newScene) => ValidateSceneChange(newScene, true);
            EditorSceneManager.newSceneCreated += (s, _, __) => ValidateSceneChange(s, true);
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneClosing += (s, _) => SceneUnloaded(s);
            EditorSceneManager.sceneOpened += (s, _) => ValidateSceneChange(s, !IsCurrentSceneValid());
            //SceneManager.sceneUnloaded += SceneUnloaded;
            SceneManager.sceneLoaded += (s, _) => ValidateSceneChange(s, true);

            EditorSceneManager.activeSceneChangedInEditMode -= EditorSceneManager_ActiveSceneChangedInEditMode;
            EditorSceneManager.activeSceneChangedInEditMode += EditorSceneManager_ActiveSceneChangedInEditMode;

            RegisterCallback<KeyUpEvent>(OnKeyUp);

        }
        
        private void EditorSceneManager_ActiveSceneChangedInEditMode(Scene current, Scene next)
        {
            if (CurrentProcedure && CurrentProcedure.ScenePath == next.path)
            {
                // Here we way have the same scene reloading
                ValidateSceneChange(next, true);
                Reset(resetStates: false);
            }
        }

        private void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            if(m_procedureViewport != null && !string.IsNullOrEmpty(m_procedureViewport.procedureGuid))
            {
                EditorPrefs.SetString(KEY_LAST_VIEWPORT, JsonUtility.ToJson(m_procedureViewport));
            }
        }
        
        private void SceneUnloaded(Scene scene)
        {
            if (CurrentProcedure && !BuildPipeline.isBuildingPlayer)
            {
                var procedureScene = CurrentProcedure.GetScene();
                if (scene.path == procedureScene.path)
                {
                    ProcedureObjectInspector.ResetSelection();
                    FullSave();
                    Controller = null;
                    string scenePath = CurrentProcedure.ScenePath;
                    m_messages.Clear();
                    m_messages.visible = true;
                    ProcedureMessage.ShowFormat(m_messages, "Scene not loaded",
                    $"The scene {CurrentProcedure.SceneName} is not loaded. Please either #reload# the scene to edit the procedure, \n or #create# a new one",
                    () => EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive),
                    CreateProcedureWizard.ShowWindow);
                }
            }
        }

        private void OnSceneSaved(Scene scene)
        {
            if (CurrentProcedure && CurrentProcedure.ScenePath == scene.path)
            {
                FullSave();
            }
        }

        private void ValidateProcedure()
        {
            if (ValidateSceneChange())
            {
                if (Controller == null || Controller.Model != m_currentProcedure)
                {
                    Controller = new ProcedureController(m_currentProcedure);
                    UpdateViewportFromProcedure();
                }
            }
            else
            {
                Controller = null;
            }
        }

        private void ValidateSceneChange(Scene changedScene, bool disableInspector)
        {
            if (disableInspector)
            {
                ProcedureObjectInspector.ResetSelection();
            }
            if (CurrentProcedure)
            {
                ValidateProcedure();
            }
        }

        private bool IsCurrentSceneValid() => CurrentProcedure && CurrentProcedure.ScenePath == SceneManager.GetActiveScene().path;

        private bool ValidateSceneChange()
        {
            m_messages.Clear();
            if (string.IsNullOrEmpty(CurrentProcedure.SceneName))
            {
                if (string.IsNullOrEmpty(CurrentProcedure.ScenePath))
                {
                    m_messages.visible = false;
                    return true;
                }
                m_messages.visible = true;
                ProcedureMessage.ShowFormat(m_messages, "Scene reference broken",
                    "Scene reference for this procedure seems broken. #Try fix it#",
                    () =>
                    {
                        if (CurrentProcedure.TryFixSceneReference())
                        {
                            Reset();
                            DelayedUpdateProcedureToolbar();
                        }
                    });
                return false;
            }

            var procedureScene = CurrentProcedure.GetScene();
            if (procedureScene.IsValid() && procedureScene.isLoaded)
            {
                m_messages.visible = false;
                DelayedUpdateProcedureToolbar();
                return true;
            }
            m_messages.visible = true;

            if (BuildPipeline.isBuildingPlayer)
            {
                ProcedureMessage.Show("Build in progress...", "Please be patient");
                return false;
            }

            var currentScene = SceneManager.GetActiveScene();

            var scenePath = CurrentProcedure.ScenePath;
            if (!procedureScene.IsValid())
            {
                CurrentProcedure.Graph.ReferencesTable.SceneData.ValidateSceneData();
                scenePath = CurrentProcedure.ScenePath;
            }

            if (currentScene.IsValid() && currentScene.isLoaded)
            {
                ProcedureMessage.ShowFormat(m_messages, "Scene not compatible",
                    "Either #port current procedure# to this scene, #load the scene# referenced by this procedure \n or #create# a new procedure for this scene",
                    () =>
                    {
                        CurrentProcedure.Graph.ReferencesTable.AdaptToScene(currentScene);
                        Reset();
                    },
                    () => EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single),
                    CreateProcedureWizard.ShowWindow);
            }
            else
            {
                ProcedureMessage.ShowFormat(m_messages, "Scene not ready",
                    $"The scene {CurrentProcedure.SceneName} is not ready yet. Please #refresh# to edit the procedure",
                    () => Debug.Log("Refresh"));
            }
            DelayedUpdateProcedureToolbar();
            return false;
        }

        private void ClearGraphView()
        {
            foreach (var node in nodes.ToList())
            {
                RemoveElement(node);
            }
            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }
        }

        internal void AddAdditionalButtons(params (GUIContent command, Action action)[] additionalButtons)
        {
            int count = m_additionalButtons.Count;
            foreach (var pair in additionalButtons)
            {
                if (!m_additionalButtons.Contains(pair))
                {
                    m_additionalButtons.Add(pair);
                }
            }
            if (count != m_additionalButtons.Count)
            {
                BuildToolbar();
            }
        }

        internal void RemoveAdditionalButtons(params (GUIContent command, Action action)[] additionalButtons)
        {
            int count = m_additionalButtons.Count;
            foreach (var pair in additionalButtons)
            {
                m_additionalButtons.Remove(pair);
            }
            if (count != m_additionalButtons.Count)
            {
                BuildToolbar();
            }
        }

        private void ShowError(Exception e)
        {
            m_footer.AddToClassList("error");
            m_footer.StatsVisibility = false;

            Insert(1, m_errorView);
            m_errorView.SetException($"Failed to load procedure: {m_currentProcedure.name}", e);
        }

        private void UpdateViewportFromProcedure()
        {
            if (panel == null) { return; }
            var json = EditorPrefs.GetString(KEY_LAST_VIEWPORT);
            if (!string.IsNullOrEmpty(json))
            {
                m_procedureViewport = JsonUtility.FromJson<ProcedureViewport>(json) ?? new ProcedureViewport();
            }
            else {
                m_procedureViewport = new ProcedureViewport();
            }
            if (!CurrentProcedure || m_procedureViewport.procedureGuid != CurrentProcedure.Guid)
            {
                schedule.Execute(() => FrameAll()).ExecuteLater(80);
            }
            else
            {
                UpdateViewTransform(m_procedureViewport.position, m_procedureViewport.scale);
            }
        }

        private string GetCurrentProcedureKey()
        {
            return $"{m_currentProcedure?.name}_{m_currentProcedure?.Guid}";
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            switch (WeavrEditor.Commands[evt.modifiers, evt.keyCode])
            {
                case Core.Command.Group:
                    CreateGroup(null);
                    evt.StopPropagation();
                    break;
            }
        }

        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange state)
        {
            if ((state == PlayModeStateChange.ExitingEditMode) && CurrentProcedure)
            {
                FullSave();

                //SavePersistentData();
            }
            else if ((state == PlayModeStateChange.ExitingPlayMode) && CurrentProcedure)
            {
                CurrentProcedure.Save(updateReferences: false);

                //SavePersistentData();
            }
            if (state == PlayModeStateChange.EnteredEditMode && CurrentProcedure)
            {
                CurrentProcedure.Graph.ReferencesTable?.RefreshData();
                DelayedAutoResolve(1000);
                //if (m_currentSettings != null && !Application.isPlaying)
                //{
                //    m_procedureDebugger.IsTestActive = m_currentSettings.testingIsOn;
                //}
            }
        }

        private void ProcedureViewChanged(GraphView graphView)
        {
            m_procedureViewport?.Update(graphView.viewTransform, CurrentProcedure);
        }

        private void BuildToolbar()
        {
            if (m_toolbar == null)
            {
                m_toolbar = new Toolbar();
                m_toolbar.AddToClassList("toolbar");
            }
            else
            {
                m_toolbar.Clear();
            }

            Button button = null;

            if (ProcedureEditor.Instance && ProcedureEditor.Instance.HasPreviousProcedures)
            {
                var editorInstance = ProcedureEditor.Instance;
                button = new ToolbarButton(() => editorInstance?.LoadPreviousProcedure());
                button.text = "Back";
                m_toolbar.Add(button);
            }

            button = new ToolbarButton(CreateProcedureWizard.ShowWindow);
            button.text = "New";
            m_toolbar.Add(button);

            button = new ToolbarButton(() => LoadProcedureWizard.Show(ProcedureEditor.ShowAndEdit));
            button.text = "Load";
            m_toolbar.Add(button);

            WeavrEditor.Settings.RegisterCallback(s =>
            {
                if (s.Value is bool v && v)
                {
                    m_toolbar.Insert(1, button);
                }
                else { button.RemoveFromHierarchy(); }
            }, "AllowImport");
            if (WeavrEditor.Settings.GetValue("AllowImport", true))
            {
                m_toolbar.Add(button);
            }

            VisualElement spacer = new VisualElement();
            spacer.style.width = 10;
            m_toolbar.Add(spacer);

            UpdateProcedureToolbar();

            Add(m_toolbar);
        }

        private void DelayedUpdateProcedureToolbar()
        {
            schedule.Execute(BuildToolbar).StartingIn(200);
        }

        private void UpdateProcedureToolbar()
        {
            if (!CurrentProcedure) { return; }

            var procedureScene = CurrentProcedure.GetScene();
            if (!procedureScene.IsValid() || !procedureScene.isLoaded)
            {
                return;
            }

            Button button;

            if (m_additionalButtons?.Count > 0)
            {
                foreach (var (command, action) in m_additionalButtons)
                {
                    button = new ToolbarButton(action)
                    {
                        text = command.text,
                        tooltip = command.tooltip
                    };
                    // TODO: Add image to button in Procedure View
                    m_toolbar.Add(button);
                }
            }

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            m_toolbar.Add(spacer);
            
            m_toolbar.Add(m_procedureDebugger);


            spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            m_toolbar.Add(spacer);

            var searchBar = new ToolbarSearchField();
            searchBar.RegisterValueChangedCallback(SearchValueChanged);
            searchBar.Q<TextField>().name = "searchTextField";
            m_toolbar.Add(searchBar);

            button = new ToolbarButton(() => Reset());
            button.name = "reset-button";
            button.text = "Reset";
            m_toolbar.Add(button);

            button = new ToolbarButton(CurrentProcedure.Graph.ReferencesTable.BackupReferences);
            button.name = "backup-button";
            button.text = "Backup";
            m_toolbar.Add(button);

            Toggle toggleAutoCompile = new ToolbarToggle();
            toggleAutoCompile.text = "Minimap";
            toggleAutoCompile.SetValueWithoutNotify(false);
            toggleAutoCompile.RegisterCallback<ChangeEvent<bool>>(OnMinimapVisible);
            m_toolbar.Add(toggleAutoCompile);
            
            EditorApplication.RepaintHierarchyWindow();
        }

        private void SearchValueChanged(ChangeEvent<string> evt)
        {
            //var value = string.IsNullOrEmpty(evt.newValue) || evt.newValue.Length < 2 ? string.Empty : evt.newValue;

            var value = evt.newValue;
            Controller?.Search(value);
            //SearchHub.Current.CurrentSearchValue = value;
        }

        public void Reset(bool resetStates = true)
        {
            if (CurrentProcedure && !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer)
            {
                m_toolbar.Q<Button>("reset-button")?.RemoveFromClassList("needed");
                var currentProcScene = CurrentProcedure.GetScene();
                bool procSceneIsValid = currentProcScene.IsValid() && currentProcScene.isLoaded;
                if (procSceneIsValid)
                {
                    // Hide any over the top messages
                    m_messages.visible = false;
                    var controller = Controller;
                    if (controller != null)
                    {
                        DisconnectController();
                        controller.Cleanup();
                        if (CurrentProcedure.Graph && CurrentProcedure.Graph.ReferencesTable && CurrentProcedure.Graph.ReferencesTable.References.Count == 0)
                        {
                            CurrentProcedure.Graph.ReferencesTable.TryRestorePreviousReferences();
                        }
                        else if(CurrentProcedure.Graph && CurrentProcedure.Graph.ReferencesTable)
                        {
                            CurrentProcedure.Graph.ReferencesTable.SceneWeavr = null;
                        }
                        ConnectController();
                    }
                    else
                    {
                        Controller = new ProcedureController(m_currentProcedure);
                    }

                    Controller.Model.FixInternalProcedureReferences();
                }
            }
            else
            {
                m_toolbar.Q<Button>("reset-button")?.AddToClassList("needed");
            }
            if (resetStates)
            {
                Controller?.ResetStates();
            }
        }

        private void OnExecutionModeChanged(ChangeEvent<ExecutionMode> evt)
        {
            if (CurrentProcedure)
            {
                CurrentProcedure.DefaultExecutionMode = evt.newValue;
            }
        }

        public void FullSave()
        {
            if (CurrentProcedure)
            {
                CurrentProcedure.Save(updateReferences: true);
            }
        }

        private void ToggleAssetVisibility(ChangeEvent<bool> evt)
        {
            if (!CurrentProcedure) { return; }
            if (evt.newValue)
            {
                CurrentProcedure.hideFlags &= ~HideFlags.HideInHierarchy;
            }
            else
            {
                CurrentProcedure.hideFlags |= HideFlags.HideInHierarchy;
            }
        }

        void DisconnectController()
        {
            m_controller?.UnregisterHandler(this);
            //m_Controller.useCount--;

            canPasteSerializedData = null;
            serializeGraphElements = null;
            unserializeAndPaste = null;
            deleteSelection = null;
            nodeCreationRequest = null;

            elementsAddedToGroup = null;
            elementsRemovedFromGroup = null;
            groupTitleChanged = null;

            //m_GeometrySet = false;

            // Remove all in view now that the controller has been disconnected.
            foreach (var element in ControlledSteps.Values)
            {
                element.Controller = null;
                RemoveElement(element);
            }
            foreach (var element in ControlledNodes.Values)
            {
                element.Controller = null;
                RemoveElement(element);
            }
            foreach (var element in ControlledNodesSteps.Values)
            {
                if (element is GraphObjectView gObj)
                {
                    gObj.Controller = null;
                    RemoveElement(gObj);
                }
                else if (element is GraphElement gEl)
                {
                    RemoveElement(gEl);
                }
            }
            foreach (var element in ControlledFlowEdges.Values)
            {
                element.Controller = null;
                RemoveElement(element);
            }
            foreach (var element in OutputFlowPorts.Values)
            {
                RemoveElement(element);
            }
            foreach (var pair in m_controlledElements)
            {
                if (pair.Value is IControlledElement cEl)
                {
                    pair.Key?.UnregisterHandler(cEl);
                }
                if (pair.Value is GraphElement gEl)
                {
                    RemoveElement(gEl);
                }
            }

            ControlledSteps.Clear();
            ControlledNodes.Clear();
            StartNodes.Clear();
            DebugStartNodes.Clear();
            ControlledNodesSteps.Clear();
            ControlledFlowEdges.Clear();
            OutputFlowPorts.Clear();
            
            RemoveElement(m_procedureTitleLabel);
            //m_procedureTitleLabel?.RemoveFromHierarchy();
        }

        void ConnectController()
        {
            m_controller.RegisterHandler(this);

            FastAddElement(m_procedureTitleLabel);

            canPasteSerializedData = CanPasteSerializationData;
            serializeGraphElements = SerializeElements;
            unserializeAndPaste = UnserializeAndPasteElements;
            deleteSelection = Delete;
            nodeCreationRequest = OnCreateNode;

            elementsAddedToGroup = ElementAddedToGroupNode;
            elementsRemovedFromGroup = ElementRemovedFromGroupNode;
            groupTitleChanged = GroupNodeTitleChanged;

            m_InControllerChanged = true;
            Controller.ApplyChanges();
            m_InControllerChanged = false;
            SyncWithController();

            if (CurrentProcedure)
            {
                LocalizationManager.Current.Table = CurrentProcedure.LocalizationTable;
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AutoResolveReferences();
            }
            else if (Controller.ReferenceTable.IsReady)
            {
                Controller.ReferenceTable.SceneWeavr = null;
            }

            CurrentProcedure.Graph.UI_Position = layout.center;

            m_procedureTitleLabel.text = CurrentProcedure.ProcedureName;
            m_procedureTitleLabel.SetPosition(new Rect(CurrentProcedure.Graph.UI_Position, Vector2.zero));
        }

        private void SyncWithController()
        {
            ControllerChanged(-1);
        }

        public void Select(ProcedureObject obj)
        {
            Find(obj, (_, selectable) =>
            {
                ClearSelection();
                AddToSelection(selectable);
                FrameSelection();
            });
        }

        public void Highlight(ProcedureObject obj, bool focus = true)
        {
            Find(obj, (found, selectable) =>
            {
                //var move = found.layout.size * 0.5f;
                //var targetPosition = found.transform.position;
                //found.transform.position -= new Vector3(move.x, move.y, 1);
                //found.transform.scale = Vector3.one * 2;
                //found.schedule.Execute(() =>
                //{
                //    found.transform.scale = Vector3.MoveTowards(found.transform.scale, Vector3.one, 0.1f);
                //    found.transform.position = Vector3.MoveTowards(found.transform.position, targetPosition, 0.1f);
                //})
                //             .Every(20)
                //             .Until(() => found.transform.scale == Vector3.one);
                found.Ping();
                if (focus)
                {
                    ClearSelection();
                    AddToSelection(selectable);
                    FrameSelection();
                }
            });
        }


        private void Find(ProcedureObject obj, Action<VisualElement, ISelectable> onFoundCallback)
        {
            switch (obj)
            {
                case BaseAnimationBlock animBlock:
                    obj = animBlock.Composer;
                    break;
            }

            foreach (var elemPair in m_controlledElements)
            {
                if (elemPair.Key.GetModel() == obj)
                {
                    var found = elemPair.Value;
                    VisualElement view = elemPair.Value;
                    while (view != null && !(view is ISelectable))
                    {
                        view = view.parent;
                    }
                    if (view is ISelectable selectable)
                    {
                        onFoundCallback(found, selectable);
                        return;
                    }
                    onFoundCallback(found, null);
                }
            }

            foreach (var elem in m_controlledElements.Values)
            {
                if (elem is ISelectable selectable)
                {
                    if (elem.TryFind(e => e is IControlledElement ce && ce.Controller?.GetModel() == obj, out VisualElement found))
                    {
                        onFoundCallback(found, selectable);
                        return;
                    }
                }
            }

            if (obj is BaseCondition condition)
            {
                foreach (var elem in m_controlledElements.Values)
                {
                    if (elem is ISelectable selectable)
                    {
                        if (elem.TryFind(e => e is IControlledElement ce
                                            && ce.Controller?.GetModel() is FlowConditionsContainer fc
                                            && fc.Conditions.Any(c => c.Contains(condition)), out VisualElement found))
                        {
                            onFoundCallback(found, selectable);
                            return;
                        }
                    }
                }
            }
        }

        private void DelayedAutoResolve(long delayMs)
        {
            schedule.Execute(() =>
            {
                if (Controller != null && Controller.ReferenceTable && Controller.ReferenceTable.IsReady)
                {
                    Controller.ReferenceTable.AutoResolve();
                }
            }).StartingIn(delayMs);
        }

        private void AutoResolveReferences()
        {
            if (Controller.ReferenceTable.IsReady)
            {
                Controller.ReferenceTable.AutoResolve();
            }
            else
            {
                schedule.Execute(() => Controller.ReferenceTable.AutoResolve()).StartingIn(2000);
            }
        }

        private new void FrameAll()
        {
            if(m_procedureTitleLabel != null && m_controlledNodes.Count > 0)
            {
                RemoveElement(m_procedureTitleLabel);
                base.FrameAll();
                AddElement(m_procedureTitleLabel);
                m_procedureTitleLabel.SendToBack();
            }
            else
            {
                base.FrameAll();
            }
        }

        public void Select(GraphObject obj, bool focusOnElement)
        {
            if (GetView(obj) is ISelectable graphElement)
            {
                ClearSelection();
                AddToSelection(graphElement);
                if (focusOnElement)
                {
                    FrameSelection();
                }
            }
        }

        public VisualElement GetView(GraphObject obj)
        {
            if (Controller == null)
            {
                return null;
            }

            var controller = Controller.GetController<GraphObjectController>(obj);
            return ControlledElements.TryGetValue(controller, out VisualElement found) ? found : null;
        }

        public BaseNodeView GetNode(GraphObjectController controller)
        {
            return controller != null && m_controlledNodes.TryGetValue(controller, out BaseNodeView node) ? node : null;
        }

        public void RegisterFlowPort(Controller portController, Port port)
        {
            if (port.direction == Direction.Output)
            {
                m_outputFlowPorts[portController] = port;
            }
        }

        public void UnregisterFlowPort(Controller portController)
        {
            m_outputFlowPorts.Remove(portController);
        }

        public Port GetPort(Controller sourcePort)
        {
            return sourcePort != null && m_outputFlowPorts.TryGetValue(sourcePort, out Port port) ? port : null;
        }

        private void GroupNodeTitleChanged(Group arg1, string arg2)
        {

        }

        private void ElementRemovedFromGroupNode(Group arg1, IEnumerable<GraphElement> arg2)
        {
            //Debug.Log($"Removed {arg2} from {arg1}");
        }

        private void ElementAddedToGroupNode(Group arg1, IEnumerable<GraphElement> arg2)
        {
            //Debug.Log($"Added {arg2} to {arg1}");
        }

        private void OnCreateNode(NodeCreationContext ctx)
        {
            if (Controller == null)
            {
                return;
            }

            Controller.CreateNode<GenericNode>(contentViewContainer.WorldToLocal(ScreenToViewPosition(ctx.screenMousePosition)));
        }

        private void Delete(string operationName, AskUser askUser)
        {
            foreach (var edge in selection.Where(s => s is FlowEdge && s is IControlledElement).Select(s => s as FlowEdge).ToList())
            {
                Controller.RemoveTransition((edge as IControlledElement).Controller);
            }
            foreach (var node in selection.Where(s => s is BaseNodeView).Select(s => s as BaseNodeView).ToList())
            {
                Controller.RemoveNode(node.Controller);
            }
            foreach (var group in selection.Where(s => s is StepView).Select(s => s as StepView).ToList())
            {
                Controller.RemoveStep(group.Controller);
            }
        }

        private bool CanPasteSerializationData(string data)
        {
            if (Controller != null && Controller.CanDeserializeData(data))
            {
                return true;
            }

            // Workaround to avoid a stack overflow by recursively calling this method
            canPasteSerializedData = null;
            bool value = CanPasteSerializedData(data);
            canPasteSerializedData = CanPasteSerializationData;
            return value;
        }

        private void UnserializeAndPasteElements(string operationName, string data)
        {
            if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer) && !EditorGUIUtility.systemCopyBuffer.Contains(data))
            {
                data = EditorGUIUtility.systemCopyBuffer;
            }
            var objects = Controller.UnserializeData(operationName, data);
            if (objects.Count() > 0)
            {
                ClearSelection();
                foreach (var obj in objects)
                {
                    if (obj is Controller && m_controlledElements.TryGetValue(obj as Controller, out VisualElement element) && element is ISelectable)
                    {
                        AddToSelection(element as ISelectable);
                    }
                }
            }
            else if (ProcedureObjectInspector.CurrentEditor is IPasteClient pasteClient)
            {
                pasteClient.Paste(data);
            }
        }

        private string SerializeElements(IEnumerable<GraphElement> elements)
        {
            var json = Controller.SerializeData(elements.Select(e => (e as IControlledElement)?.Controller).Where(e => e != null));
            //Debug.Log($"[JSON]: {json}");
            if (!string.IsNullOrEmpty(json))
            {
                EditorGUIUtility.systemCopyBuffer = null;
            }
            return json;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> ports = new List<Port>();
            foreach (var port in this.Query<Port>().ToList())
            {
                if (port != startPort && port.direction != startPort.direction && port.node != startPort.node)
                {
                    ports.Add(port);
                }
            }
            return ports;
        }

        private void OnMinimapVisible(ChangeEvent<bool> evt)
        {
            if (evt.newValue && m_minimap.panel == null)
            {
                Add(m_minimap);
            }
            else if (!evt.newValue)
            {
                m_minimap.RemoveFromHierarchy();
            }
            evt.StopPropagation();
        }

        private MiniMap AddMinimap(float width, float height)
        {
            var minimap = new MiniMap();
            minimap.maxHeight = width;
            minimap.maxWidth = height;
            minimap.SetPosition(new Rect(1000, 20, 0, 0));
            //minimap.style.top = 20;
            minimap.RemoveFromClassList("graphElement");
            minimap.AddToClassList("minimap");
            minimap.Q<Label>().AddToClassList("minimap-title");
            //Add(minimap);

            return minimap;
        }

        private void SelectionChanged()
        {
            if (Selection.activeObject is Procedure)
            {
                m_messages.visible = false;
            }
        }

        #region [  GUIVIEW Hidden Methods  ]

        static PropertyInfo s_ownerPropertyInfo;

        private Vector2 GUIViewScreenPosition
        {
            get
            {
                if (s_ownerPropertyInfo == null)
                {
                    s_ownerPropertyInfo = panel.GetType().GetProperty("ownerObject", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);
                }

                if (s_ownerPropertyInfo != null)
                {
                    var guiView = s_ownerPropertyInfo.GetValue(panel);
                    if (guiView != null)
                    {
                        PropertyInfo screenPosition = guiView.GetType().GetProperty("screenPosition", BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Public);

                        if (screenPosition != null)
                        {
                            return ((Rect)screenPosition.GetValue(guiView)).position;
                        }
                    }
                }
                return Vector2.zero;
            }
        }

        public int callbackOrder => 0;

        public Vector2 ScreenToViewPosition(Vector2 position)
        {
            return position - GUIViewScreenPosition;
        }

        public Vector2 ViewToScreenPosition(Vector2 position)
        {
            return position + GUIViewScreenPosition;
        }

        #endregion

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            //base.BuildContextualMenu(evt);
            if (evt.target is BaseNodeView nodeView)
            {
                evt.menu.InsertAction(0, "Add Group", m => CreateGroup(evt.target as BaseNodeView), m => DropdownMenuAction.Status.Normal);
                if (Controller.StartNodesControllers.Contains(nodeView.Controller))
                {
                    evt.menu.InsertAction(1, "Main Flow", m => Controller.RemoveFromStartNodes(nodeView.Controller), m => Controller.StartNodesControllers.Count > 1 ?
                                                                                                                        DropdownMenuAction.Status.Checked :
                                                                                                                        DropdownMenuAction.Status.Disabled);
                }
                else if (Controller.FlowStartNodesControllers.Contains(nodeView.Controller))
                {
                    evt.menu.InsertAction(2, "Secondary Flow", m => Controller.RemoveFromFlowStartNodes(nodeView.Controller), DropdownMenuAction.Status.Checked);
                }
                else
                {
                    evt.menu.InsertAction(1, "Main Flow", m => Controller.SetAsStartNode(nodeView.Controller), m => DropdownMenuAction.Status.Normal);
                    evt.menu.InsertAction(2, "Secondary Flow", m => Controller.SetAsFlowStartNode(nodeView.Controller), m => DropdownMenuAction.Status.Normal);
                }

                if (Controller.DebugStartNodesControllers.Contains(nodeView.Controller))
                {
                    evt.menu.InsertAction(3, "Test from here", m => Controller.RemoveFromDebugStartNode(nodeView.Controller), m => Controller.DebugStartNodesControllers.Any(c => nodeView.Controller == c) ?
                                                                                                                        DropdownMenuAction.Status.Checked :
                                                                                                                        DropdownMenuAction.Status.Disabled);
                }
                else if(Controller.Graph.IsReacheableFromStartPoints(nodeView.Controller.Model))
                {
                    evt.menu.InsertAction(3, "Test from here", m => Controller.SetAsDebugStartNode(nodeView.Controller), m => DropdownMenuAction.Status.Normal);
                }
            }
            else if(evt.target is FlowEdge edgeView)
            {
                if(m_fastForwardPath != null)
                {
                    if (m_fastForwardPath.Contains(edgeView))
                    {
                        evt.menu.InsertAction(0, "Reset path", m =>
                        {
                            m_nextWeightToApply = 0;
                            Controller.Graph.ResetPathsPriorities();
                        }, m => DropdownMenuAction.Status.Normal);
                    }
                    else if(edgeView is IControlledElement controlledElem && controlledElem.Controller is TransitionController tController)
                    {
                        evt.menu.InsertAction(0, "Go this path", 
                            m => tController.Priority = m_nextWeightToApply, 
                            m => Controller.Graph.IsReacheableFromDebugStartPoints(tController.FromModel) ? 
                                    DropdownMenuAction.Status.Normal :
                                    DropdownMenuAction.Status.Disabled);
                        if(tController.Priority != BaseTransition.k_DefaultPriority)
                        {
                            evt.menu.InsertAction(0, "Reset Priority",
                            m => tController.Priority = BaseTransition.k_DefaultPriority,
                            m => DropdownMenuAction.Status.Normal);
                        }
                        //evt.menu.InsertAction(0, "High priority", m => tController.Priority = -1000, m => DropdownMenuAction.Status.Normal);
                    }
                }
            }
            else
            {
                base.BuildContextualMenu(evt);
                var nodePosition = contentViewContainer.WorldToLocal(ScreenToViewPosition(GUIUtility.GUIToScreenPoint(evt.localMousePosition)));
                evt.menu.InsertAction(1, "Create Hub Node",
                                       m => Controller.CreateNode<TrafficNode>(nodePosition),
                                       m => DropdownMenuAction.Status.Normal);
            }
        }

        private void CreateGroup(BaseNodeView baseNodeView)
        {
            var nodesToAdd = selection.Where(s => s is BaseNodeView).Cast<BaseNodeView>().ToList();
            if (baseNodeView != null && !nodesToAdd.Contains(baseNodeView))
            {
                nodesToAdd.Add(baseNodeView);
            }

            Controller.AddStep<BaseStep>(nodesToAdd.Select(n => n.Controller));
        }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == Controller)
            {
                ControllerChanged(e.change);
            }
        }

        bool m_InControllerChanged;

        void ControllerChanged(int change)
        {
            if (m_InControllerChanged) { return; }
            //Debug.Log($"Received Change {change}");
            //m_InControllerChanged = true;

            if (change == ProcedureController.Change.Step)
            {
                SyncGroupNodes();

                var groupNodes = m_controlledSteps;
                foreach (var groupNode in groupNodes.Values)
                {
                    groupNode.SelfChange();
                }

                m_footer.UpdateValues(this);
                return;
            }

            if (change >= WEAVR.Procedure.Controller.AnyThing)
            {
                SyncNodes();
            }

            SyncFlowPorts();
            SyncGroupNodes();
            SyncTransitions(change);
            SyncStartNodes();
            SyncDebugStartNodes();

            RefreshControllersDictionary();
            // Clear fast forward path
            ClearFastForwardPath();
            RetrieveAndApplyFastForwardPath();

            UpdateReacheableNodes();

            if (Controller != null)
            {
                if (change == WEAVR.Procedure.Controller.AnyThing)
                {
                    // if the asset is destroyed somehow, fox example if the user delete the asset, update the controller and update the window.
                    var asset = Controller.Model;
                    if (asset == null)
                    {
                        Controller = null;
                        m_footer.UpdateValues(this);
                        return;
                    }
                }
            }
            m_footer.UpdateValues(this);
        }

        private void RefreshControllersDictionary()
        {
            m_controlledElements.Clear();
            foreach (var pair in m_controlledNodes)
            {
                m_controlledElements.Add(pair.Key, pair.Value);
            }
            foreach (var pair in m_controlledSteps)
            {
                m_controlledElements.Add(pair.Key, pair.Value);
            }
            foreach (var pair in m_controlledFlowEdges)
            {
                m_controlledElements.Add(pair.Key, pair.Value);
            }
        }

        private void SyncFlowPorts()
        {
            if (Controller == null)
            {
                foreach (var element in OutputFlowPorts.Values.ToArray())
                {
                    SafeRemoveElement(element);
                }
                OutputFlowPorts.Clear();
            }
            else
            {
                if (OutputFlowPorts.Count == 0)
                {
                    foreach (var port in this.Query<FlowPort>().ToList())
                    {
                        if (port is IControlledElement && port.direction == Direction.Output)
                        {
                            OutputFlowPorts[(port as IControlledElement).Controller] = port;
                        }
                    }
                }
                var deletedControllers = OutputFlowPorts.Keys.Except(Controller.PortsControllers).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    SafeRemoveElement(OutputFlowPorts[deletedController]);
                    OutputFlowPorts.Remove(deletedController);
                }
            }
        }

        //public override void OnPersistentDataReady()
        //{
        //    base.OnPersistentDataReady();
        //    if (CurrentProcedure)
        //    {
        //        UpdateViewportFromProcedure();
        //    }
        //    m_currentSettings = GetOrCreatePersistentData<ProcedureSettings>(m_currentSettings, "Procedure_Settings");

        //    //if (CurrentProcedure)
        //    //{
        //    //    this.Q<Toggle>("procedure-test-toggle").value = m_currentSettings.testingIsOn;
        //    //}
        //    if (m_currentSettings.testingIsOn && !Application.isPlaying)
        //    {
        //        m_procedureDebugger.IsTestActive = true;
        //    }
        //}

        public GraphElement GetGroupNodeElement(Controller controller)
        {
            return controller is GraphObjectController && m_controlledSteps.TryGetValue(controller as GraphObjectController, out StepView view) ? view : null;
        }

        void SyncNodes()
        {
            if (Controller == null)
            {
                foreach (var element in ControlledNodes.Values.ToArray())
                {
                    SafeRemoveElement(element);
                }
                ControlledNodes.Clear();
                ControlledNodesSteps.Clear();
            }
            else
            {
                elementsAddedToGroup = null;
                elementsRemovedFromGroup = null;

                var deletedControllers = ControlledNodes.Keys.Except(Controller.NodesControllers).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    SafeRemoveElement(ControlledNodes[deletedController]);
                    ControlledNodes.Remove(deletedController);
                    ControlledNodesSteps.Remove(deletedController);
                }

                foreach (var newController in Controller.NodesControllers.Except(ControlledNodes.Keys).ToArray())
                {
                    BaseNodeView nodeView = null;
                    if (newController is GenericNodeController)
                    {
                        nodeView = new GenericNodeView();
                    }
                    else if (newController is TrafficNodeController)
                    {
                        nodeView = new TrafficNodeView();
                    }
                    else
                    {
                        throw new InvalidOperationException("Can't find right ui for controller" + newController.GetType().Name);
                    }

                    //FastAddElement(newElement);

                    AddElement(nodeView);
                    ControlledNodes[newController] = nodeView;
                    ControlledNodesSteps[newController] = nodeView;
                    (nodeView as ISettableControlledElement<GraphObjectController>).Controller = newController;

                    foreach (var port in nodeView.Query<FlowPort>().ToList())
                    {
                        if ((port is IControlledElement controlledPort))
                        {
                            RegisterFlowPort(controlledPort.Controller, port);
                        }
                    }
                }
                
                //elementsAddedToGroup = ElementAddedToGroupNode;
                //elementsRemovedFromGroup = ElementRemovedFromGroupNode;
            }
        }

        private void SyncStartNodes()
        {
            if (Controller == null)
            {
                foreach (var element in StartNodes.Values.ToArray())
                {
                    ToggleMarker(element, null, "entryPointMarker");
                    ToggleMarker(element, null, "flowStartMarker");
                }
                StartNodes.Clear();
            }
            else
            {
                var deletedControllers = StartNodes.Keys
                                                   .Except(Controller.StartNodesControllers)
                                                   .Except(Controller.FlowStartNodesControllers)
                                                   .ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    var nodeView = StartNodes[deletedController];
                    ToggleMarker(nodeView, null, "entryPointMarker");
                    ToggleMarker(nodeView, null, "flowStartMarker");
                    StartNodes.Remove(deletedController);
                }

                foreach (var startNodeController in Controller.StartNodesControllers.Except(StartNodes.Keys).ToArray())
                {
                    var nodeView = ControlledNodes[startNodeController];
                    StartNodes[startNodeController] = nodeView;

                    ToggleMarker(nodeView, () => new Label("Primary Flow Start"), "entryPointMarker");
                }

                foreach (var startNodeController in Controller.FlowStartNodesControllers.Except(StartNodes.Keys).ToArray())
                {
                    var nodeView = ControlledNodes[startNodeController];
                    StartNodes[startNodeController] = nodeView;

                    ToggleMarker(nodeView, () => new Label("Secondary Flow Start"), "flowStartMarker");
                }
            }
        }

        private void SyncDebugStartNodes()
        {
            if (Controller == null)
            {
                foreach (var element in DebugStartNodes.Values.ToArray())
                {
                    ToggleMarker(element, null, "debugPointMarker");
                }
                DebugStartNodes.Clear();
            }
            else
            {
                var deletedControllers = DebugStartNodes.Keys.Except(Controller.DebugStartNodesControllers).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    var nodeView = DebugStartNodes[deletedController];
                    ToggleMarker(nodeView, null, "debugPointMarker");
                    DebugStartNodes.Remove(deletedController);
                }

                foreach (var startNodeController in Controller.DebugStartNodesControllers.Except(DebugStartNodes.Keys).ToArray())
                {
                    var nodeView = ControlledNodes[startNodeController];
                    DebugStartNodes[startNodeController] = nodeView;

                    ToggleMarker(nodeView, () => new Label("Run from Here"), "debugPointMarker");
                }
            }

        }


        private void UpdateReacheableNodes()
        {
            Controller.UpdateReacheability();

            //bool shouldRepaint = false;
            //foreach (var pair in ControlledNodes)
            //{
            //    if (pair.Key is BaseNodeController nodeController && nodeController.UpdateReacheability())
            //    {
            //        shouldRepaint = true;
            //    }
            //}
            //if (shouldRepaint)
            //{
            //    UpdateViewTransform(viewTransform.position, viewTransform.scale);
            //}
        }

        private void RetrieveAndApplyFastForwardPath()
        {
            if (DebugStartNodes.Count > 0)
            {
                var path = Controller.Graph.ShortestDebugPath();
                if (path != null)
                {
                    m_nextWeightToApply = -path.Links.Sum(l => Mathf.Abs(l.Edge.weight)) - 1;
                    m_fastForwardPath = new List<VisualElement>();
                    var list = path.Convert<GraphObject>();
                    foreach (var elem in list)
                    {
                        var view = GetView(elem);
                        if (view != null && !DebugStartNodes.Values.Contains(view))
                        {
                            m_fastForwardPath.Add(view);
                            SetAsFastForward(view, true);
                            view.BringToFront();
                        }
                    }
                }
            }
        }

        private void ClearFastForwardPath()
        {
            if (m_fastForwardPath != null && m_fastForwardPath.Count > 0)
            {
                foreach (var elem in m_fastForwardPath)
                {
                    SetAsFastForward(elem, false);
                }
                m_fastForwardPath = null;
            }
            //m_currentPathWeight = 0;
        }

        private static void SetAsFastForward(VisualElement elem, bool enable)
        {
            elem?.EnableInClassList("fast-forward-path", enable);
        }

        private static void ToggleMarker(VisualElement element, Func<VisualElement> createCallback, string markerName = "marker")
        {
            if (element == null) { return; }
            if (createCallback != null)
            {
                var entryPointLabel = createCallback();
                entryPointLabel.name = markerName;
                element.Add(entryPointLabel);

                element.AddToClassList($"marked-{markerName}");
            }
            else
            {
                element.Q(markerName)?.RemoveFromHierarchy();

                element.RemoveFromClassList($"marked-{markerName}");
            }
        }

        static FieldInfo s_Member_ContainerLayer = typeof(GraphView).GetField("m_ContainerLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo s_Method_GetLayer = typeof(GraphView).GetMethod("GetLayer", BindingFlags.NonPublic | BindingFlags.Instance);

        public void FastAddElement(GraphElement graphElement)
        {
            if (graphElement.IsResizable())
            {
                graphElement.hierarchy.Add(new Resizer());
                graphElement.style.borderBottomWidth = 6;
            }

            int newLayer = graphElement.layer;
            if (!(s_Member_ContainerLayer.GetValue(this) as IDictionary).Contains(newLayer))
            {
                AddLayer(newLayer);
            }
            (s_Method_GetLayer.Invoke(this, new object[] { newLayer }) as VisualElement).Add(graphElement);
        }

        void SyncGroupNodes()
        {
            if (Controller == null)
            {
                foreach (var kv in ControlledSteps)
                {
                    RemoveElement(kv.Value);
                }
                ControlledSteps.Clear();
            }
            else
            {
                var deletedControllers = ControlledSteps.Keys.Except(Controller.StepsControllers).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(ControlledSteps[deletedController]);
                    ControlledSteps.Remove(deletedController);
                }

                foreach (var newController in Controller.StepsControllers.Except(ControlledSteps.Keys))
                {
                    var newElement = new StepView();
                    FastAddElement(newElement);
                    newElement.Controller = newController as StepController;
                    ControlledSteps.Add(newController, newElement);
                }
            }
        }

        public void SafeRemoveElement(GraphElement element)
        {
            StepView.inRemoveElement = true;

            RemoveElement(element);

            StepView.inRemoveElement = false;
        }

        void SyncTransitions(int change)
        {
            //if (change != ProcedureController.Change.Transition)
            //{
            // TODO: DATA EDGES SYNC
            //}

            if (change != ProcedureController.Change.DataEdge)
            {
                if (Controller == null)
                {
                    foreach (var element in ControlledFlowEdges.Values)
                    {
                        RemoveElement(element);
                    }
                    ControlledFlowEdges.Clear();
                }
                else
                {
                    var deletedControllers = ControlledFlowEdges.Keys.Except(Controller.TransitionsControllers).ToArray();

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = ControlledFlowEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                        ControlledFlowEdges.Remove(deletedController);
                    }

                    foreach (var newController in Controller.TransitionsControllers.Except(ControlledFlowEdges.Keys))
                    {
                        var edge = new FlowEdge();
                        //FastAddElement(newElement);
                        AddElement(edge);
                        UpdateFlowEdge(newController, edge);
                        if (edge.input != null && edge.output != null)
                        {
                            ControlledFlowEdges.Add(newController, edge);
                        }
                        else
                        {
                            RemoveElement(edge);
                        }
                    }
                }
            }
        }

        private void UpdateFlowEdge(TransitionController controller, FlowEdge edge)
        {
            edge.input = GetNode(controller.To)?.InputPort;
            edge.output = GetPort(controller.SourcePort);
            edge.input?.Connect(edge);
            edge.output?.Connect(edge);
            edge.Controller = controller;
        }

        private void FullySaveProcedure()
        {
            FullSave();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void DelayedRestore()
        {
            schedule.Execute(() => Reset()).StartingIn(2000);
        }

        [Serializable]
        private class ProcedureViewport
        {
            public Vector3 position;
            public Vector3 scale;
            public string procedureGuid;

            public void Update(ITransform transform, Procedure procedure)
            {
                position = transform.position;
                scale = transform.scale;
                procedureGuid = procedure ? procedure.Guid : null;
            }
        }

        [Serializable]
        private class ProcedureSettings
        {
            public bool testingIsOn;
            public bool minimapIsOn;
        }
    }
}
