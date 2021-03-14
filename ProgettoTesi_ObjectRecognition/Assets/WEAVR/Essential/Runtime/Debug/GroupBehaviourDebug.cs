using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Debugging
{

    [AddComponentMenu("WEAVR/Debug/Group Behaviour Debug")]
    public class GroupBehaviourDebug : MonoBehaviour
    {
        [Header("Components")]
        [Draggable]
        public BehaviourDebug moduleSample;
        [Draggable]
        public DebugLine lineSample;
        [Draggable]
        public GameObject visualPanel;
        [Draggable]
        public Button toggleButton;

        [Header("Options")]
        [SerializeField]
        [InvokeOnChange(nameof(UpdateVisibility))]
        private bool m_isVisible = true;
        [Range(1, 20)]
        [Tooltip("Update info each x Frames")]
        public int updateRate = 3;

        [Header("Behaviours")]
        [Tooltip("Container where to store all behaviours watchers")]
        [Draggable]
        [InvokeOnChange(nameof(UpdateContainer))]
        public GameObject container;

        [Space]
        [SerializeField]
        [HideInInspector]
        private List<BehaviourDebug> m_behavioursToDebug;

        public IReadOnlyList<BehaviourDebug> BehavioursToDebug => m_behavioursToDebug;

        private void Reset() {
            if(container == null) {
                var behaviourDebug = GetComponentInChildren<BehaviourDebug>(true);
                if(behaviourDebug != null) {
                    container = behaviourDebug.transform.parent.gameObject;
                    UpdateContainer();
                }
            }
        }

        public void AddBehaviour(MonoBehaviour behaviour)
        {
            foreach(var behaviourDebugger in m_behavioursToDebug)
            {
                if(behaviourDebugger.behaviour == null)
                {
                    behaviourDebugger.ChangeBehaviour(behaviour);
                    return;
                }
                else if(behaviourDebugger.behaviour == behaviour)
                {
                    return;
                }
            }

            var newModule = Instantiate(moduleSample.gameObject) as GameObject;
            newModule.transform.SetParent(transform, false);
        }

        public void AddBehaviour()
        {
            var newModule = Instantiate(moduleSample.gameObject) as GameObject;
            newModule.transform.SetParent(container != null ? container.transform : transform, false);
            newModule.name = "ModulePanel";
            newModule.SetActive(true);
            m_behavioursToDebug.Add(newModule.GetComponent<BehaviourDebug>());
        }

        public void RemoveBehaviourAt(int index)
        {
            if(index < 0 || index >= m_behavioursToDebug.Count)
            {
                return;
            }
            var behaviour = m_behavioursToDebug[index];
            m_behavioursToDebug.RemoveAt(index);
            DestroyImmediate(behaviour.gameObject);
        }

        public void ToggleVisibility()
        {
            m_isVisible = !m_isVisible;
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (m_isVisible)
            {
                if(toggleButton != null)
                {
                    toggleButton.GetComponentInChildren<Text>().text = "Hide Debug";
                }
                if (visualPanel != null)
                {
                    visualPanel.SetActive(true);
                }
            }
            else
            {
                if (toggleButton != null)
                {
                    toggleButton.GetComponentInChildren<Text>().text = "Show Debug";
                }
                if (visualPanel != null)
                {
                    visualPanel.SetActive(false);
                }
            }
        }

        private void Start()
        {
            UpdateDebuggers();
        }

        private void UpdateContainer() {
            if (container != null) {
                if (moduleSample == null) {
                    moduleSample = container.GetComponentInChildren<BehaviourDebug>(true);
                }
                if(lineSample == null) {
                    lineSample = container.GetComponentInChildren<DebugLine>(true);
                }
            }
            UpdateDebuggers();
        }

        public void UpdateDebuggers()
        {
            if(m_behavioursToDebug == null) {
                m_behavioursToDebug = new List<BehaviourDebug>();
            }
            m_behavioursToDebug.Clear();
            m_behavioursToDebug.AddRange((container != null ? container.GetComponentsInChildren<BehaviourDebug>() : GetComponentsInChildren<BehaviourDebug>()));
        }

        void Update()
        {
            if (Time.frameCount % updateRate == 0)
            {
                foreach (var behaviour in m_behavioursToDebug)
                {
                    if (behaviour.gameObject.activeInHierarchy && behaviour.IsActive)
                    {
                        behaviour.UpdateInfo(updateRate);
                    }
                }
            }
        }
    }
}
