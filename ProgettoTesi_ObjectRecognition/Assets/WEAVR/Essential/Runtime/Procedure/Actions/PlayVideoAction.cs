using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using System;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public class PlayVideoAction : BaseReversibleProgressAction, ITargetingObject
    {
        [Serializable]
        private class ValueProxyVideoClip : ValueProxyObject<VideoClip> { }

        [Serializable]
        private class ValueProxyVideoPlayer : ValueProxyComponent<VideoPlayer> { }

        [SerializeField]
        [Tooltip("The video player target to play the video in")]
        [Draggable]
        private ValueProxyVideoPlayer m_player;
        [SerializeField]
        [Tooltip("The video clip to play")]
        [Draggable]
        private ValueProxyVideoClip m_video;
        [SerializeField]
        [Tooltip("The time from where to start the video playback")]
        private OptionalDouble m_startAt;
        [SerializeField]
        [Tooltip("The time when the playback will end")]
        private OptionalDouble m_endAt;
        [SerializeField]
        [Tooltip("Whether to loop the video or not")]
        [ShowIf(nameof(CanLoop))]
        private OptionalBool m_loop;
        [SerializeField]
        [Tooltip("The volume of the video player")]
        [AnimationDataFrom(nameof(GetDurationFromVideo))]
        private OptionalAnimatedFloat m_volume;
        [SerializeField]
        [Tooltip("The playback speed of the player")]
        [AnimationDataFrom(nameof(GetDurationFromVideo))]
        private OptionalAnimatedFloat m_speed;

        public Object Target {
            get => m_player;
            set => m_player.Value = value is VideoPlayer v ? v : 
                            value is Component c ? c.GetComponent<VideoPlayer>() : 
                            value is GameObject go ? go.GetComponent<VideoPlayer>() : 
                            value == null ? null : m_player.Value; }

        public string TargetFieldName => nameof(m_player);

        private VideoClip m_previousClip;
        private bool m_wasPlaying;
        private bool m_wasLooping;
        private float m_prevSpeed;
        private double m_previousTime;
        private float[] m_prevVolumes;
        private float m_duration;
        private bool m_willPlay;

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            var clip = m_video.Value;
            var player = m_player.Value;

            m_previousClip = player.clip;
            m_wasPlaying = player.isPlaying;
            m_wasLooping = player.isLooping;
            m_previousTime = player.time;
            m_prevSpeed = player.playbackSpeed;

            m_prevVolumes = new float[clip.audioTrackCount];
            for (ushort i = 0; i < m_prevVolumes.Length; i++)
            {
                m_prevVolumes[i] = player.GetDirectAudioVolume(i);
            }

            player.clip = m_video;
            player.Stop();
            if (m_loop.enabled) { player.isLooping = m_loop && AsyncThread != 0; }
            if (m_startAt.enabled) { player.time = m_startAt; }
            if (m_volume.enabled)
            {
                if (m_volume.value.IsAnimated)
                {
                    float maxVolume = 0;
                    for (ushort i = 0; i < clip.audioTrackCount; i++)
                    {
                        maxVolume = Mathf.Max(player.GetDirectAudioVolume(i), maxVolume);
                    }
                    m_volume.value.Start(maxVolume);
                }
                else
                {
                    SetVolume(m_volume.value);
                }
            }
            if (m_speed.enabled)
            {
                m_speed.value.Start(player.playbackSpeed);
            }

            m_duration = (float)(m_endAt.enabled ? m_endAt.value : clip.length) - (float)(m_startAt.enabled ? m_startAt.value : 0);
        }

        public override void OnValidate()
        {
            base.OnValidate();
            var video = m_video.Value;
            if (video)
            {
                m_startAt.value = m_startAt.value >= video.length ? video.length - 0.01f : m_startAt.value < 0 ? 0 : m_startAt.value;
                m_endAt.value = m_endAt.value <= m_startAt.value ? m_startAt.value + 0.01f : 
                                m_endAt.value >= video.length ? video.length - 0.01f : m_endAt.value;
            }
            m_loop.value &= AsyncThread != 0;
        }

        private bool CanLoop()
        {
            return AsyncThread != 0;
        }

        private void SetVolume(float volume)
        {
            var player = m_player.Value;
            for (ushort i = 0; i < m_video.Value.audioTrackCount; i++)
            {
                player.SetDirectAudioVolume(i, volume);
            }
        }

        private float GetDurationFromVideo()
        {
            return m_video.Value ? (float)(m_endAt.enabled ? m_endAt.value : m_video.Value.length) - (float)(m_startAt.enabled ? m_startAt.value : 0) : 0;
        }

        public override bool Execute(float dt)
        {
            var player = m_player.Value;

            if (!player.isPlaying)
            {
                player.Play();
                m_willPlay = true;
            }
            else
            {
                m_willPlay = false;
            }
            if (m_volume.enabled && m_volume.value.IsAnimated)
            {
                SetVolume(m_volume.value.Next(dt));
            }
            if (m_speed.enabled)
            {
                player.playbackSpeed = m_speed.value.Next(dt);
            }
            if(m_endAt.enabled && player.time >= m_endAt.value)
            {
                if (m_loop.enabled && m_loop.value)
                {
                    player.time = m_startAt.enabled ? m_startAt.value : 0;
                }
                else
                {
                    player.Stop();
                }
            }
            Progress = (float)(player.time - (m_startAt.enabled ? m_startAt.value : 0)) / m_duration;
            return !m_willPlay && !player.isPlaying;
        }

        public override void OnContextExit(ExecutionFlow flow)
        {
            m_willPlay = false;
            if (RevertOnExit)
            {
                var player = m_player.Value;

                player.Stop();
                player.clip = m_previousClip;
                player.isLooping = m_wasPlaying;
                player.time = m_previousTime;
                player.playbackSpeed = m_prevSpeed;
                
                for (ushort i = 0; i < m_prevVolumes.Length; i++)
                {
                    player.SetDirectAudioVolume(i, m_prevVolumes[i]);
                }

                if (m_wasPlaying)
                {
                    player.Play();
                }
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            m_willPlay = false;
            m_player.Value.Stop();
        }

        public override void FastForward()
        {
            base.FastForward();
            m_willPlay = false;
            m_player.Value.Stop();
        }

        public override string GetDescription()
        {
            string player = m_player.ToString();
            string video = m_video.ToString();
            return $"{player} play video: {video}";
        }
    }
}