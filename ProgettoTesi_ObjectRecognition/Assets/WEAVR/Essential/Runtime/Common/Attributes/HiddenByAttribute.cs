using System;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Common
{
    /// <summary>
    /// Attribute to declare which property can hide the target one
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HiddenByAttribute : WeavrAttribute
    {
        public string ControllingProperties { get; private set; }
        public bool HideWhenTrue { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controllingProperties">The properties separated by comma ';' which can hide the target</param>
        public HiddenByAttribute(string controllingProperties)
        {
            ControllingProperties = controllingProperties;
            HideWhenTrue = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controllingProperties">The properties which can hide the target</param>
        public HiddenByAttribute(params string[] controllingProperties)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < controllingProperties.Length - 1; i++)
            {
                sb.Append(controllingProperties[i]).Append(";");
            }
            sb.Append(controllingProperties[controllingProperties.Length - 1]);
            ControllingProperties = sb.ToString();
            HideWhenTrue = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="controllingProperties">The properties separated by comma ';' which can hide the target</param>
        /// <param name="hiddenWhenTrue">Whether to hide when the controlling properties value is "True" or other way around</param>
        public HiddenByAttribute(string controllingProperties, bool hiddenWhenTrue)
        {
            ControllingProperties = controllingProperties;
            HideWhenTrue = hiddenWhenTrue;
        }
    }
}