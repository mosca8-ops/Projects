using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Common
{

    public class ReorderableListAttribute : WeavrAttribute
    {
        public bool Collapsing { get; private set; }

        public ReorderableListAttribute(bool collapsing = true)
        {
            Collapsing = collapsing;
        }
    }
}
