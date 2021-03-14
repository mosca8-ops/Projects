namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PropertyPathAttribute : WeavrAttribute
    {
        public string ObjectPropertyName { get; private set; }

        public PropertyPathAttribute() { }

        public PropertyPathAttribute(string objectPropertyName) {
            ObjectPropertyName = objectPropertyName;
        }
    }
}