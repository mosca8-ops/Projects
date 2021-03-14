namespace TXT.WEAVR.Core
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property)]
    public class ShortNameAttribute : WeavrAttribute
    {
        public string ShortName { get; private set; }

        public ShortNameAttribute(string name) {
            ShortName = name;
        }
    }
}