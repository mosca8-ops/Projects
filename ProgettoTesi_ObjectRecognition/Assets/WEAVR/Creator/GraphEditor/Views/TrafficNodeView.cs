using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Profiling;

namespace TXT.WEAVR.Procedure
{

    class TrafficNodeView : BaseNodeView<TrafficNodeController>
    {
        private VisualElement m_flowOutput;
        private Label m_inputCount;
        private Label m_acquiredCount;
        private Label m_outputCount;
        private TrafficNodeOutputPort m_outputPort;

        public new TrafficNodeController Controller
        {
            get => base.Controller as TrafficNodeController;
            set
            {
                if (base.Controller != value)
                {
                    if(Controller != null)
                    {
                        Controller.PortController.UnregisterHandler(this);
                    }
                    base.Controller = value;
                    if (Controller != null)
                    {
                        Controller.PortController.RegisterHandler(this);
                    }
                }
            }
        }

        protected override void SelfChange()
        {
            base.SelfChange();
            if(Controller == null) { return; }

            if (m_acquiredCount != null)
            {
                m_acquiredCount.text = $"{Controller.AcquiredTransitions} /";
            }
            EnableInClassList("aggressive", Controller.EndIncomingFlows);
            m_inputCount.text = Controller.InputTransitions.ToString();
            m_outputCount.text = Controller.Transitions.Count.ToString();
        }

        public TrafficNodeView() : base("uxml/TrafficNode")
        {
            this.AddStyleSheetPath("TrafficNodeView");
            AddToClassList("trafficNodeView");

            //mainContainer.clippingOptions = ClippingOptions.NoClipping;
            //clippingOptions = ClippingOptions.NoClipping;

            m_inputCount = this.Q<Label>("input-count");
            m_acquiredCount = this.Q<Label>("acquired-count");
            m_outputCount = this.Q<Label>("output-count");

            m_flowOutput = this.Q("flow-outputs");

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            if (Controller == null) { return; }
            GetFirstAncestorOfType<ProcedureView>()?.RegisterFlowPort(Controller.PortController, m_outputPort);
            //if (!Application.isPlaying)
            //{
            //    m_acquiredCount.RemoveFromHierarchy();
            //}
        }

        public override bool HitTest(Vector2 localPoint)
        {
            return ContainsPoint(localPoint);
        }

        public override void OnControllerChanged(ref ControllerChangedEvent e)
        {
            base.OnControllerChanged(ref e);
            if(e.controller == Controller?.PortController)
            {
                SelfChange();
            }
        }

        protected override void OnNewController()
        {
            base.OnNewController();
            if (Controller != null)
            {
                if(m_outputPort != null)
                {
                    m_outputPort.RemoveFromHierarchy();
                }
                m_outputPort = TrafficNodeOutputPort.Create(Controller.PortController);
                m_flowOutput.Add(m_outputPort);
                if (panel != null)
                {
                    GetFirstAncestorOfType<ProcedureView>()?.RegisterFlowPort(Controller.PortController, m_outputPort);
                }
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
        }
    }
}