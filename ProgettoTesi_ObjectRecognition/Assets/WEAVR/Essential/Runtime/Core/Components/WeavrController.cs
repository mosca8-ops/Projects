using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    [ExecuteAlways]
    [AddComponentMenu("")]
    public sealed class WeavrController : MonoBehaviour
    {
        private static WeavrController s_instance;

        private static object m_lockObject = new object();
        private static HashSet<IExecuteDisabled> m_disabledObjects = new HashSet<IExecuteDisabled>();

        public static event Action StartEvent;
        public static event Action AwakeEvent;
        public static event Action UpdateEvent;

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeSystems()
        {
            if (!Application.isEditor)
            {
                // Initialize here the new System
            }
        }

        public static void UpdateDisabled()
        {
            if (m_disabledObjects.Count > 0)
            {
                lock (m_lockObject)
                {
                    foreach (var disabledObject in m_disabledObjects)
                    {
                        if (disabledObject is Behaviour b && b && (!b.enabled || !b.gameObject.activeInHierarchy))
                        {
                            Debug.LogError($"{b.gameObject.name}.InitDisabled()");
                            disabledObject.InitDisabled();
                        }
                    }
                    m_disabledObjects.Clear();
                }
            }
        }

        public static void RegisterForDisabledInit(IExecuteDisabled executeDisabled)
        {
            if (executeDisabled is Component)
            {
                lock (m_lockObject)
                {
                    m_disabledObjects.Add(executeDisabled);
                }
            }
        }

        public static void UnregisterForDisabledInit(IExecuteDisabled executeDisabled)
        {
            if (executeDisabled is Component)
            {
                lock (m_lockObject)
                {
                    m_disabledObjects.Remove(executeDisabled);
                }
            }
        }

        private void Awake()
        {
            if(s_instance && s_instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
                return;
            }

            s_instance = this;

            AwakeEvent?.Invoke();
        }

        void Start()
        {
            StartEvent?.Invoke();
        }

        void Update()
        {
            UpdateDisabled();
            UpdateEvent?.Invoke();
        }
    }
}
