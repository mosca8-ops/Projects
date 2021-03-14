using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace TXT.WEAVR.Procedure
{

    class ProcedureViewFooter : VisualElement
    {

        private VisualElement m_stateContainer;
        private Label m_execMode;
        private VisualElement m_debugNode;
        private Label m_debugNodeLabel;
        private Image m_stateIcon;

        private Button m_procedureButton;

        private VisualElement m_statsContainer;
        private Image m_statusIcon;
        private Label m_stepsCount;
        private Label m_nodesCount;
        private Label m_edgesCount;

        private IVisualElementScheduledItem m_lastAnimation;
        private TimeStateHandler m_timeHandler;

        Color? m_refreshColor;
        public Color RefreshColor
        {
            get
            {
                return m_refreshColor ?? Color.white;
            }
            set
            {
                m_refreshColor = value;
            }
        }

        Color? m_refreshBackgroundColor;
        public Color RefreshBackgroundColor
        {
            get
            {
                return m_refreshBackgroundColor ?? Color.black;
            }
            set
            {
                m_refreshBackgroundColor = value;
            }
        }

        Color? m_defaultBackgroundColor;
        public Color DefaultBackgroundColor
        {
            get
            {
                return m_defaultBackgroundColor ?? Color.black;
            }
            set
            {
                m_defaultBackgroundColor = value;
            }
        }

        private Color? m_descriptionDefaultColor;

        [NonSerialized]
        private Procedure m_currentProcedure;

        public Procedure CurrentProcedure
        {
            get => m_currentProcedure;
            set
            {
                if(m_currentProcedure != value)
                {
                    if (m_currentProcedure && m_currentProcedure.Configuration)
                    {
                        m_currentProcedure.Configuration.ExecutionModeChanged -= Configuration_ExecutionModeChanged;
                    }
                    m_currentProcedure = value;
                    StatsVisibility = m_currentProcedure;
                    m_debugNode.visible = m_currentProcedure;
                    if (m_currentProcedure)
                    {
                        if (m_currentProcedure.Configuration)
                        {
                            m_currentProcedure.Configuration.ExecutionModeChanged -= Configuration_ExecutionModeChanged;
                            m_currentProcedure.Configuration.ExecutionModeChanged += Configuration_ExecutionModeChanged;
                        }
                        RemoveFromClassList("error");
                        AnimateChange();
                    }
                }
            }
        }

        private void Configuration_ExecutionModeChanged(ExecutionMode obj)
        {
            if(m_execMode != null)
            {
                m_execMode.text = CurrentProcedure.DefaultExecutionMode ? CurrentProcedure.DefaultExecutionMode.ModeName : string.Empty;
            }
        }

        public bool StatsVisibility { 
            get => m_statsContainer.visible;
            set
            {
                m_statsContainer.visible = value;
                m_stateContainer.visible = value;
            }
        }
        
        public ProcedureViewFooter()
        {
            var tpl = EditorGUIUtility.Load(WeavrEditor.PATH + "Creator/Resources/uxml/ProcedureViewFooter.uxml") as VisualTreeAsset;
            tpl?.CloneTree(this);

            AddToClassList("procedureViewFooter");

            m_procedureButton = this.Q<Button>("procedure");
            m_procedureButton.clickable.clicked += () =>
            {
                if (CurrentProcedure)
                {
                    //Selection.activeObject = CurrentProcedure;
                    ProcedureObjectInspector.Selected = CurrentProcedure;
                    EditorGUIUtility.PingObject(CurrentProcedure);
                }
            };

            m_statsContainer = this.Q("stats-container");
            m_statusIcon = this.Q<Image>("status-icon");
            m_stepsCount = this.Q<Label>("steps-count");
            m_nodesCount = this.Q<Label>("nodes-count");
            m_edgesCount = this.Q<Label>("edges-count");

            m_statsContainer.visible = false;

            m_stateContainer = this.Q("state-container");
            m_stateIcon = this.Q<Image>("state-icon");
            m_execMode = this.Q<Label>("exec-mode");
            m_debugNode = this.Q("debug-node");
            m_debugNodeLabel = this.Q<Label>("debug-node-label");

            m_stateContainer.visible = false;

            m_descriptionDefaultColor = Color.grey;

            m_debugNodeLabel.RegisterCallback<MouseDownEvent>(OnDebugNodeClicked);
            m_statsContainer.RegisterCallback<MouseDownEvent>(OnStatsClicked);

            RegisterCallback<CustomStyleResolvedEvent>(evt => OnCustomStyleResolved(evt.customStyle));
        }

        private void OnDebugNodeClicked(MouseDownEvent evt)
        {
            if (evt.button == 0 && CurrentProcedure && CurrentProcedure.Graph.DebugStartNodes.Count > 0)
            {
                ProcedureEditor.Instance.Select(CurrentProcedure.Graph.DebugStartNodes.FirstOrDefault(), true);
            }
        }

        private void OnStatsClicked(MouseDownEvent evt)
        {
            if(evt.button == 0 && CurrentProcedure && CurrentProcedure.Graph)
            {
                ProcedureObjectInspector.Selected = CurrentProcedure.Graph;
            }
        }

        protected virtual void OnCustomStyleResolved(ICustomStyle styles)
        {
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--refresh-color"), out Color v)) { m_refreshColor = v; } else { m_refreshColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--background-refresh-color"), out v)) { m_refreshBackgroundColor = v; } else { m_refreshBackgroundColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--background-default-color"), out v)) { m_defaultBackgroundColor = v; } else { m_defaultBackgroundColor = null; }
            //m_descriptionDefaultColor = m_description.style.color;

            AnimateChange();
        }

        private void AnimateChange()
        {
            var path = AssetDatabase.GetAssetOrScenePath(m_currentProcedure);
            if (string.IsNullOrEmpty(path))
            {
                schedule.Execute(() => m_procedureButton.text = AssetDatabase.GetAssetOrScenePath(m_currentProcedure)).Until(() => !string.IsNullOrEmpty(AssetDatabase.GetAssetOrScenePath(m_currentProcedure)));
            }
            else
            {
                m_procedureButton.text = path;
            }
            if (!m_descriptionDefaultColor.HasValue && m_procedureButton.resolvedStyle.color != Color.clear)
            {
                m_descriptionDefaultColor = m_procedureButton.resolvedStyle.color;
            }
            if (m_descriptionDefaultColor.HasValue)
            {
                m_lastAnimation?.Pause();
                m_timeHandler.Invalidate();
                m_lastAnimation = schedule.Execute(AnimateChangeDraw).Every(20).ForDuration(1200);
            }
        }

        private void AnimateChangeDraw(TimerState time)
        {
            float t = m_timeHandler.GetElapsed(time);
            m_procedureButton.style.color = Color.Lerp(RefreshColor, m_descriptionDefaultColor.Value, t);
            style.backgroundColor = Color.Lerp(RefreshBackgroundColor, DefaultBackgroundColor, t);
        }

        public void UpdateValues(ProcedureView procedureView)
        {
            m_stepsCount.text = procedureView.Controller.Steps.Count.ToString();
            m_nodesCount.text = procedureView.Controller.Nodes.Count.ToString();
            m_edgesCount.text = procedureView.Controller.Transitions.Count.ToString();
            
            EnableInClassList("bad-graph", CurrentProcedure && CurrentProcedure.Graph && CurrentProcedure.Graph.HasIssues);

            EnableInClassList("testing-procedure-active", !Application.isPlaying && procedureView.Debugger.IsTestActive);
            EnableInClassList("testing-procedure-running", Application.isPlaying && procedureView.Debugger.IsTestActive && procedureView.Debugger.CurrentProcedureRunner.CurrentProcedure == CurrentProcedure);

            EnableInClassList("has-debug-node", procedureView.Controller.DebugStartNodes.Count > 0);

            m_execMode.text = CurrentProcedure.DefaultExecutionMode ? CurrentProcedure.DefaultExecutionMode.ModeName : string.Empty;

            if (procedureView.Debugger != null)
            {
                procedureView.Debugger.FastForwardDebugChanged -= Debugger_FastForwardDebugChanged;
                procedureView.Debugger.FastForwardDebugChanged += Debugger_FastForwardDebugChanged;

                Debugger_FastForwardDebugChanged(procedureView.Debugger, procedureView.Debugger.IsFastForwardDebugActive);
            }
            else
            {
                m_debugNode.visible = false;
            }
        }

        private void Debugger_FastForwardDebugChanged(ProcedureDebugger debugger, bool value)
        {
            if (value && CurrentProcedure && CurrentProcedure.Graph.DebugStartNodes.Count > 0)
            {
                m_debugNode.visible = true;
                var debugNode = CurrentProcedure.Graph.DebugStartNodes.FirstOrDefault();
                m_debugNodeLabel.text = !debugNode ? string.Empty : debugNode is IProcedureStep s ? $"{s.Number} - {debugNode.Title}" : debugNode.Title;
            }
            else
            {
                m_debugNode.visible = false;
            }
        }
    }
}
