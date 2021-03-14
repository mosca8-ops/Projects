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

    public class ProcedureFlowsInfo : VisualElement
    {
        private const string k_tempProcedureName = "_ProcedureTester_";

        private static readonly Color[] k_FlowColors = {
            new Color(),

        };

        ProcedureHierarchyDrawer m_hierarchyDrawer;
        private Procedure m_procedure;
        private bool m_isTesting;
        private int m_flowColorIndex;

        public event Action<ProcedureDebugger, bool> InTestChanged;
        public event Action<ProcedureDebugger, bool> FastForwardDebugChanged;

        private Dictionary<ExecutionFlow, VisualElement> m_flowElements;

        public Procedure Procedure
        {
            get => m_procedure;
            set
            {
                if (m_procedure != value)
                {
                    m_procedure = value;
                    Clear();
                    Rebuild();
                }
            }
        }

        private ProcedureRunner m_procedureRunner;
        public ProcedureRunner CurrentProcedureRunner
        {
            get
            {
                if (!m_procedureRunner)
                {
                    m_procedureRunner = Weavr.TryGetInCurrentScene<ProcedureRunner>();
                    if (m_procedureRunner)
                    {
                        m_procedureRunner.OnFlowCreated?.RemoveListener(OnFlowCreated);
                        m_procedureRunner.OnFlowToBeKilled?.RemoveListener(OnFlowEnded);
                        m_procedureRunner.OnFlowEnded?.RemoveListener(OnFlowEnded);
                        m_procedureRunner.OnFlowCreated?.AddListener(OnFlowCreated);
                        m_procedureRunner.OnFlowToBeKilled?.AddListener(OnFlowEnded);
                        m_procedureRunner.OnFlowEnded?.AddListener(OnFlowEnded);
                    }
                }
                return m_procedureRunner;
            }
            set
            {
                if (m_procedureRunner != value)
                {
                    Debug.Log($"Setting Runner: {value}");
                    if (m_procedureRunner)
                    {
                        m_procedureRunner.OnFlowCreated.RemoveListener(OnFlowCreated);
                        m_procedureRunner.OnFlowToBeKilled.RemoveListener(OnFlowEnded);
                        m_procedureRunner.OnFlowEnded.RemoveListener(OnFlowEnded);
                    }
                    m_procedureRunner = value;
                    if (m_procedureRunner)
                    {
                        m_procedureRunner.OnFlowCreated.RemoveListener(OnFlowCreated);
                        m_procedureRunner.OnFlowToBeKilled.RemoveListener(OnFlowEnded);
                        m_procedureRunner.OnFlowEnded.RemoveListener(OnFlowEnded);
                        m_procedureRunner.OnFlowCreated.AddListener(OnFlowCreated);
                        m_procedureRunner.OnFlowToBeKilled.AddListener(OnFlowEnded);
                        m_procedureRunner.OnFlowEnded.AddListener(OnFlowEnded);
                    }
                }
            }
        }

        public ProcedureFlowsInfo()
        {
            name = "procedure-flows-info";
            this.AddStyleSheetPath("ProcedureFlowsInfo");

            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);
            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);

            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;

            AddToClassList("flow-info-container");
            m_flowElements = new Dictionary<ExecutionFlow, VisualElement>();
        }

        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange state)
        {
            Rebuild();
        }

        private void AttachedToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
        }

        private void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.playModeStateChanged -= EditorApplication_PlayModeStateChanged;
            //CurrentProcedureRunner = null;
        }

        private void Rebuild()
        {
            if (!Procedure) { return; }

            Clear();
            m_flowElements.Clear();

            if (!CurrentProcedureRunner)
            {
                CurrentProcedureRunner = Weavr.TryGetInCurrentScene<ProcedureRunner>();
            }
            if (CurrentProcedureRunner)
            {
                foreach(var flow in CurrentProcedureRunner.RunningFlows)
                {
                    OnFlowCreated(flow);
                }
                UpdateInfo();
            }
        }

        private void OnFlowEnded(ExecutionFlow flow)
        {
            flow.ContextChanged -= Flow_ContextChanged;

            if (m_flowElements.TryGetValue(flow, out VisualElement flowView))
            {
                flowView?.RemoveFromHierarchy();
                m_flowElements.Remove(flow);
            }
        }

        private void OnFlowCreated(ExecutionFlow flow)
        {
            if (!flow.IsPrimaryFlow) { return; }

            flow.ContextChanged -= Flow_ContextChanged;
            flow.ContextChanged += Flow_ContextChanged;

            var colors = ProcedureDefaults.Current.ColorPalette.GetGroup("ExecutionFlowColors");
            var flowView = new Label();
            flowView.AddToClassList("flow-info");
            var color = colors?.Count > 0 ? colors[m_flowColorIndex++ % colors.Count] : new Color(UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(0.5f, 1f), UnityEngine.Random.Range(0.5f, 1f), 0.6f);
            flowView.style.color = color;
            flowView.RegisterCallback<MouseDownEvent>(e => FlowClicked(flow, flowView));
            flowView.RegisterCallback<MouseOverEvent>(e => FlowOver(flow, flowView));
            flowView.RegisterCallback<MouseOutEvent>(e => FlowOut(flow, flowView, color));
            m_flowElements[flow] = flowView;
            Add(flowView);
        }

        private void FlowOut(ExecutionFlow flow, Label flowView, in Color color)
        {
            flowView.style.color = color;
        }

        private void FlowOver(ExecutionFlow flow, Label flowView)
        {
            flowView.style.color = new StyleColor(StyleKeyword.Null);
        }

        private void FlowClicked(ExecutionFlow flow, Label flowView)
        {
            if(flow.CurrentContext is GraphObject gObj)
            {
                ProcedureEditor.Instance.Select(gObj, true);
            }
        }

        private void Flow_ContextChanged(ExecutionFlow flow, IFlowContext newContext)
        {
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            foreach (var pair in m_flowElements)
            {
                if (pair.Key && pair.Value is TextElement textElem && pair.Key.CurrentContext is GraphObject currentContext)
                {
                    if (pair.Key.NextContext is GraphObject nextContext)
                    {
                        textElem.text = $"Flow [{pair.Key.Id}]: {GetObjectInfo(currentContext)} -> {GetObjectInfo(nextContext)}";
                    }
                    else
                    {
                        textElem.text = $"Flow [{pair.Key.Id}]: {GetObjectInfo(currentContext)}";
                    }
                }
            }
        }

        private static string GetObjectInfo(GraphObject obj)
        {
            switch (obj)
            {
                case GenericNode elem: return $"[{elem.Number}] {elem.Title}";
                case BaseTransition elem: return $"[{elem.From} --> {elem.To}";
                case TrafficNode elem: return $"[JOIN & SPLIT]";
            }
            return obj.ToString();
        }
    }
}
