using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Groups/Group Activator")]
    public class GroupActivator : AbstractGameObjectActivator
    {
        public bool distinctOnly = true;
        [SerializeField]
        private int m_cycleFrom = 0;
        [SerializeField]
        [Button(nameof(ResetEndRange), "Reset")]
        private int m_cycleTo = 0;
        [Draggable]
        public GameObject[] objects;

        public override GameObject[] GameObjects => objects;

        [SerializeField]
        [HideInInspector]
        private int m_prevLength;

        private void Reset()
        {
            objects = GetComponentsInChildren<Transform>(true).Where(t => t != transform).Select(t => t.gameObject).ToArray();
        }

        private void ResetEndRange()
        {
            m_cycleTo = objects.Length - 1;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && distinctOnly)
            {
                objects = objects.Distinct().ToArray();
            }

            if (m_prevLength != objects.Length)
            {
                if (m_cycleTo == m_prevLength - 1 || m_cycleTo <= 0)
                {
                    m_cycleTo = objects.Length - 1;
                }
                m_prevLength = objects.Length;
            }
            else
            {
                m_cycleTo = Mathf.Clamp(m_cycleTo, m_cycleFrom, objects.Length - 1);
            }
            m_cycleFrom = Mathf.Clamp(m_cycleFrom, 0, m_cycleTo);
        }

        private void Start()
        {
            DefaultStartIndex = m_cycleFrom;
            DefaultEndIndex = m_cycleTo;
        }
    }
}
