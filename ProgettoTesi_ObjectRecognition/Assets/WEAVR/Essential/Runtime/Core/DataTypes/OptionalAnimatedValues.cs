using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR
{

    public abstract class OptionalAnimatedValue<T> : Optional<T>, IProgressElement where T : AnimatedValue
    {
        public virtual bool HasFinished => !enabled || value.HasFinished;

        public void Update(float dt)
        {
            if (enabled) { value?.Update(dt); }
        }

        public float Progress => enabled ? value.Progress : 1;

        public void ResetProgress()
        {
            if (enabled)
            {
                value.Progress = 0;
            }
        }
    }

    [Serializable]
    public class OptionalAnimatedInt : OptionalAnimatedValue<AnimatedInt>
    {
        public static implicit operator OptionalAnimatedInt(AnimatedInt value)
        {
            return new OptionalAnimatedInt()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedInt(int value)
        {
            return new OptionalAnimatedInt()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedInt(OptionalAnimatedInt optional)
        {
            return optional.value;
        }

        public static implicit operator int(OptionalAnimatedInt optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedFloat : OptionalAnimatedValue<AnimatedFloat>
    {
        public static implicit operator OptionalAnimatedFloat(AnimatedFloat value)
        {
            return new OptionalAnimatedFloat()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedFloat(float value)
        {
            return new OptionalAnimatedFloat()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedFloat(OptionalAnimatedFloat optional)
        {
            return optional.value;
        }

        public static implicit operator float(OptionalAnimatedFloat optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedBool : OptionalAnimatedValue<AnimatedBool>
    {
        public static implicit operator OptionalAnimatedBool(AnimatedBool value)
        {
            return new OptionalAnimatedBool()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedBool(bool value)
        {
            return new OptionalAnimatedBool()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedBool(OptionalAnimatedBool optional)
        {
            return optional.value;
        }

        public static implicit operator bool(OptionalAnimatedBool optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedString : OptionalAnimatedValue<AnimatedString>
    {
        public static implicit operator OptionalAnimatedString(AnimatedString value)
        {
            return new OptionalAnimatedString()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedString(string value)
        {
            return new OptionalAnimatedString()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedString(OptionalAnimatedString optional)
        {
            return optional.value;
        }

        public static implicit operator string(OptionalAnimatedString optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedColor : OptionalAnimatedValue<AnimatedColor>
    {
        public static implicit operator OptionalAnimatedColor(AnimatedColor value)
        {
            return new OptionalAnimatedColor()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedColor(Color value)
        {
            return new OptionalAnimatedColor()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedColor(OptionalAnimatedColor optional)
        {
            return optional.value;
        }

        public static implicit operator Color(OptionalAnimatedColor optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedVector2 : OptionalAnimatedValue<AnimatedVector2>
    {
        public static implicit operator OptionalAnimatedVector2(AnimatedVector2 value)
        {
            return new OptionalAnimatedVector2()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedVector2(Vector2 value)
        {
            return new OptionalAnimatedVector2()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedVector2(OptionalAnimatedVector2 optional)
        {
            return optional.value;
        }

        public static implicit operator Vector2(OptionalAnimatedVector2 optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedVector3 : OptionalAnimatedValue<AnimatedVector3>
    {
        public static implicit operator OptionalAnimatedVector3(AnimatedVector3 value)
        {
            return new OptionalAnimatedVector3()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedVector3(Vector3 value)
        {
            return new OptionalAnimatedVector3()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedVector3(OptionalAnimatedVector3 optional)
        {
            return optional.value;
        }

        public static implicit operator Vector3(OptionalAnimatedVector3 optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedVector4 : OptionalAnimatedValue<AnimatedVector4>
    {
        public static implicit operator OptionalAnimatedVector4(AnimatedVector4 value)
        {
            return new OptionalAnimatedVector4()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedVector4(Vector4 value)
        {
            return new OptionalAnimatedVector4()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedVector4(OptionalAnimatedVector4 optional)
        {
            return optional.value;
        }

        public static implicit operator Vector4(OptionalAnimatedVector4 optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedVector2Int : OptionalAnimatedValue<AnimatedVector2Int>
    {
        public static implicit operator OptionalAnimatedVector2Int(AnimatedVector2Int value)
        {
            return new OptionalAnimatedVector2Int()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedVector2Int(Vector2Int value)
        {
            return new OptionalAnimatedVector2Int()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedVector2Int(OptionalAnimatedVector2Int optional)
        {
            return optional.value;
        }

        public static implicit operator Vector2Int(OptionalAnimatedVector2Int optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedVector3Int : OptionalAnimatedValue<AnimatedVector3Int>
    {
        public static implicit operator OptionalAnimatedVector3Int(AnimatedVector3Int value)
        {
            return new OptionalAnimatedVector3Int()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedVector3Int(Vector3Int value)
        {
            return new OptionalAnimatedVector3Int()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedVector3Int(OptionalAnimatedVector3Int optional)
        {
            return optional.value;
        }

        public static implicit operator Vector3Int(OptionalAnimatedVector3Int optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalAnimatedQuaternion : OptionalAnimatedValue<AnimatedQuaternion>
    {
        public static implicit operator OptionalAnimatedQuaternion(AnimatedQuaternion value)
        {
            return new OptionalAnimatedQuaternion()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedQuaternion(Quaternion value)
        {
            return new OptionalAnimatedQuaternion()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedQuaternion(OptionalAnimatedQuaternion optional)
        {
            return optional.value;
        }

        public static implicit operator Quaternion(OptionalAnimatedQuaternion optional)
        {
            return optional.value;
        }
    }
}
