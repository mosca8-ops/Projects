namespace TXT.WEAVR.Cockpit
{
    using UnityEngine;

    public abstract class BaseModifierState : BaseState
    {
        public delegate void OnValueChange(BaseModifierState sender, object lastValue, object newValue);

        public event OnValueChange ValueChanged;

        protected bool _canUpdateProperty;

        [SerializeField]
        protected object _value;
        public virtual object Value {
            get {
                return _value;
            }
            set
            {
                if (_value != value)
                {
                    if(ValueChanged != null)
                    {
                        var lastValue = _value;
                        _value = value;
                        ValueChanged(this, lastValue, value);
                    }
                    else
                    {
                        _value = value;
                    }
                    if (_canUpdateProperty)
                    {
                        EffectiveBinding.Property.Value = _value;
                    }
                }
            }
        }

        protected virtual void OnEnable() {
            _canUpdateProperty = EffectiveBinding != null && EffectiveBinding.mode != BindingMode.Read && EffectiveBinding.Property != null;
        }
    }
}