using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class BillboardModifierAttribute : WeavrAttribute
    {
        public Type ElementType { get; private set; }
        public BillboardModifierAttribute(Type elementType)
        {
            ElementType = elementType;
        }
    }
}
