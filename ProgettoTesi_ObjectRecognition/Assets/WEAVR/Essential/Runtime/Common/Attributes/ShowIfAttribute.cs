namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    /// <summary>
    /// Attribute to declare a property to be shown as Read Only
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ShowIfAttribute : WeavrAttribute
    {
        public string MethodPath { get; private set; }
        public bool Invert { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="methodPath">The path to the method to validate. Should be parameterless and return bool</param>
        public ShowIfAttribute(string methodPath, bool invert = false) {
            MethodPath = methodPath;
            Invert = invert;
        }
    }
}