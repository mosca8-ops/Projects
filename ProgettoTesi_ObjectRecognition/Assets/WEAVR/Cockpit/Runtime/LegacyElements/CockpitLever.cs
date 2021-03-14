namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    [Serializable]
    public abstract class CockpitLever : CockpitElement
    {
        [SerializeField]
        [HideInInspector]
        protected List<InteractiveElementState> _interactiveStates;

        public List<InteractiveElementState> EditorInteractiveStates {
            get {
                if (_interactiveStates == null)
                {
                    _interactiveStates = new List<InteractiveElementState>();
                }
                return _interactiveStates;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (_interactiveStates == null)
            {
                _interactiveStates = new List<InteractiveElementState>();
            }
            else
            {
                foreach (var state in _interactiveStates)
                {
                    if (state != null)
                    {
                        state.PointerUp += State_PointerUp;
                    }
                }
            }
        }

        protected abstract void State_PointerUp(object sender, UnityEngine.EventSystems.PointerEventData data);
    }
}