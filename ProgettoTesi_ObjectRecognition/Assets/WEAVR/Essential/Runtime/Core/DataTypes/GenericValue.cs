using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public class ParameterValue : GenericValue
    {
        [SerializeField]
        private int m_paramId;

        public UnityEngine.Object UnityObjectRef
        {
            get => m_objectValue;
            set
            {
                if (m_objectValue != value)
                {
                    m_objectValue = value;
                }
            }
        }
    }

    [Serializable]
    public class GenericValue
    {
        public enum ValueType
        {
            //
            // Summary:
            //     Integer property.
            Integer = 0,
            //
            // Summary:
            //     Boolean property.
            Boolean = 1,
            //
            // Summary:
            //     Float property.
            Float = 2,
            //
            // Summary:
            //     String property.
            String = 3,
            //
            // Summary:
            //     Color property.
            Color = 4,
            //
            // Summary:
            //     Reference to another object.
            ObjectReference = 5,
            //
            // Summary:
            //     Exposed Reference to another object.
            ExposedObjectReference = 6,
            //
            // Summary:
            //     Enumeration property.
            Enum = 7,
            //
            // Summary:
            //     2D vector property.
            Vector2 = 8,
            //
            // Summary:
            //     3D vector property.
            Vector3 = 9,
            //
            // Summary:
            //     4D vector property.
            Vector4 = 10,
            //
            // Summary:
            //     Animation curve property.
            AnimationCurve = 11,
            //
            // Summary:
            //     Gradient property.
            Gradient = 16,
            //
            // Summary:
            //     Quaternion property.
            Quaternion = 17,
            //
            // Summary:
            //     Generic property.
            Generic = 90,
            //
            // Summary:
            //    Byte property.
            Byte = 91,
            //
            // Summary:
            //    Byte property.
            Short = 92,

            None = 99,
            Auto = 100,
        }

        [SerializeField]
        private ValueType m_valueType = ValueType.String;

        [SerializeField]
        [AssignableFrom(nameof(m_variable))]
        private bool m_boolValue;
        [SerializeField]
        [AssignableFrom(nameof(m_variable))]
        private int m_intValue;
        [SerializeField]
        [AssignableFrom(nameof(m_variable))]
        [LongText]
        private string m_stringValue;
        [SerializeField]
        [AssignableFrom(nameof(m_variable))]
        private float m_floatValue;
        [SerializeField]
        private AnimationCurve m_animationCurveValue;
        [SerializeField]
        private string m_gradientValue;
        [SerializeField]
        [AssignableFrom(nameof(m_variable))]
        private Vector4 m_vectorValue;
        [SerializeField]
        protected UnityEngine.Object m_objectValue;
        [SerializeField]
        private ExposedReference<UnityEngine.Object> m_expObjectValue;
        [SerializeField]
        private string m_typename;
        [SerializeField]
        [AssignableFrom(nameof(m_variable))]
        private byte m_byteValue;
        [SerializeField]
        [AssignableFrom(nameof(m_variable))]
        private short m_shortValue;

        [SerializeField]
        private string m_variable;

        [NonSerialized]
        private Gradient m_gradient;
        [NonSerialized]
        private SerializedGradient m_serializedGradient;

        public bool IsVariableValue => !string.IsNullOrEmpty(m_variable);
        public string VariableName => m_variable;

        private Type m_type;
        public Type Type
        {
            get
            {
                if (m_type == null && !string.IsNullOrEmpty(m_typename))
                {
                    m_type = Type.GetType(m_typename);
                }
                return m_type;
            }
            set
            {
                if (Application.isEditor && m_type != value)
                {
                    m_type = value;
                    m_typename = value?.AssemblyQualifiedName;
                    if (m_type != null)
                    {
                        if (m_type.IsSubclassOf(typeof(UnityEngine.Object)))
                        {
                            m_valueType = ValueType.ObjectReference;
                            Value = null;
                        }
                        else
                        {
                            Value = GetDefault(m_type);
                        }
                    }
                }
            }
        }

        public Action<string, UnityEngine.Object, UnityEngine.Object> OnObjectChanged;

        /// <summary>
        /// Since Gradient is quite heavy on serialization, it is better to save it only when needed
        /// </summary>
        private Gradient GradientValue
        {
            get
            {
                if (m_gradient == null)
                {
                    if (string.IsNullOrEmpty(m_gradientValue))
                    {
                        m_gradient = new Gradient();
                    }
                    else
                    {
                        m_serializedGradient = JsonUtility.FromJson<SerializedGradient>(m_gradientValue);
                        if (m_serializedGradient.alphaKeys == null || m_serializedGradient.colorKeys == null)
                        {
                            m_serializedGradient = new SerializedGradient();
                        }
                        m_gradient = m_serializedGradient?.GetUnityGradient();
                    }
                }
                return m_gradient;
            }
            set
            {
                if (m_serializedGradient == null || !m_serializedGradient.Equals(value))
                {
                    m_gradient = value;
                    if (value == null)
                    {
                        m_serializedGradient = null;
                        m_gradientValue = string.Empty;
                    }
                    else if (m_serializedGradient == null)
                    {
                        m_serializedGradient = new SerializedGradient(value);
                        m_gradientValue = JsonUtility.ToJson(m_serializedGradient);
                    }
                    else
                    {
                        m_gradientValue = JsonUtility.ToJson(m_serializedGradient.Update(value));
                    }
                }
            }
        }

        private bool GradientDiffers(Gradient a, Gradient b)
        {
            if (a.alphaKeys.Length != b.alphaKeys.Length || a.colorKeys.Length != b.colorKeys.Length || a.mode != b.mode)
            {
                return false;
            }

            for (int i = 0; i < a.colorKeys.Length; i++)
            {
                if (a.colorKeys[i].time != b.colorKeys[i].time || a.colorKeys[i].color != b.colorKeys[i].color)
                {
                    return false;
                }
            }

            for (int i = 0; i < a.alphaKeys.Length; i++)
            {
                if (a.alphaKeys[i].time != b.alphaKeys[i].time || a.alphaKeys[i].alpha != b.alphaKeys[i].alpha)
                {
                    return false;
                }
            }

            return true;
        }

        [NonSerialized]
        private bool m_valueNeedsInitialization = true;
        private object m_value;

        private IExposedPropertyTable m_resolver;

        public IExposedPropertyTable ReferencesResolver
        {
            get => m_resolver;
            set
            {
                if (m_resolver != value)
                {
                    m_resolver = value;
                }
            }
        }


        public object Value
        {
            get
            {
                if (m_valueType == ValueType.ObjectReference)
                {
                    var objValue = m_objectValue ? m_objectValue : null;
                    objValue = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? objValue : GlobalValues.Current.GetValue(m_variable, objValue);
                    if (Type == typeof(GameObject) && objValue is Component c)
                    {
                        m_value = c.gameObject;
                    }
                    else if (objValue && typeof(Component).IsAssignableFrom(Type))
                    {
                        if (objValue is GameObject go)
                        {
                            m_value = go.GetComponent(Type);
                        }
                        else if (Type?.IsAssignableFrom(objValue.GetType()) == true)
                        {
                            m_value = objValue;
                        }
                        else if (objValue is Component cObj)
                        {
                            m_value = cObj.GetComponent(Type);
                        }
                        else
                        {
                            m_value = objValue;
                        }
                    }
                    else
                    {
                        m_value = objValue;
                    }
                }
                else if (m_valueNeedsInitialization || Application.isEditor)
                {
                    m_type = null;
                    m_valueNeedsInitialization = false;
                    switch (m_valueType)
                    {
                        case ValueType.Integer:
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? m_intValue : GlobalValues.Current.GetValue(m_variable, 0);
                            break;
                        case ValueType.Boolean:
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? m_boolValue : GlobalValues.Current.GetValue(m_variable, false);
                            break;
                        case ValueType.Float:
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? m_floatValue : GlobalValues.Current.GetValue(m_variable, 0);
                            break;
                        case ValueType.Color:
                            var color = new Color(m_vectorValue[0], m_vectorValue[1], m_vectorValue[2], m_vectorValue[3]);
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? color : GlobalValues.Current.GetValue(m_variable, color);
                            break;
                        case ValueType.Gradient:
                            m_value = GradientValue;
                            break;
                        case ValueType.AnimationCurve:
                            m_value = m_animationCurveValue;
                            break;
                        case ValueType.Quaternion:
                            m_value = new Quaternion(m_vectorValue[0], m_vectorValue[1], m_vectorValue[2], m_vectorValue[3]);
                            break;
                        case ValueType.Vector2:
                            m_value = new Vector2(m_vectorValue[0], m_vectorValue[1]);
                            break;
                        case ValueType.Vector3:
                            var vector = new Vector3(m_vectorValue[0], m_vectorValue[1], m_vectorValue[2]);
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? vector : GlobalValues.Current.GetValue(m_variable, vector); ;
                            break;
                        case ValueType.Vector4:
                            m_value = m_vectorValue;
                            break;
                        case ValueType.String:
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? m_stringValue : GlobalValues.Current.GetValue(m_variable, null);
                            break;
                        //case ValueType.ObjectReference:
                        //    m_value = m_objectValue;
                        //    break;
                        case ValueType.ExposedObjectReference:
                            m_value = m_expObjectValue.Resolve(m_resolver != null ? m_resolver : IDBookkeeper.GetSingleton());
                            if (m_value == null && m_objectValue)
                            {
                                m_value = m_objectValue;
                            }
                            break;
                        case ValueType.Enum:
                            m_value = m_stringValue;
                            if (!string.IsNullOrEmpty(m_typename))
                            {
                                Type type = Type.GetType(m_typename);
                                if (type != null && type.IsEnum)
                                {
                                    m_value = Enum.GetValues(type).GetValue(m_intValue);
                                }
                            }
                            break;
                        case ValueType.Byte:
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? m_byteValue : GlobalValues.Current.GetValue(m_variable, 0);
                            break;
                        case ValueType.Short:
                            m_value = string.IsNullOrEmpty(m_variable) || !Application.isPlaying ? m_shortValue : GlobalValues.Current.GetValue(m_variable, 0);
                            break;
                        case ValueType.Generic:
                            m_value = m_stringValue;
                            if (Type != null)
                            {
                                m_value = JsonUtility.FromJson(m_stringValue, Type);
                            }
                            break;
                        case ValueType.Auto:
                            if (Type != null)
                            {
                                if (m_type.IsSubclassOf(typeof(UnityEngine.Object)))
                                {
                                    m_valueType = ValueType.ObjectReference;
                                    Value = null;
                                }
                                else
                                {
                                    Value = GetDefault(m_type);
                                }
                            }
                            break;
                        default:
                            m_value = null;
                            break;
                    }
                }
                return m_value;
            }
            set
            {
                if (Application.isEditor)
                {
                    m_type = null;
                    m_typename = value?.GetType().AssemblyQualifiedName;
                    switch (value)
                    {
                        case int v:
                            m_valueType = ValueType.Integer;
                            m_intValue = string.IsNullOrEmpty(m_variable) ? v : GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case float v:
                            m_valueType = ValueType.Float;
                            m_floatValue = string.IsNullOrEmpty(m_variable) ? v : GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case bool v:
                            m_valueType = ValueType.Boolean;
                            m_boolValue = string.IsNullOrEmpty(m_variable) ? v : GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case Color v:
                            m_valueType = ValueType.Color;
                            m_vectorValue = string.IsNullOrEmpty(m_variable) ? v : GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case AnimationCurve v:
                            m_valueType = ValueType.AnimationCurve;
                            m_animationCurveValue = v;
                            break;
                        case Quaternion q:
                            m_valueType = ValueType.Quaternion;
                            m_vectorValue = new Vector4(q[0], q[1], q[2], q[3]);
                            break;
                        case Vector2 v:
                            m_valueType = ValueType.Vector2;
                            m_vectorValue = v;
                            break;
                        case Vector3 v:
                            m_valueType = ValueType.Vector3;
                            m_vectorValue = string.IsNullOrEmpty(m_variable) ? v : GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case Vector4 v:
                            m_valueType = ValueType.Vector4;
                            m_vectorValue = v;
                            break;
                        case string v:
                            m_valueType = ValueType.String;
                            m_stringValue = string.IsNullOrEmpty(m_variable) ? v : GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case UnityEngine.Object v:
                            m_valueType = ValueType.ObjectReference;
                            m_objectValue = string.IsNullOrEmpty(m_variable) ? v : GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case Enum v:
                            m_valueType = ValueType.Enum;
                            m_stringValue = v.ToString();
                            try
                            {
                                m_intValue = (int)value;
                            }
                            catch
                            {
                                m_intValue = 0;
                                foreach (var en in Enum.GetValues(value.GetType()))
                                {
                                    if (m_value == en)
                                    {
                                        break;
                                    }
                                    m_intValue++;
                                }
                            }
                            break;
                        case byte v:
                            m_valueType = ValueType.Byte;
                            m_byteValue = string.IsNullOrEmpty(m_variable) ? v : (byte)GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case short v:
                            m_valueType = ValueType.Short;
                            m_shortValue = string.IsNullOrEmpty(m_variable) ? v : (short)GlobalValues.Current.GetValue(m_variable, v);
                            break;
                        case null:

                            break;
                        default:
                            m_valueType = ValueType.Generic;
                            m_stringValue = JsonUtility.ToJson(value);
                            break;
                    }
                }
            }
        }

        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        [Serializable]
        private class SerializedGradient
        {
            [Serializable]
            public struct SerializedAlphaKey
            {
                public float alpha;
                public float time;

                public SerializedAlphaKey(float a, float t)
                {
                    alpha = a;
                    time = t;
                }

                public SerializedAlphaKey(GradientAlphaKey key)
                {
                    alpha = key.alpha;
                    time = key.time;
                }

                public GradientAlphaKey AsGradientKey()
                {
                    return new GradientAlphaKey(alpha, time);
                }
            }

            [Serializable]
            public struct SerializedColorKey
            {
                public Color color;
                public float time;

                public SerializedColorKey(Color c, float t)
                {
                    color = c;
                    time = t;
                }

                public SerializedColorKey(GradientColorKey key)
                {
                    color = key.color;
                    time = key.time;
                }

                public GradientColorKey AsGradientKey()
                {
                    return new GradientColorKey(color, time);
                }
            }

            public SerializedAlphaKey[] alphaKeys;
            public SerializedColorKey[] colorKeys;
            public GradientMode mode;

            public SerializedGradient()
            {
                alphaKeys = new SerializedAlphaKey[]
                {
                    new SerializedAlphaKey(1, 0),
                    new SerializedAlphaKey(1, 1),
                };
                colorKeys = new SerializedColorKey[]
                {
                    new SerializedColorKey(Color.white, 0),
                    new SerializedColorKey(Color.white, 1),
                };
                mode = GradientMode.Blend;
            }

            public SerializedGradient(Gradient source)
            {
                alphaKeys = new SerializedAlphaKey[source.alphaKeys.Length];
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    alphaKeys[i] = new SerializedAlphaKey(source.alphaKeys[i]);
                }
                colorKeys = new SerializedColorKey[source.colorKeys.Length];
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i] = new SerializedColorKey(source.colorKeys[i]);
                }
                mode = source.mode;
            }

            public SerializedGradient Update(Gradient source)
            {
                alphaKeys = new SerializedAlphaKey[source.alphaKeys.Length];
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    alphaKeys[i] = new SerializedAlphaKey(source.alphaKeys[i]);
                }
                colorKeys = new SerializedColorKey[source.colorKeys.Length];
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i] = new SerializedColorKey(source.colorKeys[i]);
                }
                mode = source.mode;

                return this;
            }

            public bool Equals(Gradient b)
            {
                if (b == null)
                {
                    return false;
                }
                if (alphaKeys.Length != b.alphaKeys.Length || colorKeys.Length != b.colorKeys.Length || mode != b.mode)
                {
                    return false;
                }

                for (int i = 0; i < colorKeys.Length; i++)
                {
                    if (colorKeys[i].time != b.colorKeys[i].time || colorKeys[i].color != b.colorKeys[i].color)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    if (alphaKeys[i].time != b.alphaKeys[i].time || alphaKeys[i].alpha != b.alphaKeys[i].alpha)
                    {
                        return false;
                    }
                }

                return true;
            }

            public Gradient GetUnityGradient(Gradient existing = null)
            {
                if (existing == null)
                {
                    existing = new Gradient();
                }
                existing.mode = mode;

                var aKeys = new GradientAlphaKey[alphaKeys.Length];
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    aKeys[i] = alphaKeys[i].AsGradientKey();
                }
                existing.alphaKeys = aKeys;

                var cKeys = new GradientColorKey[colorKeys.Length];
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    cKeys[i] = colorKeys[i].AsGradientKey();
                }
                existing.colorKeys = cKeys;

                return existing;
            }
        }
    }
}
