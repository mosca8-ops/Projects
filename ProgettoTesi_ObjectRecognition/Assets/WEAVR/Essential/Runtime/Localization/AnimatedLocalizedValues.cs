using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [Serializable]
    public class AnimatedLocalizedString : AnimatedValue<LocalizedString>
    {
        protected override LocalizedString Interpolate(LocalizedString a, LocalizedString b, float ratio)
        {
            return b?.CurrentValue.Substring(0, (int)(b.CurrentValue.Length * ratio));
        }

        public static implicit operator AnimatedLocalizedString(string value)
        {
            return new AnimatedLocalizedString() { m_target = value };
        }

        public static implicit operator AnimatedLocalizedString(LocalizedString value)
        {
            return new AnimatedLocalizedString() { m_target = value };
        }
    }

    [Serializable]
    public class OptionalAnimatedLocalizedString : OptionalAnimatedValue<AnimatedLocalizedString>
    {
        public static implicit operator OptionalAnimatedLocalizedString(AnimatedLocalizedString value)
        {
            return new OptionalAnimatedLocalizedString()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedLocalizedString(LocalizedString value)
        {
            return new OptionalAnimatedLocalizedString()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator OptionalAnimatedLocalizedString(string value)
        {
            return new OptionalAnimatedLocalizedString()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator AnimatedLocalizedString(OptionalAnimatedLocalizedString optional)
        {
            return optional.value;
        }

        public static implicit operator LocalizedString(OptionalAnimatedLocalizedString optional)
        {
            return optional.value;
        }

        public static implicit operator string(OptionalAnimatedLocalizedString optional)
        {
            return optional.value.Value;
        }
    }
}
