using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Maintenance {

    public enum Switch2WayState { Up, Down }

    public abstract class AbstractTwoWaySwitch : AbstractSwitch
    {
        [SerializeField]
        protected SwitchState m_up = new SwitchState(true, false);
        
        [SerializeField]
        protected SwitchState m_down = new SwitchState(true, false);
        
        [Space]
        [SerializeField]
        protected Switch2WayState m_initialState = Switch2WayState.Up;
        [SerializeField]
        [Range(0, 2)]
        protected float m_transitionTime = 0.1f;
        [SerializeField]
        [ShowAsReadOnly]
        [IgnoreStateSerialization]
        protected Switch2WayState m_currentState = Switch2WayState.Up;

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

        private float m_statesLinearDistance;
        private float m_statesAngularDistance;
        
        private List<SwitchState> m_states;
        public override IReadOnlyList<SwitchState> States
        {
            get
            {
                if(m_states == null || m_states.Count == 0)
                {
                    m_states = new List<SwitchState>()
                    {
                        m_up,
                        m_down,
                    };
                }
                return m_states;
            }
        }

        private bool m_inTransition;

        [SerializeField]
        private Events m_events;

        public UnityEvent OnDown => m_events.OnDown;
        public UnityEvent OnUp => m_events.OnUp;
        public UnityEvent OnStateChanged => m_events.OnStateChanged;
        public UnityFloatEvent OnContinuouslyDown => m_events.OnContinuouslyDown;
        public UnityFloatEvent OnContinuouslyUp => m_events.OnContinuouslyUp;

        public float TransitionTime => m_transitionTime;

        public Switch2WayState CurrentState
        {
            get { return m_currentState; }
            set
            {
                if (!m_inTransition && m_currentState != value)
                {
                    ChangeStateTo(value, m_transitionTime);
                }
            }
        }

        public void SetState(int state)
        {
            if(state != 0)
            {
                SetState(Switch2WayState.Up);
            }
            else
            {
                SetState(Switch2WayState.Down);
            }
        }

        public void SilentlySetState(Switch2WayState value)
        {
            if (m_currentState != value)
            {
                m_changedTimestamp = Time.time;
                m_currentState = value;
                PlayAudio();
            }
            else if (m_down.isContinuous && m_currentState == Switch2WayState.Down && value == Switch2WayState.Down)
            {
                OnContinuouslyDown.Invoke(Time.time - m_changedTimestamp);
            }
            else if (m_up.isContinuous && m_currentState == Switch2WayState.Up && value == Switch2WayState.Up)
            {
                OnContinuouslyUp.Invoke(Time.time - m_changedTimestamp);
            }
        }

        protected void SetState(Switch2WayState value)
        {
            if (m_currentState != value)
            {
                m_currentState = value;
                OnStateChanged.Invoke();
                PlayAudio();
                if (m_currentState == Switch2WayState.Down)
                {
                    OnDown.Invoke();
                }
                else if (m_currentState == Switch2WayState.Up)
                {
                    OnUp.Invoke();
                }
            }
            else if (m_down.isContinuous && m_currentState == Switch2WayState.Down && value == Switch2WayState.Down)
            {
                OnContinuouslyDown.Invoke(Time.time - m_changedTimestamp);
            }
            else if (m_up.isContinuous && m_currentState == Switch2WayState.Up && value == Switch2WayState.Up)
            {
                OnContinuouslyUp.Invoke(Time.time - m_changedTimestamp);
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return "Switch";
        }


        public override void Interact(ObjectsBag currentBag)
        {
            ChangeStateTo(m_currentState == Switch2WayState.Down ? Switch2WayState.Up : Switch2WayState.Down, m_transitionTime);
        }

        protected override void Reset()
        {
            base.Reset();
            m_up.Switch = this;
            m_down.Switch = this;
        }

        protected virtual void OnValidate()
        {
            m_up.Switch = this;
            m_down.Switch = this;

            if (!m_up.isStable && !m_down.isStable)
            {
                m_up.isStable = false;
            }

            if (m_currentState != m_initialState && GetSwitchState(m_initialState).isStable)
            {
                m_up.Initialize();
                m_down.Initialize();
                PhysicallySetState(m_initialState);
            }
        }

        protected SwitchState GetSwitchState(Switch2WayState state)
        {
            return state == Switch2WayState.Down ? m_down : m_up;
        }

        // Use this for initialization
        protected virtual void Start()
        {
            m_up.Initialize(this);
            m_down.Initialize(this);

            m_statesLinearDistance = Vector3.Distance(m_up.LocalPosition, m_down.LocalPosition);
            m_statesAngularDistance = Quaternion.Angle(m_up.LocalRotation, m_down.LocalRotation);

            CurrentState = m_initialState;

            Controller.DefaultBehaviour = this;
        }

        protected void ChangeStateTo(Switch2WayState toState, float transitionTime)
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

        private IEnumerator ChangeStateCoroutine(Switch2WayState toState, float transitionTime, float timeout = 2)
        {

            m_inTransition = true;

            if (transitionTime == 0)
            {
                PhysicallySetState(toState);
            }
            else
            {
                Vector3 targetPosition = toState == Switch2WayState.Down ? m_down.LocalPosition : m_up.LocalPosition;
                Quaternion targetRotation = toState == Switch2WayState.Down ? m_down.LocalRotation : m_up.LocalRotation;

                float linearVelocity = m_statesLinearDistance / transitionTime;
                float angularVelocity = m_statesAngularDistance / transitionTime;
                while (timeout > 0 && (Vector3.SqrMagnitude(targetPosition - transform.localPosition) > 0.0000001f
                    || Quaternion.Angle(transform.localRotation, targetRotation) > 1f))
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, Time.deltaTime * linearVelocity);
                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, Time.deltaTime * angularVelocity);
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                SetState(toState);
            }

            if (toState == Switch2WayState.Down && !m_down.isStable)
            {
                yield return null;
                m_changedTimestamp = Time.time;
                while (IsInteractionStillActive())
                {
                    SetState(Switch2WayState.Down);
                    yield return null;
                }
                //SetState(State.Up);
                ChangeStateTo(Switch2WayState.Up, transitionTime);
            }
            else if(toState == Switch2WayState.Up && !m_up.isStable)
            {
                yield return null;
                m_changedTimestamp = Time.time;
                while (IsInteractionStillActive())
                {
                    SetState(Switch2WayState.Up);
                    yield return null;
                }
                //SetState(State.Down);
                ChangeStateTo(Switch2WayState.Down, transitionTime);
            }

            m_inTransition = false;
            m_changeStateCoroutine = null;
        }

        private void PhysicallySetState(Switch2WayState toState)
        {
            transform.localPosition = toState == Switch2WayState.Down ? m_down.LocalPosition : m_up.LocalPosition;
            transform.localRotation = toState == Switch2WayState.Down ? m_down.LocalRotation : m_up.LocalRotation;
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
        private struct Events
        {
            public UnityEvent OnDown;
            public UnityEvent OnUp;
            public UnityEvent OnStateChanged;
            public UnityFloatEvent OnContinuouslyDown;
            public UnityFloatEvent OnContinuouslyUp;
        }

    }
}
