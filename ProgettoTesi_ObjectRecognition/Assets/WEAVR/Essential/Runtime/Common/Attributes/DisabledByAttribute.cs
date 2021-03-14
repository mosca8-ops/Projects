namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    /// <summary>
    /// Attribute to declare which property can disable the target one
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DisabledByAttribute : WeavrAttribute
    {
        public string ControllingProperties { get; private set; }
        public bool DisableWhenTrue { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controllingProperties">The properties separated by comma ';' which can disable the target</param>
        public DisabledByAttribute(string controllingProperties) {
            ControllingProperties = controllingProperties;
            DisableWhenTrue = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controllingProperties">The properties separated by comma ';' which can disable the target</param>
        /// <param name="disableWhenTrue">Whether to disable when the controlling properties value is "True" or other way around</param>
        public DisabledByAttribute(string controllingProperties, bool disableWhenTrue) {
            ControllingProperties = controllingProperties;
            DisableWhenTrue = disableWhenTrue;
        }
    }
}