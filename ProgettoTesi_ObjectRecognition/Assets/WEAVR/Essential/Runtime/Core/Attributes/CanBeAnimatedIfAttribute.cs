using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{
    public class CanBeAnimatedIfAttribute : WeavrAttribute
    {
        public string MethodName { get; private set; }

        public CanBeAnimatedIfAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}