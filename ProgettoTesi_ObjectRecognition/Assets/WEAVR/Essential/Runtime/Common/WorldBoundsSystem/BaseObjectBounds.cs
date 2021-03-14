using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    public abstract class BaseObjectBounds : MonoBehaviour
    {
        [SerializeField]
        [ShowAsReadOnly]
        protected GameObject m_boundingObject;
        [SerializeField]
        [ShowAsReadOnly]
        protected Collider[] m_colliders;

        protected WorldBounds m_worldBounds;

        protected bool m_isDestroying;

        public virtual GameObject BoundsCollider {
            get { return m_boundingObject; }
            set {
                if(value != null) {
                    m_boundingObject = value;
                }
            }
        }

        public virtual Collider[] Colliders {
            get { return m_colliders; }
            protected set { m_colliders = value; }
        }

        public virtual WorldBounds WorldBounds {
            get { return m_worldBounds; }
            set { m_worldBounds = value; }
        }

        protected virtual void OnEnable() {
            if(m_boundingObject != null) { m_boundingObject.SetActive(true); }
        }

        protected virtual void OnDisable() {
            if(m_boundingObject != null) { m_boundingObject.SetActive(false); }
        }

        public virtual void SyncTransforms() {
            m_boundingObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
            m_boundingObject.transform.localScale = transform.localScale;
        }

        public virtual void Remove() {
            if (m_boundingObject != null && m_boundingObject != gameObject) {
                DestroySmart(m_boundingObject);
                m_boundingObject = null;
            }
            if (!m_isDestroying) {
                m_isDestroying = true;
                DestroySmart(this);
            }
        }

        private void OnDestroy() {
            if (!m_isDestroying) {
                m_isDestroying = true;
                Remove();
            }
        }

        protected static void DestroySmart(Object obj) {
            if (Application.isPlaying) {
                Destroy(obj);
            }
            else {
                DestroyImmediate(obj);
            }
        }
    }
}
