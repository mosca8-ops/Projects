using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Maintenance
{
    public enum Switch3WayState { Up, Middle, Down }

    public abstract class AbstractThreeWaySwitch : AbstractSwitch
    {
        [SerializeField]
        protected SwitchState m_up = new SwitchState(true, false);
        [SerializeField]
        protected SwitchState m_middle = new SwitchState(true, false);
        [SerializeField]
        protected SwitchState m_down = new SwitchState(true, false);

        [Space]
        [SerializeField]
        protected Switch3WayState m_initialState = Switch3WayState.Middle;
        [SerializeField]
        [Range(0, 2)]
        protected float m_transitionTime = 0.1f;
        [SerializeField]
        [ShowAsReadOnly]
        [IgnoreStateSerialization]
        protected Switch3WayState m_currentState = Switch3WayState.Middle;

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
        
        private List<SwitchState> m_states;
        public override IReadOnlyList<SwitchState> States
        {
            get
            {
                if (m_states == null || m_states.Count == 0)
                {
                    m_states = new List<SwitchState>()
                    {
                        m_up,
                        m_middle,
                        m_down,
                    };
                }
                return m_states;
            }
        }

        private bool m_inTransition;

        protected Switch3WayState m_lastState = Switch3WayState.Middle;

        [SerializeField]
        private Events m_events;

        public UnityEvent OnDown => m_events.OnDown;
        public UnityEvent OnMiddle => m_events.OnMiddle;
        public UnityEvent OnUp => m_events.OnUp;
        public UnityEvent OnStateChanged => m_events.OnStateChanged;
        public UnityFloatEvent OnContinuouslyDown => m_events.OnContinuouslyDown;
        public UnityFloatEvent OnContinuouslyUp => m_events.OnContinuouslyUp;

        public float TransitionTime => m_transitionTime;

        public Switch3WayState CurrentState
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

        public void SilentlySetState(Switch3WayState value, float? sameStateTime = null, bool updateAlsoPhysically = true)
        {
            bool updatePhysically = false;
            if (m_currentState != value)
            {
                m_changedTimestamp = Time.time;
                m_lastState = m_currentState;
                m_currentState = value;
                PlayAudio();

                updatePhysically = updateAlsoPhysically;
            }
            else if (m_down.isContinuous && m_currentState == Switch3WayState.Down && value == Switch3WayState.Down)
            {
                OnContinuouslyDown.Invoke(sameStateTime ?? (Time.time - m_changedTimestamp));
                updatePhysically = updateAlsoPhysically;
            }
            else if (m_up.isContinuous && m_currentState == Switch3WayState.Up && value == Switch3WayState.Up)
            {
                OnContinuouslyUp.Invoke(sameStateTime ?? (Time.time - m_changedTimestamp));
                updatePhysically = updateAlsoPhysically;
            }
            if (updatePhysically)
            {
                transform.localPosition = GetSwitchState(value).LocalPosition;
                transform.localRotation = GetSwitchState(value).LocalRotation;
            }
        }

        protected void SetState(Switch3WayState value)
        {
            if (m_currentState != value)
            {
                m_changedTimestamp = Time.time;
                m_lastState = m_currentState;
                m_currentState = value;
                OnStateChanged.Invoke();
                PlayAudio();
                if (m_currentState == Switch3WayState.Down)
                {
                    OnDown.Invoke();
                }
                else if(m_currentState == Switch3WayState.Middle)
                {
                    OnMiddle.Invoke();
                }
                else if (m_currentState == Switch3WayState.Up)
                {
                    OnUp.Invoke();
                }
            }
            else if (m_down.isContinuous && m_currentState == Switch3WayState.Down && value == Switch3WayState.Down)
            {
                OnContinuouslyDown.Invoke(Time.time - m_changedTimestamp);
            }
            else if (m_up.isContinuous && m_currentState == Switch3WayState.Up && value == Switch3WayState.Up)
            {
                OnContinuouslyUp.Invoke(Time.time - m_changedTimestamp);
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

        protected virtual Switch3WayState GetNextState()
        {
            if(m_currentState == Switch3WayState.Up || m_currentState == Switch3WayState.Down)
            {
                return Switch3WayState.Middle;
            }
            return m_lastState == Switch3WayState.Down ? Switch3WayState.Up : Switch3WayState.Down;
        }

        protected override void Reset()
        {
            base.Reset();
            m_up.Switch = this;
            m_middle.Switch = this;
            m_down.Switch = this;
        }

        protected virtual void OnValidate()
        {
            m_up.Switch = this;
            m_middle.Switch = this;
            m_down.Switch = this;

            m_middle.isStable = true;

            if (m_currentState != m_initialState && GetSwitchState(m_initialState).isStable)
            {
                m_up.Initialize();
                m_middle.Initialize();
                m_down.Initialize();
                PhysicallySetState(m_initialState);
            }
        }

        protected SwitchState GetSwitchState(Switch3WayState state)
        {
            return state == Switch3WayState.Down ? m_down : state == Switch3WayState.Middle ? m_middle : m_up;
        }

        // Use this for initialization
        protected virtual void Start()
        {
            m_up.Initialize(this);
            m_middle.Initialize(this);
            m_down.Initialize(this);
            
            CurrentState = m_initialState;

            Controller.DefaultBehaviour = this;
        }

        protected void ChangeStateTo(Switch3WayState toState, float transitionTime)
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

        private IEnumerator ChangeStateCoroutine(Switch3WayState toState, float transitionTime, float timeout = 2)
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

            if (toState == Switch3WayState.Down && !m_down.isStable)
            {
                yield return null;
                m_changedTimestamp = Time.time;
                while (IsInteractionStillActive())
                {
                    SetState(Switch3WayState.Down);
                    yield return null;
                }
                //SetState(State.Up);
                ChangeStateTo(Switch3WayState.Middle, transitionTime);
            }
            else if (toState == Switch3WayState.Up && !m_up.isStable)
            {
                yield return null;
                m_changedTimestamp = Time.time;
                while (IsInteractionStillActive())
                {
                    SetState(Switch3WayState.Up);
                    yield return null;
                }
                //SetState(State.Down);
                ChangeStateTo(Switch3WayState.Middle, transitionTime);
            }

            m_inTransition = false;
            m_changeStateCoroutine = null;
        }

        protected void PhysicallySetState(Switch3WayState toState)
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
        private struct Events
        {
            public UnityEvent OnDown;
            public UnityEvent OnMiddle;
            public UnityEvent OnUp;
            public UnityEvent OnStateChanged;
            public UnityFloatEvent OnContinuouslyDown;
            public UnityFloatEvent OnContinuouslyUp;
        }

    }
}
