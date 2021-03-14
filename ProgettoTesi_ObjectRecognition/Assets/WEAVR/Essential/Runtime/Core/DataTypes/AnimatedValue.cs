using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public class AnimationData
    {
        public float duration;
        public AnimationCurve curve;

        public AnimationData()
        {
            duration = 1;
            curve = AnimationCurve.Linear(0, 0, 1, 1);
        }
    }

    public abstract class AnimatedValue: IProgressElement
    {
        [SerializeField]
        protected bool m_animate;
        [SerializeField]
        protected bool m_overrideDuration;
        [SerializeField]
        protected bool m_overrideCurve;
        [SerializeField]
        protected bool m_reversible;

        public bool IsAnimated => m_animate;

        public abstract float Progress { get; set; }
        public abstract float Duration { get; set; }
        public abstract bool HasFinished { get; }
        public abstract void Update(float dt);

        public void ResetProgress()
        {
            Progress = 0;
        }

        public bool Reversible { get => m_reversible; set => m_reversible = value; }
    }

    [Serializable]
    public abstract class AnimatedValue<V> : AnimatedValue<V, V>
    {
        public override V ConvertFromInner(V value) => value;
        public override V ConvertToInner(V value) => value;
    }

    [Serializable]
    public abstract class AnimatedValue<T, V> : AnimatedValue
    {
        [SerializeField]
        protected T m_target;
        [SerializeField]
        protected float m_duration;
        [SerializeField]
        protected AnimationCurve m_curve;

        protected V m_startValue;
        protected V m_targetValue;
        protected V m_currentValue;
        protected float m_effectiveDuration;
        protected float m_normalizedProgress;
        protected float m_timeProgress;

        private Action<V> m_animationCallback;

        public abstract T ConvertFromInner(V value);
        public abstract V ConvertToInner(T value);

        public V Value {
            get => m_animate ? m_currentValue : m_targetValue;
        }

        public V TargetValue
        {
            get => ConvertToInner(m_target);
            set
            {
                if (!Application.isPlaying)
                {
                    m_target = ConvertFromInner(value);
                }
            }
        }

        public V CurrentTargetValue {
            get => m_targetValue;
            set {
                if (m_targetValue == null || !m_targetValue.Equals(value)) {
                    m_targetValue = value;
                    OnTargetValueChanged();
                }
            }
        }

        protected virtual void OnTargetValueChanged() {
            
        }

        public override bool HasFinished => m_timeProgress >= m_effectiveDuration;

        protected float TimeProgress {
            get => m_timeProgress;
            set {
                value = Mathf.Clamp(0, value, m_effectiveDuration);
                if (m_timeProgress != value) {
                    m_timeProgress = value;
                    m_normalizedProgress = m_effectiveDuration > 0 ? m_timeProgress / m_effectiveDuration : 1;
                    if (m_animate && m_timeProgress < m_effectiveDuration) {
                        m_currentValue = Interpolate(m_startValue, m_targetValue, m_curve.Evaluate(m_timeProgress));
                    } 
                    else {
                        m_currentValue = m_targetValue;
                    }
                }
            }
        }

        public override float Duration {
            get => m_duration;
            set {
                if(value >= 0 && m_duration != value)
                {
                    if (m_animate)
                    {
                        m_duration = value;
                        m_effectiveDuration = value;
                        //if (m_needsCurveNormalization)
                        //{
                        //}
                        m_curve.Normalize(value);
                        m_timeProgress = m_normalizedProgress * m_effectiveDuration;
                        if (m_timeProgress != m_effectiveDuration)
                        {
                            m_currentValue = Interpolate(m_startValue, m_targetValue, m_curve.Evaluate(m_timeProgress));
                        }
                        else
                        {
                            m_currentValue = m_targetValue;
                        }
                    }
                    else
                    {
                        m_duration = 0;
                        m_effectiveDuration = 0;
                        m_timeProgress = 0;
                        m_normalizedProgress = 1;
                        m_currentValue = m_targetValue;
                    }
                }
            }
        }

        public override float Progress {
            get => m_normalizedProgress;
            set {
                if(m_normalizedProgress != value) {
                    m_normalizedProgress = Mathf.Clamp01(value);
                    m_timeProgress = m_normalizedProgress * m_effectiveDuration;
                    if (m_animate && m_timeProgress != m_effectiveDuration) {
                        m_currentValue = Interpolate(m_startValue, m_targetValue, m_curve.Evaluate(m_timeProgress));
                    } else {
                        m_currentValue = m_targetValue;
                    }
                }
            }
        }
        
        public AnimatedValue() {
            m_curve = AnimationCurve.Linear(0, 0, 1, 1);
        }

        public TAnim Clone<TAnim>() where TAnim : AnimatedValue<T, V>, new()
        {
            return new TAnim()
            {
                m_animate = m_animate,
                m_animationCallback = m_animationCallback,
                m_currentValue = m_currentValue,
                m_curve = m_curve,
                m_duration = m_duration,
                m_effectiveDuration = m_effectiveDuration,
                m_normalizedProgress = m_normalizedProgress,
                m_overrideCurve = m_overrideCurve,
                m_overrideDuration = m_overrideDuration,
                m_reversible = m_reversible,
                m_startValue = m_startValue,
                m_target = m_target,
                m_targetValue = m_targetValue,
                m_timeProgress = m_timeProgress
            };
        }

        public void Start(V startValue, Action<V> updateCallback)
        {
            Start(startValue);
            m_animationCallback = updateCallback;
        }

        public void Start(V startValue) {
            Start(startValue, TargetValue, m_animate ? m_duration : 0);
        }

        public void Start(V startValue, V endValue) {
            Start(startValue, endValue, m_animate ? m_duration : 0);
        }

        public virtual void Start(V startValue, V endValue, float duration) {
            m_animationCallback = null;
            m_effectiveDuration = duration;
            m_startValue = startValue;
            if (m_effectiveDuration <= 0.001f)
            {
                m_timeProgress = duration;
                m_normalizedProgress = 1;
                m_currentValue = endValue;
                CurrentTargetValue = endValue;
            }
            else
            {
                m_currentValue = startValue;
                m_timeProgress = m_normalizedProgress = 0;
                CurrentTargetValue = endValue;
            }
            OnStart();
        }

        protected virtual void OnStart()
        {
            
        }

        public void AutoAnimate(V endValue, Action<V> updateCallback)
        {
            AutoAnimate(m_currentValue, endValue, m_animate && m_reversible ? m_duration : 0, updateCallback);
        }

        public void AutoAnimate(V startValue, V endValue, Action<V> updateCallback)
        {
            AutoAnimate(startValue, endValue, m_animate && m_reversible ? m_duration : 0, updateCallback);
        }

        public virtual void AutoAnimate(V startValue, V endValue, float duration, Action<V> updateCallback)
        {
            if (Application.isEditor)
            {
                try
                {
                    AutoAnimateInternal(startValue, endValue, duration, updateCallback);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{typeof(V).Name}].AutoAnimate: {e.Message}");
                }
            }
            else
            {
                AutoAnimateInternal(startValue, endValue, duration, updateCallback);
            }
        }

        private void AutoAnimateInternal(V startValue, V endValue, float duration, Action<V> updateCallback)
        {
            if (updateCallback == null)
            {
                return;
            }

            m_effectiveDuration = duration;
            if (m_effectiveDuration <= 0.001f)
            {
                m_timeProgress = duration;
                m_normalizedProgress = 1;
                m_currentValue = endValue;
                CurrentTargetValue = endValue;

                updateCallback(m_currentValue);
            }
            else
            {
                TimeProgress = 0;
                CurrentTargetValue = endValue;
                m_startValue = m_currentValue = startValue;

                AnimatedValuesUpdater.Instance.RegisterUpdateCallback(this, dt => updateCallback(Next(dt)));
            }
        }

        public override void Update(float dt)
        {
            if (m_animationCallback != null)
            {
                m_animationCallback(Next(dt));
            }
            else
            {
                Next(dt);
            }
        }

        public virtual V Next() {
            return Next(Time.deltaTime);
        }

        public virtual V Next(float dt) {
            if (m_timeProgress < m_effectiveDuration) {
                TimeProgress += dt;
            }
            return m_currentValue;
        }

        protected abstract V Interpolate(V a, V b, float ratio);

        public static implicit operator V(AnimatedValue<T, V> animValue)
        {
            return animValue.Value;
        }

        public override string ToString()
        {
            return m_target?.ToString();
        }
    }
}