using System;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Common
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PopupIntAttribute : WeavrAttribute
    {
        public int Min { get; private set; }
        public int Max { get; private set; }
        public string Prelabel { get; private set; }
        public int Step { get; private set; }

        public PopupIntAttribute(int min, int max, string preLabel = null, int step = 1)
        {
            Min = min;
            Max = max;
            Prelabel = string.IsNullOrEmpty(preLabel) ? string.Empty : preLabel + " ";
            Step = step < 1 ? 1 : step;
        }
    }
}
