using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ConditionAnd : ConditionNode
    {
        protected override bool EvaluateCondition()
        {
            foreach (var child in Children)
            {
                if (!child.Value)
                {
                    return false;
                }
            }

            return Children.Count > 0;
        }

        public override string ToFullString()
        {
            if(Children.Count == 0) { return "[]"; }
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            foreach(var child in Children)
            {
                if (child)
                {
                    sb.Append(child.ToFullString()).Append(" AND ");
                }
            }
            sb.Remove(sb.Length - " AND ".Length, " AND ".Length);
            return sb.Append(']').ToString();
        }
    }
}