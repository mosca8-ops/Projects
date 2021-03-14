using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR
{
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public abstract class AnimatedOptionalValue<T> : AnimatedValue<T> where T : Optional
    {
        public bool HasValue => m_targetValue.enabled;

        public override float Progress { get => m_targetValue.enabled ? base.Progress : 1; set => base.Progress = value; }

        public override void AutoAnimate(T startValue, T endValue, float duration, Action<T> updateCallback)
        {
            base.AutoAnimate(startValue, endValue, duration, m_targetValue.enabled ? updateCallback : null);
        }

        public T Next(T fallbackValue)
        {
            return HasValue ? base.Next() : fallbackValue;
        }

        public T Next(float dt, T fallbackValue)
        {
            return HasValue ? base.Next(dt) : fallbackValue;
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalInt : AnimatedOptionalValue<OptionalInt>
    {
        protected override OptionalInt Interpolate(OptionalInt a, OptionalInt b, float ratio)
        {
            m_currentValue.value = (int)Mathf.Lerp(a, b, ratio);
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalInt(OptionalInt value)
        {
            return new AnimatedOptionalInt() { m_target = value };
        }

        public static implicit operator AnimatedOptionalInt(int value)
        {
            return new AnimatedOptionalInt() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalFloat : AnimatedOptionalValue<OptionalFloat>
    {
        protected override OptionalFloat Interpolate(OptionalFloat a, OptionalFloat b, float ratio)
        {
            m_currentValue.value = Mathf.Lerp(a, b, ratio);
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalFloat(OptionalFloat value)
        {
            return new AnimatedOptionalFloat() { m_target = value };
        }

        public static implicit operator AnimatedOptionalFloat(float value)
        {
            return new AnimatedOptionalFloat() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalColor : AnimatedOptionalValue<OptionalColor>
    {
        protected override OptionalColor Interpolate(OptionalColor a, OptionalColor b, float ratio)
        {
            m_currentValue.value = Color.Lerp(a, b, ratio);
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalColor(OptionalColor value)
        {
            return new AnimatedOptionalColor() { m_target = value };
        }

        public static implicit operator AnimatedOptionalColor(Color value)
        {
            return new AnimatedOptionalColor() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalString : AnimatedOptionalValue<OptionalString>
    {
        protected override OptionalString Interpolate(OptionalString a, OptionalString b, float ratio)
        {
            m_currentValue.value = b?.value.Substring((int)(b.value.Length * ratio));
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalString(OptionalString value)
        {
            return new AnimatedOptionalString() { m_target = value };
        }

        public static implicit operator AnimatedOptionalString(string value)
        {
            return new AnimatedOptionalString() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalBool : AnimatedOptionalValue<OptionalBool>
    {
        [SerializeField]
        private float m_floatValue;

        public float Interpolation => m_floatValue;

        protected override void OnStart()
        {
            base.OnStart();
            m_floatValue = CurrentTargetValue ? 1 : 0;
        }

        protected override OptionalBool Interpolate(OptionalBool a, OptionalBool b, float ratio)
        {
            m_floatValue = ratio;
            m_currentValue.value = ratio >= 0.01f;
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalBool(OptionalBool value)
        {
            return new AnimatedOptionalBool() { m_target = value };
        }

        public static implicit operator AnimatedOptionalBool(bool value)
        {
            return new AnimatedOptionalBool() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalVector2 : AnimatedOptionalValue<OptionalVector2>
    {
        protected override OptionalVector2 Interpolate(OptionalVector2 a, OptionalVector2 b, float ratio)
        {
            m_currentValue.value = Vector2.Lerp(a, b, ratio);
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalVector2(OptionalVector2 value)
        {
            return new AnimatedOptionalVector2() { m_target = value };
        }

        public static implicit operator AnimatedOptionalVector2(Vector2 value)
        {
            return new AnimatedOptionalVector2() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalVector3 : AnimatedOptionalValue<OptionalVector3>
    {
        protected override OptionalVector3 Interpolate(OptionalVector3 a, OptionalVector3 b, float ratio)
        {
            m_currentValue.value = Vector3.Lerp(a, b, ratio);
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalVector3(OptionalVector3 value)
        {
            return new AnimatedOptionalVector3() { m_target = value };
        }

        public static implicit operator AnimatedOptionalVector3(Vector3 value)
        {
            return new AnimatedOptionalVector3() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalVector4 : AnimatedOptionalValue<OptionalVector4>
    {
        protected override OptionalVector4 Interpolate(OptionalVector4 a, OptionalVector4 b, float ratio)
        {
            m_currentValue.value = Vector4.Lerp(a, b, ratio);
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalVector4(OptionalVector4 value)
        {
            return new AnimatedOptionalVector4() { m_target = value };
        }

        public static implicit operator AnimatedOptionalVector4(Vector4 value)
        {
            return new AnimatedOptionalVector4() { m_target = value };
        }
    }

    [Serializable]
    [Obsolete("Use OptionalAnimatedValue instead", false)]
    public class AnimatedOptionalQuaternion : AnimatedOptionalValue<OptionalQuaternion>
    {
        protected override OptionalQuaternion Interpolate(OptionalQuaternion a, OptionalQuaternion b, float ratio)
        {
            m_currentValue.value = Quaternion.Lerp(a, b, ratio);
            return m_currentValue;
        }

        public static implicit operator AnimatedOptionalQuaternion(OptionalQuaternion value)
        {
            return new AnimatedOptionalQuaternion() { m_target = value };
        }

        public static implicit operator AnimatedOptionalQuaternion(Quaternion value)
        {
            return new AnimatedOptionalQuaternion() { m_target = value };
        }
    }
}
