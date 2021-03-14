using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace TXT.WEAVR.Procedure
{

    class FlowEdge : Edge, IControlledElement, ISettableControlledElement<TransitionController>
    {
        protected static readonly ContextState[] s_states =
        {
            ContextState.Standby,
            ContextState.Ready,
            ContextState.Running,
            ContextState.Finished,
            ContextState.Faulted,
            ContextState.ForceStopped,
        };

        private TextBadge m_textBadge;
        private FlowBlocksGraphContainer m_actionsContainer;
        
        protected ContextState m_lastState;

        private TransitionController m_controller;
        
        public TransitionController Controller
        {
            get => m_controller;
            set
            {
                if(m_controller != value)
                {
                    if (m_controller != null)
                    {
                        m_controller.UnregisterHandler(this);
                    }
                    m_controller = value;
                    OnNewController();
                    if (m_controller != null)
                    {
                        m_controller.RegisterHandler(this);
                    }
                }
            }
        }

        Controller IControlledElement.Controller => m_controller;

        Color? m_inputEdgeColor;
        public Color InputEdgeColor
        {
            get
            {
                return m_inputEdgeColor ?? edgeControl.inputColor;
            }
            set
            {
                m_inputEdgeColor = value;
            }
        }

        Color? m_outputEdgeColor;
        public Color OutputEdgeColor
        {
            get
            {
                return m_outputEdgeColor ?? edgeControl.outputColor;
            }
            set
            {
                m_outputEdgeColor = value;
            }
        }

        public FlowEdge()
        {
            this.AddStyleSheetPath("FlowConnector");
            AddToClassList("flowEdge");

            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);

            m_actionsContainer = new FlowBlocksGraphContainer();
            m_actionsContainer.AddStyleSheetPath("FlowConnector");
            m_actionsContainer.BlocksContainer.AddToClassList("actions-container");

            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if (m_actionsContainer?.panel != null)
            {
                m_actionsContainer.SetPosition(new Rect(edgeControl.layout.center, Vector2.zero));
            }
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            SearchHub.Current.SearchValueChanged -= SearchValueChanged;
            SearchHub.Current.SearchValueChanged += SearchValueChanged;

            if (!string.IsNullOrEmpty(SearchHub.Current.CurrentSearchValue))
            {
                SearchValueChanged(SearchHub.Current.CurrentSearchValue);
            }
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if(m_actionsContainer?.panel != null)
            {
                m_actionsContainer.RemoveFromHierarchy();
            }
        }

        private void SearchValueChanged(string newValue)
        {
            if (panel == null)
            {
                SearchHub.Current.SearchValueChanged -= SearchValueChanged;
                return;
            }
            if (Controller == null)
            {
                return;
            }
            newValue = newValue?.ToLower();
            EnableInClassList("searchPositive", !string.IsNullOrEmpty(newValue) && Controller.Model.Guid.ToLower().StartsWith(newValue));
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);

            if(styles.TryGetValue(new CustomStyleProperty<Color>("--input-edge-color"), out Color v)) { m_inputEdgeColor = v; } else { m_inputEdgeColor = null; }
            if(styles.TryGetValue(new CustomStyleProperty<Color>("--output-edge-color"), out Color v2)) { m_outputEdgeColor = v2; } else { m_outputEdgeColor = null; }
        }

        private void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            output?.RemoveFromClassList("with-actions");
        }

        private void AttachedToPanel(AttachToPanelEvent evt)
        {
            edgeControl.AddToClassList("edge-control");
            if (m_textBadge == null && edgeControl != null && edgeControl.panel != null)
            {
                m_textBadge = new TextBadge();
                m_textBadge.style.position = Position.Absolute;
                edgeControl.Add(m_textBadge);
                m_textBadge.RegisterCallback<MouseDownEvent>(Badge_MouseDown);
            }

            if (m_textBadge != null)
            {
                m_textBadge.visible = false;
                UpdateActionsBadge();
            }
        }

        private void Badge_MouseDown(MouseDownEvent evt)
        {
            if(evt.clickCount != 1) { return; }
            if (evt.ctrlKey)
            {
                GetFirstAncestorOfType<GraphView>()?.AddToSelection(this);
            }
            else
            {
                var graphView = GetFirstAncestorOfType<GraphView>();
                graphView?.ClearSelection();
                graphView?.AddToSelection(this);
            }
        }

        protected virtual void OnNewController()
        {
            if (Controller != null)
            {
                viewDataKey = ComputePersistenceKey();
                schedule.Execute(() =>
                {
                    if(m_textBadge != null && m_textBadge.visible)
                    {
                        BringToFront();
                    }
                }).StartingIn(200);
            }
        }

        public string ComputePersistenceKey()
        {
            return Controller != null ? $"GraphObject-{Controller.Model?.GetType().Name}-{Controller.Model.Guid}" : null;
        }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if(e.controller == m_controller)
            {
                SelfChange();
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if(m_controller != null)
            {
                BringToFront();
                ProcedureObjectInspector.Selected = m_controller;
                if (IsOnlyOneSelected())
                {
                    ShowActionsContainer();
                }
            }
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            if (ProcedureObjectInspector.Selected == Controller)
            {
                schedule.Execute(() =>
                {
                    if (ProcedureObjectInspector.Selected == Controller && panel != null)
                    {
                        ProcedureObjectInspector.Selected = null;
                    }
                }).StartingIn(100);
            }
            GetFirstAncestorOfType<GraphView>()?.RemoveElement(m_actionsContainer);
        }

        private void UpdateStateChanges()
        {
            if (m_lastState == Controller.CurrentState) { return; }

            m_lastState = Controller.CurrentState;

            if (Controller.CurrentState != ContextState.Standby)
            {
                foreach (ContextState state in s_states)
                {
                    if (Controller.CurrentState.HasFlag(state))
                    {
                        AddToClassList(state.ToString());
                        output?.AddToClassList(state.ToString());
                        input?.AddToClassList(state.ToString());
                    }
                    else
                    {
                        RemoveFromClassList(state.ToString());
                        output?.RemoveFromClassList(state.ToString());
                        input?.RemoveFromClassList(state.ToString());
                    }
                }
                //AddToClassList(Controller.CurrentState.ToString());
            }
            else
            {
                foreach (ContextState state in s_states)
                {
                    RemoveFromClassList(state.ToString());
                    output?.RemoveFromClassList(state.ToString());
                    input?.RemoveFromClassList(state.ToString());
                }
            }

            OnControllerStateChanged();
        }

        protected virtual void OnControllerStateChanged()
        {

        }

        private void SelfChange()
        {
            if(m_controller == null) { return; }

            if(panel != null)
            {
                UpdateActionsBadge();
                UpdateStateChanges();
            }

            EnableInClassList("mandatory", Controller.IsMainFlow);

            EnableInClassList("high-priority", m_controller.Priority != BaseTransition.k_DefaultPriority);

            if ((IsOnlyOneSelected() || Controller.CurrentState.HasFlag(ContextState.Finished)) && m_actionsContainer.panel == null)
            {
                ShowActionsContainer();
            }
            else if (m_actionsContainer.panel != null)
            {
                m_actionsContainer.BlocksContainer.RefreshContext(Controller.ActionControllers);
                if (Controller.ActionControllers.Count == 0)
                {
                    GetFirstAncestorOfType<GraphView>().RemoveElement(m_actionsContainer);
                }
            }
        }

        private void ShowActionsContainer()
        {
            if (Controller.ActionControllers.Count > 0)
            {
                m_actionsContainer.BlocksContainer.RefreshContext(Controller.ActionControllers);
                GetFirstAncestorOfType<GraphView>().AddElement(m_actionsContainer);
                m_actionsContainer.SetPosition(new Rect(edgeControl.layout.center, Vector2.zero));
                BringToFront();
            }
        }

        protected bool IsOnlyOneSelected()
        {
            var graphView = GetFirstAncestorOfType<GraphView>();

            return graphView != null && graphView.selection.Count == 1 && graphView.selection[0] == this;
        }

        private void UpdateActionsBadge()
        {
            if(Controller == null || m_textBadge == null) { return; }
            int actionsCount = Controller.ActionControllers.Count;
            m_textBadge.mainText = actionsCount.ToString();
            m_textBadge.visible = actionsCount > 0;

            input?.EnableInClassList("with-actions", actionsCount > 0);
            output?.EnableInClassList("with-actions", actionsCount > 0);

            if (actionsCount > 0)
            {
                BringToFront();
            }

            if (actionsCount == 1)
            {
                m_textBadge.badgeText = $"{actionsCount} Action";
            }
            else
            {
                m_textBadge.badgeText = $"{actionsCount} Actions";
            }
        }
    }
}
