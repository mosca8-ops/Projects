using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class TrafficNodeController : BaseNodeController<TrafficNode>, IOutputPortsProvider
    {
        public IEnumerable<IPortController> OutputPorts => m_outputPorts;

        private TrafficNodePortController m_portController;

        private List<IPortController> m_outputPorts;

        private HashSet<BaseTransition> m_inputTransitions;
        public TrafficNodePortController PortController => m_portController;
        public IReadOnlyList<TransitionController> TransitionsControllers => PortController.TransitionsControllers;
        public IReadOnlyList<BaseTransition> Transitions => Model.OutputTransitions;

        public bool EndIncomingFlows => Model.EndIncomingFlows;
        public int InputTransitions { get => Model.InputTransitionsCount; set => Model.InputTransitionsCount = value; }
        public int AcquiredTransitions => Model.InputTransitionsCount - Model.TransitionsToWait;
        public int TransitionsToWait => Model.TransitionsToWait;

        public TrafficNodeController(ProcedureController viewController, TrafficNode model) : base(viewController, model)
        {
            if (string.IsNullOrEmpty(model.Title))
            {
                model.Title = "Join & Split";
            }

            model.OnModified -= Model_OnModified;
            model.OnModified += Model_OnModified;

            m_inputTransitions = new HashSet<BaseTransition>();

            if(m_portController == null)
            {
                m_portController = new TrafficNodePortController(this, model);
            }

            m_outputPorts = new List<IPortController>() { m_portController };
        }

        public override void TransitionConnected(TransitionController transition)
        {
            base.TransitionConnected(transition);
            if(!m_inputTransitions.Contains(transition.Model) && !Model.OutputTransitions.Contains(transition.Model))
            {
                m_inputTransitions.Add(transition.Model);
                UpdateInputTransitions();
            }
        }

        public override void TransitionDisconnected(TransitionController transition)
        {
            base.TransitionDisconnected(transition);
            m_inputTransitions.Remove(transition.Model);
            UpdateInputTransitions();
        }

        private void UpdateInputTransitions()
        {
            var transitionsToRemove = new List<BaseTransition>();
            foreach(var transition in m_inputTransitions)
            {
                if(!transition || !transition.From || transition.To != Model)
                {
                    transitionsToRemove.Add(transition);
                }
            }
            foreach(var transition in transitionsToRemove)
            {
                m_inputTransitions.Remove(transition);
            }
            Model.InputTransitionsCount = m_inputTransitions.Count;
        }

        public override void ResetState()
        {
            base.ResetState();
            Model.Reset();
            NotifyChange(AnyThing);
        }

        protected override void ModelChanged(TrafficNode obj)
        {
            NotifyChange(AnyThing);
        }

        private void Model_OnModified(ProcedureObject obj)
        {
            if(obj == Model)
            {
                ApplyChanges();
            }
        }
    }
}
