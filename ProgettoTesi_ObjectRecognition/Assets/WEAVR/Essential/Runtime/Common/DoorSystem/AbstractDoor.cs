using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace TXT.WEAVR.Common
{
    [Serializable]
    public class DoorOpenEvent : UnityEvent<float> { }

    [SelectionBase]
    public abstract class AbstractDoor : AbstractInteractiveBehaviour, IDoor
    {
        public delegate void OnExternalActionDelegate(AbstractDoor door, DoorAction action);

        public enum DoorAction
        {
            Open,
            Close,
            Lock,
            Unlock,
            StartInteraction,
            EndInteraction,
        }

        public event OnExternalActionDelegate OnDoorAction;

        [Space]
        [SerializeField]
        protected bool m_blockOnFullyOpened = true;
        [SerializeField]
        protected bool m_blockOnClosed = true;
        [SerializeField]
        protected bool m_snapOnClosed = true;

        [Space]
        [SerializeField]
        [Button(nameof(FindLocks), label: "Find Locks")]
        protected bool m_canBeLocked = true;
        [SerializeField]
        [DisabledBy(nameof(m_canBeLocked))]
        [RangeFrom(0, nameof(m_locks))]
        protected int m_locksThreshold;
        [SerializeField]
        [DisabledBy(nameof(m_canBeLocked))]
        [Draggable]
        protected List<AbstractDoorLock> m_locks;

        [Header("Audio")]
        [SerializeField]
        protected bool m_playSounds = false;
        [SerializeField]
        [HiddenBy(nameof(m_playSounds))]
        [Draggable]
        protected AudioSource m_audioSource;
        [SerializeField]
        [HiddenBy(nameof(m_playSounds))]
        [Draggable]
        protected AudioClip m_soundOnClose;
        [SerializeField]
        [HiddenBy(nameof(m_playSounds))]
        [Draggable]
        protected AudioClip m_soundOnOpen;
        [SerializeField]
        [HiddenBy(nameof(m_playSounds))]
        [Draggable]
        protected AudioClip m_soundOnFullyOpen;
        [SerializeField]
        [HiddenBy(nameof(m_playSounds))]
        [Draggable]
        protected AudioClip m_soundDuringOpen;
        [SerializeField]
        [HiddenBy(nameof(m_playSounds))]
        protected float m_repeatPatternDuration = 0.2f;

        [Header("Events")]
        [SerializeField]
        [IgnoreStateSerialization]
        protected EventsContainer m_events;
        [SerializeField]
        [IgnoreStateSerialization]
        protected AdvancedEventsContainer m_advancedEvents;

        [SerializeField]
        [HideInInspector]
        protected Vector3 m_closedLocalPosition;
        [SerializeField]
        [HideInInspector]
        protected Quaternion m_closedLocalRotation;

        [SerializeField]
        [HideInInspector]
        protected Vector3 m_openedLocalPosition;
        [SerializeField]
        [HideInInspector]
        protected Quaternion m_openedLocalRotation;

        protected Rigidbody m_rigidBody;
        protected bool m_wasKinematicOnClose;
        protected bool m_wasKinematicOnFullyOpen;

        protected bool m_isClosed;
        protected bool m_isFullyOpened;
        protected bool m_isLocked;

        protected float m_currentOpening;
        protected bool m_doorIsAnimated;

        private Coroutine m_doorMoveCoroutine;

        public UnityEvent OnClosed => m_events.OnDoorClosed;
        public UnityEvent OnOpening => m_events.OnDoorOpening;
        public UnityEvent OnLocked => m_events.OnDoorLocked;
        public UnityEvent OnUnlocked => m_events.OnDoorUnlocked;
        public UnityEvent OnFullyOpened => m_advancedEvents.OnDoorFullyOpened;
        public DoorOpenEvent OnOpenProgress => m_advancedEvents.OnDoorOpenProgress;
        public UnityEvent OnClosing => m_advancedEvents.OnDoorClosing;

        [IgnoreStateSerialization]
        public float AnimatedOpenProgress
        {
            get { return m_currentOpening; }
            set
            {
                if (m_currentOpening != value)
                {
                    SetCurrentProgressAnimated(value);
                }
            }
        }
        [DoNotExpose]
        public float CurrentOpenProgress
        {
            get
            {
                return m_currentOpening;
            }
            set
            {
                if(m_currentOpening != value)
                {
                    if(value <= 0)
                    {
                        IsFullyOpened = false;
                        IsClosed = true;
                        m_currentOpening = 0;
                    }
                    else if(value >= 1)
                    {
                        IsClosed = false;
                        IsFullyOpened = true;
                        m_currentOpening = 1;
                    }
                    else
                    {
                        m_currentOpening = value;
                        PlayAudio(m_soundDuringOpen, m_repeatPatternDuration);
                    }

                    if (m_doorIsAnimated)
                    {
                        AnimateDoorMovement(m_currentOpening);
                    }
                    m_advancedEvents.OnDoorOpenProgress.Invoke(m_currentOpening);
                }
            }
        }

        [ForcedSetter(nameof(ForceClose))]
        public virtual bool IsClosed {
            get {
                return m_isClosed;
            }
            set {
                if(m_isClosed != value)
                {
                    m_isClosed = value;
                    if (value)
                    {
                        MoveDoorProgressTo(0);
                        PlayAudio(m_soundOnClose);
                        m_events.OnDoorClosed.Invoke();
                        if (m_rigidBody)
                        {
                            m_wasKinematicOnClose = m_rigidBody.isKinematic;
                        }
                        if (m_blockOnClosed)
                        {
                            if (m_rigidBody)
                            {
                                m_rigidBody.isKinematic = true;
                            }
                        }
                        if (m_snapOnClosed)
                        {
                            bool hadGravity = m_rigidBody && m_rigidBody.useGravity;
                            if (m_rigidBody)
                            {
                                m_rigidBody.useGravity = false;
                            }
                            
                            DelayedAction(0.1f, () =>
                            {
                                //transform.localPosition = m_closedLocalPosition;
                                //transform.localRotation = m_closedLocalRotation;
                                if (m_rigidBody)
                                {
                                    m_rigidBody.isKinematic = true;
                                    m_rigidBody.useGravity = hadGravity;
                                }
                            });
                        }
                    }
                    else if(!IsLocked)
                    {
                        if (m_blockOnClosed)
                        {
                            if(m_rigidBody && m_rigidBody.isKinematic)
                            {
                                m_rigidBody.isKinematic = m_wasKinematicOnClose;
                            }
                        }
                        PlayAudio(m_soundOnOpen);
                        m_events.OnDoorOpening.Invoke();
                    }
                    else
                    {
                        m_isClosed = true;
                    }
                }
            }
        }
        
        [ForcedSetter(nameof(ForceFullyOpen))]
        public virtual bool IsFullyOpened {
            get {
                return m_isFullyOpened;
            }
            set {
                if(m_isFullyOpened != value)
                {
                    m_isFullyOpened = value;
                    if (value)
                    {
                        if (m_rigidBody)
                        {
                            m_wasKinematicOnFullyOpen = m_rigidBody.isKinematic;
                        }
                        PlayAudio(m_soundOnFullyOpen);
                        m_advancedEvents.OnDoorFullyOpened?.Invoke();
                        if (m_blockOnFullyOpened)
                        {
                            if (m_rigidBody)
                            {
                                m_rigidBody.isKinematic = true;
                            }
                            MoveDoorProgressTo(1);
                        }
                        else
                        {
                            CurrentOpenProgress = 1;
                        }
                    }
                    else
                    {
                        if (!m_isClosed)
                        {
                            if (m_blockOnFullyOpened && m_rigidBody)
                            {
                                m_rigidBody.isKinematic = m_wasKinematicOnFullyOpen;
                            }
                            m_advancedEvents.OnDoorClosing?.Invoke();
                        }
                    }
                }
            }
        }
        
        [ForcedSetter(nameof(ForceLock))]
        public bool IsLocked {
            get {
                return m_isLocked;
            }
            set {
                if(m_isLocked != value && m_canBeLocked)
                {
                    if (value)
                    {
                        OnDoorAction?.Invoke(this, DoorAction.Lock);
                        Lock();
                    }
                    else
                    {
                        OnDoorAction?.Invoke(this, DoorAction.Unlock);
                        Unlock();
                    }
                }
            }
        }

        protected void RegisterAction(DoorAction action)
        {
            OnDoorAction?.Invoke(this, action);
        }

        public virtual void SetCurrentProgressAnimated(float value)
        {
            StopAllCoroutines();
            if (0 < value && value < 1)
            {
                m_doorIsAnimated = true;
                CurrentOpenProgress = value;
                m_doorIsAnimated = false;
            }
            else
            {
                CurrentOpenProgress = value;
            }
        }

        private void ForceLock(bool @lock)
        {
            if (@lock)
            {
                AnimatedOpenProgress = 0;
                Lock();
                return;
            }
            if (IsLocked)
            {
                Unlock();
            }
        }

        private void ForceClose(bool close)
        {
            if (close) {
                AnimatedOpenProgress = 0;
                return;
            }
            if(IsLocked)
            {
                Unlock();
            }
            IsClosed = false;
        }

        private void ForceFullyOpen(bool fullyOpen)
        {
            if (!fullyOpen)
            {
                IsFullyOpened = false;
                return;
            }
            if (IsLocked)
            {
                Unlock();
            }
            AnimatedOpenProgress = 1;
        }

        protected void PlayAudio(AudioClip clip, float repeatPattern = 0)
        {
            if (m_playSounds && m_audioSource != null && clip != null && Time.time > 3)
            {
                if(m_audioSource.clip != clip && repeatPattern == 0)
                {
                    m_audioSource.PlayOneShot(clip);
                    return;
                }
                if(m_audioSource.clip != clip || m_audioSource.time > clip.length * 0.5f)
                {
                    m_audioSource.Stop();
                    m_audioSource.clip = clip;
                }
                if (!m_audioSource.isPlaying)
                {
                    m_audioSource.time = 0;
                    m_audioSource.Play();
                }
                if(repeatPattern != 0)
                {
                    m_audioSource.loop = true;
                    m_audioSource.SetScheduledEndTime(AudioSettings.dspTime + repeatPattern);
                }
                else
                {
                    m_audioSource.loop = false;
                }
            }
        }

        protected void DelayedAction(float delay, Action action)
        {
            StartCoroutine(DelayedActionCoroutine(delay, action));
        }

        private IEnumerator DelayedActionCoroutine(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

        protected virtual void OnValidate()
        {
            if(m_playSounds && m_audioSource == null)
            {
                m_audioSource = GetComponentInChildren<AudioSource>();
            }
        }

        protected override void Reset()
        {
            base.Reset();
            m_locks = new List<AbstractDoorLock>(GetComponentsInChildren<AbstractDoorLock>());
            m_locksThreshold = m_locks.Count;
        }

        protected virtual void Awake()
        {
            m_rigidBody = GetComponent<Rigidbody>();
        }

        // Use this for initialization
        protected virtual void Start()
        {
            if (m_locks != null)
            {
                for (int i = 0; i < m_locks.Count; i++)
                {
                    m_locks[i].OnLock.RemoveListener(CheckIfLocked);
                    m_locks[i].OnLock.AddListener(CheckIfLocked);
                    m_locks[i].OnUnlock.RemoveListener(CheckIfUnlocked);
                    m_locks[i].OnUnlock.AddListener(CheckIfUnlocked);
                }
            }
            DelayedAction(0.1f, CheckIfLocked);
        }

        protected virtual void Update()
        {
            if (transform.hasChanged && !m_doorIsAnimated)
            {
                UpdateState();
            }
        }

        protected abstract void UpdateState();

        protected virtual void FindLocks()
        {
            m_locks = m_locks.Union(GetComponentsInChildren<AbstractDoorLock>(true)).ToList();
        }

        public void Open()
        {
            if (IsClosed)
            {
                OnDoorAction?.Invoke(this, DoorAction.Open);
                MoveDoorProgressTo(1);
            }
        }
        
        public void Close()
        {
            if (!IsClosed)
            {
                OnDoorAction?.Invoke(this, DoorAction.Close);
                MoveDoorProgressTo(0);
            }
        }

        private void MoveDoorProgressTo(float value)
        {
            StopDoorMove();
            m_doorMoveCoroutine = StartCoroutine(MoveDoor(value, 1.1f));
        }

        protected void StopDoorMove()
        {
            m_doorIsAnimated = false;
            if (m_doorMoveCoroutine != null)
            {
                StopCoroutine(m_doorMoveCoroutine);
            }
        }

        private IEnumerator MoveDoor(float target, float timeout)
        {
            target = Mathf.Clamp01(target);
            bool wasKinematic = false;
            if(m_rigidBody)
            {
                wasKinematic = m_rigidBody.isKinematic;
                m_rigidBody.isKinematic = true;
            }
            m_doorIsAnimated = true;
            float distance = Mathf.Abs(target - m_currentOpening);
            while(m_currentOpening != target && timeout > 0 && m_doorIsAnimated)
            {
                CurrentOpenProgress = Mathf.MoveTowards(m_currentOpening, target, Time.deltaTime);
                timeout -= Time.deltaTime;
                yield return null;
            }
            if(timeout <= 0)
            {
                CurrentOpenProgress = target;
            }
            m_doorIsAnimated = false;
            if(m_rigidBody)
            {
                m_rigidBody.isKinematic = wasKinematic;
            }
            m_doorMoveCoroutine = null;
        }

        public void Unlock()
        {
            if (!m_canBeLocked || !IsLocked) { return; }

            for (int i = 0; i < m_locks.Count; i++)
            {
                m_locks[i].OnUnlock.RemoveListener(CheckIfUnlocked);
                m_locks[i].UnLock();
                m_locks[i].OnUnlock.AddListener(CheckIfUnlocked);
            }

            m_isLocked = false;
            m_events.OnDoorUnlocked.Invoke();
        }

        public void Lock()
        {
            if (!m_canBeLocked || !IsClosed || IsLocked) { return; }

            for (int i = 0; i < m_locks.Count/* && i <= m_locksThreshold*/; i++)
            {
                m_locks[i].OnLock.RemoveListener(CheckIfLocked);
                m_locks[i].Lock();
                m_locks[i].OnLock.AddListener(CheckIfLocked);
            }

            m_isLocked = true;
            m_events.OnDoorLocked.Invoke();
        }

        protected abstract void AnimateDoorMovement(float progress);

        public void DebugSetDoorPosition(float progress)
        {
            CurrentOpenProgress = progress;
            AnimateDoorMovement(progress);
        }

        public void SilentAnimateDoorMovement(float progress)
        {
            if (!IsLocked)
            {
                if (Mathf.Approximately(progress, 0))
                {
                    IsClosed = true;
                }
                else if (Mathf.Approximately(progress, 1))
                {
                    IsFullyOpened = true;
                }
                else
                {
                    m_currentOpening = progress;
                    AnimateDoorMovement(progress);
                    PlayAudio(m_soundDuringOpen, m_repeatPatternDuration);
                }
            }
        }

        public virtual void SnapshotClosed()
        {
            m_closedLocalPosition = transform.localPosition;
            m_closedLocalRotation = transform.localRotation;
        }

        public virtual void SnapshotFullyOpen()
        {
            m_openedLocalPosition = transform.localPosition;
            m_openedLocalRotation = transform.localRotation;
        }

        internal void RegisterLock(AbstractDoorLock doorLock)
        {
            if(m_locks == null)
            {
                m_locks = new List<AbstractDoorLock>();
            }
            if (!m_locks.Contains(doorLock))
            {
                m_locks.Add(doorLock);
                doorLock.OnLock.RemoveListener(CheckIfLocked);
                doorLock.OnLock.AddListener(CheckIfLocked);
                doorLock.OnUnlock.RemoveListener(CheckIfUnlocked);
                doorLock.OnUnlock.AddListener(CheckIfUnlocked);
            }
        }

        protected void CheckIfLocked()
        {
            if(m_locks == null || m_locks.Count == 0 || !m_canBeLocked) { return; }

            int lockedLocks = 0;
            for (int i = 0; i < m_locks.Count; i++)
            {
                if(!m_locks[i] || m_locks[i].door != this)
                {
                    m_locks.RemoveAt(i--);
                    continue;
                }
                if (m_locks[i].IsLocked)
                {
                    lockedLocks++;
                }
            }

            if(lockedLocks >= m_locksThreshold)
            {
                Lock();
            }
        }

        protected void CheckIfUnlocked()
        {
            if (m_locks == null || m_locks.Count == 0 || !m_canBeLocked) { return; }
            int unlockedLocks = 0;
            for (int i = 0; i < m_locks.Count; i++)
            {
                if (!m_locks[i] || m_locks[i].door != this)
                {
                    m_locks.RemoveAt(i--);
                    continue;
                }
                if (!m_locks[i].IsLocked)
                {
                    unlockedLocks++;
                }
            }

            if (unlockedLocks >= m_locksThreshold)
            {
                Unlock();
            }
        }

        #region [  INTERACTIVE BEHAVIOUR PART  ]

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return base.CanInteract(currentBag) && !IsLocked;
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return !IsLocked;
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return IsLocked ? "Unlock" : IsClosed ? "Open" : "Close";
        }

        public override void Interact(ObjectsBag currentBag)
        {
            StopDoorMove();
            OnDoorAction?.Invoke(this, DoorAction.StartInteraction);
            if (IsLocked)
            {
                Unlock();
            }
            else if (IsClosed)
            {
                Open();
            }
            else
            {
                Close();
            }
        }

        protected override void StopInteraction(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteraction(nextBehaviour);
            OnDoorAction?.Invoke(this, DoorAction.EndInteraction);
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            Interact(currentBag);
        }

        #endregion

        [Serializable]
        protected struct EventsContainer
        {
            public UnityEvent OnDoorClosed;
            [FormerlySerializedAs("OnDoorOpened")]
            public UnityEvent OnDoorOpening;
            public UnityEvent OnDoorUnlocked;
            public UnityEvent OnDoorLocked;
        }

        [Serializable]
        protected struct AdvancedEventsContainer
        {
            public UnityEvent OnDoorFullyOpened;
            public DoorOpenEvent OnDoorOpenProgress;
            public UnityEvent OnDoorClosing;
        }
    }
}
