using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [AddComponentMenu("WEAVR/Procedures/Procedures Timer")]
    public class ProcedureTimer : MonoBehaviour
    {

        private float m_scheduledTime;
        private float m_timer;

        public float Timer {
            get { return m_timer; }
            set {
                if (value > 0)
                {
                    m_timer = value;
                    m_scheduledTime = Time.time + value;
                }
            }
        }

        public bool Elapsed => m_scheduledTime <= Time.time;

        // Use this for initialization
        void Start()
        {
            m_scheduledTime = float.MaxValue;
        }
    }
}