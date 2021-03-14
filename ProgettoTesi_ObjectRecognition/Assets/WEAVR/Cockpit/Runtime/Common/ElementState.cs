namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using UnityEngine;

    [Serializable]
    public class ElementState
    {
        public string state;
        public Vector3 position;
        public Quaternion rotation;
        public bool skippable;
        public bool useAnimator;
        public string animatorParameterName;
        [SerializeField]
        public Common.AnimatorParameter parameter;

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
    }
}