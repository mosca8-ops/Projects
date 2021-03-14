using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public static class ProcedureExtensions
    {

        public static T To<T>(this ITargetingObject targetting, Object value, T defaultValue) where T : Component
        {
            if(value == null)
            {
                return null;
            }
            else if(value is T t)
            {
                return t;
            }
            else if(value is Component c)
            {
                var ct = c.GetComponent<T>();
                return ct ? ct : defaultValue;
            }
            else if(value is GameObject go)
            {
                var got = go.GetComponent<T>();
                return got ? got : defaultValue;
            }
            return defaultValue;
        }

        public static GameObject To(this ITargetingObject targetting, Object value, GameObject defaultValue)
        {
            return value == null ? null : value is GameObject go ? go : value is Component c ? c.gameObject : defaultValue;
        }

        public static Transform To(this ITargetingObject targetting, Object value, Transform defaultValue)
        {
            return value == null ? null : value is GameObject go ? go.transform : value is Component c ? c.transform : defaultValue;
        }
    }
}
