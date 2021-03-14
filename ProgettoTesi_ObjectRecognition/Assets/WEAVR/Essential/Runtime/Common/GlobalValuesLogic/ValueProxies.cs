using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    public abstract class ValueProxy
    {
        [SerializeField]
        protected string m_variableName;
        [SerializeField]
        protected bool m_isVar;
        public abstract ValuesStorage.ValueType ValueType { get; }
        public bool IsVariable => m_isVar;
        public string VariableName
        {
            get => m_variableName;
            set
            {
                if(m_variableName != value)
                {
                    m_variableName = value;
                }
            }
        }

        public abstract string ValueFieldName { get; }

        public abstract void RegisterValue();
    }

    public abstract class ValueProxy<T> : ValueProxy
    {
        [SerializeField]
        protected T m_value;

        public override string ValueFieldName => nameof(m_value);

        public abstract T Value { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CanUseGlobalValues() => m_isVar && Application.isPlaying;

        public static implicit operator T(ValueProxy<T> valueProxy)
        {
            return valueProxy.Value;
        }

        public override string ToString()
        {
            return m_isVar ? $"[{VariableName}]" : m_value?.ToString();
        }
    }

    public abstract class ValueProxyObject<T> : ValueProxy where T : UnityEngine.Object
    {
        [SerializeField]
        protected T m_value;

        public override string ValueFieldName => nameof(m_value);

        public virtual T Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Object;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CanUseGlobalValues() => m_isVar && Application.isPlaying;

        public static implicit operator T(ValueProxyObject<T> valueProxy)
        {
            return valueProxy?.Value;
        }

        public override string ToString()
        {
            return m_isVar ? $"[{VariableName}]" : m_value ? m_value.name : "null";
        }

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }
    }

    [Serializable]
    public class ValueProxyLocalizedString : ValueProxy
    {
        [SerializeField]
        [LongText]
        protected LocalizedString m_value;

        public override string ValueFieldName => nameof(m_value);

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.String;

        public string Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value.CurrentValue) : m_value.CurrentValue;
            set
            {
                if (m_isVar)
                {
                    RegisterValue();
                }
                else
                {
                    m_value = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CanUseGlobalValues() => m_isVar && Application.isPlaying;

        public static implicit operator string(ValueProxyLocalizedString valueProxy)
        {
            return valueProxy.Value;
        }

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }
    }

    #region [ BASIC VALUE TYPES  ]

    [Serializable]
    public sealed class ValueProxyInt : ValueProxy<int>
    {
        public override int Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Integer;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }

        public static implicit operator ValueProxyInt(int value)
        {
            return new ValueProxyInt()
            {
                m_value = value,
            };
        }
    }

    [Serializable]
    public sealed class ValueProxyShort : ValueProxy<short>
    {
        public override short Value
        {
            get => CanUseGlobalValues() ? (short)GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Integer;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }

        public static implicit operator ValueProxyShort(short value)
        {
            return new ValueProxyShort()
            {
                m_value = value,
            };
        }
    }

    [Serializable]
    public sealed class ValueProxyBool : ValueProxy<bool>
    {
        public override bool Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Bool;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }

        public static implicit operator ValueProxyBool(bool value)
        {
            return new ValueProxyBool()
            {
                m_value = value,
            };
        }
    }

    [Serializable]
    public sealed class ValueProxyFloat : ValueProxy<float>
    {
        public override float Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Float;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }

        public static implicit operator ValueProxyFloat(float value)
        {
            return new ValueProxyFloat()
            {
                m_value = value,
            };
        }
    }
    
    [Serializable]
    public sealed class ValueProxyDouble : ValueProxy<double>
    {
        public override double Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, (float)m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Float;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, (float)m_value);
            }
        }

        public static implicit operator ValueProxyDouble(double value)
        {
            return new ValueProxyDouble()
            {
                m_value = value,
            };
        }
    }

    [Serializable]
    public sealed class ValueProxyColor : ValueProxy<Color>
    {
        public override Color Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Color;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }

        public static implicit operator ValueProxyColor(Color value)
        {
            return new ValueProxyColor()
            {
                m_value = value,
            };
        }
    }

    [Serializable]
    public sealed class ValueProxyVector3 : ValueProxy<Vector3>
    {
        public override Vector3 Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.Vector3;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }

        public static implicit operator ValueProxyVector3(Vector3 value)
        {
            return new ValueProxyVector3()
            {
                m_value = value,
            };
        }
    }


    [Serializable]
    public sealed class ValueProxyString : ValueProxy<string>
    {
        public override string Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }

        public override ValuesStorage.ValueType ValueType => ValuesStorage.ValueType.String;

        public override void RegisterValue()
        {
            if (CanUseGlobalValues() && !string.IsNullOrEmpty(m_variableName))
            {
                GlobalValues.Current.SetValue(m_variableName, m_value);
            }
        }

        public static implicit operator ValueProxyString(string value)
        {
            return new ValueProxyString()
            {
                m_value = value,
            };
        }
    }

    [Serializable]
    public sealed class ValueProxyGameObject : ValueProxyObject<GameObject>
    {
        public override GameObject Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetGameObject(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }
    }

    [Serializable]
    public class ValueProxyComponent<T> : ValueProxyObject<T> where T : Component
    {
        public Type ComponentType => typeof(T);

        public override T Value
        {
            get => CanUseGlobalValues() ? GlobalValues.Current.GetComponentValue(m_variableName, m_value) : m_value;
            set
            {
                m_value = value;
                RegisterValue();
            }
        }
    }

    [Serializable]
    public class ValueProxyRenderer : ValueProxyComponent<Renderer> { }

    [Serializable]
    public class ValueProxyTransform : ValueProxyComponent<Transform> { }

    [Serializable]
    public class ValueProxyTexture : ValueProxyObject<Texture> { }

    #endregion

    #region [  OPTIONAL TYPES  ]

    [Serializable]
    public class OptionalProxyInt : Optional<ValueProxyInt>
    {
        public static implicit operator int(OptionalProxyInt optional)
        {
            return optional.value;
        }

        public static implicit operator int?(OptionalProxyInt optional)
        {
            return optional.enabled ? optional.value.Value : (int?)null;
        }

        public static implicit operator OptionalProxyInt(int value)
        {
            return new OptionalProxyInt()
            {
                enabled = true,
                value = new ValueProxyInt()
                {
                    Value = value,
                }
            };
        }
    }

    [Serializable]
    public class OptionalProxyBool : Optional<ValueProxyBool>
    {
        public static implicit operator bool(OptionalProxyBool optional)
        {
            return optional.value;
        }

        public static implicit operator bool?(OptionalProxyBool optional)
        {
            return optional.enabled ? optional.value.Value : (bool?)null;
        }
    }

    [Serializable]
    public class OptionalProxyShort : Optional<ValueProxyShort>
    {
        public static implicit operator short(OptionalProxyShort optional)
        {
            return optional.value;
        }

        public static implicit operator short?(OptionalProxyShort optional)
        {
            return optional.enabled ? optional.value.Value : (short?)null;
        }
    }


    [Serializable]
    public class OptionalProxyFloat : Optional<ValueProxyFloat>
    {
        public static implicit operator float(OptionalProxyFloat optional)
        {
            return optional.value;
        }

        public static implicit operator float?(OptionalProxyFloat optional)
        {
            return optional.enabled ? optional.value.Value : (float?)null;
        }
    }

    [Serializable]
    public class OptionalProxyDouble : Optional<ValueProxyDouble>
    {
        public static implicit operator double(OptionalProxyDouble optional)
        {
            return optional.value;
        }

        public static implicit operator double?(OptionalProxyDouble optional)
        {
            return optional.enabled ? optional.value.Value : (double?)null;
        }
    }

    [Serializable]
    public class OptionalProxyColor : Optional<ValueProxyColor>
    {
        public static implicit operator Color(OptionalProxyColor optional)
        {
            return optional.value;
        }

        public static implicit operator Color?(OptionalProxyColor optional)
        {
            return optional.enabled ? optional.value.Value : (Color?)null;
        }
    }

    [Serializable]
    public class OptionalProxyVector3 : Optional<ValueProxyVector3>
    {
        public static implicit operator Vector3(OptionalProxyVector3 optional)
        {
            return optional.value;
        }

        public static implicit operator Vector3?(OptionalProxyVector3 optional)
        {
            return optional.enabled ? optional.value.Value : (Vector3?)null;
        }
    }

    [Serializable]
    public class OptionalProxyString : Optional<ValueProxyString>
    {
        public static implicit operator string(OptionalProxyString optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalProxyGameObject : Optional<ValueProxyGameObject>
    {
        public static implicit operator GameObject(OptionalProxyGameObject optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalProxyTransform : Optional<ValueProxyTransform>
    {
        public static implicit operator Transform(OptionalProxyTransform optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalProxyRenderer : Optional<ValueProxyRenderer>
    {
        public static implicit operator Renderer(OptionalProxyRenderer optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalProxyTexture : Optional<ValueProxyTexture>
    {
        public static implicit operator Texture(OptionalProxyTexture optional)
        {
            return optional.value;
        }
    }

    #endregion

    #region [  ANIMATED TYPES  ]

    [Serializable]
    public abstract class AnimatedProxy<T, V> : AnimatedValue<T, V> where T: ValueProxy<V>
    {
        public override V ConvertToInner(T value) => value.Value;
    }

    [Serializable]
    public class AnimatedProxyInt : AnimatedProxy<ValueProxyInt, int>
    {
        public override ValueProxyInt ConvertFromInner(int value) => new ValueProxyInt() { Value = value, };

        protected override int Interpolate(int a, int b, float ratio) => (int)Mathf.Lerp(a, b, ratio);
    }

    [Serializable]
    public class AnimatedProxyShort : AnimatedProxy<ValueProxyShort, short>
    {
        public override ValueProxyShort ConvertFromInner(short value) => new ValueProxyShort() { Value = value, };

        protected override short Interpolate(short a, short b, float ratio) => (short)Mathf.Lerp(a, b, ratio);
    }

    [Serializable]
    public class AnimatedProxyFloat : AnimatedProxy<ValueProxyFloat, float>
    {
        public override ValueProxyFloat ConvertFromInner(float value) => new ValueProxyFloat() { Value = value, };

        protected override float Interpolate(float a, float b, float ratio) => (float)Mathf.Lerp(a, b, ratio);
    }

    [Serializable]
    public class AnimatedProxyDouble : AnimatedProxy<ValueProxyDouble, double>
    {
        public override ValueProxyDouble ConvertFromInner(double value) => new ValueProxyDouble() { Value = value, };

        protected override double Interpolate(double a, double b, float ratio) => Mathf.Lerp((float)a, (float)b, ratio);
    }

    [Serializable]
    public class AnimatedProxyColor : AnimatedProxy<ValueProxyColor, Color>
    {
        public override ValueProxyColor ConvertFromInner(Color value) => new ValueProxyColor() { Value = value, };

        protected override Color Interpolate(Color a, Color b, float ratio) => Color.Lerp(a, b, ratio);
    }

    [Serializable]
    public class AnimatedProxyVector3 : AnimatedProxy<ValueProxyVector3, Vector3>
    {
        public override ValueProxyVector3 ConvertFromInner(Vector3 value) => new ValueProxyVector3() { Value = value, };

        protected override Vector3 Interpolate(Vector3 a, Vector3 b, float ratio) => Vector3.Lerp(a, b, ratio);
    }

    [Serializable]
    public class AnimatedProxyString : AnimatedProxy<ValueProxyString, string>
    {
        public override ValueProxyString ConvertFromInner(string value) => new ValueProxyString() { Value = value, };

        protected override string Interpolate(string a, string b, float ratio) => b != null ? b.Substring(0, (int)(b.Length * ratio)) : "";
    }

    [Serializable]
    public class AnimatedProxyBool : AnimatedProxy<ValueProxyBool, bool>
    {
        private float m_floatValue;

        public float Interpolation => m_floatValue;

        protected override void OnStart()
        {
            base.OnStart();
            m_floatValue = m_startValue ? 1 : 0;
        }

        protected override bool Interpolate(bool a, bool b, float ratio)
        {
            m_floatValue = b ? ratio : 1 - ratio;
            return b ? ratio > 0.99f : ratio < 0.01f;
        }

        public override ValueProxyBool ConvertFromInner(bool value) => new ValueProxyBool() { Value = value, };
    }

    #endregion

    #region [  OPTIONAL ANIMATED TYPES  ]

    [Serializable]
    public class OptionalAnimatedProxyInt : Optional<AnimatedProxyInt>
    {
        public static implicit operator int(OptionalAnimatedProxyInt optional)
        {
            return optional.value;
        }

        public static implicit operator int?(OptionalAnimatedProxyInt optional)
        {
            return optional.enabled ? optional.value.Value : (int?)null;
        }
    }

    [Serializable]
    public class OptionalAnimatedProxyBool : Optional<AnimatedProxyBool>
    {
        public static implicit operator bool(OptionalAnimatedProxyBool optional)
        {
            return optional.value;
        }

        public static implicit operator bool?(OptionalAnimatedProxyBool optional)
        {
            return optional.enabled ? optional.value.Value : (bool?)null;
        }
    }

    [Serializable]
    public class OptionalAnimatedProxyShort : Optional<AnimatedProxyShort>
    {
        public static implicit operator short(OptionalAnimatedProxyShort optional)
        {
            return optional.value;
        }

        public static implicit operator short?(OptionalAnimatedProxyShort optional)
        {
            return optional.enabled ? optional.value.Value : (short?)null;
        }
    }


    [Serializable]
    public class OptionalAnimatedProxyFloat : Optional<AnimatedProxyFloat>
    {
        public static implicit operator float(OptionalAnimatedProxyFloat optional)
        {
            return optional.value;
        }

        public static implicit operator float?(OptionalAnimatedProxyFloat optional)
        {
            return optional.enabled ? optional.value.Value : (float?)null;
        }
    }

    [Serializable]
    public class OptionalAnimatedProxyDouble : Optional<AnimatedProxyDouble>
    {
        public static implicit operator double(OptionalAnimatedProxyDouble optional)
        {
            return optional.value;
        }

        public static implicit operator double?(OptionalAnimatedProxyDouble optional)
        {
            return optional.enabled ? optional.value.Value : (double?)null;
        }
    }

    [Serializable]
    public class OptionalAnimatedProxyColor : Optional<AnimatedProxyColor>
    {
        public static implicit operator Color(OptionalAnimatedProxyColor optional)
        {
            return optional.value;
        }

        public static implicit operator Color?(OptionalAnimatedProxyColor optional)
        {
            return optional.enabled ? optional.value.Value : (Color?)null;
        }
    }

    [Serializable]
    public class OptionalAnimatedProxyVector3 : Optional<AnimatedProxyVector3>
    {
        public static implicit operator Vector3(OptionalAnimatedProxyVector3 optional)
        {
            return optional.value;
        }

        public static implicit operator Vector3?(OptionalAnimatedProxyVector3 optional)
        {
            return optional.enabled ? optional.value.Value : (Vector3?)null;
        }
    }

    [Serializable]
    public class OptionalAnimatedProxyString : Optional<AnimatedProxyString>
    {
        public static implicit operator string(OptionalAnimatedProxyString optional)
        {
            return optional.value;
        }
    }

    #endregion
}
