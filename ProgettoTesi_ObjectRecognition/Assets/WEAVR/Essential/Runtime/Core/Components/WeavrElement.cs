using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR
{

    [AddComponentMenu("WEAVR/Setup/Indexed Element")]
    public class WeavrElement : MonoBehaviour
    {
        [SerializeField]
        private string m_key;
        [SerializeField]
        [HideInInspector]
        private string m_lowerKey;

        public string Key => m_key;

        private void Reset()
        {
            m_key = name;
            m_lowerKey = m_key?.ToLower();
        }

        private void OnValidate()
        {
            m_lowerKey = m_key?.ToLower();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(m_key)) {
                m_key = name;
                m_lowerKey = m_key?.ToLower();
            }
        }

        public static GameObject Find(GameObject root, string key)
        {
            key = key.ToLower();
            return root.GetComponentsInChildren<WeavrElement>(true).FirstOrDefault(e => e.m_lowerKey == key)?.gameObject;
        }
    }
}
