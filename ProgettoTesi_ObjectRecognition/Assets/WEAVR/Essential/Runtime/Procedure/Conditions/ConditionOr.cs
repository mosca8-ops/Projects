using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ConditionOr : ConditionNode
    {
        protected override bool EvaluateCondition()
        {
            foreach(var child in Children)
            {
                if (child.Value)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToFullString()
        {
            if (Children.Count == 0) { return "[]"; }
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            foreach (var child in Children)
            {
                if (child)
                {
                    sb.Append(child.ToFullString()).Append(" OR ");
                }
            }
            sb.Remove(sb.Length - " OR ".Length, " OR ".Length);
            return sb.Append(']').ToString();
        }
    }
}
