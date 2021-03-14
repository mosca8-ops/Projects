using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public class PlayAudioBlock : ComponentAnimation<Transform>, IAsyncAnimationBlock
    {
        [SerializeField]
        private AudioClip m_audio;

        private AudioSource m_source;

        public bool IsAsync => true;

        public override void OnValidate()
        {
            base.OnValidate();
            if (m_audio)
            {
                m_duration = m_audio.length;
            }
        }

        public override bool CanProvide<T>()
        {
            return false;
        }

        public override T Provide<T>()
        {
            return default;
        }

        public override bool CanPreview() => false;

        public override void OnStart()
        {
            base.OnStart();
            CreateAndPlay();
        }

        private void CreateAndPlay()
        {
            m_source = AudioSourcePool.Current.Get();
            if (m_target)
            {
                m_source.transform.SetParent(m_target, false);
                m_source.transform.localPosition = Vector3.zero;
            }
            m_source.clip = m_audio;
            m_source.Play();
        }

        public override void OnEnd(float normalizedDelta)
        {
            base.OnEnd(normalizedDelta);
            if (m_source)
            {
                m_source.Stop();
                AudioSourcePool.Current.Reclaim(m_source);
                m_source = null;
            }
        }

        protected override void Animate(float value, float normalizedValue)
        {
            if (!m_source)
            {
                CreateAndPlay();
            }
        }
    }
}