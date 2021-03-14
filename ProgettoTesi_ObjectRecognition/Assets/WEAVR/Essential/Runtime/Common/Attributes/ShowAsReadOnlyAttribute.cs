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
    public class ShowAsReadOnlyAttribute : WeavrAttribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public ShowAsReadOnlyAttribute() { }
    }
}