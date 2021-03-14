using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    /// <summary>
    /// Fills in null values of other languages with current value, but only when changing it
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AutoFillAttribute : WeavrAttribute
    {
        
    }
}
