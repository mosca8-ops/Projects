using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForcedSetterAttribute : WeavrAttribute
    {
        public string SetterMember { get; private set; }

        public ForcedSetterAttribute(string otherMemberSetterName)
        {
            SetterMember = otherMemberSetterName;
        }
    }
}