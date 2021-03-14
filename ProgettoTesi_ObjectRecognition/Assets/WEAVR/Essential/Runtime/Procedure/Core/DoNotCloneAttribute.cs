using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class DoNotCloneAttribute : WeavrAttribute
    {
        public bool CutReference { get; private set; }

        public DoNotCloneAttribute(bool cutReference = false)
        {
            CutReference = cutReference;
        }
    }
}
