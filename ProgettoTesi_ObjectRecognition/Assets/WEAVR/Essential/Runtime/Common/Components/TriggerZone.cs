using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{
    [Serializable]
    public class TriggerEnterEvent : UnityEvent<GameObject> { }

    [AddComponentMenu("WEAVR/Components/Trigger Zone")]
    public class TriggerZone : MonoBehaviour
    {
        public bool filterObjects = true;

        [SerializeField]
        [HiddenBy(nameof(filterObjects))]
        [Draggable]
        private List<Transform> m_whiteList;

        public TriggerEnterEvent TriggerEnter;
        public TriggerEnterEvent TriggerExit;

        public bool HasObjects { get; private set; }

        private void Reset()
        {
            if(GetComponentInChildren<Collider>() == null)
            {
                var collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!filterObjects || IsWhitelisted(other.transform))
            {
                HasObjects = true;
                TriggerEnter.Invoke(other.gameObject);
            }
        }

        private bool IsWhitelisted(Transform target)
        {
            for (int i = 0; i < m_whiteList.Count; i++)
            {
                if (target.IsChildOf(m_whiteList[i])){
                    return true;
                }
            }
            return false;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!filterObjects || IsWhitelisted(other.transform))
            {
                HasObjects = false;
                TriggerExit.Invoke(other.gameObject);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!filterObjects || IsWhitelisted(other.transform))
            {
                HasObjects = true;
                TriggerEnter.Invoke(other.gameObject);
            }
        }
    }
}
