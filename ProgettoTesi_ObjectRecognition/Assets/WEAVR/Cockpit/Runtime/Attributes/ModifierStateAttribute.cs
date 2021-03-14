namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Class)]
    public class ModifierStateAttribute : WeavrAttribute
    {
        public string StateTypeName { get; private set; }

        public ModifierStateAttribute(string stateTypeName) {
            StateTypeName = stateTypeName;
        }
    }
}