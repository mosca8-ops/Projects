using System;
using TXT.WEAVR.Core;

namespace TXT.WEAVR.Common
{

    /// <summary>
    /// This attribute marks the field or the property as ignorable for save/restore logic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IgnoreStateSerializationAttribute : WeavrAttribute
    {
        
    }
}
