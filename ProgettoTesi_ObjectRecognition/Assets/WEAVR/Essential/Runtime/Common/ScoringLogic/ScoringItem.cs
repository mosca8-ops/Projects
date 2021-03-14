using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{
    [Serializable]
    public class UnityEventScore : UnityEvent<float> { }

    [Serializable]
    public abstract class ScoringItem : MonoBehaviour, IScoringItem
    {
        [SerializeField]
        protected ScoringSystem m_system;
        [SerializeField]
        protected bool m_autoRegisterToSystem = true;
        [SerializeField]
        protected bool m_autoUnregisterToSystem = true;

        [Space]
        [SerializeField]
        protected OptionalFloat m_validScore;
        [SerializeField]
        protected OptionalFloat m_invalidScore;

        [Space]
        [SerializeField]
        protected ScoringItemEvents m_events;

        [Space]
        [SerializeField]
        [ShowAsReadOnly]
        private float m_currentScore;
        [SerializeField]
        [ShowAsReadOnly]
        private ScoringSystem.Answer m_currentAnswer = ScoringSystem.Answer.NotAnswered;

        public UnityEventFloat OnScoreChanged => m_events.onScoreChanged;
        public UnityEventString OnCorrectAnswer => m_events.onCorrectAnswer;
        public UnityEventString OnWrongAnswer => m_events.onWrongAnswer;
        public UnityEventString OnValueChanged => m_events.onValueChanged;
        public UnityEventString OnCorrectValueChanged => m_events.onCorrectValueChanged;

        public event Action<IScoringItem, ScoringSystem.Answer> AnswerChanged;

        protected void InvokeAnswerChanged() => AnswerChanged?.Invoke(this, CurrentAnswer);

        public ScoringSystem System
        {
            get => m_system;
            set
            {
                if (m_system != value)
                {
                    UnregisterFromSystem();
                    m_system = value;
                    RegisterToSystem();
                }
            }
        }

        public float CurrentScore
        {
            get => m_currentScore;
            set
            {
                if (m_currentScore != value)
                {
                    m_currentScore = value;
                    OnScoreChanged?.Invoke(m_currentScore);
                }
            }
        }

        public ScoringSystem.Answer CurrentAnswer
        {
            get => m_currentAnswer;
            set
            {
                if (m_currentAnswer != value)
                {
                    m_currentAnswer = value;
                    CurrentScore = m_currentAnswer == ScoringSystem.Answer.Correct ? CorrectAnswerPoints : m_currentAnswer == ScoringSystem.Answer.Wrong ? WrongAnswerPoints : 0;
                    if (m_currentAnswer == ScoringSystem.Answer.Correct)
                    {
                        OnCorrectAnswer?.Invoke(AnswerToString());
                    }
                    else if (m_currentAnswer == ScoringSystem.Answer.Wrong)
                    {
                        OnWrongAnswer?.Invoke(AnswerToString());
                    }
                    InvokeAnswerChanged();
                }
            }
        }

        protected virtual void UpdateAnswer()
        {
            
        }

        public void ResetAnswer()
        {
            CurrentAnswer = ScoringSystem.Answer.NotAnswered;
        }

        public virtual float CorrectAnswerPoints => m_validScore.enabled ? m_validScore.value : m_system ? m_system.correctAnswerPoints : 0;

        public virtual float WrongAnswerPoints => m_invalidScore.enabled ? m_invalidScore.value : m_system ? m_system.wrongAnswerPoints : 0;

        protected virtual void Reset()
        {
            m_validScore = 10;
            m_validScore.enabled = false;
            m_invalidScore = -1;
            m_invalidScore.enabled = false;

            m_system = GetComponentInParent<ScoringSystem>();
        }

        protected virtual void OnValidate()
        {
            if (!m_system)
            {
                m_system = GetComponentInParent<ScoringSystem>();
            }
        }

        public void UnregisterFromSystem()
        {
            if (m_system)
            {
                m_system.Unregister(this);
            }
        }

        public void RegisterToSystem()
        {
            if (m_system)
            {
                m_system.Register(this);
            }
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            m_currentScore = 0;
            m_currentAnswer = ScoringSystem.Answer.NotAnswered;
        }

        protected virtual void OnEnable()
        {
            if (m_autoRegisterToSystem)
            {
                if (!m_system)
                {
                    m_system = GetComponentInParent<ScoringSystem>();
                }
                RegisterToSystem();
            }
        }

        protected virtual void OnDisable()
        {
            if (m_autoUnregisterToSystem)
            {
                UnregisterFromSystem();
            }
        }

        protected virtual string AnswerToString() => m_currentAnswer.ToString();

        [Serializable]
        public struct ScoringItemEvents
        {
            public UnityEventFloat onScoreChanged;
            public UnityEventString onCorrectAnswer;
            public UnityEventString onWrongAnswer;
            public UnityEventString onValueChanged;
            public UnityEventString onCorrectValueChanged;
        }
    }

    public abstract class ScoringItem<T> : ScoringItem, IScoringItem<T>
    { 
        protected T m_currentValue;
        protected T m_correctValue;

        public virtual T CurrentAnsweredValue {
            get => m_currentValue;
            set
            {
                if(!Equals(m_currentValue, value))
                {
                    m_currentValue = value;
                    OnValueChanged?.Invoke(ToString(m_currentValue));
                    UpdateAnswer();
                }
            }
        }

        protected override string AnswerToString() => ToString(m_currentValue);

        protected virtual string ToString(T value) => value?.ToString();

        protected override void UpdateAnswer()
        {
            CurrentAnswer = Equals(CurrentAnsweredValue, CorrectValue) ? ScoringSystem.Answer.Correct : ScoringSystem.Answer.Wrong;
        }

        public virtual T CorrectValue {
            get => m_correctValue;
            set
            {
                if(!Equals(m_correctValue, value))
                {
                    m_correctValue = value;
                    OnCorrectValueChanged?.Invoke(ToString(m_correctValue));
                    if (CurrentAnswer != ScoringSystem.Answer.NotAnswered)
                    {
                        UpdateAnswer();
                    }
                }
            }
        }
    }
}
