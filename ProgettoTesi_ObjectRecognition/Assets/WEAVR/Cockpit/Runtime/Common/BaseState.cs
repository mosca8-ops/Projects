using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TXT.WEAVR.Cockpit
{
    [Serializable]
    public abstract class BaseState : ScriptableObject
    {
        [SerializeField]
        private string _stateName = "State";
        [DoNotExpose]
        public virtual string Name
        {
            get
            {
                return _stateName;
            }
            set
            {
                _stateName = value;
            }
        }

        [SerializeField]
        private BaseCockpitElement _owner;
        [DoNotExpose]
        public virtual BaseCockpitElement Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                if (_owner != value)
                {
                    _owner = value;
                    if (_owner != null)
                    {
                        _binding = _owner.Bindings[0];
                    }
                }
            }
        }

        [SerializeField]
        private bool _isEditable = true;
        [DoNotExpose]
        public virtual bool IsEditable
        {
            get
            {
                return _isEditable;
            }
            protected set
            {
                _isEditable = value;
            }
        }

        [SerializeField]
        private Binding _binding;
        [DoNotExpose]
        public Binding Binding
        {
            get
            {
                return _binding;
            }
            set
            {
                if (_binding != value)
                {
                    _binding = value;
                }
            }
        }

        [DoNotExpose]
        [SerializeField]
        private Binding _overrideBinding;
        public Binding OverrideBinding
        {
            get
            {
                return _overrideBinding;
            }
            set
            {
                if (_overrideBinding != value)
                {
                    _overrideBinding = value;
                }
            }
        }

        [DoNotExpose]
        public Binding EffectiveBinding
        {
            get
            {
                return _overrideBinding ?? _binding;
            }
        }

        public abstract bool UseOwnerEvents { get; }

        protected bool _bindingIsValid;

        public virtual void CreateOverrideBinding()
        {
            if (_overrideBinding == null)
            {
                OverrideBinding = CreateInstance<Binding>();
                OverrideBinding.id = "Override";
                OverrideBinding.mode = Binding.mode;
                OverrideBinding.dataSource = Binding.dataSource;
                OverrideBinding.propertyPath = Binding.propertyPath;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public virtual void OnRemove()
        {
            // Nothing for now (Maybe Override Binding...)
        }

        public virtual void Initialize()
        {
            _bindingIsValid = EffectiveBinding != null && EffectiveBinding.Property != null;
        }

        #region [  INTERACTION  ]
        public virtual bool OnPointerUp(PointerEventData eventData) { return false; }
        public virtual bool OnPointerDown(PointerEventData eventData) { return false; }
        //public virtual bool OnPointerExit(PointerEventData eventData) { return false; }
        //public virtual bool OnPointerEnter(PointerEventData eventData) { return false; }
        public virtual bool OnPointerDrag(PointerEventData eventData) { return false; }
        public virtual bool OnPointerBeginDrag(PointerEventData eventData) { return false; }
        #endregion

        public virtual void Update() { /*Let the derived classes override it if needed */ }
    }
}