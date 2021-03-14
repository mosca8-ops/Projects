using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Groups/LOD Group")]
    public class MegaLODGroup : MonoBehaviour
    {

        [Button(nameof(GetFromChildren), "Reload")]
        [ShowAsReadOnly]
        public int m_lodGroupCount;
        [Draggable]
        public LODGroup[] m_groups;

        private void Reset()
        {
            m_groups = GetComponentsInChildren<LODGroup>();
        }

        private void OnValidate()
        {
            if (m_groups != null)
            {
                m_lodGroupCount = m_groups.Length;
            }
        }

        private void GetFromChildren()
        {
            m_groups = GetComponentsInChildren<LODGroup>();
            m_lodGroupCount = m_groups.Length;
        }

        public void SetVisibility(bool visible)
        {
            for (int i = 0; i < m_groups.Length; i++)
            {
                if (m_groups[i] != null)
                {
                    m_groups[i].enabled = visible;
                }
            }
        }
    }
}