using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class GlobalToggleComponentAction : BaseReversibleAction, ISerializedNetworkProcedureObject
    {
        [SerializeField]
        [Tooltip("The component to enable or disable")]
        [HideInInspector]
        private string m_component;
        [SerializeField]
        [Tooltip("Whether to enable or disable the selected component on all objects in scene")]
        private ValueProxyBool m_enable;
        [SerializeField]
        [Tooltip("Whether to consider inactive objects or not")]
        private bool m_inactiveObjects;

        [SerializeField]
        private bool m_isGlobal = true;

        [System.NonSerialized]
        private Type m_componentType;
        private Dictionary<Component, bool> m_components;

        public string ComponentTypename
        {
            get => m_component;
            set
            {
                if(m_component != value)
                {
                    BeginChange();
                    m_component = value;
                    PropertyChanged(nameof(ComponentTypename));
                }
            }
        }

        public Type ComponentType
        {
            get
            {
                if(m_componentType == null && !string.IsNullOrEmpty(m_component))
                {
                    m_componentType = Type.GetType(m_component);
                }
                return m_componentType;
            }
            set
            {
                if(m_componentType != value)
                {
                    BeginChange();
                    m_componentType = value;
                    m_component = m_componentType?.AssemblyQualifiedName;
                    PropertyChanged(nameof(ComponentType));
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

        public bool IncludeInactive
        {
            get => m_inactiveObjects;
            set
            {
                if (m_inactiveObjects != value)
                {
                    BeginChange();
                    m_inactiveObjects = value;
                    PropertyChanged(nameof(IncludeInactive));
                }
            }
        }

        public string IsGlobalFieldName => nameof(m_isGlobal);

        public bool IsGlobal => m_isGlobal;

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_components == null)
            {
                m_components = new Dictionary<Component, bool>();
            }
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            m_components.Clear();
            foreach (var rootObject in UnityEngine.SceneManagement.SceneManager.GetSceneByPath(Procedure.ScenePath).GetRootGameObjects())
            {
                if (!rootObject.activeInHierarchy && !m_inactiveObjects) { continue; }
                foreach (var component in rootObject.GetComponentsInChildren(ComponentType, m_inactiveObjects))
                {
                    m_components[component] = IsComponentEnabled(component);
                }
            }
        }

        public override bool Execute(float dt)
        {
            foreach (var componentPair in m_components)
            {
                ToggleComponent(componentPair.Key, m_enable);
            }
            return true;
        }

        public override string GetDescription()
        {
            return $"{(m_enable? "Enable" : "Disable")} all components {ComponentType?.Name} {(m_inactiveObjects ? "(include inactive objects)" : "(exclude inactive objects)")})";
        }

        public override void FastForward()
        {
            base.FastForward();
            foreach (var componentPair in m_components)
            {
                ToggleComponent(componentPair.Key, m_enable);
            }
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            foreach (var componentPair in m_components)
            {
                ToggleComponent(componentPair.Key, componentPair.Value);
            }
        }

        private void ToggleComponent(Component component, bool enable)
        {
            if (component is Behaviour behaviour)
            {
                behaviour.enabled = enable;
            }
            else if (component is Collider collider)
            {
                collider.enabled = enable;
            }
            else if (component is Renderer renderer)
            {
                renderer.enabled = enable;
            }
        }

        private bool IsComponentEnabled(Component component)
        {
            if (component is Behaviour behaviour)
            {
                return behaviour.enabled;
            }
            else if (component is Collider collider)
            {
                return collider.enabled;
            }
            else if (component is Renderer renderer)
            {
                return renderer.enabled;
            }
            return true;
        }
    }
}
