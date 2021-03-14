using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class GenericNode : BaseNode, IFlowProvider, IProcedureStep, INetworkProcedureObjectsContainer
    {
        [SerializeField]
        protected string m_number;
        [SerializeField]
        [LongText]
        protected LocalizedString m_description;
        [SerializeField]
        protected bool m_isMandatory;
        [SerializeField]
        protected Hint m_hint;
        [SerializeField]
        protected bool m_precheck;
        [SerializeField]
        protected List<GraphObject> m_flowElements;

        public List<GraphObject> FlowElements => m_flowElements;

        public override bool CanBeShared => true;

        public Hint Hint
        {
            get => m_hint;
            set
            {
                if(m_hint != value)
                {
                    BeginChange();
                    m_hint = value;
                    PropertyChanged(nameof(Hint));
                }
            }
        }

        public bool IsMandatory
        {
            get => m_isMandatory;
            set
            {
                if(m_isMandatory != value)
                {
                    BeginChange();
                    m_isMandatory = value;
                    PropertyChanged(nameof(IsMandatory));
                }
            }
        }

        public string StepGUID => m_step != null ? m_step.StepGUID : Guid;

        string IProcedureStep.Title { get => m_step ? m_step.Title : Title; set => Title = value; }

        public string Number {
            get => m_step != null ? m_step.Number : m_number;
            set
            {
                if(m_number != value)
                {
                    BeginChange();
                    m_number = value;
                    PropertyChanged(nameof(Number));
                }
            }
        }
        public string Description
        {
            get => m_step != null ? m_step.Description : m_description?.CurrentValue;
            set
            {
                if(m_description != value)
                {
                    BeginChange();
                    m_description = value;
                    PropertyChanged(nameof(Description));
                }
            }
        }

        public void SetDescription(LocalizedString description)
        {
            if (description != null)
            {
                m_description = description.Clone();
            }
        }

        public bool ShouldPreCheckConditions
        {
            get => m_precheck;
            set
            {
                if(m_precheck != value)
                {
                    BeginChange();
                    m_precheck = value;
                    PropertyChanged(nameof(ShouldPreCheckConditions));
                }
            }
        }

        public IEnumerable<INetworkProcedureObject> NetworkObjects
        {
            get
            {
                List<INetworkProcedureObject> networkObjects = new List<INetworkProcedureObject>();
                for (int i = 0; i < FlowElements.Count; i++)
                {
                    if(FlowElements[i] is INetworkProcedureObjectsContainer childContainer)
                    {
                        networkObjects.AddRange(childContainer.NetworkObjects);
                    }
                }
                return networkObjects;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_title == null)
            {
                m_title = "New Step";
            }
            if (m_flowElements == null)
            {
                m_flowElements = new List<GraphObject>();
            }
            else
            {
                for (int i = 0; i < m_flowElements.Count; i++)
                {
                    if (!m_flowElements[i])
                    {
                        m_flowElements.RemoveAt(i--);
                        WeavrDebug.LogError($"{GetType().Name} - {m_title}", "Unable to identify a flow element and thus it was deleted.");
                    }
                }
            }
            if(m_hint == null)
            {
                m_hint = CreateInstance<Hint>();
            }
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            base.CollectProcedureObjects(list);
            foreach(var elem in m_flowElements)
            {
                elem.CollectProcedureObjects(list);
            }
        }

        public override ProcedureObject Clone()
        {
            var clone = base.Clone() as GenericNode;
            if(clone == null) { return clone; }

            for (int i = 0; i < clone.m_flowElements.Count; i++)
            {
                clone.m_flowElements[i] = clone.m_flowElements[i].Clone() as GraphObject;
            }

            clone.m_hint = Instantiate(m_hint);

            return clone;
        }

        protected override void OnAssignedToProcedure(Procedure value)
        {
            base.OnAssignedToProcedure(value);
            foreach (var elem in FlowElements)
            {
                if (elem) { elem.Procedure = value; }
            }
        }

        public bool Contains<T>() where T : GraphObject
        {
            foreach (var elem in m_flowElements)
            {
                if (elem is T)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains(Type type)
        {
            foreach (var elem in m_flowElements)
            {
                if (elem != null && (elem.GetType() == type || elem.GetType().IsSubclassOf(type)))
                {
                    return true;
                }
            }
            return false;
        }

        public List<IFlowElement> GetFlowElements()
        {
            List<IFlowElement> elements = new List<IFlowElement>();
            foreach (var elem in m_flowElements)
            {
                if (elem is IFlowElement)
                {
                    elements.Add(elem as IFlowElement);
                }
            }
            return elements;
        }
    }
}