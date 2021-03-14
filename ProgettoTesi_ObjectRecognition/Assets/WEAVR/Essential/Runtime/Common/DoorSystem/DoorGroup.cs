using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Groups/Door Group")]
    public class DoorGroup : MonoBehaviour
    {

        [Button(nameof(GetFromChildren), "Reload")]
        [ShowAsReadOnly]
        public int m_doorCount;
        [Draggable]
        public AbstractDoor[] m_doors;

        [Space]
        public UnityEvent onOpened;
        public UnityEvent onClosed;

        public UnityEvent onLocked;
        public UnityEvent onUnlocked;

        public UnityEvent onAppear;
        public UnityEvent onDisappear;

        public UnityEventBoolean onToggleVisibility;

        private void Reset()
        {
            m_doors = GetComponentsInChildren<AbstractDoor>();
        }

        private void OnValidate()
        {
            if (m_doors != null)
            {
                m_doorCount = m_doors.Length;
            }
        }

        private void GetFromChildren()
        {
            m_doors = GetComponentsInChildren<AbstractDoor>();
            m_doorCount = m_doors.Length;
        }

        public void Open()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].Open();
                }
            }

            onOpened.Invoke();
        }

        public void Close()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].Close();
                }
            }

            onClosed.Invoke();
        }

        public void Lock()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].Lock();
                }
            }
            onLocked.Invoke();
        }

        public void Unlock()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].Unlock();
                }
            }
            onUnlocked.Invoke();
        }

        public void Disappear()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].gameObject.SetActive(false);
                }
            }
            onToggleVisibility.Invoke(false);
            onDisappear.Invoke();
        }

        public void Appear()
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].gameObject.SetActive(true);
                }
            }

            onToggleVisibility.Invoke(true);
            onAppear.Invoke();
        }

        public void SetVisible(bool visible)
        {
            for (int i = 0; i < m_doors.Length; i++)
            {
                if (m_doors[i] != null)
                {
                    m_doors[i].gameObject.SetActive(visible);
                }
            }
            onToggleVisibility.Invoke(visible);
            if (visible)
            {
                onAppear.Invoke();
            }
            else
            {
                onDisappear.Invoke();
            }
        }
    }
}
