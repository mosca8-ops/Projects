using System;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Utility
{
    [AddComponentMenu("WEAVR/Utilities/Scene Time")]
    public class SceneTime : MonoBehaviour
    {
        public enum TimeType
        {
            Component,
            Scene,
            Application,
            Current,
        }

        public TimeType timeType;
        public UnityEventFloat onTick;

        private float m_enableTime;

        private void Start()
        {
            m_enableTime = Time.time;
        }

        private void Update()
        {
            switch (timeType)
            {
                case TimeType.Component:
                    onTick.Invoke(Time.time - m_enableTime);
                    break;
                case TimeType.Scene:
                    onTick.Invoke(Time.timeSinceLevelLoad);
                    break;
                case TimeType.Application:
                    onTick.Invoke(Time.time);
                    break;
                case TimeType.Current:
                    onTick.Invoke((float)DateTime.Now.TimeOfDay.TotalSeconds);
                    break;
            }
        }

        public void ResetComponentTime()
        {
            m_enableTime = Time.time;
        }
    }
}