using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TXT.WEAVR.Procedure
{

    public class VideoModifier : BillboardModifier<VideoPlayer>
    {
        [SerializeField]
        [Tooltip("The video to play")]
        [Draggable]
        private VideoClip m_video;
        [SerializeField]
        [Tooltip("Whether to loop the video or not")]
        [ShowIf(nameof(CanLoop))]
        private OptionalBool m_loop;
        [SerializeField]
        [Tooltip("The time when to start the playback")]
        private OptionalDouble m_startAt;
        [SerializeField]
        [Tooltip("At which time mark to end playback")]
        private OptionalDouble m_endAt;

        public int AsyncThread => Action.AsyncThread;

        private VideoClip m_previousClip;
        private bool m_wasPlaying;
        private bool m_wasLooping;
        private double m_previousTime;
        private float m_duration;
        private bool m_willPlay;

        public override void Prepare(Billboard billboard)
        {
            base.Prepare(billboard);

            m_previousClip = m_target.clip;
            m_wasPlaying = m_target.isPlaying;
            m_wasLooping = m_target.isLooping;
            m_previousTime = m_target.time;

            m_target.clip = m_video;
            m_target.Stop();
            if (m_loop.enabled) { m_target.isLooping = m_loop && AsyncThread != 0; }
            if (m_startAt.enabled) { m_target.time = m_startAt; }

            m_duration = (float)(m_endAt.enabled ? m_endAt.value : m_video.length) - (float)(m_startAt.enabled ? m_startAt.value : 0);
        }

        public override void OnValidate()
        {
            base.OnValidate();
            if (m_video)
            {
                m_startAt.value = m_startAt.value >= m_video.length ? m_video.length - 0.01f : m_startAt.value < 0 ? 0 : m_startAt.value;
                m_endAt.value = m_endAt.value <= m_startAt.value ? m_startAt.value + 0.01f :
                                m_endAt.value >= m_video.length ? m_video.length - 0.01f : m_endAt.value;
            }
            m_loop.value &= AsyncThread != 0;
        }

        private bool CanLoop()
        {
            return AsyncThread != 0;
        }

        private void SetVolume(float volume)
        {
            for (ushort i = 0; i < m_video.audioTrackCount; i++)
            {
                m_target.SetDirectAudioVolume(i, volume);
            }
        }

        private float GetDurationFromVideo()
        {
            return m_video ? (float)(m_endAt.enabled ? m_endAt.value : m_video.length) - (float)(m_startAt.enabled ? m_startAt.value : 0) : 0;
        }

        public override void Apply(float dt)
        {
            if (!m_target.isPlaying)
            {
                m_target.Play();
                m_willPlay = true;
            }
            else
            {
                m_willPlay = false;
            }
            if (m_endAt.enabled && m_target.time >= m_endAt.value)
            {
                if (m_loop.enabled && m_loop.value)
                {
                    m_target.time = m_startAt.enabled ? m_startAt.value : 0;
                }
                else
                {
                    m_target.Stop();
                }
            }
            Progress = Mathf.Clamp01((float)(m_target.time - (m_startAt.enabled ? m_startAt.value : 0)) / m_duration);
        }

        public override void OnRevert()
        {
            m_willPlay = false;
            m_target.Stop();
            m_target.clip = m_previousClip;
            m_target.isLooping = m_wasPlaying;
            m_target.time = m_previousTime;

            if (m_wasPlaying)
            {
                m_target.Play();
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            m_willPlay = false;
            m_target.Stop();
        }

        protected override void ApplyPreview(VideoPlayer preview)
        {
            preview.clip = m_video;
            if (m_startAt.enabled)
            {
                preview.time = m_startAt;
            }
            preview.Stop();
            preview.Play();
        }

        public override string Description
        {
            get
            {
                string target = m_target ? m_target.name + "." : "";
                return target + $"video = {m_video?.name}";
            }
        }
    }
}
