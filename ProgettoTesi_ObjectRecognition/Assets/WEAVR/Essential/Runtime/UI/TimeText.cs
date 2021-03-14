namespace TXT.WEAVR.UI
{
    using System;
    using TXT.WEAVR.Common;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Text))]
    [AddComponentMenu("WEAVR/UI/Time Text")]
    public class TimeText : MonoBehaviour
    {

        [SerializeField]
        [Draggable]
        private Text m_textComponent;
        [SerializeField]
        private string m_timeFormat = "mm:ss";
        [SerializeField]
        private string m_fallbackText = "--:--";

        [Space]
        [SerializeField]
        private bool m_hasTargetTime = false;
        [SerializeField]
        [HiddenBy(nameof(m_hasTargetTime))]
        private float m_targetTime;
        [SerializeField]
        [HiddenBy(nameof(m_hasTargetTime))]
        private Gradient m_targetGradient;

        [Header("Debug")]
        [SerializeField]
        [ShowAsReadOnly]
        private float m_timeFloat;
        [SerializeField]
        [ShowAsReadOnly]
        private string m_timeText;

        private bool m_ready;
        private float? m_timespan;

        public string TimeFormat {
            get {
                if (!m_ready)
                {
                    MakeReady();
                }
                return m_timeFormat;
            }
            set {
                if (m_timeFormat != value)
                {
                    m_timeFormat = string.IsNullOrWhiteSpace(value) ? "mm:ss" : value;
                    m_timeFormat = m_timeFormat.Replace(":", @"\:");
                    // Check if correct format
                }
            }
        }

        public float Time {
            get { return m_timeFloat; }
            set {
                if (m_timeFloat != value)
                {
                    if (value <= 0)
                    {
                        m_timeFloat = 0;
                        m_timeText = m_fallbackText;
                        m_textComponent.text = m_timeText;
                    }
                    else
                    {
                        m_timeFloat = value;
                        m_timeText = TimeSpan.FromSeconds(value).ToString(TimeFormat);
                        m_textComponent.text = m_timeText;
                    }
                    UpdateTextColor();
                }
            }
        }

        public bool HasTargetTime {
            get { return m_hasTargetTime; }
            set {
                if (m_hasTargetTime != value)
                {
                    m_hasTargetTime = value;

                }
            }
        }

        public float TargetTime {
            get { return m_targetTime; }
            set {
                if (m_targetTime != value)
                {
                    m_targetTime = value;
                    m_timespan = null;
                    UpdateTextColor();
                }
            }
        }

        private void UpdateTextColor()
        {
            if (m_hasTargetTime)
            {
                if (!m_timespan.HasValue)
                {
                    m_timespan = Mathf.Abs(m_timeFloat - m_targetTime);
                }
                m_textComponent.color = m_targetGradient.Evaluate(1f - Mathf.Clamp01(Mathf.Abs(m_targetTime - m_timeFloat) / m_timespan.Value));
            }
            //else
            //{
            //    m_textComponent.color = m_targetGradient.Evaluate(0);
            //}
        }

        private void OnValidate()
        {
            if (m_textComponent == null)
            {
                m_textComponent = GetComponent<Text>();
            }
            m_timeFormat = string.IsNullOrWhiteSpace(m_timeFormat) ? "mm:ss" : m_timeFormat;
        }

        // Use this for initialization
        void Awake()
        {
            OnValidate();
            MakeReady();
        }

        private void OnEnable()
        {
            MakeReady();
        }

        private void MakeReady()
        {
            if (!m_ready)
            {
                m_timeFormat = string.IsNullOrWhiteSpace(m_timeFormat) ? "mm:ss" : m_timeFormat;
                m_timeFormat = m_timeFormat.Replace(":", @"\:");

                m_ready = true;
            }
        }
    }
}