using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Components/Delayed Events")]
    public class DelayedEventRaiser : MonoBehaviour
    {

        public UnityEvent OnAwake;

        [SerializeField]
        protected DelayedUnityEvent[] m_calls;

        public void StopOthersAndRaise(int index)
        {
            if (index < 0 || index >= m_calls.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Stop(index);
            Raise(index);
        }

        public void Raise(int index)
        {
            if (index < 0 || index >= m_calls.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            m_calls[index].coroutine = RaiseCoroutine(m_calls[index]);
            StartCoroutine(m_calls[index].coroutine);
        }

        public void StopAndRaise(int index)
        {
            if (index < 0 || index >= m_calls.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Stop(index);
            m_calls[index].coroutine = RaiseCoroutine(m_calls[index]);
            StartCoroutine(m_calls[index].coroutine);
        }

        public void Stop(int index)
        {
            if (index < 0 || index >= m_calls.Length)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            var coroutine = m_calls[index].coroutine;
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        private void Awake()
        {
            OnAwake.Invoke();
        }

        private void Start()
        {
            for (int i = 0; i < m_calls.Length; i++)
            {
                if (m_calls[i].raiseOnStart)
                {
                    Raise(i);
                }
            }
        }

        public void StopAll()
        {
            StopAllCoroutines();
        }

        private IEnumerator RaiseCoroutine(DelayedUnityEvent dEvent)
        {
            if (dEvent.realSeconds)
            {
                yield return new WaitForSecondsRealtime(dEvent.delay);
            }
            else
            {
                yield return new WaitForSeconds(dEvent.delay);
            }
            dEvent.callback.Invoke();
        }

        [Serializable]
        protected class DelayedUnityEvent
        {
            [Tooltip("Delay (in seconds) to make the call")]
            public float delay;
            [Tooltip("Whether to start delayed events on start or not")]
            public bool raiseOnStart;
            [Tooltip("Whether to use real seconds or gameplay seconds")]
            public bool realSeconds;
            public UnityEvent callback;
            public IEnumerator coroutine;
        }
    }
}