using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("")]
    public class DoorHandle : MonoBehaviour
    {
        public bool isActive;
        [Draggable]
        public AbstractDoor door;

        protected Transform m_fixedShadowHandle;

        private void Reset()
        {
            door = GetComponentInParent<AbstractDoor>();
        }

        public void Handle()
        {
            isActive = true;
        }

        public void Release()
        {
            isActive = false;
        }

        public virtual Vector3 Position => transform.position;

        public virtual Quaternion Rotation => transform.rotation;

        protected virtual void Start()
        {
            m_fixedShadowHandle = new GameObject($"{name}_shadowHandle").transform;
            m_fixedShadowHandle.gameObject.hideFlags = HideFlags.HideAndDontSave;
            m_fixedShadowHandle.SetParent(transform.parent, false);
            m_fixedShadowHandle.SetPositionAndRotation(transform.position, transform.rotation);
        }

        /// <summary>
        /// Changes the position and rotation to keep the same pose relative to the door
        /// </summary>
        public virtual void Stabilize()
        {
            if (m_fixedShadowHandle != null)
            {
                transform.SetPositionAndRotation(m_fixedShadowHandle.position, m_fixedShadowHandle.rotation);
            }
        }
    }
}
