using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class TextToSpeechAction : BaseAction, IProgressElement, IPreviewElement, IReplayModeElement
    {
        [SerializeField]
        [Tooltip("The text to be synthetized")]
        private LocalizedTTS m_speech;

        public LocalizedTTS Speech
        {
            get => m_speech;
            set
            {
                if(m_speech != value)
                {
                    BeginChange();
                    m_speech = value;
                    PropertyChanged(nameof(Speech));
                }
            }
        }
        public string Text => m_speech?.CurrentValue?.text;

        [SerializeField]
        [HideInInspector]
        private LocalizedAudioClip m_audioClip;
        [SerializeField]
        [Tooltip("The volume to be applied")]
        [Range(0, 1)]
        private float m_volume = 1;
        [SerializeField]
        [Tooltip("The object to play the audio on")]
        [DoNotAutofill]
        [Draggable]
        private ValueProxyTransform m_target;
        [SerializeField]
        [Tooltip("The point in the world where to play the audio. Note: if the target is set then this option is not considered")]
        [HiddenBy(nameof(m_target), hiddenWhenTrue: true)]
        private OptionalProxyVector3 m_point;
        [SerializeField]
        [Tooltip("The pitch of the audio to be played")]
        private OptionalAnimatedFloat m_pitch;
        [SerializeField]
        [Tooltip("If enabled, the audio will become 3D and the distance value will be the max distance from which the audio can be heard")]
        private OptionalAnimatedFloat m_distance;
        [SerializeField]
        [Tooltip("Whether to loop or not the audio")]
        [ShowIf(nameof(CanLoop))]
        private bool m_loop;

        [SerializeField]
        [HideInInspector]
        private string m_debugText;

        private bool m_playAudio;

        private AudioSource m_source;

        public float Progress { get; private set; }
        
        public LocalizedAudioClip LocalizedClip
        {
            get => m_audioClip;
            set
            {
                if(m_audioClip != value)
                {
                    BeginChange();
                    m_audioClip = value;
                    PropertyChanged(nameof(LocalizedClip));
                }
            }
        }

        public AudioClip Clip => m_audioClip;

        public float Volume
        {
            get => m_volume;
            set
            {
                if (m_volume != value)
                {
                    BeginChange();
                    m_volume = Mathf.Clamp01(value);
                    PropertyChanged(nameof(Volume));
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeGUID();
            Progress = 0;

            if(m_audioClip == null)
            {
                var clip = new LocalizedAudioClip();
                m_audioClip = clip;
            }
        }

        protected virtual bool ShowAudioClip()
        {
            return true;
        }

        protected virtual bool CanLoop()
        {
            return AsyncThread != 0;
        }

        public override void OnValidate()
        {
            base.OnValidate();
            m_loop &= AsyncThread != 0;
            m_debugText = Text;
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);

            m_source = AudioSourcePool.Current.Get();

            m_playAudio = true;
            Progress = 0;

            if (m_pitch.enabled)
            {
                m_pitch.value.Start(m_source.pitch);
            }
            if (m_distance.enabled)
            {
                m_source.spatialBlend = 1;
                m_distance.value.Start(m_source.minDistance);
            }
        }

        public override bool Execute(float dt)
        {
            if (m_playAudio)
            {
                m_playAudio = false;
                if (m_target.Value)
                {
                    if (m_target.Value.gameObject.activeInHierarchy)
                    {
                        m_source.transform.SetParent(m_target, false);
                    }
                    else
                    {
                        m_source.transform.position = m_target.Value.position;
                    }
                }
                else if (m_point.enabled)
                {
                    m_source.transform.position = m_point.value;
                }
                m_source.clip = m_audioClip;
                m_source.volume = m_volume;
                m_source.loop = m_loop;
                m_source.Play();
            }
            if (m_pitch.enabled)
            {
                m_source.pitch = m_pitch.value.Next(dt);
            }
            if (m_distance.enabled)
            {
                m_source.minDistance = m_distance.value.Next(dt);
            }
            Progress = m_audioClip.CurrentValue.length > 0 ? m_source.time / m_audioClip.CurrentValue.length : 1;
            return !m_loop && !m_source.isPlaying;
        }

        public override void OnStop()
        {
            base.OnStop();
            StopPlaying();
            ReclaimAudioSource();
        }

        private void ReclaimAudioSource()
        {
            if (m_source)
            {
                AudioSourcePool.Current.Reclaim(m_source);
            }
            m_source = null;
        }

        public override void FastForward()
        {
            base.FastForward();
            StopPlaying();
            Progress = 1;
            ReclaimAudioSource();
        }

        private void StopPlaying()
        {
            if (m_source && m_source.isPlaying)
            {
                m_source.Stop();
            }
            m_playAudio = false;
        }

        protected override void OnStateChanged(ExecutionState value)
        {
            base.OnStateChanged(value);
            if (value.HasFlag(ExecutionState.Finished))
            {
                ReclaimAudioSource();
            }
        }

        public void ResetProgress()
        {
            Progress = 0;
        }

        public bool CanPreview()
        {
            return m_audioClip.CurrentValue;
        }

        public override string GetDescription()
        {
            return $"TTS: " + (string.IsNullOrEmpty(m_debugText) ? Text : m_debugText);
        }
    }
}
