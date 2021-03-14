using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Utilities/Random Activator")]
    public class RandomActivator : AbstractGameObjectActivator
    {
        [Button(nameof(ActivateRandom), "Randomize")]
        public bool startRandomized = false;
        public bool exclusiveActivation;

        [SerializeField]
        private int m_cycleFrom = 0;
        [SerializeField]
        [Button(nameof(ResetEndRange), "Reset")]
        private int m_cycleTo = 0;

        [SerializeField]
        private GameObject[] m_objects;

        [SerializeField]
        [HideInInspector]
        private int m_prevLength;

        private void Reset()
        {
            m_objects = GetComponentsInChildren<Renderer>(true).Where(r => r.transform.parent == transform).Select(r => r.gameObject).ToArray();
        }

        private void ResetEndRange()
        {
            m_cycleTo = m_objects.Length - 1;
        }

        private int m_currentlyActiveIndex;
        public int CurrentlyActiveIndex
        {
            get => m_currentlyActiveIndex;
            set
            {
                if (m_currentlyActiveIndex != value)
                {
                    m_currentlyActiveIndex = value;
                    if (0 <= m_currentlyActiveIndex && m_currentlyActiveIndex < m_objects.Length)
                    {
                        Activate(m_objects[m_currentlyActiveIndex]);
                    }
                }
            }
        }

        public override int OnlyActiveIndex 
        { 
            get => CurrentlyActiveIndex;
            set
            {
                bool wasExclusive = exclusiveActivation;
                exclusiveActivation = true;
                CurrentlyActiveIndex = value;
                exclusiveActivation = wasExclusive;
            }
        }

        public GameObject CurrentlyActive {
            get => 0 <= m_currentlyActiveIndex && m_currentlyActiveIndex < m_objects.Length ? m_objects[m_currentlyActiveIndex] : null;
            set
            {
                if (value)
                {
                    for (int i = 0; i < m_objects.Length; i++)
                    {
                        if (m_objects[i] == value)
                        {
                            m_currentlyActiveIndex = i;
                            Activate(value);
                        }
                    }
                }
                else if(m_currentlyActiveIndex >= 0)
                {
                    m_currentlyActiveIndex = -1;
                    for (int i = 0; i < m_objects.Length; i++)
                    {
                        m_objects[i].SetActive(false);
                    }
                }
            }
        }

        public override GameObject OnlyActive { 
            get => CurrentlyActive;
            set
            {
                bool wasExclusive = exclusiveActivation;
                exclusiveActivation = true;
                CurrentlyActive = value;
                m_currentlyActiveIndex = base.OnlyActiveIndex;
                exclusiveActivation = wasExclusive;
            }
        }

        public override GameObject[] GameObjects => m_objects;

        // Start is called before the first frame update
        void Start()
        {
            if (startRandomized)
            {
                m_currentlyActiveIndex = -1;
                ActivateRandom();
            }

            DefaultStartIndex = m_cycleFrom;
            DefaultEndIndex = m_cycleTo;
        }

        private void OnValidate()
        {
            if(m_cycleTo == 0)
            {
                m_cycleTo = m_objects.Length - 1;
            }
            if (m_prevLength != m_objects.Length)
            {
                if (m_cycleTo == m_prevLength - 1 || m_cycleTo <= 0)
                {
                    m_cycleTo = m_objects.Length - 1;
                }
                m_prevLength = m_objects.Length;
            }
            else
            {
                m_cycleTo = Mathf.Clamp(m_cycleTo, m_cycleFrom, m_objects.Length - 1);
            }
            m_cycleFrom = Mathf.Clamp(m_cycleFrom, 0, m_cycleTo);
        }

        public void ActivateRandom()
        {
            CurrentlyActiveIndex = Random.Range(0, m_objects.Length);
        }

        public void Activate(GameObject go)
        {
            if (exclusiveActivation)
            {
                for (int i = 0; i < m_objects.Length; i++)
                {
                    if (go == m_objects[i])
                    {
                        m_currentlyActiveIndex = i;
                        m_objects[i].SetActive(true);
                    }
                    else
                    {
                        m_objects[i].SetActive(false);
                    }
                }
            }
            else
            {
                go.SetActive(true);
            }
        }
    }
}
