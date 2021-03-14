using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    /// <summary>
    /// This attribute marks the member | class | struct as stateless, 
    /// which means it won't be considered for states save/restore logic
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    public class StatelessAttribute : WeavrAttribute
    {
        
    }
}
