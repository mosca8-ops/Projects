namespace TXT.WEAVR.Maintenance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Interaction;
    using TXT.WEAVR.Procedure;
    using UnityEngine;
    using UnityEngine.Events;

    public abstract class AbstractExecutable : AbstractInteractiveBehaviour
    {
        public string commandName = "Execute";
        [DoNotExpose]
        public UnityEvent onExecute;

        public event Func<GameObject, ObjectsBag, bool> ConditionToExecute;

        private ObjectsBag _currentBag;
        private bool m_executed;
        private Coroutine m_restoreExecuted;

        public event Action<AbstractExecutable> OnExecuted;

        public bool HasExecuted
        {
            get => m_executed && enabled && gameObject.activeInHierarchy;
            set
            {
                if (m_restoreExecuted != null)
                {
                    StopCoroutine(m_restoreExecuted);
                    m_restoreExecuted = null;
                }
                value &= ConditionToExecute == null || ConditionToExecute(gameObject, _currentBag);
                if (value)
                {
                    OnExecuted?.Invoke(this);
                    onExecute.Invoke();
                    m_executed = true;
                    Controller.CurrentBehaviour = this;
                    m_restoreExecuted = StartCoroutine(RestoreExecutedState());
                }
            }
        }
        
        public bool CanExecute {
            get {
                return ConditionToExecute == null || ConditionToExecute(gameObject, _currentBag);
            }
        }

        public override bool CanBeDefault => true;

        public override bool RequiresContinuousInteractionCallback => true;

        [ExposeMethod]
        public virtual void Execute()
        {
            HasExecuted = true;
        }

        public override bool CanInteract(ObjectsBag currentBag)
        {
            if (!base.CanInteract(currentBag)) { return false; }
            if (currentBag != null)
            {
                _currentBag = currentBag;
                return CanExecute;
            }
            return ConditionToExecute == null;
        }

        public override void Interact(ObjectsBag currentBag)
        {
            if (currentBag != null)
            {
                _currentBag = currentBag;
            }
            Execute();
        }

        protected virtual void Start()
        {
            if (_currentBag == null)
            {
                _currentBag = BagHolder.Main.Bag;
            }
        }

        private void OnEnable()
        {
            m_executed = false;
        }

        private IEnumerator RestoreExecutedState()
        {
            yield return null;
            m_executed = false;
            m_restoreExecuted = null;
        }

        public override void OnDisableInteraction()
        {
            base.OnDisableInteraction();
            if (Weavr.TryGetWEAVRInScene(gameObject.scene, out Transform weavr) && weavr.gameObject.activeInHierarchy)
            {
                weavr.GetComponent<MonoBehaviour>()?.StartCoroutine(RestoreExecutedState());
            }
            //DelayedDisable();
        }

        private async void DelayedDisable()
        {
            await Task.Run(() =>
            {
                Task.Delay(100);
                m_executed = false;
            });
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return commandName;
        }

        public void ChangeCommandName(string name)
        {
            commandName = name;
        }
    }
}