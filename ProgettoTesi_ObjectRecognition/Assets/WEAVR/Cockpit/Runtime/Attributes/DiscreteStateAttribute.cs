namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Class)]
    public class DiscreteStateAttribute : WeavrAttribute
    {
        public string StateTypeName { get; private set; }

        public DiscreteStateAttribute(string stateTypeName) {
            StateTypeName = stateTypeName;
        }
    }
}