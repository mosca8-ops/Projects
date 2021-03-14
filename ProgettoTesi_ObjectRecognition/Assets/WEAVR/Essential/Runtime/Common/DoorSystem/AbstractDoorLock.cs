using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    public abstract class AbstractDoorLock : AbstractInteractiveBehaviour
    {
        [Space]
        [Draggable]
        [Button(nameof(FindAndAssignDoor), "Find")]
        public AbstractDoor door;
        [SerializeField]
        protected bool m_isLocked = false;

        [SerializeField]
        protected string m_unlockCommand = "Unlock";
        [SerializeField]
        protected string m_lockCommand = "Lock";
        [SerializeField]
        [EnableIfComponentExists(typeof(Animator))]
        protected bool m_useAnimator = false;

        [Space]
        [SerializeField]
        [Draggable]
        protected List<GameObject> m_keys;

        [Header("Audio")]
        [SerializeField]
        public bool m_playAudio = false;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        public float m_audioStartupDelay = 5;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        [Button(nameof(GetSourceOnLock), "Try Get")]
        public AudioSource m_audioSourceOnLock;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        public AudioClip m_audioClipOnLock;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        [Button(nameof(GetSourceOnUnlock), "Try Get")]
        public AudioSource m_audioSourceOnUnlock;
        [SerializeField]
        [HiddenBy(nameof(m_playAudio))]
        [Draggable]
        protected AudioClip m_audioClipOnUnlock;

        private float m_audioIgnore;

        public UnityEvent OnLock;
        public UnityEvent OnUnlock;

        protected Animator m_animator;

        public bool CanLock => door == null || door.IsClosed;

        public virtual bool IsLocked {
            get { return m_isLocked; }
            set {
                if (m_isLocked != value)
                {
                    m_isLocked = value;
                    ApplyLockValue();
                }
            }
        }

        public override bool CanBeDefault => true;

        protected override void Reset()
        {
            base.Reset();
            door = GetComponentInParent<AbstractDoor>();
            Controller.DefaultBehaviour = this;
        }

        private void FindAndAssignDoor()
        {
            door = GetComponentInParent<AbstractDoor>();
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyLockValue();
            }
        }

        // Use this for initialization
        void Awake()
        {
            m_animator = GetComponent<Animator>();
            if (door)
            {
                door.RegisterLock(this);
            }
        }

        protected virtual void Start()
        {
            Controller.DefaultBehaviour = this;
            m_audioIgnore = Time.time + m_audioStartupDelay;
            ApplyLockValue();
        }

        private void ApplyLockValue()
        {
            if (m_isLocked)
            {
                OnLock.Invoke();
                PlayAudioOnLock();
                if (m_useAnimator && m_animator)
                {
                    m_animator.SetTrigger(m_lockCommand);
                }
            }
            else
            {
                OnUnlock.Invoke();
                PlayAudioOnUnlock();
                if (m_useAnimator && m_animator)
                {
                    m_animator.SetTrigger(m_unlockCommand);
                }
            }
        }

        public void Lock()
        {
            IsLocked = true;
        }

        public void UnLock()
        {
            IsLocked = false;
        }

        protected void PlayAudioOnUnlock()
        {
            PlayAudio(m_audioSourceOnUnlock, m_audioClipOnUnlock);
        }

        protected void PlayAudioOnLock()
        {
            PlayAudio(m_audioSourceOnLock, m_audioClipOnLock);
        }

        protected void PlayAudio(AudioSource source, AudioClip clip)
        {
            if (m_playAudio && source != null && Time.time > m_audioIgnore)
            {
                if (clip != null)
                {
                    //var lastClip = source.clip;
                    //source.clip = clip;
                    source.PlayOneShot(clip);
                    //source.clip = lastClip;
                }
                else
                {
                    source.Play();
                }
            }
        }

        private void GetSourceOnLock()
        {
            m_audioSourceOnLock = GetComponentInParent<AudioSource>();
        }

        private void GetSourceOnUnlock()
        {
            m_audioSourceOnUnlock = GetComponentInParent<AudioSource>();
        }

        public void ToggleLock()
        {
            if (CanLock)
            {
                IsLocked = !IsLocked;
            }
        }

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return base.CanInteract(currentBag) && (IsLocked || CanLock);
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return IsLocked ? m_unlockCommand : m_lockCommand;
        }

        public override void Interact(ObjectsBag currentBag)
        {
            if (CanLock)
            {
                if (IsLocked) { UnLock(); }
                else { Lock(); }
            }
        }
    }
}
