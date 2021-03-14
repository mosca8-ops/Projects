namespace TXT.WEAVR.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
    public class DoNotExposeAttribute : WeavrAttribute
    {
        
    }
}