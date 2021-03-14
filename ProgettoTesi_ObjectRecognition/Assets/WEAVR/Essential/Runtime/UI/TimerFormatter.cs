using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/UI/Timer Formatter")]
    public class TimerFormatter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip(@"Use this to format the texts. 
  -'HH' for 24h hours format
  -'hh' for 12h hours format
  -'mm' for minutes
  -'ss' for seconds
  -'uu' for milliseconds")]
        private string m_format = "mm:ss";

        [SerializeField]
        private Text[] m_timerTexts;

        [Space]
        public UnityEventString onTimeChanged;
        public UnityEventInt onHourChanged;
        public UnityEventInt onMinuteChanged;
        public UnityEventInt onSecondChanged;
        public UnityEventInt onMillisecondChanged;

        private string m_actualFormat;

        private TimeSpan m_time;
        public TimeSpan Time
        {
            get => m_time;
            set
            {
                if(m_time != value)
                {
                    m_time = value;
                    Hours = m_time.Hours;
                    Minutes = m_time.Minutes;
                    Seconds = m_time.Seconds;
                    Milliseconds = m_time.Milliseconds;
                    UpdateTimerTexts();
                }
            }
        }


        public string FormatString
        {
            get => m_actualFormat;
            set
            {
                if(m_actualFormat != value)
                {
                    m_actualFormat = FixFormatString(value);
                    if (Application.isPlaying)
                    {
                        UpdateTimerTexts();
                    }
                }
            }
        }

        private float m_currentTime;
        public float CurrentTimeInSeconds
        {
            get => m_currentTime;
            set
            {
                if(m_currentTime != value)
                {
                    m_currentTime = value;
                    DispatchTime();
                }
            }
        }

        private int m_hours;
        public int Hours
        {
            get => m_hours;
            set
            {
                if (m_hours != value)
                {
                    m_hours = Mathf.Clamp(value, 0, 23);
                    onHourChanged?.Invoke(m_hours);
                    ComposeTime(value, Minutes, Seconds, Milliseconds);
                }
            }
        }

        private int m_minutes;
        public int Minutes
        {
            get => m_minutes;
            set
            {
                if (m_minutes != value)
                {
                    m_minutes = Mathf.Clamp(value, 0, 59);
                    onMinuteChanged?.Invoke(m_minutes);
                    ComposeTime(Hours, value, Seconds, Milliseconds);
                }
            }
        }

        private int m_seconds;
        public int Seconds
        {
            get => m_seconds;
            set
            {
                if (m_seconds != value)
                {
                    m_seconds = Mathf.Clamp(value, 0, 59);
                    onSecondChanged?.Invoke(m_seconds);
                    ComposeTime(Hours, Minutes, value, Milliseconds);
                }
            }
        }

        private int m_milliseconds;
        public int Milliseconds
        {
            get => m_milliseconds;
            set
            {
                if (m_milliseconds != value)
                {
                    m_milliseconds = Mathf.Clamp(value, 0, 999);
                    onMillisecondChanged?.Invoke(m_milliseconds);
                    ComposeTime(Hours, Minutes, Seconds, value);
                }
            }
        }

        private bool m_isInternalChange;
        private void ComposeTime(int hours, int minutes, int seconds, int milliseconds)
        {
            if (m_isInternalChange) { return; }
            Time = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        }

        private void OnValidate()
        {
            FormatString = m_format;
        }

        private void OnEnable()
        {
            m_actualFormat = FixFormatString(m_format);
            var timer = GetComponent<GenericTimer>();
            if (timer)
            {
                timer.OnTick -= Timer_OnTick;
                timer.OnTick += Timer_OnTick;
            }
        }

        private void OnDisable()
        {
            var timer = GetComponent<GenericTimer>();
            if (timer)
            {
                timer.OnTick -= Timer_OnTick;
            }
        }

        private void Timer_OnTick(float curentTime)
        {
            CurrentTimeInSeconds = curentTime;
        }

        private void UpdateTimerTexts()
        {
            string timeString = m_time.ToString(m_actualFormat);
            for (int i = 0; i < m_timerTexts.Length; i++)
            {
                if (m_timerTexts[i]) { m_timerTexts[i].text = timeString; }
            }
            onTimeChanged?.Invoke(timeString);
        }

        private string FixFormatString(string format) => format.Replace(":", @"\:");

        private void DispatchTime()
        {
            m_isInternalChange = true;
            Time = TimeSpan.FromSeconds(CurrentTimeInSeconds);
            m_isInternalChange = false;
        }
    }
}
