using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Utilities/Generic Timer")]
    public class GenericTimer : MonoBehaviour
    {
        private enum Mode
        {
            Timer,
            Stopwatch
        }

        [SerializeField]
        private bool m_autoStart = true;
        [SerializeField]
        private Mode m_mode = Mode.Timer;

        [SerializeField]
        [Draggable]
        private TextMesh m_textMesh; 

        [SerializeField]
        private float m_startValue = 0;
        [SerializeField]
        [Button(nameof(Swap))]
        private OptionalFloat m_endValue;

        [SerializeField]
        [ShowAsReadOnly]
        private bool m_paused;

        [Header("Events")]
        [SerializeField]
        private TimerEvents m_events;

        [Space]
        [SerializeField]
        [ShowOnEnum(nameof(m_mode), (int)Mode.Stopwatch)]
        private StopwatchEvents m_stopwatchEvents;

        public event Action<float> OnTick;

        private float m_currentTime;
        private float m_direction;

        public float CurrentTime
        {
            get => m_currentTime;
            set
            {
                if(m_currentTime != value)
                {
                    ApplyTimes(m_currentTime, value, false);
                }
            }
        }

        public float CurrentTimeWithEvents
        {
            get => m_currentTime;
            set
            {
                if (m_currentTime != value)
                {
                    ApplyTimes(m_currentTime, value, true);
                }
            }
        }

        public bool Paused
        {
            get => m_paused;
            set
            {
                if(m_paused != value)
                {
                    m_paused = value;
                }
            }
        }

        public bool StartTimer
        {
            get => m_paused;
            set => Paused = !value;
        }

        public bool ResetTimer { get => false; set => ResetTimerFromStart(); }
        public float AddTime { get => m_currentTime; set => CurrentTimeWithEvents += value; }
        public bool IsEndTimeReached => enabled && m_endValue.enabled && (m_direction < 0 ? m_currentTime <= m_endValue.value : m_currentTime >= m_endValue.value);

        public float ChronoTime { get => m_currentTime - m_startValue; set => CurrentTimeWithEvents = m_startValue + value * m_direction; }

        private void Swap()
        {
            if (m_endValue.enabled)
            {
                float temp = m_startValue;
                m_startValue = m_endValue.value;
                m_endValue.value = temp;
            }
        }

        private void OnValidate()
        {
            if(m_endValue == null) { return; }

            m_direction = m_endValue.enabled && m_startValue > m_endValue.value ? -1 : 1;
            if(m_endValue.enabled && m_endValue.value == m_startValue)
            {
                m_endValue.value = m_startValue + m_direction;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            m_direction = m_endValue.enabled && m_startValue > m_endValue.value ? -1 : 1;
            m_currentTime = m_startValue;
            m_events.onTimeChanged.Invoke(m_currentTime);
            m_paused = !m_autoStart;
        }

        // Update is called once per frame
        void Update()
        {
            if (!m_paused)
            {
                CurrentTimeWithEvents += Time.deltaTime * m_direction;
            }
        }

        private void ApplyTimes(float prevTime, float newTime, bool raiseEvents)
        {
            var endWasAlreadyReached = IsEndTimeReached;
            m_currentTime = newTime;
            var endReached = IsEndTimeReached;
            if (!endWasAlreadyReached)
            {
                if (endReached)
                {
                    m_currentTime = m_endValue.value;
                }
                if (raiseEvents)
                {
                    if (m_mode == Mode.Stopwatch)
                    {
                        m_stopwatchEvents.EvaluateEvents(prevTime, m_currentTime, m_direction < 0);
                    }

                    OnTick?.Invoke(m_currentTime);
                    m_events.onTimeChanged.Invoke(m_currentTime);
                    if(m_textMesh != null)
                    {
                        m_textMesh.text = Mathf.RoundToInt(m_currentTime).ToString();
                    }
                    if (endReached)
                    {
                        m_events.onEndValueReached.Invoke(m_currentTime);
                    }
                }
            }
            else if (endReached)
            {
                m_currentTime = m_endValue.value;
            }
        }

        public void ResetTimerFromStart()
        {
            CurrentTimeWithEvents = m_startValue;
            m_events.onReset.Invoke(m_startValue);
        }

        public void AddExtraTime(float time)
        {
            CurrentTimeWithEvents += time;
        }

        [Serializable]
        private struct TimerEvents
        {
            public UnityEventFloat onTimeChanged;
            public UnityEventFloat onEndValueReached;
            public UnityEventFloat onReset;
        }

        [Serializable]
        private struct StopwatchEvents
        {
            public StopwatchEvent[] events;

            public void EvaluateEvents(float prevTime, float newTime, bool goDown)
            {
                float min = prevTime;
                float max = newTime;
                if (goDown)
                {
                    min = newTime;
                    max = prevTime;
                }
                for (int i = 0; i < events.Length; i++)
                {
                    if(min <= events[i].time && events[i].time <= max)
                    {
                        events[i].Invoke();
                    }
                }
            }
        }

        [Serializable]
        private struct StopwatchEvent
        {
            public float time;
            public UnityEventFloat m_event;

            public void Invoke() => m_event.Invoke(time);
        }
    }
}
