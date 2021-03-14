using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public struct GenericOperator
    {
        public static float epsilon = 0.000001f;

        [SerializeField]
        private ComparisonOperator m_operator;

        public event Action OnChanged;

        public ComparisonOperator Operator
        {
            get => m_operator;
            set
            {
                if(m_operator != value)
                {
                    m_operator = value;
                    OnChanged?.Invoke();
                }
            }
        }

        public bool Evaluate(object operandA, object operandB)
        {
            if(operandA is IComparable && operandA.GetType() == operandB?.GetType())
            {
                int result = (operandA as IComparable).CompareTo(operandB);
                switch (m_operator)
                {
                    case ComparisonOperator.Equals: return result == 0;
                    case ComparisonOperator.NotEquals: return result != 0;
                    case ComparisonOperator.GreaterThan: return result > 0;
                    case ComparisonOperator.GreaterThanOrEquals: return result >= 0;
                    case ComparisonOperator.LessThan: return result < 0;
                    case ComparisonOperator.LessThanOrEquals: return result <= 0;
                    default: return true;
                }
            }
            return operandA == operandB || (operandA != null && operandA.Equals(operandB)) || (operandB != null && operandB.Equals(operandA));
        }

        public object GetPotentialFirstValue(object value)
        {
            switch (m_operator)
            {
                case ComparisonOperator.Equals:
                case ComparisonOperator.LessThanOrEquals:
                case ComparisonOperator.GreaterThanOrEquals:
                    return value;
                case ComparisonOperator.LessThan:
                    switch (value)
                    {
                        case int v: return v - 1;
                        case float v: return v - epsilon;
                        case double v: return v - epsilon;
                        case bool v: return !v;
                        default: return value != default ? default : value;
                    }
                case ComparisonOperator.GreaterThan:
                case ComparisonOperator.NotEquals:
                    switch (value)
                    {
                        case int v: return v + 1;
                        case float v: return v + epsilon;
                        case double v: return v + epsilon;
                        case bool v: return !v;
                        default: return value != default ? default : value;
                    }
            };
            return value;
        }

        public override string ToString()
        {
            return m_operator.ToMathString();
        }
    }
}
