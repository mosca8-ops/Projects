using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ToggleComponentAction : BaseReversibleAction, ITargetingObject, ISerializedNetworkProcedureObject
    {
        [SerializeField]
        [Tooltip("The object to toggle components in")]
        //[DoNotAutofill]
        [Draggable]
        private GameObject m_target;
        private GameObject m_oldTarget;
        [SerializeField]
        [Tooltip("The component to toggle")]
        [ArrayElement("m_componentsStringArray")]
        private string m_component;
        [SerializeField]
        [HideInInspector]
        private string[] m_componentsStringArray;
        [SerializeField]
        [Tooltip("Whether to enable or disable the component")]
        private bool m_enable;
        [SerializeField]
        private bool m_isGlobal = true;

        private Component[] m_componentsArray;
        private Component m_selectedBehaviour;

        private Component[] ComponentsArray
        {
            get => m_componentsArray;
            set
            {
                m_componentsArray = value;
                m_componentsStringArray = new string[m_componentsArray.Length];
                for (int i = 0; i < m_componentsArray.Length; i++)
                {
                    int suffix = 1;
                    for (int j = 0; j < i; j++)
                    {
                        if (m_componentsArray[i].GetType() == m_componentsArray[j].GetType())
                        {
                            suffix++;
                        }
                    }
                    m_componentsStringArray[i] = m_componentsArray[i].GetType().Name + (suffix > 1 ? (" " + suffix.ToString()) : "");
                }
                if (!m_componentsStringArray.Contains(m_component))
                {
                    m_component = m_componentsStringArray.Length > 0 ? m_componentsStringArray[0] : string.Empty;
                }
            }
        }


        public Object Target
        {
            get => m_target;
            set
            {
                GameObject newTarget = m_target;
                if (value is GameObject go)
                {
                    newTarget = go;
                }
                else if(value is Component c)
                {
                    newTarget = c.gameObject;
                }
                else if(value == null)
                {
                    newTarget = null;
                }
                if(newTarget != m_target)
                {
                    BeginChange();
                    m_target = newTarget;
                    OnValidate();
                    PropertyChanged(nameof(Target));
                }
            }
        }

        public string Component
        {
            get => m_component;
            set
            {
                if (m_component != value)
                {
                    BeginChange();
                    m_component = value;
                    PropertyChanged(nameof(Component));
                }
            }
        }

        public bool ShouldEnable
        {
            get => m_enable;
            set
            {
                if (m_enable != value)
                {
                    BeginChange();
                    m_enable = value;
                    PropertyChanged(nameof(ShouldEnable));
                }
            }
        }

        public string TargetFieldName => nameof(m_target);

        public string IsGlobalFieldName => nameof(m_isGlobal);

        public bool IsGlobal => m_isGlobal;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_oldTarget = m_target;
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            var split = m_component.Split(' ');
            if(split.Length <= 1)
            {
                m_selectedBehaviour = m_target.GetComponent(m_component);
            }
            else
            {
                var components = m_target.GetComponents<Component>();
                int index = int.Parse(split[1]) - 1;

                for (int i = 0; i < components.Length; i++)
                {
                    if(components[i].GetType().Name == split[0] && --index < 0)
                    {
                        m_selectedBehaviour = components[i];
                        break;
                    }
                }
            }
        }

        public override bool Execute(float dt)
        {
            if(m_selectedBehaviour)
            {
                ToggleComponent(m_enable);
            }
            return true;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (m_target != m_oldTarget)
            {
                m_oldTarget = m_target;
                if (m_target)
                {
                    List<Component> components = new List<Component>();
                    foreach (var item in m_target.GetComponents<Component>())
                    {
                        if(item is Behaviour || item is Collider || item is Renderer)
                        {
                            components.Add(item);
                        }
                    }
                    ComponentsArray = components.ToArray();
                }
                else if (ReferenceEquals(m_target, null))
                {
                    ComponentsArray = new Component[0];
                }
            }
        }

        public override string GetDescription()
        {
            if (m_target != null)
            {
                if (m_enable)
                {
                    return $"Enable component {m_target.name}.{m_component}";
                }
                else
                {
                    return $"Disable component {m_target.name}.{m_component}";
                }
            }
            return "Missing Target";
        }

        public override void FastForward()
        {
            base.FastForward();
            if (!m_selectedBehaviour) { return; }
            ToggleComponent(m_enable);
        }

        private void ToggleComponent(bool enable)
        {
            if (m_selectedBehaviour is Behaviour behaviour)
            {
                behaviour.enabled = enable;
            }
            else if (m_selectedBehaviour is Collider collider)
            {
                collider.enabled = enable;
            }
            else if(m_selectedBehaviour is Renderer renderer)
            {
                renderer.enabled = enable;
            }
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            if (m_selectedBehaviour != null && RevertOnExit)
            {
                ToggleComponent(!m_enable);
            }
        }
    }
}
