using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Audio/Appearence Sound")]
    public class AppearenceSound : MonoBehaviour
    {
        [Draggable]
        public AudioSource audioSource;

        [Space]
        [Draggable]
        public AudioClip onEnable;
        [Draggable]
        public AudioClip onDisable;

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponentInParent<AudioSource>();
            }
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            OnValidate();
        }

        private void OnEnable()
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(onEnable);
            }
        }

        private void OnDisable()
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(onDisable);
            }
        }
    }
}