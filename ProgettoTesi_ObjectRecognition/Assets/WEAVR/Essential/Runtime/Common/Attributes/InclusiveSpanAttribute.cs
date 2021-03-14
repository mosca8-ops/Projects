namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InclusiveSpanAttribute : WeavrAttribute
    {
        public string ControllingSpan { get; private set; }
        
        public InclusiveSpanAttribute(string controllingSpan) {
            ControllingSpan = controllingSpan;
        }
    }
}