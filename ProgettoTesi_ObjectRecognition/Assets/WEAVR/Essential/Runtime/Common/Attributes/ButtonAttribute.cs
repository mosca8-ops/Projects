using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class ButtonAttribute : WeavrAttribute
    {
        public string MethodName { get; private set; }
        public string ValidationMethodName { get; private set; }
        public string Label { get; private set; }
        public float Height { get; private set; }
        public bool Inline { get; private set; }
        public float? Width { get; private set; }
        
        public ButtonAttribute(string methodName, string label = null, string validationMethod = null, float height = 16, bool inline = true, float width = 0)
        {
            MethodName = methodName;
            Label = label ?? methodName;
            Height = height;
            Width = width <= 0 ? (float?)null : width;
            Inline = inline;
            ValidationMethodName = validationMethod;
        }
    }
}