using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class SetValueAction : BaseAction, IFlowContextClosedElement, ITargetingObject, ISerializedNetworkProcedureObject
    {
        [SerializeField]
        [Tooltip("The target object to change a value in")]
        [Draggable]
        private GameObject m_target;
        [SerializeField]
        [Tooltip("The property to change")]
        [PropertyDataFrom(nameof(m_target))]
        private Property m_property;
        [SerializeField]
        [Tooltip("The new value to apply to the property")]
        [GenericValueTypeFrom(nameof(m_property), true)]
        private GenericValue m_value;

        [SerializeField]
        [HideInInspector]
        private bool m_undoOnExit;

        #region [  ISerializedNetworkProcedureObject IMPLEMENTATION  ]

        [SerializeField]
        private bool m_isGlobal = true;
        public string IsGlobalFieldName => nameof(m_isGlobal);
        public bool IsGlobal => m_isGlobal;

        #endregion

        private object m_previousValue;

        public bool RevertOnExit {
            get => m_undoOnExit;
            set
            {
                if(m_undoOnExit != value)
                {
                    BeginChange();
                    m_undoOnExit = value;
                    PropertyChanged(nameof(RevertOnExit));
                }
            }
        }

        public Object Target {
            get => m_target;
            set
            {
                GameObject newTarget = m_target;
                if(value is GameObject go)
                {
                    newTarget = go;
                }
                else if(value is Component c)
                {
                    newTarget = c.gameObject;
                }
                else if(!value)
                {
                    newTarget = null;
                }

                if(m_target != newTarget)
                {
                    BeginChange();
                    m_target = newTarget;
                    PropertyChanged(nameof(Target));
                }
            }
        }

        public string TargetFieldName => nameof(m_target);

        public string PropertyPath
        {
            get => m_property?.Path;
            set
            {
                if (m_property == null)
                {
                    m_property = new Property();
                }
                if (m_property.Path != value)
                {
                    BeginChange();
                    m_property.Path = value;
                    PropertyChanged(nameof(PropertyPath));
                }
            }
        }

        public object Value
        {
            get => m_value.Value;
            set => m_value.Value = value;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_property == null)
            {
                m_property = new Property();
            }
            if(m_value == null)
            {
                m_value = new GenericValue();
            }
            m_value.ReferencesResolver = ReferenceResolver;
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            if (m_undoOnExit)
            {
                m_previousValue = m_property.Get(m_target);
            }
        }

        public override bool Execute(float dt)
        {
            m_property.Set(m_target, m_value.Value);
            return true;
        }

        public override void FastForward()
        {
            base.FastForward();
            m_property.Set(m_target, m_value.Value);
        }

        public override string GetDescription()
        {
            var value = m_value?.Value;
            string valueString = m_value != null && m_value.IsVariableValue ? $"[{m_value.VariableName}]" : value is Object unityObj && unityObj ? unityObj.name : value == null ? "null" : value.ToString();
            return !m_target ? "Target not set" :
                               m_property == null || string.IsNullOrEmpty(m_property.Path) ? $"Set {m_target.name}.[ ? ]" :
                               $"Set {m_target.name}.{m_property.PropertyName} = {valueString}";
        }

        public void OnContextExit(ExecutionFlow flow)
        {
            m_property.Set(m_target, m_previousValue);
        }
    }
}
