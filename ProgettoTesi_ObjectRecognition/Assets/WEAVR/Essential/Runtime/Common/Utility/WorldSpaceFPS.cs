namespace TXT.WEAVR.Utility
{
    using UnityEngine;
    using UnityEngine.UI;

    [AddComponentMenu("WEAVR/Utilities/World Space FPS")]
    public class WorldSpaceFPS : MonoBehaviour
    {
        public Text textComponent;
        [Range(0.01f, 1f)]
        public float updateTime = 0.1f;

        private float m_periodFrameCount;
        private float m_periodStart;

        // Use this for initialization
        void Start()
        {
            m_periodFrameCount = 0;
            m_periodStart = Time.unscaledTime;
        }

        // Update is called once per frame
        void Update()
        {
            m_periodFrameCount++;
            if (Time.unscaledTime - m_periodStart > updateTime)
            {
                textComponent.text = (m_periodFrameCount / (Time.unscaledTime - m_periodStart)).ToString();
                m_periodStart = Time.unscaledTime;
                m_periodFrameCount = 0;
            }
        }
    }
}