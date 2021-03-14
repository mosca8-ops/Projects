using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PropertyDataFromAttribute : WeavrAttribute
    {
        public string TargetFieldName { get; private set; }
        public string TypeFieldName { get; private set; }
        public bool IsSetter { get; private set; }
        public string TypeFilterGetMethod { get; set; }

        public PropertyDataFromAttribute(string targetFieldName, bool isSetter = true)
        {
            TargetFieldName = targetFieldName;
            IsSetter = isSetter;
        }

        public PropertyDataFromAttribute(string targetFieldName, string typeFieldName, bool isSetter = true)
        {
            TargetFieldName = targetFieldName;
            TypeFieldName = typeFieldName;
            IsSetter = isSetter;
        }
    }
}
