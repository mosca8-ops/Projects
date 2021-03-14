using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class RangeFromAttribute : WeavrAttribute
    {
        public float? Min { get; private set; }
        public float? Max { get; private set; }

        public float MinOffset { get; set; }
        public float MaxOffset { get; set; }

        public string MinField { get; private set; }
        public string MaxField { get; private set; }

        /// <summary>
        /// Range for element
        /// </summary>
        /// <param name="min">The lower bound</param>
        /// <param name="max">The upper bound</param>
        public RangeFromAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Range for element
        /// </summary>
        /// <param name="min">The lower bound</param>
        /// <param name="maxField">The object to get upper bound from</param>
        public RangeFromAttribute(float min, string maxField)
        {
            Min = min;
            MaxField = maxField;
        }

        /// <summary>
        /// Range for element
        /// </summary>
        /// <param name="minField">The object to get upper bound from</param>
        /// <param name="max">The upper bound</param>
        public RangeFromAttribute(string minField, float max)
        {
            MinField = minField;
            Max = max;
        }

        /// <summary>
        /// Range for element
        /// </summary>
        /// <param name="minField">The object to get upper bound from</param>
        /// <param name="maxField">The object to get upper bound from</param>
        public RangeFromAttribute(string minField, string maxField)
        {
            MinField = minField;
            MaxField = maxField;
        }
    }
}