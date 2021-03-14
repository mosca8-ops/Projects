using System;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [Obsolete("Use Impact System components which are more robust and more functional")]
    [AddComponentMenu("")]
    public class ImpactSound : MonoBehaviour
    {

        [SerializeField]
        private Impact[] m_impacts;

        private Rigidbody m_rigidBody;

        public void OnValidate()
        {

        }

        private void Start()
        {
            m_impacts = m_impacts.Where(i => i.sounds.Length > 0).OrderBy(i => i.impulse).ToArray();
            m_rigidBody = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Time.time < 4) { return; }
            float impulse = collision.impulse.magnitude;
            if (impulse < m_impacts[0].impulse || (m_rigidBody != null && m_rigidBody.velocity.sqrMagnitude < 0.0001f))
            {
                return;
            }
            for (int i = 1; i < m_impacts.Length; i++)
            {
                if (impulse < m_impacts[i].impulse)
                {
                    m_impacts[i - 1].Play(collision.contacts[0].point);
                    return;
                }
            }
            m_impacts[m_impacts.Length - 1].Play(collision.contacts[0].point);
        }

        [Serializable]
        private class Impact
        {
            public float impulse;
            [Range(0, 1)]
            public float volume = 1;
            public AudioClip[] sounds;

            public AudioClip Sound => sounds.Length == 1 ? sounds[0] : sounds[UnityEngine.Random.Range(0, sounds.Length)];

            public void Play(Vector3 point)
            {
                AudioSource.PlayClipAtPoint(Sound, point, volume);
            }
        }
    }
}