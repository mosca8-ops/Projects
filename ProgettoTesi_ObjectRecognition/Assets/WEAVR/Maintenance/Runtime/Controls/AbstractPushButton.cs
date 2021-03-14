using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Maintenance
{
    [Serializable]
    public class UnityFloatEvent : UnityEvent<float> { }

    public abstract class AbstractPushButton : AbstractInteractiveBehaviour
    {
        public enum State { Up, Down }

        [Space]
        [SerializeField]
        protected State m_initialState = State.Up;

        [SerializeField]
        [Tooltip("When stable, the Down state is kept stable")]
        protected bool m_isStable = false;
        [SerializeField]
        protected bool m_isContinuous = false;
        [SerializeField]
        [HiddenBy(nameof(m_isContinuous))]
        protected bool m_hoverLock = false;

        [SerializeField]
        [ShowAsReadOnly]
        protected State m_currentState = State.Up;

        [Space]
        [SerializeField]
        [Range(0, 2)]
        protected float m_transitionTime = 0.1f;
        [SerializeField]
        [Button(nameof(SaveOnPosition), "Save")]
        protected Vector3 m_deltaDownPosition;
        [SerializeField]
        [Button(nameof(SaveOnRotation), "Save")]
        protected Vector3 m_deltaDownEuler;

        [Header("Audio")]
        [SerializeField]
        public bool m_playAudio = false;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        [CanBeGenerated("State_DOWN")]
        public AudioSource m_audioSourceOnDown;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        public AudioClip m_audioClipOnDown;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        [CanBeGenerated("State_UP")]
        public AudioSource m_audioSourceOnUp;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        protected AudioClip m_audioClipOnUp;

        [SerializeField]
        [HideInInspector]
        protected Vector3 m_localUpPosition;
        [SerializeField]
        [HideInInspector]
        protected Vector3 m_localUpEuler;
        
        protected Vector3 m_localDownPosition;
        protected Vector3 m_localDownEuler;

        protected float m_linearSpeed;
        protected float m_angularSpeed;

        private Coroutine m_changeStateCoroutine;
        private float m_pushDownTimestamp;

        private bool m_inTransition;
        protected bool m_isStillActive;

        [SerializeField]
        [HideInInspector]
        protected bool m_hideMainEvents;

        [SerializeField]
        [Space]
        [HiddenBy(nameof(m_hideMainEvents), hiddenWhenTrue: true)]
        private EventGroup m_events;

        public float TransitionTime => m_transitionTime;

        public UnityEvent OnDown => m_events.OnDown;
        public UnityEvent OnUp => m_events.OnUp;
        public UnityEvent OnStateChanged => m_events.OnStateChanged;
        public UnityFloatEvent OnContinuouslyDown => m_events.OnContinuouslyDown;

        public State CurrentState
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

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return true;
        }

        public void SilentlySetState(State value, float? time = null, bool updatedPhysicallyAlso = true)
        {
            if (m_currentState != value)
            {
                m_pushDownTimestamp = Time.time;
                m_currentState = value;
                if (m_currentState == State.Down)
                {
                    PlayAudioOnDown();
                }
                else if (m_currentState == State.Up)
                {
                    PlayAudioOnUp();
                }
            }
            else if (m_isContinuous && m_currentState == State.Down && value == State.Down)
            {
                OnContinuouslyDown.Invoke(time ?? (Time.time - m_pushDownTimestamp));
            }
            if (updatedPhysicallyAlso)
            {
                transform.localPosition = (value == State.Down ? m_localDownPosition : m_localUpPosition);
                transform.localEulerAngles = (value == State.Down ? m_localDownEuler : m_localUpEuler);
            }
        }

        protected void SetState(State value)
        {
            if (m_currentState != value)
            {
                m_currentState = value;
                OnStateChanged.Invoke();
                if (m_currentState == State.Down)
                {
                    OnDown.Invoke();
                    PlayAudioOnDown();
                }
                else if (m_currentState == State.Up)
                {
                    OnUp.Invoke();
                    PlayAudioOnUp();
                }
            }
            else if (m_isContinuous && m_currentState == State.Down && value == State.Down)
            {
                OnContinuouslyDown.Invoke(Time.time - m_pushDownTimestamp);
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return "Push";
        }


        public override void Interact(ObjectsBag currentBag)
        {
            SetupContinuousInteraction();
            ChangeStateTo(m_currentState == State.Down ? State.Up : State.Down, m_transitionTime);
        }

        private void SetupContinuousInteraction()
        {
            m_isStillActive = m_currentState == State.Up && InteractTrigger == BehaviourInteractionTrigger.OnPointerDown && m_isContinuous;
        }

        protected override void StopInteraction(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteraction(nextBehaviour);
            m_isStillActive = false;
        }

        protected virtual void OnValidate()
        {
            SaveUpState();
        }

        // Use this for initialization
        protected virtual void Start()
        {
            SaveUpState();
            SaveDownState();

            CurrentState = m_initialState;
        }

        private void SaveUpState()
        {
            m_localUpPosition = transform.localPosition;
            m_localUpEuler = transform.localEulerAngles;
        }

        private void SaveDownState()
        {
            m_localDownPosition = transform.localPosition + m_deltaDownPosition;
            m_localDownEuler = transform.localEulerAngles + m_deltaDownEuler;
        }

        protected void ChangeStateTo(State toState, float transitionTime)
        {
            if(m_changeStateCoroutine != null)
            {
                StopCoroutine(m_changeStateCoroutine);
                StopAllCoroutines();
                m_changeStateCoroutine = null;
                m_inTransition = false;
            }
            m_changeStateCoroutine = StartCoroutine(ChangeStateCoroutine(toState, transitionTime));
        }

        private void SetStatePhysically(State toState)
        {
            transform.localPosition = (toState == State.Down ? m_localDownPosition : m_localUpPosition);
            transform.localEulerAngles = (toState == State.Down ? m_localDownEuler : m_localUpEuler);
            SetState(toState);
        }

        private IEnumerator ChangeStateCoroutine(State toState, float transitionTime, float timeout = 2)
        {
            Vector3 targetPosition = (toState == State.Down ? m_localDownPosition : m_localUpPosition);
            Vector3 targetEuler = (toState == State.Down ? m_localDownEuler : m_localUpEuler);

            m_inTransition = true;

            if (transitionTime == 0)
            {
                transform.localPosition = targetPosition;
                transform.localEulerAngles = targetEuler;
                SetState(toState);
            }
            else
            {
                float linearVelocity = m_deltaDownPosition.magnitude / transitionTime;
                float angularVelocity = m_deltaDownEuler.magnitude / transitionTime;
                while (timeout > 0 && (Vector3.SqrMagnitude(targetPosition - transform.localPosition) > 0.000000001f
                    || Vector3.SqrMagnitude(targetEuler - transform.localEulerAngles) > 0.01f))
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, Time.deltaTime * linearVelocity);
                    transform.localEulerAngles = Vector3.MoveTowards(transform.localEulerAngles, targetEuler, Time.deltaTime * angularVelocity);
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                SetState(toState);
            }

            if(!m_isStable && toState == State.Down)
            {
                yield return null;
                m_pushDownTimestamp = Time.time;
                while (IsInteractionStillActive())
                {
                    SetState(State.Down);
                    yield return null;
                }
                SetState(State.Up);
                ChangeStateTo(State.Up, transitionTime);
            }

            m_inTransition = false;
            m_changeStateCoroutine = null;
        }

        protected virtual bool IsInteractionStillActive()
        {
            return m_isStillActive;
        }

        protected virtual void SaveOnPosition()
        {
            m_deltaDownPosition = transform.localPosition - m_localUpPosition;
        }

        protected virtual void SaveOnRotation()
        {
            m_deltaDownEuler = transform.localEulerAngles - m_localUpEuler;
        }

        protected void PlayAudioOnUp()
        {
            PlayAudio(m_audioSourceOnUp, m_audioClipOnUp);
        }

        protected void PlayAudioOnDown()
        {
            PlayAudio(m_audioSourceOnDown, m_audioClipOnDown);
        }

        protected void PlayAudio(AudioSource source, AudioClip clip)
        {
            if (m_playAudio && source != null && Time.time > 3)
            {
                if(clip != null) {
                    var lastClip = source.clip;
                    source.clip = clip;
                    source.Play();
                    source.clip = lastClip;
                }
                else
                {
                    source.Play();
                }
            }
        }

        [Serializable]
        protected struct EventGroup {
            public UnityEvent OnDown;
            public UnityEvent OnUp;
            public UnityEvent OnStateChanged;
            public UnityFloatEvent OnContinuouslyDown;
        }
    }
}
