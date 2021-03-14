using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InvokeOnChangeAttribute : WeavrAttribute
    {
        public string MethodName { get; private set; }
        
        public InvokeOnChangeAttribute(string methodName) {
            MethodName = methodName;
        }
    }
}
