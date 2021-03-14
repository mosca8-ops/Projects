using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public class AnimatedInt : AnimatedValue<int>
    {
        protected override int Interpolate(int a, int b, float ratio) {
            return (int)Mathf.Lerp(a, b, ratio);
        }

        public static implicit operator AnimatedInt(int value) {
            return new AnimatedInt() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedFloat : AnimatedValue<float>
    {
        protected override float Interpolate(float a, float b, float ratio) {
            return Mathf.Lerp(a, b, ratio);
        }

        public static implicit operator AnimatedFloat(float value) {
            return new AnimatedFloat() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedBool : AnimatedValue<bool>
    {
        [SerializeField]
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

        public static implicit operator AnimatedBool(bool value)
        {
            return new AnimatedBool() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedColor : AnimatedValue<Color>
    {
        protected override Color Interpolate(Color a, Color b, float ratio) {
            return Color.Lerp(a, b, ratio);
        }

        public static implicit operator AnimatedColor(Color value) {
            return new AnimatedColor() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedString : AnimatedValue<string>
    {
        protected override string Interpolate(string a, string b, float ratio) {
            return b != null ? b.Substring(0, (int)(b.Length * ratio)) : "";
        }

        public static implicit operator AnimatedString(string value) {
            return new AnimatedString() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedVector2 : AnimatedValue<Vector2>
    {
        protected override Vector2 Interpolate(Vector2 a, Vector2 b, float ratio)
        {
            return Vector2.Lerp(a, b, ratio);
        }

        public static implicit operator AnimatedVector2(Vector2 value)
        {
            return new AnimatedVector2() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedVector3 : AnimatedValue<Vector3>
    {
        protected override Vector3 Interpolate(Vector3 a, Vector3 b, float ratio)
        {
            return Vector3.Lerp(a, b, ratio);
        }

        public static implicit operator AnimatedVector3(Vector3 value)
        {
            return new AnimatedVector3() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedVector4 : AnimatedValue<Vector4>
    {
        protected override Vector4 Interpolate(Vector4 a, Vector4 b, float ratio)
        {
            return Vector4.Lerp(a, b, ratio);
        }

        public static implicit operator AnimatedVector4(Vector4 value)
        {
            return new AnimatedVector4() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedQuaternion : AnimatedValue<Quaternion>
    {
        protected override Quaternion Interpolate(Quaternion a, Quaternion b, float ratio)
        {
            return Quaternion.Lerp(a, b, ratio);
        }

        public static implicit operator AnimatedQuaternion(Quaternion value)
        {
            return new AnimatedQuaternion() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedVector2Int : AnimatedValue<Vector2Int>
    {
        protected override Vector2Int Interpolate(Vector2Int a, Vector2Int b, float ratio)
        {
            return new Vector2Int(a.x + (int)((b.x - a.x) * ratio), 
                                  a.y + (int)((b.y - a.y)*ratio));
        }

        public static implicit operator AnimatedVector2Int(Vector2Int value)
        {
            return new AnimatedVector2Int() { m_target = value };
        }
    }

    [Serializable]
    public class AnimatedVector3Int : AnimatedValue<Vector3Int>
    {
        protected override Vector3Int Interpolate(Vector3Int a, Vector3Int b, float ratio)
        {
            return new Vector3Int(a.x + (int)((b.x - a.x) * ratio),
                                  a.y + (int)((b.y - a.y) * ratio),
                                  a.z + (int)((b.z - a.z) * ratio));
        }

        public static implicit operator AnimatedVector3Int(Vector3Int value)
        {
            return new AnimatedVector3Int() { m_target = value };
        }
    }
}
