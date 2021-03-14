using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    public abstract class BaseInteractableXApi<T> : MonoBehaviour, IXApiProvider where T : AbstractInteractiveBehaviour
    {
        private List<T> m_behaviours = new List<T>();
        protected XAPIEventDelegate Callback { get; private set; }

        public virtual bool Active => isActiveAndEnabled;

        public void Cleanup()
        {
            CleanupInternal();
            foreach (var interactable in m_behaviours)
            {
                UnregisterEvents(interactable, Callback);
            }
            m_behaviours.Clear();
        }

        protected virtual void Start()
        {

        }

        protected virtual void CleanupInternal()
        {
            
        }

        public void Prepare(Procedure.Procedure procedure, ExecutionMode mode, IEnumerable<AbstractInteractiveBehaviour> behaviours, XAPIEventDelegate callbackToRaise)
        {
            m_behaviours.Clear();
            if(!CanHandle(procedure, mode)) { return; }
            Callback = callbackToRaise;

            PrepareInternal(procedure, mode, behaviours, callbackToRaise);

            foreach(var elem in behaviours)
            {
                if(elem is T interactable)
                {
                    m_behaviours.Add(interactable);
                    RegisterEvents(interactable, procedure, mode, callbackToRaise);
                }
            }
        }

        protected abstract void RegisterEvents(T interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise);
        protected abstract void UnregisterEvents(T interactable, XAPIEventDelegate callbackToRaise);

        protected virtual bool CanHandle(Procedure.Procedure procedure, ExecutionMode mode) => true;

        protected virtual void PrepareInternal(Procedure.Procedure procedure, ExecutionMode mode, IEnumerable<AbstractInteractiveBehaviour> behaviours, XAPIEventDelegate callbackToRaise)
        {
            
        }
    }
}