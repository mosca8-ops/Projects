using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AnimationDataFromAttribute : WeavrAttribute
    {
        public string DurationProperty { get; set; }
        public string CurveProperty { get; set; }

        public AnimationDataFromAttribute(string durationPropertyOrMethodPath) : this(durationPropertyOrMethodPath, null) { }

        public AnimationDataFromAttribute(string durationPropertyOrMethodPath, string curvePropertyName)
        {
            DurationProperty = durationPropertyOrMethodPath;
            CurveProperty = curvePropertyName;
        }
    }
}
