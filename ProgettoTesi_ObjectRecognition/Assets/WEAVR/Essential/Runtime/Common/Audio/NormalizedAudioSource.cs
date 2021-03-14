using System;
using System.Threading.Tasks;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("WEAVR/Audio/Normalized Audio Source")]
    public class NormalizedAudioSource : MonoBehaviour
    {

        private enum PlayingPart { None, Start, Middle, End }

        [SerializeField]
        [Draggable]
        [Loading(nameof(IsLoading), nameof(Progress))]
        private AudioClip m_clip;
        [SerializeField]
        private bool m_autoDetect = true;
        public float startLength;
        public float middleLength;
        public float endLength;

        [SerializeField]
        [Draggable]
        private AudioSource m_audioSource;

        private float m_normalizedStartLength;
        private float m_normalizedEndLength;
        private float m_normalizedMiddleLength;
        private PlayingPart m_currentPlayingPart;

        [System.NonSerialized]
        private bool m_isLoading;
        [System.NonSerialized]
        private float m_progress;

        public AudioClip Clip {
            get { return m_audioSource.clip; }
            set {
                if (m_audioSource.clip != value)
                {
                    m_clip = value;
                    m_audioSource.clip = value;

                    if (value && !Application.isPlaying && m_autoDetect)
                    {
                        TryAutoSetValues();
                    }
                }
            }
        }

        private void OnValidate()
        {
            m_isLoading = false;
            m_progress = 0;
            if (m_audioSource == null)
            {
                m_audioSource = GetComponent<AudioSource>();
            }
            Clip = m_clip;
        }

        bool IsLoading() => m_isLoading;

        float Progress() => m_progress;

        // Use this for initialization
        void Start()
        {
            OnValidate();
            m_normalizedStartLength = startLength / Clip.length;
            m_normalizedEndLength = endLength / Clip.length;
            m_normalizedMiddleLength = middleLength / Clip.length;
            m_currentPlayingPart = PlayingPart.None;
        }

        public void Play(float value)
        {
            value = Mathf.Clamp01(value);

            if (value < m_normalizedStartLength && m_currentPlayingPart != PlayingPart.Start)
            {
                m_currentPlayingPart = PlayingPart.Start;
                m_audioSource.Play();
                m_audioSource.SetScheduledEndTime(Time.time);
            }
            //m_clip.
        }

        private async void TryAutoSetValues()
        {
            float[] samples = new float[m_clip.samples / 2];
            if (!m_clip.GetData(samples, 0))
            { return; }

            m_progress = 0;
            m_isLoading = true;
            float clipLength = m_clip.length;
            var (hasRepeatingPattern, repeatStart, repeatLength, repeatEnd, autoValue) = await Task.Run(() => GetRepeatingLength(samples, 2, clipLength));

            if(!hasRepeatingPattern || autoValue < 0.5f)
            {
                m_isLoading = false;
                return;
            }

            middleLength = repeatLength;
            startLength = repeatStart;
            endLength = m_clip.length - repeatEnd;
            m_isLoading = false;
            m_progress = 0;
        }

        private float Mean(float[] samples)
        {
            if (samples.Length == 0) { return 0; }
            float sum = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i];
            }
            return sum / samples.Length;
        }

        private float Variance(float[] samples, float mean)
        {
            if (samples.Length == 0) { return 0; }
            float[] values = new float[samples.Length];
            return Variance(samples, values, mean);
        }

        private float Variance(float[] samples, float[] values, float mean)
        {
            if (samples.Length == 0) { return 0; }
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (samples[i] - mean) * (samples[i] - mean);
            }
            return Mean(values);
        }

        private float Autoccorelation(float[] samples, int tau, float mean, float variance)
        {
            if (samples.Length == 0) { return 0; }
            float[] values = new float[samples.Length];

            return Autocorrelation(samples, values, tau, mean, variance);
        }

        private float Autocorrelation(float[] values, float[] tempValues, int tau, float mean, float variance)
        {
            if (values.Length == 0) { return 0; }
            for (int i = 0; i < values.Length - tau; i++)
            {
                tempValues[i] = (values[i] - mean) * (values[i + tau] - mean);
            }

            return Mean(tempValues) / (values.Length * variance * variance);
            //return Mean(tempValues) / variance;
        }

        private int ArgMax(float[] samples)
        {
            if (samples.Length == 0) { return 0; }
            int maxInt = 0;
            float maxValue = float.MinValue;
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i] > maxValue)
                {
                    maxValue = samples[i];
                    maxInt = i;
                }
            }
            return maxInt;
        }

        private (bool, float repeatStart, float repeatLength, float repeatEnd, float maxAutoValue) GetRepeatingLength(float[] samples, float maxTestLength, float totalLengthTime)
        {
            float freq = samples.Length / totalLengthTime;
            int maxTau = Mathf.FloorToInt(freq * Mathf.Min(maxTestLength, totalLengthTime));

            float[] values = new float[samples.Length];
            float[] tempValues = new float[samples.Length];
            float[] autoValues = new float[maxTau];
            float mean = Mean(samples);
            float variance = Variance(samples, values, mean);

            int maxIndex = 0;
            int firstOccurence = 0;
            int lastMaxIndex = 0;
            int repeatSize = 0;
            float maxAutoValue = float.MinValue;
            
            for (int i = 0; i < maxTau; i++)
            {
                float autoValue = Autocorrelation(values, tempValues, i, mean, variance);
                if (autoValue > maxAutoValue)
                {
                    maxAutoValue = autoValue;
                    maxIndex = i;
                }
                autoValues[i] = autoValue;

                m_progress = 0.98f * i / maxTau;
            }

            float validAutoValue = maxAutoValue - 0.0001f;
            for (int i = 0; i < maxTau; i++)
            {
                if (autoValues[i] > validAutoValue)
                {
                    if (firstOccurence == 0)
                    {
                        firstOccurence = i;
                    }
                    if (lastMaxIndex != 0 && i - lastMaxIndex > repeatSize)
                    {
                        repeatSize = i - lastMaxIndex;
                        break;
                    }
                    lastMaxIndex = i;
                }
            }

            float repeatStart = (float)firstOccurence / samples.Length;
            float repeatLength = (float)repeatSize / samples.Length;
            float repeatEnd = (float)maxIndex / samples.Length;

            m_progress = 1;

            return (maxIndex != 0, repeatStart, repeatLength, repeatEnd, maxAutoValue);
            //return maxIndex / samples.Length;
        }
    }
}