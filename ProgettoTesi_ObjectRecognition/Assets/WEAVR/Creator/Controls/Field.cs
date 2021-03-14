using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR
{

    public static class FieldConstants
    {
        public const string indeterminateText = "\u2014";
        public static readonly Color indeterminateTextColor = new Color(0.82f, 0.82f, 0.82f);
    }

    public abstract class Field<T> : BaseField<T>
    {
       
        public Field() : base(null, null) { }

        public override void SetValueWithoutNotify(T newValue)
        {
            base.SetValueWithoutNotify(newValue);
            ValueToGUI(false);
        }

        public void ForceUpdate()
        {
            ValueToGUI(true);
        }

        public abstract bool indeterminate { get; set; }

        protected abstract void ValueToGUI(bool force);
    }
}
