using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Utilities/Value To String")]
    public class ValueToString : MonoBehaviour
    {
        public string prefix;
        public string format = "{0}";
        public string floatFormat = "0.00";

        [Space]
        [SerializeField]
        private UnityEventString m_toString;

        public float Float
        {
            get => 0;
            set
            {
                Invoke(FormatFloat(value));
            }
        }
        
        public int Integer
        {
            get => 0;
            set
            {
                Invoke(value);
            }
        }

        public char Char
        {
            get => ' ';
            set
            {
                Invoke(value);
            }
        }

        public bool Boolean
        {
            get => false;
            set
            {
                Invoke(value);
            }
        }

        public string String
        {
            get => string.Empty;
            set
            {
                Invoke(value);
            }
        }

        public Object UnityObject
        {
            get => null;
            set
            {
                Invoke(value ? value.name : null);
            }
        }

        public Vector2 Vector2
        {
            get => Vector2.zero;
            set
            {
                Invoke($"[{FormatFloat(value.x)}, {FormatFloat(value.y)}]");
            }
        }

        public Vector3 Vector3
        {
            get => Vector3.zero;
            set
            {
                Invoke($"[{FormatFloat(value.x)}, {FormatFloat(value.y)}, {FormatFloat(value.z)}]");
            }
        }

        private bool m_useFormat;
        private void Start()
        {
            m_useFormat = !string.IsNullOrEmpty(format);
        }

        private void Invoke(object s)
        {
            m_toString.Invoke(Format(s));
        }
        private string Format(object s) => prefix + (m_useFormat ? string.Format(format, s) : s);
        private string FormatFloat(float value) => string.IsNullOrEmpty(floatFormat) ? value.ToString() : value.ToString(floatFormat);

    }
}
