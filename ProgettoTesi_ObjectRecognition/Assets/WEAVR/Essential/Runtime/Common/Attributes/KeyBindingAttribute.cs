using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    public class KeyBindingAttribute : WeavrAttribute
    {
        public string BindingFieldName { get; private set; }

        public KeyBindingAttribute(string bindingFieldName)
        {
            BindingFieldName = bindingFieldName;
        }
    }
}
