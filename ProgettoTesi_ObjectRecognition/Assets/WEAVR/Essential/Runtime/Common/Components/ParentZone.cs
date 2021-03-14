using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Parent Zone")]
    public class ParentZone : MonoBehaviour
    {
        private Dictionary<Transform, Transform> m_parents;
        [SerializeField]
        [Draggable]
        private Transform m_container;
        [SerializeField]
        [Draggable]
        private Collider m_collider;
        [SerializeField]
        private bool m_onlyRigidBodies = false;
        [Space]
        [SerializeField]
        private bool m_isLocked = false;
        [SerializeField]
        private bool m_markChildrenKinematic = true;

        private Dictionary<Transform, List<Collider>> m_triggers;
        private HashSet<Transform> m_nonKinematicRB;
        private HashSet<Transform> m_objectsInside;

        public bool IsLocked
        {
            get { return m_isLocked; }
            set
            {
                if (m_isLocked != value)
                {
                    m_isLocked = value;
                    if (value)
                    {
                        if (m_markChildrenKinematic)
                        {
                            List<Collider> triggers = null;
                            foreach (var pair in m_parents)
                            {
                                var rb = pair.Key.GetComponent<Rigidbody>();
                                if (rb != null && !rb.isKinematic)
                                {
                                    rb.isKinematic = true;
                                    m_nonKinematicRB.Add(pair.Key);
                                }
                                triggers = new List<Collider>();
                                foreach (var collider in pair.Key.GetComponentsInChildren<Collider>())
                                {
                                    if (collider.enabled)
                                    {
                                        collider.enabled = false;
                                        triggers.Add(collider);
                                    }
                                }
                                if (triggers.Count > 0)
                                {
                                    m_triggers[pair.Key] = triggers;
                                }

                            }
                        }
                    }
                    else
                    {
                        foreach (var pair in m_triggers)
                        {
                            if (m_nonKinematicRB.Contains(pair.Key))
                            {
                                var rb = pair.Key.GetComponent<Rigidbody>();
                                if (rb != null)
                                {
                                    rb.isKinematic = false;
                                }
                            }
                            foreach (var collider in pair.Value)
                            {
                                collider.enabled = true;
                            }
                        }
                        m_triggers.Clear();
                        m_nonKinematicRB.Clear();
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (m_collider == null)
            {
                m_collider = GetComponent<Collider>();
            }
            if (!m_container)
            {
                m_container = transform;
            }
        }

        public void SetLock(bool isLock)
        {
            IsLocked = isLock;
        }

        // Use this for initialization
        void Start()
        {
            if (!m_container)
            {
                m_container = transform;
            }
            m_objectsInside = new HashSet<Transform>();
            m_parents = new Dictionary<Transform, Transform>();
            m_triggers = new Dictionary<Transform, List<Collider>>();
            m_nonKinematicRB = new HashSet<Transform>();
            if (m_collider != null)
            {
                m_collider.isTrigger = true;
                if (!m_isLocked)
                {
                    foreach (var col in Physics.OverlapBox(m_collider.bounds.center, m_collider.bounds.extents))
                    {
                        ChangeParent(col);
                    }
                }
            }
            for (int i = 0; i < m_container.childCount; i++)
            {
                var child = m_container.GetChild(i);
                m_objectsInside.Add(m_container.GetChild(i));
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_isLocked) { return; }
            ChangeParent(other);
        }

        private void ChangeParent(Collider other)
        {
            if (!CanFit(other.bounds, m_collider.bounds))
            {
                return;
            }
            if (other.transform.parent)
            {
                var parent = other.transform.parent;
                while (parent)
                {
                    if (parent == m_container || m_parents.ContainsKey(parent))
                    {
                        return;
                    }
                    parent = parent.parent;
                }
            }
            if (m_onlyRigidBodies)
            {
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    m_parents[other.transform] = other.transform.parent;
                    other.transform.SetParent(m_container, true);

                    m_objectsInside.Add(other.transform);
                }
            }
            else
            {
                m_parents[other.transform] = other.transform.parent;
                other.transform.SetParent(m_container, true);

                m_objectsInside.Add(other.transform);
            }
        }

        private bool CanFit(Bounds a, Bounds target)
        {
            var exA = a.extents;
            var exT = target.extents;
            return exA.x < exT.x && exA.y < exT.y && exA.z < exT.z;
        }

        private void OnTriggerExit(Collider other)
        {
            Transform parent = null;
            m_objectsInside.Remove(other.transform);
            if (other.transform.IsChildOf(m_container))
            {
                if (m_parents.TryGetValue(other.transform, out parent))
                {
                    other.transform.SetParent(/*parent == transform ? null :*/ parent, true);
                    m_parents.Remove(other.transform);
                }
                else if (other.transform.parent == m_container)
                {
                    other.transform.SetParent(null, true);
                }
            }
            List<Collider> triggers = null;
            if (m_nonKinematicRB.Contains(other.transform))
            {
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }
            }
            if (m_triggers.TryGetValue(other.transform, out triggers))
            {
                foreach (var trigger in triggers)
                {
                    trigger.enabled = true;
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < m_container.childCount; i++)
            {
                var child = m_container.GetChild(i);
                if (!m_objectsInside.Contains(child))
                {
                    if (m_parents.TryGetValue(child, out Transform parent) && parent != m_container)
                    {
                        child.SetParent(parent, true);
                    }
                    else
                    {
                        child.SetParent(null, true);
                    }
                }
            }
        }
    }
}
