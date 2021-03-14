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
    public class EnableIfComponentExistsAttribute : WeavrAttribute
    {
        public Type[] ControllingComponents { get; private set; }
        public bool DisableWhenTrue { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controllingComponents">The types of components which can enable/disable the target</param>
        public EnableIfComponentExistsAttribute(params Type[] controllingComponents) {
            ControllingComponents = controllingComponents;
            DisableWhenTrue = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="disableWhenTrue">Whether to disable when the controlling properties value is "True" or other way around</param>
        /// <param name="controllingComponents">The types of components which can enable/disable the target</param>
        public EnableIfComponentExistsAttribute(bool disableWhenTrue, params Type[] controllingComponents) {
            ControllingComponents = controllingComponents;
            DisableWhenTrue = disableWhenTrue;
        }
    }
}