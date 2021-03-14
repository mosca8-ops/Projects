namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [HideInternals]
    public class DynamicEnum
    {
        private Dictionary<string, int> _values;

        public Dictionary<string, int> Values {
            get {
                return _values;
            }
        }

        public string CurrentValue {
            get; set;
        }

        public DynamicEnum() {
            _values = new Dictionary<string, int>();
        }

        public DynamicEnum(Enum e) {
            _values = new Dictionary<string, int>();
            int index = 0;
            foreach(Enum en in Enum.GetValues(e.GetType())) {
                _values[en.ToString()] = index++;
            }
        }

        public int this[string name] {
            get {
                int value = -1;
                _values.TryGetValue(name, out value);
                return value;
            }
            set {
                _values[name] = value;
            }
        }
    }
}