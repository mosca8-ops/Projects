using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/Groups/Interaction Controllers Group")]
    public class InteractionControllerGroup : MonoBehaviour
    {

        [Serializable]
        private class UnityEventBool : UnityEvent<bool> { }
        public bool useUnityEvents = true;
        [Button(nameof(AssignChildren))]
        public bool startEnabled = true;
        //[Tooltip("Whether to enable/disable when this component is enabled/disabled")]
        //public bool useUnityMessages = true;
        public AbstractInteractionController[] controllers;

        private bool m_isActive;

        [IgnoreStateSerialization]
        public bool IsActive
        {
            get { return m_isActive; }
            set
            {
                if(m_isActive != value)
                {
                    m_isActive = value;
                    for (int i = 0; i < controllers.Length; i++)
                    {
                        if (controllers[i] != null)
                        {
                            controllers[i].enabled = value;
                        }
                    }
                    m_onToggle.Invoke(value);
                    if (value)
                    {
                        m_onEnable.Invoke();
                    }
                    else
                    {
                        m_onDisable.Invoke();
                    }
                    if (useUnityEvents)
                    {
                        enabled = value;
                    }
                }
            }
        }

        [SerializeField]
        private UnityEvent m_onEnable;
        [SerializeField]
        private UnityEvent m_onDisable;
        [SerializeField]
        private UnityEventBool m_onToggle;

        private void Reset()
        {
            if (controllers == null)
            {
                controllers = new AbstractInteractionController[0];
            }
            AssignChildren();
        }

        private void OnValidate()
        {
            if(controllers == null)
            {
                controllers = new AbstractInteractionController[0];
            }
        }

        private void Start()
        {
            IsActive = startEnabled;
        }

        private void OnEnable()
        {
            if (useUnityEvents)
            {
                Enable();
            }
        }

        private void Enable()
        {
            IsActive = true;
        }

        public void Toggle()
        {
            IsActive = !IsActive;
        }

        private void AssignChildren()
        {
            var current = new List<AbstractInteractionController>(controllers);
            foreach(var child in GetComponentsInChildren<AbstractInteractionController>(true))
            {
                if (!current.Contains(child))
                {
                    current.Add(child);
                }
            }
            controllers = current.ToArray();
        }

        private void OnDisable()
        {
            if (useUnityEvents)
            {
                Disable();
            }
        }

        private void Disable()
        {
            IsActive = false;
        }
    }
}
