using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Debugging
{

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class CanDebugAttribute : WeavrAttribute
    {

    }
}
