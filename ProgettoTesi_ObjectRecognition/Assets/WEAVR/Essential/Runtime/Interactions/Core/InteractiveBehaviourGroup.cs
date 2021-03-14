using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/Groups/Interactive Behaviours Group")]
    public class InteractiveBehaviourGroup : MonoBehaviour
    {

        [Serializable]
        private class UnityEventBool : UnityEvent<bool> { }

        [Button(nameof(AssignChildren))]
        public bool startEnabled = true;
        //[Tooltip("Whether to enable/disable when this component is enabled/disabled")]
        //public bool useUnityMessages = true;
        public AbstractInteractiveBehaviour[] behaviours;

        [SerializeField]
        private UnityEvent m_onEnable;
        [SerializeField]
        private UnityEvent m_onDisable;
        [SerializeField]
        private UnityEventBool m_onToggle;

        private void OnValidate()
        {
            if(behaviours == null)
            {
                behaviours = new AbstractInteractiveBehaviour[0];
            }
        }

        private void Start()
        {
            enabled = startEnabled;
        }

        private void OnEnable()
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = true;
                }
            }
            m_onEnable.Invoke();
            m_onToggle.Invoke(true);
        }

        public void Toggle()
        {
            enabled = !enabled;
        }

        private void AssignChildren()
        {
            var current = new List<AbstractInteractiveBehaviour>(behaviours);
            foreach (var child in GetComponentsInChildren<AbstractInteractiveBehaviour>(true))
            {
                if (!current.Contains(child))
                {
                    current.Add(child);
                }
            }
            behaviours = current.ToArray();
        }

        private void OnDisable()
        {
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = false;
                }
            }
            m_onDisable.Invoke();
            m_onToggle.Invoke(false);
        }
    }
}
