namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections.Generic;
    using Core;
    using UnityEngine;

    public abstract class BaseDiscreteState : BaseState
    {
        public StateTriggerZone triggerZone;

        [SerializeField]
        private List<BaseState> _connectedStates;
        [DoNotExpose]
        public List<BaseState> ConnectedStates {
            get {
                if (_connectedStates == null)
                {
                    _connectedStates = new List<BaseState>();
                }
                return _connectedStates;
            }
        }

        public virtual void Awake()
        {
            if (_value == null &&
                EffectiveBinding != null && EffectiveBinding.Property?.Value != null)
            {
                _value = Convert.ChangeType(_stringValue, EffectiveBinding.Property.Value.GetType());
            }
        }

        [SerializeField]
        protected string _stringValue;

        protected object _value;
        public virtual object Value {
            get {
                return _value ?? _stringValue;
            }
            set {
                if (_value != value)
                {
                    _value = value;

                    _stringValue = (value ?? "").ToString();
                }
            }
        }

        public AnimatorParameter AnimatorParameter;

        protected virtual bool UseAnimator {
            get {
                return Owner.UseAnimator;
            }
        }

        [SerializeField]
        private bool _hasValue = true;
        [DoNotExpose]
        public virtual bool HasValue {
            get {
                return _hasValue;
            }
            set {
                _hasValue = value;
            }
        }

        public virtual bool CanEnterState(object value)
        {
            return object.Equals(value, Value);
        }

        public virtual bool CanEnterState(BaseDiscreteState fromState)
        {
            return true;
        }


        protected virtual void ApplyValueUpdate()
        {
            if (_bindingIsValid)
            {
                if (EffectiveBinding.mode != BindingMode.Read && HasValue)
                {
                    EffectiveBinding.Property.Value = Value;
                }
                if (EffectiveBinding.mode != BindingMode.Write && HasValue)
                {
                    Value = EffectiveBinding.Property.Value;
                }
            }
        }

        public virtual Vector3 GetDefaultPosition() { return Owner.defaultLocalPosition; }
        public virtual Quaternion GetDefaultRotation() { return Owner.defaultLocalRotation; }

        public abstract void OnStateEnter(BaseDiscreteState fromState);
        public abstract void OnStateExit(BaseDiscreteState toState);
    }
}