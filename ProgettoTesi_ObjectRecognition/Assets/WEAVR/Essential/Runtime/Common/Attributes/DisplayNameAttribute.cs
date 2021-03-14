using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
    public class DisplayNameAttribute : WeavrAttribute
    {
        public string DisplayName { get; private set; }
        public string MethodToGetName { get; set; }

        public DisplayNameAttribute(string nameToDisplay) {
            DisplayName = nameToDisplay;
        }

        public DisplayNameAttribute()
        {

        }
    }
}