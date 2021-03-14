using UnityEngine;

namespace TXT.WEAVR.Common
{

    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("WEAVR/Audio/Startup Audio Source")]
    public class StartupAudioSource : MonoBehaviour
    {

        public float ignoreSeconds = 3;
        [Draggable]
        public AudioSource audioSource;

        private float m_ingoreTime;

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        // Use this for initialization
        void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            m_ingoreTime = Time.time + ignoreSeconds;
        }

        public void Play()
        {
            if (Time.time > m_ingoreTime)
            {
                audioSource.Play();
            }
        }
    }
}