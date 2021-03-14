namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    public abstract class OverrideAccessorsAttribute : PropertyAttribute
    {
        public virtual Func<object, object> GetGetter(object obj, MemberInfo memberInfo, Func<object, object> fallbackGetter)
        {
            return fallbackGetter;
        }

        public virtual Action<object, object> GetSetter(object obj, MemberInfo memberInfo, Action<object, object> fallbackSetter)
        {
            return fallbackSetter;
        }

        public virtual Func<object, object> GetConverter(object obj, MemberInfo memberInfo, Func<object, object> fallbackConverter)
        {
            return fallbackConverter;
        }

        public virtual Func<object, object> GetGetter(Type ownerType, MemberInfo memberInfo, Func<object, object> fallbackGetter)
        {
            return fallbackGetter;
        }

        public virtual Action<object, object> GetSetter(Type ownerType, MemberInfo memberInfo, Action<object, object> fallbackSetter)
        {
            return fallbackSetter;
        }

        public virtual Func<object, object> GetConverter(Type ownerType, MemberInfo memberInfo, Func<object, object> fallbackConverter)
        {
            return fallbackConverter;
        }
    }
}