using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SpecialStateContainerAttribute : PropertyAttribute
    {
        public Type ComponentType { get; private set; }

        public SpecialStateContainerAttribute(Type componentType)
        {
            ComponentType = componentType;
        }
    }
}
