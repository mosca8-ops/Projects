namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Class)]
    public class StateDrawerAttribute : WeavrAttribute
    {
        public Type StateType { get; private set; }

        public StateDrawerAttribute(Type stateType) {
            StateType = stateType;
        }
    }
}