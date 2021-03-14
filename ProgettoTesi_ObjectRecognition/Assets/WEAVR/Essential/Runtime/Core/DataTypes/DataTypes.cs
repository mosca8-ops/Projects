using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    public enum ComparisonOperator
    {
        Equals = 0,
        LessThan = 1,
        LessThanOrEquals = 2,
        GreaterThan = 3,
        GreaterThanOrEquals = 4,
        NotEquals = 5,
    }

    public static class ComparisonOperatorExtensions
    {
        private static readonly ComparisonOperator[] _OPERATORS = {
            ComparisonOperator.Equals,
            ComparisonOperator.LessThan,
            ComparisonOperator.LessThanOrEquals,
            ComparisonOperator.GreaterThan,
            ComparisonOperator.GreaterThanOrEquals,
            ComparisonOperator.NotEquals,
        };
        
        /// <summary>
        /// Prints the operator to a representative symbol
        /// </summary>
        /// <param name="op"></param>
        /// <returns>The string representing this operation</returns>
        public static string ToMathString(this ComparisonOperator op) {
            switch (op) {
                case ComparisonOperator.Equals:
                    return "=";
                case ComparisonOperator.LessThan:
                    return "<";
                case ComparisonOperator.LessThanOrEquals:
                    return @"≤";
                case ComparisonOperator.GreaterThan:
                    return ">";
                case ComparisonOperator.GreaterThanOrEquals:
                    return @"≥";
                case ComparisonOperator.NotEquals:
                    return @"≠";
            }
            return null;
        }

        /// <summary>
        /// Parses the <see cref="string"/> to a <see cref="ComparisonOperator"/> 
        /// or <see cref="ComparisonOperator.None"/> if parsing fails.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="s">The string to parse</param>
        /// <returns></returns>
        public static ComparisonOperator Parse(this ComparisonOperator op, string s) {
            switch (s) {
                case "=":
                    return ComparisonOperator.Equals;
                case "<":
                    return ComparisonOperator.LessThan;
                case ">":
                    return ComparisonOperator.GreaterThan;
                case "<=":
                case @"≤":
                    return ComparisonOperator.LessThanOrEquals;
                case ">=":
                case @"≥":
                    return ComparisonOperator.GreaterThanOrEquals;
                case "!=":
                case "<>":
                case @"≠":
                    return ComparisonOperator.NotEquals;

            }
            return ComparisonOperator.Equals;
        }

        /// <summary>
        /// Gets the next <see cref="ComparisonOperator"/>. Think of this method as a roulette.
        /// </summary>
        /// <param name="op"></param>
        /// <returns>The next or first <see cref="ComparisonOperator"/></returns>
        public static ComparisonOperator Next(this ComparisonOperator op) {
            return _OPERATORS[(int)(op + 1) % _OPERATORS.Length];
        }

        /// <summary>
        /// Gets the previous <see cref="ComparisonOperator"/>. Think of this method as a roulette.
        /// </summary>
        /// <param name="op"></param>
        /// <returns>The previous or last <see cref="ComparisonOperator"/></returns>
        public static ComparisonOperator Previous(this ComparisonOperator op) {
            int previous = (int)(op - 1);
            return _OPERATORS[previous < 0 ? _OPERATORS.Length - 1 : previous];
        }
    }
}