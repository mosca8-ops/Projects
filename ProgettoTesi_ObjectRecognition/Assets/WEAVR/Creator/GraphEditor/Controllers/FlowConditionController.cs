using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class FlowConditionController : ProcedureObjectController<FlowCondition>
    {
        private FlowConditionPortController m_portController;
        private List<IProgressElement> m_progressElements = new List<IProgressElement>();
        private bool m_isMainFlow;

        [System.Flags]
        public enum SyncType
        {
            None = 0,
            Global = 1,
            Local = 2,
            Both = Global | Local,
        }

        public class Change
        {
            public const int IsMainFlow = 99;
        }

        public FlowConditionPortController PortController => m_portController;
        public string Description => Model.GetDescription();
        public bool WasTriggered => Model.Triggered;

        public bool Negated => Model.IsNegated || (Model.Child is ConditionAnd && IsNegated(Model.Child as ConditionAnd));

        private bool IsNegated(ConditionAnd condition)
        {
            return condition.IsNegated ^ (condition.Children.Count == 1 && condition.Children[0].IsNegated);
        }

        public bool IsProgressElement => m_progressElements.Count > 0;
        public float Progress => GetProgressAverage();
        
        public ConditionsController Container { get; private set; }

        public bool IsMainFlow {
            get => m_isMainFlow;
            set
            {
                if (PortController != null)
                {
                    PortController.IsMainFlow = value;
                }
                if (m_isMainFlow != value)
                {
                    m_isMainFlow = value;
                    NotifyChange(Change.IsMainFlow);
                }
            }
        }

        public FlowConditionController(FlowCondition model, GraphObjectController owner) : base(owner.ViewController, model)
        {
            model.OnModified -= ModelModified;
            model.OnModified += ModelModified;

            Container = owner as ConditionsController;

            if (m_portController == null)
            {
                m_portController = new FlowConditionPortController(model, this, owner.ViewController);
                m_portController.IsMainFlow = m_portController.IsMainFlow;
            }

            UpdateProgressElement();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (m_portController != null)
            {
                m_portController.OnDisable();
            }
        }

        public override void ResetState()
        {
            base.ResetState();
            Model.MuteEvents = true;
            Model.Triggered = false;
            Model.MuteEvents = false;

            NotifyChange(AnyThing);
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            UpdateProgressElement();
        }

        private void UpdateProgressElement()
        {
            m_progressElements.Clear();
            GetProgressElements(Model.Condition, m_progressElements);
        }

        private float GetProgressAverage()
        {
            if(m_progressElements.Count == 0)
            {
                return 0;
            }
            float sum = 0;
            for (int i = 0; i < m_progressElements.Count; i++)
            {
                sum += m_progressElements[i].Progress;
            }

            return sum / m_progressElements.Count;
        }

        public SyncType GetSyncType()
        {
            SyncType syncType = SyncType.None;
            return Model && Model.Child ? GetSyncType(Model.Child, ref syncType) : syncType;
        }

        private SyncType GetSyncType(BaseCondition condition, ref SyncType syncType)
        {
            if (condition.CanBeShared)
            {
                syncType |= condition.IsGlobal ? SyncType.Global : SyncType.Local;
            }
            if (syncType == SyncType.Both)
            {
                return syncType;
            }
            if (condition is IConditionsContainer container)
            {
                foreach (var child in container.Children)
                {
                    if (child && GetSyncType(child, ref syncType) == SyncType.Both)
                    {
                        return syncType;
                    }
                }
            }
            else if(condition is IConditionParent parent && parent.Child)
            {
                return GetSyncType(parent.Child, ref syncType);
            }
            return syncType;
        }

        private void GetProgressElements(BaseCondition condition, List<IProgressElement> elements)
        {
            if(condition is IProgressElement progressElement)
            {
                elements.Add(progressElement);
            }
            if(condition is IConditionsContainer)
            {
                foreach(var child in (condition as IConditionsContainer).Children)
                {
                    GetProgressElements(child, elements);
                }
            }
            else if(condition is IConditionParent)
            {
                GetProgressElements((condition as IConditionParent).Child, elements);
            }
        }

        protected void ModelModified(ProcedureObject model)
        {
            if (model == Model)
            {
                ApplyChanges();
                NotifyChange(ModelHasChanges);
            }
        }
    }
}
