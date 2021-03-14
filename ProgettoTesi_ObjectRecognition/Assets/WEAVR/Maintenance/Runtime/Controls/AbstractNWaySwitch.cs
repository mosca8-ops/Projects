using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Maintenance
{

    public abstract class AbstractNWaySwitch : AbstractSwitch
    {

        [SerializeField]
        protected List<SwitchState> m_states;

        [Space]
        [SerializeField]
        protected int m_initialState = 0;
        [SerializeField]
        [Range(0, 2)]
        protected float m_transitionTime = 0.1f;
        [SerializeField]
        [ShowAsReadOnly]
        protected int m_currentState = 0;

        [Header("Audio")]
        [SerializeField]
        public bool m_playAudio = false;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        public AudioSource m_audioSourceOnStateChange;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        public AudioClip m_audioClipOnStateChange;

        protected float m_linearSpeed;
        protected float m_angularSpeed;

        private Coroutine m_changeStateCoroutine;
        private float m_changedTimestamp;

        private bool m_inTransition;

        protected int m_lastState = 0;
        protected int m_direction = 1;

        [SerializeField]
        private Events m_events;
        public UnityEventInteger OnStateChanged => m_events.OnStateChanged;
        public UnityFloatEvent OnContinuously => m_events.OnContinuously;

        public override IReadOnlyList<SwitchState> States => m_states;

        public int CurrentStateIndex {
            get { return m_currentState; }
            set {
                if (!m_inTransition && m_currentState != value && (0 <= value && value < m_states.Count))
                {
                    ChangeStateTo(value, m_transitionTime);
                }
            }
        }

        public SwitchState CurrentState => m_states[m_currentState];

        protected void SetState(int value)
        {
            if (m_currentState != value)
            {
                m_lastState = m_currentState;
                m_currentState = value;
                OnStateChanged.Invoke(value);
                PlayAudio();
            }
            else if (CurrentState.isContinuous && m_currentState == value)
            {
                OnContinuously.Invoke(Time.time - m_changedTimestamp);
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return "Switch to " + GetNextState();
        }

        public override void Interact(ObjectsBag currentBag)
        {
            ChangeStateTo(GetNextState(), m_transitionTime);
        }

        protected virtual int GetNextState()
        {
            int nextState = m_currentState + m_direction;
            if (nextState >= m_states.Count || nextState < 0)
            {
                m_direction = -m_direction;
            }
            return m_currentState + m_direction;
        }

        protected override void Reset()
        {
            base.Reset();
            RefreshStates();
        }

        private void RefreshStates()
        {
            if (m_states == null)
            {
                m_states = new List<SwitchState>();
            }
            if (m_states.Count == 0)
            {
                m_states.Add(new SwitchState(true, false));
            }
            for (int i = 0; i < m_states.Count; i++)
            {
                m_states[i].Switch = this;
            }
        }

        protected virtual void OnValidate()
        {
            RefreshStates();

            if (m_currentState != m_initialState && GetSwitchState(m_initialState).isStable)
            {
                PhysicallySetState(m_initialState);
            }
        }

        protected SwitchState GetSwitchState(int state)
        {
            return m_states[state];
        }

        // Use this for initialization
        protected virtual void Start()
        {
            for (int i = 0; i < m_states.Count; i++)
            {
                m_states[i].Initialize(this);
            }

            CurrentStateIndex = m_initialState;

            Controller.DefaultBehaviour = this;
        }

        protected void ChangeStateTo(int toState, float transitionTime)
        {
            if (m_changeStateCoroutine != null)
            {
                StopCoroutine(m_changeStateCoroutine);
                StopAllCoroutines();
                m_changeStateCoroutine = null;
                m_inTransition = false;
            }
            m_changeStateCoroutine = StartCoroutine(ChangeStateCoroutine(toState, transitionTime));
        }

        private IEnumerator ChangeStateCoroutine(int toState, float transitionTime, float timeout = 2)
        {

            m_inTransition = true;

            if (transitionTime == 0)
            {
                PhysicallySetState(toState);
            }
            else
            {
                var nextState = GetSwitchState(toState);
                var curState = GetSwitchState(m_currentState);

                Vector3 targetPosition = nextState.LocalPosition;
                Quaternion targetRotation = nextState.LocalRotation;

                float linearVelocity = Vector3.Distance(curState.LocalPosition, nextState.LocalPosition) / transitionTime;
                float angularVelocity = Quaternion.Angle(curState.LocalRotation, nextState.LocalRotation) / transitionTime;

                while (timeout > 0 && (Vector3.SqrMagnitude(targetPosition - transform.localPosition) > 0.00001f
                    || Quaternion.Angle(transform.localRotation, targetRotation) > 0.5f))
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, Time.deltaTime * linearVelocity);
                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, Time.deltaTime * angularVelocity);
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                SetState(toState);
            }

            if (!GetSwitchState(toState).isStable)
            {
                yield return null;
                m_changedTimestamp = Time.time;
                while (IsInteractionStillActive())
                {
                    SetState(toState);
                    yield return null;
                }
                //SetState(State.Up);
                ChangeStateTo(m_lastState, transitionTime);
            }

            m_inTransition = false;
            m_changeStateCoroutine = null;
        }

        private void PhysicallySetState(int toState)
        {
            transform.localPosition = GetSwitchState(toState).LocalPosition;
            transform.localRotation = GetSwitchState(toState).LocalRotation;
            SetState(toState);
        }

        protected virtual bool IsInteractionStillActive()
        {
            return false;
        }

        protected void PlayAudio()
        {
            if (m_playAudio && m_audioSourceOnStateChange != null && Time.time > 3)
            {
                if (m_audioClipOnStateChange != null)
                {
                    var lastClip = m_audioSourceOnStateChange.clip;
                    m_audioSourceOnStateChange.clip = m_audioClipOnStateChange;
                    m_audioSourceOnStateChange.Play();
                    m_audioSourceOnStateChange.clip = lastClip;
                }
                else
                {
                    m_audioSourceOnStateChange.Play();
                }
            }
        }

        [Serializable]
        public class UnityEventInteger : UnityEvent<int> { }
        [Serializable]
        public class UnityEventState : UnityEvent<SwitchState> { }

        [Serializable]
        private struct Events
        {
            public UnityEventInteger OnStateChanged;
            public UnityFloatEvent OnContinuously;
        }

    }
}
