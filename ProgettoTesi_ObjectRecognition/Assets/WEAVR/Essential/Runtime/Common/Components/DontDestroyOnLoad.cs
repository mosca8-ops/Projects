using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Components/Don't Destroy On Load")]
    public class DontDestroyOnLoad : MonoBehaviour
    {
        #region [  STATIC PART  ]

        private static Dictionary<int, DontDestroyOnLoad> s_instances;
        private static Dictionary<int, DontDestroyOnLoad> Instances
        {
            get
            {
                if(s_instances == null)
                {
                    s_instances = new Dictionary<int, DontDestroyOnLoad>();
                }
                return s_instances;
            }
        }

        #endregion

        [SerializeField]
        [Button(nameof(ChangeId), label: "Change")]
        [ShowAsReadOnly]
        private int m_identifier;

        private void Reset()
        {
            ChangeId();
        }

        void Awake()
        {
            if(m_identifier != 0 && Instances.TryGetValue(m_identifier, out DontDestroyOnLoad other) && other != this)
            {
                Destroy(gameObject);
                return;
            }
            Instances[m_identifier] = this;
            DontDestroyOnLoad(this);
        }

        private void ChangeId()
        {
            m_identifier = Random.Range(int.MinValue, int.MaxValue);
        }

        private void OnDestroy()
        {
            if (Instances.TryGetValue(m_identifier, out DontDestroyOnLoad other) && other == this)
            {
                Instances.Remove(m_identifier);
            }
        }
    }
}