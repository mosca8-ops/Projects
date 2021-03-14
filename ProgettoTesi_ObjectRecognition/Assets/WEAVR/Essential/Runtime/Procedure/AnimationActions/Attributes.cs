using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InputDataAttribute : WeavrAttribute
    {
        public string Type { get; private set; }

        public InputDataAttribute(string type)
        {
            Type = (type ?? "").ToLower();
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OutputDataAttribute : WeavrAttribute
    {
        public string Type { get; private set; }

        public OutputDataAttribute(string type)
        {
            Type = (type ?? "").ToLower();
        }
    }
}
