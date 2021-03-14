using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AssignableFromAttribute : WeavrAttribute
    {
        public string VariableName { get; set; }
        public string VariableFieldName { get; private set; }
        public ValuesStorage.AccessType AccessType { get; set; } = ValuesStorage.AccessType.ReadWrite;

        public AssignableFromAttribute(string variableFieldName)
        {
            VariableFieldName = variableFieldName;
        }

        public AssignableFromAttribute()
        {

        }
    }
}
