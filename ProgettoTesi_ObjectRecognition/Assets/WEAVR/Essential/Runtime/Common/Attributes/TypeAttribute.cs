using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TypeAttribute : WeavrAttribute
    {
        public Type Type { get; private set; }

        public TypeAttribute(Type type)
        {
            Type = type;
        }
    }
}
