using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("")]
    public class AudioSourcePool : MonoBehaviour, IWeavrSingleton
    {
        public static AudioSourcePool Current => Weavr.GetInCurrentScene<AudioSourcePool>();

        [Draggable]
        public AudioSource sample;

        private List<AudioSource> m_activeClips = new List<AudioSource>();
        private List<AudioSource> m_inactiveClips = new List<AudioSource>();

        private void Awake()
        {
            if(m_activeClips == null)
            {
                m_activeClips = new List<AudioSource>();
            }
            if(m_inactiveClips == null)
            {
                m_inactiveClips = new List<AudioSource>();
            }
        }

        public AudioSource Get()
        {
            AudioSource source = null;
            if (m_inactiveClips.Count > 0)
            {
                source = m_inactiveClips[0];
                m_inactiveClips.RemoveAt(0);
            }
            else if (sample)
            {
                source = Instantiate(sample);
                source.gameObject.hideFlags = HideFlags.HideAndDontSave;
                source.gameObject.name = $"AudioSource_{m_inactiveClips.Count + m_activeClips.Count}";
                source.transform.SetParent(transform, false);
            }
            else
            {
                source = new GameObject($"AudioSource_{m_inactiveClips.Count + m_activeClips.Count}").AddComponent<AudioSource>();
                source.gameObject.hideFlags = HideFlags.HideAndDontSave;
                source.transform.SetParent(transform, false);
            }
            m_activeClips.Add(source);
            return source;
        }

        public void Reclaim(AudioSource source)
        {
            if (m_activeClips.Remove(source))
            {
                source.clip = null;
                source.volume = sample ? sample.volume : 1;
                source.loop = false;
                source.spatialBlend = sample ? sample.spatialBlend : 0;
                source.transform.position = Vector3.zero;
                source.transform.SetParent(transform, false);
                m_inactiveClips.Add(source);
            }
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying)
            {
                foreach(var clip in m_activeClips)
                {
                    DestroyImmediate(clip);
                }
                foreach (var clip in m_inactiveClips)
                {
                    DestroyImmediate(clip);
                }
            }
            else
            {
                foreach (var clip in m_activeClips)
                {
                    Destroy(clip);
                }
                foreach (var clip in m_inactiveClips)
                {
                    Destroy(clip);
                }
            }
        }
    }
}
