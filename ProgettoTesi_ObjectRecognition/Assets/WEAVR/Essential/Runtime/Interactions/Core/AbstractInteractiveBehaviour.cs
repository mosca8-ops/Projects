namespace TXT.WEAVR.Interaction
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using UnityEngine;

    public enum BehaviourInteractionTrigger { OnPointerUp, OnPointerDown }

    public abstract class AbstractInteractiveBehaviour : MonoBehaviour
    {
        private bool m_isInteractionEnding;

        [SerializeField]
        protected ObjectClass _objectClass;
        public virtual ObjectClass ObjectClass {
            get {
                return _objectClass;
            }
            set {
                _objectClass = value;
            }
        }

        [SerializeField]
        [IgnoreStateSerialization]
        private BehaviourInteractionTrigger m_interactTrigger = BehaviourInteractionTrigger.OnPointerUp;
        [IgnoreStateSerialization]
        public virtual BehaviourInteractionTrigger InteractTrigger
        {
            get
            {
                return m_interactTrigger;
            }
            set
            {
                m_interactTrigger = value;
            }
        }

        protected Func<bool> m_interactCondition = () => true;
        public Func<bool> InteractCondition {
            get { return m_interactCondition; }
            set { m_interactCondition = value; }
        }

        [SerializeField]
        [Tooltip("If not persistent, it will stop on next interaction")]
        protected bool m_persistent = false;
        [IgnoreStateSerialization]
        public bool IsPersistent
        {
            get { return m_persistent; }
            set { m_persistent = value; }
        }

        [SerializeField]
        protected bool m_canInteract = true;
        [IgnoreStateSerialization]
        public virtual bool IsInteractive {
            get {
                return m_canInteract;
            }
            set {
                m_canInteract = value;
            }
        }

        private AbstractInteractionController m_controller;
        public AbstractInteractionController Controller {
            get {
                if (m_controller == null)
                {
                    m_controller = GetComponent<AbstractInteractionController>();
                }
                return m_controller;
            }
        }

        public event Action<AbstractInteractiveBehaviour> Stopped;

        [DoNotExpose]
        public virtual bool RequiresContinuousInteractionCallback => false;

        public virtual bool CanBeDefault { get { return false; } }

        public bool IsCurrentBehaviour => Controller?.CurrentBehaviour == this;

        protected virtual void Reset()
        {
            if (Controller == null)
            {
                m_controller = gameObject.AddComponent<AbstractInteractionController>();
            }
            else
            {
                Controller.UpdateList();
            }

            foreach (var behaviour in GetComponents<AbstractInteractiveBehaviour>())
            {
                if (behaviour != this && !string.IsNullOrEmpty(behaviour.ObjectClass.type))
                {
                    _objectClass = behaviour.ObjectClass;
                    break;
                }
            }
        }

        public bool IsKeepPressedLogic()
        {
            return InteractTrigger == BehaviourInteractionTrigger.OnPointerDown;
        }

        protected virtual void OnDestroy()
        {
            var controller = GetComponent<AbstractInteractionController>();
            if (Controller && this)
            {
                Controller.RemoveFromList(this);
                m_controller = null;
            }
        }

        public virtual void OnDisableInteraction() { }
        public virtual void OnEnableInteraction() { }

        public abstract string GetInteractionName(ObjectsBag currentBag);

        public void EndInteraction(AbstractInteractiveBehaviour nextBehaviour = null)
        {
            if (!m_isInteractionEnding)
            {
                m_isInteractionEnding = true;
                StopInteraction(nextBehaviour);
                StopInteractionVR(nextBehaviour);
                Stopped?.Invoke(this);
                m_isInteractionEnding = false;
            }
        }

        protected virtual void StopInteraction(AbstractInteractiveBehaviour nextBehaviour) { }
        protected virtual void StopInteractionVR(AbstractInteractiveBehaviour nextBehaviour) { }

        public virtual bool CanInteract(ObjectsBag currentBag)
        {
            return m_interactCondition();
        }

        public abstract void Interact(ObjectsBag currentBag);


        #region [  VR PART  ]

        public virtual void InteractVR(ObjectsBag currentBag, object hand)
        {

        }

        public virtual bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return false;
        }
        public virtual bool UseStandardVRInteraction(ObjectsBag bag, object hand)
        {
            return false;
        }

        #endregion
    }
}
