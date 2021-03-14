using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ConditionOr))]
    class ConditionOrEditor : ConditionNodeEditor
    {

        protected override bool CanDragChildren => false;

        protected override string List_GetSeparatorText(int elementIndex, bool isActive, bool isFocused)
        {
            return "OR";
        }
    }
}
