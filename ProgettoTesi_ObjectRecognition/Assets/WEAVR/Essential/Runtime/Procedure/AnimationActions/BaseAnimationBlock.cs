using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class BaseAnimationBlock : ProcedureObject, IRequiresValidation
    {
        public enum DataSource
        {
            Self = 0,
            FromPrevious = 1,
            FromPreviousInTrack = 2,
            FromBlockIndex = 3,
        }

        [SerializeField]
        private AnimationComposer m_composer;
        [SerializeField]
        private int m_variant;
        [SerializeField]
        [Tooltip("The animation curve for the animation block to follow")]
        protected AnimationCurve m_curve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField]
        protected float m_startTime;
        [SerializeField]
        [Tooltip("The duration of the animation block")]
        [AbsoluteValue]
        protected float m_duration = 1;
        [SerializeField]
        [Tooltip("The track in the composer")]
        [PopupInt(AnimationComposer.k_MinTrackId, AnimationComposer.k_MaxTrackId, "Track")]
        protected int m_track;

        [SerializeField]
        [Tooltip("How to get the target")]
        protected DataSource m_targetFrom;
        [SerializeField]
        [Tooltip("from which animation block to get the target")]
        protected int m_targetRefBlock;

        [SerializeField]
        public int separator;

        //private float m_lastTimeAnimated = float.MinValue;
        private float m_lastNormalizedValue = 0;
        private bool m_isAnimating;

        public bool IsAnimating => m_isAnimating;
        private float NormalizedValue => m_lastNormalizedValue;

        private string m_errorMessage;
        public string ErrorMessage
        {
            get => m_errorMessage;
            set
            {
                if(m_errorMessage != value)
                {
                    m_errorMessage = value;
                    PropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        public bool HasErrors => !string.IsNullOrEmpty(m_errorMessage);

        public AnimationComposer Composer {
            get => m_composer;
            set
            {
                if(m_composer != value)
                {
                    BeginChange();
                    m_composer = value;
                    PropertyChanged(nameof(Composer));
                }
            }
         }

        public int Index => m_composer?.AnimationBlocks.IndexOf(this) ?? -1;
        
        public DataSource TargetSourceFrom
        {
            get => m_targetFrom;
            set
            {
                if(m_targetFrom != value)
                {
                    BeginChange();
                    m_targetFrom = value;
                    PropertyChanged(nameof(TargetSourceFrom));
                }
            }
        }

        public int TargetSourceId
        {
            get => m_targetRefBlock;
            set
            {
                if (m_targetRefBlock != value)
                {
                    BeginChange();
                    m_targetRefBlock = value;
                    PropertyChanged(nameof(TargetSourceId));
                }
            }
        }

        public int Variant
        {
            get => m_variant;
            set
            {
                if (m_variant != value)
                {
                    BeginChange();
                    m_variant = value;
                    PropertyChanged(nameof(Variant));
                }
            }
        }

        public float StartTime
        {
            get => m_startTime;
            set
            {
                if(m_startTime != value)
                {
                    BeginChange();
                    m_startTime = value;
                    PropertyChanged(nameof(StartTime));
                }
            }
        }

        public float EndTime => m_startTime + m_duration;

        public float Duration
        {
            get => m_duration;
            set
            {
                value = Mathf.Max(value, 0);
                if(m_duration != value)
                {
                    BeginChange();
                    m_duration = value;
                    PropertyChanged(nameof(Duration));
                }
            }
        }

        public int Track
        {
            get => m_track;
            set
            {
                value = Mathf.Clamp(value, AnimationComposer.k_MinTrackId, AnimationComposer.k_MaxTrackId);
                if (m_track != value)
                {
                    BeginChange();
                    m_track = value;
                    PropertyChanged(nameof(Track));
                }
            }
        }

        private BaseAnimationBlock GetLast(Func<BaseAnimationBlock, bool> filter, int stopId)
        {
            var blocks = m_composer.AnimationBlocks;
            stopId = Mathf.Min(stopId, blocks.Count);
            for (int i = stopId - 1; i >= 0; i--)
            {
                if (filter(blocks[i]))
                {
                    return blocks[i];
                }
            }
            return null;
        }

        public void Reset()
        {
            m_lastNormalizedValue = 0;
            m_isAnimating = false;
        }

        public virtual void Prepare()
        {
            Reset();
        }

        public void Animate(float time)
        {
            float normalizedValue = m_curve.Evaluate(time - m_startTime);
            //float normalizedValue = Mathf.Clamp01(m_curve.Evaluate(time - m_startTime));
            float normalizedDelta = normalizedValue - m_lastNormalizedValue;
            if(normalizedDelta != 0)
            {
                Animate(normalizedDelta, normalizedValue);
                m_isAnimating = NormalizedValue != 1;
            }
            else
            {
                m_isAnimating = false;
            }
            if(m_lastNormalizedValue > 0 && normalizedValue <= 0)
            {
                OnEnd(normalizedDelta);
                m_isAnimating = false;
            }
            else if (m_lastNormalizedValue < 1 && normalizedValue >= 1)
            {
                OnEnd(normalizedDelta);
                m_isAnimating = false;
            }
            m_lastNormalizedValue = normalizedValue;
        }

        public void ResetPreview()
        {
            if (!Application.isPlaying)
            {
                Animate(-m_lastNormalizedValue, 0);
                Reset();
            }
        }

        public virtual void OnStart()
        {
            
        }

        public virtual void OnEnd(float normalizedDelta)
        {

        }

        #region [  INVERSE LERPS  ]

        protected static float InverseLerpA(float delta, float b, float t)
        {
            if(1 - t > 0.00001f)
            {
                return (delta - b * t) / (1 - t);
            }
            else
            {
                return delta - b;
            }
        }

        protected static float InverseLerpB(float delta, float a, float t)
        {
            if (t > 0.00001f)
            {
                return (delta + a*(t - 1)) / t;
            }
            else
            {
                return delta - a;
            }
        }

        protected static Vector3 InverseLerpA(Vector3 delta, Vector3 b, float t)
        {
            if (1 - t > 0.00001f)
            {
                return (delta - b * t) / (1 - t);
            }
            else
            {
                return delta - b;
            }
        }

        protected static Vector3 InverseLerpB(Vector3 delta, Vector3 a, float t)
        {
            if (t > 0.00001f)
            {
                return (delta + a * (t - 1)) / t;
            }
            else
            {
                return delta - a;
            }
        }

        protected static Quaternion InverseSlerpA(Quaternion delta, Quaternion b, float t)
        {
            if (1 - t > 0.00001f)
            {
                Quaternion.RotateTowards(b, delta, 180).ToAngleAxis(out float angle, out Vector3 axis);
                return Quaternion.Inverse(Quaternion.AngleAxis(angle / (1 - t), axis));
            }
            else
            {
                return Quaternion.Inverse(Quaternion.RotateTowards(b, delta, 180));
            }
        }

        protected static Quaternion InverseSlerpB(Quaternion delta, Quaternion a, float t)
        {
            if (t > 0.00001f)
            {
                Quaternion.RotateTowards(a, delta, 180).ToAngleAxis(out float angle, out Vector3 axis);
                return Quaternion.AngleAxis(angle / t, axis);
            }
            else
            {
                return Quaternion.RotateTowards(a, delta, 180);
            }
        }

        #endregion

        public virtual void OnValidate()
        {

        }

        public virtual bool CanProvide<T>() => false;

        public virtual T Provide<T>() => default;

        /// <summary>
        /// Apply a delta change in the animation
        /// </summary>
        /// <param name="normalizedDelta">The normalized delta value, useful when using delta increments</param>
        /// <param name="normalizedValue">The normalized value, usefult when using interpolation</param>
        protected abstract void Animate(float normalizedDelta, float normalizedValue);
    }
}
