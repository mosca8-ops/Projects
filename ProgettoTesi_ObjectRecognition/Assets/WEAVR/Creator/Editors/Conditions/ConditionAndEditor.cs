using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ConditionAnd))]
    class ConditionAndEditor : ConditionNodeEditor
    {

        protected override bool CanDragChildren => false;

        protected override string List_GetSeparatorText(int elementIndex, bool isActive, bool isFocused)
        {
            return "AND";
        }
    }
}
