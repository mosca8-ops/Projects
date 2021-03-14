using System;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class InfoBoxAttribute : WeavrAttribute
    {
        public enum InfoIconType 
        { 
            None = 0, 
            Information = 1, 
            Warning = 2, 
            Error = 3 
        }

        public string InfoText { get; private set; }
        public InfoIconType IconType { get; private set; }

        public InfoBoxAttribute(string infoText) {
            InfoText = infoText;
            IconType = InfoIconType.Information;
        }

        public InfoBoxAttribute(InfoIconType iconType, string infoText)
        {
            InfoText = infoText;
            IconType = iconType;
        }
    }
}